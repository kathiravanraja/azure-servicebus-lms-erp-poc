using System.ServiceModel;
using System.Text.Json;
using ServiceBus.Shared.Models;

namespace TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Service Bus POC - Test Client");
            Console.WriteLine("===================================");

            // Test HTTP health endpoint first
            await TestHealthEndpoint();

            // Test WCF service
            await TestWcfService();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task TestHealthEndpoint()
        {
            Console.WriteLine("\n1. Testing Health Endpoint...");
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("http://localhost:5000/health");
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task TestWcfService()
        {
            Console.WriteLine("\n2. Testing WCF Service...");
            
            try
            {
                // Create WCF client manually
                var binding = new BasicHttpBinding();
                var endpoint = new EndpointAddress("http://localhost:5000/GradingService.svc");
                
                // For this demo, we'll use a simple HTTP POST to test
                // In a real scenario, you'd generate a service reference
                await TestWcfWithHttp();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task TestWcfWithHttp()
        {
            try
            {
                // Test the service with a simple HTTP request
                // This demonstrates that the WCF service is running
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("http://localhost:5000/GradingService.svc");
                
                Console.WriteLine($"WCF Service Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✓ WCF Service is accessible");
                    Console.WriteLine("To test SOAP operations, you would:");
                    Console.WriteLine("1. Add a Service Reference to http://localhost:5000/GradingService.svc");
                    Console.WriteLine("2. Or use WSDL: http://localhost:5000/GradingService.svc?wsdl");
                    
                    // Show sample request data
                    ShowSampleGradingRequest();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing WCF service: {ex.Message}");
            }
        }

        static void ShowSampleGradingRequest()
        {
            Console.WriteLine("\nSample Grading Request Data:");
            
            var sampleRequest = new
            {
                StudentId = "STU001",
                HomeworkId = "HW001", 
                CourseId = "CS101",
                TeacherId = "TCH001",
                Grade = 85.5m,
                Comments = "Good work on the assignment!",
                AcademicYear = "2024-2025",
                Semester = "Fall",
                CreditHours = 3
            };

            var json = JsonSerializer.Serialize(sampleRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);

            Console.WriteLine("\nSample Batch Request (multiple grades):");
            var batchRequest = new
            {
                GradingRequests = new[]
                {
                    new { StudentId = "STU001", HomeworkId = "HW001", CourseId = "CS101", TeacherId = "TCH001", Grade = 85.5m },
                    new { StudentId = "STU002", HomeworkId = "HW001", CourseId = "CS101", TeacherId = "TCH001", Grade = 92.0m },
                    new { StudentId = "STU003", HomeworkId = "HW001", CourseId = "CS101", TeacherId = "TCH001", Grade = 78.5m }
                }
            };

            var batchJson = JsonSerializer.Serialize(batchRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(batchJson);
        }
    }
}
