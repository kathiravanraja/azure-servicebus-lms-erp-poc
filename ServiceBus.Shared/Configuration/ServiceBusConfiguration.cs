namespace ServiceBus.Shared.Configuration
{
    public class ServiceBusConfiguration
    {
        public const string SectionName = "ServiceBus";

        public string ConnectionString { get; set; } = string.Empty;
        public string HomeworkSyncQueueName { get; set; } = "homework-sync";
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
        public bool UseLocalEmulator { get; set; } = true;
    }
}
