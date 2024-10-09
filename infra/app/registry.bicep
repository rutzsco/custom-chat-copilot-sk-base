param existingContainerRegistryName string
param existingContainerRegistryResourceGroup string
param name string
param location string = resourceGroup().location
param tags object = {}

param adminUserEnabled bool = true
param anonymousPullEnabled bool = false
param dataEndpointEnabled bool = false
param networkRuleBypassOptions string = 'AzureServices'
param publicNetworkAccess string = 'Enabled'
param sku object = {
  name: !empty(privateEndpointSubnetId) ? 'Premium' : 'Standard'
}
param keyVaultName string
param privateEndpointSubnetId string
param privateEndpointName string

var registrySecretName = 'acr-registry-secret'

var resourceGroupName = resourceGroup().name

resource existingContainerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = if (existingContainerRegistryName != '') {
  scope: resourceGroup(existingContainerRegistryResourceGroup)
  name: existingContainerRegistryName
}

// 2023-01-01-preview needed for anonymousPullEnabled
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = if (existingContainerRegistryName == '') {
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

module privateEndpoint '../shared/private-endpoint.bicep' = if (existingContainerRegistryName == '' && !empty(privateEndpointSubnetId)){
  name: '${name}-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['registry']
    privateLinkServiceId: containerRegistry.id
    subnetId: privateEndpointSubnetId
  }
}

output loginServer string = existingContainerRegistryName != '' ? existingContainerRegistry.properties.loginServer : containerRegistry.properties.loginServer
output id string = existingContainerRegistryName != '' ? existingContainerRegistry.id : containerRegistry.id
output name string = existingContainerRegistryName != '' ? existingContainerRegistry.name : containerRegistry.name
output resourceGroupName string = existingContainerRegistryName != '' ? existingContainerRegistryResourceGroup : resourceGroupName
output registrySecretName string = registrySecretName
