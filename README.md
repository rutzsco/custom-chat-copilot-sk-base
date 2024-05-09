# custom-chat-copilot-sk-base

# Chat Application 

## Resource Deployment

The applications azure resourcees can be deployed with main.bicep file located in the `infra` folder of this repository. The main.bicep file is used to define and deploy Azure infrastructure resources in a declarative way. The following azure resources will be created:

- Azure Container Apps (Application Hosting)
- Storage Account (Blob)
- CosmosDB (NO SQL Application Database)
- Azure AI Search (Vector Database)
- Azure Function (Generate index)
- Key Vault (Store secrets)
- Managed Identity (Retrieve secrets)
- Azure OpenAI (Human language interpretation)

***REFERENCE***
- https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deploy-vscode
- https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-bicep
- https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd?tabs=winget-windows%2Cbrew-mac%2Cscript-linux&pivots=os-windows

## Code Deployment

### BUILD

```bash
docker login <ACRNAME>.azurecr.io
```
```bash
cd app

docker build . -t custom-chat-copilot-sk-base/chat-app
```

### DEPLOY

#### Azure Developer CLI

```bash
azd up
```

#### Manual

```bash
docker tag custom-chat-copilot-sk-base/chat-app <ACRNAME>.azurecr.io/custom-chat-copilot-sk-base/chat-app:<VERSION>
```

```bash
docker push <ACRNAME>.azurecr.io/custom-chat-copilot-sk-base/chat-app:<VERSION>
```

```bash
az containerapp update --name <APPLICATION_NAME> --resource-group <RESOURCE_GROUP_NAME> --image <IMAGE_NAME>
```

## Local Development

***appsettings.Development.json***

```bash
{
  "AzureSearchContentIndex": "",
  "AzureSearchServiceEndpoint": "",
  "AzureSearchServiceKey": "",
  "AzureStorageAccountEndpoint": "",
  "AzureStorageContainer": "content",
  "AzureStorageAccountConnectionString": "",
  "AOAIPremiumServiceEndpoint": "",
  "AOAIPremiumServiceKey": "",
  "AOAIPremiumChatGptDeployment": "",
  "AOAIStandardServiceEndpoint": "",
  "AOAIStandardServiceKey": "",
  "AOAIStandardChatGptDeployment": "",
  "AOAIEmbeddingsDeployment": "",
  "CosmosDBConnectionString": ""
}
```
