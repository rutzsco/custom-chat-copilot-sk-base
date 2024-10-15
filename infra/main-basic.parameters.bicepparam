using 'main-basic.bicep'

@description('ID of the service principal that will be granted access to the Key Vault')
param principalId = '99999999-9999-9999-9999-999999999999'

@description('If you have an existing Cog Services Account, provide the name here')
param existingCogServicesName = ''
param existingCogServicesResourceGroup  = ''

@description('If you have an existing Container Registry Account, provide the name here')
param existingContainerRegistryName = ''
param existingContainerRegistryResourceGroup = ''

param environmentName = 'CI'
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
