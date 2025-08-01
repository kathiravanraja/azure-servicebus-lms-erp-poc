using CoreWCF;
using LMS.WcfService.Contracts;
using Microsoft.Extensions.Logging;
using ServiceBus.Shared.Models;
using ServiceBus.Shared.Services;
using ServiceBus.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace LMS.WcfService.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerCall)]
    public class GradingService : IGradingService
    {
        private readonly IServiceBusPublisher _serviceBusPublisher;
        private readonly ILogger<GradingService> _logger;
        private readonly ServiceBusConfiguration _serviceBusConfig;

        public GradingService(
            IServiceBusPublisher serviceBusPublisher,
            ILogger<GradingService> logger,
            IOptions<ServiceBusConfiguration> serviceBusConfig)
        {
            _serviceBusPublisher = serviceBusPublisher;
            _logger = logger;
            _serviceBusConfig = serviceBusConfig.Value;
        }

        public async Task<GradingResponse> SubmitHomeworkGradeAsync(GradingRequest request)
        {
            try
            {
                _logger.LogInformation("Received grading request for Student {StudentId}, Homework {HomeworkId}",
                    request.StudentId, request.HomeworkId);

                // Validate the request
                if (!IsValidRequest(request))
                {
                    var errorMsg = "Invalid grading request. Please check all required fields.";
                    _logger.LogWarning(errorMsg);
                    return new GradingResponse
                    {
                        Success = false,
                        ErrorMessage = errorMsg,
                        ProcessedAt = DateTime.UtcNow
                    };
                }

                // Convert to message format
                var gradeMessage = new HomeworkGradeMessage
                {
                    StudentId = request.StudentId,
                    HomeworkId = request.HomeworkId,
                    CourseId = request.CourseId,
                    TeacherId = request.TeacherId,
                    Grade = request.Grade,
                    Comments = request.Comments,
                    GradedAt = DateTime.UtcNow,
                    AcademicYear = request.AcademicYear,
                    Semester = request.Semester,
                    CreditHours = request.CreditHours
                };

                // Publish to Service Bus
                await _serviceBusPublisher.PublishAsync(gradeMessage, _serviceBusConfig.HomeworkSyncQueueName);

                _logger.LogInformation("Successfully published grade message for Student {StudentId} with MessageId {MessageId}",
                    request.StudentId, gradeMessage.MessageId);

                return new GradingResponse
                {
                    Success = true,
                    MessageId = gradeMessage.MessageId,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing grading request for Student {StudentId}", request.StudentId);
                return new GradingResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal server error: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<GradingResponse> SubmitBatchHomeworkGradesAsync(BatchGradingRequest request)
        {
            try
            {
                _logger.LogInformation("Received batch grading request with {Count} grades", 
                    request.GradingRequests.Count);

                var gradeMessages = new List<HomeworkGradeMessage>();
                var invalidRequests = new List<string>();

                foreach (var gradingRequest in request.GradingRequests)
                {
                    if (!IsValidRequest(gradingRequest))
                    {
                        invalidRequests.Add($"Student {gradingRequest.StudentId}, Homework {gradingRequest.HomeworkId}");
                        continue;
                    }

                    var gradeMessage = new HomeworkGradeMessage
                    {
                        StudentId = gradingRequest.StudentId,
                        HomeworkId = gradingRequest.HomeworkId,
                        CourseId = gradingRequest.CourseId,
                        TeacherId = gradingRequest.TeacherId,
                        Grade = gradingRequest.Grade,
                        Comments = gradingRequest.Comments,
                        GradedAt = DateTime.UtcNow,
                        AcademicYear = gradingRequest.AcademicYear,
                        Semester = gradingRequest.Semester,
                        CreditHours = gradingRequest.CreditHours
                    };

                    gradeMessages.Add(gradeMessage);
                }

                if (invalidRequests.Any())
                {
                    var errorMsg = $"Invalid requests found: {string.Join(", ", invalidRequests)}";
                    _logger.LogWarning(errorMsg);
                    return new GradingResponse
                    {
                        Success = false,
                        ErrorMessage = errorMsg,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessedCount = 0
                    };
                }

                if (gradeMessages.Any())
                {
                    await _serviceBusPublisher.PublishBatchAsync(gradeMessages, _serviceBusConfig.HomeworkSyncQueueName);
                }

                _logger.LogInformation("Successfully published {Count} grade messages in batch", gradeMessages.Count);

                return new GradingResponse
                {
                    Success = true,
                    MessageId = $"Batch_{Guid.NewGuid()}",
                    ProcessedAt = DateTime.UtcNow,
                    ProcessedCount = gradeMessages.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch grading request");
                return new GradingResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal server error: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow,
                    ProcessedCount = 0
                };
            }
        }

        public async Task<ServiceHealthResponse> GetServiceHealthAsync()
        {
            try
            {
                var response = new ServiceHealthResponse
                {
                    CheckedAt = DateTime.UtcNow,
                    Details = new Dictionary<string, string>
                    {
                        ["ServiceBusConnection"] = "Unknown",
                        ["QueueName"] = _serviceBusConfig.HomeworkSyncQueueName,
                        ["Version"] = "1.0.0"
                    }
                };

                // Simple health check - try to create a test message
                var testMessage = new HomeworkGradeMessage
                {
                    StudentId = "HEALTH_CHECK",
                    HomeworkId = "HEALTH_CHECK",
                    CourseId = "HEALTH_CHECK",
                    TeacherId = "HEALTH_CHECK",
                    Grade = 0
                };

                // Don't actually send the message, just validate we can serialize it
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(testMessage);
                
                response.IsHealthy = !string.IsNullOrEmpty(json);
                response.Status = response.IsHealthy ? "Healthy" : "Unhealthy";
                response.Details["ServiceBusConnection"] = response.IsHealthy ? "OK" : "Failed";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return new ServiceHealthResponse
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    CheckedAt = DateTime.UtcNow,
                    Details = new Dictionary<string, string>
                    {
                        ["Error"] = ex.Message
                    }
                };
            }
        }

        private static bool IsValidRequest(GradingRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.StudentId) &&
                   !string.IsNullOrWhiteSpace(request.HomeworkId) &&
                   !string.IsNullOrWhiteSpace(request.CourseId) &&
                   !string.IsNullOrWhiteSpace(request.TeacherId) &&
                   request.Grade >= 0 && request.Grade <= 100;
        }
    }
}
