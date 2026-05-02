using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using HealthCare.Realtime;
using HealthCare.Services.UserInteraction;
using HealthCare.Services.Report;
using HealthCare.Services.PatientManagement;
using HealthCare.Services.MedicationBilling;
using Microsoft.Extensions.Hosting;
using HealthCare.Infrastructure.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace HealthCare.Services.OutpatientCare
{
    /// <summary>
    /// Service quản lý phiếu CLS, chi tiết DV, kết quả và phiếu tổng hợp.
    /// </summary>
    public class ClsService : IClsService
    {
        private readonly DataContext _db;
        private readonly IRealtimeService _realtime;
        private readonly INotificationService _notifications;
        private readonly IDashboardService _dashboard;
        private readonly IPatientService _patients;
        private readonly IQueueService _queue;
        private readonly IHistoryService _history;
        private readonly IBillingService _billing;
        private readonly IMongoHistoryRepository _mongoHistory;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOverdueWorkflowCleanupService _overdueCleanup;

        public ClsService(
            DataContext db,
            IRealtimeService realtime,
            INotificationService notifications,
            IDashboardService dashboard,
            IPatientService patients,
            IQueueService queue,
            IHistoryService history,
            IBillingService billing,
            IMongoHistoryRepository mongoHistory,
            IWebHostEnvironment webHostEnvironment,
            IOverdueWorkflowCleanupService overdueCleanup)
        {
            _db = db;
            _realtime = realtime;
            _notifications = notifications;
            _dashboard = dashboard;
            _patients = patients;
            _queue = queue;
            _history = history;
            _billing = billing;
            _mongoHistory = mongoHistory;
            _webHostEnvironment = webHostEnvironment;
            _overdueCleanup = overdueCleanup;
        }
        // ================== HELPER ==================

        private sealed record StoredAttachmentPayload(
            string Id,
            string Name,
            string Url);

        private static string? BuildThongTinChiTiet(BenhNhan bn)
        {
            var parts = new List<string>();

            void Add(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add($"{label}: {value}");
            }

            Add("Dị ứng", bn.DiUng);
            Add("Chống chỉ định", bn.ChongChiDinh);
            Add("Thuốc đang dùng", bn.ThuocDangDung);
            Add("Tiền sử bệnh", bn.TieuSuBenh);
            Add("Tiền sử phẫu thuật", bn.TienSuPhauThuat);
            Add("Nhóm máu", bn.NhomMau);
            Add("Bệnh mạn tính", bn.BenhManTinh);
            Add("Sinh hiệu", bn.SinhHieu);

            return parts.Count == 0 ? null : string.Join(" | ", parts);
        }

        private sealed record StaffDutyInfo(
            string? MaNhanSu,
            string? TenNhanSu,
            string? VaiTro,
            string? ChucVu);

        private sealed record ClsExecutionInfo(
            string? MaNhanSuThucHien,
            string? TenNhanSuThucHien,
            string? VaiTro,
            string? ChucVu,
            DateTime? ThoiGianBatDau,
            DateTime? ThoiGianKetThuc);

        private static bool IsTechnicianRole(string? vaiTro, string? chucVu) =>
            string.Equals(vaiTro, "ky_thuat_vien", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(chucVu, "ky_thuat_vien", StringComparison.OrdinalIgnoreCase);

        private static string NormalizeJsonArrayOrEmpty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "[]";

            var trimmed = value.Trim();
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return doc.RootElement.ValueKind == JsonValueKind.Array
                    ? trimmed
                    : JsonSerializer.Serialize(new[] { trimmed });
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new[] { trimmed });
            }
        }

        private static string? ReadJsonStringProperty(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text.Trim();
                }
            }

            return null;
        }

        private static bool IsDataUrl(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

        private static string BuildSafeFileName(string? value)
        {
            var source = string.IsNullOrWhiteSpace(value) ? "cls-file" : value.Trim();
            var invalid = Path.GetInvalidFileNameChars();
            var chars = source
                .Select(ch => invalid.Contains(ch) ? '-' : ch)
                .ToArray();
            var cleaned = new string(chars).Trim().Trim('.');
            return string.IsNullOrWhiteSpace(cleaned) ? "cls-file" : cleaned;
        }

        private static string ResolveFileExtension(string? mimeType, string? fileName)
        {
            var existing = Path.GetExtension(fileName ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(existing))
                return existing;

            return (mimeType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                "application/pdf" => ".pdf",
                "text/plain" => ".txt",
                _ => ".bin"
            };
        }

        private async Task<string?> SaveDataUrlAttachmentAsync(string dataUrl, string? fileNameHint)
        {
            var marker = ";base64,";
            var markerIndex = dataUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex <= 5)
                return null;

            var mimeType = dataUrl.Substring(5, markerIndex - 5);
            var base64 = dataUrl[(markerIndex + marker.Length)..];
            if (string.IsNullOrWhiteSpace(base64))
                return null;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                return null;
            }

            var root = string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath)
                ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
                : _webHostEnvironment.WebRootPath;

            var folderPath = Path.Combine(root, "uploads", "cls");
            Directory.CreateDirectory(folderPath);

            var baseName = BuildSafeFileName(Path.GetFileNameWithoutExtension(fileNameHint));
            var extension = ResolveFileExtension(mimeType, fileNameHint);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"{baseName}-{suffix}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            await File.WriteAllBytesAsync(filePath, bytes);
            return $"/uploads/cls/{fileName}";
        }

        private StoredAttachmentPayload? BuildStoredAttachmentFromPlainString(string? raw, int index)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var trimmed = raw.Trim();
            var name = Path.GetFileName(trimmed);
            if (string.IsNullOrWhiteSpace(name))
                name = $"Tệp {index}";

            var url = trimmed.StartsWith("/", StringComparison.Ordinal) ||
                      trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                      trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : string.Empty;

            return new StoredAttachmentPayload($"file-{index}", name, url);
        }

        private async Task<StoredAttachmentPayload?> NormalizeAttachmentElementAsync(JsonElement element, int index)
        {
            if (element.ValueKind == JsonValueKind.String)
                return BuildStoredAttachmentFromPlainString(element.GetString(), index);

            if (element.ValueKind != JsonValueKind.Object)
                return null;

            var id = ReadJsonStringProperty(element, "id") ?? $"file-{index}";
            var name = ReadJsonStringProperty(element, "name", "fileName", "filename");
            var url = ReadJsonStringProperty(element, "url", "href", "path");

            if (IsDataUrl(url))
            {
                url = await SaveDataUrlAttachmentAsync(url!, name);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = !string.IsNullOrWhiteSpace(url)
                    ? Path.GetFileName(url)
                    : $"Tệp {index}";
            }

            return new StoredAttachmentPayload(id, name!, url ?? string.Empty);
        }

        private async Task<string> NormalizeAttachmentsForStorageAsync(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "[]";

            var trimmed = value.Trim();

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;
                var stored = new List<StoredAttachmentPayload>();

                if (root.ValueKind == JsonValueKind.Array)
                {
                    var index = 1;
                    foreach (var item in root.EnumerateArray())
                    {
                        var normalized = await NormalizeAttachmentElementAsync(item, index++);
                        if (normalized is not null)
                            stored.Add(normalized);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object || root.ValueKind == JsonValueKind.String)
                {
                    var normalized = await NormalizeAttachmentElementAsync(root, 1);
                    if (normalized is not null)
                        stored.Add(normalized);
                }

                return JsonSerializer.Serialize(stored);
            }
            catch (JsonException)
            {
                var single = BuildStoredAttachmentFromPlainString(trimmed, 1);
                return JsonSerializer.Serialize(single is null ? Array.Empty<StoredAttachmentPayload>() : new[] { single });
            }
        }

        private static string InferClsResultType(string? serviceType)
        {
            var normalized = (serviceType ?? string.Empty).Trim().ToLowerInvariant();
            return normalized.Contains("hinh_anh") ||
                   normalized.Contains("chan_doan") ||
                   normalized.Contains("x_quang") ||
                   normalized.Contains("sieu_am")
                ? "chan_doan_hinh_anh"
                : "xet_nghiem";
        }

        private static bool IsFinalClsResultStatus(string? status)
        {
            var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
            return normalized is "da_co_ket_qua" or "da_chot" or "hoan_tat";
        }

        private static BsonValue BsonOrNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? BsonNull.Value : value.Trim();

        private static BsonValue BsonDateOrNull(DateTime? value) =>
            value.HasValue ? new BsonDateTime(value.Value.ToUniversalTime()) : BsonNull.Value;

        private async Task LogMedicalEventSafeAsync(
            string? maBenhNhan,
            string eventType,
            BsonDocument payload,
            string? maNhanSu = null)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return;

            try
            {
                await _mongoHistory.LogEventAsync(maBenhNhan, eventType, payload, maNhanSu);
            }
            catch (Exception)
            {
                // MongoDB is a read/history store. Operational MySQL flow must remain successful.
            }
        }

        private static BsonDocument BuildClsItemPayload(ClsItemDto item) => new()
        {
            { "ma_chi_tiet_dv", item.MaChiTietDv },
            { "ma_phieu_kham_cls", item.MaPhieuKhamCls },
            { "ma_dich_vu", item.MaDichVu },
            { "ten_dich_vu", item.TenDichVu },
            { "loai_dich_vu", BsonOrNull(item.LoaiDichVu) },
            { "ma_phong", BsonOrNull(item.MaPhong) },
            { "ten_phong", BsonOrNull(item.TenPhong) },
            { "ma_ky_thuat_vien", BsonOrNull(item.MaKyThuatVienThucHien ?? item.MaNhanSuThucHien) },
            { "ten_ky_thuat_vien", BsonOrNull(item.TenKyThuatVienThucHien ?? item.TenNhanSuThucHien) },
            { "trang_thai", BsonOrNull(item.TrangThai) },
            { "ghi_chu", BsonOrNull(item.GhiChu) }
        };

        private static JsonElement BsonDocumentToJsonElement(BsonDocument document)
        {
            var json = document.ToJson(new JsonWriterSettings
            {
                OutputMode = JsonOutputMode.RelaxedExtendedJson
            });

            using var parsed = JsonDocument.Parse(json);
            return parsed.RootElement.Clone();
        }

        private static bool IsMongoEventForCls(BsonDocument document, string maPhieuKhamCls)
        {
            if (!document.TryGetValue("data", out var dataValue) || !dataValue.IsBsonDocument)
                return false;

            var data = dataValue.AsBsonDocument;
            return data.TryGetValue("ma_phieu_kham_cls", out var clsValue) &&
                   clsValue.IsString &&
                   string.Equals(clsValue.AsString, maPhieuKhamCls, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<IReadOnlyList<JsonElement>> GetMongoClsEventsForSnapshotAsync(
            string maBenhNhan,
            string maPhieuKhamCls)
        {
            try
            {
                var events = await _mongoHistory.GetPatientHistoryAsync(
                    maBenhNhan,
                    eventType: null,
                    fromDate: null,
                    toDate: null,
                    limit: 500);

                return events
                    .Where(e => IsMongoEventForCls(e, maPhieuKhamCls))
                    .OrderBy(e => e.TryGetValue("event_date", out var date) && date.IsBsonDateTime
                        ? date.ToUniversalTime()
                        : DateTime.MinValue)
                    .Select(BsonDocumentToJsonElement)
                    .ToList();
            }
            catch (Exception)
            {
                return Array.Empty<JsonElement>();
            }
        }

        private static LichTruc? SelectBestDuty(IEnumerable<LichTruc> schedules, DateTime at)
        {
            var list = schedules
                .Where(l => !l.NghiTruc)
                .ToList();

            if (list.Count == 0)
                return null;

            var ngay = at.Date;
            var gio = at.TimeOfDay;

            var sameDay = list
                .Where(l => l.Ngay.Date == ngay)
                .ToList();

            var active = sameDay
                .Where(l => l.GioBatDau <= gio && l.GioKetThuc >= gio)
                .OrderBy(l => l.GioBatDau)
                .FirstOrDefault();

            if (active is not null)
                return active;

            var nearestSameDay = sameDay
                .OrderBy(l => Math.Abs((l.GioBatDau - gio).TotalMinutes))
                .ThenBy(l => l.GioBatDau)
                .FirstOrDefault();

            if (nearestSameDay is not null)
                return nearestSameDay;

            return list
                .OrderBy(l => Math.Abs((l.Ngay.Date.Add(l.GioBatDau) - at).TotalMinutes))
                .ThenByDescending(l => l.Ngay)
                .ThenBy(l => l.GioBatDau)
                .FirstOrDefault();
        }

        private async Task<IReadOnlyList<ClsItemDto>> MapClsItemsAsync(
            IEnumerable<ChiTietDichVu> chiTietList,
            DateTime? thoiDiem = null)
        {
            var list = chiTietList.ToList();
            if (list.Count == 0)
                return Array.Empty<ClsItemDto>();

            var now = thoiDiem ?? DateTime.Now;
            var ngay = now.Date;
            var gio = now.TimeOfDay;

            var maPhongs = list
                .Select(ct => ct.DichVuYTe?.MaPhongThucHien)
                .Where(mp => !string.IsNullOrWhiteSpace(mp))
                .Select(mp => mp!)
                .Distinct()
                .ToList();
            var fixedStaffByPhong = maPhongs.Count == 0
                ? new Dictionary<string, StaffDutyInfo>()
                : await _db.Phongs
                    .AsNoTracking()
                    .Include(p => p.KTVPhuTrach)
                    .Where(p => maPhongs.Contains(p.MaPhong) && p.MaKTVPhuTrach != null)
                    .ToDictionaryAsync(
                        p => p.MaPhong,
                        p => new StaffDutyInfo(
                            p.MaKTVPhuTrach,
                            p.KTVPhuTrach != null ? p.KTVPhuTrach.HoTen : null,
                            p.KTVPhuTrach != null ? p.KTVPhuTrach.VaiTro : null,
                            p.KTVPhuTrach != null ? p.KTVPhuTrach.ChucVu : null));

            List<LichTruc> lichTrucs = new();
            if (maPhongs.Count > 0)
            {
                var sameDayLichTrucs = await _db.LichTrucs
                    .AsNoTracking()
                    .Include(l => l.YTaTruc)
                    .Where(l =>
                        maPhongs.Contains(l.MaPhong) &&
                        !l.NghiTruc &&
                        l.Ngay == ngay)
                    .ToListAsync();

                var missingPhongIds = maPhongs
                    .Except(sameDayLichTrucs.Select(l => l.MaPhong))
                    .ToList();

                var fallbackLichTrucs = missingPhongIds.Count == 0
                    ? new List<LichTruc>()
                    : await _db.LichTrucs
                        .AsNoTracking()
                        .Include(l => l.YTaTruc)
                        .Where(l =>
                            missingPhongIds.Contains(l.MaPhong) &&
                            !l.NghiTruc)
                        .ToListAsync();

                lichTrucs = sameDayLichTrucs
                    .Concat(fallbackLichTrucs)
                    .ToList();
            }

            var dutyByPhong = lichTrucs
                .GroupBy(l => l.MaPhong)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var lich = SelectBestDuty(g, now)!;
                        return new StaffDutyInfo(
                            lich.MaYTaTruc,
                            lich.YTaTruc?.HoTen,
                            lich.YTaTruc?.VaiTro,
                            lich.YTaTruc?.ChucVu);
                    });

            var maChiTietDvs = list
                .Select(ct => ct.MaChiTietDv)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var queueRefs = maChiTietDvs.Count == 0
                ? new List<(string MaChiTietDv, string MaHangDoi)>()
                : (await _db.HangDois
                    .AsNoTracking()
                    .Where(h => h.MaChiTietDv != null && maChiTietDvs.Contains(h.MaChiTietDv))
                    .Select(h => new
                    {
                        MaChiTietDv = h.MaChiTietDv!,
                        h.MaHangDoi
                    })
                    .ToListAsync())
                    .Select(x => (x.MaChiTietDv, x.MaHangDoi))
                    .ToList();

            var queueByChiTietDv = queueRefs
                .GroupBy(x => x.MaChiTietDv)
                .ToDictionary(g => g.Key, g => g.First().MaHangDoi);

            var maHangDois = queueByChiTietDv.Values
                .Distinct()
                .ToList();

            List<(string MaHangDoi, string? MaNhanSu, string? TenNhanSu, string? VaiTro, string? ChucVu, DateTime? BatDau, DateTime? KetThuc)> clsVisits;
            if (maHangDois.Count == 0)
            {
                clsVisits = new();
            }
            else
            {
                clsVisits = (await _db.LuotKhamBenhs
                    .AsNoTracking()
                    .Where(l => maHangDois.Contains(l.MaHangDoi))
                    .OrderByDescending(l => l.NgayCapNhat)
                    .ThenByDescending(l => l.NgayTao)
                    .Select(l => new
                    {
                        l.MaHangDoi,
                        MaNhanSu = l.MaNhanSuThucHien,
                        TenNhanSu = l.NhanSuThucHien != null ? l.NhanSuThucHien.HoTen : null,
                        VaiTro = l.NhanSuThucHien != null ? l.NhanSuThucHien.VaiTro : null,
                        ChucVu = l.NhanSuThucHien != null ? l.NhanSuThucHien.ChucVu : null,
                        BatDau = l.ThoiGianBatDau,
                        KetThuc = l.ThoiGianKetThuc
                    })
                    .ToListAsync())
                    .Select(x => (
                        MaHangDoi: x.MaHangDoi,
                        MaNhanSu: (string?)x.MaNhanSu,
                        TenNhanSu: (string?)x.TenNhanSu,
                        VaiTro: (string?)x.VaiTro,
                        ChucVu: (string?)x.ChucVu,
                        BatDau: (DateTime?)x.BatDau,
                        KetThuc: x.KetThuc))
                    .ToList();
            }

            var visitByQueue = clsVisits
                .GroupBy(x => x.MaHangDoi)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var luot = g.First();
                        return new ClsExecutionInfo(
                            luot.MaNhanSu,
                            luot.TenNhanSu,
                            luot.VaiTro,
                            luot.ChucVu,
                            luot.BatDau,
                            luot.KetThuc);
                    });

            var executionByChiTietDv = queueByChiTietDv
                .Where(kv => visitByQueue.ContainsKey(kv.Value))
                .ToDictionary(kv => kv.Key, kv => visitByQueue[kv.Value]);

            return list.Select(ct => MapClsItem(ct, fixedStaffByPhong, dutyByPhong, executionByChiTietDv)).ToList();
        }

        private async Task<ClsItemDto> MapClsItemAsync(ChiTietDichVu c, DateTime? thoiDiem = null)
        {
            var mapped = await MapClsItemsAsync(new[] { c }, thoiDiem);
            return mapped.First();
        }

        private static ClsItemDto MapClsItem(
            ChiTietDichVu c,
            IDictionary<string, StaffDutyInfo> fixedStaffByPhong,
            IDictionary<string, StaffDutyInfo> dutyByPhong,
            IDictionary<string, ClsExecutionInfo> executionByChiTietDv)
        {
            var dv = c.DichVuYTe;
            var trangThai = c.TrangThai?.ToLowerInvariant();
            var statusNormalized = trangThai switch
            {
                "da_co_ket_qua" => "da_co_ket_qua",
                "dang_thuc_hien" => "dang_thuc_hien",
                _ => "chua_co_ket_qua"
            };

            var maPhong = dv?.MaPhongThucHien ?? "";
            fixedStaffByPhong.TryGetValue(maPhong, out var fixedStaff);
            dutyByPhong.TryGetValue(maPhong, out var duty);
            executionByChiTietDv.TryGetValue(c.MaChiTietDv, out var execution);

            var maNhanSu = execution?.MaNhanSuThucHien ?? fixedStaff?.MaNhanSu ?? duty?.MaNhanSu;
            var tenNhanSu = execution?.TenNhanSuThucHien ?? fixedStaff?.TenNhanSu ?? duty?.TenNhanSu;
            var vaiTro = execution?.VaiTro ?? fixedStaff?.VaiTro ?? duty?.VaiTro;
            var chucVu = execution?.ChucVu ?? fixedStaff?.ChucVu ?? duty?.ChucVu;
            var maKyThuatVien = IsTechnicianRole(vaiTro, chucVu) ? maNhanSu : null;
            var tenKyThuatVien = IsTechnicianRole(vaiTro, chucVu) ? tenNhanSu : null;

            return new ClsItemDto
            {
                MaChiTietDv = c.MaChiTietDv,
                MaPhieuKhamCls = c.MaPhieuKhamCls,
                MaDichVu = c.MaDichVu,
                TenDichVu = dv?.TenDichVu ?? "",
                LoaiDichVu = dv?.LoaiDichVu ?? "",
                PhiDV = dv?.DonGia.ToString("0") ?? "0",
                MaPhong = maPhong,
                TenPhong = dv?.PhongThucHien?.TenPhong ?? "",
                MaNhanSuThucHien = maNhanSu,
                TenNhanSuThucHien = tenNhanSu,
                MaKyThuatVienThucHien = maKyThuatVien,
                TenKyThuatVienThucHien = tenKyThuatVien,
                MaYTaThucHien = maNhanSu,
                TenYTaThucHien = tenNhanSu,
                ThoiGianBatDau = execution?.ThoiGianBatDau,
                ThoiGianKetThuc = execution?.ThoiGianKetThuc,
                GhiChu = c.GhiChu,
                TrangThai = statusNormalized
            };
        }

        private async Task<ClsOrderDto?> BuildClsOrderDtoAsync(string maPhieuKhamCls)
        {
            var row =
                await (from cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                       join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                           on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                       join bn in _db.BenhNhans.AsNoTracking()
                           on ls.MaBenhNhan equals bn.MaBenhNhan
                       join nguoiLap in _db.NhanVienYTes.AsNoTracking()
                           on ls.MaNguoiLap equals nguoiLap.MaNhanVien into nguoiLapGroup
                       from nguoiLap in nguoiLapGroup.DefaultIfEmpty()
                       where cls.MaPhieuKhamCls == maPhieuKhamCls
                       select new { cls, ls, bn, nguoiLap })
                .FirstOrDefaultAsync();

            if (row is null) return null;

            var chiTietList = await _db.ChiTietDichVus
                .AsNoTracking()
                .Where(c => c.MaPhieuKhamCls == maPhieuKhamCls)
                .Include(c => c.DichVuYTe)
                    .ThenInclude(dv => dv.PhongThucHien)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .ToListAsync();

            var itemDtos = await MapClsItemsAsync(chiTietList, row.cls.NgayGioLap);

            var firstDv = chiTietList.FirstOrDefault()?.DichVuYTe;

            return new ClsOrderDto
            {
                MaPhieuKhamCls = row.cls.MaPhieuKhamCls,
                MaPhieuKhamLs = row.cls.MaPhieuKhamLs,
                MaBenhNhan = row.bn.MaBenhNhan,

                HoTen = row.bn.HoTen,
                TenBenhNhan = row.bn.HoTen,
                NgaySinh = row.bn.NgaySinh,
                GioiTinh = row.bn.GioiTinh,
                DienThoai = row.bn.DienThoai,
                Email = row.bn.Email,
                DiaChi = row.bn.DiaChi,

                TrangThai = row.cls.TrangThai,
                AutoPublishEnabled = row.cls.AutoPublishEnabled,
                GhiChu = row.cls.GhiChu,
                NgayLap = row.cls.NgayGioLap,
                GioLap = row.cls.NgayGioLap.TimeOfDay,

                MaKhoa = firstDv?.PhongThucHien?.MaKhoa ?? "",
                TenKhoa = firstDv?.PhongThucHien?.KhoaChuyenMon?.TenKhoa,

                MaNguoiLap = row.ls.MaNguoiLap,
                TenNguoiLap = row.nguoiLap?.HoTen,

                ThongTinChiTiet = BuildThongTinChiTiet(row.bn),
                ListItemDV = itemDtos
            };
        }

        // ================== 1. PHIẾU CLS ==================

        public async Task<ClsOrderDto> TaoPhieuClsAsync(ClsOrderCreateRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(request.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaPhieuKhamLs))
                throw new ArgumentException("MaPhieuKhamLs là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaNguoiLap))
                throw new ArgumentException("MaNguoiLap là bắt buộc");

            // Đảm bảo phiếu CLS luôn gắn với đúng phiếu LS + BN
            var phieuLs = await _db.PhieuKhamLamSangs
                            .Include(p => p.BenhNhan)
                            .Include(p => p.DichVuKham)
                            .FirstOrDefaultAsync(p =>
            p.MaPhieuKham == request.MaPhieuKhamLs &&
            p.MaBenhNhan == request.MaBenhNhan)
                                ?? throw new InvalidOperationException("Không tìm thấy phiếu khám LS tương ứng");

            // Nếu đã có phiếu CLS gắn với phiếu LS này thì trả về, không tạo mới (tránh duplicate)
            var existedCls = await _db.PhieuKhamCanLamSangs
                .Include(c => c.ChiTietDichVus)
                .FirstOrDefaultAsync(c => c.MaPhieuKhamLs == request.MaPhieuKhamLs);
            if (existedCls is not null)
            {
                if (!string.Equals(existedCls.TrangThai, "da_hoan_tat", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Phiếu CLS cho phiếu khám này đang chưa hoàn tất, không thể tạo thêm.");

                var dtoDup = await BuildClsOrderDtoAsync(existedCls.MaPhieuKhamCls);
                if (dtoDup is not null)
                    return dtoDup;

                throw new InvalidOperationException("Phiếu CLS cho phiếu khám này đã tồn tại.");
            }

            var now = DateTime.Now;
            PhieuKhamCanLamSang phieuCls;

            // Transaction: tạo phiếu CLS + chi tiết DV một lượt
            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                phieuCls = new PhieuKhamCanLamSang
                {
                    MaPhieuKhamCls = $"CLS-{Guid.NewGuid():N}",
                    MaPhieuKhamLs = phieuLs.MaPhieuKham,
                    NgayGioLap = now,
                    AutoPublishEnabled = request.AutoPublishEnabled,
                    TrangThai = request.TrangThai ?? "da_lap",
                    GhiChu = request.GhiChu
                };

                _db.PhieuKhamCanLamSangs.Add(phieuCls);
                await _db.SaveChangesAsync();

                // Tạo danh sách chi tiết DV nếu có
                if (request.ListItemDV is not null && request.ListItemDV.Count > 0)
                {
                    foreach (var item in request.ListItemDV)
                    {
                        // Mỗi dòng BS nhập trên UI -> 1 ChiTietDichVu
                        var createReq = new ClsItemCreateRequest
                        {
                            MaPhieuKhamCls = phieuCls.MaPhieuKhamCls,
                            MaDichVu = item.MaDichVu,
                            GhiChu = item.GhiChu,
                            TrangThai = string.IsNullOrWhiteSpace(item.TrangThai)
                                ? "chua_co_ket_qua"
                                : item.TrangThai
                        };

                        // Tận dụng hàm có sẵn (tự map + load DichVuYTe)
                        await TaoChiTietDichVuAsync(createReq);
                    }
                }

                await tx.CommitAsync();
            }

            // Sau khi chỉ định CLS, cập nhật trạng thái trong ngày của BN
            await _patients.CapNhatTrangThaiBenhNhanAsync(
                phieuLs.MaBenhNhan,
                new PatientStatusUpdateRequest
                {
                    TrangThaiHomNay = "cho_tiep_nhan_dv"
                });

            var dto = await BuildClsOrderDtoAsync(phieuCls.MaPhieuKhamCls)
                      ?? throw new InvalidOperationException("Không build được DTO CLS");

            // Realtime: phiếu CLS mới
            await LogMedicalEventSafeAsync(
                phieuLs.MaBenhNhan,
                "cls_order_created",
                new BsonDocument
                {
                    { "ma_phieu_kham_cls", phieuCls.MaPhieuKhamCls },
                    { "ma_phieu_kham_ls", phieuCls.MaPhieuKhamLs },
                    { "ma_benh_nhan", phieuLs.MaBenhNhan },
                    { "ma_nguoi_lap", request.MaNguoiLap },
                    { "ngay_gio_lap", BsonDateOrNull(phieuCls.NgayGioLap) },
                    { "trang_thai", phieuCls.TrangThai },
                    { "auto_publish_enabled", phieuCls.AutoPublishEnabled },
                    { "ghi_chu", BsonOrNull(phieuCls.GhiChu) },
                    { "items", new BsonArray(dto.ListItemDV.Select(BuildClsItemPayload)) }
                },
                request.MaNguoiLap);

            await _realtime.BroadcastClsOrderCreatedAsync(dto);

            // Thông báo: chỉ định CLS mới
            await TaoThongBaoChiDinhClsMoiAsync(dto);

            // Cập nhật Dashboard: lượt khám + hoạt động gần đây
            var dashboard = await _dashboard.LayDashboardHomNayAsync();

            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastTodayExamOverviewAsync(dashboard.LuotKhamHomNay);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);

            return dto;
        }

        public async Task<ClsOrderDto?> LayPhieuClsAsync(string maPhieuKhamCls)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                return null;

            return await BuildClsOrderDtoAsync(maPhieuKhamCls);
        }

        public async Task<ClsOrderDto?> CapNhatTrangThaiPhieuClsAsync(string maPhieuKhamCls, string trangThai)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                return null;
            if (string.IsNullOrWhiteSpace(trangThai))
                throw new ArgumentException("TrangThai là bắt buộc");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _db.PhieuKhamCanLamSangs
                    .Include(p => p.PhieuKhamLamSang)
                        .ThenInclude(ls => ls.BenhNhan)
                    .Include(p => p.ChiTietDichVus)
                        .ThenInclude(ct => ct.DichVuYTe)
                    .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls);

                if (phieu is null) return null;

                // Auto-billing: tạo hóa đơn khi chuyển sang "dang_thuc_hien"
                if (string.Equals(trangThai, "dang_thuc_hien", StringComparison.OrdinalIgnoreCase))
                {
                    var daCoHoaDon = await _db.HoaDonThanhToans
                        .AsNoTracking()
                        .AnyAsync(hd => hd.MaPhieuKhamCls == phieu.MaPhieuKhamCls);

                    if (!daCoHoaDon)
                    {
                        var maBenhNhan = phieu.PhieuKhamLamSang?.MaBenhNhan;
                        if (!string.IsNullOrWhiteSpace(maBenhNhan))
                        {
                            // Tính tổng tiền từ tất cả dịch vụ
                            var tongTien = phieu.ChiTietDichVus.Sum(ct => ct.DichVuYTe?.DonGia ?? 0m);
                            
                            // Lấy nhân sự thu (có thể là người lập phiếu hoặc nhân sự mặc định)
                            var thuNgan = await _db.NhanVienYTes
                                .AsNoTracking()
                                .FirstOrDefaultAsync(nv => nv.VaiTro == "y_ta" && nv.LoaiYTa == "hanhchinh")
                                ?? await _db.NhanVienYTes.AsNoTracking().FirstOrDefaultAsync()
                                ?? throw new InvalidOperationException("Không tìm thấy nhân sự thu");

                            var invoiceReq = new InvoiceCreateRequest
                            {
                                MaBenhNhan = maBenhNhan,
                                MaNhanSuThu = thuNgan.MaNhanVien,
                                LoaiDotThu = "can_lam_sang",
                                SoTien = tongTien,
                                MaPhieuKhamCls = phieu.MaPhieuKhamCls,
                                PhuongThucThanhToan = "tien_mat",
                                NoiDung = $"Thu phí cận lâm sàng - Phiếu {phieu.MaPhieuKhamCls}"
                            };
                            await _billing.TaoHoaDonAsync(invoiceReq);
                        }
                    }
                }

                phieu.TrangThai = trangThai;
                await _db.SaveChangesAsync();

                var maBenhNhan2 = phieu.PhieuKhamLamSang?.MaBenhNhan;
                var firstCt = phieu.ChiTietDichVus.FirstOrDefault();

                var benhNhan = phieu.PhieuKhamLamSang?.BenhNhan;
                if (benhNhan is not null)
                {
                    benhNhan.TrangThaiHomNay = "cho_kham_dv";
                    benhNhan.NgayTrangThai = DateTime.Today;
                    await _db.SaveChangesAsync();
                }

                if (firstCt is not null)
                {
                    await CapNhatTrangThaiChiTietDVAsync(firstCt.MaChiTietDv, trangThai);
                }

                // Queue management: tạo hàng đợi cho dịch vụ đầu tiên
                if (firstCt is not null && !string.IsNullOrWhiteSpace(maBenhNhan2))
                {
                    var daCoHangDoi = await _db.HangDois.AnyAsync(h => h.MaChiTietDv == firstCt.MaChiTietDv);
                    if (!daCoHangDoi)
                    {
                        var phongDv = firstCt.DichVuYTe?.MaPhongThucHien ?? "CLS_XN_01";
                        await _queue.ThemVaoHangDoiAsync(new QueueEnqueueRequest
                        {
                            MaBenhNhan = maBenhNhan2!,
                            MaPhong = phongDv,
                            LoaiHangDoi = "can_lam_sang",
                            MaChiTietDv = firstCt.MaChiTietDv,
                            MaPhieuKham = null,
                            Nguon = null,
                            Nhan = null,
                            ThoiGianLichHen = null
                        });
                    }
                }

                await transaction.CommitAsync();

                // Broadcast after successful transaction
                var dto = await BuildClsOrderDtoAsync(maPhieuKhamCls);

                if (dto is not null)
                {
                    var benhNhanForBroadcast = phieu.PhieuKhamLamSang?.BenhNhan;
                    if (benhNhanForBroadcast is not null)
                    {
                        await _realtime.BroadcastPatientStatusUpdatedAsync(new PatientDto
                        {
                            MaBenhNhan = benhNhanForBroadcast.MaBenhNhan,
                            HoTen = benhNhanForBroadcast.HoTen,
                            NgaySinh = benhNhanForBroadcast.NgaySinh,
                            GioiTinh = benhNhanForBroadcast.GioiTinh,
                            DienThoai = benhNhanForBroadcast.DienThoai,
                            Email = benhNhanForBroadcast.Email,
                            DiaChi = benhNhanForBroadcast.DiaChi,
                            TrangThaiTaiKhoan = benhNhanForBroadcast.TrangThaiTaiKhoan,
                            TrangThaiHomNay = benhNhanForBroadcast.TrangThaiHomNay,
                            NgayTrangThai = benhNhanForBroadcast.NgayTrangThai
                        });
                    }

                    await LogMedicalEventSafeAsync(
                        dto.MaBenhNhan,
                        "cls_order_status_changed",
                        new BsonDocument
                        {
                            { "ma_phieu_kham_cls", dto.MaPhieuKhamCls },
                            { "ma_phieu_kham_ls", dto.MaPhieuKhamLs },
                            { "ma_benh_nhan", dto.MaBenhNhan },
                            { "trang_thai", dto.TrangThai },
                            { "ma_nguoi_lap", BsonOrNull(dto.MaNguoiLap) },
                            { "items", new BsonArray(dto.ListItemDV.Select(BuildClsItemPayload)) }
                        },
                        dto.MaNguoiLap);

                    await _realtime.BroadcastClsOrderStatusUpdatedAsync(dto);
                    var dashboard = await _dashboard.LayDashboardHomNayAsync();
                    await _realtime.BroadcastDashboardTodayAsync(dashboard);
                }

                return dto;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ClsItemDto?> CapNhatTrangThaiChiTietDVAsync(string maChiTietDv, string trangThai)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maChiTietDv))
                return null;
            if (string.IsNullOrWhiteSpace(trangThai))
                throw new ArgumentException("TrangThai là bắt buộc");

            var ct = await _db.ChiTietDichVus
                .Include(c => c.DichVuYTe)
                .FirstOrDefaultAsync(c => c.MaChiTietDv == maChiTietDv);

            if (ct is null) return null;

            ct.TrangThai = trangThai;
            await _db.SaveChangesAsync();

            var dto = await MapClsItemAsync(ct);

            // Broadcast cập nhật chi tiết DV để FE/queue nắm trạng thái
            await _realtime.BroadcastClsItemUpdatedAsync(dto);

            return dto;
        }

