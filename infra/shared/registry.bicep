param name string
param location string = resourceGroup().location
param tags object = {}

param adminUserEnabled bool = true
param anonymousPullEnabled bool = false
param dataEndpointEnabled bool = false
param encryption object = {
  status: 'disabled'
}
param networkRuleBypassOptions string = 'AzureServices'
param publicNetworkAccess string = 'Enabled'
param sku object = {
  name: 'Standard'
}
param zoneRedundancy string = 'Disabled'
param keyVaultName string

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
    encryption: encryption
    networkRuleBypassOptions: networkRuleBypassOptions
    publicNetworkAccess: publicNetworkAccess
    zoneRedundancy: zoneRedundancy
  }
}

module registrySecret './keyvault-secret.bicep' = {
  name: registrySecretName
  params: {
    keyVaultName: keyVaultName
    name: registrySecretName
    secretValue: containerRegistry.listCredentials().passwords[0].value
  }
}

output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name
output registrySecretName string = registrySecretName
