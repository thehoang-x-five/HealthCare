using HealthCare.Datas;
using HealthCare.Entities;
using HealthCare.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthCare.Services.Background
{
    /// <summary>
    /// Background service tự động:
    /// 1. Cuối ngày (23:59): Hủy tất cả phiếu khám/đơn chưa hoàn thành
    /// 2. Đầu ngày mới (00:00): Reset trạng thái hôm nay của bệnh nhân
    /// </summary>
    public class DailyResetService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyResetService> _logger;

        public DailyResetService(
            IServiceProvider serviceProvider,
            ILogger<DailyResetService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyResetService đã khởi động");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextRun = CalculateNextRunTime(now);
                    var delay = nextRun - now;

                    _logger.LogInformation(
                        "Lần chạy tiếp theo: {NextRun} (sau {Delay})",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss"),
                        delay);

                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    // Xác định task cần chạy
                    var currentTime = DateTime.Now;
                    var hour = currentTime.Hour;
                    var minute = currentTime.Minute;

                    // 23:59 - Hủy phiếu khám chưa hoàn thành
                    if (hour == 23 && minute >= 59)
                    {
                        await CancelUnfinishedExamsAsync();
                    }
                    // 00:00 - Reset trạng thái hôm nay
                    else if (hour == 0 && minute < 5)
                    {
                        await ResetDailyStatusAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong DailyResetService");
                    // Đợi 1 phút trước khi thử lại
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("DailyResetService đã dừng");
        }

        /// <summary>
        /// Tính thời điểm chạy tiếp theo (23:59 hoặc 00:00)
        /// </summary>
        private DateTime CalculateNextRunTime(DateTime now)
        {
            var today = now.Date;
            var endOfDay = today.AddHours(23).AddMinutes(59);
            var startOfNextDay = today.AddDays(1);

            // Nếu chưa đến 23:59 hôm nay -> chạy lúc 23:59
            if (now < endOfDay)
                return endOfDay;

            // Nếu đã qua 23:59 -> chạy lúc 00:00 ngày mai
            return startOfNextDay;
        }

        /// <summary>
        /// Task 1: Hủy tất cả phiếu khám/đơn chưa hoàn thành (chạy lúc 23:59)
        /// </summary>
        private async Task CancelUnfinishedExamsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            try
            {
                _logger.LogInformation("Bắt đầu hủy phiếu khám chưa hoàn thành...");

                var today = DateTime.Today;
                var now = DateTime.Now;

                // 1. Lấy danh sách bệnh nhân có trạng thái hôm nay khác hoàn thành
                var unfinishedPatients = await db.BenhNhans
                    .Where(b =>
                        !string.IsNullOrEmpty(b.TrangThaiHomNay) &&
                        b.TrangThaiHomNay != TrangThaiHomNay.DaHoanTat &&
                        b.NgayTrangThai == today)
                    .Select(b => b.MaBenhNhan)
                    .ToListAsync();

                if (!unfinishedPatients.Any())
                {
                    _logger.LogInformation("Không có bệnh nhân nào cần hủy phiếu khám");
                    return;
                }

                _logger.LogInformation(
                    "Tìm thấy {Count} bệnh nhân chưa hoàn thành",
                    unfinishedPatients.Count);

                // 2. Hủy phiếu khám lâm sàng
                var canceledLs = await db.PhieuKhamLamSangs
                    .Where(p =>
                        unfinishedPatients.Contains(p.MaBenhNhan) &&
                        p.NgayLap == today &&
                        p.TrangThai != TrangThaiPhieuKhamLs.DaHoanTat &&
                        p.TrangThai != TrangThaiPhieuKhamLs.DaHuy)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.TrangThai, TrangThaiPhieuKhamLs.DaHuy));

                _logger.LogInformation("Đã hủy {Count} phiếu khám lâm sàng", canceledLs);

                // 3. Hủy phiếu khám CLS
                var canceledCls = await db.PhieuKhamCanLamSangs
                    .Where(p =>
                        unfinishedPatients.Contains(p.PhieuKhamLamSang.MaBenhNhan) &&
                        p.NgayGioLap.Date == today &&
                        p.TrangThai != TrangThaiPhieuKhamCls.DaHoanTat)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.TrangThai, TrangThaiPhieuKhamCls.DaHuy));

                _logger.LogInformation("Đã hủy {Count} phiếu khám CLS", canceledCls);

                // 4. Hủy chi tiết dịch vụ CLS
                var canceledClsItems = await db.ChiTietDichVus
                    .Where(ct =>
                        unfinishedPatients.Contains(ct.PhieuKhamCanLamSang.PhieuKhamLamSang.MaBenhNhan) &&
                        ct.PhieuKhamCanLamSang.NgayGioLap.Date == today &&
                        ct.TrangThai != TrangThaiChiTietDv.DaCoKetQua)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ct => ct.TrangThai, TrangThaiChiTietDv.DaHuy));

                _logger.LogInformation("Đã hủy {Count} chi tiết dịch vụ CLS", canceledClsItems);

                // 5. Hủy đơn thuốc (chỉ hủy đơn chưa phát)
                var canceledPrescriptions = await db.DonThuocs
                    .Where(d =>
                        unfinishedPatients.Contains(d.MaBenhNhan) &&
                        d.ThoiGianKeDon.Date == today &&
                        d.TrangThai != TrangThaiDonThuoc.DaPhat)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(d => d.TrangThai, TrangThaiDonThuoc.DaHuy));

                _logger.LogInformation("Đã hủy {Count} đơn thuốc", canceledPrescriptions);

                // 6. Hủy lịch hẹn khám chưa check-in
                var canceledAppointments = await db.LichHenKhams
                    .Where(l =>
                        unfinishedPatients.Contains(l.MaBenhNhan) &&
                        l.NgayHen == today &&
                        l.TrangThai != TrangThaiLichHen.DaCheckin &&
                        l.TrangThai != TrangThaiLichHen.DaHuy)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(l => l.TrangThai, TrangThaiLichHen.DaHuy));

                _logger.LogInformation("Đã hủy {Count} lịch hẹn khám", canceledAppointments);

                // 7. Xóa hàng đợi chưa hoàn thành (chỉ xóa hàng đợi chưa phục vụ)
                var deletedQueues = await db.HangDois
                    .Where(h =>
                        unfinishedPatients.Contains(h.MaBenhNhan) &&
                        h.ThoiGianCheckin.Date == today &&
                        h.TrangThai != TrangThaiHangDoi.DaPhucVu)
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Đã xóa {Count} hàng đợi", deletedQueues);

                // 8. Hủy lượt khám bệnh chưa hoàn tất (set về hoàn tất với thời gian kết thúc)
                var canceledVisits = await db.LuotKhamBenhs
                    .Where(l =>
                        unfinishedPatients.Contains(l.HangDoi.MaBenhNhan) &&
                        l.ThoiGianBatDau.Date == today &&
                        l.TrangThai != TrangThaiLuotKham.HoanTat)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(l => l.TrangThai, TrangThaiLuotKham.HoanTat)
                        .SetProperty(l => l.ThoiGianKetThuc, DateTime.Now));

                _logger.LogInformation("Đã đóng {Count} lượt khám bệnh", canceledVisits);

                _logger.LogInformation(
                    "Hoàn thành hủy phiếu khám: LS={Ls}, CLS={Cls}, ClsItems={ClsItems}, Prescriptions={Prescriptions}, Appointments={Appointments}, Queues={Queues}, Visits={Visits}",
                    canceledLs, canceledCls, canceledClsItems, canceledPrescriptions, canceledAppointments, deletedQueues, canceledVisits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy phiếu khám chưa hoàn thành");
                throw;
            }
        }

        /// <summary>
        /// Task 2: Reset trạng thái hôm nay của tất cả bệnh nhân (chạy lúc 00:00)
        /// </summary>
        private async Task ResetDailyStatusAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            try
            {
                _logger.LogInformation("Bắt đầu reset trạng thái hôm nay...");

                var yesterday = DateTime.Today.AddDays(-1);

                // Reset tất cả bệnh nhân có trạng thái của ngày hôm qua
                var resetCount = await db.BenhNhans
                    .Where(b =>
                        !string.IsNullOrEmpty(b.TrangThaiHomNay) &&
                        b.NgayTrangThai == yesterday)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.TrangThaiHomNay, (string?)null)
                        .SetProperty(b => b.NgayTrangThai, DateTime.Today));

                _logger.LogInformation(
                    "Đã reset trạng thái hôm nay cho {Count} bệnh nhân",
                    resetCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi reset trạng thái hôm nay");
                throw;
            }
        }
    }
}
