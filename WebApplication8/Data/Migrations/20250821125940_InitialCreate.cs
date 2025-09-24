using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication8.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_AspNetUsers_UserId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Rooms_RoomId",
                table: "Classes");

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "Classes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "CoursePrerequisites",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    PrerequisiteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePrerequisites", x => new { x.CourseId, x.PrerequisiteId });
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_PrerequisiteId",
                        column: x => x.PrerequisiteId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseTypes",
                columns: table => new
                {
                    CourseTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTypes", x => x.CourseTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.FacultyId);
                });

            migrationBuilder.CreateTable(
                name: "GradeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Coefficient = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeDefinitions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    SemesterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrollmentStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrollmentEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddDropStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddDropEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmitClassesEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmitGradesEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.SemesterId);
                });

            migrationBuilder.CreateTable(
                name: "StudyPrograms",
                columns: table => new
                {
                    StudyProgramId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPrograms", x => x.StudyProgramId);
                    table.ForeignKey(
                        name: "FK_StudyPrograms_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "FacultyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    GradeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGrades_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentGrades_GradeDefinitions_GradeDefinitionId",
                        column: x => x.GradeDefinitionId,
                        principalTable: "GradeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramCourses",
                columns: table => new
                {
                    StudyProgramId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CourseTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramCourses", x => new { x.StudyProgramId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_ProgramCourses_CourseTypes_CourseTypeId",
                        column: x => x.CourseTypeId,
                        principalTable: "CourseTypes",
                        principalColumn: "CourseTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramCourses_StudyPrograms_StudyProgramId",
                        column: x => x.StudyProgramId,
                        principalTable: "StudyPrograms",
                        principalColumn: "StudyProgramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPrograms",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudyProgramId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPrograms", x => new { x.UserId, x.StudyProgramId });
                    table.ForeignKey(
                        name: "FK_UserPrograms_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPrograms_StudyPrograms_StudyProgramId",
                        column: x => x.StudyProgramId,
                        principalTable: "StudyPrograms",
                        principalColumn: "StudyProgramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_SemesterId",
                table: "Classes",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_PrerequisiteId",
                table: "CoursePrerequisites",
                column: "PrerequisiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassId",
                table: "Enrollments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId",
                table: "Enrollments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDefinitions_CourseId",
                table: "GradeDefinitions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramCourses_CourseId",
                table: "ProgramCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramCourses_CourseTypeId",
                table: "ProgramCourses",
                column: "CourseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_EnrollmentId",
                table: "StudentGrades",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_GradeDefinitionId",
                table: "StudentGrades",
                column: "GradeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPrograms_FacultyId",
                table: "StudyPrograms",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrograms_StudyProgramId",
                table: "UserPrograms",
                column: "StudyProgramId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_AspNetUsers_UserId",
                table: "Classes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Rooms_RoomId",
                table: "Classes",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Semesters_SemesterId",
                table: "Classes",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "SemesterId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_AspNetUsers_UserId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Rooms_RoomId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Semesters_SemesterId",
                table: "Classes");

            migrationBuilder.DropTable(
                name: "CoursePrerequisites");

            migrationBuilder.DropTable(
                name: "ProgramCourses");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "StudentGrades");

            migrationBuilder.DropTable(
                name: "UserPrograms");

            migrationBuilder.DropTable(
                name: "CourseTypes");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "GradeDefinitions");

            migrationBuilder.DropTable(
                name: "StudyPrograms");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropIndex(
                name: "IX_Classes_SemesterId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Classes");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_AspNetUsers_UserId",
                table: "Classes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Rooms_RoomId",
                table: "Classes",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
