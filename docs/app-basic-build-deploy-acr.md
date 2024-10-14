# App Build and Deploy using ACR Tasks

## Prerequisites

 Infra Deployment

## App Build and Deploy

## Step 1: Clone Repository

```bash
git clone https://github.com/rutzsco/custom-chat-copilot-sk-base.git
```

## Step 2: Edit Configuration (Optional)

```bash
cd app/backend/wwwroot
code appsettings.json
```

```json
{
  "LogoImagePath": "",
  "ColorPaletteLightAppbarBackground": "",
  "ColorPaletteLightSecondary": "",
  "ColorPaletteLightPrimary": "",
  "ShowSampleQuestions": true,
  "ShowPremiumAOAIToggleSelection": false,
  "ShowFileUploadSelection": false,
  "ShowCollectionsSelection" :  true
}
```

## Step 3: Build and push image to ACR

```bash
cd app
az acr build --registry <ACR> --image <IMAGE_NAME> --file Dockerfile .
```
**Example**

```bash
cd app
az acr build --registry rutzscolabcr --image custom-chat-copilot/chat-app:v1 --file Dockerfile .
```

## Step 4: Deploy app to Azure Container Apps(ACA)


```bash
az containerapp update --name <NAME> --resource-group <RG> --image <IMAGE>
```

**Example**

```bash
az containerapp update --name chatApp --resource-group rutzsco-chat-copilot-demo --image custom-chat-copilot/chat-app:v1
```