using ERP.ConsumerService.Services;
using Microsoft.Extensions.Logging;
using ServiceBus.Shared.Models;
using Xunit;

namespace Integration.Tests
{
    public class ErpGradeProcessorTests
    {
        private readonly ErpGradeProcessor _processor;
        private readonly ILogger<ErpGradeProcessor> _logger;

        public ErpGradeProcessorTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<ErpGradeProcessor>();
            _processor = new ErpGradeProcessor(_logger);
        }

        [Fact]
        public async Task ProcessHomeworkGradeAsync_ValidMessage_ShouldReturnTrue()
        {
            // Arrange
            var gradeMessage = new HomeworkGradeMessage
            {
                StudentId = "STU001",
                HomeworkId = "HW001",
                CourseId = "CS101",
                TeacherId = "TCH001",
                Grade = 85.5m,
                Comments = "Good work!",
                GradedAt = DateTime.UtcNow
            };

            // Act
            var result = await _processor.ProcessHomeworkGradeAsync(gradeMessage);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ProcessHomeworkGradeAsync_InvalidStudent_ShouldReturnFalse()
        {
            // Arrange
            var gradeMessage = new HomeworkGradeMessage
            {
                StudentId = "INVALID_STUDENT",
                HomeworkId = "HW001",
                CourseId = "CS101",
                TeacherId = "TCH001",
                Grade = 85.5m,
                GradedAt = DateTime.UtcNow
            };

            // Act
            var result = await _processor.ProcessHomeworkGradeAsync(gradeMessage);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ProcessHomeworkGradeAsync_InvalidCourse_ShouldReturnFalse()
        {
            // Arrange
            var gradeMessage = new HomeworkGradeMessage
            {
                StudentId = "STU001",
                HomeworkId = "HW001",
                CourseId = "INVALID_COURSE",
                TeacherId = "TCH001",
                Grade = 85.5m,
                GradedAt = DateTime.UtcNow
            };

            // Act
            var result = await _processor.ProcessHomeworkGradeAsync(gradeMessage);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("STU001", true)]
        [InlineData("STU002", true)]
        [InlineData("STU999", false)]
        [InlineData("", false)]
        public async Task ValidateStudentExistsAsync_VariousStudentIds_ShouldReturnExpectedResult(string studentId, bool expected)
        {
            // Act
            var result = await _processor.ValidateStudentExistsAsync(studentId);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("CS101", true)]
        [InlineData("CS201", true)]
        [InlineData("INVALID", false)]
        [InlineData("", false)]
        public async Task ValidateCourseExistsAsync_VariousCourseIds_ShouldReturnExpectedResult(string courseId, bool expected)
        {
            // Act
            var result = await _processor.ValidateCourseExistsAsync(courseId);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SaveGradeToErpAsync_ValidMessage_ShouldReturnTrue()
        {
            // Arrange
            var gradeMessage = new HomeworkGradeMessage
            {
                StudentId = "STU001",
                HomeworkId = "HW001",
                CourseId = "CS101",
                TeacherId = "TCH001",
                Grade = 85.5m,
                Comments = "Good work!",
                GradedAt = DateTime.UtcNow
            };

            // Act
            var result = await _processor.SaveGradeToErpAsync(gradeMessage);

            // Assert
            Assert.True(result);
        }
    }
}
