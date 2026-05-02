using HealthCare.Datas;
using HealthCare.Enums;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services.OutpatientCare
{
    public interface IOverdueWorkflowCleanupService
    {
        Task CleanupAsync(CancellationToken cancellationToken = default);
    }

    public class OverdueWorkflowCleanupService(DataContext db) : IOverdueWorkflowCleanupService
    {
        private readonly DataContext _db = db;
        private const string CanceledStatus = "da_huy";
        private const string LegacyCanceledStatus = "huy";
        private const string ReservedInvoiceStatus = "bao_luu";

        private static readonly string[] ExpiringPatientStatuses =
        {
            TrangThaiHomNay.ChoTiepNhan,
            TrangThaiHomNay.ChoTiepNhanDv,
            TrangThaiHomNay.ChoKham,
            TrangThaiHomNay.ChoKhamDv,
            TrangThaiHomNay.DangKham,
            TrangThaiHomNay.DangKhamDv,
            TrangThaiHomNay.ChoXuLy,
            TrangThaiHomNay.ChoXuLyDv
        };

        public async Task CleanupAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            var overdueClinical = await _db.PhieuKhamLamSangs
                .Where(p =>
                    p.NgayLap < today &&
                    p.TrangThai != TrangThaiPhieuKhamLs.DaHoanTat &&
                    p.TrangThai != TrangThaiPhieuKhamLs.DaHuy)
                .Select(p => new { p.MaPhieuKham, p.MaBenhNhan })
                .ToListAsync(cancellationToken);

            var overdueClinicalIds = overdueClinical
                .Select(p => p.MaPhieuKham)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var overdueCls = await _db.PhieuKhamCanLamSangs
                .Where(p =>
                    p.NgayGioLap < today &&
                    p.TrangThai != TrangThaiPhieuKhamCls.DaHoanTat &&
                    p.TrangThai != TrangThaiPhieuKhamCls.DaHuy)
                .Select(p => new
                {
                    p.MaPhieuKhamCls,
                    p.MaPhieuKhamLs,
                    p.PhieuKhamLamSang.MaBenhNhan
                })
                .ToListAsync(cancellationToken);

            var overdueClsIds = overdueCls
                .Select(p => p.MaPhieuKhamCls)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var overduePrescriptions = await _db.DonThuocs
                .Where(d =>
                    d.ThoiGianKeDon < today &&
                    d.TrangThai != TrangThaiDonThuoc.DaPhat &&
                    d.TrangThai != TrangThaiDonThuoc.DaHuy)
                .Select(d => new { d.MaDonThuoc, d.MaBenhNhan })
                .ToListAsync(cancellationToken);

            var overduePrescriptionIds = overduePrescriptions
                .Select(d => d.MaDonThuoc)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var overdueQueuePatients = await _db.HangDois
                .Where(h =>
                    h.ThoiGianCheckin < today &&
                    h.TrangThai != TrangThaiHangDoi.DaPhucVu &&
                    h.TrangThai != CanceledStatus &&
                    h.TrangThai != LegacyCanceledStatus)
                .Select(h => h.MaBenhNhan)
                .Distinct()
                .ToListAsync(cancellationToken);

            var overdueVisitPatients = await _db.LuotKhamBenhs
                .Where(l =>
                    l.ThoiGianBatDau < today &&
                    l.TrangThai != TrangThaiLuotKham.HoanTat &&
                    l.TrangThai != TrangThaiLuotKham.DaHuy)
                .Select(l => l.HangDoi.MaBenhNhan)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (overdueClinicalIds.Count > 0)
            {
                await _db.PhieuKhamLamSangs
                    .Where(p => overdueClinicalIds.Contains(p.MaPhieuKham))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.TrangThai, TrangThaiPhieuKhamLs.DaHuy), cancellationToken);
            }

            if (overdueClsIds.Count > 0)
            {
                await _db.PhieuKhamCanLamSangs
                    .Where(p => overdueClsIds.Contains(p.MaPhieuKhamCls))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.TrangThai, TrangThaiPhieuKhamCls.DaHuy), cancellationToken);

                await _db.ChiTietDichVus
                    .Where(ct =>
                        overdueClsIds.Contains(ct.MaPhieuKhamCls) &&
                        ct.TrangThai != TrangThaiChiTietDv.DaCoKetQua &&
                        ct.TrangThai != "da_hoan_tat" &&
                        ct.TrangThai != TrangThaiChiTietDv.DaHuy)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ct => ct.TrangThai, TrangThaiChiTietDv.DaHuy), cancellationToken);
            }

            if (overduePrescriptionIds.Count > 0)
            {
                await _db.DonThuocs
                    .Where(d => overduePrescriptionIds.Contains(d.MaDonThuoc))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(d => d.TrangThai, TrangThaiDonThuoc.DaHuy), cancellationToken);
            }

            await _db.HangDois
                .Where(h =>
                    h.ThoiGianCheckin < today &&
                    h.TrangThai != TrangThaiHangDoi.DaPhucVu &&
                    h.TrangThai != CanceledStatus &&
                    h.TrangThai != LegacyCanceledStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(h => h.TrangThai, CanceledStatus)
                    .SetProperty(h => h.NgayCapNhat, now), cancellationToken);

            await _db.LuotKhamBenhs
                .Where(l =>
                    l.ThoiGianBatDau < today &&
                    l.TrangThai != TrangThaiLuotKham.HoanTat &&
                    l.TrangThai != TrangThaiLuotKham.DaHuy)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(l => l.TrangThai, TrangThaiLuotKham.DaHuy)
                    .SetProperty(l => l.ThoiGianKetThuc, now)
                    .SetProperty(l => l.NgayCapNhat, now), cancellationToken);

            await CleanupInvoicesAsync(overdueClinicalIds, overdueClsIds, overduePrescriptionIds, now, cancellationToken);

            var overduePatientIds = overdueClinical.Select(p => p.MaBenhNhan)
                .Concat(overdueCls.Select(p => p.MaBenhNhan))
                .Concat(overduePrescriptions.Select(p => p.MaBenhNhan))
                .Concat(overdueQueuePatients)
                .Concat(overdueVisitPatients)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (overduePatientIds.Count > 0)
            {
                var expiringStatuses = ExpiringPatientStatuses;

                await _db.BenhNhans
                    .Where(b =>
                        overduePatientIds.Contains(b.MaBenhNhan) &&
                        b.TrangThaiHomNay != null &&
                        expiringStatuses.Contains(b.TrangThaiHomNay) &&
                        b.NgayTrangThai < today)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.TrangThaiHomNay, TrangThaiHomNay.DaHuy), cancellationToken);
            }
        }

        private async Task CleanupInvoicesAsync(
            IReadOnlyCollection<string> overdueClinicalIds,
            IReadOnlyCollection<string> overdueClsIds,
            IReadOnlyCollection<string> overduePrescriptionIds,
            DateTime canceledAt,
            CancellationToken cancellationToken)
        {
            if (overdueClinicalIds.Count == 0 &&
                overdueClsIds.Count == 0 &&
                overduePrescriptionIds.Count == 0)
            {
                return;
            }

            await _db.HoaDonThanhToans
                .Where(h =>
                    h.TrangThai == "chua_thu" &&
                    (
                        (h.MaPhieuKham != null && overdueClinicalIds.Contains(h.MaPhieuKham)) ||
                        (h.MaPhieuKhamCls != null && overdueClsIds.Contains(h.MaPhieuKhamCls)) ||
                        (h.MaDonThuoc != null && overduePrescriptionIds.Contains(h.MaDonThuoc))
                    ))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(h => h.TrangThai, TrangThaiHoaDon.DaHuy)
                    .SetProperty(h => h.ThoiGianHuy, canceledAt), cancellationToken);

            await _db.HoaDonThanhToans
                .Where(h =>
                    h.TrangThai == "da_thu" &&
                    (
                        (h.MaPhieuKham != null && overdueClinicalIds.Contains(h.MaPhieuKham)) ||
                        (h.MaPhieuKhamCls != null && overdueClsIds.Contains(h.MaPhieuKhamCls)) ||
                        (h.MaDonThuoc != null && overduePrescriptionIds.Contains(h.MaDonThuoc))
                    ))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(h => h.TrangThai, ReservedInvoiceStatus)
                    .SetProperty(h => h.ThoiGianHuy, canceledAt), cancellationToken);
        }
    }
}
