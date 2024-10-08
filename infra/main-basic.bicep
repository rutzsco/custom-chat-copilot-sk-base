targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

param backendExists bool
@secure()
param backendDefinition object

@description('Id of the user or app to assign application roles')
param principalId string

@description('Workload profiles for the Container Apps environment')
param containerAppEnvironmentWorkloadProfiles array = []

@description('Name of the Container Apps Environment workload profile to use for the app')
param appContainerAppEnvironmentWorkloadProfileName string

param useManagedIdentityResourceAccess bool = false

param virtualNetworkName string = ''
param virtualNetworkResourceGroupName string = ''
param containerAppSubnetName string= ''
@description('Address prefix for the container app subnet')
param containerAppSubnetAddressPrefix string= ''
param privateEndpointSubnetName string= ''
@description('Address prefix for the private endpoint subnet')
param privateEndpointSubnetAddressPrefix string= ''

@description('Name of the text embedding model deployment')
param azureEmbeddingDeploymentName string = 'text-embedding'
@description('Name of the chat GPT deployment')
param azureChatGptStandardDeploymentName string = 'chat'
@description('Name of the chat GPT deployment')
param azureChatGptPremiumDeploymentName string = 'chat-gpt4'


@description('Name of an existing Cognitive Services account to use')
param existingCogServicesName string = ''
param existingCogServicesResourceGroup string = resourceGroup().name

@description('Name of an existing Azure Container Registry account to use')
param existingContainerRegistryName string = ''
@description('Name of ResourceGoupd for an existing Azure Container Registry account to use')
param existingContainerRegistryResourceGroup string = resourceGroup().name

param runDateTime string = utcNow()
var deploymentSuffix = '-${runDateTime}'

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
}

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(resourceGroup().id, environmentName, location))

