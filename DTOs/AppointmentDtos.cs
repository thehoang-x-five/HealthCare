namespace HealthCare.DTOs
{
    public record class AppointmentReadRequestDto
    { 
        public string MaLichHen { get; set;} = default!;
        public DateTime NgayHen { get; set;}
        public TimeSpan GioHen { get; set;}
        public string? MaBenhNhan { get; set;}
        public string? LoaiHen { get; set;}          
        public string TenBenhNhan { get; set;} = default!;
        public string SoDienThoai { get; set;} = default!;
        public string MaBacSiKham { get; set; } = default!;
        public string TenBacSiKham { get; set;} = default!;
        public string KhoaKham { get; set;} = default!;
        public string? MaLichTruc { get; set;}
        public string? GhiChu { get; set;}
        public string TrangThai { get; set;} = default!;
    }
    public record class AppointmentCreateRequestDto
    {
        public string? MaLichHen { get; set;} 
        public DateTime NgayHen { get; set;}
        public TimeSpan GioHen { get; set;}
        public string? MaBenhNhan { get; set;}
        public string? LoaiHen { get; set;}
        public string TenBenhNhan { get; set;} = default!;
        public string SoDienThoai { get; set;} = default!;
        public string TenBacSiKham { get; set;} = default!;
        public string KhoaKham { get; set;} = default!;
        public string? MaLichTruc { get; set;}
        public string? GhiChu { get; set;}
        public string TrangThai { get; set;} = default!;
    }
    public record class AppointmentFilterRequest
    {
        public DateTime? FromDate { get; set;}
        public DateTime? ToDate { get; set;}
        public string? MaBenhNhan { get; set;}
        public string? LoaiHen { get; set;}
        public string? TrangThai { get; set;}
        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    public record class AppointmentStatusUpdateRequest
    {
        public string TrangThai { get; set;} = default!;
    }
    public record class AppointmentUpdateRequest
    {
        public DateTime? NgayHen { get; set;}
        public TimeSpan? GioHen { get; set;}
        public string? GhiChu { get; set;}
    }
}
