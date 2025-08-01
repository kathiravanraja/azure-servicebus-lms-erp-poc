# Azure Service Bus POC - LMS to ERP Integration

This project demonstrates a **real-time integration between a Learning Management System (LMS) and an Enterprise Resource Planning (ERP) system** using Azure Service Bus as the messaging backbone.

## 🏗️ Architecture Overview

```
📚 LMS System → 🌐 WCF Service → 🚌 Azure Service Bus → 📥 Consumer → 💾 ERP System
     ↓              ↓                    ↓               ↓            ↓
  Homework      HTTP/SOAP          homework-sync      Background    Grade 
   Grades       Endpoint            Queue             Worker       Records
                                      ↓
                              ⚰️ Dead Letter Queue
```

## 📦 Project Components

### Core Services
1. **LMS.WcfService** - CoreWCF service that receives homework grades via HTTP/SOAP and publishes to Service Bus
2. **ERP.ConsumerService** - Background worker service that consumes messages and processes grades in mock ERP system
3. **ServiceBus.Shared** - Shared models, services, and configuration library

### Testing & Validation
4. **CompleteEndToEndTest** - Comprehensive test demonstrating the complete pipeline (HTTP → WCF → Service Bus → Consumer)
5. **MessageDeliveryTest** - Direct Service Bus publishing test for validation
6. **Integration.Tests** - Unit and integration tests using xUnit framework
7. **TestClient** - Simple client for manual testing

## 🛠️ Technology Stack

- **.NET 8.0** - Target framework
- **CoreWCF 1.4.0** - Modern WCF implementation with BasicHttpBinding
- **Azure.Messaging.ServiceBus 7.17.5** - Azure Service Bus SDK
- **Azure Service Bus** - Cloud messaging service (ctaintegration.servicebus.windows.net)
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **Structured Logging** - Microsoft.Extensions.Logging

## 🚀 Quick Start Guide

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Azure Service Bus namespace (or use our provided connection)

### Step 1: Clone and Build
```bash
git clone <repository-url>
cd AzureServiceBusPOC
dotnet restore
dotnet build
```

### Step 2: Configure Azure Service Bus
The project requires an Azure Service Bus connection string. Update the connection string in all `appsettings.json` files:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=your-policy-name;SharedAccessKey=your-access-key",
    "QueueName": "homework-sync"
  }
}
```

**Required Files to Update:**
- `LMS.WcfService/appsettings.json`
- `ERP.ConsumerService/appsettings.json` 
- `CompleteEndToEndTest/appsettings.json`
- `MessageDeliveryTest/appsettings.json`
- `Integration.Tests/appsettings.json`

### Step 3: Start the Services

#### Terminal 1 - Start the ERP Consumer Service
```bash
cd ERP.ConsumerService
dotnet run
```
**Expected Output:**
```
info: ERP.ConsumerService.Worker[0]
      ERP Consumer Service started successfully
info: ServiceBus.Shared.Services.ServiceBusConsumer[0]
      Starting to listen for messages on queue: homework-sync
```

#### Terminal 2 - Start the LMS WCF Service
```bash
cd LMS.WcfService
dotnet run
```
**Expected Output:**
```
info: LMS.WcfService.Program[0]
      WCF Service starting on http://localhost:5000
info: LMS.WcfService.Program[0]
      WCF Service started successfully
```

**Service Endpoints:**
- WCF Service: `http://localhost:5000/GradingService.svc`
- Health Check: `http://localhost:5000/health`
- Service Metadata: `http://localhost:5000/GradingService.svc?wsdl`

### Step 4: Run the Complete End-to-End Test

#### Terminal 3 - Run Comprehensive Test
```bash
cd CompleteEndToEndTest
dotnet run
```

**Expected Test Flow:**
```
🎯 Complete End-to-End Test: HTTP → WCF → Service Bus → Consumer
=================================================================

🔍 Step 1: Testing WCF Service Health via HTTP
✅ WCF Service Health: OK
✅ WCF Service Endpoint: OK

📝 Step 2: Testing Direct Service Bus Publishing
🧪 Testing Service Bus connection and publishing capability...
✅ Service Bus connection and publishing test successful

📨 Step 3: Publishing Test Homework Grade Message
📚 Student ID: STU001
📝 Homework: HW_MATH_001
📖 Course: MATH101
🎯 Grade: 85
✅ Homework grade message published successfully to Azure Service Bus

⏳ Step 4: Waiting for ERP Consumer Processing
📋 Step 5: Validation Summary
✅ HTTP Test: WCF Service is accessible
✅ Service Bus: Connection and publishing tested
✅ Message: Test homework grade published successfully
✅ Consumer: Check ERP Consumer Service logs for processing
```