module monitoring './app/monitoring.bicep' = {
  name: 'monitoring${deploymentSuffix}'
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
    azureMonitorPrivateLinkScopeName: ''
    azureMonitorPrivateLinkScopeResourceGroupName: ''
    privateEndpointSubnetId: ''
    privateEndpointName: ''
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

module dashboard './app/dashboard-web.bicep' = {
  name: 'dashboard${deploymentSuffix}'
  params: {
    name: '${abbrs.portalDashboards}${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    location: location
    tags: tags
  }
}

module managedIdentity './app/identity.bicep' = {
  name: 'identity${deploymentSuffix}'
  params: {
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
    location: location
    tags: tags
  }
}

module virtualNetwork './app/virtual-network.bicep' = if(virtualNetworkName != '') {
  name: 'virtual-network${deploymentSuffix}'
  params: {
    virtualNetworkName: virtualNetworkName
    location: location
    containerAppSubnetName: containerAppSubnetName
    containerAppSubnetAddressPrefix: containerAppSubnetAddressPrefix
    containerAppSubnetNsgName: '${abbrs.networkNetworkSecurityGroups}container-app-${resourceToken}'
    privateEndpointSubnetName: privateEndpointSubnetName
    privateEndpointSubnetAddressPrefix: privateEndpointSubnetAddressPrefix
    privateEndpointSubnetNsgName: '${abbrs.networkNetworkSecurityGroups}private-endpoint-${resourceToken}'
  }
  scope: resourceGroup(virtualNetworkResourceGroupName)
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing =  {
  scope: resourceGroup(existingContainerRegistryResourceGroup)
  name: existingContainerRegistryName
}

module registrySecret './shared/keyvault-secret.bicep' = {
  name: 'container-registry-password'
  params: {
    keyVaultName: keyVault.outputs.name
    name: 'container-registry-password'
    secretValue: registry.listCredentials().passwords[0].value
  }
}

module cosmos './app/cosmosdb.bicep' = {
  name: 'cosmos${deploymentSuffix}'
  params: {
    accountName: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    databaseName: 'ChatHistory' 
    location: location
    tags: tags
    keyVaultName: keyVault.outputs.name
    privateEndpointSubnetId: ''
    privateEndpointName:  ''
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

module keyVault './app/keyvault.bicep' = {
  name: 'keyvault${deploymentSuffix}'
  params: {
    location: location
    tags: tags
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    userPrincipalId: principalId
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
    publicNetworkAccess: 'Enabled'
    privateEndpointSubnetId: ''
    privateEndpointName: ''
  }
}

module appsEnv './app/apps-env.bicep' = {
  name: 'apps-env${deploymentSuffix}'
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
    containerAppSubnetId: ''
    containerAppEnvironmentWorkloadProfiles: containerAppEnvironmentWorkloadProfiles
  }
}

var storageAccountContainerName = 'content'
var dataProtectionKeysContainerName = 'dataprotectionkeys'

module storageAccount './app/storage-account.bicep' = {
  name: 'storage${deploymentSuffix}'
  params: {
    name: '${abbrs.storageStorageAccounts}${resourceToken}'
    location: location
    tags: tags
    keyVaultName: keyVault.outputs.name
    containers: [
      {
        name: storageAccountContainerName
      }
      {
        name: dataProtectionKeysContainerName
      }
    ]
    publicNetworkAccess: 'Enabled'
    allowBlobPublicAccess: true
    privateEndpointSubnetId: ''
    privateEndpointName: ''
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

module search './app/search-services.bicep' = {
  name: 'search${deploymentSuffix}'
  params: {
    location: location
    keyVaultName: keyVault.outputs.name
    name: '${abbrs.searchSearchServices}${resourceToken}'
    publicNetworkAccess: 'enabled'
    privateEndpointSubnetId: ''
    privateEndpointName: ''
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

resource azureOpenAi 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  scope: resourceGroup(existingCogServicesResourceGroup)
  name: existingCogServicesName
}

var appDefinition = {
  settings : (union(array(backendDefinition.settings), [
    {
      name: 'acrpassword'
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/container-registry-password'
      secretRef: 'acrpassword'
      secret: true
    }
    {
      name: 'CosmosDBConnectionString'
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${cosmos.outputs.connectionStringSecretName}'
      secretRef: 'cosmosdbconnectionstring'
      secret: true
    }
    {
      name: 'AzureStorageAccountConnectionString'
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${storageAccount.outputs.storageAccountConnectionStringSecretName}'
      secretRef: 'azurestorageconnectionstring'
      secret: true
    }    
    {
      name: 'AzureSearchServiceKey'
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${search.outputs.searchKeySecretName}'
      secretRef: 'azuresearchservicekey'
      secret: true
    }
    {
      name: 'CosmosDBEndpoint'
      value: cosmos.outputs.endpoint
    }
    {
      name: 'AzureStorageAccountEndpoint'
      value: storageAccount.outputs.primaryEndpoints.blob
    }
    {
      name: 'AzureStorageContainer'
      value: storageAccountContainerName
    }
    {
      name: 'AzureSearchServiceEndpoint'
      value: search.outputs.endpoint
    }
    {
      name: 'AOAIStandardServiceEndpoint'
      value: azureOpenAi.properties.endpoint
    }
    {
      name: 'AOAIStandardChatGptDeployment'
      value: azureChatGptStandardDeploymentName
    }
    {
      name: 'AOAIEmbeddingsDeployment'
      value: azureEmbeddingDeploymentName
    }
    {
      name: 'EnableDataProtectionBlobKeyStorage'
      value: string(false)
    }
    {
      name: 'UseManagedIdentityResourceAccess'
      value: string(true)
    }
  ]))
}

module app './app/app.bicep' = {
  name: 'app${deploymentSuffix}'
  params: {
    name: '${abbrs.appContainerApps}backend-${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerAppsEnvironmentWorkloadProfileName: appContainerAppEnvironmentWorkloadProfileName
    containerRegistryName: registry.name
    containerRegistryResourceGroup: existingContainerRegistryResourceGroup
    exists: backendExists
    appDefinition: appDefinition
    identityName: managedIdentity.outputs.identityName
    clientId: ''
    clientIdScope: ''
    clientSecretSecretName: ''
    tokenStoreSasSecretName: ''
  }
}
