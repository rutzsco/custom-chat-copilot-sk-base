using 'main-basic.bicep'

@description('ID of the service principal that will be granted access to the Key Vault')
param principalId = 'a247cd79-ef65-47ab-88a5-739d5d06945e'

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

param useManagedIdentityResourceAccess = false
param azureChatGptStandardDeploymentName = 'gpt-4o'
param azureEmbeddingDeploymentName = 'text-embedding'
