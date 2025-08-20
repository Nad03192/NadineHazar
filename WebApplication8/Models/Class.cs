using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication8.Models
{
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

        public string UserId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public Shift? Shift { get; set; } = null;

        public IdentityUser? User { get; set; } = null;
    }

    public class LoadedTime
    {
        [Key] // This marks UserId as the primary key
        public string UserId { get; set; }

        public int HoursPerWeek { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }
    }

    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public int CreditNumber { get; set; }

        public List<Class> Classes { get; set; } = new List<Class>();
    }

    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public int Capacity { get; set; }

        public List<Class> Classes { get; set; } = new List<Class>();
    }

    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        public string UserId { get; set; }

        public int CourseId { get; set; }

        public int RoomId { get; set; }

        public TimeSpan StartTime { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public IdentityUser? User { get; set; } = null;

        public Course? Course { get; set; } = null;

        public Room? Room { get; set; } = null;
    }
    public class InstructorCourse
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public int CourseId { get; set; }

        public IdentityUser? User { get; set; }
        public Course? Course { get; set; }
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
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
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

        // ✅ NEW: List of selected days for custom shift
        public List<DayOfWeek> CustomShiftDays { get; set; } = new();
    }


}
