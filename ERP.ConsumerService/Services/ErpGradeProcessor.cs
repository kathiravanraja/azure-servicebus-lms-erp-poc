using ServiceBus.Shared.Models;

namespace ERP.ConsumerService.Services
{
    public interface IErpGradeProcessor
    {
        Task<bool> ProcessHomeworkGradeAsync(HomeworkGradeMessage gradeMessage);
        Task<bool> ValidateStudentExistsAsync(string studentId);
        Task<bool> ValidateCourseExistsAsync(string courseId);
        Task<bool> SaveGradeToErpAsync(HomeworkGradeMessage gradeMessage);
    }

    public class ErpGradeProcessor : IErpGradeProcessor
    {
        private readonly ILogger<ErpGradeProcessor> _logger;
        private readonly Dictionary<string, StudentRecord> _mockStudentDatabase;
        private readonly Dictionary<string, CourseRecord> _mockCourseDatabase;
        private readonly List<GradeRecord> _mockGradeDatabase;

        public ErpGradeProcessor(ILogger<ErpGradeProcessor> logger)
        {
            _logger = logger;
            _mockStudentDatabase = InitializeMockStudents();
            _mockCourseDatabase = InitializeMockCourses();
            _mockGradeDatabase = new List<GradeRecord>();
        }

        public async Task<bool> ProcessHomeworkGradeAsync(HomeworkGradeMessage gradeMessage)
        {
            try
            {
                _logger.LogInformation("Processing homework grade for Student {StudentId}, Course {CourseId}, Grade {Grade}",
                    gradeMessage.StudentId, gradeMessage.CourseId, gradeMessage.Grade);

                // Validate message
                if (!gradeMessage.IsValid())
                {
                    _logger.LogWarning("Invalid grade message received: {MessageId}", gradeMessage.MessageId);
                    return false;
                }

                // Validate student exists
                if (!await ValidateStudentExistsAsync(gradeMessage.StudentId))
                {
                    _logger.LogWarning("Student {StudentId} not found in ERP system", gradeMessage.StudentId);
                    return false;
                }

                // Validate course exists
                if (!await ValidateCourseExistsAsync(gradeMessage.CourseId))
                {
                    _logger.LogWarning("Course {CourseId} not found in ERP system", gradeMessage.CourseId);
                    return false;
                }

                // Save grade to ERP
                var success = await SaveGradeToErpAsync(gradeMessage);
                if (success)
                {
                    _logger.LogInformation("Successfully processed grade for Student {StudentId}, Homework {HomeworkId}",
                        gradeMessage.StudentId, gradeMessage.HomeworkId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing homework grade for Student {StudentId}", gradeMessage.StudentId);
                return false;
            }
        }

        public async Task<bool> ValidateStudentExistsAsync(string studentId)
        {
            // Simulate async database call
            await Task.Delay(50);
            
            var exists = _mockStudentDatabase.ContainsKey(studentId);
            _logger.LogDebug("Student {StudentId} validation result: {Exists}", studentId, exists);
            return exists;
        }

        public async Task<bool> ValidateCourseExistsAsync(string courseId)
        {
            // Simulate async database call
            await Task.Delay(50);
            
            var exists = _mockCourseDatabase.ContainsKey(courseId);
            _logger.LogDebug("Course {CourseId} validation result: {Exists}", courseId, exists);
            return exists;
        }

        public async Task<bool> SaveGradeToErpAsync(HomeworkGradeMessage gradeMessage)
        {
            try
            {
                // Simulate saving to ERP database
                await Task.Delay(100);

                var gradeRecord = new GradeRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = gradeMessage.StudentId,
                    CourseId = gradeMessage.CourseId,
                    HomeworkId = gradeMessage.HomeworkId,
                    TeacherId = gradeMessage.TeacherId,
                    Grade = gradeMessage.Grade,
                    Comments = gradeMessage.Comments,
                    GradedAt = gradeMessage.GradedAt,
                    ProcessedAt = DateTime.UtcNow,
                    MessageId = gradeMessage.MessageId,
                    AcademicYear = gradeMessage.AcademicYear,
                    Semester = gradeMessage.Semester,
                    CreditHours = gradeMessage.CreditHours
                };

                _mockGradeDatabase.Add(gradeRecord);

                _logger.LogInformation("Grade saved to ERP: Student {StudentId}, Course {CourseId}, Grade {Grade}, Record ID {RecordId}",
                    gradeMessage.StudentId, gradeMessage.CourseId, gradeMessage.Grade, gradeRecord.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save grade to ERP for Student {StudentId}", gradeMessage.StudentId);
                return false;
            }
        }

        private static Dictionary<string, StudentRecord> InitializeMockStudents()
        {
            return new Dictionary<string, StudentRecord>
            {
                ["STU001"] = new StudentRecord { Id = "STU001", Name = "John Doe", Email = "john.doe@university.edu" },
                ["STU002"] = new StudentRecord { Id = "STU002", Name = "Jane Smith", Email = "jane.smith@university.edu" },
                ["STU003"] = new StudentRecord { Id = "STU003", Name = "Mike Johnson", Email = "mike.johnson@university.edu" },
                ["STU004"] = new StudentRecord { Id = "STU004", Name = "Sarah Wilson", Email = "sarah.wilson@university.edu" },
                ["STU005"] = new StudentRecord { Id = "STU005", Name = "David Brown", Email = "david.brown@university.edu" }
            };
        }

        private static Dictionary<string, CourseRecord> InitializeMockCourses()
        {
            return new Dictionary<string, CourseRecord>
            {
                ["CS101"] = new CourseRecord { Id = "CS101", Name = "Introduction to Computer Science", CreditHours = 3 },
                ["CS201"] = new CourseRecord { Id = "CS201", Name = "Data Structures and Algorithms", CreditHours = 4 },
                ["CS301"] = new CourseRecord { Id = "CS301", Name = "Software Engineering", CreditHours = 3 },
                ["MATH101"] = new CourseRecord { Id = "MATH101", Name = "Calculus I", CreditHours = 4 },
                ["MATH201"] = new CourseRecord { Id = "MATH201", Name = "Linear Algebra", CreditHours = 3 }
            };
        }
    }

    public class StudentRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CourseRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CreditHours { get; set; }
    }

    public class GradeRecord
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string HomeworkId { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public decimal Grade { get; set; }
        public string? Comments { get; set; }
        public DateTime GradedAt { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public string? AcademicYear { get; set; }
        public string? Semester { get; set; }
        public int? CreditHours { get; set; }
    }
}
