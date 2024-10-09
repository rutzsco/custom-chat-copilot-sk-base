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

@description('Name of the chat GPT deployment')
param azureChatGptStandardDeploymentName string = 'chat'

@description('Name of the chat GPT model. Default: gpt-35-turbo')
@allowed([ 'gpt-35-turbo', 'gpt-4', 'gpt-35-turbo-16k', 'gpt-4-16k', 'gpt-4o' ])
param azureOpenAIChatGptStandardModelName string = 'gpt-35-turbo'

param azureOpenAIChatGptStandardModelVersion string ='0613'

@description('Capacity of the chat GPT deployment. Default: 10')
param chatGptStandardDeploymentCapacity int = 10

@description('Name of the chat GPT deployment')
param azureChatGptPremiumDeploymentName string = 'chat-gpt4'

@description('Name of the chat GPT model. Default: gpt-35-turbo')
@allowed([ 'gpt-35-turbo', 'gpt-4', 'gpt-35-turbo-16k', 'gpt-4-16k', 'gpt-4o' ])
param azureOpenAIChatGptPremiumModelName string = 'gpt-4o'

param azureOpenAIChatGptPremiumModelVersion string ='2024-05-13'

@description('Capacity of the chat GPT deployment. Default: 10')
param chatGptPremiumDeploymentCapacity int = 10

@description('Name of the embedding deployment. Default: embedding')
param azureEmbeddingDeploymentName string = 'embedding'

@description('Name of the embedding model. Default: text-embedding-ada-002')
param azureEmbeddingModelName string = 'text-embedding-ada-002'

@description('Capacity of the embedding deployment. Default: 30')
param embeddingDeploymentCapacity int = 30

@description('Name of the virtual network to use for the app. If empty, the app will be created without virtual network integration.')
param virtualNetworkName string

param virtualNetworkResourceGroupName string

param containerAppSubnetName string

@description('Address prefix for the container app subnet')
param containerAppSubnetAddressPrefix string

param privateEndpointSubnetName string

@description('Address prefix for the private endpoint subnet')
param privateEndpointSubnetAddressPrefix string

@description('Name of the Azure Monitor private link scope')
param azureMonitorPrivateLinkScopeName string

@description('Resource group name of the Azure Monitor private link scope')
param azureMonitorPrivateLinkScopeResourceGroupName string

@description('Workload profiles for the Container Apps environment')
param containerAppEnvironmentWorkloadProfiles array = []

@description('Name of the Container Apps Environment workload profile to use for the app')
param appContainerAppEnvironmentWorkloadProfileName string

@description('Should deploy Azure OpenAI service')
param shouldDeployAzureOpenAIService bool = true

param azureSpClientId string = ''
@secure()
param azureSpClientSecret string = ''
param azureTenantId string = ''
param azureAuthorityHost string = ''
param ocpApimSubscriptionKey string = ''
param azureSpOpenAiAudience string = ''
param azureOpenAiEndpoint string = ''
param azureSpClientIdScope string = ''
param useManagedIdentityResourceAccess bool = false

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
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
    azureMonitorPrivateLinkScopeName: !empty(virtualNetworkName) ? azureMonitorPrivateLinkScopeName : ''
    azureMonitorPrivateLinkScopeResourceGroupName: !empty(virtualNetworkName) ? azureMonitorPrivateLinkScopeResourceGroupName : ''
    privateEndpointSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: !empty(virtualNetworkName) ? '${abbrs.networkPrivateLinkServices}azureMonitorPrivateLinkService-${resourceToken}': ''
    publicNetworkAccessForIngestion: !empty(virtualNetworkName) ? 'Disabled' : 'Enabled'
    publicNetworkAccessForQuery: !empty(virtualNetworkName) ? 'Disabled' : 'Enabled'
  }
}

module dashboard './app/dashboard-web.bicep' = {
  name: 'dashboard'
  params: {
    name: '${abbrs.portalDashboards}${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    location: location
    tags: tags
  }
}

