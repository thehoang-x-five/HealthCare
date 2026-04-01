using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.MedicationBilling
{
    public interface ILichSuXuatKhoService
    {
        /// <summary>
        /// Ghi log xuất kho (trừ tồn kho).
        /// </summary>
        Task GhiLogXuatKhoAsync(string maThuoc, int soLuong, string lyDo, string? nguoiThucHien = null);

        /// <summary>
        /// Ghi log nhập kho (cộng tồn kho / hoàn trả).
        /// </summary>
        Task GhiLogNhapKhoAsync(string maThuoc, int soLuong, string lyDo, string? nguoiThucHien = null);

        /// <summary>
        /// Lấy lịch sử giao dịch kho theo thuốc.
        /// </summary>
        Task<List<LichSuXuatKhoDto>> LayLichSuTheoThuocAsync(string maThuoc, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Lấy lịch sử giao dịch kho (tất cả thuốc).
        /// </summary>
        Task<PagedResult<LichSuXuatKhoDto>> LayLichSuXuatKhoAsync(
            string? maThuoc = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? loaiGiaoDich = null,
            int page = 1,
            int pageSize = 50);
    }
}
