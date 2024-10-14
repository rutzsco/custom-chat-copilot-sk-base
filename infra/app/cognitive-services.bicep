param existingCogServicesName string
param existingCogServicesResourceGroup string
param name string
param location string = resourceGroup().location
param tags object = {}
param deployments array = []
param kind string = 'OpenAI'
param publicNetworkAccess string
param sku object = {
  name: 'S0'
}
param privateEndpointSubnetId string
param privateEndpointName string
param deploymentSuffix string = '-kv'

var resourceGroupName = resourceGroup().name
var cognitiveServicesKeySecretName = 'cognitive-services-key'

resource existingAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' existing =
  if (!empty(existingCogServicesName)) {
    scope: resourceGroup(existingCogServicesResourceGroup)
    name: existingCogServicesName
  }

resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' =
  if (empty(existingCogServicesName)) {
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
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [
  for deployment in deployments: if (empty(existingCogServicesName)) {
    parent: account
    name: deployment.name
    properties: {
      model: deployment.model
      raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
    }
    sku: contains(deployment, 'sku')
      ? deployment.sku
      : {
          name: 'Standard'
          capacity: 20
        }
  }
]

module privateEndpoint '../shared/private-endpoint.bicep' =
  if (!empty(existingCogServicesName) && !empty(privateEndpointSubnetId)) {
    name: '${name}-private-endpoint'
    params: {
      name: privateEndpointName
      groupIds: ['account']
      privateLinkServiceId: account.id
      subnetId: privateEndpointSubnetId
    }
  }

output endpoint string = !empty(existingCogServicesName)
  ? existingAccount.properties.endpoint
  : account.properties.endpoint
output id string = !empty(existingCogServicesName) ? existingAccount.id : account.id
output name string = !empty(existingCogServicesName) ? existingAccount.name : account.name
output resourceGroupName string = !empty(existingCogServicesName) ? existingCogServicesResourceGroup : resourceGroupName
output cognitiveServicesKeySecretName string = cognitiveServicesKeySecretName
