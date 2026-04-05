using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCare.Migrations
{
    /// <inheritdoc />
    public partial class SplitUserFromStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenDangNhap",
                table: "nhan_vien_y_te");

            migrationBuilder.DropColumn(
                name: "MatKhauHash",
                table: "nhan_vien_y_te");

            migrationBuilder.DropColumn(
                name: "VaiTro",
                table: "nhan_vien_y_te");

            migrationBuilder.DropColumn(
                name: "LoaiYTa",
                table: "nhan_vien_y_te");

            migrationBuilder.DropColumn(
                name: "ChucVu",
                table: "nhan_vien_y_te");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenDangNhap",
                table: "nhan_vien_y_te",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MatKhauHash",
                table: "nhan_vien_y_te",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VaiTro",
                table: "nhan_vien_y_te",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LoaiYTa",
                table: "nhan_vien_y_te",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChucVu",
                table: "nhan_vien_y_te",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
