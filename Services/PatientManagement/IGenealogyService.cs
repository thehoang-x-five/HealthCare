using HealthCare.DTOs;

namespace HealthCare.Services.PatientManagement
{
    /// <summary>
    /// Genealogy Service — Quản lý pha hệ di truyền bệnh nhân
    /// Sử dụng SQL Recursive CTE để truy vấn đa đời
    /// </summary>
    public interface IGenealogyService
    {
        /// <summary>
        /// Lấy cây pha hệ đa đời của bệnh nhân (lên tổ tiên + xuống con cháu)
        /// </summary>
        Task<GenealogyTreeDto> GetGenealogyTreeAsync(string maBenhNhan);

        /// <summary>
        /// Liên kết cha/mẹ cho bệnh nhân
        /// </summary>
        Task<GenealogyNodeDto> LinkParentsAsync(string maBenhNhan, LinkParentsRequest request);

        /// <summary>
        /// Lấy tiền sử bệnh của toàn bộ gia phả
        /// </summary>
        Task<FamilyDiseaseSummaryDto> GetFamilyDiseasesAsync(string maBenhNhan);
    }
}
