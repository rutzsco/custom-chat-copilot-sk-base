# App Build and Deploy using ACR Commands

## Step 1 - Deploy Azure Resources

### 1.1 - Create a Resource Group

```bash
$RESOURCE_GROUP="rg-chat-copilot-demo"
$REGION="eastus2"

az group create --name $RESOURCE_GROUP --location $REGION
```

### 1.2 - Edit the Bicep Parameter file

Edit the infra/main-basic.parameters.bicepparam parameters as needed. At a minimum, you should change the principal Id to match your Id so that you have access to the key vault.

```bash
cd custom-chat-copilot-sk-base
cd infra
code main-basic.parameters.bicepparam 
```

### 1.3 - Deploy Resources via Bicep

```bash
$RESOURCE_GROUP="rg-chat-copilot-demo"
$TEMPLATE_FILE_NAME="main-basic.bicep"
$PARAMETER_FILE_NAME="yourParameters.bicepparam"

cd custom-chat-copilot-sk-base
cd infra
az deployment group create --resource-group $RESOURCE_GROUP --template-file $TEMPLATE_FILE_NAME --parameters $PARAMETER_FILE_NAME
```

## Step 2: Clone Repository

Open a terminal in Azure CLoud Shell and clone the repository

```bash
git clone https://github.com/rutzsco/custom-chat-copilot-sk-base.git
```

## Step 3: Edit Website Configuration (Optional)

Edit the AppSettings.json file to customize the look and feel of the website:

```bash
cd custom-chat-copilot-sk-base
cd app/frontend/wwwroot
code appsettings.json
```

The contents of the file should look like this (replace the image URL with your own):

```json
{
  "LogoImagePath": "<pathToYour-512x512-Logo>",
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

Before you run this command interactively, you should ensure that your account is a has the `acrpush` role in the Azure Container Registry (ACR) that you created in the previous steps.

```bash
$ACR_NAME="yourACRName"
$IMAGE_NAME="custom-chat-copilot/chat-app:v1"

cd custom-chat-copilot-sk-base
cd app
az acr build --registry $ACR_NAME --image $IMAGE_NAME --file Dockerfile .
```

## Step 5: Deploy app to an Azure Container App (ACA)

Deploy the image built in the previous step to your Azure Container App.

> NOTE: this does not seem to be the correct command yet...  Go into the ACA - Revisions page and deploy a new revision from the ACR and that should work better.  This command will be updated in the near future...

```bash
$APP_NAME="yourContainerAppName"
$RESOURCE_GROUP="rg-chat-copilot-demo"
$IMAGE_NAME="custom-chat-copilot/chat-app:v1"

az containerapp update --name $APP_NAME --resource-group $RESOURCE_GROUP --image $IMAGE_NAME
az containerapp revision list --name $APP_NAME --resource-group $RESOURCE_GROUP -o table
```
