// --------------------------------------------------------------------------------
// This BICEP file will create KeyVault Password secret for an existing Cognitive Service
// --------------------------------------------------------------------------------
metadata description = 'Creates or updates a secret in an Azure Key Vault.'
param keyVaultName string
param name string
param cognitiveServiceName string
param cognitiveServiceResourceGroup string
param tags object = {}
param contentType string = 'string'
@description('The value of the secret. Provide only derived values like blob storage access, but do not hard code any secrets in your templates')

param enabled bool = true
param exp int = 0
param nbf int = 0

resource existingResource 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = { 
  scope: resourceGroup(cognitiveServiceResourceGroup)
  name: cognitiveServiceName 
}
var secretValue = existingResource.listKeys().key1

resource keyVaultResource 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource keyVaultSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
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
