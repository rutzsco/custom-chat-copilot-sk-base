param location string
param name string
param environmentName string

param storageBlobEndpoint string
param storageContainerName string
param cosmosDBConnectionString string
param azureSearchServiceKey string

param aoaiPremiumServiceEndpoint string
param aoaiPremiumServiceKey string
param aoaiPremiumChatGptDeployment string

param aoaiStandardServiceEndpoint string
param aoaiStandardServiceKey string
param aoaiStandardChatGptDeployment string

param aoaiEmbeddingsDeployment string

@description('Name of the Log Analytics workspace')
param logAnalyticsWorkspaceName string

// Container Image ref
param containerImage string

// Networking
param useExternalIngress bool = false
param containerPort int

param envVars array = []

param acrName string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource acr 'Microsoft.ContainerRegistry/registries@2021-06-01-preview' existing = {
  name: acrName
  scope: resourceGroup('rutzsco-core-cicd')
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-04-01-preview' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: name
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      secrets: [
        {
          name: 'acrpassword'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'cosmosdbconnectionstring'
          value: cosmosDBConnectionString
        }
        {
          name: 'aoaistandardservicekey'
          value: aoaiStandardServiceKey
        }
        {
          name: 'aoaipremiumservicekey'
          value: aoaiPremiumServiceKey
        }
        {
          name: 'azuresearchservicekey'
          value: azureSearchServiceKey
        }
      ]
      registries: [
        {
          server: '${acrName}.azurecr.io'
          username: acr.listCredentials().username
          passwordSecretRef: 'acrpassword'
        }
      ]
      ingress: {
        external: useExternalIngress
        targetPort: containerPort
      }
    }
    template: {
      containers: [
        {
          image: containerImage
          name: name
          env: [
            {
              name: 'AzureStorageAccountEndpoint'
              value: storageBlobEndpoint
            }
            {
              name: 'AzureStorageContainer'
              value: storageContainerName
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
              name: 'AOAIPremiumServiceEndpoint'
              value: aoaiPremiumServiceEndpoint
            }
            {
              name: 'AOAIPremiumServiceKey'
              secretRef: 'aoaipremiumservicekey'
            }
            {
              name: 'AOAIPremiumChatGptDeployment'
              value: aoaiPremiumChatGptDeployment
            }
            {
              name: 'AOAIStandardServiceEndpoint'
              value: aoaiStandardServiceEndpoint
            }
            {
              name: 'AOAIStandardServiceKey'
              secretRef: 'aoaistandardservicekey'
            }
            {
              name: 'AOAIStandardChatGptDeployment'
              value: aoaiStandardChatGptDeployment
            }
            {
              name: 'AOAIEmbeddingsDeployment'
              value: aoaiEmbeddingsDeployment
            }
            
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
