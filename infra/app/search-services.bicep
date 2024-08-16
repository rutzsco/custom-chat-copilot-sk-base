param name string
param location string = resourceGroup().location
param tags object = {}

param sku object = {
  name: 'standard'
}

param networkRuleSet object = {
  bypass: 'AzurePortal'
  ipRules: []
}
param partitionCount int = 1
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string
param replicaCount int = 1
param keyVaultName string

param privateEndpointSubnetId string
param privateEndpointName string

var searchKeySecretName = 'search-key'

resource search 'Microsoft.Search/searchServices@2021-04-01-preview' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    networkRuleSet: networkRuleSet
    partitionCount: partitionCount
    publicNetworkAccess: publicNetworkAccess
    replicaCount: replicaCount
  }
  sku: sku
}

module searchSecret '../shared/keyvault-secret.bicep' = {
  name: searchKeySecretName
  params: {
    keyVaultName: keyVaultName
    name: searchKeySecretName
    secretValue: search.listAdminKeys().primaryKey
  }
}

module privateEndpoint '../shared/private-endpoint.bicep' = if(privateEndpointSubnetId != ''){
  name: '${name}-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['searchService']
    privateLinkServiceId: search.id
    subnetId: privateEndpointSubnetId
  }
}

output id string = search.id
output endpoint string = 'https://${name}.search.windows.net/'
output name string = search.name
output searchKeySecretName string = searchKeySecretName
