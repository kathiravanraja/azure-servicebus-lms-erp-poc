using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ServiceBus.Shared.Services
{
    public interface IServiceBusConsumer
    {
        Task StartProcessingAsync<T>(string queueName, Func<T, Task<bool>> messageHandler) where T : class;
        Task StopProcessingAsync();
    }

    public class ServiceBusConsumer : IServiceBusConsumer, IDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusConsumer> _logger;
        private ServiceBusProcessor? _processor;
        private ServiceBusProcessor? _deadLetterProcessor;

        public ServiceBusConsumer(string connectionString, ILogger<ServiceBusConsumer> logger)
        {
            _client = new ServiceBusClient(connectionString);
            _logger = logger;
        }

        public async Task StartProcessingAsync<T>(string queueName, Func<T, Task<bool>> messageHandler) where T : class
        {
            var processorOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10)
            };

            _processor = _client.CreateProcessor(queueName, processorOptions);
            _processor.ProcessMessageAsync += async args =>
            {
                try
                {
                    var messageBody = args.Message.Body.ToString();
                    _logger.LogInformation("Received message with ID {MessageId} from queue {QueueName}", 
                        args.Message.MessageId, queueName);

                    var message = JsonConvert.DeserializeObject<T>(messageBody);
                    if (message == null)
                    {
                        _logger.LogWarning("Failed to deserialize message with ID {MessageId}", args.Message.MessageId);
                        await args.DeadLetterMessageAsync(args.Message, "Deserialization failed", "Unable to deserialize message body");
                        return;
                    }

                    var success = await messageHandler(message);
                    if (success)
                    {
                        await args.CompleteMessageAsync(args.Message);
                        _logger.LogInformation("Successfully processed message with ID {MessageId}", args.Message.MessageId);
                    }
                    else
                    {
                        await args.AbandonMessageAsync(args.Message);
                        _logger.LogWarning("Message processing failed for message ID {MessageId}, message abandoned", args.Message.MessageId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message with ID {MessageId}", args.Message.MessageId);
                    
                    // Move to dead letter queue after max delivery count is reached
                    if (args.Message.DeliveryCount >= 3)
                    {
                        await args.DeadLetterMessageAsync(args.Message, "Processing failed", ex.Message);
                        _logger.LogError("Message with ID {MessageId} moved to dead letter queue after {DeliveryCount} attempts", 
                            args.Message.MessageId, args.Message.DeliveryCount);
                    }
                    else
                    {
                        await args.AbandonMessageAsync(args.Message);
                    }
                }
            };

            _processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error occurred in message processor for queue {QueueName}", queueName);
                return Task.CompletedTask;
            };

            // Also create a dead letter processor
            await SetupDeadLetterProcessorAsync(queueName);

            await _processor.StartProcessingAsync();
            _logger.LogInformation("Started processing messages from queue {QueueName}", queueName);
        }

        private async Task SetupDeadLetterProcessorAsync(string queueName)
        {
            var deadLetterOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            };

            _deadLetterProcessor = _client.CreateProcessor(queueName, 
                new ServiceBusProcessorOptions { SubQueue = SubQueue.DeadLetter });

            _deadLetterProcessor.ProcessMessageAsync += async args =>
            {
                _logger.LogWarning("Processing dead letter message with ID {MessageId}. Reason: {DeadLetterReason}, Description: {DeadLetterDescription}",
                    args.Message.MessageId, 
                    args.Message.DeadLetterReason, 
                    args.Message.DeadLetterErrorDescription);

                // Log the message content for debugging
                _logger.LogDebug("Dead letter message content: {MessageBody}", args.Message.Body.ToString());

                // Complete the dead letter message to remove it from the queue
                // In a real scenario, you might want to store this in a database or send an alert
                await args.CompleteMessageAsync(args.Message);
            };

            _deadLetterProcessor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error occurred in dead letter processor");
                return Task.CompletedTask;
            };

            await _deadLetterProcessor.StartProcessingAsync();
            _logger.LogInformation("Started processing dead letter messages for queue {QueueName}", queueName);
        }

        public async Task StopProcessingAsync()
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync();
                await _processor.DisposeAsync();
                _logger.LogInformation("Stopped message processing");
            }

            if (_deadLetterProcessor != null)
            {
                await _deadLetterProcessor.StopProcessingAsync();
                await _deadLetterProcessor.DisposeAsync();
                _logger.LogInformation("Stopped dead letter processing");
            }
        }

        public void Dispose()
        {
            StopProcessingAsync().Wait();
            _client?.DisposeAsync().AsTask().Wait();
        }
    }
}
