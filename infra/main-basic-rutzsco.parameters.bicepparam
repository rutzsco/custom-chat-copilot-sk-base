using 'main-basic.bicep'

@description('ID of the service principal that will be granted access to the Key Vault')
param principalId = 'cbe636d7-ae22-4c39-87b6-8dab6dd901fa'

@description('If you have an existing Cog Services Account, provide the name here')
param existingCogServicesName = 'rutzsco-demo-openai'
param existingCogServicesResourceGroup  = 'rutzsco-demo-openai'

@description('If you have an existing Container Registry Account, provide the name here')
param existingContainerRegistryName = 'rutzscolabcr'
param existingContainerRegistryResourceGroup = 'rutzsco-core-cicd'

param environmentName = 'rutzsco-chat-copilot-dev'
param location = 'eastus'
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
param azureChatGptPremiumDeploymentName = 'gpt-4o'
param azureEmbeddingDeploymentName = 'text-embedding'

@description('If you have an existing VNET to use, provide the name here')
param virtualNetworkName = ''
param virtualNetworkResourceGroupName = ''
param privateEndpointSubnetName = ''
param privateEndpointSubnetAddressPrefix = ''
param containerAppSubnetName = ''
param containerAppSubnetAddressPrefix = ''
