param existingCogServicesName string
param name string
param location string = resourceGroup().location
param tags object = {}
param deployments array = []
param kind string = 'OpenAI'
param publicNetworkAccess string
param sku object = {
  name: 'S0'
}
param keyVaultName string

param privateEndpointSubnetId string
param privateEndpointName string

resource existingAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = if (existingCogServicesName != '') {
  name: existingCogServicesName
}
resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' = if (existingCogServicesName == '') {
  name: name
  location: location
  tags: tags
  kind: kind
  properties: {
    publicNetworkAccess: publicNetworkAccess
    networkAcls: {
      defaultAction: !empty(privateEndpointSubnetId) ? 'Deny' : 'Allow'
    }
    customSubDomainName: name
  }
  sku: sku
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for deployment in deployments: if (existingCogServicesName == '') {
  parent: account
  name: deployment.name
  properties: {
    model: deployment.model
    raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
  }
  sku: contains(deployment, 'sku') ? deployment.sku : {
    name: 'Standard'
    capacity: 20
  }
}]

var cognitiveServicesKeySecretName = 'cognitive-services-key'

module cognitiveServicesSecret '../shared/keyvault-secret.bicep' = if (existingCogServicesName == '') {
  name: cognitiveServicesKeySecretName
  params: {
    keyVaultName: keyVaultName
    name: cognitiveServicesKeySecretName
    secretValue: account.listKeys().key1
  }
}

module privateEndpoint '../shared/private-endpoint.bicep' = if (existingCogServicesName == '' && !empty(privateEndpointSubnetId)){
  name: '${name}-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['account']
    privateLinkServiceId: account.id
    subnetId: privateEndpointSubnetId
  }
}

output endpoint string = existingCogServicesName != '' ? existingAccount.properties.endpoint : account.properties.endpoint
output id string = existingCogServicesName != '' ? existingAccount.id : account.id
output name string = existingCogServicesName != '' ? existingAccount.name : account.name
output cognitiveServicesKeySecretName string = cognitiveServicesKeySecretName
