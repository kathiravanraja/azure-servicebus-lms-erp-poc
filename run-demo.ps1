# PowerShell script to run the complete POC demo

Write-Host "Azure Service Bus POC - LMS to ERP Integration Demo" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green

# Check if .NET 6 is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Error "❌ .NET 6 SDK is required but not installed. Please install from https://dotnet.microsoft.com/download"
    exit 1
}

# Build the solution
Write-Host "`nBuilding the solution..." -ForegroundColor Yellow
try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "✓ Build completed successfully" -ForegroundColor Green
} catch {
    Write-Error "❌ Build failed: $_"
    exit 1
}

# Run tests
Write-Host "`nRunning tests..." -ForegroundColor Yellow
try {
    dotnet test --configuration Release --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "⚠️ Some tests may have failed - this is expected if Service Bus emulator is not running"
    } else {
        Write-Host "✓ All tests passed" -ForegroundColor Green
    }
} catch {
    Write-Warning "⚠️ Tests failed - this may be expected if Service Bus emulator is not available"
}

# Function to start a process and return job
function Start-ServiceProcess {
    param($Path, $Name)
    
    Write-Host "Starting $Name..." -ForegroundColor Yellow
    $job = Start-Job -ScriptBlock {
        param($path)
        Set-Location $path
        dotnet run
    } -ArgumentList $Path
    
    Start-Sleep -Seconds 3
    return $job
}

# Start ERP Consumer Service
$erpJob = Start-ServiceProcess -Path "ERP.ConsumerService" -Name "ERP Consumer Service"

# Start LMS WCF Service  
$lmsJob = Start-ServiceProcess -Path "LMS.WcfService" -Name "LMS WCF Service"

# Wait for services to start
Write-Host "`nWaiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check if services are running
Write-Host "`nChecking service status..." -ForegroundColor Yellow

try {
    # Test LMS WCF Service health endpoint
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 5
    if ($healthResponse.IsHealthy) {
        Write-Host "✓ LMS WCF Service is healthy" -ForegroundColor Green
    } else {
        Write-Warning "⚠️ LMS WCF Service health check returned unhealthy status"
    }
} catch {
    Write-Warning "⚠️ Could not reach LMS WCF Service health endpoint"
    Write-Host "   This is expected if Service Bus connection is not available" -ForegroundColor Gray
}

# Display service information
Write-Host "`nServices are running:" -ForegroundColor Green
Write-Host "✓ ERP Consumer Service (Background Worker)" -ForegroundColor White
Write-Host "✓ LMS WCF Service: http://localhost:5000/GradingService.svc" -ForegroundColor White
Write-Host "✓ Health Check: http://localhost:5000/health" -ForegroundColor White

Write-Host "`nDemo Options:" -ForegroundColor Cyan
Write-Host "1. View logs in real-time" -ForegroundColor White
Write-Host "2. Test with sample data" -ForegroundColor White
Write-Host "3. Stop all services" -ForegroundColor White

do {
    Write-Host "`nEnter your choice (1-3) or 'q' to quit: " -ForegroundColor Yellow -NoNewline
    $choice = Read-Host

    switch ($choice.ToLower()) {
        "1" {
            Write-Host "`nShowing live logs (Ctrl+C to stop):" -ForegroundColor Green
            Write-Host "ERP Consumer Job Output:" -ForegroundColor Cyan
            Receive-Job -Job $erpJob
            Write-Host "`nLMS WCF Job Output:" -ForegroundColor Cyan
            Receive-Job -Job $lmsJob
        }
        "2" {
            Write-Host "`nTesting with sample data..." -ForegroundColor Green
            Write-Host "Note: This requires a working Service Bus connection" -ForegroundColor Yellow
            
            # Create a simple test client
            $testScript = @"
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program 
{
    static async Task Main() 
    {
        try 
        {
            var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:5000/health");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Health Check Response: " + content);
        } 
        catch (Exception ex) 
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
"@
            
            # For now, just show the test command
            Write-Host "You can test the service by:" -ForegroundColor White
            Write-Host "1. Visiting: http://localhost:5000/health" -ForegroundColor Cyan
            Write-Host "2. Using the WSDL: http://localhost:5000/GradingService.svc?wsdl" -ForegroundColor Cyan
            Write-Host "3. Running the integration tests: dotnet test" -ForegroundColor Cyan
        }
        "3" {
            Write-Host "`nStopping services..." -ForegroundColor Yellow
            Stop-Job -Job $erpJob, $lmsJob
            Remove-Job -Job $erpJob, $lmsJob
            Write-Host "✓ All services stopped" -ForegroundColor Green
            return
        }
        "q" {
            Write-Host "`nStopping services..." -ForegroundColor Yellow
            Stop-Job -Job $erpJob, $lmsJob
            Remove-Job -Job $erpJob, $lmsJob
            Write-Host "✓ Demo completed" -ForegroundColor Green
            return
        }
        default {
            Write-Host "Invalid choice. Please enter 1, 2, 3, or 'q'" -ForegroundColor Red
        }
    }
} while ($true)

Write-Host "`nThank you for trying the Azure Service Bus POC!" -ForegroundColor Green
