using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthCare.Services.MedicationBilling
{
    /// <summary>
    /// Interface for inventory transaction logging service.
    /// </summary>
    public interface ILichSuXuatKhoService
    {
        /// <summary>
        /// Ghi log xuất kho (trừ tồn kho).
        /// </summary>
        Task GhiLogXuatKhoAsync(string maThuoc, int soLuong, string? maDonThuoc, string maNhanSuXuat, string? ghiChu = null);

        /// <summary>
        /// Ghi log nhập/hoàn kho (cộng tồn kho).
        /// </summary>
        Task GhiLogNhapKhoAsync(string maThuoc, int soLuong, string? maDonThuoc, string maNhanSuXuat, string? ghiChu = null);

        /// <summary>
        /// Lấy lịch sử giao dịch kho theo thuốc.
        /// </summary>
        Task<List<LichSuXuatKhoDto>> LayLichSuTheoThuocAsync(string maThuoc, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Lấy lịch sử giao dịch kho (tất cả thuốc, có paging).
        /// </summary>
        Task<PagedResult<LichSuXuatKhoDto>> LayLichSuXuatKhoAsync(
            string? maThuoc = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? loaiGiaoDich = null,
            int page = 1,
            int pageSize = 50);
    }

    /// <summary>
    /// Service for logging inventory transactions (xuất kho / nhập kho / hoàn trả).
    /// Matches the LichSuXuatKho entity on main (MaNhanSuXuat, SoLuongConLai, ThoiGianXuat).
    /// </summary>
    public class LichSuXuatKhoService : ILichSuXuatKhoService
    {
        private readonly DataContext _db;
        private readonly ILogger<LichSuXuatKhoService> _logger;

        public LichSuXuatKhoService(DataContext db, ILogger<LichSuXuatKhoService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task GhiLogXuatKhoAsync(string maThuoc, int soLuong, string? maDonThuoc, string maNhanSuXuat, string? ghiChu = null)
        {
            if (string.IsNullOrWhiteSpace(maThuoc))
                throw new ArgumentException("MaThuoc là bắt buộc");
            if (soLuong <= 0)
                throw new ArgumentException("SoLuong phải > 0");

            var thuoc = await _db.KhoThuocs
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.MaThuoc == maThuoc);

            if (thuoc == null)
            {
                _logger.LogWarning("Thuốc {MaThuoc} không tồn tại, không ghi log xuất kho", maThuoc);
                return;
            }

            var log = new LichSuXuatKho
            {
                MaGiaoDich = Guid.NewGuid().ToString("N"),
                MaThuoc = maThuoc,
                MaDonThuoc = maDonThuoc,
                MaNhanSuXuat = maNhanSuXuat,
                LoaiGiaoDich = "xuat_ban",
                SoLuong = soLuong,
                SoLuongConLai = thuoc.SoLuongTon, // Tồn hiện tại (sau khi đã trừ)
                ThoiGianXuat = DateTime.Now,
                GhiChu = ghiChu
            };

            _db.LichSuXuatKhos.Add(log);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Ghi log xuất kho: Thuốc={MaThuoc}, SoLuong={SoLuong}, ConLai={ConLai}",
                maThuoc, soLuong, thuoc.SoLuongTon);
        }

        public async Task GhiLogNhapKhoAsync(string maThuoc, int soLuong, string? maDonThuoc, string maNhanSuXuat, string? ghiChu = null)
        {
            if (string.IsNullOrWhiteSpace(maThuoc))
                throw new ArgumentException("MaThuoc là bắt buộc");
            if (soLuong <= 0)
                throw new ArgumentException("SoLuong phải > 0");

            var thuoc = await _db.KhoThuocs
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.MaThuoc == maThuoc);

            if (thuoc == null)
            {
                _logger.LogWarning("Thuốc {MaThuoc} không tồn tại, không ghi log nhập kho", maThuoc);
                return;
            }

            var log = new LichSuXuatKho
            {
                MaGiaoDich = Guid.NewGuid().ToString("N"),
                MaThuoc = maThuoc,
                MaDonThuoc = maDonThuoc,
                MaNhanSuXuat = maNhanSuXuat,
                LoaiGiaoDich = "hoan_tra",
                SoLuong = soLuong,
                SoLuongConLai = thuoc.SoLuongTon, // Tồn hiện tại (sau khi đã cộng)
                ThoiGianXuat = DateTime.Now,
                GhiChu = ghiChu
            };

            _db.LichSuXuatKhos.Add(log);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Ghi log nhập kho: Thuốc={MaThuoc}, SoLuong={SoLuong}, ConLai={ConLai}",
                maThuoc, soLuong, thuoc.SoLuongTon);
        }

        public async Task<List<LichSuXuatKhoDto>> LayLichSuTheoThuocAsync(
            string maThuoc,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _db.LichSuXuatKhos
                .AsNoTracking()
                .Include(l => l.KhoThuoc)
                .Include(l => l.NhanSuXuat)
                .Where(l => l.MaThuoc == maThuoc);

            if (fromDate.HasValue)
                query = query.Where(l => l.ThoiGianXuat >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.ThoiGianXuat <= toDate.Value);

            var list = await query
                .OrderByDescending(l => l.ThoiGianXuat)
                .ToListAsync();

            return list.Select(MapDto).ToList();
        }

        public async Task<PagedResult<LichSuXuatKhoDto>> LayLichSuXuatKhoAsync(
            string? maThuoc = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? loaiGiaoDich = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _db.LichSuXuatKhos
                .AsNoTracking()
                .Include(l => l.KhoThuoc)
                .Include(l => l.NhanSuXuat)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(maThuoc))
                query = query.Where(l => l.MaThuoc == maThuoc);

            if (fromDate.HasValue)
                query = query.Where(l => l.ThoiGianXuat >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.ThoiGianXuat <= toDate.Value);

            if (!string.IsNullOrWhiteSpace(loaiGiaoDich))
                query = query.Where(l => l.LoaiGiaoDich == loaiGiaoDich);

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .OrderByDescending(l => l.ThoiGianXuat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<LichSuXuatKhoDto>
            {
                Items = list.Select(MapDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        private static LichSuXuatKhoDto MapDto(LichSuXuatKho entity)
        {
            return new LichSuXuatKhoDto
            {
                MaGiaoDich = entity.MaGiaoDich,
                MaThuoc = entity.MaThuoc,
                TenThuoc = entity.KhoThuoc?.TenThuoc ?? "",
                MaDonThuoc = entity.MaDonThuoc,
                MaNhanSuXuat = entity.MaNhanSuXuat,
                TenNhanSuXuat = entity.NhanSuXuat?.HoTen ?? "",
                LoaiGiaoDich = entity.LoaiGiaoDich,
                SoLuong = entity.SoLuong,
                SoLuongConLai = entity.SoLuongConLai,
                ThoiGianXuat = entity.ThoiGianXuat,
                GhiChu = entity.GhiChu
            };
        }
    }
}
