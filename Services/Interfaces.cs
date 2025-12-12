using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services
{
    public interface IHistoryService
    {
        Task<PagedResult<HistoryVisitRecordDto>> LayLichSuAsync(HistoryFilterRequest filter);
        Task<HistoryVisitDetailDto?> LayChiTietLichSuKhamAsync(string maLuotKham);
        // MỚI: Tạo lượt khám
        Task<HistoryVisitRecordDto> TaoLuotKhamAsync(HistoryVisitCreateRequest request);

        // MỚI: Cập nhật trạng thái lượt khám
        Task<HistoryVisitRecordDto?> CapNhatTrangThaiLuotKhamAsync(
            string maLuotKham,
            HistoryVisitStatusUpdateRequest request);
    }

    public interface INotificationService
    {
      
        Task<NotificationDto> TaoThongBaoAsync(NotificationCreateRequest request);

    
        Task<PagedResult<NotificationDto>> LayThongBaoNguoiNhanAsync(NotificationFilterRequest filter);


        Task<NotificationDto?> DanhDauDaDocAsync(long maTbNguoiNhan);

        Task<PagedResult<NotificationDto>> TimKiemThongBaoAsync(NotificationSearchFilter filter);


        Task<NotificationDto?> CapNhatTrangThaiThongBaoAsync(
            string maThongBao,
            NotificationStatusUpdateRequest request);
    }

    public interface IClinicalService
    {
        Task<ClinicalExamDto> TaoPhieuKhamAsync(ClinicalExamCreateRequest request);
        Task<ClinicalExamDto?> LayPhieuKhamAsync(string maPhieuKham);
        Task<ClinicalExamDto?> CapNhatTrangThaiPhieuKhamAsync(string maPhieuKham, ClinicalExamStatusUpdateRequest request);
        Task<FinalDiagnosisDto> TaoChanDoanCuoiAsync(FinalDiagnosisCreateRequest request);
        Task<FinalDiagnosisDto?> LayChanDoanCuoiAsync(string maPhieuKham);

        Task<PagedResult<ClinicalExamDto>> TimKiemPhieuKhamAsync(
            string? maBenhNhan,
            string? maBacSi,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize);
    }

    public interface IClsService
    {
       
        Task<ClsOrderDto> TaoPhieuClsAsync(ClsOrderCreateRequest request);
        Task<ClsOrderDto?> LayPhieuClsAsync(string maPhieuKhamCls);
        Task<ClsOrderDto?> CapNhatTrangThaiPhieuClsAsync(string maPhieuKhamCls, string trangThai);

        Task<PagedResult<ClsOrderDto>> TimKiemPhieuClsAsync(
            string? maBenhNhan,
            string? maBacSi,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize);

  
        Task<IReadOnlyList<ClsItemDto>> LayDanhSachDichVuClsAsync(string maPhieuKhamCls);
        Task<ClsItemDto> TaoChiTietDichVuAsync(ClsItemCreateRequest request);
        Task<ClsResultDto> TaoKetQuaClsAsync(ClsResultCreateRequest request);
        Task<IReadOnlyList<ClsResultDto>> LayKetQuaTheoPhieuClsAsync(string maPhieuKhamCls);

       
        Task<ClsSummaryDto> TaoTongHopAsync(string maPhieuKhamCls);
        Task<PagedResult<ClsSummaryDto>> LayTongHopKetQuaChoLapPhieuKhamAsync(ClsSummaryFilter filter);
        Task<ClsSummaryDto?> LayPhieuTongHopKetQuaAsync(string maPhieuTongHop);

        Task<ClsSummaryDto?> CapNhatTrangThaiTongHopAsync(
            string maPhieuTongHop,
            ClsSummaryStatusUpdateRequest request);

        Task<ClsSummaryDto?> CapNhatPhieuTongHopAsync(
            string maPhieuTongHop,
            ClsSummaryUpdateRequest request);
    }
    public interface IDashboardService
    {
        // ===== Trang "Tổng quan" (dashboard hôm nay) =====
        Task<DashboardTodayDto> LayDashboardHomNayAsync();
    }
    public interface IReportService
    {

        // ===== Trang "Báo cáo" (hình anh vừa gửi) =====
        // Dùng cho cả:
        //  - Tab "Xem biểu đồ" (Biểu đồ tổng quan)
        //  - Tab "Xem bảng số liệu" (Bảng dữ liệu theo ngày)
        Task<ReportOverviewDto> LayBaoCaoTongQuanAsync(ReportFilterRequest filter);
    }

    public interface IAppointmentService
    {
        Task<PagedResult<AppointmentReadRequestDto>> TimKiemLichHenAsync(AppointmentFilterRequest filter);
        Task<AppointmentReadRequestDto> TaoLichHenAsync(AppointmentCreateRequestDto request);
        Task<AppointmentReadRequestDto> LayLichHenAsync(string maLichHen);
        Task<AppointmentReadRequestDto?> CapNhatTrangThaiLichHenAsync(string maLichHen, AppointmentStatusUpdateRequest request);
        Task<AppointmentReadRequestDto?> CapNhatLichHenAsync(string maLichHen, AppointmentUpdateRequest request);
    }

    public interface IBillingService
    {

        Task<InvoiceDto> TaoHoaDonAsync(InvoiceCreateRequest request);
        Task<InvoiceDto?> LayHoaDonAsync(string maHoaDon);
        Task<PagedResult<InvoiceHistoryRecordDto>> TimKiemHoaDonAsync(InvoiceSearchFilter filter);
        Task<InvoiceDto?> CapNhatTrangThaiHoaDonAsync(string maHoaDon, InvoiceStatusUpdateRequest request);
    }

    public interface IMasterDataService
    {
        // KHOA – bắt buộc truyền ngày + giờ
        Task<IReadOnlyList<DepartmentOverviewDto>> LayTongQuanKhoaAsync(
            DateTime? ngay,
            TimeSpan? gio, string? MaDV);

        // NHÂN SỰ (BS) – bắt buộc truyền khoa + ngày + giờ
        Task<IReadOnlyList<StaffOverviewDto>> LayTongQuanNhanSuAsync(
            string maKhoa,
            DateTime? ngay,
            TimeSpan? gio);

        // DỊCH VỤ – bắt buộc truyền phòng
        Task<IReadOnlyList<ServiceOverviewDto>> LayTongQuanDichVuAsync(
            string? maPhong,string? loaiDichVu);

        // Phòng – lịch điều dưỡng tuần
        Task<RoomDutyWeekDto?> LayLichDieuDuongPhongTuanAsync(string maPhong, DateTime? today = null);

        // Nhân sự – card / detail / duty-week
        Task<PagedResult<StaffCardDto>> TimKiemNhanSuCardAsync(StaffSearchFilter filter);
        Task<StaffDetailDto?> LayChiTietNhanSuAsync(string maNhanVien);
        Task<StaffDutyWeekDto?> LayLichTrucNhanSuTuanAsync(string maNhanVien, DateTime? today = null);
        // --- KHOA ---
        Task<IReadOnlyList<DepartmentDto>> LayDanhSachKhoaAsync();
        Task<PagedResult<DepartmentDto>> TimKiemKhoaAsync(DepartmentSearchFilter filter);

        // --- PHÒNG ---
        Task<IReadOnlyList<RoomDto>> LayDanhSachPhongAsync(string? maKhoa = null, string? loaiPhong = null);
        Task<PagedResult<RoomDto>> TimKiemPhongAsync(RoomSearchFilter filter);
        Task<PagedResult<RoomCardDto>> TimKiemPhongCardAsync(RoomSearchFilter filter);
        Task<RoomDetailDto?> LayChiTietPhongAsync(string maPhong);

        // --- NHÂN SỰ ---
        Task<IReadOnlyList<StaffDto>> LayDanhSachNhanSuAsync(string? maKhoa = null, string? vaiTro = null);
        Task<PagedResult<StaffDto>> TimKiemNhanSuAsync(StaffSearchFilter filter);

        // --- LỊCH TRỰC ---
        Task<IReadOnlyList<DutyScheduleDto>> LayDanhSachLichTrucAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? maPhong = null,
            string? maYTaTruc = null);

        Task<PagedResult<DutyScheduleDto>> TimKiemLichTrucAsync(DutyScheduleSearchFilter filter);
        Task<IReadOnlyList<DutyScheduleDto>> LayLichTrucBacSiAsync(
          string maBacSi,
          DateTime? fromDate = null,
          DateTime? toDate = null);
        // --- DỊCH VỤ Y TẾ ---
        Task<IReadOnlyList<ServiceDto>> LayDanhSachDichVuAsync(
            string? maKhoa = null,
            string? maPhong = null,
            string? loaiDichVu = null,
            string? trangThai = null);

        Task<PagedResult<ServiceDto>> TimKiemDichVuAsync(ServiceSearchFilter filter);
              Task<ServiceDetailInfoDto?> LayThongTinDichVuAsync(string maDichVu);
    }

    public interface IPatientService
    {
        // CRUD chính cho bệnh nhân
        Task<PatientUpsertResultDto> TaoHoacCapNhatBenhNhanAsync(PatientCreateUpdateRequest request);

        /// <summary>
        /// Lấy chi tiết bệnh nhân cho màn xem (PatientModal),
        /// có kèm lịch sử khám + lịch sử giao dịch.
        /// Map: GET /patients/{id}
        /// </summary>
        Task<PatientDetailDto?> LayBenhNhanAsync(string maBenhNhan);

        Task<PagedResult<PatientDto>> TimKiemBenhNhanAsync(PatientSearchFilter filter);

        Task<PatientDetailDto?> CapNhatTrangThaiBenhNhanAsync(
            string maBenhNhan,
            PatientStatusUpdateRequest request);

        // Hai hàm dưới nếu muốn tách endpoint rõ ràng:
        // GET /patients/{id}/visits
        Task<IReadOnlyList<PatientVisitSummaryDto>> LayLichSuKhamBenhNhanAsync(
            string maBenhNhan);

        // GET /patients/{id}/transactions
        Task<IReadOnlyList<PatientTransactionSummaryDto>> LayLichSuGiaoDichBenhNhanAsync(
            string maBenhNhan);
    }


    public interface IPharmacyService
    {
        // ===== KHO THUỐC =====
        Task<DrugDto> TaoHoacCapNhatThuocAsync(DrugDto dto);

        /// <summary>
        /// Lấy toàn bộ danh sách thuốc (không phân trang) – dùng cho dropdown, autocomplete.
        /// </summary>
        Task<IReadOnlyList<DrugDto>> LayDanhSachThuocAsync();

        /// <summary>
        /// Tìm kiếm kho thuốc với filter + sort + paging – dùng cho màn stock.
        /// </summary>
        Task<PagedResult<DrugDto>> TimKiemThuocAsync(DrugSearchFilter filter);

        // ===== ĐƠN THUỐC =====
        Task<PrescriptionDto> TaoDonThuocAsync(PrescriptionCreateRequest request);
        Task<PrescriptionDto?> LayDonThuocAsync(string maDonThuoc);
        Task<PrescriptionDto?> CapNhatTrangThaiDonThuocAsync(
            string maDonThuoc,
            PrescriptionStatusUpdateRequest request);

        Task<PagedResult<PrescriptionDto>> TimKiemDonThuocAsync(
            string? maBenhNhan,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize);
    }

    public interface IQueueService
    {
        Task<QueueItemDto> ThemVaoHangDoiAsync(QueueEnqueueRequest request);
        Task<QueueItemDto?> LayHangDoiAsync(string maHangDoi);
        Task<IReadOnlyList<QueueItemDto>> LayHangDoiTheoPhongAsync(string maPhong, string? loaiHangDoi = null, string? trangThai = null);
        Task<QueueItemDto?> CapNhatTrangThaiHangDoiAsync(string maHangDoi, QueueStatusUpdateRequest request);
        Task<QueueItemDto?> CapNhatThongTinHangDoiAsync(string maHangDoi, QueueEnqueueRequest request);

        Task<QueueItemDto?> LayTiepTheoTrongPhongAsync(string maPhong, string? loaiHangDoi = null);
        Task<PagedResult<QueueItemDto>> TimKiemHangDoiAsync(QueueSearchFilter filter);
        int TinhDoUuTien(QueueEnqueueRequest request);
    }
    public interface IAuthService
    {
        Task<AuthTokenResponse> LoginAsync(AuthLoginRequest request, string? ipAddress);
        Task<AuthTokenResponse> RefreshAsync(string refreshToken, string? ipAddress, string? currentUserId = null);
        Task LogoutAsync(string userId, string? refreshToken, string? ipAddress);

        Task ForgotPasswordAsync(AuthForgotPasswordRequest request);
        Task ChangePasswordAsync(string userId, AuthChangePasswordRequest request, string? ipAddress = null);

    }
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
    }
}
