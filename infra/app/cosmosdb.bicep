@description('Cosmos DB account name')
param accountName string = 'sql-${uniqueString(resourceGroup().id)}'

@description('The name for the SQL database')
param databaseName string

@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

param tags object = {}

var connectionStringSecretName = 'azure-cosmos-connection-string'
param keyVaultName string

param privateEndpointSubnetId string
param privateEndpointName string

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: toLower(accountName)
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    disableKeyBasedMetadataWriteAccess: false
    enableFreeTier: false
    enableAnalyticalStorage: false
    createMode: 'Default'
    databaseAccountOfferType: 'Standard'
    publicNetworkAccess: privateEndpointSubnetId != '' ? 'Disabled' : 'Enabled'
    networkAclBypass: 'AzureServices'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    cors: []
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource database  'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2020-06-01-preview' = {
  parent: account
  name: databaseName
  tags: tags
  properties: {
    resource: {
      id: databaseName
    }
    options: {
    }
  }
}

resource chatTurn  'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2020-06-01-preview' = {
  parent: database
  name: 'ChatTurn'
  tags: tags
  properties: {
    resource: {
      id: 'ChatTurn'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/chatId'
        ]
        kind: 'Hash'
      }
      uniqueKeyPolicy: {
        uniqueKeys: []
      }
      conflictResolutionPolicy: {
        mode: 'LastWriterWins'
        conflictResolutionPath: '/_ts'
      }
    }
    options: {
    }
  }
}

module cosmosConnectionStringSecret '../shared/keyvault-secret.bicep' = {
  name: connectionStringSecretName
  params: {
    keyVaultName: keyVaultName
    name: connectionStringSecretName
    secretValue: account.listConnectionStrings().connectionStrings[0].connectionString
  }
}

module privateEndpoint '../shared/private-endpoint.bicep' = if(privateEndpointSubnetId != ''){
  name: '${accountName}-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['Sql']
    privateLinkServiceId: account.id
    subnetId: privateEndpointSubnetId
  }
}

output connectionStringSecretName string = connectionStringSecretName
output endpoint string = account.properties.documentEndpoint
output id string = account.id
output name string = account.name
