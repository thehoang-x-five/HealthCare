
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace HealthCare.Datas
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {

        // ========== DbSet ==========
        public DbSet<KhoaChuyenMon> KhoaChuyenMons { get; set; } = default!;
        public DbSet<Phong> Phongs { get; set; } = default!;
        public DbSet<NhanVienYTe> NhanVienYTes { get; set; } = default!;
        public DbSet<LichTruc> LichTrucs { get; set; } = default!;
        public DbSet<DichVuYTe> DichVuYTes { get; set; } = default!;

        public DbSet<BenhNhan> BenhNhans { get; set; } = default!;
        public DbSet<LichHenKham> LichHenKhams { get; set; } = default!;
        public DbSet<PhieuKhamLamSang> PhieuKhamLamSangs { get; set; } = default!;
        public DbSet<PhieuKhamCanLamSang> PhieuKhamCanLamSangs { get; set; } = default!;
        public DbSet<ChiTietDichVu> ChiTietDichVus { get; set; } = default!;
        public DbSet<KetQuaDichVu> KetQuaDichVus { get; set; } = default!;
        public DbSet<HangDoi> HangDois { get; set; } = default!;
        public DbSet<LuotKhamBenh> LuotKhamBenhs { get; set; } = default!;
        public DbSet<PhieuTongHopKetQua> PhieuTongHopKetQuas { get; set; } = default!;
        public DbSet<PhieuChanDoanCuoi> PhieuChanDoanCuois { get; set; } = default!;

        public DbSet<DonThuoc> DonThuocs { get; set; } = default!;
        public DbSet<ChiTietDonThuoc> ChiTietDonThuocs { get; set; } = default!;
        public DbSet<KhoThuoc> KhoThuocs { get; set; } = default!;
        public DbSet<HoaDonThanhToan> HoaDonThanhToans { get; set; } = default!;

        public DbSet<ThongBaoHeThong> ThongBaoHeThongs { get; set; } = default!;
        public DbSet<ThongBaoNguoiNhan> ThongBaoNguoiNhans { get; set; } = default!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity (auto-increment) cho thong_bao_nguoi_nhan.ma_tb_nguoi_nhan (bigint)
            modelBuilder.Entity<ThongBaoNguoiNhan>()
                .Property(t => t.MaTbNguoiNhan)
                .ValueGeneratedOnAdd();

            // Decimal precision
            modelBuilder.Entity<DichVuYTe>()
                .Property(d => d.DonGia)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<KhoThuoc>()
                .Property(t => t.GiaNiemYet)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ChiTietDonThuoc>()
                .Property(c => c.ThanhTien)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DonThuoc>()
                .Property(d => d.TongTienDon)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<HoaDonThanhToan>()
                .Property(h => h.SoTien)
                .HasColumnType("decimal(18,2)");

            // Phong.ThietBi : List<string> -> string (JSON/CSV)
            var stringListConverter = new ValueConverter<List<string>, string>(
                v => v == null || v.Count == 0 ? string.Empty : string.Join(";", v),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<string>()
                    : v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 ?? new()).SequenceEqual(c2 ?? new()),
                c => (c ?? new()).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => (c ?? new()).ToList()
            );

            modelBuilder.Entity<Phong>()
                .Property(p => p.ThietBi)
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);

            // ====== DuLieuNen ======

            // KhoaChuyenMon - Phong (1 - n)
            modelBuilder.Entity<KhoaChuyenMon>()
                .HasMany(k => k.Phongs)
                .WithOne(p => p.KhoaChuyenMon)
                .HasForeignKey(p => p.MaKhoa)
                .OnDelete(DeleteBehavior.Restrict);

            // KhoaChuyenMon - NhanVienYTe (1 - n)
            modelBuilder.Entity<KhoaChuyenMon>()
                .HasMany(k => k.NhanVienYTes)
                .WithOne(nv => nv.KhoaChuyenMon)
                .HasForeignKey(nv => nv.MaKhoa)
                .OnDelete(DeleteBehavior.Restrict);


            // Phong - BacSiPhuTrach (1 - 1, mỗi phòng tối đa 1 BS phụ trách)
            modelBuilder.Entity<Phong>()
                .HasOne(p => p.BacSiPhuTrach)
                .WithOne(nv => nv.PhongsPhuTrach)
                .HasForeignKey<Phong>(p => p.MaBacSiPhuTrach)
                .OnDelete(DeleteBehavior.Restrict);

            // Phong - LichTruc (1 - n)
            modelBuilder.Entity<LichTruc>()
                .HasOne(lt => lt.Phong)
                .WithMany(p => p.LichTrucs)
                .HasForeignKey(lt => lt.MaPhong)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe (Y tá trực) - LichTruc (1 - n)
            modelBuilder.Entity<LichTruc>()
                .HasOne(lt => lt.YTaTruc)
                .WithMany(nv => nv.LichTrucsYTa)
                .HasForeignKey(lt => lt.MaYTaTruc)
                .OnDelete(DeleteBehavior.Restrict);

            // Phong - DichVuYTe (1 - n)  (ma_phong_thuc_hien)
            modelBuilder.Entity<DichVuYTe>()
                .HasOne(dv => dv.PhongThucHien)
                .WithMany(p => p.DichVuYTes)
                .HasForeignKey(dv => dv.MaPhongThucHien)
                .OnDelete(DeleteBehavior.Restrict);

            // ====== TiepNhanVaBenhNhan ======

            // BenhNhan - LichHenKham (1 - n, optional FK)
            modelBuilder.Entity<LichHenKham>()
                .HasOne(lh => lh.BenhNhan)
                .WithMany(bn => bn.LichHenKhams)
                .HasForeignKey(lh => lh.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // LichTruc - LichHenKham (1 - n)
            modelBuilder.Entity<LichHenKham>()
                .HasOne(lh => lh.LichTruc)
                .WithMany(lt => lt.LichHenKhams)
                .HasForeignKey(lh => lh.MaLichTruc)
                .OnDelete(DeleteBehavior.Restrict);

            // BenhNhan - PhieuKhamLamSang (1 - n)
            modelBuilder.Entity<PhieuKhamLamSang>()
                .HasOne(pk => pk.BenhNhan)
                .WithMany(bn => bn.PhieuKhamLamSangs)
                .HasForeignKey(pk => pk.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // LichHenKham - PhieuKhamLamSang (1 - 1 optional)
            modelBuilder.Entity<PhieuKhamLamSang>()
                .HasOne(pk => pk.LichHenKham)
                .WithOne(lh => lh.PhieuKhamLamSangs)
                .HasForeignKey<PhieuKhamLamSang>(pk => pk.MaLichHen)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - PhieuKhamLamSang (lap_phieu: MaNguoiLap)
            modelBuilder.Entity<PhieuKhamLamSang>()
                .HasOne(pk => pk.NguoiLap)
                .WithMany(nv => nv.PhieuKhamLamSangLap)
                .HasForeignKey(pk => pk.MaNguoiLap)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - PhieuKhamLamSang (bac_si_kham: MaBacSiKham)
            modelBuilder.Entity<PhieuKhamLamSang>()
                .HasOne(pk => pk.BacSiKham)
                .WithMany(nv => nv.PhieuKhamLamSangKham)
                .HasForeignKey(pk => pk.MaBacSiKham)
                .OnDelete(DeleteBehavior.Restrict);

            // DichVuYTe (kham_lam_sang) - PhieuKhamLamSang (1 - n)
            modelBuilder.Entity<PhieuKhamLamSang>()
                .HasOne(pk => pk.DichVuKham)
                .WithMany(dv => dv.PhieuKhamLamSangs)
                .HasForeignKey(pk => pk.MaDichVuKham)
                .OnDelete(DeleteBehavior.Restrict);

            // PhieuKhamLamSang - PhieuChanDoanCuoi (1 - 1)
            modelBuilder.Entity<PhieuChanDoanCuoi>()
                .HasOne(pc => pc.PhieuKhamLamSang)
                .WithOne(pk => pk.PhieuChanDoanCuoi)
                .HasForeignKey<PhieuChanDoanCuoi>(pc => pc.MaPhieuKham)
                .OnDelete(DeleteBehavior.Restrict);

            // ====== CLS ======

            // PhieuKhamLamSang - PhieuKhamCanLamSang (1 - 1)
            modelBuilder.Entity<PhieuKhamCanLamSang>()
                .HasOne(cls => cls.PhieuKhamLamSang)
                .WithOne(ls => ls.PhieuKhamCanLamSang)
                .HasForeignKey<PhieuKhamCanLamSang>(cls => cls.MaPhieuKhamLs)
                .OnDelete(DeleteBehavior.Restrict);



            // PhieuKhamCanLamSang - ChiTietDichVu (1 - n)
            modelBuilder.Entity<ChiTietDichVu>()
                .HasOne(ct => ct.PhieuKhamCanLamSang)
                .WithMany(cls => cls.ChiTietDichVus)
                .HasForeignKey(ct => ct.MaPhieuKhamCls)
                .OnDelete(DeleteBehavior.Restrict);

            // DichVuYTe (can_lam_sang) - ChiTietDichVu (1 - n)
            modelBuilder.Entity<ChiTietDichVu>()
                .HasOne(ct => ct.DichVuYTe)
                .WithMany(dv => dv.ChiTietDichVus)
                .HasForeignKey(ct => ct.MaDichVu)
                .OnDelete(DeleteBehavior.Restrict);

            // ChiTietDichVu - KetQuaDichVu (1 - 1)
            modelBuilder.Entity<KetQuaDichVu>()
                .HasOne(kq => kq.ChiTietDichVu)
                .WithOne(ct => ct.KetQuaDichVu)
                .HasForeignKey<KetQuaDichVu>(kq => kq.MaChiTietDv)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - KetQuaDichVu (1 - n)
            modelBuilder.Entity<KetQuaDichVu>()
                .HasOne(kq => kq.NhanVienYTes)
                .WithMany(nv => nv.KetQuaDichVus)
                .HasForeignKey(kq => kq.MaNguoiTao)
                .OnDelete(DeleteBehavior.Restrict);

            // PhieuKhamCanLamSang - PhieuTongHopKetQua (1 - 1)
            modelBuilder.Entity<PhieuTongHopKetQua>()
                .HasOne(pt => pt.PhieuKhamCanLamSang)
                .WithOne(cls => cls.PhieuTongHopKetQua)
                .HasForeignKey<PhieuTongHopKetQua>(pt => pt.MaPhieuKhamCls)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - PhieuTongHopKetQua (1 - n)
            modelBuilder.Entity<PhieuTongHopKetQua>()
                .HasOne(pt => pt.NhanSuXuLy)
                .WithMany(nv => nv.PhieuTongHopXuLy)
                .HasForeignKey(pt => pt.MaNhanSuXuLy)
                .OnDelete(DeleteBehavior.Restrict);

            // PhieuTongHopKetQua - PhieuKhamLamSang (follow_up_ls, 1 - 1 optional)
            modelBuilder.Entity<PhieuTongHopKetQua>()
                .HasOne(pt => pt.PhieuKhamLamSang)
                .WithOne(pk => pk.PhieuTongHopKetQua)
                .HasForeignKey<PhieuKhamLamSang>(pk => pk.MaPhieuKqKhamCls)
                .OnDelete(DeleteBehavior.Restrict);

            // ====== Hàng đợi & Lượt khám ======

            // BenhNhan - HangDoi (1 - n)
            modelBuilder.Entity<HangDoi>()
                .HasOne(h => h.BenhNhan)
                .WithMany(bn => bn.HangDois)
                .HasForeignKey(h => h.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // Phong - HangDoi (1 - n)
            modelBuilder.Entity<HangDoi>()
                .HasOne(h => h.Phong)
                .WithMany(p => p.HangDois)
                .HasForeignKey(h => h.MaPhong)
                .OnDelete(DeleteBehavior.Restrict);

            // HangDoi - PhieuKhamLamSang (queue_LS, 1 - 1 optional)
            modelBuilder.Entity<HangDoi>()
                .HasOne(h => h.PhieuKhamLamSang)
                .WithOne(pk => pk.HangDois)
                .HasForeignKey<HangDoi>(h => h.MaPhieuKham)
                .OnDelete(DeleteBehavior.Restrict);

            // HangDoi - ChiTietDichVu (queue_CLS, 1 - 1 optional)
            modelBuilder.Entity<HangDoi>()
                .HasOne(h => h.ChiTietDichVu)
                .WithOne(ct => ct.HangDois)
                .HasForeignKey<HangDoi>(h => h.MaChiTietDv)
                .OnDelete(DeleteBehavior.Restrict);

            // HangDoi - LuotKhamBenh (1 - 1, MaHangDoi unique)
            modelBuilder.Entity<LuotKhamBenh>()
                .HasOne(lk => lk.HangDoi)
                .WithOne(h => h.LuotKhamBenh)
                .HasForeignKey<LuotKhamBenh>(lk => lk.MaHangDoi)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - LuotKhamBenh (bac_si_kham)
            modelBuilder.Entity<LuotKhamBenh>()
                .HasOne(lk => lk.NhanSuThucHien)
                .WithMany(nv => nv.LuotKhamThucHien)
                .HasForeignKey(lk => lk.MaNhanSuThucHien)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - LuotKhamBenh (y_ta_ho_tro)
            modelBuilder.Entity<LuotKhamBenh>()
                .HasOne(lk => lk.YTaHoTro)
                .WithMany(nv => nv.LuotKhamYTaHoTro)
                .HasForeignKey(lk => lk.MaYTaHoTro)
                .OnDelete(DeleteBehavior.Restrict);
            // NhanVienYTe - RefreshToken (1 - n)
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(r => r.Id);

                // Khóa chính dạng string GUID "N" => 32 ký tự
                entity.Property(r => r.Id)
                    .HasMaxLength(64);

                // Index để tìm nhanh theo token & user
                entity.HasIndex(r => r.Token)
                    .IsUnique();

                entity.HasIndex(r => new { r.MaNhanVien, r.IsTrangThai });

                entity.HasOne(r => r.NhanVien)
                    .WithMany(nv => nv.RefreshTokens)
                    .HasForeignKey(r => r.MaNhanVien)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ====== Kê đơn & Thuốc ======

            // BenhNhan - DonThuoc (1 - n)
            modelBuilder.Entity<DonThuoc>()
                .HasOne(dt => dt.BenhNhan)
                .WithMany(bn => bn.DonThuocs)
                .HasForeignKey(dt => dt.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - DonThuoc (bs_ke_don)
            modelBuilder.Entity<DonThuoc>()
                .HasOne(dt => dt.BacSiKeDon)
                .WithMany(nv => nv.DonThuocKe)
                .HasForeignKey(dt => dt.MaBacSiKeDon)
                .OnDelete(DeleteBehavior.Restrict);

            // DonThuoc - ChiTietDonThuoc (1 - n)
            modelBuilder.Entity<ChiTietDonThuoc>()
                .HasOne(ct => ct.DonThuoc)
                .WithMany(dt => dt.ChiTietDonThuocs)
                .HasForeignKey(ct => ct.MaDonThuoc)
                .OnDelete(DeleteBehavior.Restrict);

            // KhoThuoc - ChiTietDonThuoc (1 - n)
            modelBuilder.Entity<ChiTietDonThuoc>()
                .HasOne(ct => ct.KhoThuoc)
                .WithMany(t => t.ChiTietDonThuocs)
                .HasForeignKey(ct => ct.MaThuoc)
                .OnDelete(DeleteBehavior.Restrict);

            // PhieuChanDoanCuoi - DonThuoc (1 - 1 optional)
            modelBuilder.Entity<DonThuoc>()
                .HasOne(dt => dt.PhieuChanDoanCuoi)
                .WithOne(pc => pc.DonThuoc)
                .HasForeignKey<PhieuChanDoanCuoi>(pc => pc.MaDonThuoc)
                .OnDelete(DeleteBehavior.Restrict);

            // ====== Hóa đơn ======

            // BenhNhan - HoaDonThanhToan (1 - n)
            modelBuilder.Entity<HoaDonThanhToan>()
                .HasOne(hd => hd.BenhNhan)
                .WithMany(bn => bn.HoaDonThanhToans)
                .HasForeignKey(hd => hd.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - HoaDonThanhToan (nhan_su_thu)
            modelBuilder.Entity<HoaDonThanhToan>()
                .HasOne(hd => hd.NhanSuThu)
                .WithMany(nv => nv.HoaDonThu)
                .HasForeignKey(hd => hd.MaNhanSuThu)
                .OnDelete(DeleteBehavior.Restrict);

            // HoaDonThanhToan - DonThuoc (1 - 1 optional)
            modelBuilder.Entity<HoaDonThanhToan>()
                .HasOne(hd => hd.DonThuoc)
                .WithOne(dt => dt.HoaDonThanhToans)
                .HasForeignKey<HoaDonThanhToan>(hd => hd.MaDonThuoc)
                .OnDelete(DeleteBehavior.Restrict);

            // HoaDonThanhToan - PhieuKhamCanLamSang (1 - 1 optional)
            modelBuilder.Entity<HoaDonThanhToan>()
                .HasOne(hd => hd.PhieuKhamCanLamSang)
                .WithOne(cls => cls.HoaDonThanhToans)
                .HasForeignKey<HoaDonThanhToan>(hd => hd.MaPhieuKhamCls)
                .OnDelete(DeleteBehavior.Restrict);

            // HoaDonThanhToan - PhieuKhamLamSang (1 - 1 optional)
            modelBuilder.Entity<HoaDonThanhToan>()
                .HasOne(hd => hd.PhieuKhamLamSang)
                .WithOne(pk => pk.HoaDonThanhToans)
                .HasForeignKey<HoaDonThanhToan>(hd => hd.MaPhieuKham)
                .OnDelete(DeleteBehavior.Restrict);

            // ====== Thông báo ======

            // ThongBaoHeThong - ThongBaoNguoiNhan (1 - n)
            modelBuilder.Entity<ThongBaoNguoiNhan>()
                .HasOne(tn => tn.ThongBaoHeThong)
                .WithMany(tb => tb.ThongBaoNguoiNhans)
                .HasForeignKey(tn => tn.MaThongBao)
                .OnDelete(DeleteBehavior.Cascade);

            // BenhNhan - ThongBaoNguoiNhan (1 - n optional)
            modelBuilder.Entity<ThongBaoNguoiNhan>()
                .HasOne(tn => tn.BenhNhan)
                .WithMany(bn => bn.ThongBaoNguoiNhans)
                .HasForeignKey(tn => tn.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // NhanVienYTe - ThongBaoNguoiNhan (1 - n optional)
            modelBuilder.Entity<ThongBaoNguoiNhan>()
                .HasOne(tn => tn.NhanVienYTe)
                .WithMany(nv => nv.ThongBaoNguoiNhans)
                .HasForeignKey(tn => tn.MaNhanSu)
                .OnDelete(DeleteBehavior.Restrict);


            // LuotKhamBenh - ThongBaoHeThong (1 - n)
            modelBuilder.Entity<ThongBaoHeThong>()
                .HasOne(tb => tb.LuotKhamBenh)
                .WithMany(lk => lk.ThongBaoHeThongs)
                .HasForeignKey(tb => tb.MaLuotKham)
                .OnDelete(DeleteBehavior.Restrict);

            // PhieuKhamLamSang - ThongBaoHeThong (1 - n)
            modelBuilder.Entity<ThongBaoHeThong>()
                .HasOne(tb => tb.PhieuKhamLamSang)
                .WithMany(pk => pk.ThongBaoHeThongs)
                .HasForeignKey(tb => tb.MaPhieuKham)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
