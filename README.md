# custom-chat-copilot-sk-base

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
