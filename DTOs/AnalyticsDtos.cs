using System;
using System.Collections.Generic;

namespace HealthCare.DTOs
{
    /// <summary>
    /// Statistics of abnormal test results.
    /// </summary>
    public class AbnormalStatsDto
    {
        public int TotalTests { get; set; }
        public int AbnormalCount { get; set; }
        public decimal AbnormalPercentage { get; set; }
        public List<AbnormalTestTypeDto> ByTestType { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class AbnormalTestTypeDto
    {
        public string TestType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Disease trends based on ICD-10 codes.
    /// </summary>
    public class DiseaseTrendsDto
    {
        public int TotalDiagnoses { get; set; }
        public List<DiseaseStatDto> TopDiseases { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class DiseaseStatDto
    {
        public string ICD10Code { get; set; } = string.Empty;
        public string DiseaseName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Popular medications statistics.
    /// </summary>
    public class PopularDrugsDto
    {
        public int TotalPrescriptions { get; set; }
        public List<DrugStatDto> TopDrugs { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class DrugStatDto
    {
        public string MaThuoc { get; set; } = string.Empty;
        public string TenThuoc { get; set; } = string.Empty;
        public int PrescriptionCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

    /// <summary>
    /// Inventory transaction history (xuất kho / nhập kho).
    /// </summary>
    public class LichSuXuatKhoDto
    {
        public string MaGiaoDich { get; set; } = string.Empty;
        public string MaThuoc { get; set; } = string.Empty;
        public string TenThuoc { get; set; } = string.Empty;
        public string LoaiGiaoDich { get; set; } = string.Empty; // "xuat_kho" | "nhap_kho"
        public int SoLuong { get; set; }
        public int TonTruoc { get; set; }
        public int TonSau { get; set; }
        public string? LyDo { get; set; }
        public string? NguoiThucHien { get; set; }
        public DateTime ThoiGian { get; set; }
    }
