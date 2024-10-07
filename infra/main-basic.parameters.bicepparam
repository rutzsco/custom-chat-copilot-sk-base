using 'main-basic.bicep'

@description('ID of the service principal that will be granted access to the Key Vault')
param principalId = 'af35198e-8dc7-4a2e-a41e-b2ba79bebd51'

@description('If you have an existing Cog Services Account, provide the name here')
param existingCogServicesName = ''

@description('If you have an existing Container Registry Account, provide the name here')
param existingContainerRegistryName = ''

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

param azureChatGptStandardDeploymentName = 'chat'
param azureChatGptPremiumDeploymentName = 'chat-gpt4'
param azureEmbeddingDeploymentName = 'text-embedding'
param azureEmbeddingModelName = 'text-embedding-ada-002'
param embeddingDeploymentCapacity = 30
param azureOpenAIChatGptStandardModelName = 'gpt-35-turbo'
param azureOpenAIChatGptStandardModelVersion = '0613'
param chatGptStandardDeploymentCapacity = 10
param azureOpenAIChatGptPremiumModelName = 'gpt-4o'
param azureOpenAIChatGptPremiumModelVersion = '2024-05-13'
param chatGptPremiumDeploymentCapacity = 10

@description('If you have an existing VNET to use, provide the name here')
param virtualNetworkName = ''
param virtualNetworkResourceGroupName = ''
param privateEndpointSubnetName = ''
param privateEndpointSubnetAddressPrefix = ''
param containerAppSubnetName = ''
param containerAppSubnetAddressPrefix = ''
