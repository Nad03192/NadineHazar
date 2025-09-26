using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication8.Data.Migrations
{
    /// <inheritdoc />
    public partial class gradesss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentGrades_EnrollmentId",
                table: "StudentGrades");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_EnrollmentId_GradeDefinitionId",
                table: "StudentGrades",
                columns: new[] { "EnrollmentId", "GradeDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentGrades_EnrollmentId_GradeDefinitionId",
                table: "StudentGrades");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_EnrollmentId",
                table: "StudentGrades",
                column: "EnrollmentId");
        }
    }
}
