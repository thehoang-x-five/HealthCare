using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCare.Migrations
{
    /// <inheritdoc />
    public partial class AddMaKTVPhuTrachToPhong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaKTVPhuTrach",
                table: "phong",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_phong_MaKTVPhuTrach",
                table: "phong",
                column: "MaKTVPhuTrach",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_phong_nhan_vien_y_te_MaKTVPhuTrach",
                table: "phong",
                column: "MaKTVPhuTrach",
                principalTable: "nhan_vien_y_te",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_phong_nhan_vien_y_te_MaKTVPhuTrach",
                table: "phong");

            migrationBuilder.DropIndex(
                name: "IX_phong_MaKTVPhuTrach",
                table: "phong");

            migrationBuilder.DropColumn(
                name: "MaKTVPhuTrach",
                table: "phong");
        }
    }
}
