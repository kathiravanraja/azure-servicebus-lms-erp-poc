using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ServiceBus.Shared.Services;
using ServiceBus.Shared.Models;
using ServiceBus.Shared.Configuration;

namespace MessageDeliveryTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Azure Service Bus Message Delivery Test");
        Console.WriteLine("==========================================");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .Build();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.Configure<ServiceBusConfiguration>(configuration.GetSection("ServiceBus"));
        
        // Register ServiceBusPublisher with connection string
        var connectionString = configuration.GetConnectionString("ServiceBus") ?? 
                              configuration.GetSection("ServiceBus:ConnectionString").Value ?? 
                              throw new InvalidOperationException("ServiceBus connection string not found");
        
        services.AddSingleton<IServiceBusPublisher>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ServiceBusPublisher>>();
            return new ServiceBusPublisher(connectionString, logger);
        });

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var publisher = serviceProvider.GetRequiredService<IServiceBusPublisher>();

        try
        {
            // Create a test message with tracking (using valid student ID from mock ERP data)
            var trackingId = Guid.NewGuid().ToString("N")[..8];
            var testMessage = new HomeworkGradeMessage
            {
                StudentId = "STU001", // Valid student ID from mock ERP system
                CourseId = "CS101",   // Valid course ID from mock ERP system
                HomeworkId = $"HW-VALID-{trackingId}",
                TeacherId = "TCH001",
                Grade = 88.5m,
                Comments = $"✅ VALID TEST MESSAGE for successful processing - Tracking: {trackingId}",
                AcademicYear = "2024-2025",
                Semester = "Fall",
                CreditHours = 3,
                GradedAt = DateTime.UtcNow
            };

            Console.WriteLine($"\n📤 Publishing test message to Service Bus:");
            Console.WriteLine($"   Student ID: {testMessage.StudentId}");
            Console.WriteLine($"   Homework: {testMessage.HomeworkId}");
            Console.WriteLine($"   Grade: {testMessage.Grade}");
            Console.WriteLine($"   Tracking ID: {trackingId}");

            // Publish the message
            await publisher.PublishAsync(testMessage, "homework-sync");
            var messageId = testMessage.MessageId;
            
            Console.WriteLine($"\n✅ Message published successfully!");
            Console.WriteLine($"📨 Message ID: {messageId}");
            Console.WriteLine($"⏰ Published at: {DateTime.UtcNow}");

            Console.WriteLine($"\n🔍 VALIDATION STEPS:");
            Console.WriteLine($"===================");
            Console.WriteLine($"1. ✅ Message sent to Azure Service Bus queue 'homework-sync'");
            Console.WriteLine($"2. 🔄 Check ERP Consumer Service logs for message processing");
            Console.WriteLine($"3. 👀 Look for these identifiers in consumer logs:");
            Console.WriteLine($"   - Student ID: {testMessage.StudentId}");
            Console.WriteLine($"   - Homework: {testMessage.HomeworkId}");
            Console.WriteLine($"   - Tracking: {trackingId}");
            Console.WriteLine($"   - Message ID: {messageId}");

            Console.WriteLine($"\n⏳ Waiting 10 seconds to allow message processing...");
            await Task.Delay(10000);

            Console.WriteLine($"\n📋 Check the ERP Consumer Service terminal now!");
            Console.WriteLine($"   Look for: 'Processing homework grade for student {testMessage.StudentId}'");
            Console.WriteLine($"   Expected: 'Successfully processed grade for student {testMessage.StudentId}'");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to publish test message");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine($"\n🏁 Message Delivery Test Complete");
        Console.WriteLine($"=================================");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
