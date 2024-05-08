param identityName string
param location string
param tags object

resource userIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

output identityName string = userIdentity.name
output identityClientId string = userIdentity.properties.clientId
output identityPrincipalId string = userIdentity.properties.principalId
