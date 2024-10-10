// --------------------------------------------------------------------------------
// This BICEP file will create KeyVault Password secret for an existing Container Registry
// --------------------------------------------------------------------------------
metadata description = 'Creates or updates a secret in an Azure Key Vault.'
param keyVaultName string
param name string
param registryName string
param registryResourceGroup string
param tags object = {}
param contentType string = 'string'
param enabled bool = true
param exp int = 0
param nbf int = 0

resource existingResource 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  scope: resourceGroup(registryResourceGroup)
  name: registryName
}
var secretValue = existingResource.listCredentials().passwords[0].value

resource keyVaultResource 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}
resource keyVaultSecret 'Microsoft.KeyVault/vaults/secrets@2024-04-01-preview' = {
  name: name
  tags: tags
  parent: keyVaultResource
  properties: {
    attributes: {
      enabled: enabled
      exp: exp
      nbf: nbf
    }
    contentType: contentType
    value: secretValue
  }
}

output secretUri string = keyVaultSecret.properties.secretUri
output secretName string = name
