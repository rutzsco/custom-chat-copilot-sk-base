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
param azureChatGptDeploymentName string = 'chat'

@description('Name of the chat GPT model. Default: gpt-35-turbo')
@allowed([ 'gpt-35-turbo', 'gpt-4', 'gpt-35-turbo-16k', 'gpt-4-16k' ])
param azureOpenAIChatGptModelName string = 'gpt-35-turbo'

param azureOpenAIChatGptModelVersion string ='0613'

@description('Capacity of the chat GPT deployment. Default: 10')
param chatGptDeploymentCapacity int = 10

@description('Name of the embedding deployment. Default: embedding')
param azureEmbeddingDeploymentName string = 'embedding'

@description('Name of the embedding model. Default: text-embedding-ada-002')
param azureEmbeddingModelName string = 'text-embedding-ada-002'

@description('Capacity of the embedding deployment. Default: 30')
param embeddingDeploymentCapacity int = 30

param searchContentIndex string = 'manuals'

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

module monitoring './shared/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

module dashboard './shared/dashboard-web.bicep' = {
  name: 'dashboard'
  params: {
    name: '${abbrs.portalDashboards}${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    location: location
    tags: tags
  }
}

module managedIdentity './shared/identity.bicep' = {
  name: 'identity'
  params: {
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
    location: location
    tags: tags
  }
}

module registry './shared/registry.bicep' = {
  name: 'registry'
  params: {
    location: location
    tags: tags
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    keyVaultName: keyVault.outputs.name
  }
}

module cosmos './shared/cosmosdb.bicep' = {
  name: 'cosmos'
  params: {
    accountName: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    databaseName: 'ChatHistory' 
    location: location
    tags: tags
    keyVaultName: keyVault.outputs.name
  }
}

module keyVault './shared/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    tags: tags
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    userPrincipalId: principalId
    managedIdentityPrincipalId: managedIdentity.outputs.identityPrincipalId
  }
}

module appsEnv './shared/apps-env.bicep' = {
  name: 'apps-env'
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
  }
}

var storageAccountContainerName = 'content'

module storageAccount './shared/storage-account.bicep' = {
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
    ]
  }
}

module search './shared/search-services.bicep' = {
  name: 'search'
  params: {
    keyVaultName: keyVault.outputs.name
    name: '${abbrs.searchSearchServices}${resourceToken}'
  }
}

var appDefinition = {
  settings : (union(array(backendDefinition.settings), [
    {
      name: 'acrpassword'
      value: '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=${registry.outputs.registrySecretName})'
      secret: true
    }
    {
      name: 'cosmosdbconnectionstring'
      value: '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=${cosmos.outputs.connectionStringSecretName})'
      secret: true
    }
    {
      name: 'azurestorageconnectionstring'
      value: '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=${storageAccount.outputs.storageAccountConnectionStringSecretName})'
      secret: true
    }
    {
      name: 'aoaistandardservicekey'
      value: '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=${azureOpenAi.outputs.cognitiveServicesKeySecretName})'
      secret: true
    }
    {
      name: 'azuresearchservicekey'
      value: '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=${search.outputs.searchKeySecretName})'
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
      name: 'AzureStorageConnectionString'
      secretRef: 'azurestorageconnectionstring'
    }
    {
      name: 'CosmosDBConnectionString'
      secretRef: 'cosmosdbconnectionstring'
    }
    {
      name: 'AzureSearchServiceKey'
      secretRef: 'azuresearchservicekey'
    }
    {
      name: 'AzureSearchServiceEndpoint'
      value: search.outputs.endpoint
    }
    {
      name: 'AzureSearchContentIndex'
      value: searchContentIndex
    }
    {
      name: 'AOAIPremiumServiceEndpoint'
      value: ''//aoaiPremiumServiceEndpoint
    }
    {
      name: 'AOAIPremiumServiceKey'
      secretRef: 'aoaipremiumservicekey'
    }
    {
      name: 'AOAIPremiumChatGptDeployment'
      value: ''//aoaiPremiumChatGptDeployment
    }
    {
      name: 'AOAIStandardServiceEndpoint'
      value: azureOpenAi.outputs.endpoint
    }
    {
      name: 'AOAIStandardServiceKey'
      secretRef: 'aoaistandardservicekey'
    }
    {
      name: 'AOAIStandardChatGptDeployment'
      value: azureChatGptDeploymentName
    }
    {
      name: 'AOAIEmbeddingsDeployment'
      value: azureEmbeddingDeploymentName
    }
  ]))
}

module app './app/app.bicep' = {
  name: 'app'
  params: {
    name: '${abbrs.appContainerApps}backend-${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerRegistryName: registry.outputs.name
    exists: backendExists
    appDefinition: appDefinition
    identityName: managedIdentity.outputs.identityName
  }
}

module azureOpenAi 'shared/cognitive-services.bicep' =  {
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
        name: azureChatGptDeploymentName
        model: {
          format: 'OpenAI'
          name: azureOpenAIChatGptModelName
          version: azureOpenAIChatGptModelVersion
        }
        sku: {
          name: 'Standard'
          capacity: chatGptDeploymentCapacity
        }
      }
    ])
    keyVaultName: keyVault.outputs.name
  }
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint
