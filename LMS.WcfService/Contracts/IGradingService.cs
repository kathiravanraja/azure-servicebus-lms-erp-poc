using System.Runtime.Serialization;
using CoreWCF;

namespace LMS.WcfService.Contracts
{
    [ServiceContract]
    public interface IGradingService
    {
        [OperationContract]
        Task<GradingResponse> SubmitHomeworkGradeAsync(GradingRequest request);

        [OperationContract]
        Task<GradingResponse> SubmitBatchHomeworkGradesAsync(BatchGradingRequest request);

        [OperationContract]
        Task<ServiceHealthResponse> GetServiceHealthAsync();
    }

    [DataContract]
    public class GradingRequest
    {
        [DataMember]
        public string StudentId { get; set; } = string.Empty;

        [DataMember]
        public string HomeworkId { get; set; } = string.Empty;

        [DataMember]
        public string CourseId { get; set; } = string.Empty;

        [DataMember]
        public string TeacherId { get; set; } = string.Empty;

        [DataMember]
        public decimal Grade { get; set; }

        [DataMember]
        public string? Comments { get; set; }

        [DataMember]
        public string? AcademicYear { get; set; }

        [DataMember]
        public string? Semester { get; set; }

        [DataMember]
        public int? CreditHours { get; set; }
    }

    [DataContract]
    public class BatchGradingRequest
    {
        [DataMember]
        public List<GradingRequest> GradingRequests { get; set; } = new List<GradingRequest>();
    }

    [DataContract]
    public class GradingResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string? ErrorMessage { get; set; }

        [DataMember]
        public string? MessageId { get; set; }

        [DataMember]
        public DateTime ProcessedAt { get; set; }

        [DataMember]
        public int ProcessedCount { get; set; } = 1;
    }

    [DataContract]
    public class ServiceHealthResponse
    {
        [DataMember]
        public bool IsHealthy { get; set; }

        [DataMember]
        public string Status { get; set; } = string.Empty;

        [DataMember]
        public DateTime CheckedAt { get; set; }

        [DataMember]
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
    }
}
