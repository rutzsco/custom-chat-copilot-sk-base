param name string
param location string = resourceGroup().location
param tags object = {}

param userPrincipalId string
param managedIdentityPrincipalId string

param privateEndpointSubnetId string
param privateEndpointName string

var defaultAccessPolicies = [
  {
    objectId: userPrincipalId
    permissions: { secrets: [ 'get', 'list' ] }
    tenantId: subscription().tenantId
  }
  {
    objectId: managedIdentityPrincipalId
    permissions: { secrets: [ 'get', 'list' ] }
    tenantId: subscription().tenantId
  }
]

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enabledForTemplateDeployment: true
    accessPolicies: union(defaultAccessPolicies, [
      // define access policies here
    ])
  }
}

module privateEndpoint '../shared/private-endpoint.bicep' = if(privateEndpointSubnetId != ''){
  name: '${name}-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['vault']
    privateLinkServiceId: keyVault.id
    subnetId: privateEndpointSubnetId
  }
}

output endpoint string = keyVault.properties.vaultUri
output name string = keyVault.name
