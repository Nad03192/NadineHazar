using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication8.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueToSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Semesters",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_Name",
                table: "Semesters",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Semesters_Name",
                table: "Semesters");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Semesters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
