param name string
param location string = resourceGroup().location
param tags object = {}

@allowed([
  'Cool'
  'Hot'
  'Premium' ])
param accessTier string = 'Hot'
param allowBlobPublicAccess bool
param allowSharedKeyAccess bool = true
param containers array = []
param kind string = 'StorageV2'
param minimumTlsVersion string = 'TLS1_2'
param networkAcls object = {
  bypass: 'AzureServices'
  defaultAction: 'Allow'
}
@allowed([ 'Enabled', 'Disabled' ])
param publicNetworkAccess string
param sku object = { name: 'Standard_LRS' }
param keyVaultName string

param privateEndpointSubnetId string
param privateEndpointName string

var storageAccountConnectionStringSecretName = 'storage-account-connection-string'

resource storage 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  sku: sku
  properties: {
    accessTier: accessTier
    allowBlobPublicAccess: allowBlobPublicAccess
    allowSharedKeyAccess: allowSharedKeyAccess
    minimumTlsVersion: minimumTlsVersion
    networkAcls: networkAcls
    publicNetworkAccess: publicNetworkAccess
  }

  resource blobServices 'blobServices' = if (!empty(containers)) {
    name: 'default'
    resource container 'containers' = [for container in containers: {
      name: container.name
      properties: {
        publicAccess: contains(container, 'publicAccess') ? container.publicAccess : 'None'
      }
    }]
  }
}

module storageAccountConnectionStringSecret '../shared/keyvault-secret.bicep' = {
  name: storageAccountConnectionStringSecretName
  params: {
    keyVaultName: keyVaultName
    name: storageAccountConnectionStringSecretName
    secretValue: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
  }
}

module privateEndpoint '../shared/private-endpoint.bicep' = if(privateEndpointSubnetId != ''){
  name: '${name}-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['blob']
    privateLinkServiceId: storage.id
    subnetId: privateEndpointSubnetId
  }
}

output primaryEndpoints object = storage.properties.primaryEndpoints
output storageAccountName string = storage.name
output storageAccountConnectionStringSecretName string = storageAccountConnectionStringSecretName
