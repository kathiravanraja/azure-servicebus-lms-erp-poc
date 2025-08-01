using ERP.ConsumerService.Services;
using Microsoft.Extensions.Options;
using ServiceBus.Shared.Configuration;
using ServiceBus.Shared.Models;
using ServiceBus.Shared.Services;

namespace ERP.ConsumerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceBusConsumer _serviceBusConsumer;
        private readonly IErpGradeProcessor _gradeProcessor;
        private readonly ServiceBusConfiguration _serviceBusConfig;

        public Worker(
            ILogger<Worker> logger,
            IServiceBusConsumer serviceBusConsumer,
            IErpGradeProcessor gradeProcessor,
            IOptions<ServiceBusConfiguration> serviceBusConfig)
        {
            _logger = logger;
            _serviceBusConsumer = serviceBusConsumer;
            _gradeProcessor = gradeProcessor;
            _serviceBusConfig = serviceBusConfig.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ERP Consumer Service starting at: {time}", DateTimeOffset.Now);

            try
            {
                // Start processing messages from the Service Bus queue
                await _serviceBusConsumer.StartProcessingAsync<HomeworkGradeMessage>(
                    _serviceBusConfig.HomeworkSyncQueueName,
                    ProcessHomeworkGradeMessageAsync);

                _logger.LogInformation("Successfully started processing messages from queue: {QueueName}",
                    _serviceBusConfig.HomeworkSyncQueueName);

                // Keep the service running until cancellation is requested
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("ERP Consumer Service running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(30000, stoppingToken); // Log every 30 seconds
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ERP Consumer Service is stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in ERP Consumer Service");
                throw;
            }
            finally
            {
                await _serviceBusConsumer.StopProcessingAsync();
                _logger.LogInformation("ERP Consumer Service stopped at: {time}", DateTimeOffset.Now);
            }
        }

        private async Task<bool> ProcessHomeworkGradeMessageAsync(HomeworkGradeMessage gradeMessage)
        {
            try
            {
                _logger.LogInformation("Received homework grade message: MessageId {MessageId}, Student {StudentId}, Grade {Grade}",
                    gradeMessage.MessageId, gradeMessage.StudentId, gradeMessage.Grade);

                // Process the grade message through the ERP processor
                var success = await _gradeProcessor.ProcessHomeworkGradeAsync(gradeMessage);

                if (success)
                {
                    _logger.LogInformation("Successfully processed homework grade: MessageId {MessageId}",
                        gradeMessage.MessageId);
                }
                else
                {
                    _logger.LogWarning("Failed to process homework grade: MessageId {MessageId}",
                        gradeMessage.MessageId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing homework grade message: MessageId {MessageId}",
                    gradeMessage.MessageId);
                return false;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ERP Consumer Service is stopping...");
            
            try
            {
                await _serviceBusConsumer.StopProcessingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping Service Bus consumer");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