module managedIdentity './app/identity.bicep' = {
  name: 'identity'
  params: {
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
    location: location
    tags: tags
  }
}

module virtualNetwork './app/virtual-network.bicep' = if(!empty(virtualNetworkName)) {
  name: 'virtual-network'
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

module registry './app/registry.bicep' = {
  name: 'registry'
  params: {
    location: location
    tags: tags
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    keyVaultName: keyVault.outputs.name
    privateEndpointSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.privateEndpointSubnetId: ''
    publicNetworkAccess: !empty(virtualNetworkName) ? 'Disabled' : 'Enabled'
    privateEndpointName: !empty(virtualNetworkName) ? '${abbrs.networkPrivateLinkServices}${abbrs.containerRegistryRegistries}${resourceToken}': ''
  }
}

module cosmos './app/cosmosdb.bicep' = {
  name: 'cosmos'
  params: {
    accountName: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    databaseName: 'ChatHistory' 
    location: location
    tags: tags
    keyVaultName: keyVault.outputs.name
    privateEndpointSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: !empty(virtualNetworkName) ? '${abbrs.networkPrivateLinkServices}${abbrs.documentDBDatabaseAccounts}${resourceToken}': ''
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

module keyVault './app/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    tags: tags
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    userPrincipalId: principalId
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
    publicNetworkAccess: !empty(virtualNetworkName) ? 'Disabled' : 'Enabled'
    privateEndpointSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: !empty(virtualNetworkName) ? '${abbrs.networkPrivateLinkServices}${abbrs.keyVaultVaults}${resourceToken}': ''
  }
}

module appsEnv './app/apps-env.bicep' = {
  name: 'apps-env'
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
    containerAppSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.containerAppSubnetId : ''
    containerAppEnvironmentWorkloadProfiles: containerAppEnvironmentWorkloadProfiles
  }
}

var storageAccountContainerName = 'content'
var dataProtectionKeysContainerName = 'dataprotectionkeys'

module storageAccount './app/storage-account.bicep' = {
  name: 'storage'
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
    publicNetworkAccess: !empty(virtualNetworkName) ? 'Disabled' : 'Enabled'
    allowBlobPublicAccess: !empty(virtualNetworkName) ? false : true
    privateEndpointSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: !empty(virtualNetworkName) ? '${abbrs.networkPrivateLinkServices}${abbrs.storageStorageAccounts}${resourceToken}': ''
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: !empty(virtualNetworkName) ? 'Deny' : 'Allow'
    }
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

module search './app/search-services.bicep' = {
  name: 'search'
  params: {
    keyVaultName: keyVault.outputs.name
    name: '${abbrs.searchSearchServices}${resourceToken}'
    publicNetworkAccess: !empty(virtualNetworkName) ? 'disabled' : 'enabled'
    privateEndpointSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: !empty(virtualNetworkName) ? '${abbrs.networkPrivateLinkServices}${abbrs.searchSearchServices}${resourceToken}': ''
    useManagedIdentityResourceAccess: useManagedIdentityResourceAccess
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

var tokenStoreSasSecretName = 'token-store-sas'
var clientSecretSecretName = 'microsoft-provider-authentication-secret'
var apimSubscriptionKeySecretName = 'apim-subscription-key'
var tokenStoreContainerName = 'token-store'

module appAuthorizationSecrets './app/app-authorization-secrets.bicep' = if(azureSpClientId != '') {
  name: 'app-authorization-secrets'
  params: {
    keyVaultName: keyVault.outputs.name
    storageAccountName: storageAccount.outputs.storageAccountName
    tokenStoreContainerName: tokenStoreContainerName
    tokenStoreSasSecretName: tokenStoreSasSecretName
    clientSecretSecretName: clientSecretSecretName
    clientSecret: azureSpClientSecret
    apimSubscriptionKey: ocpApimSubscriptionKey
    apimSubscriptionKeySecretName: apimSubscriptionKeySecretName
  }
}

var appDefinition = {
  settings : (union(array(backendDefinition.settings), [
    {
      name: 'acrpassword'
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${registry.outputs.registrySecretName}'
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
      name: clientSecretSecretName
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${clientSecretSecretName}'
      secretRef: clientSecretSecretName
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
      name: 'AOAIPremiumServiceEndpoint'
      value: search.outputs.endpoint
    }
    {
      name: 'AOAIPremiumServiceKey'
      value: 'aoaipremiumservicekey'
    }
    {
      name: 'AOAIPremiumChatGptDeployment'
      value: azureChatGptPremiumDeploymentName
    }
    {
      name: 'AOAIStandardServiceEndpoint'
      value: (shouldDeployAzureOpenAIService) ? azureOpenAi.outputs.endpoint : azureOpenAiEndpoint
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
  ], 
  (shouldDeployAzureOpenAIService) ? [
      {
        name: 'AOAIStandardServiceKey'
        value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${azureOpenAi.outputs.cognitiveServicesKeySecretName}'
        secretRef: 'aoaistandardservicekey'
        secret: true
      }
  ] : [],
  (azureSpClientId != '') ? [
    {
      name: 'AZURE_SP_CLIENT_ID'
      value: azureSpClientId
    }
    {
      name: 'AZURE_SP_CLIENT_SECRET'
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${clientSecretSecretName}'
      secretRef: clientSecretSecretName
      secret: true
    }
    {
      name: 'AZURE_TENANT_ID'
      value: azureTenantId
    }
    {
      name: 'AZURE_AUTHORITY_HOST'
      value: azureAuthorityHost
    }
    {
      name: 'Ocp-Apim-Subscription-Key'
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${apimSubscriptionKeySecretName}'
      secretRef: apimSubscriptionKeySecretName
      secret: true
    }
    {
      name: 'AZURE_SP_OPENAI_AUDIENCE'
      value: azureSpOpenAiAudience
    }
    {
      name: tokenStoreSasSecretName
      value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/secrets/${tokenStoreSasSecretName}'
      secretRef: tokenStoreSasSecretName
      secret: true
    }
  ] : [],
  (useManagedIdentityResourceAccess) ? [
    {
      name: 'UseManagedIdentityResourceAccess'
      value: string(useManagedIdentityResourceAccess)
    }
    {
      name: 'UserAssignedManagedIdentityClientId'
      value: managedIdentity.outputs.identityClientId
    }
  ]: []))
}

module app './app/app.bicep' = {
  name: 'app'
  params: {
    name: '${abbrs.appContainerApps}backend-${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerAppsEnvironmentWorkloadProfileName: appContainerAppEnvironmentWorkloadProfileName
    containerRegistryName: registry.outputs.name
    exists: backendExists
    appDefinition: appDefinition
    identityName: managedIdentity.outputs.identityName
    clientId: azureSpClientId
    clientIdScope: azureSpClientIdScope
    clientSecretSecretName: clientSecretSecretName
    tokenStoreSasSecretName: tokenStoreSasSecretName
  }
}

module azureOpenAi './app/cognitive-services.bicep' = if(shouldDeployAzureOpenAIService) {
  name: 'openai'
  params: {
    name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: location
    tags: tags
    deployments: concat([      
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
    ], [
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
    ], [
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
    ])
    keyVaultName: keyVault.outputs.name
    publicNetworkAccess: !empty(virtualNetworkName) ? 'Disabled' : 'Enabled'
    privateEndpointSubnetId: !empty(virtualNetworkName) ? virtualNetwork.outputs.privateEndpointSubnetId: ''
    privateEndpointName: !empty(virtualNetworkName) ? '${abbrs.networkPrivateLinkServices}${abbrs.cognitiveServicesAccounts}${resourceToken}': ''
  }
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint
output AZURE_VNET_NAME string = virtualNetworkName
