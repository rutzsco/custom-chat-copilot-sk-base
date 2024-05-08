param location string
param workloadStackName string
param acrName string

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string = workloadStackName

@description('Name of the storage account')
param storageAccountName string

@description('Name of the storage container. Default: content')
param storageContainerName string = 'content'

@description('SKU name for the Azure Cognitive Search service. Default: standard')
param searchServiceSkuName string = 'standard'
param searchContentIndex string = 'manuals'

param aoaiPremiumServiceEndpoint string = 'NA'
param aoaiPremiumServiceKey string = 'NA'
param aoaiPremiumChatGptDeployment string = 'NA'

param aoaiStandardServiceEndpoint string
param aoaiStandardServiceKey string
param aoaiStandardChatGptDeployment string

param aoaiEmbeddingsDeployment string = 'text-embedding'

var cosmosDbAccountName = workloadStackName
var logAnalyticsWorkspaceName = workloadStackName
var searchServiceName = workloadStackName
var containerAppsEnvironmentName = workloadStackName

var tags = {
  'azd-env-name': environmentName
}

// Log Analytics
module logAnalytics 'log-analytics.bicep' = {
  name: 'logAnalytics' 
  params: {
    workspaceName: logAnalyticsWorkspaceName
    location: location
    tags: tags
  }
}

// CosmosDB
module db 'cosmosdb.bicep' = {
	name: 'cosmosdb'
	params: {
      location: location
      accountName: cosmosDbAccountName
      databaseName: 'ChatHistory'
      tags: tags
	}
}

module searchService 'search-services.bicep' = {
  name: 'search-service'
  params: {
    name: searchServiceName
    location: location
    tags: tags
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
    sku: {
      name: searchServiceSkuName
    }
    semanticSearch: 'free'
  }
}

module storage 'storage-account.bicep' = {
  name: 'storage'
  params: {
    name: storageAccountName
    location: location
    tags: tags
    publicNetworkAccess: 'Enabled'
    allowBlobPublicAccess: false
    sku: {
      name: 'Standard_ZRS'
    }
    deleteRetentionPolicy: {
      enabled: true
      days: 2
    }
    containers: [
      {
        name: storageContainerName
        publicAccess: 'None'
      }
    ]
  }
}

module registry 'registry.bicep' = {
  name: 'registry'
  params: {
    name: acrName
    location: location
    tags: tags
  }
}

module aca 'aca.bicep' = {
  name: 'aca'
  params: {
    name: 'chatapp'
    environmentName: containerAppsEnvironmentName
    location: location
    tags: tags
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    containerImage: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
    envVars: []
    useExternalIngress: true
    containerPort: 8080
    acrName: acrName
    storageConnectionString: storage.outputs.connectionString
    storageBlobEndpoint: storage.outputs.primaryEndpoints.blob
    storageContainerName: storageContainerName
    cosmosDBConnectionString: db.outputs.connectionString

    azureSearchServiceKey: searchService.outputs.key
    azureSearchContentIndex: searchContentIndex
    azureSearchServiceEndpoint: searchService.outputs.endpoint

    aoaiPremiumServiceEndpoint: aoaiPremiumServiceEndpoint
    aoaiPremiumServiceKey: aoaiPremiumServiceKey
    aoaiPremiumChatGptDeployment: aoaiPremiumChatGptDeployment
    
    aoaiStandardServiceEndpoint: aoaiStandardServiceEndpoint
    aoaiStandardServiceKey: aoaiStandardServiceKey
    aoaiStandardChatGptDeployment: aoaiStandardChatGptDeployment

    aoaiEmbeddingsDeployment: aoaiEmbeddingsDeployment
  }
}
