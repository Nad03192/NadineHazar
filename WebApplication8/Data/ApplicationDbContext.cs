using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Models;

namespace WebApplication8.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core entities
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Availability> Availabilities { get; set; }
        public DbSet<LoadedTime> LoadedTimes { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<InstructorCourse> InstructorCourses { get; set; }

        // Faculty / Program
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<StudyProgram> StudyPrograms { get; set; }
        public DbSet<ProgramCourse> ProgramCourses { get; set; }
        public DbSet<UserProgram> UserPrograms { get; set; }

        // Prerequisites
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }

        // Enrollment / Grades
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<GradeDefinition> GradeDefinitions { get; set; }
        public DbSet<StudentGrade> StudentGrades { get; set; }

        // Course type
        public DbSet<CourseType> CourseTypes { get; set; }

        // Semester
        public DbSet<Semester> Semesters { get; set; }
        // user campuses
        public DbSet<UserCampus> UserCampuses { get; set; }
        public DbSet<Campus> Campuses { get; set; }
        public DbSet<Building> Buildings { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------- USER CAMPUS --------------------
            modelBuilder.Entity<UserCampus>()
                .HasKey(uc => new { uc.UserId, uc.CampusId });

            // Campus -> Buildings
            modelBuilder.Entity<Building>()
                .HasOne(b => b.Campus)
                .WithMany(c => c.Buildings)
                .HasForeignKey(b => b.CampusId)
                .OnDelete(DeleteBehavior.Cascade);

            // Building -> Rooms
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Building)
                .WithMany(b => b.Rooms)
                .HasForeignKey(r => r.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserCampus relation
            modelBuilder.Entity<UserCampus>()
                .HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCampus>()
                .HasOne(uc => uc.Campus)
                .WithMany(c => c.UserCampuses)
                .HasForeignKey(uc => uc.CampusId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------- COURSE PREREQUISITES --------------------
            modelBuilder.Entity<CoursePrerequisite>()
                .HasKey(cp => new { cp.CourseId, cp.PrerequisiteId });

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.Course)
                .WithMany(c => c.Prerequisites)
                .HasForeignKey(cp => cp.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.Prerequisite)
                .WithMany(c => c.IsPrerequisiteFor)
                .HasForeignKey(cp => cp.PrerequisiteId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------- PROGRAM COURSE --------------------
            modelBuilder.Entity<ProgramCourse>()
                .HasKey(pc => new { pc.StudyProgramId, pc.CourseId });

            modelBuilder.Entity<ProgramCourse>()
                .HasOne(pc => pc.StudyProgram)
                .WithMany(sp => sp.ProgramCourses)
                .HasForeignKey(pc => pc.StudyProgramId);

            modelBuilder.Entity<ProgramCourse>()
                .HasOne(pc => pc.Course)
                .WithMany(c => c.ProgramCourses)
                .HasForeignKey(pc => pc.CourseId);

            modelBuilder.Entity<ProgramCourse>()
                .HasOne(pc => pc.CourseType)
                .WithMany(ct => ct.ProgramCourses)
                .HasForeignKey(pc => pc.CourseTypeId);

            // -------------------- USER PROGRAM --------------------
            modelBuilder.Entity<UserProgram>()
                .HasKey(up => new { up.UserId, up.StudyProgramId });

            modelBuilder.Entity<UserProgram>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserProgram>()
                .HasOne(up => up.StudyProgram)
                .WithMany(sp => sp.UserPrograms)
                .HasForeignKey(up => up.StudyProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------- ENROLLMENT --------------------
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Class)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict); // prevent multiple cascade paths

            // -------------------- STUDENT GRADE --------------------
        
            modelBuilder.Entity<StudentGrade>()
                .HasOne(sg => sg.Enrollment)
                .WithMany(e => e.Grades)
                .HasForeignKey(sg => sg.EnrollmentId);

            modelBuilder.Entity<StudentGrade>()
                .HasOne(sg => sg.GradeDefinition)
                .WithMany()
                .HasForeignKey(sg => sg.GradeDefinitionId);

            // Enforce uniqueness: One Enrollment + GradeDefinition can only have one grade
            modelBuilder.Entity<StudentGrade>()
                .HasIndex(sg => new { sg.EnrollmentId, sg.GradeDefinitionId })
                .IsUnique();


            // -------------------- INSTRUCTOR COURSE --------------------
            modelBuilder.Entity<InstructorCourse>()
                .HasOne(ic => ic.User)
                .WithMany()
                .HasForeignKey(ic => ic.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InstructorCourse>()
                .HasOne(ic => ic.Course)
                .WithMany(c => c.InstructorCourses)
                .HasForeignKey(ic => ic.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------- CLASS --------------------
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Course)
                .WithMany(cu => cu.Classes)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.Room)
                .WithMany(r => r.Classes)
                .HasForeignKey(c => c.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.Semester)
                .WithMany(s => s.Classes)
                .HasForeignKey(c => c.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------- SHIFT & AVAILABILITY --------------------
            modelBuilder.Entity<Availability>()
                .HasOne(a => a.Shift)
                .WithMany(s => s.Availabilities)
                .HasForeignKey(a => a.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Availability>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoadedTime>()
                .HasOne(lt => lt.User)
                .WithMany()
                .HasForeignKey(lt => lt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------- OPTIONAL: FACULTY & PROGRAM --------------------
            modelBuilder.Entity<StudyProgram>()
                .HasOne(sp => sp.Faculty)
                .WithMany(f => f.StudyPrograms)
                .HasForeignKey(sp => sp.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);
            // -------------------- OPTIONAL: PROGRAM MANAGER--------------------
            modelBuilder.Entity<ProgramManager>()
    .HasKey(pm => new { pm.UserId, pm.StudyProgramId });

        }

        public DbSet<WebApplication8.Models.ProgramManager> ProgramManager { get; set; } = default!;



    }
}
