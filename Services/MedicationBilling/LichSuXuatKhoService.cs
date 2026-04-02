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
    public class LichSuXuatKhoService : ILichSuXuatKhoService
    {
        private readonly DataContext _db;
        private readonly ILogger<LichSuXuatKhoService> _logger;

        public LichSuXuatKhoService(DataContext db, ILogger<LichSuXuatKhoService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task GhiLogXuatKhoAsync(string maThuoc, int soLuong, string lyDo, string? nguoiThucHien = null)
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
                TenThuoc = thuoc.TenThuoc,
                LoaiGiaoDich = "xuat_kho",
                SoLuong = soLuong,
                TonTruoc = thuoc.SoLuongTon + soLuong,
                TonSau = thuoc.SoLuongTon,
                LyDo = lyDo,
                NguoiThucHien = nguoiThucHien,
                ThoiGian = DateTime.Now
            };

            _db.LichSuXuatKhos.Add(log);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Ghi log xuất kho: Thuốc={MaThuoc}, SoLuong={SoLuong}, TonSau={TonSau}",
                maThuoc, soLuong, thuoc.SoLuongTon);
        }

        public async Task GhiLogNhapKhoAsync(string maThuoc, int soLuong, string lyDo, string? nguoiThucHien = null)
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
                TenThuoc = thuoc.TenThuoc,
                LoaiGiaoDich = "nhap_kho",
                SoLuong = soLuong,
                TonTruoc = thuoc.SoLuongTon - soLuong,
                TonSau = thuoc.SoLuongTon,
                LyDo = lyDo,
                NguoiThucHien = nguoiThucHien,
                ThoiGian = DateTime.Now
            };

            _db.LichSuXuatKhos.Add(log);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Ghi log nhập kho: Thuốc={MaThuoc}, SoLuong={SoLuong}, TonSau={TonSau}",
                maThuoc, soLuong, thuoc.SoLuongTon);
        }

        public async Task<List<LichSuXuatKhoDto>> LayLichSuTheoThuocAsync(
            string maThuoc,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _db.LichSuXuatKhos
                .AsNoTracking()
                .Where(l => l.MaThuoc == maThuoc);

            if (fromDate.HasValue)
                query = query.Where(l => l.ThoiGian >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.ThoiGian <= toDate.Value);

            var list = await query
                .OrderByDescending(l => l.ThoiGian)
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
            var query = _db.LichSuXuatKhos.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(maThuoc))
                query = query.Where(l => l.MaThuoc == maThuoc);

            if (fromDate.HasValue)
                query = query.Where(l => l.ThoiGian >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.ThoiGian <= toDate.Value);

            if (!string.IsNullOrWhiteSpace(loaiGiaoDich))
                query = query.Where(l => l.LoaiGiaoDich == loaiGiaoDich);

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .OrderByDescending(l => l.ThoiGian)
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
                TenThuoc = entity.TenThuoc,
                LoaiGiaoDich = entity.LoaiGiaoDich,
                SoLuong = entity.SoLuong,
                TonTruoc = entity.TonTruoc,
                TonSau = entity.TonSau,
                LyDo = entity.LyDo,
                NguoiThucHien = entity.NguoiThucHien,
                ThoiGian = entity.ThoiGian
            };
        }
    }
}
