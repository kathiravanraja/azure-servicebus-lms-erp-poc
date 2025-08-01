# Azure Service Bus Setup Guide

This guide helps you set up Azure Service Bus for the LMS-ERP Integration POC.

## Option 1: Create Your Own Azure Service Bus (Recommended)

### Step 1: Create Azure Service Bus Namespace
1. Go to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource"
3. Search for "Service Bus"
4. Click "Create"
5. Fill in the details:
   - **Resource Group**: Create new or use existing
   - **Namespace name**: Choose a unique name (e.g., `your-company-integration`)
   - **Location**: Choose closest to your location
   - **Pricing tier**: Standard (required for dead letter queues)

### Step 2: Create the Queue
1. Navigate to your Service Bus namespace
2. Go to "Queues" in the left menu
3. Click "+ Queue"
4. Name: `homework-sync`
5. Configure settings:
   - **Max queue size**: 1 GB
   - **Message time to live**: 14 days
   - **Lock duration**: 30 seconds
   - **Enable dead lettering**: Yes
   - **Max delivery count**: 10

### Step 3: Get Connection String
1. In your Service Bus namespace, go to "Shared access policies"
2. Click on "RootManageSharedAccessKey" (or create a new policy)
3. Copy the "Primary Connection String"

### Step 4: Update Configuration
Replace the connection string in all `appsettings.json` files:

```json
{
  "ServiceBus": {
    "ConnectionString": "YOUR_COPIED_CONNECTION_STRING_HERE",
    "QueueName": "homework-sync"
  }
}
```

**Files to update:**
- `LMS.WcfService/appsettings.json`
- `ERP.ConsumerService/appsettings.json`
- `CompleteEndToEndTest/appsettings.json`
- `MessageDeliveryTest/appsettings.json`
- `Integration.Tests/appsettings.json`

## Option 2: Use Environment Variables (Production)

For production deployments, use environment variables instead of hard-coding connection strings:

### Windows (PowerShell)
```powershell
$env:AZURE_SERVICEBUS_CONNECTION_STRING = "your-connection-string"
```

### Linux/Mac
```bash
export AZURE_SERVICEBUS_CONNECTION_STRING="your-connection-string"
```

### Update Code to Read Environment Variables
In your `Program.cs` or startup code:

```csharp
var connectionString = Environment.GetEnvironmentVariable("AZURE_SERVICEBUS_CONNECTION_STRING") 
                      ?? configuration.GetConnectionString("ServiceBus")
                      ?? configuration.GetSection("ServiceBus:ConnectionString").Value
                      ?? throw new InvalidOperationException("ServiceBus connection string not found");
```

## Option 3: Azure Key Vault (Enterprise)

For enterprise scenarios, store the connection string in Azure Key Vault:

1. Create an Azure Key Vault
2. Add your connection string as a secret
3. Configure your application to read from Key Vault
4. Use managed identity for authentication

## Testing Your Setup

### 1. Verify Connection
Run the connection test:
```bash
cd MessageDeliveryTest
dotnet run
```

### 2. Full End-to-End Test
```bash
# Terminal 1: Start Consumer
cd ERP.ConsumerService
dotnet run

# Terminal 2: Start WCF Service  
cd LMS.WcfService
dotnet run

# Terminal 3: Run Test
cd CompleteEndToEndTest
dotnet run
```

## Troubleshooting

### Common Connection Issues

#### "InvalidSignature" Error
- **Cause**: Wrong connection string or expired key
- **Solution**: Regenerate access key in Azure Portal

#### "MessagingEntityNotFound" Error
- **Cause**: Queue doesn't exist
- **Solution**: Create the `homework-sync` queue in your namespace

#### "UnauthorizedAccessException" Error
- **Cause**: Insufficient permissions
- **Solution**: Use a connection string with "Manage" permissions

### Verification Checklist
- [ ] Service Bus namespace created
- [ ] Queue `homework-sync` exists
- [ ] Connection string has "Manage" permissions
- [ ] All `appsettings.json` files updated
- [ ] Queue allows dead lettering
- [ ] Pricing tier is Standard or Premium

## Cost Considerations

### Standard Tier Pricing
- **Base cost**: ~$10/month for namespace
- **Message operations**: $0.05 per million operations
- **Storage**: Included up to 1GB per queue

### Optimize Costs
- Use Standard tier (not Premium) for POC
- Delete namespace when not in use
- Monitor usage in Azure Cost Management

## Security Best Practices

### Connection String Security
- Never commit real connection strings to source control
- Use Azure Key Vault for production
- Rotate access keys regularly
- Use least-privilege access policies

### Network Security
- Configure firewall rules for Service Bus
- Use private endpoints for production
- Enable diagnostic logs for monitoring

## Production Recommendations

### Monitoring
- Enable Application Insights
- Set up alerts for dead letter queue depth
- Monitor message processing latency
- Track error rates and retry patterns

### Scaling
- Use multiple consumer instances
- Implement message batching
- Consider partitioned queues for high throughput
- Plan for disaster recovery scenarios

For more detailed information, see the [Azure Service Bus documentation](https://docs.microsoft.com/en-us/azure/service-bus-messaging/).
