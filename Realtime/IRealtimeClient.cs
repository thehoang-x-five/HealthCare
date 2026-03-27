using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Realtime
{
    /// <summary>
    /// Hợp đồng cho client SignalR (FE).
    /// Mỗi method tương ứng với 1 event bên FE: on("MethodName", handler).
    /// </summary>
    public interface IRealtimeClient
    {
        // =======================
        // ===== DASHBOARD =======
        // =======================

        Task DashboardTodayUpdated(DashboardTodayDto dashboard);
        Task TodayPatientsKpiUpdated(TodayPatientsKpiDto dto);
        Task TodayAppointmentsKpiUpdated(TodayAppointmentsKpiDto dto);
        Task TodayRevenueKpiUpdated(TodayRevenueKpiDto dto);
        Task TodayExamOverviewUpdated(TodayExamOverviewDto dto);

        /// <summary>
        /// Cập nhật danh sách lịch hẹn sắp tới (hôm nay) trên dashboard.
        /// </summary>
        Task UpcomingAppointmentsUpdated(
            IReadOnlyList<UpcomingAppointmentDashboardItemDto> items);

        /// <summary>
        /// Cập nhật danh sách "Hoạt động gần đây" trên dashboard.
        /// </summary>
        Task RecentActivitiesUpdated(
            IReadOnlyList<DashboardActivityDto> items);


        // =======================
        // ===== BỆNH NHÂN =======
        // =======================

        Task PatientCreated(PatientDto benhNhan);
        Task PatientUpdated(PatientDto benhNhan);
        Task PatientStatusUpdated(PatientDto benhNhan);


        // ==========================================
        // ===== KHÁM BỆNH (PHIẾU KHÁM LÂM SÀNG) ====
        // ==========================================

        Task ClinicalExamCreated(ClinicalExamDto phieuKham);
        Task ClinicalExamUpdated(ClinicalExamDto phieuKham);
        Task FinalDiagnosisChanged(FinalDiagnosisDto chanDoan);


        // ======================================
        // ===== CẬN LÂM SÀNG (CLS SERVICE) =====
        // ======================================

        Task ClsOrderCreated(ClsOrderDto phieuCls);
        Task ClsOrderUpdated(ClsOrderDto phieuCls);
        Task ClsOrderStatusUpdated(ClsOrderDto phieuCls);
        Task ClsResultCreated(ClsResultDto ketQua);
        Task ClsSummaryCreated(ClsSummaryDto tongHop);
        Task ClsSummaryUpdated(ClsSummaryDto tongHop);
        Task ClsItemUpdated(ClsItemDto item);


        // ==========================
        // ===== PHARMACY ===========
        // ==========================

        Task DrugChanged(DrugDto thuoc);
        Task PrescriptionCreated(PrescriptionDto donThuoc);
        Task PrescriptionStatusUpdated(PrescriptionDto donThuoc);


        // =====================================
        // ===== LƯỢT KHÁM (VISIT / HISTORY) ===
        // =====================================

        Task VisitCreated(HistoryVisitRecordDto luotKham);
        Task VisitStatusUpdated(HistoryVisitRecordDto luotKham);


        // =======================
        // ===== HÀNG ĐỢI ========
        // =======================

        Task QueueByRoomUpdated(IReadOnlyList<QueueItemDto> items);
        Task QueueItemChanged(QueueItemDto item);


        // =======================
        // ===== LỊCH HẸN ========
        // =======================

        Task AppointmentChanged(AppointmentReadRequestDto lichHen);


        // ===================================
        // ===== HOÁ ĐƠN / THANH TOÁN =======
        // ===================================

        Task InvoiceChanged(InvoiceDto hoaDon);


        // =========================
        // ===== THÔNG BÁO =========
        // =========================

        Task NotificationCreated(NotificationDto thongBao);
        Task NotificationUpdated(NotificationDto thongBao);


        // ==========================
        // ===== NHÂN SỰ (ADMIN) ===
        // ==========================

        /// <summary>
        /// Khi Admin tạo/sửa/khóa-mở nhân viên.
        /// FE: cập nhật danh sách nhân sự, dashboard online count.
        /// </summary>
        Task StaffChanged(AdminUserDto staff);
    }
}
