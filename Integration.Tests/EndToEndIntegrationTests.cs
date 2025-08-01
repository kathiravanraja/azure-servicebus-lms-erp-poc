using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ServiceBus.Shared.Configuration;
using ServiceBus.Shared.Models;
using ServiceBus.Shared.Services;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests
{
    /// <summary>
    /// End-to-end integration test that demonstrates the complete flow:
    /// 1. Publish a message to Service Bus
    /// 2. Wait for message to be processed
    /// 3. Verify message was consumed
    /// 
    /// Note: This test requires Azure Service Bus Emulator to be running
    /// </summary>
    public class EndToEndIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ServiceBusPublisher _publisher;
        private readonly ServiceBusConsumer _consumer;
        private readonly ServiceBusConfiguration _config;
        private readonly ILogger<ServiceBusPublisher> _publisherLogger;
        private readonly ILogger<ServiceBusConsumer> _consumerLogger;
        private readonly List<HomeworkGradeMessage> _receivedMessages;

        public EndToEndIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _receivedMessages = new List<HomeworkGradeMessage>();

            _config = new ServiceBusConfiguration
            {
                ConnectionString = "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=your-policy-name;SharedAccessKey=your-access-key",
                HomeworkSyncQueueName = "homework-sync"
            };

            var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().AddProvider(new XUnitLoggerProvider(_output)));
            
            _publisherLogger = loggerFactory.CreateLogger<ServiceBusPublisher>();
            _consumerLogger = loggerFactory.CreateLogger<ServiceBusConsumer>();

            _publisher = new ServiceBusPublisher(_config.ConnectionString, _publisherLogger);
            _consumer = new ServiceBusConsumer(_config.ConnectionString, _consumerLogger);
        }

        [Fact]
        public async Task EndToEndFlow_PublishAndConsume_ShouldProcessMessageSuccessfully()
        {
            // Skip this test if Service Bus emulator is not available
            if (!await IsServiceBusAvailable())
            {
                _output.WriteLine("Skipping test - Service Bus emulator not available");
                return;
            }

            // Arrange
            var testMessage = new HomeworkGradeMessage
            {
                StudentId = "STU001",
                HomeworkId = "HW_E2E_001",
                CourseId = "CS101",
                TeacherId = "TCH001",
                Grade = 88.5m,
                Comments = "End-to-end test message",
                GradedAt = DateTime.UtcNow,
                AcademicYear = "2024-2025",
                Semester = "Fall",
                CreditHours = 3
            };

            var messageReceived = false;
            var processingComplete = new TaskCompletionSource<bool>();

            // Set up consumer
            await _consumer.StartProcessingAsync<HomeworkGradeMessage>(
                _config.HomeworkSyncQueueName,
                async (message) =>
                {
                    _output.WriteLine($"Received message: {message.MessageId}");
                    _receivedMessages.Add(message);
                    messageReceived = true;
                    processingComplete.SetResult(true);
                    return true;
                });

            // Act - Publish the message
            _output.WriteLine($"Publishing message: {testMessage.MessageId}");
            await _publisher.PublishAsync(testMessage, _config.HomeworkSyncQueueName);

            // Wait for message to be processed (with timeout)
            var completedTask = await Task.WhenAny(
                processingComplete.Task,
                Task.Delay(TimeSpan.FromSeconds(30)));

            // Assert
            Assert.True(messageReceived, "Message was not received within the timeout period");
            Assert.Single(_receivedMessages);
            
            var receivedMessage = _receivedMessages.First();
            Assert.Equal(testMessage.StudentId, receivedMessage.StudentId);
            Assert.Equal(testMessage.HomeworkId, receivedMessage.HomeworkId);
            Assert.Equal(testMessage.CourseId, receivedMessage.CourseId);
            Assert.Equal(testMessage.Grade, receivedMessage.Grade);

            _output.WriteLine("End-to-end test completed successfully!");
        }

        [Fact]
        public async Task BatchFlow_PublishAndConsumeBatch_ShouldProcessAllMessages()
        {
            // Skip this test if Service Bus emulator is not available
            if (!await IsServiceBusAvailable())
            {
                _output.WriteLine("Skipping test - Service Bus emulator not available");
                return;
            }

            // Arrange
            var testMessages = new List<HomeworkGradeMessage>
            {
                new HomeworkGradeMessage
                {
                    StudentId = "STU001",
                    HomeworkId = "HW_BATCH_001",
                    CourseId = "CS101",
                    TeacherId = "TCH001",
                    Grade = 85.0m
                },
                new HomeworkGradeMessage
                {
                    StudentId = "STU002",
                    HomeworkId = "HW_BATCH_002",
                    CourseId = "CS101",
                    TeacherId = "TCH001",
                    Grade = 92.5m
                },
                new HomeworkGradeMessage
                {
                    StudentId = "STU003",
                    HomeworkId = "HW_BATCH_003",
                    CourseId = "CS101",
                    TeacherId = "TCH001",
                    Grade = 78.0m
                }
            };

            var expectedMessageCount = testMessages.Count;
            var processingComplete = new TaskCompletionSource<bool>();

            // Set up consumer
            await _consumer.StartProcessingAsync<HomeworkGradeMessage>(
                _config.HomeworkSyncQueueName,
                async (message) =>
                {
                    _output.WriteLine($"Received batch message: {message.MessageId} for student {message.StudentId}");
                    _receivedMessages.Add(message);
                    
                    if (_receivedMessages.Count >= expectedMessageCount)
                    {
                        processingComplete.SetResult(true);
                    }
                    
                    return true;
                });

            // Act - Publish the batch
            _output.WriteLine($"Publishing batch of {testMessages.Count} messages");
            await _publisher.PublishBatchAsync(testMessages, _config.HomeworkSyncQueueName);

            // Wait for all messages to be processed (with timeout)
            var completedTask = await Task.WhenAny(
                processingComplete.Task,
                Task.Delay(TimeSpan.FromSeconds(45)));

            // Assert
            Assert.True(_receivedMessages.Count >= expectedMessageCount, 
                $"Expected {expectedMessageCount} messages, but received {_receivedMessages.Count}");

            // Verify all test messages were received
            foreach (var testMessage in testMessages)
            {
                var receivedMessage = _receivedMessages.FirstOrDefault(m => m.HomeworkId == testMessage.HomeworkId);
                Assert.NotNull(receivedMessage);
                Assert.Equal(testMessage.StudentId, receivedMessage.StudentId);
                Assert.Equal(testMessage.Grade, receivedMessage.Grade);
            }

            _output.WriteLine("Batch end-to-end test completed successfully!");
        }

        private async Task<bool> IsServiceBusAvailable()
        {
            try
            {
                var testMessage = new HomeworkGradeMessage
                {
                    StudentId = "TEST",
                    HomeworkId = "TEST",
                    CourseId = "TEST",
                    TeacherId = "TEST",
                    Grade = 0
                };

                // Try to publish a test message
                await _publisher.PublishAsync(testMessage, _config.HomeworkSyncQueueName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _consumer?.StopProcessingAsync().Wait();
            _consumer?.Dispose();
            _publisher?.Dispose();
        }
    }

    /// <summary>
    /// Custom logger provider for XUnit test output
    /// </summary>
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_output, categoryName);
        }

        public void Dispose() { }
    }

    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _categoryName;

        public XUnitLogger(ITestOutputHelper output, string categoryName)
        {
            _output = output;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _output.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
        }
    }
}
