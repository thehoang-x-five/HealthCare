using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCare.Migrations
{
    /// <inheritdoc />
    public partial class thenew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "benh_nhan",
                columns: table => new
                {
                    MaBenhNhan = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HoTen = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgaySinh = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GioiTinh = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DienThoai = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiaChi = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiUng = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChongChiDinh = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThuocDangDung = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TieuSuBenh = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TienSuPhauThuat = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NhomMau = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BenhManTinh = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SinhHieu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiTaiKhoan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiHomNay = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayTrangThai = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_benh_nhan", x => x.MaBenhNhan);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kho_thuoc",
                columns: table => new
                {
                    MaThuoc = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenThuoc = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DonViTinh = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CongDung = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GiaNiemYet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuongTon = table.Column<int>(type: "int", nullable: false),
                    HanSuDung = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SoLo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kho_thuoc", x => x.MaThuoc);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "khoa_chuyen_mon",
                columns: table => new
                {
                    MaKhoa = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenKhoa = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MoTa = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DienThoai = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiaDiem = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GhiChu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_khoa_chuyen_mon", x => x.MaKhoa);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nhan_vien_y_te",
                columns: table => new
                {
                    MaNhanVien = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenDangNhap = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MatKhauHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnhDaiDien = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoNamKinhNghiem = table.Column<int>(type: "int", nullable: false),
                    ChuyenMon = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HocVi = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HoTen = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VaiTro = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiYTa = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DienThoai = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiCongTac = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MoTa = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaKhoa = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nhan_vien_y_te", x => x.MaNhanVien);
                    table.ForeignKey(
                        name: "FK_nhan_vien_y_te_khoa_chuyen_mon_MaKhoa",
                        column: x => x.MaKhoa,
                        principalTable: "khoa_chuyen_mon",
                        principalColumn: "MaKhoa",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "don_thuoc",
                columns: table => new
                {
                    MaDonThuoc = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBacSiKeDon = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBenhNhan = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianKeDon = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TongTienDon = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_don_thuoc", x => x.MaDonThuoc);
                    table.ForeignKey(
                        name: "FK_don_thuoc_benh_nhan_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "benh_nhan",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_don_thuoc_nhan_vien_y_te_MaBacSiKeDon",
                        column: x => x.MaBacSiKeDon,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phong",
                columns: table => new
                {
                    MaPhong = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenPhong = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaKhoa = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiPhong = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SucChua = table.Column<int>(type: "int", nullable: true),
                    ViTri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GioMoCua = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    GioDongCua = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    ThietBi = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DienThoai = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBacSiPhuTrach = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phong", x => x.MaPhong);
                    table.ForeignKey(
                        name: "FK_phong_khoa_chuyen_mon_MaKhoa",
                        column: x => x.MaKhoa,
                        principalTable: "khoa_chuyen_mon",
                        principalColumn: "MaKhoa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phong_nhan_vien_y_te_MaBacSiPhuTrach",
                        column: x => x.MaBacSiPhuTrach,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaNhanVien = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Token = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ThoiGianHetHan = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsTrangThai = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianThuHoi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RevokedIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplacedToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_token", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_token_nhan_vien_y_te_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chi_tiet_don_thuoc",
                columns: table => new
                {
                    MaChiTietDon = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaDonThuoc = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaThuoc = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChiDinhSuDung = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chi_tiet_don_thuoc", x => x.MaChiTietDon);
                    table.ForeignKey(
                        name: "FK_chi_tiet_don_thuoc_don_thuoc_MaDonThuoc",
                        column: x => x.MaDonThuoc,
                        principalTable: "don_thuoc",
                        principalColumn: "MaDonThuoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chi_tiet_don_thuoc_kho_thuoc_MaThuoc",
                        column: x => x.MaThuoc,
                        principalTable: "kho_thuoc",
                        principalColumn: "MaThuoc",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dich_vu_y_te",
                columns: table => new
                {
                    MaDichVu = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiDichVu = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenDichVu = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThoiGianDuKienPhut = table.Column<int>(type: "int", nullable: false),
                    MaPhongThucHien = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dich_vu_y_te", x => x.MaDichVu);
                    table.ForeignKey(
                        name: "FK_dich_vu_y_te_phong_MaPhongThucHien",
                        column: x => x.MaPhongThucHien,
                        principalTable: "phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lich_truc",
                columns: table => new
                {
                    MaLichTruc = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NghiTruc = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CaTruc = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GioBatDau = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    MaYTaTruc = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhong = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lich_truc", x => x.MaLichTruc);
                    table.ForeignKey(
                        name: "FK_lich_truc_nhan_vien_y_te_MaYTaTruc",
                        column: x => x.MaYTaTruc,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lich_truc_phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lich_hen_kham",
                columns: table => new
                {
                    MaLichHen = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CoHieuLuc = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NgayHen = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GioHen = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    ThoiLuongPhut = table.Column<int>(type: "int", nullable: false),
                    MaBenhNhan = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiHen = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenBenhNhan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoDienThoai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaLichTruc = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GhiChu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lich_hen_kham", x => x.MaLichHen);
                    table.ForeignKey(
                        name: "FK_lich_hen_kham_benh_nhan_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "benh_nhan",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lich_hen_kham_lich_truc_MaLichTruc",
                        column: x => x.MaLichTruc,
                        principalTable: "lich_truc",
                        principalColumn: "MaLichTruc",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chi_tiet_dich_vu",
                columns: table => new
                {
                    MaChiTietDv = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKhamCls = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaDichVu = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GhiChu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chi_tiet_dich_vu", x => x.MaChiTietDv);
                    table.ForeignKey(
                        name: "FK_chi_tiet_dich_vu_dich_vu_y_te_MaDichVu",
                        column: x => x.MaDichVu,
                        principalTable: "dich_vu_y_te",
                        principalColumn: "MaDichVu",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ket_qua_dich_vu",
                columns: table => new
                {
                    MaKetQua = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaChiTietDv = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiChot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoiDungKetQua = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaNguoiTao = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TepDinhKem = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ket_qua_dich_vu", x => x.MaKetQua);
                    table.ForeignKey(
                        name: "FK_ket_qua_dich_vu_chi_tiet_dich_vu_MaChiTietDv",
                        column: x => x.MaChiTietDv,
                        principalTable: "chi_tiet_dich_vu",
                        principalColumn: "MaChiTietDv",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ket_qua_dich_vu_nhan_vien_y_te_MaNguoiTao",
                        column: x => x.MaNguoiTao,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "hang_doi",
                columns: table => new
                {
                    MaHangDoi = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBenhNhan = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhong = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiHangDoi = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nguon = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nhan = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CapCuu = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PhanLoaiDen = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianCheckin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ThoiGianLichHen = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DoUuTien = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GhiChu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKham = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaChiTietDv = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hang_doi", x => x.MaHangDoi);
                    table.ForeignKey(
                        name: "FK_hang_doi_benh_nhan_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "benh_nhan",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hang_doi_chi_tiet_dich_vu_MaChiTietDv",
                        column: x => x.MaChiTietDv,
                        principalTable: "chi_tiet_dich_vu",
                        principalColumn: "MaChiTietDv",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hang_doi_phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "luot_kham_benh",
                columns: table => new
                {
                    MaLuotKham = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaHangDoi = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaNhanSuThucHien = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaYTaHoTro = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiLuot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ThoiGianKetThuc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_luot_kham_benh", x => x.MaLuotKham);
                    table.ForeignKey(
                        name: "FK_luot_kham_benh_hang_doi_MaHangDoi",
                        column: x => x.MaHangDoi,
                        principalTable: "hang_doi",
                        principalColumn: "MaHangDoi",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_luot_kham_benh_nhan_vien_y_te_MaNhanSuThucHien",
                        column: x => x.MaNhanSuThucHien,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_luot_kham_benh_nhan_vien_y_te_MaYTaHoTro",
                        column: x => x.MaYTaHoTro,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "hoa_don_thanh_toan",
                columns: table => new
                {
                    MaHoaDon = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBenhNhan = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaNhanSuThu = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKhamCls = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKham = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaDonThuoc = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiDotthu = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGian = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoiDung = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hoa_don_thanh_toan", x => x.MaHoaDon);
                    table.ForeignKey(
                        name: "FK_hoa_don_thanh_toan_benh_nhan_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "benh_nhan",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hoa_don_thanh_toan_don_thuoc_MaDonThuoc",
                        column: x => x.MaDonThuoc,
                        principalTable: "don_thuoc",
                        principalColumn: "MaDonThuoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hoa_don_thanh_toan_nhan_vien_y_te_MaNhanSuThu",
                        column: x => x.MaNhanSuThu,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phieu_chan_doan_cuoi",
                columns: table => new
                {
                    MaPhieuChanDoan = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKham = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaDonThuoc = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChanDoanSoBo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChanDoanCuoi = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoiDungKham = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HuongXuTri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoiKhuyen = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhatDoDieuTri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phieu_chan_doan_cuoi", x => x.MaPhieuChanDoan);
                    table.ForeignKey(
                        name: "FK_phieu_chan_doan_cuoi_don_thuoc_MaDonThuoc",
                        column: x => x.MaDonThuoc,
                        principalTable: "don_thuoc",
                        principalColumn: "MaDonThuoc",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phieu_kham_can_lam_sang",
                columns: table => new
                {
                    MaPhieuKhamCls = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKhamLs = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayGioLap = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AutoPublishEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GhiChu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phieu_kham_can_lam_sang", x => x.MaPhieuKhamCls);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phieu_tong_hop_ket_qua",
                columns: table => new
                {
                    MaPhieuTongHop = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKhamCls = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiPhieu = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaNhanSuXuLy = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianXuLy = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SnapshotJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phieu_tong_hop_ket_qua", x => x.MaPhieuTongHop);
                    table.ForeignKey(
                        name: "FK_phieu_tong_hop_ket_qua_nhan_vien_y_te_MaNhanSuXuLy",
                        column: x => x.MaNhanSuXuLy,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phieu_tong_hop_ket_qua_phieu_kham_can_lam_sang_MaPhieuKhamCls",
                        column: x => x.MaPhieuKhamCls,
                        principalTable: "phieu_kham_can_lam_sang",
                        principalColumn: "MaPhieuKhamCls",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phieu_kham_lam_sang",
                columns: table => new
                {
                    MaPhieuKham = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBacSiKham = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaNguoiLap = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBenhNhan = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaLichHen = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaDichVuKham = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKqKhamCls = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HinhThucTiepNhan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayLap = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GioLap = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    TrieuChung = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phieu_kham_lam_sang", x => x.MaPhieuKham);
                    table.ForeignKey(
                        name: "FK_phieu_kham_lam_sang_benh_nhan_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "benh_nhan",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phieu_kham_lam_sang_dich_vu_y_te_MaDichVuKham",
                        column: x => x.MaDichVuKham,
                        principalTable: "dich_vu_y_te",
                        principalColumn: "MaDichVu",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phieu_kham_lam_sang_lich_hen_kham_MaLichHen",
                        column: x => x.MaLichHen,
                        principalTable: "lich_hen_kham",
                        principalColumn: "MaLichHen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phieu_kham_lam_sang_nhan_vien_y_te_MaBacSiKham",
                        column: x => x.MaBacSiKham,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phieu_kham_lam_sang_nhan_vien_y_te_MaNguoiLap",
                        column: x => x.MaNguoiLap,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phieu_kham_lam_sang_phieu_tong_hop_ket_qua_MaPhieuKqKhamCls",
                        column: x => x.MaPhieuKqKhamCls,
                        principalTable: "phieu_tong_hop_ket_qua",
                        principalColumn: "MaPhieuTongHop",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "thong_bao_he_thong",
                columns: table => new
                {
                    MaThongBao = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TieuDe = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoiDung = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiThongBao = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DoUuTien = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianGui = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MaLuotKham = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaPhieuKham = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thong_bao_he_thong", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_thong_bao_he_thong_luot_kham_benh_MaLuotKham",
                        column: x => x.MaLuotKham,
                        principalTable: "luot_kham_benh",
                        principalColumn: "MaLuotKham",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_thong_bao_he_thong_phieu_kham_lam_sang_MaPhieuKham",
                        column: x => x.MaPhieuKham,
                        principalTable: "phieu_kham_lam_sang",
                        principalColumn: "MaPhieuKham",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "thong_bao_nguoi_nhan",
                columns: table => new
                {
                    MaTbNguoiNhan = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaThongBao = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiNguoiNhan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaBenhNhan = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaNhanSu = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DaDoc = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ThoiGianDoc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thong_bao_nguoi_nhan", x => x.MaTbNguoiNhan);
                    table.ForeignKey(
                        name: "FK_thong_bao_nguoi_nhan_benh_nhan_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "benh_nhan",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_thong_bao_nguoi_nhan_nhan_vien_y_te_MaNhanSu",
                        column: x => x.MaNhanSu,
                        principalTable: "nhan_vien_y_te",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_thong_bao_nguoi_nhan_thong_bao_he_thong_MaThongBao",
                        column: x => x.MaThongBao,
                        principalTable: "thong_bao_he_thong",
                        principalColumn: "MaThongBao",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_dich_vu_MaDichVu",
                table: "chi_tiet_dich_vu",
                column: "MaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_dich_vu_MaPhieuKhamCls",
                table: "chi_tiet_dich_vu",
                column: "MaPhieuKhamCls");

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_don_thuoc_MaDonThuoc",
                table: "chi_tiet_don_thuoc",
                column: "MaDonThuoc");

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_don_thuoc_MaThuoc",
                table: "chi_tiet_don_thuoc",
                column: "MaThuoc");

            migrationBuilder.CreateIndex(
                name: "IX_dich_vu_y_te_MaPhongThucHien",
                table: "dich_vu_y_te",
                column: "MaPhongThucHien");

            migrationBuilder.CreateIndex(
                name: "IX_don_thuoc_MaBacSiKeDon",
                table: "don_thuoc",
                column: "MaBacSiKeDon");

            migrationBuilder.CreateIndex(
                name: "IX_don_thuoc_MaBenhNhan",
                table: "don_thuoc",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_hang_doi_MaBenhNhan",
                table: "hang_doi",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_hang_doi_MaChiTietDv",
                table: "hang_doi",
                column: "MaChiTietDv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hang_doi_MaPhieuKham",
                table: "hang_doi",
                column: "MaPhieuKham",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hang_doi_MaPhong",
                table: "hang_doi",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_hoa_don_thanh_toan_MaBenhNhan",
                table: "hoa_don_thanh_toan",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_hoa_don_thanh_toan_MaDonThuoc",
                table: "hoa_don_thanh_toan",
                column: "MaDonThuoc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hoa_don_thanh_toan_MaNhanSuThu",
                table: "hoa_don_thanh_toan",
                column: "MaNhanSuThu");

            migrationBuilder.CreateIndex(
                name: "IX_hoa_don_thanh_toan_MaPhieuKham",
                table: "hoa_don_thanh_toan",
                column: "MaPhieuKham",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hoa_don_thanh_toan_MaPhieuKhamCls",
                table: "hoa_don_thanh_toan",
                column: "MaPhieuKhamCls",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ket_qua_dich_vu_MaChiTietDv",
                table: "ket_qua_dich_vu",
                column: "MaChiTietDv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ket_qua_dich_vu_MaNguoiTao",
                table: "ket_qua_dich_vu",
                column: "MaNguoiTao");

            migrationBuilder.CreateIndex(
                name: "IX_lich_hen_kham_MaBenhNhan",
                table: "lich_hen_kham",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_lich_hen_kham_MaLichTruc",
                table: "lich_hen_kham",
                column: "MaLichTruc");

            migrationBuilder.CreateIndex(
                name: "IX_lich_truc_MaPhong",
                table: "lich_truc",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_lich_truc_MaYTaTruc",
                table: "lich_truc",
                column: "MaYTaTruc");

            migrationBuilder.CreateIndex(
                name: "IX_luot_kham_benh_MaHangDoi",
                table: "luot_kham_benh",
                column: "MaHangDoi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_luot_kham_benh_MaNhanSuThucHien",
                table: "luot_kham_benh",
                column: "MaNhanSuThucHien");

            migrationBuilder.CreateIndex(
                name: "IX_luot_kham_benh_MaYTaHoTro",
                table: "luot_kham_benh",
                column: "MaYTaHoTro");

            migrationBuilder.CreateIndex(
                name: "IX_nhan_vien_y_te_MaKhoa",
                table: "nhan_vien_y_te",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_chan_doan_cuoi_MaDonThuoc",
                table: "phieu_chan_doan_cuoi",
                column: "MaDonThuoc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phieu_chan_doan_cuoi_MaPhieuKham",
                table: "phieu_chan_doan_cuoi",
                column: "MaPhieuKham",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kham_can_lam_sang_MaPhieuKhamLs",
                table: "phieu_kham_can_lam_sang",
                column: "MaPhieuKhamLs",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kham_lam_sang_MaBacSiKham",
                table: "phieu_kham_lam_sang",
                column: "MaBacSiKham");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kham_lam_sang_MaBenhNhan",
                table: "phieu_kham_lam_sang",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kham_lam_sang_MaDichVuKham",
                table: "phieu_kham_lam_sang",
                column: "MaDichVuKham");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kham_lam_sang_MaLichHen",
                table: "phieu_kham_lam_sang",
                column: "MaLichHen",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kham_lam_sang_MaNguoiLap",
                table: "phieu_kham_lam_sang",
                column: "MaNguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kham_lam_sang_MaPhieuKqKhamCls",
                table: "phieu_kham_lam_sang",
                column: "MaPhieuKqKhamCls",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phieu_tong_hop_ket_qua_MaNhanSuXuLy",
                table: "phieu_tong_hop_ket_qua",
                column: "MaNhanSuXuLy");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_tong_hop_ket_qua_MaPhieuKhamCls",
                table: "phieu_tong_hop_ket_qua",
                column: "MaPhieuKhamCls",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phong_MaBacSiPhuTrach",
                table: "phong",
                column: "MaBacSiPhuTrach",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phong_MaKhoa",
                table: "phong",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_MaNhanVien_IsTrangThai",
                table: "refresh_token",
                columns: new[] { "MaNhanVien", "IsTrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_Token",
                table: "refresh_token",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_thong_bao_he_thong_MaLuotKham",
                table: "thong_bao_he_thong",
                column: "MaLuotKham");

            migrationBuilder.CreateIndex(
                name: "IX_thong_bao_he_thong_MaPhieuKham",
                table: "thong_bao_he_thong",
                column: "MaPhieuKham");

            migrationBuilder.CreateIndex(
                name: "IX_thong_bao_nguoi_nhan_MaBenhNhan",
                table: "thong_bao_nguoi_nhan",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_thong_bao_nguoi_nhan_MaNhanSu",
                table: "thong_bao_nguoi_nhan",
                column: "MaNhanSu");

            migrationBuilder.CreateIndex(
                name: "IX_thong_bao_nguoi_nhan_MaThongBao",
                table: "thong_bao_nguoi_nhan",
                column: "MaThongBao");

            migrationBuilder.AddForeignKey(
                name: "FK_chi_tiet_dich_vu_phieu_kham_can_lam_sang_MaPhieuKhamCls",
                table: "chi_tiet_dich_vu",
                column: "MaPhieuKhamCls",
                principalTable: "phieu_kham_can_lam_sang",
                principalColumn: "MaPhieuKhamCls",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_hang_doi_phieu_kham_lam_sang_MaPhieuKham",
                table: "hang_doi",
                column: "MaPhieuKham",
                principalTable: "phieu_kham_lam_sang",
                principalColumn: "MaPhieuKham",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_hoa_don_thanh_toan_phieu_kham_can_lam_sang_MaPhieuKhamCls",
                table: "hoa_don_thanh_toan",
                column: "MaPhieuKhamCls",
                principalTable: "phieu_kham_can_lam_sang",
                principalColumn: "MaPhieuKhamCls",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_hoa_don_thanh_toan_phieu_kham_lam_sang_MaPhieuKham",
                table: "hoa_don_thanh_toan",
                column: "MaPhieuKham",
                principalTable: "phieu_kham_lam_sang",
                principalColumn: "MaPhieuKham",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_phieu_chan_doan_cuoi_phieu_kham_lam_sang_MaPhieuKham",
                table: "phieu_chan_doan_cuoi",
                column: "MaPhieuKham",
                principalTable: "phieu_kham_lam_sang",
                principalColumn: "MaPhieuKham",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_phieu_kham_can_lam_sang_phieu_kham_lam_sang_MaPhieuKhamLs",
                table: "phieu_kham_can_lam_sang",
                column: "MaPhieuKhamLs",
                principalTable: "phieu_kham_lam_sang",
                principalColumn: "MaPhieuKham",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_phieu_kham_lam_sang_dich_vu_y_te_MaDichVuKham",
                table: "phieu_kham_lam_sang");

            migrationBuilder.DropForeignKey(
                name: "FK_phieu_tong_hop_ket_qua_phieu_kham_can_lam_sang_MaPhieuKhamCls",
                table: "phieu_tong_hop_ket_qua");

            migrationBuilder.DropTable(
                name: "chi_tiet_don_thuoc");

            migrationBuilder.DropTable(
                name: "hoa_don_thanh_toan");

            migrationBuilder.DropTable(
                name: "ket_qua_dich_vu");

            migrationBuilder.DropTable(
                name: "phieu_chan_doan_cuoi");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "thong_bao_nguoi_nhan");

            migrationBuilder.DropTable(
                name: "kho_thuoc");

            migrationBuilder.DropTable(
                name: "don_thuoc");

            migrationBuilder.DropTable(
                name: "thong_bao_he_thong");

            migrationBuilder.DropTable(
                name: "luot_kham_benh");

            migrationBuilder.DropTable(
                name: "hang_doi");

            migrationBuilder.DropTable(
                name: "chi_tiet_dich_vu");

            migrationBuilder.DropTable(
                name: "dich_vu_y_te");

            migrationBuilder.DropTable(
                name: "phieu_kham_can_lam_sang");

            migrationBuilder.DropTable(
                name: "phieu_kham_lam_sang");

            migrationBuilder.DropTable(
                name: "lich_hen_kham");

            migrationBuilder.DropTable(
                name: "phieu_tong_hop_ket_qua");

            migrationBuilder.DropTable(
                name: "benh_nhan");

            migrationBuilder.DropTable(
                name: "lich_truc");

            migrationBuilder.DropTable(
                name: "phong");

            migrationBuilder.DropTable(
                name: "nhan_vien_y_te");

            migrationBuilder.DropTable(
                name: "khoa_chuyen_mon");
        }
    }
}
