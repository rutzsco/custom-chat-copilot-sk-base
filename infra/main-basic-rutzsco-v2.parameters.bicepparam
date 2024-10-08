using 'main-basic.bicep'

@description('ID of the service principal that will be granted access to the Key Vault')
param principalId = 'a247cd79-ef65-47ab-88a5-739d5d06945e'

@description('If you have an existing Cog Services Account, provide the name here')
param existingCogServicesName = 'rutzscodev-openai'

@description('If you have an existing Container Registry Account, provide the name here')
param existingContainerRegistryName = 'rutzscodevcr'

param environmentName = 'rutzscodev-ai-copilot'
param location = 'eastus2'
param backendExists = false
param backendDefinition = {
  settings: []
}
param appContainerAppEnvironmentWorkloadProfileName = 'app'
param containerAppEnvironmentWorkloadProfiles = [
  {
    name: 'app'
    workloadProfileType: 'D4'
    minimumCount: 1
    maximumCount: 10
  }
]

param useManagedIdentityResourceAccess = true
param azureChatGptStandardDeploymentName = 'gpt-4o'
param azureEmbeddingDeploymentName = 'text-embedding'
