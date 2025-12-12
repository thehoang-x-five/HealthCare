using System;
using System.Collections.Generic;

namespace HealthCare.DTOs
{
    /// <summary>
    /// 1 dòng trong bảng Lịch sử khám (tab "Khám bệnh").
    /// Map cho FE: HistoryTable tab="visits".
    /// </summary>
    public record class HistoryVisitRecordDto
    {
        /// <summary>
        /// Thời điểm khám (FE: row.date).
        /// </summary>
        public DateTime ThoiGian { get; set;}

        /// <summary>Mã & tên BN (FE: id, name).</summary>
        public string MaBenhNhan { get; set;} = default!;
        public string TenBenhNhan { get; set;} = default!;

        /// <summary>Khoa/Phòng (FE: dept).</summary>
        public string? MaKhoa { get; set;}
        public string? TenKhoa { get; set;}

        /// <summary>Bác sĩ chính (FE: doctor).</summary>
        public string? MaBacSi { get; set;}
        public string? TenBacSi { get; set;}

        /// <summary>
        /// Loai luot: "kham_lam_sang" / "can_lam_sang".
        /// FE dung de hien thi chip LS/CLS.
        /// </summary>
        public string LoaiLuot { get; set;} = "kham_lam_sang";

        /// <summary>Tóm tắt hiển thị list (FE: note).</summary>
        public string? GhiChu { get; set;}

        /// <summary>True nếu là khám dịch vụ (FE: isServiceVisit).</summary>
        public bool LaKhamDichVu { get; set;}

        // ===== Khoá phục vụ điều hướng / load chi tiết =====
        public string MaLuotKham { get; set;} = default!;
        public string? MaPhieuKhamLs { get; set;}
        public string? MaPhieuKhamCls { get; set;}
        public string? MaPhieuTongHopCls { get; set;}
        public string? MaPhieuChanDoanCuoi { get; set;}
        public string? MaDonThuoc { get; set;}
    }

    /// <summary>
    /// Chi tiết 1 lần khám (HistoryDetailModal type="visit").
    /// Dùng cho popup chi tiết ở màn Lịch sử hoặc màn Hồ sơ BN.
    /// </summary>
    public record class HistoryVisitDetailDto
    {
        // ===== Thông tin chung =====
        public DateTime ThoiGian { get; set;}

        public string MaBenhNhan { get; set;} = default!;
        public string TenBenhNhan { get; set;} = default!;

        public string? MaKhoa { get; set;}
        public string? TenKhoa { get; set;}

        public string? MaBacSi { get; set;}
        public string? TenBacSi { get; set;}

        /// <summary>"kham_lam_sang" / "can_lam_sang".</summary>
        public string LoaiLuot { get; set;} = "kham_lam_sang";
        public bool LaKhamDichVu { get; set;}

        /// <summary>Tóm tắt kết quả khám (FE: row.note).</summary>
        public string? TomTatKham { get; set;}

        // ===== Khoá liên kết sang các chứng từ =====
        public string MaLuotKham { get; set;}= default!;
        public string? MaPhieuKhamLs { get; set;}
        public string? MaPhieuKhamCls { get; set;}
        public string? MaPhieuTongHopCls { get; set;}
        public string? MaPhieuChanDoanCuoi { get; set;}
        public string? MaDonThuoc { get; set;}

        // ===== Nội dung chi tiết: KQ khám + CLS + chẩn đoán =====
        public IReadOnlyList<HistoryExamRowDto> KetQuaKham { get; set;}
            = Array.Empty<HistoryExamRowDto>();

        public IReadOnlyList<HistoryServiceResultDto> KetQuaDichVu { get; set;}
            = Array.Empty<HistoryServiceResultDto>();

        public HistoryDiagnosisDto? ChanDoan { get; set;}
    }

    /// <summary>Dòng kết quả khám lâm sàng (FE: examRows[i].label/value).</summary>
    public record class HistoryExamRowDto
    {
        public string Label { get; set;} = string.Empty;
        public string Value { get; set;} = string.Empty;
    }

