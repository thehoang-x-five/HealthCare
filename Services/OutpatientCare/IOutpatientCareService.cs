using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.OutpatientCare
{
    /// <summary>
    /// Outpatient Care Service Module - Quản lý khám lâm sàng, CLS, hàng đợi, lượt khám
    /// Tables: PhieuKhamLamSangs, PhieuKhamCanLamSangs, ChiTietDichVus, KetQuaDichVus, HangDois, LuotKhamBenhs
    /// </summary>
    
    public interface IClinicalService
    {
        // ===== PHIẾU KHÁM LÂM SÀNG =====
        Task<ClinicalExamDto> TaoPhieuKhamAsync(ClinicalExamCreateRequest request);
        Task<ClinicalExamDto?> LayPhieuKhamAsync(string maPhieuKham);
        Task<ClinicalExamDto?> CapNhatTrangThaiPhieuKhamAsync(
            string maPhieuKham, 
            ClinicalExamStatusUpdateRequest request);
        Task<PagedResult<ClinicalExamDto>> TimKiemPhieuKhamAsync(
            string? maBenhNhan,
            string? maBacSi,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize);

        // ===== CHẨN ĐOÁN CUỐI =====
        Task<FinalDiagnosisDto> TaoChanDoanCuoiAsync(FinalDiagnosisCreateRequest request);
        Task<FinalDiagnosisDto?> LayChanDoanCuoiAsync(string maPhieuKham);
        
        // ===== HOÀN TẤT PHIẾU KHÁM =====
        Task<ClinicalExamDto> CompleteExamAsync(string maPhieuKham, CompleteExamRequest request);
    }

    public interface IClsService
    {
        // ===== PHIẾU CLS =====
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

        // ===== CHI TIẾT DỊCH VỤ CLS =====
        Task<IReadOnlyList<ClsItemDto>> LayDanhSachDichVuClsAsync(string maPhieuKhamCls);
        Task<ClsItemDto> TaoChiTietDichVuAsync(ClsItemCreateRequest request);

        // ===== KẾT QUẢ CLS =====
        Task<ClsResultDto> TaoKetQuaClsAsync(ClsResultCreateRequest request);
        Task<IReadOnlyList<ClsResultDto>> LayKetQuaTheoPhieuClsAsync(string maPhieuKhamCls);

        // ===== PHIẾU TỔNG HỢP KẾT QUẢ =====
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

    public interface IQueueService
    {
        // ===== HÀNG ĐỢI =====
        Task<QueueItemDto> ThemVaoHangDoiAsync(QueueEnqueueRequest request);
        Task<QueueItemDto?> LayHangDoiAsync(string maHangDoi);
        Task<IReadOnlyList<QueueItemDto>> LayHangDoiTheoPhongAsync(
            string maPhong, 
            string? loaiHangDoi = null, 
            string? trangThai = null);
        Task<QueueItemDto?> CapNhatTrangThaiHangDoiAsync(
            string maHangDoi, 
            QueueStatusUpdateRequest request);
        Task<QueueItemDto?> CapNhatThongTinHangDoiAsync(
            string maHangDoi, 
            QueueEnqueueRequest request);
        Task<QueueItemDto?> LayTiepTheoTrongPhongAsync(
            string maPhong, 
            string? loaiHangDoi = null);
        Task<PagedResult<QueueItemDto>> TimKiemHangDoiAsync(QueueSearchFilter filter);
        int TinhDoUuTien(QueueEnqueueRequest request);
    }

    public interface IHistoryService
    {
        // ===== LƯỢT KHÁM =====
        Task<PagedResult<HistoryVisitRecordDto>> LayLichSuAsync(HistoryFilterRequest filter);
        Task<HistoryVisitDetailDto?> LayChiTietLichSuKhamAsync(string maLuotKham);
        Task<HistoryVisitRecordDto> TaoLuotKhamAsync(HistoryVisitCreateRequest request);
        Task<HistoryVisitRecordDto?> CapNhatTrangThaiLuotKhamAsync(
            string maLuotKham,
            HistoryVisitStatusUpdateRequest request);
    }
}
