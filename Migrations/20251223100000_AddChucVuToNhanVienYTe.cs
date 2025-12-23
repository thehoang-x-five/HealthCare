using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCare.Migrations
{
    /// <inheritdoc />
    public partial class AddChucVuToNhanVienYTe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChucVu",
                table: "nhan_vien_y_te",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "bac_si")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Update existing records based on VaiTro and LoaiYTa
            migrationBuilder.Sql(@"
                UPDATE nhan_vien_y_te 
                SET ChucVu = CASE 
                    WHEN VaiTro = 'bac_si' THEN 'bac_si'
                    WHEN VaiTro = 'y_ta' AND LoaiYTa = 'hanhchinh' THEN 'y_ta_hanh_chinh'
                    WHEN VaiTro = 'y_ta' AND LoaiYTa = 'ls' THEN 'y_ta_phong_kham'
                    WHEN VaiTro = 'y_ta' AND LoaiYTa = 'cls' THEN 'ky_thuat_vien'
                    ELSE 'bac_si'
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChucVu",
                table: "nhan_vien_y_te");
        }
    }
}
