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

param useManagedIdentityResourceAccess bool = true

param virtualNetworkName string = ''
param virtualNetworkResourceGroupName string = ''
param containerAppSubnetName string = ''
@description('Address prefix for the container app subnet')
param containerAppSubnetAddressPrefix string = ''
param privateEndpointSubnetName string = ''
@description('Address prefix for the private endpoint subnet')
param privateEndpointSubnetAddressPrefix string = ''

@description('Name of the text embedding model deployment')
param azureEmbeddingDeploymentName string = 'text-embedding'
param azureEmbeddingModelName string = 'text-embedding-ada-002'
param embeddingDeploymentCapacity int = 30
@description('Name of the chat GPT deployment')
param azureChatGptStandardDeploymentName string = 'chat'
@description('Name of the chat GPT model. Default: gpt-35-turbo')
@allowed(['gpt-35-turbo', 'gpt-4', 'gpt-35-turbo-16k', 'gpt-4-16k', 'gpt-4o'])
param azureOpenAIChatGptStandardModelName string = 'gpt-35-turbo'
param azureOpenAIChatGptStandardModelVersion string = '0613'
@description('Capacity of the chat GPT deployment. Default: 10')
param chatGptStandardDeploymentCapacity int = 10
@description('Name of the chat GPT deployment')
param azureChatGptPremiumDeploymentName string = 'chat-gpt4'
@description('Name of the chat GPT model. Default: gpt-35-turbo')
@allowed(['gpt-35-turbo', 'gpt-4', 'gpt-35-turbo-16k', 'gpt-4-16k', 'gpt-4o'])
param azureOpenAIChatGptPremiumModelName string = 'gpt-4o'
param azureOpenAIChatGptPremiumModelVersion string = '2024-05-13'
@description('Capacity of the chat GPT deployment. Default: 10')
param chatGptPremiumDeploymentCapacity int = 10

@description('Name of an existing Cognitive Services account to use')
param existingCogServicesName string = ''
@description('Name of ResourceGroup for an existing  Cognitive Services account to use')
param existingCogServicesResourceGroup string = resourceGroup().name

@description('Name of an existing Azure Container Registry to use')
param existingContainerRegistryName string = ''
@description('Name of ResourceGroup for an existing Azure Container Registry to use')
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

module virtualNetwork './app/virtual-network.bicep' =
  if (virtualNetworkName != '') {
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

module registry './app/registry.bicep' = {
  name: 'registry${deploymentSuffix}'
  params: {
    existingContainerRegistryName: existingContainerRegistryName
    existingContainerRegistryResourceGroup: existingContainerRegistryResourceGroup
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    tags: tags
    keyVaultName: keyVault.outputs.name
    publicNetworkAccess: 'Enabled' // virtualNetworkName != '' 'Disabled' : 'Enabled'
    privateEndpointSubnetId: '' // virtualNetworkName != '' virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: '' // virtualNetworkName != '' '${abbrs.networkPrivateLinkServices}${abbrs.containerRegistryRegistries}${resourceToken}': ''
  }
}

module registrySecret './shared/keyvault-registry-secret.bicep' =
  if (existingContainerRegistryName == '') {
    name: 'registry-secret${deploymentSuffix}'
    params: {
      registryName: registry.outputs.name
      registryResourceGroup: registry.outputs.resourceGroupName
      keyVaultName: keyVault.outputs.name
      name: registry.outputs.registrySecretName
    }
  }

module cosmos './app/cosmosdb.bicep' = {
  name: 'cosmos${deploymentSuffix}'
  params: {
    accountName: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    databaseName: 'ChatHistory'
    location: location
    tags: tags
    deploymentSuffix: deploymentSuffix
    keyVaultName: keyVault.outputs.name
    privateEndpointSubnetId: ''
    privateEndpointName: ''
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
    userPrincipalId: principalId
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
    deploymentSuffix: deploymentSuffix
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
    deploymentSuffix: deploymentSuffix
    publicNetworkAccess: 'enabled'
    privateEndpointSubnetId: ''
    privateEndpointName: ''
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

module azureOpenAi './app/cognitive-services.bicep' = {
  name: 'openai${deploymentSuffix}'
  dependsOn: [search]
  params: {
    existingCogServicesName: existingCogServicesName
    existingCogServicesResourceGroup: existingCogServicesResourceGroup
    name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: location
    tags: tags
    deploymentSuffix: deploymentSuffix
    deployments: concat(
      [
        {
          name: azureEmbeddingDeploymentName
          model: {
            format: 'OpenAI'
            name: azureEmbeddingModelName
            version: '2'
          }
          sku: {
            name: 'Standard'
            capacity: embeddingDeploymentCapacity
          }
        }
      ],
      [
        {
          name: azureChatGptStandardDeploymentName
          model: {
            format: 'OpenAI'
            name: azureOpenAIChatGptStandardModelName
            version: azureOpenAIChatGptStandardModelVersion
          }
          sku: {
            name: 'Standard'
            capacity: chatGptStandardDeploymentCapacity
          }
        }
      ],
      [
        {
          name: azureChatGptPremiumDeploymentName
          model: {
            format: 'OpenAI'
            name: azureOpenAIChatGptPremiumModelName
            version: azureOpenAIChatGptPremiumModelVersion
          }
          sku: {
            name: 'Standard'
            capacity: chatGptPremiumDeploymentCapacity
          }
        }
      ]
    )
    publicNetworkAccess: 'Enabled' // virtualNetworkName != '' 'Disabled' : 'Enabled'
    privateEndpointSubnetId: '' // virtualNetworkName != '' virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: '' // virtualNetworkName != '' '${abbrs.networkPrivateLinkServices}${abbrs.cognitiveServicesAccounts}${resourceToken}': ''
  }
}

module cognitiveSecret './shared/keyvault-cognitive-secret.bicep' = {
  name: 'openai-secret${deploymentSuffix}'
  params: {
    cognitiveServiceName: azureOpenAi.outputs.name
    cognitiveServiceResourceGroup: azureOpenAi.outputs.resourceGroupName
    keyVaultName: keyVault.outputs.name
    name: azureOpenAi.outputs.cognitiveServicesKeySecretName
  }
}

var appDefinition = {
  settings: (union(
    array(backendDefinition.settings),
    [
      {
        name: 'acrpassword'
        value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${registry.outputs.registrySecretName}'
        secretRef: 'acrpassword'
        secret: true
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
        value: azureOpenAi.outputs.endpoint
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
        value: string(true)
      }
      {
        name: 'UseManagedIdentityResourceAccess'
        value: string(useManagedIdentityResourceAccess)
      }
      {
        name: 'UseManagedManagedIdentityClientId'
        value: managedIdentity.outputs.identityClientId
      }
    ],
    (useManagedIdentityResourceAccess)
      ? [
          {
            name: 'CosmosDBEndpoint'
            value: cosmos.outputs.endpoint
          }
        ]
      : [
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
            name: 'AOAIStandardServiceKey'
            value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${azureOpenAi.outputs.cognitiveServicesKeySecretName}'
            secretRef: 'aoaistandardservicekey'
            secret: true
          }
        ]
  ))
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
    containerRegistryName: registry.outputs.name
    containerRegistryResourceGroup: registry.outputs.resourceGroupName
    exists: backendExists
    appDefinition: appDefinition
    identityName: managedIdentity.outputs.identityName
    clientId: ''
    clientIdScope: ''
    clientSecretSecretName: ''
    tokenStoreSasSecretName: ''
  }
}
