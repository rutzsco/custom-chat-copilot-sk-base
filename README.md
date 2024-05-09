# Chat Application 

## Resource Deployment

The applications azure resourcees can be deployed with main.bicep file located in the infrastructure folder of this repository. The main.bicep file is used to define and deploy Azure infrastructure resources in a declarative way. The following azure resources will be created:

- Azure Container Apps (Applicaiton Hosting)
- Storage Account (Blob)
- CosmosDB (NO SQL Application Database)
- Azure AI Search (Vector Database)

***REFERENCE***
- https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deploy-vscode
- https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-bicep

## Code Deployment

### BUILD

```bash
docker login <ACRNAME>.azurecr.io
```
```bash
docker build . -t custom-chat-copilot-sk-base/chat-app
```

### DEPLOY

```bash
docker tag custom-chat-copilot-sk-base/chat-app <ACRNAME>.azurecr.io/custom-chat-copilot-sk-base/chat-app:<VERSION>
```
```bash
docker push <ACRNAME>.azurecr.io/custom-chat-copilot-sk-base/chat-app:<VERSION>
```
```bash
az containerapp update --name <APPLICATION_NAME> --resource-group <RESOURCE_GROUP_NAME> --image <IMAGE_NAME>
```
## Application Settings Documentation

### Sample Settings file

***appsettings.Development.json***

```bash
{
  "AzureStorageUserUploadContainer": "content",
  "AzureStorageAccountConnectionString": "",
  "AzureSearchServiceEndpoint": "https://<SERVICENAME>.search.windows.net",
  "AzureSearchServiceKey": "<APIKEY>",
  "AOAIPremiumServiceEndpoint": "https://<SERVICENAME>.openai.azure.com/",
  "AOAIPremiumServiceKey": "<APIKEY>",
  "AOAIPremiumChatGptDeployment": "gpt-4",
  "AOAIStandardServiceEndpoint": "https://<SERVICENAME>.azure-api.net/",
  "AOAIStandardServiceKey": "<APIKEY>",
  "AOAIStandardChatGptDeployment": "chatgpt16k",
  "AOAIEmbeddingsDeployment": "text-embedding",
  "CosmosDBConnectionString": "AccountEndpoint=https://rutzsco-chat-copilot-demo.documents.azure.com:443/;AccountKey=<APIKEY>;",
  "IngestionPipelineAPI": "https://<SERVICENAME>.azurewebsites.net/",
  "IngestionPipelineAPIKey": "<APIKEY>",
  "EnableDataProtectionBlobKeyStorage" : "false"
}
```
This documentation outlines the various application settings used in the configuration of Azure services and other APIs.

### Azure Storage

#### `AzureStorageUserUploadContainer`
- **Description**: The name of the container in Azure Blob Storage where user uploads are stored.
- **Value**: `"content"`

#### `AzureStorageAccountConnectionString`
- **Description**: Connection string for the Azure Storage account, containing authentication information and storage endpoint details.
- **Value**: `"DefaultEndpointsProtocol=https;AccountName=<SERVICENAME>;AccountKey=...;EndpointSuffix=core.windows.net"`

### Azure Search Service

#### `AzureSearchServiceEndpoint`
- **Description**: The endpoint URL for the Azure Search Service.
- **Value**: `"https://<SERVICENAME>.search.windows.net"`

#### `AzureSearchServiceKey`
- **Description**: The primary administrative API key for the Azure Search Service.
- **Value**: `"<APIKEY>"`

### Azure OpenAI Services

#### `AOAIPremiumServiceEndpoint`
- **Description**: The endpoint URL for the Azure OpenAI Premium services.
- **Value**: `"https://<SERVICENAME>.openai.azure.com/"`

#### `AOAIPremiumServiceKey`
- **Description**: The authentication key for accessing the Azure OpenAI Premium services.
- **Value**: `"<APIKEY>"`

#### `AOAIPremiumChatGptDeployment`
- **Description**: The specific deployment of ChatGPT model used in the Azure OpenAI Premium services.
- **Value**: `"gpt-4"`

#### `AOAIStandardServiceEndpoint`
- **Description**: The endpoint URL for the Azure OpenAI Standard services.
- **Value**: `"https://<SERVICENAME>.openai.azure.com/"`

#### `AOAIStandardServiceKey`
- **Description**: The authentication key for accessing the Azure OpenAI Standard services.
- **Value**: `"f4471e39c00e4dfd86ae15bc3bcf68b1"`

#### `AOAIStandardChatGptDeployment`
- **Description**: The specific deployment of ChatGPT model used in the Azure OpenAI Standard services.
- **Value**: `"chatgpt16k"`

#### `AOAIEmbeddingsDeployment`
- **Description**: The specific deployment of the text embedding model used in the Azure OpenAI services.
- **Value**: `"text-embedding"`

### Cosmos DB

#### `CosmosDBConnectionString`
- **Description**: Connection string for accessing Azure Cosmos DB, including authentication information and endpoint details.
- **Value**: `"AccountEndpoint=https://<SERVICENAME>.documents.azure.com:443/;AccountKey=...;"`

### Ingestion Pipeline API

#### `IngestionPipelineAPI`
- **Description**: The endpoint URL for the ingestion pipeline API.
- **Value**: `"https://<SERVICENAME>.azurewebsites.net/"`

#### `IngestionPipelineAPIKey`
- **Description**: The API key for authenticating requests to the ingestion pipeline.
- **Value**: `"<APIKEY>"`

### Additional Settings

#### `EnableDataProtectionBlobKeyStorage`
- **Description**: Boolean flag to enable or disable blob key storage under the data protection mechanism.
- **Value**: `"false"`

