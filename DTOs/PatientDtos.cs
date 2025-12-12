using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthCare.DTOs
{
    /// <summary>
    /// Dòng danh sách bệnh nhân (Patients page)
    /// </summary>
    public record class PatientDto
    {
        public string MaBenhNhan { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "";
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public string? TrangThaiTaiKhoan { get; set; }

        /// <summary>Trạng thái khám trong ngày (chờ tiếp nhận, chờ khám, đã khám...)</summary>
        public string? TrangThaiHomNay { get; set; }

        /// <summary>Ngày áp dụng trạng thái hôm nay</summary>
        public DateTime NgayTrangThai { get; set; }
    }

    /// <summary>
    /// Item lịch sử khám rút gọn cho PatientModal (tab "Lịch sử khám").
    /// Map sang FE: visits[i] -> v.date, v.dept, v.doctor, v.note, v.type, v.by
    /// </summary>
    public record class PatientVisitSummaryDto
    {
        /// <summary>Thời gian khám (v.date)</summary>
        public DateTime Date { get; set; }

        /// <summary>Khoa/Phòng (v.dept)</summary>
        public string Dept { get; set; } = string.Empty;

        /// <summary>Bác sĩ chính (v.doctor)</summary>
        public string Doctor { get; set; } = string.Empty;

        /// <summary>Tóm tắt / ghi chú (v.note)</summary>
        public string? Note { get; set; }

        /// <summary>Loại lượt: khám thường / dịch vụ... (v.type)</summary>
        public string? Type { get; set; }

        /// <summary>Người cập nhật / tạo (v.by)</summary>
        public string? By { get; set; }

        /// <summary>Ref dùng để điều hướng (nếu cần) (không bắt buộc FE dùng)</summary>
        public string? Ref { get; set; }

        // ===== Khoá nghiệp vụ thêm (không bắt buộc FE dùng) =====
        public string? MaLuotKham { get; set; }
        public string? MaPhieuKham { get; set; }
    }

    /// <summary>
    /// Item lịch sử giao dịch rút gọn cho PatientModal (tab "Giao dịch").
    /// Map sang FE: transactions[i] -> t.date, t.item, t.amount, t.status, t.ref
    /// </summary>
    public record class PatientTransactionSummaryDto
    {
        /// <summary>Thời điểm giao dịch (t.date)</summary>
        public DateTime Date { get; set; }

        /// <summary>Nội dung hiển thị (t.item)</summary>
        public string Item { get; set; } = string.Empty;

        /// <summary>Số tiền giao dịch (t.amount)</summary>
        public decimal Amount { get; set; }

        /// <summary>Trạng thái (t.status)</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Ref dùng cho key / điều hướng (t.ref)</summary>
        public string? Ref { get; set; }

        // ===== Thông tin invoice chi tiết hơn (dùng nội bộ BE / màn Billing) =====
        public string? MaHoaDon { get; set; }
        public string? LoaiDotThu { get; set; }          // kham_lam_sang | cls | thuoc...
        public string? PhuongThucThanhToan { get; set; } // tien_mat | chuyen_khoan...
    }

    /// <summary>
    /// DTO chi tiết bệnh nhân cho PatientModal (View tab).
    /// Có luôn 2 list: Lịch sử khám + Lịch sử giao dịch.
    /// </summary>
    public record class PatientDetailDto
    {
        // ----- Thông tin cơ bản -----

        public string MaBenhNhan { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "";
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }

        // ----- Trạng thái tài khoản & trong ngày -----
        public string TrangThaiTaiKhoan { get; set; } = "hoat_dong";
        public string? TrangThaiHomNay { get; set; }
        public DateTime NgayTrangThai { get; set; }

        // ----- Thông tin bệnh án (Medical History) -----
        public string? DiUng { get; set; }
        public string? ChongChiDinh { get; set; }
        public string? ThuocDangDung { get; set; }
        public string? TieuSuBenh { get; set; }
        public string? TienSuPhauThuat { get; set; }
        public string? NhomMau { get; set; }
        public string? BenhManTinh { get; set; }
        public string? SinhHieu { get; set; }

        // ----- Lịch sử khám & giao dịch cho modal chi tiết -----
        public IReadOnlyList<PatientVisitSummaryDto> LichSuKham { get; set; }
            = Array.Empty<PatientVisitSummaryDto>();

        public IReadOnlyList<PatientTransactionSummaryDto> LichSuGiaoDich { get; set; }
            = Array.Empty<PatientTransactionSummaryDto>();
    }
    public record class PatientUpsertResultDto
    {
        /// <summary>
        /// true = tạo mới, false = cập nhật.
        /// </summary>
        public bool IsNew { get; init; }

        /// <summary>
        /// Thông tin bệnh nhân cơ bản (dùng chung cho cả tạo + update).
        /// </summary>
        public PatientDto Patient { get; init; } = default!;

        /// <summary>
        /// Thông tin chi tiết — chỉ có khi là cập nhật.
        /// </summary>
        public PatientDetailDto? Detail { get; init; }
    }

    public record class PatientCreateUpdateRequest
    {
        // ----- Thông tin hành chính -----
        public string? MaBenhNhan { get; set; }
        public string HoTen { get; set; } = default!;
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "";
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public string? TrangThaiHomNay { get; set; }
        public string? TrangThaiTaiKhoan { get; set; }

        // ----- Thông tin bệnh án -----
        public string? DiUng { get; set; }
        public string? ChongChiDinh { get; set; }
        public string? ThuocDangDung { get; set; }
        public string? TieuSuBenh { get; set; }
        public string? TienSuPhauThuat { get; set; }
        public string? NhomMau { get; set; }
        public string? BenhManTinh { get; set; }
        public string? SinhHieu { get; set; }
    }

    public record class PatientSearchFilter
    {
        public string? Keyword { get; set; }
        public bool OnlyToday { get; set; } = false;
        public string? MaBenhNhan { get; set; }
        public string? DienThoai { get; set; }
        public string? GioiTinh { get; set; }
        public string? TrangThaiTaiKhoan { get; set; }
        public string? TrangThaiHomNay { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 500;

        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "asc";
    }

    public record class PatientStatusUpdateRequest
    {
        public string TrangThaiHomNay { get; set; } = default!;
    }
}
