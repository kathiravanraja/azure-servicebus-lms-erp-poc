using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServiceBus.Shared.Models;

namespace ServiceBus.Shared.Services
{
    public interface IServiceBusPublisher
    {
        Task PublishAsync<T>(T message, string queueName) where T : class;
        Task PublishBatchAsync<T>(IEnumerable<T> messages, string queueName) where T : class;
    }

    public class ServiceBusPublisher : IServiceBusPublisher, IDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusPublisher> _logger;
        private readonly Dictionary<string, ServiceBusSender> _senders;

        public ServiceBusPublisher(string connectionString, ILogger<ServiceBusPublisher> logger)
        {
            _client = new ServiceBusClient(connectionString);
            _logger = logger;
            _senders = new Dictionary<string, ServiceBusSender>();
        }

        public async Task PublishAsync<T>(T message, string queueName) where T : class
        {
            try
            {
                var sender = GetOrCreateSender(queueName);
                var json = JsonConvert.SerializeObject(message);
                var serviceBusMessage = new ServiceBusMessage(json)
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    TimeToLive = TimeSpan.FromHours(24) // Messages expire after 24 hours
                };

                // Add custom properties for message routing and filtering
                if (message is HomeworkGradeMessage gradeMessage)
                {
                    serviceBusMessage.ApplicationProperties["CourseId"] = gradeMessage.CourseId;
                    serviceBusMessage.ApplicationProperties["StudentId"] = gradeMessage.StudentId;
                    serviceBusMessage.ApplicationProperties["MessageType"] = "HomeworkGrade";
                }

                await sender.SendMessageAsync(serviceBusMessage);
                _logger.LogInformation("Message published to queue {QueueName} with MessageId {MessageId}", 
                    queueName, serviceBusMessage.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
                throw;
            }
        }

        public async Task PublishBatchAsync<T>(IEnumerable<T> messages, string queueName) where T : class
        {
            try
            {
                var sender = GetOrCreateSender(queueName);
                var serviceBusMessages = messages.Select(message =>
                {
                    var json = JsonConvert.SerializeObject(message);
                    var serviceBusMessage = new ServiceBusMessage(json)
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString(),
                        TimeToLive = TimeSpan.FromHours(24)
                    };

                    if (message is HomeworkGradeMessage gradeMessage)
                    {
                        serviceBusMessage.ApplicationProperties["CourseId"] = gradeMessage.CourseId;
                        serviceBusMessage.ApplicationProperties["StudentId"] = gradeMessage.StudentId;
                        serviceBusMessage.ApplicationProperties["MessageType"] = "HomeworkGrade";
                    }

                    return serviceBusMessage;
                }).ToList();

                await sender.SendMessagesAsync(serviceBusMessages);
                _logger.LogInformation("Batch of {MessageCount} messages published to queue {QueueName}", 
                    serviceBusMessages.Count, queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish batch messages to queue {QueueName}", queueName);
                throw;
            }
        }

        private ServiceBusSender GetOrCreateSender(string queueName)
        {
            if (!_senders.ContainsKey(queueName))
            {
                _senders[queueName] = _client.CreateSender(queueName);
            }
            return _senders[queueName];
        }

        public void Dispose()
        {
            foreach (var sender in _senders.Values)
            {
                sender?.DisposeAsync().AsTask().Wait();
            }
            _client?.DisposeAsync().AsTask().Wait();
        }
    }
}
