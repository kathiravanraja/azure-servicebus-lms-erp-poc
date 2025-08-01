# PowerShell script to set up Azure Service Bus queue for the POC

param(
    [Parameter(Mandatory=$true)]
    [string]$NamespaceName,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [string]$QueueName = "homework-sync",
    
    [string]$Location = "East US",
    
    [bool]$CreateNamespace = $false
)

Write-Host "Setting up Azure Service Bus for LMS-ERP Integration POC..." -ForegroundColor Green

# Login to Azure (if not already logged in)
try {
    $context = Get-AzContext
    if (-not $context) {
        Write-Host "Please log in to Azure..." -ForegroundColor Yellow
        Connect-AzAccount
    }
} catch {
    Write-Host "Please log in to Azure..." -ForegroundColor Yellow
    Connect-AzAccount
}

# Create resource group if it doesn't exist
try {
    $resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
    if (-not $resourceGroup) {
        Write-Host "Creating resource group: $ResourceGroupName" -ForegroundColor Yellow
        New-AzResourceGroup -Name $ResourceGroupName -Location $Location
    }
} catch {
    Write-Error "Failed to create resource group: $_"
    exit 1
}

# Create Service Bus namespace if requested
if ($CreateNamespace) {
    try {
        Write-Host "Creating Service Bus namespace: $NamespaceName" -ForegroundColor Yellow
        New-AzServiceBusNamespace -Name $NamespaceName -ResourceGroupName $ResourceGroupName -Location $Location -SkuName Standard
        
        # Wait for namespace to be ready
        do {
            Start-Sleep -Seconds 10
            $namespace = Get-AzServiceBusNamespace -Name $NamespaceName -ResourceGroupName $ResourceGroupName
            Write-Host "Waiting for namespace to be ready... Status: $($namespace.Status)" -ForegroundColor Yellow
        } while ($namespace.Status -ne "Active")
        
    } catch {
        Write-Error "Failed to create Service Bus namespace: $_"
        exit 1
    }
}

# Create the queue
try {
    Write-Host "Creating queue: $QueueName" -ForegroundColor Yellow
    
    # Check if queue already exists
    $existingQueue = Get-AzServiceBusQueue -Name $QueueName -NamespaceName $NamespaceName -ResourceGroupName $ResourceGroupName -ErrorAction SilentlyContinue
    
    if ($existingQueue) {
        Write-Host "Queue $QueueName already exists" -ForegroundColor Green
    } else {
        # Create queue with dead letter settings
        New-AzServiceBusQueue -Name $QueueName -NamespaceName $NamespaceName -ResourceGroupName $ResourceGroupName `
            -EnableDeadLetteringOnMessageExpiration $true `
            -DeadLetteringOnFilterEvaluationException $true `
            -MaxDeliveryCount 3 `
            -LockDuration "00:05:00" `
            -DefaultMessageTimeToLive "1.00:00:00"
        
        Write-Host "Queue $QueueName created successfully" -ForegroundColor Green
    }
    
} catch {
    Write-Error "Failed to create queue: $_"
    exit 1
}

# Get connection string
try {
    Write-Host "Retrieving connection string..." -ForegroundColor Yellow
    $connectionString = Get-AzServiceBusKey -Name RootManageSharedAccessKey -NamespaceName $NamespaceName -ResourceGroupName $ResourceGroupName
    
    Write-Host "`nSetup completed successfully!" -ForegroundColor Green
    Write-Host "`nConnection String:" -ForegroundColor Cyan
    Write-Host $connectionString.PrimaryConnectionString -ForegroundColor White
    
    Write-Host "`nUpdate your appsettings.json files with this connection string:" -ForegroundColor Yellow
    Write-Host @"
{
  "ServiceBus": {
    "ConnectionString": "$($connectionString.PrimaryConnectionString)",
    "HomeworkSyncQueueName": "$QueueName",
    "UseLocalEmulator": false
  }
}
"@ -ForegroundColor White

} catch {
    Write-Error "Failed to retrieve connection string: $_"
    exit 1
}

Write-Host "`nNext steps:" -ForegroundColor Green
Write-Host "1. Update the connection strings in your appsettings.json files" -ForegroundColor White
Write-Host "2. Run the applications: dotnet run" -ForegroundColor White
Write-Host "3. Run the tests: dotnet test" -ForegroundColor White
