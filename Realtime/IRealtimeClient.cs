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

        /// <summary>
        /// Khi tạo mới / upsert bệnh nhân thành công.
        /// </summary>
        Task PatientCreated(PatientDto benhNhan);

        /// <summary>
        /// Khi cập nhật thông tin bệnh nhân.
        /// </summary>
        Task PatientUpdated(PatientDto benhNhan);

        /// <summary>
        /// Khi cập nhật trạng thái khám trong ngày của bệnh nhân (TrangThaiHomNay).
        /// </summary>
        Task PatientStatusUpdated(PatientDto benhNhan);


        // ==========================================
        // ===== KHÁM BỆNH (PHIẾU KHÁM LÂM SÀNG) ====
        // ==========================================

        /// <summary>
        /// Khi tạo phiếu khám LS mới (TaoPhieuKham).
        /// </summary>
        Task ClinicalExamCreated(ClinicalExamDto phieuKham);

        /// <summary>
        /// Khi sửa phiếu khám LS (nội dung khám, triệu chứng...).
        /// </summary>
        Task ClinicalExamUpdated(ClinicalExamDto phieuKham);

        /// <summary>
        /// Khi tạo / cập nhật chẩn đoán cuối.
        /// </summary>
        Task FinalDiagnosisChanged(FinalDiagnosisDto chanDoan);


        // ======================================
        // ===== CẬN LÂM SÀNG (CLS SERVICE) =====
        // ======================================

        /// <summary>
        /// Khi tạo phiếu cận lâm sàng (TaoPhieuCls).
        /// </summary>
        Task ClsOrderCreated(ClsOrderDto phieuCls);

        /// <summary>
        /// Khi cập nhật phiếu CLS (ví dụ sửa ghi chú... nếu có).
        /// </summary>
        Task ClsOrderUpdated(ClsOrderDto phieuCls);

        /// <summary>
        /// Khi đổi trạng thái phiếu CLS: da_lap, dang_thuc_hien, da_hoan_tat.
        /// </summary>
        Task ClsOrderStatusUpdated(ClsOrderDto phieuCls);

        /// <summary>
        /// Khi tạo kết quả cận lâm sàng cho 1 dịch vụ (TaoKetQua).
        /// </summary>
        Task ClsResultCreated(ClsResultDto ketQua);

        /// <summary>
        /// Khi tạo phiếu tổng hợp CLS từ 1 order.
        /// </summary>
        Task ClsSummaryCreated(ClsSummaryDto tongHop);

        /// <summary>
        /// Khi cập nhật phiếu tổng hợp CLS (trạng thái, snapshot...).
        /// </summary>
        Task ClsSummaryUpdated(ClsSummaryDto tongHop);

        /// <summary>
        /// Khi chi tiết dịch vụ CLS được cập nhật trạng thái.
        /// </summary>
        Task ClsItemUpdated(ClsItemDto item);

        // ==========================
        // ===== PHARMACY ===========
        // ==========================

        /// <summary>
        /// Khi thêm mới hoặc cập nhật thuốc trong kho.
        /// FE: cập nhật lại danh sách kho thuốc / trạng thái tồn.
        /// </summary>
        Task DrugChanged(DrugDto thuoc);

        /// <summary>
        /// Khi tạo đơn thuốc mới.
        /// FE: màn Đơn thuốc / Bệnh nhân nhận được đơn mới.
        /// </summary>
        Task PrescriptionCreated(PrescriptionDto donThuoc);

        /// <summary>
        /// Khi cập nhật trạng thái đơn thuốc (vd: da_ke, da_phat, huy...).
        /// FE: update badge / filter trạng thái đơn.
        /// </summary>
        Task PrescriptionStatusUpdated(PrescriptionDto donThuoc);
        // =====================================
        // ===== LƯỢT KHÁM (VISIT / HISTORY) ===
        // =====================================

        /// <summary>
        /// Khi tạo mới Lượt khám (LuotKham) – dùng cho HistoryService.
        /// (VisitDto là DTO mới sẽ define trong HealthCare.DTOs).
        /// </summary>
        Task VisitCreated(HistoryVisitRecordDto luotKham);

        /// <summary>
        /// Khi cập nhật trạng thái / thông tin Lượt khám.
        /// </summary>
        Task VisitStatusUpdated(HistoryVisitRecordDto luotKham);


        // =======================
        // ===== HÀNG ĐỢI ========
        // =======================

        /// <summary>
        /// Cập nhật toàn bộ hàng đợi của 1 phòng (GetByRoom).
        /// </summary>
        Task QueueByRoomUpdated(IReadOnlyList<QueueItemDto> items);

        /// <summary>
        /// Khi 1 item trong hàng đợi thay đổi (checkin, đổi trạng thái, dequeue...).
        /// </summary>
        Task QueueItemChanged(QueueItemDto item);


        // =======================
        // ===== LỊCH HẸN ========
        // =======================

        /// <summary>
        /// Khi tạo / cập nhật / đổi trạng thái lịch hẹn.
        /// </summary>
        Task AppointmentChanged(AppointmentReadRequestDto lichHen);


        // ===================================
        // ===== HOÁ ĐƠN / THANH TOÁN =======
        // ===================================

        /// <summary>
        /// Khi tạo mới hoặc đổi trạng thái hoá đơn thanh toán.
        /// </summary>
        Task InvoiceChanged(InvoiceDto hoaDon);


        // =========================
        // ===== THÔNG BÁO =========
        // =========================

        /// <summary>
        /// Khi BE tạo thông báo và push realtime cho người nhận.
        /// </summary>
        Task NotificationCreated(NotificationDto thongBao);

        /// <summary>
        /// Khi trạng thái thông báo thay đổi: da_doc, da_gui...
        /// </summary>
        Task NotificationUpdated(NotificationDto thongBao);
    }
}
