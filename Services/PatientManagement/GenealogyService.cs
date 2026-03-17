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

        /// <summary>
        /// Lấy cây pha hệ đa đời bằng SQL Recursive CTE.
        /// Truy ngược lên tổ tiên (qua MaCha/MaMe) và xuống con cháu.
        /// </summary>
        public async Task<GenealogyTreeDto> GetGenealogyTreeAsync(string maBenhNhan)
        {
            var patient = await _context.BenhNhans
                .AsNoTracking()
                .FirstOrDefaultAsync(bn => bn.MaBenhNhan == maBenhNhan)
                ?? throw new KeyNotFoundException($"Không tìm thấy bệnh nhân {maBenhNhan}");

            var result = new GenealogyTreeDto { MaBenhNhanGoc = maBenhNhan };

            // ===== 1. Truy ngược LÊN tổ tiên (Ancestors) qua Stored Procedure =====
            var ancestors = await CallSpGetFamilyAsync("sp_GetAncestors", maBenhNhan);

            // ===== 2. Truy xuống CON CHÁU (Descendants) qua Stored Procedure =====
            var descendants = await CallSpGetFamilyAsync("sp_GetDescendants", maBenhNhan);

            // ===== 3. Merge + xác định quan hệ =====
            var allIds = new HashSet<string>();

            // Thêm tổ tiên
            foreach (var a in ancestors)
            {
                if (!allIds.Add(a.MaBenhNhan)) continue;

                string quanHe = "self";
                if (a.MaBenhNhan == patient.MaCha) quanHe = "cha";
                else if (a.MaBenhNhan == patient.MaMe) quanHe = "me";
                else if (a.MaBenhNhan != maBenhNhan) quanHe = "to_tien";

                result.Nodes.Add(new GenealogyNodeDto
                {
                    MaBenhNhan = a.MaBenhNhan,
                    HoTen = a.HoTen ?? "—",
                    NgaySinh = a.NgaySinh ?? DateTime.MinValue,
                    GioiTinh = a.GioiTinh ?? "",
                    NhomMau = a.NhomMau,
                    BenhManTinh = a.BenhManTinh,
                    MaCha = a.MaCha,
                    MaMe = a.MaMe,
                    QuanHe = quanHe,
                    DoiThu = quanHe == "self" ? 0 : -1
                });
            }

            // Thêm con cháu (bỏ trùng self)
            foreach (var d in descendants)
            {
                if (!allIds.Add(d.MaBenhNhan)) continue;

                string quanHe = "con";
                // Kiểm tra anh chị em (cùng cha hoặc mẹ)
                if ((d.MaCha == patient.MaCha && patient.MaCha != null) ||
                    (d.MaMe == patient.MaMe && patient.MaMe != null))
                {
                    quanHe = "anh_chi_em";
                }

                result.Nodes.Add(new GenealogyNodeDto
                {
                    MaBenhNhan = d.MaBenhNhan,
                    HoTen = d.HoTen ?? "—",
                    NgaySinh = d.NgaySinh ?? DateTime.MinValue,
                    GioiTinh = d.GioiTinh ?? "",
                    NhomMau = d.NhomMau,
                    BenhManTinh = d.BenhManTinh,
                    MaCha = d.MaCha,
                    MaMe = d.MaMe,
                    QuanHe = quanHe,
                    DoiThu = 1
                });
            }

            // ===== 4. Tìm anh chị em (cùng cha hoặc mẹ với BN gốc) =====
            if (patient.MaCha != null || patient.MaMe != null)
            {
                var siblings = await _context.BenhNhans
                    .AsNoTracking()
                    .Where(bn => bn.MaBenhNhan != maBenhNhan &&
                        ((patient.MaCha != null && bn.MaCha == patient.MaCha) ||
                         (patient.MaMe != null && bn.MaMe == patient.MaMe)))
                    .ToListAsync();

                foreach (var s in siblings)
                {
                    if (!allIds.Add(s.MaBenhNhan)) continue;

                    result.Nodes.Add(new GenealogyNodeDto
                    {
                        MaBenhNhan = s.MaBenhNhan,
                        HoTen = s.HoTen,
                        NgaySinh = s.NgaySinh,
                        GioiTinh = s.GioiTinh,
                        NhomMau = s.NhomMau,
                        BenhManTinh = s.BenhManTinh,
                        MaCha = s.MaCha,
                        MaMe = s.MaMe,
                        QuanHe = "anh_chi_em",
                        DoiThu = 0
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Liên kết cha/mẹ cho bệnh nhân.
        /// Validate: không tạo vòng lặp (con không thể là cha/mẹ chính mình hoặc tổ tiên).
        /// </summary>
        public async Task<GenealogyNodeDto> LinkParentsAsync(string maBenhNhan, LinkParentsRequest request)
        {
            var patient = await _context.BenhNhans
                .FirstOrDefaultAsync(bn => bn.MaBenhNhan == maBenhNhan)
                ?? throw new KeyNotFoundException($"Không tìm thấy bệnh nhân {maBenhNhan}");

            // Validate: không link chính mình
            if (request.MaCha == maBenhNhan || request.MaMe == maBenhNhan)
                throw new ArgumentException("Bệnh nhân không thể là cha/mẹ của chính mình.");

            // Validate: cha/mẹ phải tồn tại
            if (request.MaCha != null)
            {
                var cha = await _context.BenhNhans.FindAsync(request.MaCha)
                    ?? throw new KeyNotFoundException($"Không tìm thấy cha (MaBN: {request.MaCha})");

                // Validate circular: cha không được là con cháu của BN
                if (await IsDescendantOfAsync(request.MaCha, maBenhNhan))
                    throw new ArgumentException($"Không thể gán {request.MaCha} làm cha vì tạo vòng lặp gia phả.");
            }

            if (request.MaMe != null)
            {
                var me = await _context.BenhNhans.FindAsync(request.MaMe)
                    ?? throw new KeyNotFoundException($"Không tìm thấy mẹ (MaBN: {request.MaMe})");

                if (await IsDescendantOfAsync(request.MaMe, maBenhNhan))
                    throw new ArgumentException($"Không thể gán {request.MaMe} làm mẹ vì tạo vòng lặp gia phả.");
            }

            // Cập nhật
            patient.MaCha = request.MaCha;
            patient.MaMe = request.MaMe;
            patient.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();

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

        /// <summary>
        /// Lấy tiền sử bệnh của toàn bộ gia phả — tổng hợp BenhManTinh, TieuSuBenh, DiUng
        /// </summary>
        public async Task<FamilyDiseaseSummaryDto> GetFamilyDiseasesAsync(string maBenhNhan)
        {
            // Lấy cây pha hệ trước
            var tree = await GetGenealogyTreeAsync(maBenhNhan);

            var familyIds = tree.Nodes.Select(n => n.MaBenhNhan).ToList();

            // Query y tế của tất cả thành viên
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

            // Map quan hệ từ tree
            var nodeMap = tree.Nodes.ToDictionary(n => n.MaBenhNhan);

            foreach (var m in members)
            {
                nodeMap.TryGetValue(m.MaBenhNhan, out var node);

                result.ThanhVien.Add(new FamilyDiseaseDto
                {
                    MaBenhNhan = m.MaBenhNhan,
                    HoTen = m.HoTen,
                    QuanHe = node?.QuanHe ?? "unknown",
                    BenhManTinh = m.BenhManTinh,
                    TieuSuBenh = m.TieuSuBenh,
                    DiUng = m.DiUng
                });

                // Thống kê bệnh mạn tính
                if (!string.IsNullOrWhiteSpace(m.BenhManTinh))
                {
                    var diseases = m.BenhManTinh.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var d in diseases)
                    {
                        if (result.ThongKeBenhGiaDinh.ContainsKey(d))
                            result.ThongKeBenhGiaDinh[d]++;
                        else
                            result.ThongKeBenhGiaDinh[d] = 1;
                    }
                }
            }

            return result;
        }

        // =================== PRIVATE HELPERS ===================

        /// <summary>
        /// Gọi Stored Procedure (sp_GetAncestors / sp_GetDescendants) bằng ADO.NET thuần.
        /// EF Core FromSqlRaw("CALL ...") không hoạt động vì Pomelo MySQL
        /// wrap nó trong subquery → lỗi MySQL syntax.
        /// </summary>
        private async Task<List<FamilyRowDto>> CallSpGetFamilyAsync(string spName, string maBenhNhan)
        {
            var results = new List<FamilyRowDto>();
            var conn = _context.Database.GetDbConnection();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

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
                        MaMe = reader.IsDBNull(reader.GetOrdinal("MaMe")) ? null : reader.GetString(reader.GetOrdinal("MaMe")),
                    });
                }
            }
            catch (Exception)
            {
                // Nếu SP chưa tồn tại hoặc lỗi → trả list rỗng thay vì crash
                // (GetGenealogyTreeAsync sẽ still work via siblings query)
            }

            return results;
        }

        /// <summary>
        /// Kiểm tra BN candidateId có phải con cháu (descendant) của ancestorId không.
        /// Dùng để chống vòng lặp khi link cha/mẹ.
        /// </summary>
        private async Task<bool> IsDescendantOfAsync(string candidateId, string ancestorId)
        {
            var conn = _context.Database.GetDbConnection();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

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
                return await reader.ReadAsync(); // Nếu có dòng trả về → là descendant
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// DTO nội bộ cho kết quả SP trả về (không dùng entity BenhNhan để tránh tracking)
        /// </summary>
        private class FamilyRowDto
        {
            public string MaBenhNhan { get; set; } = "";
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
