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

**NOTE**: You can specify the following command if you want to use an existing vNet and secure all services behind private endpoints. You will need a vNet with a /24 CIDR range in order to use this option.

```shell
azd env set AZURE_VNET_NAME <vnet-name>
azd env set AZURE_VNET_RESOURCE_GROUP_NAME <vnet-resource-group-name>
azd env set AZURE_CONTAINER_APP_SUBNET_NAME <azure-container-app-subnet-name>
azd env set AZURE_CONTAINER_APP_SUBNET_ADDRESS_PREFIX <azure-container-app-subnet-address-prefix>
azd env set AZURE_PRIVATE_ENDPOINT_SUBNET_NAME <azure-private-endpoint-subnet-name>
azd env set AZURE_PRIVATE_ENDPOINT_SUBNET_ADDRESS_PREFIX <azure-private-endpoint-subnet-address-prefix>
azd env set AZURE_MONITOR_PRIVATE_LINK_SCOPE_NAME <azure-monitor-private-link-scope-name>
azd env set AZURE_MONITOR_PRIVATE_LINK_SCOPE_RESOURCE_GROUP_NAME <azure-monitor-private-link-scope-resource-group-name>
```

**NOTE**: If you want to use an OpenAI service that already exists somewhere else, you can disable the IaC from deploying the OpenAI service.

```shell
azd env set SHOULD_DEPLOY_AZURE_OPENAI_SERVICE false
```

**NOTE**: If you want to use a Entra ID service principal to authorize requests to the OpenAI Service (using the On-Behalf-Of flow) behind API Management, you can specify the following commands to indicate the Entra ID values. The authority is the FQDN of the Entra ID tenant in either Azure Commercial or one of the sovereign clouds.

```shell
azd env set AZURE_SP_CLIENT_ID <client-id>
azd env set AZURE_SP_CLIENT_SECRET <client-secret>
azd env set AZURE_TENANT_ID <tenant-id>
azd env set AZURE_SP_OPENAI_AUDIENCE <audience-of-the-OpenAI-service-principal>
azd env set AZURE_AUTHORITY_HOST https://login.microsoftonline.com/
azd env set AZURE_SP_CLIENT_ID_SCOPE api://<client-id>/user_impersonation
azd end set AZURE_OPENAI_ENDPOINT <apim-endpoint>
```

**NOTE**: If you need to specify an API Management subscription key for the OpenAI service, you can specify the following command.

```shell
azd env set OCP_APIM_SUBSCRIPTION_KEY <apim-subscription-key>
```

**NOTE**: If you want to use managed identities to access the various Azure services instead of keys, you can specify the following command.

```shell
azd env set USE_MANAGED_IDENTITY_RESOURCE_ACCESS true
```

Run the following command to build, provision & deploy the application.

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
#### Core Settings - Managed Identity

| **Setting**                          | **Description**                                                                 | **Value**                                                                 |
|--------------------------------------|---------------------------------------------------------------------------------|-------------------------------------------------------------------------|
| `AzureStorageAccountEndpoint`       | Connection string for the Azure Storage account, containing authentication information and storage endpoint details. | `"DefaultEndpointsProtocol=https;AccountName=<SERVICENAME>;AccountKey=...;EndpointSuffix=core.windows.net"` |
| `AzureSearchServiceEndpoint`         | The endpoint URL for the Azure Search Service.                                 | `"https://<SERVICENAME>.search.windows.net"`                              |
| `AOAIPremiumServiceEndpoint`          | The endpoint URL for the Azure OpenAI Premium services.                        | `"https://<SERVICENAME>.openai.azure.com/"`                               |
| `AOAIPremiumChatGptDeployment`         | The specific deployment of ChatGPT model used in the Azure OpenAI Premium services. | `"gpt-4"`                                                              |
| `AOAIStandardServiceEndpoint`          | The endpoint URL for the Azure OpenAI Standard services.                       | `"https://<SERVICENAME>.openai.azure.com/"`                               |
| `AOAIStandardChatGptDeployment`         | The specific deployment of ChatGPT model used in the Azure OpenAI Standard services. | `"chatgpt16k"`                                                        |
| `AOAIEmbeddingsDeployment`             | The specific deployment of the text embedding model used in the Azure OpenAI services. | `"text-embedding"`                                                   |
| `CosmosDBEndpoint`             | Connection string for accessing Azure Cosmos DB, including authentication information and endpoint details. | `"AccountEndpoint=https://<SERVICENAME>.documents.azure.com:443/;AccountKey=...;"` |

