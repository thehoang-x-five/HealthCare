using System;
using System.Collections.Generic;

namespace HealthCare.DTOs
{
    // ===================== KHO THUỐC (STOCK) =====================

    public record class DrugDto
    {

        public string MaThuoc { get; set; } = default!;

        
        public string TenThuoc { get; set; } = default!;

      
        public string DonViTinh { get; set; } = default!;

     
        public string? CongDung { get; set; }

        public decimal GiaNiemYet { get; set; }

    
        public int SoLuongTon { get; set; }


        public DateTime HanSuDung { get; set; }


        public string? SoLo { get; set; }

        public string TrangThai { get; set; } = "hoat_dong";
    }


    public record class DrugSearchFilter
    {
        public string? Keyword { get; set; }      // Tên, mã, số lô...
        public string? TrangThai { get; set; }    // hoat_dong, het_han, tam_ngung...

        public DateTime? HanSuDungFrom { get; set; }
        public DateTime? HanSuDungTo { get; set; }

        public int? TonToiThieu { get; set; }
        public int? TonToiDa { get; set; }

        /// <summary>
        /// "TenThuoc", "SoLuongTon", "HanSuDung"...
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// "asc" hoặc "desc". Mặc định "asc".
        /// </summary>
        public string? SortDirection { get; set; } = "asc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 500;
    }

    // ===================== ĐƠN THUỐC (ORDERS) =====================


    public record class PrescriptionItemDto
    {
        public string MaThuoc { get; set; } = default!;   // để BE xử lý
        public string TenThuoc { get; set; } = default!;  

        public string? ChiDinhSuDung { get; set; }        

        public int SoLuong { get; set; }                  

       
        public decimal DonGia { get; set; }

        /// <summary>Thành tiền của dòng (SoLuong * DonGia)</summary>
        public decimal ThanhTien { get; set; }
    }


    public record class PrescriptionDto
    {
       
        public string MaDonThuoc { get; set; } = default!;

        public string MaBenhNhan { get; set; } = default!;

        
        public string TenBenhNhan { get; set; } = default!;

        public string MaBacSiKeDon { get; set; } = default!;
        public string TenBacSiKeDon { get; set; } = default!;

  
        public string? MaPhieuChanDoanCuoi { get; set; }

        public string? ChanDoan { get; set; }

      
        public DateTime ThoiGianKeDon { get; set; }

        /// <summary>
        /// Trạng thái domain: da_ke, cho_phat, da_phat...

        public string TrangThai { get; set; } = default!;

        public decimal TongTienDon { get; set; }

 
        public IReadOnlyList<PrescriptionItemDto> ChiTiet { get; set; }
            = Array.Empty<PrescriptionItemDto>();
    }

    // ---------- Request tạo đơn (từ khám LS / nơi khác) ----------

    public record class PrescriptionItemCreateRequest
    {
        public string MaThuoc { get; set; } = default!;
        public int SoLuong { get; set; }
        public string? ChiDinhSuDung { get; set; }

        /// <summary>
        /// Thành tiền dòng; nếu FE không gửi, BE có thể tự tính từ bảng giá.
        /// </summary>
        public decimal ThanhTien { get; set; }
    }

    /// <summary>
    /// Request tạo đơn thuốc – dùng cho API TaoDonThuocAsync.
    /// Khớp với RxPickerModal: rows = [{code,name,unit,price,dose,qty,usage}].
    /// </summary>
    public record class PrescriptionCreateRequest
    {
        public string MaBenhNhan { get; set; } = default!;
        public string MaBacSiKeDon { get; set; } = default!;


        public string? MaPhieuChanDoanCuoi { get; set; }

        /// <summary>
        /// Tổng tiền đơn; BE có thể ignore và tự sum từ Items nếu muốn.
        /// </summary>
        public decimal TongTienDon { get; set; }


        public IReadOnlyList<PrescriptionItemCreateRequest> Items { get; set; }
            = Array.Empty<PrescriptionItemCreateRequest>();
    }

    /// <summary>
    /// Cập nhật trạng thái đơn: da_ke, cho_phat, da_phat...
    /// </summary>
    public record class PrescriptionStatusUpdateRequest
    {
        public string TrangThai { get; set; } = default!;
    }

}
