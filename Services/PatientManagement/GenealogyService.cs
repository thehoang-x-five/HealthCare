using System.Globalization;
using System.Text;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace HealthCare.Services.PatientManagement
{
    public class GenealogyService : IGenealogyService
    {
        private readonly DataContext _context;

        public GenealogyService(DataContext context)
        {
            _context = context;
        }

        public async Task<GenealogyTreeDto> GetGenealogyTreeAsync(string maBenhNhan)
        {
            var patient = await _context.BenhNhans
                .AsNoTracking()
                .FirstOrDefaultAsync(bn => bn.MaBenhNhan == maBenhNhan)
                ?? throw new KeyNotFoundException($"Không tìm thấy bệnh nhân {maBenhNhan}");

            var result = new GenealogyTreeDto { MaBenhNhanGoc = maBenhNhan };

            var ancestors = await CallSpGetFamilyAsync("sp_GetAncestors", maBenhNhan);
            var descendants = await CallSpGetFamilyAsync("sp_GetDescendants", maBenhNhan);

            var allIds = new HashSet<string>();

            foreach (var ancestor in ancestors)
            {
                if (!allIds.Add(ancestor.MaBenhNhan))
                {
                    continue;
                }

                var relation = "self";
                if (ancestor.MaBenhNhan == patient.MaCha)
                {
                    relation = "cha";
                }
                else if (ancestor.MaBenhNhan == patient.MaMe)
                {
                    relation = "me";
                }
                else if (ancestor.MaBenhNhan != maBenhNhan)
                {
                    relation = "to_tien";
                }

                result.Nodes.Add(new GenealogyNodeDto
                {
                    MaBenhNhan = ancestor.MaBenhNhan,
                    HoTen = ancestor.HoTen ?? "-",
                    NgaySinh = ancestor.NgaySinh ?? DateTime.MinValue,
                    GioiTinh = ancestor.GioiTinh ?? string.Empty,
                    NhomMau = ancestor.NhomMau,
                    BenhManTinh = ancestor.BenhManTinh,
                    MaCha = ancestor.MaCha,
                    MaMe = ancestor.MaMe,
                    QuanHe = relation,
                    DoiThu = relation == "self" ? 0 : -1
                });
            }

            foreach (var descendant in descendants)
            {
                if (!allIds.Add(descendant.MaBenhNhan))
                {
                    continue;
                }

                var relation = "con";
                if ((descendant.MaCha == patient.MaCha && patient.MaCha != null) ||
                    (descendant.MaMe == patient.MaMe && patient.MaMe != null))
                {
                    relation = "anh_chi_em";
                }

                result.Nodes.Add(new GenealogyNodeDto
                {
                    MaBenhNhan = descendant.MaBenhNhan,
                    HoTen = descendant.HoTen ?? "-",
                    NgaySinh = descendant.NgaySinh ?? DateTime.MinValue,
                    GioiTinh = descendant.GioiTinh ?? string.Empty,
                    NhomMau = descendant.NhomMau,
                    BenhManTinh = descendant.BenhManTinh,
                    MaCha = descendant.MaCha,
                    MaMe = descendant.MaMe,
                    QuanHe = relation,
                    DoiThu = 1
                });
            }

            if (patient.MaCha != null || patient.MaMe != null)
            {
                var siblings = await _context.BenhNhans
                    .AsNoTracking()
                    .Where(bn => bn.MaBenhNhan != maBenhNhan &&
                        ((patient.MaCha != null && bn.MaCha == patient.MaCha) ||
                         (patient.MaMe != null && bn.MaMe == patient.MaMe)))
                    .ToListAsync();

                foreach (var sibling in siblings)
                {
                    if (!allIds.Add(sibling.MaBenhNhan))
                    {
                        continue;
                    }

                    result.Nodes.Add(new GenealogyNodeDto
                    {
                        MaBenhNhan = sibling.MaBenhNhan,
                        HoTen = sibling.HoTen,
                        NgaySinh = sibling.NgaySinh,
                        GioiTinh = sibling.GioiTinh,
                        NhomMau = sibling.NhomMau,
                        BenhManTinh = sibling.BenhManTinh,
                        MaCha = sibling.MaCha,
                        MaMe = sibling.MaMe,
                        QuanHe = "anh_chi_em",
                        DoiThu = 0
                    });
                }
            }

            return result;
        }

        public async Task<GenealogyNodeDto> LinkParentsAsync(string maBenhNhan, LinkParentsRequest request)
        {
            var patient = await _context.BenhNhans
                .FirstOrDefaultAsync(bn => bn.MaBenhNhan == maBenhNhan)
                ?? throw new KeyNotFoundException($"Không tìm thấy bệnh nhân {maBenhNhan}");

            var maCha = NormalizeId(request.MaCha);
            var maMe = NormalizeId(request.MaMe);

            if (maCha == maBenhNhan || maMe == maBenhNhan)
            {
                throw new ArgumentException("Bệnh nhân không thể là cha hoặc mẹ của chính mình.");
            }

            if (maCha != null && maMe != null && maCha == maMe)
            {
                throw new ArgumentException("Không thể gán cùng một bệnh nhân làm cả cha và mẹ.");
            }

            if (maCha != null)
            {
                var cha = await _context.BenhNhans.FindAsync(maCha)
                    ?? throw new KeyNotFoundException($"Không tìm thấy cha (MaBN: {maCha})");

                ValidateParentCandidate(patient, cha, ParentRole.Father);

                if (await IsDescendantOfAsync(maCha, maBenhNhan))
                {
                    throw new ArgumentException($"Không thể gán {maCha} làm cha vì tạo vòng lặp gia phả.");
                }
            }

            if (maMe != null)
            {
                var me = await _context.BenhNhans.FindAsync(maMe)
                    ?? throw new KeyNotFoundException($"Không tìm thấy mẹ (MaBN: {maMe})");

                ValidateParentCandidate(patient, me, ParentRole.Mother);

                if (await IsDescendantOfAsync(maMe, maBenhNhan))
                {
                    throw new ArgumentException($"Không thể gán {maMe} làm mẹ vì tạo vòng lặp gia phả.");
                }
            }

            patient.MaCha = maCha;
            patient.MaMe = maMe;
            patient.NgayCapNhat = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ArgumentException(ExtractPersistMessage(ex), ex);
            }

            return new GenealogyNodeDto
            {
                MaBenhNhan = patient.MaBenhNhan,
                HoTen = patient.HoTen,
                NgaySinh = patient.NgaySinh,
                GioiTinh = patient.GioiTinh,
                NhomMau = patient.NhomMau,
                BenhManTinh = patient.BenhManTinh,
                MaCha = patient.MaCha,
                MaMe = patient.MaMe,
                QuanHe = "self",
                DoiThu = 0
            };
        }

        public async Task<FamilyDiseaseSummaryDto> GetFamilyDiseasesAsync(string maBenhNhan)
        {
            var tree = await GetGenealogyTreeAsync(maBenhNhan);
            var familyIds = tree.Nodes.Select(n => n.MaBenhNhan).ToList();

            var members = await _context.BenhNhans
                .AsNoTracking()
                .Where(bn => familyIds.Contains(bn.MaBenhNhan))
                .Select(bn => new
                {
                    bn.MaBenhNhan,
                    bn.HoTen,
                    bn.BenhManTinh,
                    bn.TieuSuBenh,
                    bn.DiUng
                })
                .ToListAsync();

            var result = new FamilyDiseaseSummaryDto
            {
                MaBenhNhanGoc = maBenhNhan
            };

            var nodeMap = tree.Nodes.ToDictionary(n => n.MaBenhNhan);

            foreach (var member in members)
            {
                nodeMap.TryGetValue(member.MaBenhNhan, out var node);

                result.ThanhVien.Add(new FamilyDiseaseDto
                {
                    MaBenhNhan = member.MaBenhNhan,
                    HoTen = member.HoTen,
                    QuanHe = node?.QuanHe ?? "unknown",
                    BenhManTinh = member.BenhManTinh,
                    TieuSuBenh = member.TieuSuBenh,
                    DiUng = member.DiUng
                });

                if (string.IsNullOrWhiteSpace(member.BenhManTinh))
                {
                    continue;
                }

                var diseases = member.BenhManTinh.Split(',', StringSplitOptions.TrimEntries);
                foreach (var disease in diseases)
                {
                    if (result.ThongKeBenhGiaDinh.ContainsKey(disease))
                    {
                        result.ThongKeBenhGiaDinh[disease]++;
                    }
                    else
                    {
                        result.ThongKeBenhGiaDinh[disease] = 1;
                    }
                }
            }

            return result;
        }

        private async Task<List<FamilyRowDto>> CallSpGetFamilyAsync(string spName, string maBenhNhan)
        {
            var results = new List<FamilyRowDto>();
            var conn = _context.Database.GetDbConnection();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CALL {spName}(@p0)";
                cmd.CommandType = System.Data.CommandType.Text;

                var param = cmd.CreateParameter();
                param.ParameterName = "@p0";
                param.Value = maBenhNhan;
                cmd.Parameters.Add(param);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new FamilyRowDto
                    {
                        MaBenhNhan = reader.GetString(reader.GetOrdinal("MaBenhNhan")),
                        HoTen = reader.IsDBNull(reader.GetOrdinal("HoTen")) ? null : reader.GetString(reader.GetOrdinal("HoTen")),
                        NgaySinh = reader.IsDBNull(reader.GetOrdinal("NgaySinh")) ? null : reader.GetDateTime(reader.GetOrdinal("NgaySinh")),
                        GioiTinh = reader.IsDBNull(reader.GetOrdinal("GioiTinh")) ? null : reader.GetString(reader.GetOrdinal("GioiTinh")),
                        NhomMau = reader.IsDBNull(reader.GetOrdinal("NhomMau")) ? null : reader.GetString(reader.GetOrdinal("NhomMau")),
                        BenhManTinh = reader.IsDBNull(reader.GetOrdinal("BenhManTinh")) ? null : reader.GetString(reader.GetOrdinal("BenhManTinh")),
                        MaCha = reader.IsDBNull(reader.GetOrdinal("MaCha")) ? null : reader.GetString(reader.GetOrdinal("MaCha")),
                        MaMe = reader.IsDBNull(reader.GetOrdinal("MaMe")) ? null : reader.GetString(reader.GetOrdinal("MaMe"))
                    });
                }
            }
            catch
            {
                // If stored procedures are missing or fail, return empty data instead of crashing.
            }

            return results;
        }

        private async Task<bool> IsDescendantOfAsync(string candidateId, string ancestorId)
        {
            var conn = _context.Database.GetDbConnection();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CALL sp_IsDescendantOf(@p0, @p1)";
                cmd.CommandType = System.Data.CommandType.Text;

                var param0 = cmd.CreateParameter();
                param0.ParameterName = "@p0";
                param0.Value = ancestorId;
                cmd.Parameters.Add(param0);

                var param1 = cmd.CreateParameter();
                param1.ParameterName = "@p1";
                param1.Value = candidateId;
                cmd.Parameters.Add(param1);

                using var reader = await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync();
            }
            catch
            {
                return false;
            }
        }

        private static void ValidateParentCandidate(BenhNhan child, BenhNhan parent, ParentRole role)
        {
            if (role == ParentRole.Father && !IsMaleGender(parent.GioiTinh))
            {
                throw new ArgumentException(
                    $"Không thể gán {parent.MaBenhNhan} làm cha vì giới tính hiện tại là '{FormatGender(parent.GioiTinh)}'.");
            }

            if (role == ParentRole.Mother && !IsFemaleGender(parent.GioiTinh))
            {
                throw new ArgumentException(
                    $"Không thể gán {parent.MaBenhNhan} làm mẹ vì giới tính hiện tại là '{FormatGender(parent.GioiTinh)}'.");
            }

            var relationLabel = role == ParentRole.Father ? "cha" : "mẹ";
            var childBirthDate = child.NgaySinh.Date;
            var parentBirthDate = parent.NgaySinh.Date;

            if (parentBirthDate >= childBirthDate)
            {
                throw new ArgumentException(
                    $"Không thể gán {parent.MaBenhNhan} làm {relationLabel} vì ngày sinh của {relationLabel} phải sớm hơn ngày sinh của con.");
            }

            if (parentBirthDate.AddYears(12) > childBirthDate)
            {
                throw new ArgumentException(
                    $"Không thể gán {parent.MaBenhNhan} làm {relationLabel} vì khoảng cách tuổi {relationLabel}/con không hợp lệ.");
            }
        }

        private static string? NormalizeId(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsMaleGender(string? value)
        {
            var normalized = NormalizeGender(value);
            return normalized is "nam" or "male" or "m";
        }

        private static bool IsFemaleGender(string? value)
        {
            var normalized = NormalizeGender(value);
            return normalized is "nu" or "female" or "f";
        }

        private static string FormatGender(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "chưa xác định" : value.Trim();
        }

        private static string NormalizeGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd');
        }

        private static string ExtractPersistMessage(DbUpdateException exception)
        {
            if (exception.InnerException is MySqlException mySqlException &&
                !string.IsNullOrWhiteSpace(mySqlException.Message))
            {
                return mySqlException.Message;
            }

            if (!string.IsNullOrWhiteSpace(exception.InnerException?.Message))
            {
                return exception.InnerException.Message;
            }

            return "Không thể lưu liên kết cha/mẹ do dữ liệu không hợp lệ.";
        }

        private enum ParentRole
        {
            Father,
            Mother
        }

        private class FamilyRowDto
        {
            public string MaBenhNhan { get; set; } = string.Empty;
            public string? HoTen { get; set; }
            public DateTime? NgaySinh { get; set; }
            public string? GioiTinh { get; set; }
            public string? NhomMau { get; set; }
            public string? BenhManTinh { get; set; }
            public string? MaCha { get; set; }
            public string? MaMe { get; set; }
        }
    }
}
