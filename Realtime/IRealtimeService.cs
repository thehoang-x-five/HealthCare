using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Realtime
{
    /// <summary>
    /// Facade dùng từ các service nghiệp vụ để bắn realtime (SignalR).
    /// </summary>
    public interface IRealtimeService
    {
        // ==============================
        // ===== DASHBOARD / KPI ========
        // ==============================

        Task BroadcastDashboardTodayAsync(DashboardTodayDto dashboard);

        Task BroadcastTodayPatientsKpiAsync(TodayPatientsKpiDto dto);
        Task BroadcastTodayAppointmentsKpiAsync(TodayAppointmentsKpiDto dto);
        Task BroadcastTodayRevenueKpiAsync(TodayRevenueKpiDto dto);
        Task BroadcastTodayExamOverviewAsync(TodayExamOverviewDto dto);

        Task BroadcastUpcomingAppointmentsAsync(
            IReadOnlyList<UpcomingAppointmentDashboardItemDto> items);

        Task BroadcastRecentActivitiesAsync(
            IReadOnlyList<DashboardActivityDto> items);


        // ==========================
        // ===== BỆNH NHÂN =========
        // ==========================

        /// <summary>Thêm mới bệnh nhân.</summary>
        Task BroadcastPatientCreatedAsync(PatientDto benhNhan);

        /// <summary>Cập nhật thông tin hành chính (họ tên, địa chỉ...).</summary>
        Task BroadcastPatientUpdatedAsync(PatientDto benhNhan);

        /// <summary>Cập nhật trạng thái trong ngày (TrangThaiHomNay).</summary>
        Task BroadcastPatientStatusUpdatedAsync(PatientDto benhNhan);


        // =====================================
        // ===== KHÁM BỆNH (LÂM SÀNG) =========
        // =====================================

        /// <summary>Tạo phiếu khám lâm sàng mới.</summary>
        Task BroadcastClinicalExamCreatedAsync(ClinicalExamDto phieuKham);

        /// <summary>Cập nhật phiếu khám (trạng thái, triệu chứng...).</summary>
        Task BroadcastClinicalExamUpdatedAsync(ClinicalExamDto phieuKham);

        /// <summary>Cập nhật chẩn đoán cuối cùng.</summary>
        Task BroadcastFinalDiagnosisChangedAsync(FinalDiagnosisDto chanDoan);


        // ======================================
        // ===== CẬN LÂM SÀNG (CLS SERVICE) =====
        // ======================================

        /// <summary>Khi tạo phiếu cận lâm sàng (order CLS).</summary>
        Task BroadcastClsOrderCreatedAsync(ClsOrderDto phieuCls);

        /// <summary>Khi cập nhật nội dung phiếu CLS.</summary>
        Task BroadcastClsOrderUpdatedAsync(ClsOrderDto phieuCls);

        /// <summary>Khi đổi trạng thái phiếu CLS: da_lap, dang_thuc_hien, da_hoan_tat.</summary>
        Task BroadcastClsOrderStatusUpdatedAsync(ClsOrderDto phieuCls);

        /// <summary>Khi tạo kết quả CLS cho một dịch vụ.</summary>
        Task BroadcastClsResultCreatedAsync(ClsResultDto ketQua);

        /// <summary>Khi tạo phiếu tổng hợp CLS từ một order.</summary>
        Task BroadcastClsSummaryCreatedAsync(ClsSummaryDto tongHop);

        /// <summary>Khi cập nhật phiếu tổng hợp CLS.</summary>
        Task BroadcastClsSummaryUpdatedAsync(ClsSummaryDto tongHop);

        /// <summary>Cập nhật trạng thái chi tiết dịch vụ CLS.</summary>
        Task BroadcastClsItemUpdatedAsync(ClsItemDto item);

        // ==========================
        // ===== PHARMACY ===========
        // ==========================

        /// <summary>Khi thêm mới hoặc cập nhật thuốc trong kho.</summary>
        Task BroadcastDrugChangedAsync(DrugDto thuoc);

        /// <summary>Khi tạo đơn thuốc mới.</summary>
        Task BroadcastPrescriptionCreatedAsync(PrescriptionDto donThuoc);

        /// <summary>Khi cập nhật trạng thái đơn thuốc.</summary>
        Task BroadcastPrescriptionStatusUpdatedAsync(PrescriptionDto donThuoc);
        // =====================================
        // ===== LƯỢT KHÁM (VISIT / HISTORY) ===
        // =====================================

        /// <summary>Tạo mới Lượt khám (LuotKham) – dùng trong HistoryService.</summary>
        Task BroadcastVisitCreatedAsync(HistoryVisitRecordDto luotKham);

        /// <summary>Cập nhật trạng thái / thông tin Lượt khám.</summary>
        Task BroadcastVisitStatusUpdatedAsync(HistoryVisitRecordDto luotKham);


        // ==========================
        // ===== HÀNG ĐỢI (QUEUE) ===
        // ==========================

        Task BroadcastQueueByRoomAsync(
            string maPhong,
            IReadOnlyList<QueueItemDto> items);

        Task BroadcastQueueItemChangedAsync(QueueItemDto item);


        // ==========================
        // ===== LỊCH HẸN ==========
        // ==========================

        Task BroadcastAppointmentChangedAsync(
            AppointmentReadRequestDto lichHen);


        // ===================================
        // ===== HOÁ ĐƠN / THANH TOÁN =======
        // ===================================

        Task BroadcastInvoiceChangedAsync(InvoiceDto hoaDon);


        // ==========================
        // ===== THÔNG BÁO =========
        // ==========================

        /// <summary>Bắn realtime khi tạo thông báo mới.</summary>
        Task BroadcastNotificationCreatedAsync(NotificationDto thongBao);

        /// <summary>Bắn realtime khi cập nhật trạng thái / nội dung thông báo.</summary>
        Task BroadcastNotificationUpdatedAsync(NotificationDto thongBao);
    }
}
