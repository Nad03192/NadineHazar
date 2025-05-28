using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication8.Data.Migrations
{
    /// <inheritdoc />
    public partial class ns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LoadedTimes",
                table: "LoadedTimes");

            migrationBuilder.DropIndex(
                name: "IX_LoadedTimes_UserId",
                table: "LoadedTimes");

            migrationBuilder.DropColumn(
                name: "LoadedTimeId",
                table: "LoadedTimes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LoadedTimes",
                table: "LoadedTimes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LoadedTimes",
                table: "LoadedTimes");

            migrationBuilder.AddColumn<int>(
                name: "LoadedTimeId",
                table: "LoadedTimes",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LoadedTimes",
                table: "LoadedTimes",
                column: "LoadedTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_LoadedTimes_UserId",
                table: "LoadedTimes",
                column: "UserId");
        }
    }
}
