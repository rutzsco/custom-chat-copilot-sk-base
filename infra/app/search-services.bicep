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
  'enabled'
  'disabled'
])
param publicNetworkAccess string = 'enabled'
param replicaCount int = 1
param keyVaultName string

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

output id string = search.id
output endpoint string = 'https://${name}.search.windows.net/'
output name string = search.name
output searchKeySecretName string = searchKeySecretName
