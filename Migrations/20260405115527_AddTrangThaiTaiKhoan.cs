using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCare.Migrations
{
    /// <inheritdoc />
    public partial class AddTrangThaiTaiKhoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoiDungKetQua",
                table: "ket_qua_dich_vu");

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiTaiKhoan",
                table: "nhan_vien_y_te",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThaiTaiKhoan",
                table: "nhan_vien_y_te");

            migrationBuilder.AddColumn<string>(
                name: "NoiDungKetQua",
                table: "ket_qua_dich_vu",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
