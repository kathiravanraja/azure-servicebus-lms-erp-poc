using LMS.WcfService.Services;
using LMS.WcfService.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceBus.Shared.Configuration;
using ServiceBus.Shared.Services;
using ServiceBus.Shared.Models;
using Xunit;
using Moq;

namespace Integration.Tests
{
    public class GradingServiceTests
    {
        private readonly Mock<IServiceBusPublisher> _mockPublisher;
        private readonly Mock<ILogger<GradingService>> _mockLogger;
        private readonly Mock<IOptions<ServiceBusConfiguration>> _mockConfig;
        private readonly GradingService _gradingService;

        public GradingServiceTests()
        {
            _mockPublisher = new Mock<IServiceBusPublisher>();
            _mockLogger = new Mock<ILogger<GradingService>>();
            _mockConfig = new Mock<IOptions<ServiceBusConfiguration>>();

            var config = new ServiceBusConfiguration
            {
                HomeworkSyncQueueName = "homework-sync-test"
            };
            _mockConfig.Setup(x => x.Value).Returns(config);

            _gradingService = new GradingService(_mockPublisher.Object, _mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task SubmitHomeworkGradeAsync_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new GradingRequest
            {
                StudentId = "STU001",
                HomeworkId = "HW001",
                CourseId = "CS101",
                TeacherId = "TCH001",
                Grade = 85.5m,
                Comments = "Good work!"
            };

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>()))
                         .Returns(Task.CompletedTask);

            // Act
            var result = await _gradingService.SubmitHomeworkGradeAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.MessageId);
            Assert.Equal(1, result.ProcessedCount);
            _mockPublisher.Verify(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SubmitHomeworkGradeAsync_InvalidRequest_ShouldReturnFailure()
        {
            // Arrange
            var request = new GradingRequest
            {
                StudentId = "", // Invalid - empty student ID
                HomeworkId = "HW001",
                CourseId = "CS101",
                TeacherId = "TCH001",
                Grade = 85.5m
            };

            // Act
            var result = await _gradingService.SubmitHomeworkGradeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            _mockPublisher.Verify(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SubmitBatchHomeworkGradesAsync_ValidRequests_ShouldReturnSuccess()
        {
            // Arrange
            var request = new BatchGradingRequest
            {
                GradingRequests = new List<GradingRequest>
                {
                    new GradingRequest
                    {
                        StudentId = "STU001",
                        HomeworkId = "HW001",
                        CourseId = "CS101",
                        TeacherId = "TCH001",
                        Grade = 85.5m
                    },
                    new GradingRequest
                    {
                        StudentId = "STU002",
                        HomeworkId = "HW001",
                        CourseId = "CS101",
                        TeacherId = "TCH001",
                        Grade = 92.0m
                    }
                }
            };

            _mockPublisher.Setup(x => x.PublishBatchAsync(It.IsAny<IEnumerable<HomeworkGradeMessage>>(), It.IsAny<string>()))
                         .Returns(Task.CompletedTask);

            // Act
            var result = await _gradingService.SubmitBatchHomeworkGradesAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.ProcessedCount);
            _mockPublisher.Verify(x => x.PublishBatchAsync(It.IsAny<IEnumerable<HomeworkGradeMessage>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetServiceHealthAsync_ShouldReturnHealthStatus()
        {
            // Act
            var result = await _gradingService.GetServiceHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy);
            Assert.Equal("Healthy", result.Status);
            Assert.True(result.Details.ContainsKey("QueueName"));
        }

        [Theory]
        [InlineData("", "HW001", "CS101", "TCH001", 85.5, false)] // Empty StudentId
        [InlineData("STU001", "", "CS101", "TCH001", 85.5, false)] // Empty HomeworkId
        [InlineData("STU001", "HW001", "", "TCH001", 85.5, false)] // Empty CourseId
        [InlineData("STU001", "HW001", "CS101", "", 85.5, false)] // Empty TeacherId
        [InlineData("STU001", "HW001", "CS101", "TCH001", -1, false)] // Invalid Grade (negative)
        [InlineData("STU001", "HW001", "CS101", "TCH001", 101, false)] // Invalid Grade (over 100)
        [InlineData("STU001", "HW001", "CS101", "TCH001", 85.5, true)] // Valid request
        public async Task SubmitHomeworkGradeAsync_VariousInputs_ShouldReturnExpectedResult(
            string studentId, string homeworkId, string courseId, string teacherId, decimal grade, bool expectedSuccess)
        {
            // Arrange
            var request = new GradingRequest
            {
                StudentId = studentId,
                HomeworkId = homeworkId,
                CourseId = courseId,
                TeacherId = teacherId,
                Grade = grade
            };

            if (expectedSuccess)
            {
                _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>()))
                             .Returns(Task.CompletedTask);
            }

            // Act
            var result = await _gradingService.SubmitHomeworkGradeAsync(request);

            // Assert
            Assert.Equal(expectedSuccess, result.Success);
        }
    }
}