**Verify in ERP Consumer Logs:**
```
info: ERP.ConsumerService.Services.ErpGradeProcessor[0]
      Processing homework grade for Student STU001, Course MATH101, Grade 85.0
info: ERP.ConsumerService.Services.ErpGradeProcessor[0]
      Grade saved to ERP: Student STU001, Course MATH101, Grade 85.0, Record ID xxxxx-xxxx-xxxx
info: ERP.ConsumerService.Services.ErpGradeProcessor[0]
      Successfully processed grade for Student STU001, Homework HW_MATH_001
```

## 🧪 Testing Options

### Option 1: Quick Validation Test
```bash
cd MessageDeliveryTest
dotnet run
```

### Option 2: Professional Unit Tests
```bash
cd Integration.Tests
dotnet test
```

### Option 3: HTTP Health Check
```bash
# PowerShell
Invoke-RestMethod -Uri "http://localhost:5000/health"

# cURL
curl http://localhost:5000/health
```

## 📊 Message Flow Validation

### Valid Message Processing
When you send a message with a valid student ID (like STU001), you'll see:
```
✅ Message received → ✅ Student found → ✅ Grade saved → ✅ Success response
```

### Invalid Message Handling
When you send a message with an invalid student ID, you'll see:
```
⚠️ Message received → ❌ Student not found → ⚰️ Dead letter queue → 🔄 Retry logic
```

### Message Structure
```json
{
  "StudentId": "STU001",
  "HomeworkId": "HW_MATH_001", 
  "CourseId": "MATH101",
  "TeacherId": "PROF_JOHNSON",
  "Grade": 85.0,
  "Comments": "Good work on algebra problems",
  "GradedAt": "2025-08-01T13:20:35Z",
  "MessageId": "26a45b38-481b-41f0-beef-cc895ef19b7c",
  "AcademicYear": "2024-2025",
  "Semester": "Fall",
  "CreditHours": 3
}
```

## 🔧 Configuration

### Service Bus Settings
```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=your-policy;SharedAccessKey=your-key",
    "QueueName": "homework-sync"
  }
}
```

### Logging Levels
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "ServiceBus.Shared": "Information"
    }
  }
}
```

## 🔍 Troubleshooting

### Common Issues

#### 1. Service Bus Connection Errors
```
Error: "InvalidSignature" or "Unauthorized"
```
**Solution:** Verify the connection string in `appsettings.json` is correct.

#### 2. Queue Not Found
```
Error: "MessagingEntityNotFound"
```
**Solution:** Ensure the `homework-sync` queue exists in your Service Bus namespace.

#### 3. WCF Service Not Starting
```
Error: "Address already in use"
```
**Solution:** Kill any existing processes on port 5000:
```bash
# PowerShell
Get-Process -Name "dotnet" | Stop-Process -Force
```

#### 4. No Messages Being Processed
**Check:**
- ERP Consumer Service is running
- Service Bus connection is working
- Messages aren't stuck in dead letter queue

### Debugging Steps
1. **Check all services are running**
2. **Verify connection strings match**
3. **Run CompleteEndToEndTest for full validation**
4. **Check logs in both services**
5. **Validate Azure Service Bus queue status**

## 🚀 Production Deployment

### Azure Resources Needed
1. **Azure Service Bus Namespace**
2. **App Service or Container Instances** for hosting services
3. **Application Insights** for monitoring
4. **Key Vault** for connection string management

### Security Best Practices
- Use **Managed Identity** instead of connection strings
- Implement **proper authentication** for WCF endpoints
- Enable **Azure Monitor** for logging and alerting
- Configure **network security groups** for service communication

### Scalability Considerations
- **Horizontal scaling** for ERP Consumer Service
- **Message batching** for high-volume scenarios
- **Partitioned queues** for increased throughput
- **Auto-scaling** based on queue depth

## 🎯 Success Criteria

✅ **Complete Integration Working:**
- WCF Service accepts homework grades via HTTP/SOAP
- Messages flow through Azure Service Bus queue
- ERP Consumer processes valid messages
- Dead letter queue handles invalid messages
- End-to-end logging and monitoring

✅ **Validation Demonstrated:**
- HTTP health checks working
- Service Bus connectivity tested
- Message processing confirmed
- Error handling validated

## 📈 Next Steps

### Enhancements
1. **Add database persistence** for grade records
2. **Implement real authentication** and authorization
3. **Add message transformation** and validation rules
4. **Create web dashboard** for monitoring
5. **Add performance metrics** and alerting

### Integration Options
1. **Connect to real LMS** (Canvas, Blackboard, Moodle)
2. **Integrate with real ERP** (SAP, Oracle, Dynamics)
3. **Add additional message types** (attendance, assignments)
4. **Implement workflow engines** for complex business rules

## 📞 Support

This POC demonstrates a complete, working integration between LMS and ERP systems using Azure Service Bus. All components are functional and tested.

**Project Status:** ✅ **COMPLETE AND WORKING**
- Real Azure Service Bus integration
- End-to-end message processing
- Comprehensive testing suite
- Production-ready architecture patterns
