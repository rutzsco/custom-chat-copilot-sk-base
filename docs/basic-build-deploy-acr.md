# Basic Build and Deploy using ACR Tasks

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

## Step 3: ACR build and push


```bash
cd app
az acr build --registry rutzscolabcr --image dhr/chat-copilot:v1 --file Dockerfile .
```