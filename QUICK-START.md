# Azure Service Bus POC - Quick Setup Guide

## For Azure Cloud Setup (Recommended for Testing)

### 1. Create Azure Resources
Run the PowerShell script to set up Azure Service Bus:

```powershell
# Create new namespace and queue
.\setup-azure-servicebus.ps1 -NamespaceName "your-servicebus-poc" -ResourceGroupName "rg-servicebus-poc" -CreateNamespace $true

# Or use existing namespace
.\setup-azure-servicebus.ps1 -NamespaceName "existing-namespace" -ResourceGroupName "existing-rg"
```

### 2. Update Configuration
Copy the connection string from the script output and update:
- `LMS.WcfService\appsettings.json`
- `ERP.ConsumerService\appsettings.json`
- `Integration.Tests\appsettings.json`

Set `UseLocalEmulator: false` in all config files.

### 3. Run the Demo
```powershell
.\run-demo.ps1
```

## For Local Development (Limited Testing)

### 1. Use Default Local Configuration
The projects are pre-configured with local emulator settings.

### 2. Run Individual Services
```bash
# Terminal 1 - ERP Consumer
cd ERP.ConsumerService
dotnet run

# Terminal 2 - LMS WCF Service
cd LMS.WcfService
dotnet run

# Terminal 3 - Test Client
cd TestClient
dotnet run
```

## Testing Options

### 1. Unit Tests
```bash
dotnet test
```

### 2. Health Check
Visit: http://localhost:5000/health

### 3. WCF Service
- WSDL: http://localhost:5000/GradingService.svc?wsdl
- Service: http://localhost:5000/GradingService.svc

### 4. Manual Testing
Use the TestClient project to interact with the services.

## Key Features Demonstrated

✅ **WCF Service** - Receives homework grades from LMS
✅ **Service Bus Publisher** - Sends messages to Azure Service Bus queue
✅ **Service Bus Consumer** - Processes messages in ERP system
✅ **Dead Letter Queue** - Handles failed messages
✅ **Error Handling** - Retry logic and error scenarios
✅ **Batch Processing** - Multiple grades in single request
✅ **Health Monitoring** - Service health endpoints
✅ **Integration Tests** - End-to-end testing
✅ **Configuration** - Environment-specific settings

## Architecture Flow

```
Teacher grades homework in LMS
        ↓
WCF Service receives grade data
        ↓
Published to Service Bus queue (homework-sync)
        ↓
ERP Consumer Service processes message
        ↓
Grade saved in ERP system
```

## Message Flow Example

1. **LMS Input:**
   ```json
   {
     "StudentId": "STU001",
     "HomeworkId": "HW001",
     "CourseId": "CS101", 
     "TeacherId": "TCH001",
     "Grade": 85.5,
     "Comments": "Good work!"
   }
   ```

2. **Service Bus Message:**
   ```json
   {
     "MessageId": "guid",
     "StudentId": "STU001",
     "HomeworkId": "HW001",
     "CourseId": "CS101",
     "TeacherId": "TCH001", 
     "Grade": 85.5,
     "Comments": "Good work!",
     "GradedAt": "2024-08-01T10:30:00Z",
     "CreatedAt": "2024-08-01T10:30:00Z"
   }
   ```

3. **ERP Processing:**
   - Validate student exists
   - Validate course exists
   - Save grade to ERP database
   - Log successful processing

## Troubleshooting

**Service Bus Connection Issues:**
- Verify connection string is correct
- Check Azure Service Bus namespace status
- Ensure queue exists

**WCF Service Issues:**
- Check if port 5000 is available
- Verify .NET 6 SDK is installed
- Check Windows Firewall settings

**Consumer Service Issues:**
- Verify Service Bus permissions
- Check dead letter queue for failed messages
- Review logs for processing errors

## Next Steps for Production

1. **Security:** Implement managed identities
2. **Monitoring:** Add Application Insights
3. **Scaling:** Configure auto-scaling
4. **Reliability:** Implement circuit breakers
5. **Performance:** Optimize batch sizes
