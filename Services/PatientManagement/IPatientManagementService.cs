using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.PatientManagement
{
    /// <summary>
    /// Patient Management Service Module - Quản lý bệnh nhân và lịch hẹn
    /// Tables: BenhNhans, LichHenKhams
    /// </summary>
    
    public interface IPatientService
    {
        // ===== BỆNH NHÂN =====
        Task<PatientUpsertResultDto> TaoHoacCapNhatBenhNhanAsync(PatientCreateUpdateRequest request);
        Task<PatientDetailDto?> LayBenhNhanAsync(string maBenhNhan);
        Task<PagedResult<PatientDto>> TimKiemBenhNhanAsync(PatientSearchFilter filter);
        Task<PatientDetailDto?> CapNhatTrangThaiBenhNhanAsync(
            string maBenhNhan,
            PatientStatusUpdateRequest request);

        // ===== LỊCH SỬ =====
        Task<IReadOnlyList<PatientVisitSummaryDto>> LayLichSuKhamBenhNhanAsync(string maBenhNhan);
        Task<IReadOnlyList<PatientTransactionSummaryDto>> LayLichSuGiaoDichBenhNhanAsync(string maBenhNhan);
    }

    public interface IAppointmentService
    {
        // ===== LỊCH HẸN =====
        Task<PagedResult<AppointmentReadRequestDto>> TimKiemLichHenAsync(AppointmentFilterRequest filter);
        Task<AppointmentReadRequestDto> TaoLichHenAsync(AppointmentCreateRequestDto request);
        Task<AppointmentReadRequestDto> LayLichHenAsync(string maLichHen);
        Task<AppointmentReadRequestDto?> CapNhatTrangThaiLichHenAsync(
            string maLichHen, 
            AppointmentStatusUpdateRequest request);
        Task<AppointmentReadRequestDto?> CapNhatLichHenAsync(
            string maLichHen, 
            AppointmentUpdateRequest request);
    }
}
