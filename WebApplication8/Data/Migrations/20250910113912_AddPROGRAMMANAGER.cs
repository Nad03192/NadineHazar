using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication8.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPROGRAMMANAGER : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalCredits",
                table: "StudyPrograms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProgramManager",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudyProgramId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramManager", x => new { x.UserId, x.StudyProgramId });
                    table.ForeignKey(
                        name: "FK_ProgramManager_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramManager_StudyPrograms_StudyProgramId",
                        column: x => x.StudyProgramId,
                        principalTable: "StudyPrograms",
                        principalColumn: "StudyProgramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramManager_StudyProgramId",
                table: "ProgramManager",
                column: "StudyProgramId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramManager");

            migrationBuilder.DropColumn(
                name: "TotalCredits",
                table: "StudyPrograms");
        }
    }
}
