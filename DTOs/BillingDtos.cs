using System;

namespace HealthCare.DTOs
{
    /// <summary>
    /// 1 dòng lịch sử giao dịch (tab "Giao dịch" trong Billing).
    /// Map cho FE: bảng history transactions / Billing list.
    /// </summary>
    public record class InvoiceHistoryRecordDto
    {
        /// <summary>hoa_don_thanh_toan.ma_hoa_don (FE: row.invoiceId).</summary>
        public string MaHoaDon { get; set;} = default!;

        /// <summary>Thời điểm thu tiền (FE: row.date).</summary>
        public DateTime ThoiGian { get; set;}

        /// <summary>Mã & tên BN (FE: id, name).</summary>
        public string MaBenhNhan { get; set;} = default!;
        public string TenBenhNhan { get; set;} = string.Empty;

        /// <summary>Nhân sự thu tiền.</summary>
        public string MaNhanSuThu { get; set;} = default!;
        public string TenNhanSuThu { get; set;} = string.Empty;

        /// <summary>kham_lam_sang, can_lam_sang, thuoc...</summary>
        public string LoaiDotThu { get; set;} = default!;

        /// <summary>Tổng số tiền giao dịch.</summary>
        public decimal SoTien { get; set;}

        /// <summary>Tiền thuốc (nếu hoá đơn có gắn đơn thuốc).</summary>
        public decimal? TienThuoc { get; set;}

        /// <summary>da_thu, da_huy...</summary>
        public string TrangThai { get; set;} = "da_thu";

        /// <summary>tien_mat, chuyen_khoan, pos...</summary>
        public string PhuongThucThanhToan { get; set;} = "tien_mat";

        /// <summary>Nội dung hiển thị list (FE: content).</summary>
        public string NoiDung { get; set;} = string.Empty;

        // Khoá liên kết sang chứng từ nguồn
        public string? MaPhieuKham { get; set;}
        public string? MaPhieuKhamCls { get; set;}
        public string? MaDonThuoc { get; set;}
    }

    /// <summary>
    /// DTO chi tiết hoá đơn – dùng cho xem chi tiết / in.
    /// Map trực tiếp từ HoaDonThanhToan + join BenhNhan, NhanVienYTe, DonThuoc.
    /// </summary>
    public record class InvoiceDto
    {
        public string MaHoaDon { get; set;} = default!;

        public string MaBenhNhan { get; set;} = default!;
        public string TenBenhNhan { get; set;} = string.Empty;

        public string MaNhanSuThu { get; set;} = default!;
        public string TenNhanSuThu { get; set;} = string.Empty;

        public string? MaPhieuKhamCls { get; set;}
        public string? MaPhieuKham { get; set;}
        public string? MaDonThuoc { get; set;}

        /// <summary>kham_lam_sang, can_lam_sang, thuoc...</summary>
        public string LoaiDotThu { get; set;} = default!;

        /// <summary>Tổng số tiền hoá đơn.</summary>
        public decimal SoTien { get; set;}

        /// <summary>Tiền thuốc (nếu có).</summary>
        public decimal? TienThuoc { get; set;}

        /// <summary>Thời điểm thu tiền.</summary>
        public DateTime ThoiGian { get; set;}

        /// <summary>da_thu, da_huy...</summary>
        public string TrangThai { get; set;} = "da_thu";

        /// <summary>Nội dung chi tiết hoá đơn.</summary>
        public string NoiDung { get; set;} = default!;

        /// <summary>tien_mat, chuyen_khoan, pos...</summary>
        public string PhuongThucThanhToan { get; set;} = "tien_mat";
    }

    /// <summary>Request tạo hoá đơn mới.</summary>
    public record class InvoiceCreateRequest
    {
        public string MaBenhNhan { get; set;} = default!;
        public string MaNhanSuThu { get; set;} = default!;

        /// <summary>kham_lam_sang, can_lam_sang, thuoc...</summary>
        public string LoaiDotThu { get; set;} = default!; // BE validate

        public decimal SoTien { get; set;}

        /// <summary>tien_mat, chuyen_khoan, pos...</summary>
        public string PhuongThucThanhToan { get; set;} = "tien_mat";

        public string? MaPhieuKhamCls { get; set;}
        public string? MaPhieuKham { get; set;}
        public string? MaDonThuoc { get; set;}

        /// <summary>Nội dung chi tiết hoá đơn.</summary>
        public string NoiDung { get; set;} = default!;
    }

    /// <summary>Cập nhật trạng thái hoá đơn (da_thu, da_huy...).</summary>
    public record class InvoiceStatusUpdateRequest
    {
        public string TrangThai { get; set;} = default!;
    }

    /// <summary>
    /// Filter tìm kiếm lịch sử giao dịch / danh sách hoá đơn.
    /// Dùng cho IBillingService.TimKiemHoaDonAsync.
    /// </summary>
    public record class InvoiceSearchFilter
    {
        /// <summary>Mã BN – null = toàn viện.</summary>
        public string? MaBenhNhan { get; set;}

        /// <summary>Khoảng thời gian thu tiền.</summary>
        public DateTime? FromTime { get; set;}
        public DateTime? ToTime { get; set;}

        /// <summary>kham_lam_sang, can_lam_sang, thuoc... hoặc null = tất cả.</summary>
        public string? LoaiDotThu { get; set;}

        /// <summary>da_thu, da_huy... hoặc null = tất cả.</summary>
        public string? TrangThai { get; set;}

        /// <summary>tien_mat, chuyen_khoan, pos... hoặc null = tất cả.</summary>
        public string? PhuongThucThanhToan { get; set;}

        /// <summary>
        /// Từ khoá toàn văn (mã HĐ, mã BN, tên BN, nội dung…).
        /// </summary>
        public string? Keyword { get; set;}

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }
}