    /// <summary>Kết quả 1 dịch vụ CLS (FE: services[i].code/name/result/price).</summary>
    public record class HistoryServiceResultDto
    {
        /// <summary>dich_vu_y_te.ma_dich_vu (FE: s.code).</summary>
        public string MaDichVu { get; set;} = string.Empty;

        /// <summary>dich_vu_y_te.ten_dich_vu (FE: s.name).</summary>
        public string TenDichVu { get; set;} = string.Empty;

        /// <summary>ket_qua_dich_vu.noi_dung_ket_qua (FE: s.result nếu cần).</summary>
        public string? KetQua { get; set;}

        /// <summary>Đơn giá dịch vụ (FE: s.price).</summary>
        public decimal? DonGia { get; set;}
    }

    /// <summary>Chẩn đoán & kế hoạch điều trị (FE: row.diagnosis).</summary>
    public record class HistoryDiagnosisDto
    {
        public string? ChanDoanSoBo { get; set;}     // diagnosis.pre
        public string? ChanDoanXacDinh { get; set;}  // diagnosis.final
        public string? PhacDoDieuTri { get; set;}    // diagnosis.plan
        public string? TuVanDanDo { get; set;}       // diagnosis.advice
    }

    /// <summary>
    /// Filter tìm kiếm lịch sử khám (IHistoryService.LayLichSuAsync).
    /// FE map từ HistoryFilterPopover + toolbar.
    /// </summary>
    public record class HistoryFilterRequest
    {
        /// <summary>
        /// Mã BN cần xem lịch sử.
        /// null = mode "toàn viện" (tab Lịch sử tổng).
        /// </summary>
        public string? MaBenhNhan { get; set;}

        /// <summary>Khoảng thời gian (dateFrom/dateTo).</summary>
        public DateTime? FromTime { get; set;}
        public DateTime? ToTime { get; set;}

        /// <summary>
        /// Loai luot: "all" | "kham_lam_sang" | "can_lam_sang".
        /// FE: visitType.
        /// </summary>
        public string? LoaiLuot { get; set;}

        /// <summary>
        /// Từ khoá toàn văn (mã BN, tên, khoa, bác sĩ, tóm tắt…).
        /// FE: keyword.
        /// </summary>
        public string? Keyword { get; set;}

        /// <summary>
        /// true = chỉ "hôm nay" (scope = today), null/false = tất cả.
        /// </summary>
        public bool? OnlyToday { get; set;}

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    /// <summary>
    /// Request tạo mới một lượt khám (LuotKhamBenh).
    /// Thường được gọi khi BN bắt đầu được khám ở phòng.
    /// </summary>
    public class HistoryVisitCreateRequest
    {
        /// <summary>Mã hàng đợi (queue) tương ứng.</summary>
        public string MaHangDoi { get; set; } = default!;

        /// <summary>
        /// Mã nhân sự thực hiện (bác sĩ/y tá). Có thể null, backend tự suy từ context sau này.
        /// </summary>
        public string? MaNhanSuThucHien { get; set; }
        public string? MaYTaHoTro { get; set; } 
        /// <summary>Thời gian bắt đầu lượt khám. Nếu null, dùng DateTime.Now.</summary>
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        /// <summary>Ghi chú thêm (tùy chọn).</summary>
  
       
        public string? LoaiLuot { get; set; } // kham_lam_sang,can_lam_sang

     
        public string TrangThai { get; set; } = "dang_thuc_hien"; // dang_kham,hoan_tat

    }

    /// <summary>
    /// Request cập nhật trạng thái lượt khám.
    /// Ví dụ: dang_kham → da_hoan_tat, da_huy...
    /// </summary>
    public class HistoryVisitStatusUpdateRequest
    {
        /// <summary>Trạng thái mới: dang_kham | da_hoan_tat | da_huy ...</summary>
        public string TrangThai { get; set; } = default!;

        /// <summary>
        /// Thời gian kết thúc lượt khám. Nếu null và trạng thái là done/hủy
        /// thì BE sẽ tự set = DateTime.Now nếu chưa có.
        /// </summary>
        public DateTime? ThoiGianKetThuc { get; set; }
    }



}
