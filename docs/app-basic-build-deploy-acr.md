# App Build and Deploy using ACR Commands

## Step 1 - Deploy Azure Resources

### 1.1 - Create a Resource Group

```bash
$resourceGroupName="rg-chat-copilot-demo"
$region="eastus"

az group create --name $resourceGroupName --location $region
```

### 1.2 - Edit the Bicep Parameter file

Edit the infra/main-basic.parameters.bicepparam parameters as needed. At a minimum, you should change the principal Id to match your Id so that you have access to the key vault.

```bash
cd custom-chat-copilot-sk-base/infra

code main-basic.parameters.bicepparam 
```

### 1.3 - Deploy Resources via Bicep

```bash
$resourceGroupName="rg-chat-copilot-demo"
$templateFileName="main-basic.bicep"
$parameterFileName="yourParameters.bicepparam"

cd custom-chat-copilot-sk-base/infra
az deployment group create --resource-group $resourceGroupName --template-file $templateFileName --parameters $parameterFileName
```

## Step 2: Clone Repository

Open a terminal in Azure CLoud Shell and clone the repository

```bash
git clone https://github.com/rutzsco/custom-chat-copilot-sk-base.git
```

## Step 3: Edit Website Configuration (Optional)

Edit the AppSettings.json file to customize the look and feel of the website:

```bash
cd custom-chat-copilot-sk-base/app/backend/wwwroot

code appsettings.json
```

The contents of the file should look like this (replace the image URL with your own):

```json
{
  "LogoImagePath": "<pathToYourLogoIcon-512x512>",
  "ColorPaletteLightAppbarBackground": "#2f2e33",
  "ColorPaletteLightSecondary": "#6eb8ab",
  "ColorPaletteLightPrimary": "#00755f",
  "ShowSampleQuestions": true,
  "ShowPremiumAOAIToggleSelection": false,
  "ShowFileUploadSelection": false,
  "ShowCollectionsSelection" :  true
}
```

## Step 4: Build and push image to ACR

```bash
$ACR_NAME="yourACRName"
$IMAGE_NAME="custom-chat-copilot/chat-app:v1"

cd custom-chat-copilot-sk-base/app
az acr build --registry $ACR_NAME --image custom-chat-copilot/chat-app:v1 --file Dockerfile .
```

## Step 5: Deploy app to an Azure Container App (ACA)

Deploy the image built in the previous step to your Azure Container App.

```bash
$APP_NAME="chatApp"
$RESOURCE_GROUP="rg-chat-copilot-demo"
$IMAGE_NAME="custom-chat-copilot/chat-app:v1"

az containerapp update --name $APP_NAME --resource-group $RESOURCE_GROUP --image $IMAGE_NAME
```
