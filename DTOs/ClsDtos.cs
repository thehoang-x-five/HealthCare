using System;
using System.Collections.Generic;

namespace HealthCare.DTOs
{
    /// <summary>
    /// Header phiếu CLS (phieu_kham_can_lam_sang).
    /// </summary>
    public record class ClsOrderDto
    {
        public string MaPhieuKhamCls { get; set;} = default!;
        public string MaPhieuKhamLs { get; set;} = default!;
        public string MaBenhNhan { get; set;} = default!;

        // Thông tin bệnh nhân
        public string HoTen { get; set;} = default!;
        public DateTime? NgaySinh { get; set;}
        public string GioiTinh { get; set;} = "";
        public string? DienThoai { get; set;}
        public string? Email { get; set;}
        public string? DiaChi { get; set;}

        /// <summary>Alias hiển thị tên bệnh nhân.</summary>
        public string TenBenhNhan { get; set;} = "";

        /// <summary>da_lap, dang_thuc_hien, da_hoan_tat...</summary>
        public string TrangThai { get; set;} = default!;

        public bool AutoPublishEnabled { get; set;}
        public string? GhiChu { get; set;}

        public DateTime? NgayLap { get; set;}
        public TimeSpan? GioLap { get; set;}

        public string MaKhoa { get; set;} = default!;
        public string? TenKhoa { get; set;}

        public string MaNguoiLap { get; set;} = default!;
        public string? TenNguoiLap { get; set;}

        // Note: room info moved to item-level `ClsItemDto`. Header does not carry room.

        /// <summary>Toàn bộ thông tin bệnh sử / tóm tắt LS cần hiển thị cùng phiếu CLS.</summary>
        public string? ThongTinChiTiet { get; set;}

        /// <summary>Danh sách dịch vụ CLS thuộc phiếu.</summary>
        public IReadOnlyList<ClsItemDto> ListItemDV { get; set;} = Array.Empty<ClsItemDto>();
    }



    /// <summary>
    /// Request tạo phiếu CLS + danh sách dịch vụ chỉ định.
    /// </summary>
    public record class ClsOrderCreateRequest
    {
        public string MaBenhNhan { get; set;} = default!;
        public string MaPhieuKhamLs { get; set;} = default!;
        public string MaNguoiLap { get; set;} = default!;
        public bool AutoPublishEnabled { get; set;} = true;
        public string? GhiChu { get; set;}
        /// <summary>TrangThai: da_lap | dang_thuc_hien | ... (null = BE tự set default).</summary>
        public string? TrangThai { get; set;}

        /// <summary>
        /// Danh sách DV CLS chỉ định.
        /// API doc mô tả “Danh sách mã DV CLS (ma_dich_vu_y_te)”,
        /// nhưng để tiện FE/BE, dùng ClsItemCreateRequest (chỉ cần MaDichVu + GhiChu).
        /// </summary>
        public IReadOnlyList<ClsItemCreateRequest> ListItemDV { get; set;} = [];
    }
   
    /// <summary>
    ///     Tạo 1 vdịch vụ lẻ thuộc phiếu CLS (chi_tiet_dich_vu).

    public record class ClsItemCreateRequest
    {
        public string MaPhieuKhamCls { get; set;} = default!;
        public string MaDichVu { get; set;} = default!;
        public string? GhiChu { get; set;}
        /// <summary>chua_co_ket_qua | da_co_ket_qua</summary>
        public string TrangThai { get; set;} = "chua_co_ket_qua";
    }
    /// <summary>
    /// Một dịch vụ lẻ thuộc phiếu CLS (chi_tiet_dich_vu + dich_vu_y_te).
    /// </summary>
    public record class ClsItemDto
    {
        public string MaChiTietDv { get; set;} = default!;
        public string MaPhieuKhamCls { get; set;} = default!;
        public string MaDichVu { get; set;} = default!;
        public string TenDichVu { get; set;} = default!;
        public string MaPhong { get; set;} = default!;
        public string? TenPhong { get; set;}
        /// <summary>Y t  tr?c ph•ng CLS th?c hi?n d?ch v? (l?y t? lich_truc).</summary>
        public string? MaYTaThucHien { get; set;}
        public string? TenYTaThucHien { get; set;}
        /// <summary>cls,...</summary>
        public string LoaiDichVu { get; set;} = default!;
        /// <summary>Phí DV — theo API doc là string (hiển thị), không phải decimal.</summary>
        public string PhiDV { get; set;} = default!;
        public string? GhiChu { get; set;}
        /// <summary>chua_co_ket_qua | da_co_ket_qua</summary>
        public string TrangThai { get; set;} = default!;
    }


