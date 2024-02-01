param location string
param workloadStackName string
param acrName string

@description('Name of the storage account')
param storageAccountName string

@description('Name of the storage container. Default: content')
param storageContainerName string = 'content'

@description('SKU name for the Azure Cognitive Search service. Default: standard')
param searchServiceSkuName string = 'standard'

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

// Log Analytics
module logAnalytics 'log-analytics.bicep' = {
  name: 'logAnalytics' 
  params: {
    workspaceName: logAnalyticsWorkspaceName
    location: location
  }
}

// CosmosDB
module db 'cosmosdb.bicep' = {
	name: 'cosmosdb'
	params: {
      location: location
      accountName: cosmosDbAccountName
      databaseName: 'ChatHistory'
	}
}

module searchService 'search-services.bicep' = {
  name: 'search-service'
  params: {
    name: searchServiceName
    location: location
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
    publicNetworkAccess: 'Enabled'
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

module aca 'aca.bicep' = {
  name: 'aca'
  params: {
    name: 'chatapp'
    environmentName: containerAppsEnvironmentName
    location: location
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
    aoaiPremiumServiceEndpoint: aoaiPremiumServiceEndpoint
    aoaiPremiumServiceKey: aoaiPremiumServiceKey
    aoaiPremiumChatGptDeployment: aoaiPremiumChatGptDeployment
    
    aoaiStandardServiceEndpoint: aoaiStandardServiceEndpoint
    aoaiStandardServiceKey: aoaiStandardServiceKey
    aoaiStandardChatGptDeployment: aoaiStandardChatGptDeployment

    aoaiEmbeddingsDeployment: aoaiEmbeddingsDeployment
  }
}
