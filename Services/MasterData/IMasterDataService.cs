using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.MasterData
{
    /// <summary>
    /// Master Data Service Module - Quản lý danh mục khoa, phòng, nhân sự, dịch vụ
    /// Tables: Khoas, Phongs, DichVuYTes, LichTrucs, NhanVienYTes
    /// </summary>
    
    public interface IMasterDataService
    {
        // ===== TỔNG QUAN =====
        Task<IReadOnlyList<DepartmentOverviewDto>> LayTongQuanKhoaAsync(
            DateTime? ngay,
            TimeSpan? gio, 
            string? MaDV);

        Task<IReadOnlyList<StaffOverviewDto>> LayTongQuanNhanSuAsync(
            string maKhoa,
            DateTime? ngay,
            TimeSpan? gio);

        Task<IReadOnlyList<ServiceOverviewDto>> LayTongQuanDichVuAsync(
            string? maPhong,
            string? loaiDichVu);

        // ===== KHOA =====
        Task<IReadOnlyList<DepartmentDto>> LayDanhSachKhoaAsync();
        Task<PagedResult<DepartmentDto>> TimKiemKhoaAsync(DepartmentSearchFilter filter);

        // ===== PHÒNG =====
        Task<IReadOnlyList<RoomDto>> LayDanhSachPhongAsync(string? maKhoa = null, string? loaiPhong = null);
        Task<PagedResult<RoomDto>> TimKiemPhongAsync(RoomSearchFilter filter);
        Task<PagedResult<RoomCardDto>> TimKiemPhongCardAsync(RoomSearchFilter filter);
        Task<RoomDetailDto?> LayChiTietPhongAsync(string maPhong);
        Task<RoomDutyWeekDto?> LayLichDieuDuongPhongTuanAsync(string maPhong, DateTime? today = null);

        // ===== NHÂN SỰ =====
        Task<IReadOnlyList<StaffDto>> LayDanhSachNhanSuAsync(string? maKhoa = null, string? vaiTro = null);
        Task<PagedResult<StaffDto>> TimKiemNhanSuAsync(StaffSearchFilter filter);
        Task<PagedResult<StaffCardDto>> TimKiemNhanSuCardAsync(StaffSearchFilter filter);
        Task<StaffDetailDto?> LayChiTietNhanSuAsync(string maNhanVien);
        Task<StaffDutyWeekDto?> LayLichTrucNhanSuTuanAsync(string maNhanVien, DateTime? today = null);

        // ===== LỊCH TRỰC =====
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

        // ===== DỊCH VỤ Y TẾ =====
        Task<IReadOnlyList<ServiceDto>> LayDanhSachDichVuAsync(
            string? maKhoa = null,
            string? maPhong = null,
            string? loaiDichVu = null,
            string? trangThai = null);

        Task<PagedResult<ServiceDto>> TimKiemDichVuAsync(ServiceSearchFilter filter);
        Task<ServiceDetailInfoDto?> LayThongTinDichVuAsync(string maDichVu);
    }
}