#### Core Settings - Keys

| **Category**               | **Setting**                          | **Description**                                                                 | **Value**                                                                 |
|----------------------------|--------------------------------------|---------------------------------------------------------------------------------|-------------------------------------------------------------------------|
| **Azure Storage**           | `AzureStorageAccountConnectionString`| Connection string for the Azure Storage account, containing authentication information and storage endpoint details. | `"DefaultEndpointsProtocol=https;AccountName=<SERVICENAME>;AccountKey=...;EndpointSuffix=core.windows.net"` |
| **Azure Search Service**    | `AzureSearchServiceEndpoint`         | The endpoint URL for the Azure Search Service.                                 | `"https://<SERVICENAME>.search.windows.net"`                              |
|                            | `AzureSearchServiceKey`               | The primary administrative API key for the Azure Search Service.               | `"<APIKEY>"`                                                              |
| **Azure OpenAI Services**   | `AOAIPremiumServiceEndpoint`          | The endpoint URL for the Azure OpenAI Premium services.                        | `"https://<SERVICENAME>.openai.azure.com/"`                               |
|                            | `AOAIPremiumServiceKey`                | The authentication key for accessing the Azure OpenAI Premium services.        | `"<APIKEY>"`                                                              |
|                            | `AOAIPremiumChatGptDeployment`         | The specific deployment of ChatGPT model used in the Azure OpenAI Premium services. | `"gpt-4"`                                                              |
|                            | `AOAIStandardServiceEndpoint`          | The endpoint URL for the Azure OpenAI Standard services.                       | `"https://<SERVICENAME>.openai.azure.com/"`                               |
|                            | `AOAIStandardServiceKey`                | The authentication key for accessing the Azure OpenAI Standard services.       | `"f4471e39c00e4dfd86ae15bc3bcf68b1"`                                      |
|                            | `AOAIStandardChatGptDeployment`         | The specific deployment of ChatGPT model used in the Azure OpenAI Standard services. | `"chatgpt16k"`                                                        |
|                            | `AOAIEmbeddingsDeployment`             | The specific deployment of the text embedding model used in the Azure OpenAI services. | `"text-embedding"`                                                   |
| **Cosmos DB**               | `CosmosDBConnectionString`             | Connection string for accessing Azure Cosmos DB, including authentication information and endpoint details. | `"AccountEndpoint=https://<SERVICENAME>.documents.azure.com:443/;AccountKey=...;"` |
#### Optional Settings

| **Category**               | **Setting**                          | **Description**                                                                 | **Value**                                                                 |
|----------------------------|--------------------------------------|---------------------------------------------------------------------------------|-------------------------------------------------------------------------|
| **Azure Storage**           | `AzureStorageUserUploadContainer`    | The name of the container in Azure Blob Storage where user uploads are stored.  | `"content"`                                                              |
| **Ingestion Pipeline API**  | `IngestionPipelineAPI`                 | The endpoint URL for the ingestion pipeline API.                              | `"https://<SERVICENAME>.azurewebsites.net/"`                              |
|                            | `IngestionPipelineAPIKey`               | The API key for authenticating requests to the ingestion pipeline.             | `"<APIKEY>"`                                                              |
| **Additional Settings**     | `EnableDataProtectionBlobKeyStorage`    | Boolean flag to enable or disable blob key storage under the data protection mechanism. | `"false"`                                                             |

## RBAC

Storage Blob Data Contributor scope to Storage Account
Search Index Data Contributor scope Azure AI Search
Cognitive Services OpenAI Contributor scope OpenAI
