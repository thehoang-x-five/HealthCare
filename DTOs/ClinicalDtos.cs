namespace HealthCare.DTOs
{
    public record class ClinicalExamDto
    {
        public string MaPhieuKham { get; set;} = default!;
        public string MaBenhNhan { get; set;} = default!; 
        public string HoTen { get; set;} = default!;
        public DateTime? NgaySinh { get; set;}
        public string GioiTinh { get; set;} = "";
        public string? DienThoai { get; set;}
        public string? Email { get; set;}
        public string? DiaChi { get; set;}
        public string MaKhoa { get; set;} = default!;
        public string? TenKhoa { get; set;} 
        public string MaPhong { get; set;} = default!;
        public string? TenPhong { get; set;}
        public string MaBacSiKham { get; set;} = default!;

        public string? TenBacSiKham { get; set;}
        public string MaNguoiLap { get; set;} = default!;

        public string? TenNguoiLap { get; set;}
        public string MaDichVuKham { get; set;} = default!;
        public string TenDichVuKham { get; set;} = default!;
        public string LoaiDichVu { get; set;} = default!;
        public string? PhiDV{ get; set;}
        public string? MaLichHen { get; set;}
        public string? LoaiHen { get; set;} //Kham moi ,tái khám
        public string? MaPhieuKqKhamCls { get; set;}
        public string? SnapshotKqKhamCls { get; set;}
        public string HinhThucTiepNhan { get; set;} = default!;
        public DateTime? NgayLap { get; set;}
        public TimeSpan? GioLap { get; set;}
        public string? TrieuChung { get; set;}

        public string? ThongTinChiTiet { get; set;}// toàn bộ thông tin bệnh sử

        public string TrangThai { get; set;} = default!;
    }

    public record class ClinicalExamCreateRequest
    {
        public string MaBenhNhan { get; set;} = default!;
        public string MaKhoa { get; set;} = default!;
        public string MaPhong { get; set;} = default!;

        public string MaBacSiKham { get; set;} = default!;
        public string MaNguoiLap { get; set;} = default!;
        public string MaDichVuKham { get; set;} = default!;
        public string HinhThucTiepNhan { get; set;} = "walkin";
        public string? MaLichHen { get; set;}
        public DateTime? NgayLap { get; set;}
        public TimeSpan? GioLap { get; set;}
        public string? TrieuChung { get; set;}
        public string? DiUng { get; set;}
        public string? ChongChiDinh { get; set;}
        public string? ThuocDangDung { get; set;}
        public string? TieuSuBenh { get; set;}
        public string? TienSuPhauThuat { get; set;}
        public string? NhomMau { get; set;}
        public string? BenhManTinh { get; set;}
        public string? SinhHieu { get; set;}
    }

    public record class ClinicalExamStatusUpdateRequest
    {
        public string TrangThai { get; set;} = default!;
    }

    public record class FinalDiagnosisDto
    {
        public string MaPhieuChanDoan { get; set;} = default!;
        public string MaPhieuKham { get; set;} = default!;
        public string? MaBenhNhan { get; set;} // ✅ Thêm để frontend validate patient ID
        public string? MaDonThuoc { get; set;}
        public string? TenThuoc { get; set;} 
        public string? DonViTinh { get; set;} 
        public string? ChiDinhSuDung { get; set;}
        public int? SoLuong { get; set;}
        public decimal? ThanhTien { get; set;}
        public string? ChanDoanSoBo { get; set;}
        public string? ChanDoanCuoi { get; set;}
        public string? NoiDungKham { get; set;}
        public string? HuongXuTri { get; set;}
        public string? LoiKhuyen { get; set;}
        public string? PhatDoDieuTri { get; set;}   // thêm cho khớp entity
    }

    public record class FinalDiagnosisCreateRequest
    {
        public string MaPhieuKham { get; set;} = default!;
        public string? MaLuotKham { get; set;}
        public string? MaHangDoi { get; set;}
        public string? TrangThaiLuot { get; set;}
        public DateTime? ThoiGianKetThuc { get; set;}
        public string? MaDonThuoc { get; set;}
        public string? MaBacSiKeDon { get; set;}
        public string? ChanDoanSoBo { get; set;}
        public string? ChanDoanCuoi { get; set;}
        public string? NoiDungKham { get; set;}
        public string? HuongXuTri { get; set;}
        public string? LoiKhuyen { get; set;}
        public string? PhatDoDieuTri { get; set;}
        public IReadOnlyList<PrescriptionItemCreateRequest> DonThuoc { get; set;} = Array.Empty<PrescriptionItemCreateRequest>();


    }

    public record class CompleteExamRequest
    {
        public bool ForceComplete { get; set;} = false; // Cho phép hoàn tất dù còn pending
        public string? GhiChu { get; set;}
    }
}