    /// <summary>
    /// Kết quả CLS của một dịch vụ lẻ (ket_qua_dich_vu).
    /// ĐÃ bỏ PhienBan và ChiSoJson.
    /// </summary>
    public record class ClsResultDto
    {
        public string MaKetQua { get; set;} = default!;
        public string MaChiTietDv { get; set;} = default!;
        public string MaPhieuKhamCls { get; set;} = default!;
        public string MaDichVu { get; set;} = default!;
        public string TenDichVu { get; set;} = default!;

        /// <summary>cho_duyet | da_chot | ...</summary>
        public string TrangThaiChot { get; set;} = default!;
        public string? NoiDungKetQua { get; set;}

        public string MaNhanSuThucHien { get; set;} = default!;
        public string TenNhanSuThucHien { get; set;} = "";
        public DateTime ThoiGianTao { get; set;}

        public string? TepDinhKem { get; set;}
    }
    

    /// <summary>
    /// Request tạo/cập nhật kết quả CLS cho một DV lẻ.
    /// </summary>
    public record class ClsResultCreateRequest
    {
        public string MaChiTietDv { get; set;} = default!;
        public string TrangThaiChot { get; set;} = default!; // da_chot,...
        public string? NoiDungKetQua { get; set;}
        public string MaNhanSuThucHien { get; set;} = default!;
        public string? TepDinhKem { get; set;}
    }

    /// <summary>
    /// Phiếu tổng hợp kết quả CLS (phieu_tong_hop_ket_qua).
    /// ĐÃ bỏ PhienBanTong, CHỈ giữ SnapshotJson.
    /// </summary>
    public record class ClsSummaryDto
    {
        public string MaPhieuTongHop { get; set;} = default!;
        public string MaPhieuKhamCls { get; set;} = default!;

        public string MaBenhNhan { get; set;} = default!;
        public string TenBenhNhan { get; set;} = "";

        public DateTime NgayGioLapPhieuCls { get; set;}
        public DateTime ThoiGianXuLy { get; set;}

        /// <summary>
        /// Trạng thái tổng hợp:
        /// - dang_thuc_hien: đang thực hiện từng DV CLS (chưa đủ hết).
        /// - cho_xu_ly: đã đủ tất cả KQ DV CLS, chờ y tá lập phiếu LS.
        /// - da_hoan_tat: đã lập phiếu khám LS.
        /// </summary>
        public string TrangThai { get; set;} = default!;

        /// <summary>
        /// Snapshot toàn bộ kết quả CLS đã tổng hợp (JSON) để LS đọc 1 lần.
        /// </summary>
        public string SnapshotJson { get; set;} = default!;
    }

    /// <summary>
    /// Filter lấy danh sách phiếu tổng hợp cho màn "Lập phiếu khám LS có KQCLS".
    /// </summary>
    public record class ClsSummaryFilter
    {
        public string MaBenhNhan { get; set;} = default!;
        public DateTime? FromDate { get; set;}
        public DateTime? ToDate { get; set;}

        /// <summary>
        /// Mặc định lấy các phiếu đang chờ xử lý cho y tá.
        /// </summary>
        public string? TrangThai { get; set;} = "cho_xu_ly";

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 20;
    }

    /// <summary>
    /// Request cập nhật TRẠNG THÁI phiếu tổng hợp CLS.
    /// </summary>
    public record class ClsSummaryStatusUpdateRequest
    {
        /// <summary>
        /// dang_thuc_hien | cho_xu_ly | da_hoan_tat
        /// </summary>
        public string TrangThai { get; set;} = default!;
    }

    /// <summary>
    /// Request cập nhật NỘI DUNG phiếu tổng hợp CLS.
    /// </summary>
    public record class ClsSummaryUpdateRequest
    {
        /// <summary>
        /// dang_thuc_hien | cho_xu_ly | da_hoan_tat (null = giữ nguyên).
        /// </summary>
        public string? TrangThai { get; set;}

        /// <summary>
        /// Mã nhân sự xử lý tổng hợp (null = giữ nguyên).
        /// </summary>
        public string? MaNhanSuXuLy { get; set;}

        /// <summary>
        /// Snapshot mới (JSON) của toàn bộ kết quả CLS (null = giữ nguyên).
        /// </summary>
        public string? SnapshotJson { get; set;}

        /// <summary>
        /// Thời gian xử lý (null = BE tự init hoặc giữ nguyên).
        /// </summary>
        public DateTime? ThoiGianXuLy { get; set;}
    }
}

  