// ================== 2. SEARCH + PAGING PHIẾU CLS ==================

        public async Task<PagedResult<ClsOrderDto>> TimKiemPhieuClsAsync(
            string? maBenhNhan,
            string? maBacSi,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize,
            string? originMaKhoaScope = null,
            string? serviceMaKhoaScope = null,
            string? serviceMaPhongScope = null)
        {
            await _overdueCleanup.CleanupAsync();

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize; // ✅ Chuẩn hóa: 50 items mặc định

            var query =
                from cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                    on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                join bn in _db.BenhNhans.AsNoTracking()
                    on ls.MaBenhNhan equals bn.MaBenhNhan
                join nguoiLap in _db.NhanVienYTes.AsNoTracking()
                    on ls.MaNguoiLap equals nguoiLap.MaNhanVien into nguoiLapGroup
                from nguoiLap in nguoiLapGroup.DefaultIfEmpty()
                select new { cls, ls, bn, nguoiLap };

            if (!string.IsNullOrWhiteSpace(maBenhNhan))
                query = query.Where(x => x.bn.MaBenhNhan == maBenhNhan);

            if (!string.IsNullOrWhiteSpace(maBacSi))
                query = query.Where(x => x.ls.MaBacSiKham == maBacSi);

            if (!string.IsNullOrWhiteSpace(originMaKhoaScope))
            {
                query = query.Where(x =>
                    x.ls.BacSiKham.MaKhoa == originMaKhoaScope ||
                    x.ls.NguoiLap.MaKhoa == originMaKhoaScope);
            }

            if (!string.IsNullOrWhiteSpace(serviceMaKhoaScope))
            {
                query = query.Where(x =>
                    _db.ChiTietDichVus.Any(ct =>
                        ct.MaPhieuKhamCls == x.cls.MaPhieuKhamCls &&
                        ct.DichVuYTe.PhongThucHien.MaKhoa == serviceMaKhoaScope));
            }

            if (!string.IsNullOrWhiteSpace(serviceMaPhongScope))
            {
                query = query.Where(x =>
                    _db.ChiTietDichVus.Any(ct =>
                        ct.MaPhieuKhamCls == x.cls.MaPhieuKhamCls &&
                        ct.DichVuYTe.MaPhongThucHien == serviceMaPhongScope));
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.cls.NgayGioLap >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.cls.NgayGioLap < to);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.cls.TrangThai == trangThai);

            query = query.OrderByDescending(x => x.cls.NgayGioLap);

            var totalItems = await query.CountAsync();
            var pageData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var maClsList = pageData.Select(x => x.cls.MaPhieuKhamCls).ToList();

            var itemsByHeader = await _db.ChiTietDichVus
                .AsNoTracking()
                .Where(c => maClsList.Contains(c.MaPhieuKhamCls))
                .Include(c => c.DichVuYTe)
                    .ThenInclude(dv => dv.PhongThucHien)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .GroupBy(c => c.MaPhieuKhamCls)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var dtos = new List<ClsOrderDto>();

            foreach (var row in pageData)
            {
                itemsByHeader.TryGetValue(row.cls.MaPhieuKhamCls, out var list);
                var itemDtos = await MapClsItemsAsync(list ?? new List<ChiTietDichVu>(), row.cls.NgayGioLap);

                var firstDv = (list ?? new List<ChiTietDichVu>())
                    .FirstOrDefault()?.DichVuYTe;

                dtos.Add(new ClsOrderDto
                {
                    MaPhieuKhamCls = row.cls.MaPhieuKhamCls,
                    MaPhieuKhamLs = row.cls.MaPhieuKhamLs,
                    MaBenhNhan = row.bn.MaBenhNhan,

                    HoTen = row.bn.HoTen,
                    TenBenhNhan = row.bn.HoTen,
                    NgaySinh = row.bn.NgaySinh,
                    GioiTinh = row.bn.GioiTinh,
                    DienThoai = row.bn.DienThoai,
                    Email = row.bn.Email,
                    DiaChi = row.bn.DiaChi,

                    TrangThai = row.cls.TrangThai,
                    AutoPublishEnabled = row.cls.AutoPublishEnabled,
                    GhiChu = row.cls.GhiChu,
                    NgayLap = row.cls.NgayGioLap,
                    GioLap = row.cls.NgayGioLap.TimeOfDay,

                    MaKhoa = firstDv?.PhongThucHien?.MaKhoa ?? "",
                    TenKhoa = firstDv?.PhongThucHien?.KhoaChuyenMon?.TenKhoa,

                    MaNguoiLap = row.ls.MaNguoiLap,
                    TenNguoiLap = row.nguoiLap?.HoTen,

                    ThongTinChiTiet = BuildThongTinChiTiet(row.bn),
                    ListItemDV = itemDtos
                });
            }

            return new PagedResult<ClsOrderDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ================== 3. CHI TIẾT DỊCH VỤ ==================

        public async Task<ClsItemDto> TaoChiTietDichVuAsync(ClsItemCreateRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(request.MaPhieuKhamCls))
                throw new ArgumentException("MaPhieuKhamCls là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaDichVu))
                throw new ArgumentException("MaDichVu là bắt buộc");

            var headerExists = await _db.PhieuKhamCanLamSangs
                .AnyAsync(p => p.MaPhieuKhamCls == request.MaPhieuKhamCls);

            if (!headerExists)
                throw new InvalidOperationException("Không tìm thấy phiếu CLS");

            var dichVu = await _db.DichVuYTes
                .FirstOrDefaultAsync(d => d.MaDichVu == request.MaDichVu)
                    ?? throw new InvalidOperationException("Không tìm thấy dịch vụ CLS");

            var chiTiet = new ChiTietDichVu
            {
                MaChiTietDv = $"DV-{Guid.NewGuid():N}",
                MaPhieuKhamCls = request.MaPhieuKhamCls,
                MaDichVu = request.MaDichVu,
                TrangThai = "chua_co_ket_qua",
                GhiChu = request.GhiChu
            };

            _db.ChiTietDichVus.Add(chiTiet);
            await _db.SaveChangesAsync();

            var loaded = await _db.ChiTietDichVus
                .AsNoTracking()
                .Include(c => c.DichVuYTe)
                .FirstAsync(c => c.MaChiTietDv == chiTiet.MaChiTietDv);

            return await MapClsItemAsync(loaded);
        }

        public async Task<IReadOnlyList<ClsItemDto>> LayDanhSachDichVuClsAsync(string maPhieuKhamCls)
        {
            await _overdueCleanup.CleanupAsync();

            var list = await _db.ChiTietDichVus
                .AsNoTracking()
                .Where(c => c.MaPhieuKhamCls == maPhieuKhamCls)
                .Include(c => c.DichVuYTe)
                .ToListAsync();

            return await MapClsItemsAsync(list);
        }

        /// <summary>
        /// Lấy chi tiết của một dịch vụ CLS (ChiTietDichVu) dưới dạng DTO `ClsItemDto`.
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        public async Task<ClsItemDto?> LayChiTietDichVuAsync(string maChiTietDv)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maChiTietDv))
                return null;

            var ct = await _db.ChiTietDichVus
                .AsNoTracking()
                .Include(c => c.DichVuYTe)
                .FirstOrDefaultAsync(c => c.MaChiTietDv == maChiTietDv);

            if (ct is null) return null;

            return await MapClsItemAsync(ct);
        }

        // ================== 4. KẾT QUẢ CLS ==================

        public async Task<ClsResultDto> TaoKetQuaClsAsync(ClsResultCreateRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(request.MaChiTietDv))
                throw new ArgumentException("MaChiTietDv là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.TrangThaiChot))
                throw new ArgumentException("TrangThaiChot là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaNhanSuThucHien))
                throw new ArgumentException("MaNhanSuThucHien là bắt buộc");

            var chiTiet = await _db.ChiTietDichVus
                .Include(c => c.DichVuYTe)
                .FirstOrDefaultAsync(c => c.MaChiTietDv == request.MaChiTietDv)
                    ?? throw new InvalidOperationException("Không tìm thấy chi tiết dịch vụ");

            var phieuCls = await _db.PhieuKhamCanLamSangs
                .Include(p => p.PhieuKhamLamSang)
                    .ThenInclude(ls => ls.BenhNhan)
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == chiTiet.MaPhieuKhamCls);

            var ketQua = await _db.KetQuaDichVus
                .FirstOrDefaultAsync(k => k.MaChiTietDv == request.MaChiTietDv);

            var now = DateTime.Now;
            var tepDinhKemJson = await NormalizeAttachmentsForStorageAsync(request.TepDinhKem);
            var ketLuanChuyen = string.IsNullOrWhiteSpace(request.KetLuanChuyen)
                ? request.NoiDungKetQua
                : request.KetLuanChuyen;
            var thoiGianChot = IsFinalClsResultStatus(request.TrangThaiChot) ? now : (DateTime?)null;
            var loaiKetQua = InferClsResultType(chiTiet.DichVuYTe?.LoaiDichVu ?? chiTiet.DichVuYTe?.TenDichVu);

            if (ketQua is null)
            {
                ketQua = new KetQuaDichVu
                {
                    MaKetQua = $"KQ-{Guid.NewGuid():N}",
                    MaChiTietDv = chiTiet.MaChiTietDv,
                    LoaiKetQua = loaiKetQua,
                    KetLuanChuyen = ketLuanChuyen,
                    GhiChu = request.GhiChu,
                    TrangThaiChot = request.TrangThaiChot,
                    MaNguoiTao = request.MaNhanSuThucHien,
                    ThoiGianTao = now,
                    ThoiGianChot = thoiGianChot,
                    TepDinhKem = tepDinhKemJson
                };
                _db.KetQuaDichVus.Add(ketQua);
            }
            else
            {
                ketQua.LoaiKetQua = loaiKetQua;
                ketQua.TrangThaiChot = request.TrangThaiChot;
                ketQua.KetLuanChuyen = ketLuanChuyen;
                ketQua.GhiChu = request.GhiChu;
                ketQua.MaNguoiTao = request.MaNhanSuThucHien;
                ketQua.ThoiGianChot = thoiGianChot ?? ketQua.ThoiGianChot;
                ketQua.TepDinhKem = tepDinhKemJson;
            }

            chiTiet.TrangThai = "da_co_ket_qua";

            await _db.SaveChangesAsync();

            // ===== LOG TO MONGODB: CLS Result Event =====
            if (phieuCls?.PhieuKhamLamSang?.MaBenhNhan is not null)
            {
                var loaiDichVu = chiTiet.DichVuYTe?.LoaiDichVu ?? "xet_nghiem";
                var eventType = loaiDichVu.Contains("hinh_anh") ? "chan_doan_hinh_anh" : "xet_nghiem";

                var clsPayload = new BsonDocument
                {
                    { "ma_ket_qua", ketQua.MaKetQua },
                    { "ma_chi_tiet_dv", ketQua.MaChiTietDv },
                    { "ma_phieu_kham_cls", chiTiet.MaPhieuKhamCls ?? (BsonValue)BsonNull.Value },
                    { "ma_dich_vu", chiTiet.MaDichVu ?? (BsonValue)BsonNull.Value },
                    { "ten_dich_vu", chiTiet.DichVuYTe?.TenDichVu ?? (BsonValue)BsonNull.Value },
                    { "ma_ky_thuat_vien", ketQua.MaNguoiTao },
                    { "ket_luan", ketQua.KetLuanChuyen ?? (BsonValue)BsonNull.Value },
                    { "ghi_chu", ketQua.GhiChu ?? (BsonValue)BsonNull.Value },
                    { "trang_thai_chot", ketQua.TrangThaiChot ?? (BsonValue)BsonNull.Value }
                };

                if (eventType == "xet_nghiem")
                {
                    // Parse NoiDungKetQua from request as chi_so array for MongoDB
                    if (!string.IsNullOrWhiteSpace(request.NoiDungKetQua))
                    {
                        try
                        {
                            clsPayload.Add("chi_so", BsonSerializer.Deserialize<BsonArray>(request.NoiDungKetQua));
                        }
                        catch
                        {
                            clsPayload.Add("noi_dung_ket_qua", request.NoiDungKetQua);
                        }
                    }
                }
                else
                {
                    // chan_doan_hinh_anh
                    clsPayload.Add("mo_ta_hinh_anh", request.NoiDungKetQua ?? (BsonValue)BsonNull.Value);
                    
                    if (!string.IsNullOrWhiteSpace(ketQua.TepDinhKem))
                    {
                        try
                        {
                            clsPayload.Add("files", BsonSerializer.Deserialize<BsonArray>(ketQua.TepDinhKem));
                        }
                        catch
                        {
                            clsPayload.Add("tep_dinh_kem", ketQua.TepDinhKem);
                        }
                    }
                }

                try
                {
                    await _mongoHistory.LogEventAsync(
                        phieuCls.PhieuKhamLamSang.MaBenhNhan,
                        eventType,
                        clsPayload,
                        request.MaNhanSuThucHien);
                }
                catch (Exception)
                {
                    // MongoDB dual-write fail → log miss, MySQL data vẫn OK
                }
            }

            await _db.Entry(ketQua).Reference(k => k.NhanVienYTes).LoadAsync();

            var dto = new ClsResultDto
            {
                MaKetQua = ketQua.MaKetQua,
                MaChiTietDv = ketQua.MaChiTietDv,
                MaPhieuKhamCls = chiTiet.MaPhieuKhamCls,
                MaDichVu = chiTiet.MaDichVu,
                TenDichVu = chiTiet.DichVuYTe?.TenDichVu ?? "",
                TrangThaiChot = ketQua.TrangThaiChot,
                KetLuanChuyen = ketQua.KetLuanChuyen,
                GhiChu = ketQua.GhiChu,
                MaNhanSuThucHien = ketQua.MaNguoiTao,
                TenNhanSuThucHien = ketQua.NhanVienYTes?.HoTen ?? "",
                ThoiGianTao = ketQua.ThoiGianTao,
                TepDinhKem = ketQua.TepDinhKem
            };

            // Realtime: có kết quả CLS mới
            await LogMedicalEventSafeAsync(
                phieuCls?.PhieuKhamLamSang?.MaBenhNhan,
                "cls_service_completed",
                new BsonDocument
                {
                    { "ma_phieu_kham_cls", BsonOrNull(chiTiet.MaPhieuKhamCls) },
                    { "ma_chi_tiet_dv", chiTiet.MaChiTietDv },
                    { "ma_dich_vu", BsonOrNull(chiTiet.MaDichVu) },
                    { "ten_dich_vu", BsonOrNull(chiTiet.DichVuYTe?.TenDichVu) },
                    { "ma_phong", BsonOrNull(chiTiet.DichVuYTe?.MaPhongThucHien) },
                    { "ma_ket_qua", ketQua.MaKetQua },
                    { "ma_ky_thuat_vien", BsonOrNull(ketQua.MaNguoiTao) },
                    { "trang_thai_chot", BsonOrNull(ketQua.TrangThaiChot) },
                    { "thoi_gian_chot", BsonDateOrNull(ketQua.ThoiGianChot ?? ketQua.ThoiGianTao) }
                },
                request.MaNhanSuThucHien);

            await _realtime.BroadcastClsResultCreatedAsync(dto);

            // Targeted broadcast: gửi notification cho bác sĩ chỉ định
            if (phieuCls?.PhieuKhamLamSang is not null)
            {
                var maBacSiChiDinh = phieuCls.PhieuKhamLamSang.MaBacSiKham;
                if (!string.IsNullOrWhiteSpace(maBacSiChiDinh))
                {
                    var notificationReq = new NotificationCreateRequest
                    {
                        LoaiThongBao = "ket_qua_cls",
                        TieuDe = "Kết quả CLS mới",
                        NoiDung = $"Kết quả {chiTiet.DichVuYTe?.TenDichVu ?? "dịch vụ CLS"} cho bệnh nhân {phieuCls.PhieuKhamLamSang.BenhNhan?.HoTen ?? ""} đã sẵn sàng",
                        MucDoUuTien = "normal",
                        NguonLienQuan = "phieu_cls",
                        MaDoiTuongLienQuan = chiTiet.MaPhieuKhamCls,
                        NguoiNhan = new List<NotificationRecipientCreateRequest>
                        {
                            new NotificationRecipientCreateRequest
                            {
                                LoaiNguoiNhan = "bac_si",
                                MaNguoiNhan = maBacSiChiDinh
                            }
                        }
                    };
                    await _notifications.TaoThongBaoAsync(notificationReq);
                }
            }

            // ===== Sau khi chốt kết quả DV CLS =====
            // 1) Cập nhật trạng thái chi tiết DV (đã làm ở trên) -> broadcast để FE nắm
            await _realtime.BroadcastClsItemUpdatedAsync(await MapClsItemAsync(chiTiet));

            // 2) Cập nhật trạng thái hàng đợi CLS của DV hiện tại
            var hangDoi = await _db.HangDois.FirstOrDefaultAsync(h => h.MaChiTietDv == chiTiet.MaChiTietDv);
            if (hangDoi is not null)
            {
                await _queue.CapNhatTrangThaiHangDoiAsync(
                    hangDoi.MaHangDoi,
                    new QueueStatusUpdateRequest { TrangThai = "da_phuc_vu" });

                await LogMedicalEventSafeAsync(
                    phieuCls?.PhieuKhamLamSang?.MaBenhNhan,
                    "cls_queue_completed",
                    new BsonDocument
                    {
                        { "ma_phieu_kham_cls", BsonOrNull(chiTiet.MaPhieuKhamCls) },
                        { "ma_chi_tiet_dv", chiTiet.MaChiTietDv },
                        { "ma_hang_doi", hangDoi.MaHangDoi },
                        { "ma_phong", BsonOrNull(hangDoi.MaPhong) },
                        { "trang_thai", "da_phuc_vu" },
                        { "thoi_gian", BsonDateOrNull(now) }
                    },
                    request.MaNhanSuThucHien);

                // 3) Cập nhật trạng thái lượt khám CLS gắn với hàng đợi (nếu có)
                var luot = await _db.LuotKhamBenhs.FirstOrDefaultAsync(l => l.MaHangDoi == hangDoi.MaHangDoi);
                if (luot is not null)
                {
                    await _history.CapNhatTrangThaiLuotKhamAsync(
                        luot.MaLuotKham,
                        new HistoryVisitStatusUpdateRequest
                        {
                            TrangThai = "hoan_tat",
                            ThoiGianKetThuc = now
                        });
                }
            }

            // 4) Xác định còn DV khác không và enqueue DV tiếp theo nếu có
            static bool IsCompleted(string? status) =>
                string.Equals(status, "da_co_ket_qua", StringComparison.OrdinalIgnoreCase);

            var allDv = await _db.ChiTietDichVus
                .Include(c => c.DichVuYTe)
                .Where(c => c.MaPhieuKhamCls == chiTiet.MaPhieuKhamCls)
                .OrderBy(c => c.MaChiTietDv)
                .ToListAsync();

            var remaining = allDv
                .Where(c => !string.Equals(c.MaChiTietDv, chiTiet.MaChiTietDv, StringComparison.OrdinalIgnoreCase))
                .Where(c => !IsCompleted(c.TrangThai))
                .ToList();

            var maBenhNhan = phieuCls?.PhieuKhamLamSang?.MaBenhNhan;

            if (remaining.Any())
            {
                // Chuyển sang DV kế tiếp: trạng thái BN = chờ khám DV
                if (!string.IsNullOrWhiteSpace(maBenhNhan))
                {
                    await _patients.CapNhatTrangThaiBenhNhanAsync(
                        maBenhNhan,
                        new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham_dv" });
                }

                // Tạo hàng đợi cho DV tiếp theo nếu chưa có
                var next = remaining.First();
                var daCoHangDoi = await _db.HangDois.AnyAsync(h => h.MaChiTietDv == next.MaChiTietDv);
                if (!daCoHangDoi && !string.IsNullOrWhiteSpace(maBenhNhan))
                {
                    var maPhongNext = next.DichVuYTe?.MaPhongThucHien ?? "CLS_XN_01";
                    await _queue.ThemVaoHangDoiAsync(new QueueEnqueueRequest
                    {
                        MaBenhNhan = maBenhNhan,
                        MaPhong = maPhongNext,
                        LoaiHangDoi = "can_lam_sang",
                        MaChiTietDv = next.MaChiTietDv,
                        MaPhieuKham = null,
                        Nguon = null,
                        Nhan = null,
                        ThoiGianLichHen = null,
                        DoUuTien = 0,
                        CapCuu = false
                    });

                    await LogMedicalEventSafeAsync(
                        maBenhNhan,
                        "cls_transfer_to_next_service",
                        new BsonDocument
                        {
                            { "ma_phieu_kham_cls", BsonOrNull(next.MaPhieuKhamCls) },
                            { "from_ma_chi_tiet_dv", chiTiet.MaChiTietDv },
                            { "to_ma_chi_tiet_dv", next.MaChiTietDv },
                            { "to_ma_dich_vu", BsonOrNull(next.MaDichVu) },
                            { "to_ten_dich_vu", BsonOrNull(next.DichVuYTe?.TenDichVu) },
                            { "to_ma_phong", BsonOrNull(maPhongNext) },
                            { "trang_thai_benh_nhan", "cho_kham_dv" }
                        },
                        request.MaNhanSuThucHien);
                }
            }
            else
            {
                // Hoàn tất tất cả DV CLS
                if (!string.IsNullOrWhiteSpace(maBenhNhan))
                {
                    await _patients.CapNhatTrangThaiBenhNhanAsync(
                        maBenhNhan,
                        new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_xu_ly_dv" });
                }

                await LogMedicalEventSafeAsync(
                    maBenhNhan,
                    "cls_all_services_completed",
                    new BsonDocument
                    {
                        { "ma_phieu_kham_cls", BsonOrNull(chiTiet.MaPhieuKhamCls) },
                        { "completed_item_count", allDv.Count },
                        { "trang_thai_benh_nhan", "cho_xu_ly_dv" }
                    },
                    request.MaNhanSuThucHien);
            }

            // 5) AUTO PUBLISH: nếu DV cuối cùng đã xong thì tự lập phiếu tổng hợp
            await TryAutoPublishTongHopIfCompletedAsync(chiTiet.MaPhieuKhamCls);

            return dto;
        }

        public async Task<IReadOnlyList<ClsResultDto>> LayKetQuaTheoPhieuClsAsync(string maPhieuKhamCls)
        {
            await _overdueCleanup.CleanupAsync();

            var list = await _db.KetQuaDichVus
                .AsNoTracking()
                .Include(k => k.NhanVienYTes)
                .Include(k => k.ChiTietDichVu)
                    .ThenInclude(ct => ct.DichVuYTe)
                .Where(k => k.ChiTietDichVu.MaPhieuKhamCls == maPhieuKhamCls)
                .ToListAsync();

            return list.Select(k => new ClsResultDto
            {
                MaKetQua = k.MaKetQua,
                MaChiTietDv = k.MaChiTietDv,
                MaPhieuKhamCls = k.ChiTietDichVu.MaPhieuKhamCls,
                MaDichVu = k.ChiTietDichVu.MaDichVu,
                TenDichVu = k.ChiTietDichVu.DichVuYTe?.TenDichVu ?? "",
                TrangThaiChot = k.TrangThaiChot,
                KetLuanChuyen = k.KetLuanChuyen,
                GhiChu = k.GhiChu,
                MaNhanSuThucHien = k.MaNguoiTao,
                TenNhanSuThucHien = k.NhanVienYTes?.HoTen ?? "",
                ThoiGianTao = k.ThoiGianTao,
                TepDinhKem = k.TepDinhKem
            }).ToList();
        }

        // ================== 5. PHIẾU TỔNG HỢP KQ CLS ==================

        public async Task<ClsSummaryDto> TaoTongHopAsync(string maPhieuKhamCls)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                throw new ArgumentException("MaPhieuKhamCls là bắt buộc");

            var phieuCls = await _db.PhieuKhamCanLamSangs
                .Include(p => p.PhieuKhamLamSang)
                    .ThenInclude(ls => ls.BenhNhan)
                .Include(p => p.PhieuKhamLamSang)
                    .ThenInclude(ls => ls.DichVuKham)
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu CLS");

            var phieuLs = phieuCls.PhieuKhamLamSang
                ?? throw new InvalidOperationException("Phiếu CLS không gắn với phiếu khám LS");

            var bn = phieuLs.BenhNhan;
            var clsResults = await LayKetQuaTheoPhieuClsAsync(maPhieuKhamCls);
            var mongoClinicalEvents = await GetMongoClsEventsForSnapshotAsync(bn.MaBenhNhan, maPhieuKhamCls);

            var snapshotObj = new
            {
                PhieuCls = new
                {
                    phieuCls.MaPhieuKhamCls,
                    phieuCls.MaPhieuKhamLs,
                    phieuCls.NgayGioLap,
                    phieuCls.TrangThai,
                    phieuCls.GhiChu
                },
                BenhNhan = new
                {
                    bn.MaBenhNhan,
                    bn.HoTen,
                    bn.NgaySinh,
                    bn.GioiTinh,
                    bn.DienThoai,
                    bn.DiaChi
                },
                KetQua = clsResults,
                MongoClinicalEvents = mongoClinicalEvents
            };

            var snapshotJson = JsonSerializer.Serialize(snapshotObj);
            var now = DateTime.Now;
            var maNhanSuXuLy = clsResults
                .OrderByDescending(r => r.ThoiGianTao)
                .Select(r => r.MaNhanSuThucHien)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                ?? phieuLs.MaBacSiKham
                ?? phieuLs.MaNguoiLap;

            var summary = await _db.PhieuTongHopKetQuas
                .FirstOrDefaultAsync(s => s.MaPhieuKhamCls == maPhieuKhamCls);

            var isNew = summary is null;

            if (summary is null)
            {
                summary = new PhieuTongHopKetQua
                {
                    MaPhieuTongHop = $"THKQ-{Guid.NewGuid():N}",
                    MaPhieuKhamCls = maPhieuKhamCls,
                    LoaiPhieu = "tong_hop_cls",
                    MaNhanSuXuLy = maNhanSuXuLy,
                    TrangThai = "cho_xu_ly",
                    ThoiGianXuLy = now,
                    SnapshotJson = snapshotJson
                };
                _db.PhieuTongHopKetQuas.Add(summary);
            }
            else
            {
                summary.SnapshotJson = snapshotJson;
                summary.MaNhanSuXuLy = maNhanSuXuLy;
                summary.ThoiGianXuLy = now;
            }

            // ===== Gắn mã phiếu tổng hợp vào lại phiếu khám LS =====
            phieuLs.MaPhieuKqKhamCls = summary.MaPhieuTongHop;
            await _db.SaveChangesAsync();

            // CLS xong chi tao tong hop va cho y ta hanh chinh xu ly.
            // Queue service_return chi duoc mo khi y ta hanh chinh lap tiep nhan lai.
            await _patients.CapNhatTrangThaiBenhNhanAsync(
                phieuLs.MaBenhNhan,
                new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_xu_ly_dv" });

            var dto = new ClsSummaryDto
            {
                MaPhieuTongHop = summary.MaPhieuTongHop,
                MaPhieuKhamCls = summary.MaPhieuKhamCls,
                MaBenhNhan = bn.MaBenhNhan,
                TenBenhNhan = bn.HoTen,
                NgayGioLapPhieuCls = phieuCls.NgayGioLap,
                ThoiGianXuLy = summary.ThoiGianXuLy,
                TrangThai = summary.TrangThai,
                SnapshotJson = summary.SnapshotJson
            };

            await LogMedicalEventSafeAsync(
                bn.MaBenhNhan,
                "tong_hop_cls",
                new BsonDocument
                {
                    { "ma_phieu_tong_hop", summary.MaPhieuTongHop },
                    { "ma_phieu_kham_cls", summary.MaPhieuKhamCls },
                    { "ma_phieu_kham_ls", phieuCls.MaPhieuKhamLs },
                    { "ma_benh_nhan", bn.MaBenhNhan },
                    { "trang_thai", summary.TrangThai },
                    { "ma_nhan_su_xu_ly", BsonOrNull(summary.MaNhanSuXuLy) },
                    { "so_luong_ket_qua", clsResults.Count },
                    { "mongo_event_count", mongoClinicalEvents.Count },
                    { "buoc_tiep_theo", "cho_y_ta_hanh_chinh_xu_ly" },
                    { "nguon_queue_tra_ve", BsonNull.Value },
                    { "trang_thai_benh_nhan", "cho_xu_ly_dv" },
                    { "thoi_gian_xu_ly", BsonDateOrNull(summary.ThoiGianXuLy) }
                },
                summary.MaNhanSuXuLy);

            if (isNew)
            {
                await _realtime.BroadcastClsSummaryCreatedAsync(dto);
                // Thông báo: lập phiếu tổng hợp lần đầu
                await TaoThongBaoTongHopClsAsync(dto);
            }
            else
            {
                await _realtime.BroadcastClsSummaryUpdatedAsync(dto);
            }

            return dto;
        }

        public async Task<PagedResult<ClsSummaryDto>> LayTongHopKetQuaChoLapPhieuKhamAsync(
            ClsSummaryFilter filter,
            string? originMaKhoaScope = null,
            string? serviceMaKhoaScope = null,
            string? serviceMaPhongScope = null)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(filter.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc trong filter");

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;

            var query =
                from s in _db.PhieuTongHopKetQuas.AsNoTracking()
                join cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                    on s.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                    on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                join bn in _db.BenhNhans.AsNoTracking()
                    on ls.MaBenhNhan equals bn.MaBenhNhan
                where bn.MaBenhNhan == filter.MaBenhNhan
                select new { s, cls, ls, bn };

            if (!string.IsNullOrWhiteSpace(originMaKhoaScope))
            {
                query = query.Where(x =>
                    x.ls.BacSiKham.MaKhoa == originMaKhoaScope ||
                    x.ls.NguoiLap.MaKhoa == originMaKhoaScope);
            }

            if (!string.IsNullOrWhiteSpace(serviceMaKhoaScope))
            {
                query = query.Where(x =>
                    _db.ChiTietDichVus.Any(ct =>
                        ct.MaPhieuKhamCls == x.cls.MaPhieuKhamCls &&
                        ct.DichVuYTe.PhongThucHien.MaKhoa == serviceMaKhoaScope));
            }

            if (!string.IsNullOrWhiteSpace(serviceMaPhongScope))
            {
                query = query.Where(x =>
                    _db.ChiTietDichVus.Any(ct =>
                        ct.MaPhieuKhamCls == x.cls.MaPhieuKhamCls &&
                        ct.DichVuYTe.MaPhongThucHien == serviceMaPhongScope));
            }

            if (filter.FromDate.HasValue)
            {
                var from = filter.FromDate.Value.Date;
                query = query.Where(x => x.s.ThoiGianXuLy >= from);
            }

            if (filter.ToDate.HasValue)
            {
                var to = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.s.ThoiGianXuLy < to);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(x => x.s.TrangThai == filter.TrangThai);

            query = query.OrderByDescending(x => x.s.ThoiGianXuLy);

            var totalItems = await query.CountAsync();
            var pageData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = pageData.Select(x => new ClsSummaryDto
            {
                MaPhieuTongHop = x.s.MaPhieuTongHop,
                MaPhieuKhamCls = x.s.MaPhieuKhamCls,
                MaBenhNhan = x.bn.MaBenhNhan,
                TenBenhNhan = x.bn.HoTen,
                NgayGioLapPhieuCls = x.cls.NgayGioLap,
                ThoiGianXuLy = x.s.ThoiGianXuLy,
                TrangThai = x.s.TrangThai,
                SnapshotJson = x.s.SnapshotJson
            }).ToList();

            return new PagedResult<ClsSummaryDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ClsSummaryDto?> LayPhieuTongHopKetQuaAsync(string maPhieuTongHop)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maPhieuTongHop))
                return null;

            var row =
                await (from s in _db.PhieuTongHopKetQuas.AsNoTracking()
                       join cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                            on s.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                       join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                            on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                       join bn in _db.BenhNhans.AsNoTracking()
                            on ls.MaBenhNhan equals bn.MaBenhNhan
                       where s.MaPhieuTongHop == maPhieuTongHop
                       select new { s, cls, ls, bn })
                .FirstOrDefaultAsync();

            if (row is null) return null;

            return new ClsSummaryDto
            {
                MaPhieuTongHop = row.s.MaPhieuTongHop,
                MaPhieuKhamCls = row.s.MaPhieuKhamCls,
                MaBenhNhan = row.bn.MaBenhNhan,
                TenBenhNhan = row.bn.HoTen,
                NgayGioLapPhieuCls = row.cls.NgayGioLap,
                ThoiGianXuLy = row.s.ThoiGianXuLy,
                TrangThai = row.s.TrangThai,
                SnapshotJson = row.s.SnapshotJson
            };
        }

        public async Task<ClsSummaryDto?> CapNhatTrangThaiTongHopAsync(
            string maPhieuTongHop,
            ClsSummaryStatusUpdateRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maPhieuTongHop))
                return null;
            if (string.IsNullOrWhiteSpace(request.TrangThai))
                throw new ArgumentException("TrangThai là bắt buộc");

            var summary = await _db.PhieuTongHopKetQuas
                .FirstOrDefaultAsync(s => s.MaPhieuTongHop == maPhieuTongHop);

            if (summary is null) return null;

            summary.TrangThai = request.TrangThai;
            summary.ThoiGianXuLy = DateTime.Now;

            await _db.SaveChangesAsync();

            var dto = await LayPhieuTongHopKetQuaAsync(maPhieuTongHop);

            if (dto is not null)
            {
                await _realtime.BroadcastClsSummaryUpdatedAsync(dto);
            }

            return dto;
        }

        public async Task<ClsSummaryDto?> CapNhatPhieuTongHopAsync(
            string maPhieuTongHop,
            ClsSummaryUpdateRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            if (string.IsNullOrWhiteSpace(maPhieuTongHop))
                return null;

            var summary = await _db.PhieuTongHopKetQuas
                .FirstOrDefaultAsync(s => s.MaPhieuTongHop == maPhieuTongHop);

            if (summary is null) return null;

            if (!string.IsNullOrWhiteSpace(request.TrangThai))
                summary.TrangThai = request.TrangThai;

            if (!string.IsNullOrWhiteSpace(request.MaNhanSuXuLy))
                summary.MaNhanSuXuLy = request.MaNhanSuXuLy; // FE là nguồn chuẩn

            if (request.SnapshotJson is not null)
                summary.SnapshotJson = request.SnapshotJson;

            summary.ThoiGianXuLy = request.ThoiGianXuLy ?? DateTime.Now;

            await _db.SaveChangesAsync();

            var dto = await LayPhieuTongHopKetQuaAsync(maPhieuTongHop);

            if (dto is not null)
            {
                await _realtime.BroadcastClsSummaryUpdatedAsync(dto);
            }

            return dto;
        }
        // ==========================
        // =   THÔNG BÁO - CLS      =
        // ==========================

        // Auto-publish: khi tất cả DV trong 1 phiếu CLS đã có kết quả và AutoPublishEnabled = true
        // thì tự động tạo / cập nhật phiếu tổng hợp + gắn lại vào phiếu khám LS.
        private async Task TryAutoPublishTongHopIfCompletedAsync(string maPhieuKhamCls)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                return;

            var phieuCls = await _db.PhieuKhamCanLamSangs
                .Include(p => p.PhieuKhamLamSang)
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls);

            if (phieuCls is null)
                return;

            // Check AutoPublishEnabled flag
            if (!phieuCls.AutoPublishEnabled)
                return;

            // Còn chi tiết DV nào chưa hoàn tất không?
            var conChuaHoanTat = await _db.ChiTietDichVus
                .AnyAsync(c =>
                    c.MaPhieuKhamCls == maPhieuKhamCls &&
                    (c.TrangThai == null || c.TrangThai != "da_co_ket_qua"));

            if (conChuaHoanTat)
                return;

            // Tất cả DV đã hoàn tất -> chốt phiếu CLS
            phieuCls.TrangThai = "da_hoan_tat";
            await _db.SaveChangesAsync();

            // Lập / cập nhật phiếu tổng hợp
            var summary = await TaoTongHopAsync(maPhieuKhamCls);

            // Gắn MaPhieuKqKhamCls vào phiếu khám LS
            if (phieuCls.PhieuKhamLamSang is not null && !string.IsNullOrWhiteSpace(summary.MaPhieuTongHop))
            {
                phieuCls.PhieuKhamLamSang.MaPhieuKqKhamCls = summary.MaPhieuTongHop;
                await _db.SaveChangesAsync();
            }
        }



        private async Task TaoThongBaoChiDinhClsMoiAsync(ClsOrderDto order)
        {
            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "cls",
                TieuDe = "Chỉ định CLS mới",
                NoiDung = RenderThongBaoChiDinhClsMoi(order),
                MucDoUuTien = "normal",

                NguonLienQuan = "phieu_cls",
                MaDoiTuongLienQuan = order.MaPhieuKhamCls,

                // Chỉ y tá CLS (cận lâm sàng) thực hiện
                NguoiNhan = new List<NotificationRecipientCreateRequest>
{
    new NotificationRecipientCreateRequest
    {
        LoaiNguoiNhan = "y_ta_cls",
        MaNguoiNhan = null
    }
}
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        private static string RenderThongBaoChiDinhClsMoi(ClsOrderDto order)
        {
            var tenBn = string.IsNullOrWhiteSpace(order.TenBenhNhan)
                ? order.MaBenhNhan
                : $"{order.TenBenhNhan} ({order.MaBenhNhan})";

            return $"Có chỉ định cận lâm sàng mới cho bệnh nhân {tenBn}. " +
                   "Vui lòng sắp xếp thực hiện và cập nhật kết quả.";
        }


        private async Task TaoThongBaoTongHopClsAsync(ClsSummaryDto summary)
        {
            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "cls_tong_hop",
                TieuDe = "Phiếu tổng hợp kết quả CLS",
                NoiDung = RenderThongBaoTongHopCls(summary),
                MucDoUuTien = "normal",

                NguonLienQuan = "tong_hop_cls",
                MaDoiTuongLienQuan = summary.MaPhieuTongHop,

                NguoiNhan = new List<NotificationRecipientCreateRequest>
                {
                    new NotificationRecipientCreateRequest
                    {
                        LoaiNguoiNhan = "nhan_vien_y_te",
                        MaNguoiNhan = null
                    }
                }
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        private static string RenderThongBaoTongHopCls(ClsSummaryDto summary)
        {
            var tenBn = string.IsNullOrWhiteSpace(summary.TenBenhNhan)
                ? summary.MaBenhNhan
                : $"{summary.TenBenhNhan} ({summary.MaBenhNhan})";

            return $"Đã có phiếu tổng hợp kết quả cận lâm sàng cho bệnh nhân {tenBn}.";
        }

        // ============================================================
        // =                   HỦY PHIẾU CLS                          =
        // ============================================================

        public async Task HuyPhieuClsAsync(string maPhieuKhamCls)
        {
            await _overdueCleanup.CleanupAsync();

            var phieu = await _db.PhieuKhamCanLamSangs
                .Include(p => p.ChiTietDichVus)
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu CLS {maPhieuKhamCls}");

            // Chỉ cho phép hủy khi: da_tao
            if (!string.Equals(phieu.TrangThai, "da_tao", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Phiếu CLS đang ở trạng thái '{phieu.TrangThai}' — không thể hủy. Chỉ hủy được khi 'da_tao'.");

            phieu.TrangThai = "da_huy";

            // Rollback trạng thái chi tiết dịch vụ
            foreach (var ct in phieu.ChiTietDichVus)
            {
                ct.TrangThai = "da_huy";
            }

            await _db.SaveChangesAsync();

            // Broadcast: phiếu CLS đã hủy
            var dto = await BuildClsOrderDtoAsync(maPhieuKhamCls);
            if (dto is not null)
                await _realtime.BroadcastClsOrderStatusUpdatedAsync(dto);

            // Dashboard refresh
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);

            // Thông báo cho bác sĩ chỉ định CLS
            if (dto is not null)
                await TaoThongBaoHuyPhieuClsAsync(dto);
        }

        private async Task TaoThongBaoHuyPhieuClsAsync(ClsOrderDto order)
        {
            // Lấy bác sĩ chỉ định từ phiếu khám lâm sàng gốc
            var phieuCls = await _db.PhieuKhamCanLamSangs
                .Include(p => p.PhieuKhamLamSang)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == order.MaPhieuKhamCls);

            var maBacSi = phieuCls?.PhieuKhamLamSang?.MaBacSiKham;
            if (string.IsNullOrWhiteSpace(maBacSi))
                return;

            var tenBn = string.IsNullOrWhiteSpace(order.TenBenhNhan)
                ? order.MaBenhNhan
                : $"{order.TenBenhNhan} ({order.MaBenhNhan})";

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "cls",
                TieuDe = "Phiếu CLS đã hủy",
                NoiDung = $"Phiếu cận lâm sàng {order.MaPhieuKhamCls} của bệnh nhân {tenBn} đã bị hủy.",
                MucDoUuTien = "normal",

                NguonLienQuan = "phieu_cls",
                MaDoiTuongLienQuan = order.MaPhieuKhamCls,

                NguoiNhan = new List<NotificationRecipientCreateRequest>
                {
                    new NotificationRecipientCreateRequest
                    {
                        LoaiNguoiNhan = "bac_si",
                        MaNguoiNhan = maBacSi
                    }
                }
            };

            await _notifications.TaoThongBaoAsync(request);
        }

    }
}
