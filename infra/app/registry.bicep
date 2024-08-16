param name string
param location string = resourceGroup().location
param tags object = {}

param adminUserEnabled bool = true
param anonymousPullEnabled bool = false
param dataEndpointEnabled bool = false
param networkRuleBypassOptions string = 'AzureServices'
param publicNetworkAccess string = 'Enabled'
param sku object = {
  name: privateEndpointSubnetId != '' ? 'Premium' : 'Standard'
}
param keyVaultName string
param privateEndpointSubnetId string
param privateEndpointName string

var registrySecretName = 'acr-registry-secret'

// 2023-01-01-preview needed for anonymousPullEnabled
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: sku
  properties: {
    adminUserEnabled: adminUserEnabled
    anonymousPullEnabled: anonymousPullEnabled
    dataEndpointEnabled: dataEndpointEnabled
    networkRuleBypassOptions: networkRuleBypassOptions
    publicNetworkAccess: publicNetworkAccess
  }
}

module registrySecret '../shared/keyvault-secret.bicep' = {
  name: registrySecretName
  params: {
    keyVaultName: keyVaultName
    name: registrySecretName
    secretValue: containerRegistry.listCredentials().passwords[0].value
  }
}

module privateEndpoint '../shared/private-endpoint.bicep' = if(privateEndpointSubnetId != ''){
  name: '${name}-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['registry']
    privateLinkServiceId: containerRegistry.id
    subnetId: privateEndpointSubnetId
  }
}

output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name
output registrySecretName string = registrySecretName
