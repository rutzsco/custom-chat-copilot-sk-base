using 'main-basic.bicep'

@description('ID of the service principal that will be granted access to the Key Vault')
param principalId = 'af35198e-8dc7-4a2e-a41e-b2ba79bebd51'

@description('If you have an existing Cog Services Account, provide the name here')
param existingCogServicesName = ''
param existingCogServicesResourceGroup  = ''

@description('If you have an existing Container Registry Account, provide the name here')
param existingContainerRegistryName = ''
param existingContainerRegistryResourceGroup = ''

param environmentName = 'rg-copilot-demo-1014'
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
