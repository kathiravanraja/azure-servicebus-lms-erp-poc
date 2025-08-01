using System.ComponentModel.DataAnnotations;

namespace ServiceBus.Shared.Models
{
    public class HomeworkGradeMessage
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public string HomeworkId { get; set; } = string.Empty;

        [Required]
        public string CourseId { get; set; } = string.Empty;

        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal Grade { get; set; }

        public string? Comments { get; set; }

        public DateTime GradedAt { get; set; }

        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Additional metadata for ERP integration
        public string? AcademicYear { get; set; }
        public string? Semester { get; set; }
        public int? CreditHours { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(StudentId) &&
                   !string.IsNullOrWhiteSpace(HomeworkId) &&
                   !string.IsNullOrWhiteSpace(CourseId) &&
                   !string.IsNullOrWhiteSpace(TeacherId) &&
                   Grade >= 0 && Grade <= 100;
        }
    }
}
