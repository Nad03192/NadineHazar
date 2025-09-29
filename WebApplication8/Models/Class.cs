using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication8.Models
{
    // -------------------- CAMPUS --------------------
    public class Campus
    {
        [Key]
        public int CampusId { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        // Navigation properties
        public List<Building> Buildings { get; set; } = new List<Building>();
        public List<UserCampus> UserCampuses { get; set; } = new List<UserCampus>();
    }


    [Index(nameof(Name), IsUnique = true)]
    public class Building
    {
        [Key]
        public int BuildingId { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        // Campus relation
        public int CampusId { get; set; }
        public Campus? Campus { get; set; }

        // Rooms in this building
        public List<Room> Rooms { get; set; } = new List<Room>();
    }
    public class UserCampus
    {
        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public int CampusId { get; set; }
        public Campus? Campus { get; set; }
    }
    // -------------------- SEMESTER --------------------
    [Index(nameof(Name), IsUnique = true)]
    public class Semester
    {
        [Key]
        public int SemesterId { get; set; }

        [Required]
        public string Name { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime EnrollmentStart { get; set; }
        public DateTime EnrollmentEnd { get; set; }

        public DateTime AddDropStart { get; set; }
        public DateTime AddDropEnd { get; set; }

        public DateTime SubmitClassesEnd { get; set; }
        public DateTime SubmitGradesEnd { get; set; }

        public List<Class> Classes { get; set; } = new List<Class>();
    }

    // -------------------- SHIFT & AVAILABILITY --------------------
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public List<Availability> Availabilities { get; set; } = new List<Availability>();
    }

    public class Availability
    {
        [Key]
        public int AvailabilityId { get; set; }

        public int ShiftId { get; set; }
        public Shift? Shift { get; set; }

        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
    }

    public class LoadedTime
    {
        [Key]
        public string UserId { get; set; }
        public int HoursPerWeek { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }
    }

    // -------------------- COURSE --------------------
    [Index(nameof(Name), IsUnique = true)]
    public class Course
    {
        [Key]
        public int CourseId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int CreditNumber { get; set; }

        public List<Class> Classes { get; set; } = new List<Class>();

        // Prerequisites
  
        public List<CoursePrerequisite> Prerequisites { get; set; } = new();
      
        public List<CoursePrerequisite> IsPrerequisiteFor { get; set; } = new();

        public List<GradeDefinition> GradeDefinitions { get; set; } = new List<GradeDefinition>();
        public List<InstructorCourse> InstructorCourses { get; set; } = new List<InstructorCourse>();
        public List<ProgramCourse> ProgramCourses { get; set; } = new List<ProgramCourse>();
    }

    public class CoursePrerequisite
    {
        public int CourseId { get; set; }
        public int PrerequisiteId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        [ForeignKey(nameof(PrerequisiteId))]
        public Course? Prerequisite { get; set; }
    }


    [Index(nameof(Name), IsUnique = true)]
    public class CourseType
    {
        [Key]
        public int CourseTypeId { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }

        public List<ProgramCourse> ProgramCourses { get; set; } = new();
    }

    // -------------------- ROOM --------------------
    [Index(nameof(Name), IsUnique = true)]
    public class Room
    {
        [Key]
        public int RoomId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int Capacity { get; set; }
        public int BuildingId { get; set; }
        public Building? Building { get; set; }
        public List<Class> Classes { get; set; } = new List<Class>();
    }

    // -------------------- CLASS --------------------
    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }

        public TimeSpan StartTime { get; set; }
        public DayOfWeek DayOfWeek { get; set; }

        public int SemesterId { get; set; }
        public Semester? Semester { get; set; }

        public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }

    // -------------------- INSTRUCTOR COURSES --------------------
    public class InstructorCourse
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }
    }

    // -------------------- FACULTY & PROGRAM --------------------
    [Index(nameof(Name), IsUnique = true)]
    public class Faculty
    {
        [Key]
        public int FacultyId { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }

        public List<StudyProgram> StudyPrograms { get; set; } = new List<StudyProgram>();
    }
    [Index(nameof(Name), IsUnique = true)]
    public class StudyProgram
    {
        [Key]
        public int StudyProgramId { get; set; }
        [Required]
        public string Name { get; set; }

        public int FacultyId { get; set; }

        [Required]
        public int TotalCredits { get; set; }
        public Faculty? Faculty { get; set; }

        public List<ProgramCourse> ProgramCourses { get; set; } = new List<ProgramCourse>();
        public List<UserProgram> UserPrograms { get; set; } = new List<UserProgram>();
    }

    public class ProgramCourse
    {
        public int StudyProgramId { get; set; }
        public StudyProgram? StudyProgram { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public int CourseTypeId { get; set; }
        public CourseType? CourseType { get; set; }
    }

    public class UserProgram
    {
        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public int StudyProgramId { get; set; }
        public StudyProgram? StudyProgram { get; set; }
    }

    // -------------------- ENROLLMENT & GRADES --------------------
    public class Enrollment
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public int ClassId { get; set; }
        public Class? Class { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        public List<StudentGrade> Grades { get; set; } = new List<StudentGrade>();
    }

    public class GradeDefinition
    {
        [Key]
        public int Id { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Required]
        public string Name { get; set; }

        [Range(0, 1)]
        public double Coefficient { get; set; }
    }

    public class StudentGrade
    {
        [Key]
        public int Id { get; set; }

        public int EnrollmentId { get; set; }
        public Enrollment? Enrollment { get; set; }

        public int GradeDefinitionId { get; set; }
        public GradeDefinition? GradeDefinition { get; set; }

        [Range(0, 100)]
        public double? Score { get; set; }
    }


    // -------------------- PROGRAM MANAGER --------------------
    public class ProgramManager
    {
        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public int StudyProgramId { get; set; }
        public StudyProgram? StudyProgram { get; set; }
    }


    // -------------------- VIEW MODELS --------------------
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
    }
    public class GeneratedClassViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }

        public int RoomId { get; set; }
        public string RoomName { get; set; }

        public TimeSpan StartTime { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
    }

    public class ScheduleRequest
    {
        public List<GeneratedClassViewModel> Schedule { get; set; }
        public string InstructorId { get; set; }
    }

    public class RegisterUserViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
    }


    public class DayShiftSelection
    {
        public DayOfWeek Day { get; set; }
        public List<int> SelectedShiftIds { get; set; } = new List<int>();
        public List<Shift>? AvailableShifts { get; set; }
    }

    public class AvailabilityFormViewModel
    {
        public List<DayShiftSelection> WeekAvailability { get; set; } = new();
        public TimeSpan? CustomStart { get; set; }
        public TimeSpan? CustomEnd { get; set; }
        public List<DayOfWeek> CustomShiftDays { get; set; } = new();
    }

    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
