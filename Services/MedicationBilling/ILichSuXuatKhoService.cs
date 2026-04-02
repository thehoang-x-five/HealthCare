using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.MedicationBilling
{
    public interface ILichSuXuatKhoService
    {
        Task GhiLogXuatKhoAsync(string maThuoc, int soLuong, string lyDo, string? nguoiThucHien = null);
        Task GhiLogNhapKhoAsync(string maThuoc, int soLuong, string lyDo, string? nguoiThucHien = null);
        Task<List<LichSuXuatKhoDto>> LayLichSuTheoThuocAsync(string maThuoc, DateTime? fromDate = null, DateTime? toDate = null);
        Task<PagedResult<LichSuXuatKhoDto>> LayLichSuXuatKhoAsync(
            string? maThuoc = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? loaiGiaoDich = null,
            int page = 1,
            int pageSize = 50);
    }
}
