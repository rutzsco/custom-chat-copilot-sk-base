param name string
param location string = resourceGroup().location
param tags object = {}

param userPrincipalId string
param managedIdentityPrincipalId string

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

output endpoint string = keyVault.properties.vaultUri
output name string = keyVault.name
