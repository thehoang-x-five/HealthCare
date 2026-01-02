using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.MedicationBilling
{
    /// <summary>
    /// Medication Billing Service Module - Quản lý kho thuốc, đơn thuốc, hóa đơn
    /// Tables: KhoThuocs, DonThuocs, ChiTietDonThuocs, HoaDonThanhToans
    /// </summary>
    
    public interface IPharmacyService
    {
        // ===== KHO THUỐC =====
        Task<DrugDto> TaoHoacCapNhatThuocAsync(DrugDto dto);
        Task<IReadOnlyList<DrugDto>> LayDanhSachThuocAsync();
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
            string? keyword,
            int page,
            int pageSize);
    }

    public interface IBillingService
    {
        // ===== HÓA ĐƠN =====
        Task<InvoiceDto> TaoHoaDonAsync(InvoiceCreateRequest request);
        Task<InvoiceDto?> LayHoaDonAsync(string maHoaDon);
        Task<PagedResult<InvoiceHistoryRecordDto>> TimKiemHoaDonAsync(InvoiceSearchFilter filter);
        Task<InvoiceDto?> CapNhatTrangThaiHoaDonAsync(
            string maHoaDon, 
            InvoiceStatusUpdateRequest request);
    }
}
