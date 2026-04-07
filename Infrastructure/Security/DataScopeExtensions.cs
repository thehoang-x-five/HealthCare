using System.Linq;
using HealthCare.Datas;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Infrastructure.Security
{
    public static class DataScopeExtensions
    {
        public static IQueryable<string> ScopedPatientIdsByDepartment(this DataContext db, string maKhoa)
        {
            var scope = NormalizeScope(maKhoa);

            var fromQueue = db.HangDois
                .AsNoTracking()
                .Where(h => h.Phong.MaKhoa == scope)
                .Select(h => h.MaBenhNhan);

            var fromClinical = db.PhieuKhamLamSangs
                .AsNoTracking()
                .Where(p =>
                    p.BacSiKham.MaKhoa == scope ||
                    p.NguoiLap.MaKhoa == scope ||
                    p.DichVuKham.PhongThucHien.MaKhoa == scope)
                .Select(p => p.MaBenhNhan);

            var fromCls = db.PhieuKhamCanLamSangs
                .AsNoTracking()
                .Where(p =>
                    p.PhieuKhamLamSang.BacSiKham.MaKhoa == scope ||
                    p.PhieuKhamLamSang.NguoiLap.MaKhoa == scope ||
                    p.ChiTietDichVus.Any(ct => ct.DichVuYTe.PhongThucHien.MaKhoa == scope))
                .Select(p => p.PhieuKhamLamSang.MaBenhNhan);

            var fromPrescription = db.DonThuocs
                .AsNoTracking()
                .Where(d => d.BacSiKeDon.MaKhoa == scope)
                .Select(d => d.MaBenhNhan);

            return fromQueue
                .Union(fromClinical)
                .Union(fromCls)
                .Union(fromPrescription)
                .Distinct();
        }

        public static IQueryable<PhieuKhamLamSang> ApplyClinicalDepartmentScope(
            this IQueryable<PhieuKhamLamSang> query,
            string maKhoa)
        {
            var scope = NormalizeScope(maKhoa);
            return query.Where(p =>
                p.BacSiKham.MaKhoa == scope ||
                p.NguoiLap.MaKhoa == scope ||
                p.DichVuKham.PhongThucHien.MaKhoa == scope);
        }

        public static IQueryable<PhieuKhamCanLamSang> ApplyClsOriginDepartmentScope(
            this IQueryable<PhieuKhamCanLamSang> query,
            string maKhoa)
        {
            var scope = NormalizeScope(maKhoa);
            return query.Where(p =>
                p.PhieuKhamLamSang.BacSiKham.MaKhoa == scope ||
                p.PhieuKhamLamSang.NguoiLap.MaKhoa == scope);
        }

        public static IQueryable<PhieuKhamCanLamSang> ApplyClsServiceDepartmentScope(
            this IQueryable<PhieuKhamCanLamSang> query,
            string maKhoa)
        {
            var scope = NormalizeScope(maKhoa);
            return query.Where(p =>
                p.ChiTietDichVus.Any(ct => ct.DichVuYTe.PhongThucHien.MaKhoa == scope));
        }

        public static IQueryable<PhieuTongHopKetQua> ApplyClsSummaryOriginDepartmentScope(
            this IQueryable<PhieuTongHopKetQua> query,
            string maKhoa)
        {
            var scope = NormalizeScope(maKhoa);
            return query.Where(p =>
                p.PhieuKhamCanLamSang.PhieuKhamLamSang.BacSiKham.MaKhoa == scope ||
                p.PhieuKhamCanLamSang.PhieuKhamLamSang.NguoiLap.MaKhoa == scope);
        }

        public static IQueryable<PhieuTongHopKetQua> ApplyClsSummaryServiceDepartmentScope(
            this IQueryable<PhieuTongHopKetQua> query,
            string maKhoa)
        {
            var scope = NormalizeScope(maKhoa);
            return query.Where(p =>
                p.PhieuKhamCanLamSang.ChiTietDichVus.Any(ct => ct.DichVuYTe.PhongThucHien.MaKhoa == scope));
        }

        public static IQueryable<DonThuoc> ApplyPrescriptionDepartmentScope(
            this IQueryable<DonThuoc> query,
            string maKhoa)
        {
            var scope = NormalizeScope(maKhoa);
            return query.Where(d => d.BacSiKeDon.MaKhoa == scope);
        }

        private static string NormalizeScope(string maKhoa)
        {
            return maKhoa.Trim();
        }
    }
}
