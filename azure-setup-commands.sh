# Azure CLI commands to create a dedicated Service Bus access policy

# 1. Create a new access policy with specific permissions
az servicebus namespace authorization-rule create \
  --resource-group your-resource-group \
  --namespace-name ctaintegration \
  --name "LMS-ERP-Integration" \
  --rights Send Listen

# 2. Get the connection string for the new policy
az servicebus namespace authorization-rule keys list \
  --resource-group your-resource-group \
  --namespace-name ctaintegration \
  --name "LMS-ERP-Integration" \
  --query "primaryConnectionString" \
  --output tsv
