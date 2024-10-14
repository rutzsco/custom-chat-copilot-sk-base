param name string
param location string = resourceGroup().location
param tags object = {}

param sku object = {
  name: 'basic' // 'standard'
}

param networkRuleSet object = {
  bypass: 'AzurePortal'
  ipRules: []
}
param partitionCount int = 1
@allowed([
  'enabled'
  'disabled'
])
param publicNetworkAccess string
param replicaCount int = 1
param keyVaultName string

param privateEndpointSubnetId string
param privateEndpointName string
param managedIdentityPrincipalId string
param useManagedIdentityResourceAccess bool
param deploymentSuffix string = '-kv'

var searchKeySecretName = 'search-key'

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    networkRuleSet: networkRuleSet
    partitionCount: partitionCount
    publicNetworkAccess: publicNetworkAccess
    replicaCount: replicaCount
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
  }
  sku: sku
}

module searchSecret '../shared/keyvault-secret.bicep' = {
  name: '${searchKeySecretName}${deploymentSuffix}'
  params: {
    keyVaultName: keyVaultName
    name: searchKeySecretName
    secretValue: search.listAdminKeys().primaryKey
  }
}

module privateEndpoint '../shared/private-endpoint.bicep' =
  if (!empty(privateEndpointSubnetId)) {
    name: '${name}-private-endpoint'
    params: {
      name: privateEndpointName
      groupIds: ['searchService']
      privateLinkServiceId: search.id
      subnetId: privateEndpointSubnetId
    }
  }

var searchIndexDataContributorRoleDefinitionId = '8ebe5a00-799e-43f5-93ac-243d3dce84a7'

resource managedIdentitySearchIndexDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' =
  if (useManagedIdentityResourceAccess) {
    name: guid(subscription().id, managedIdentityPrincipalId, searchIndexDataContributorRoleDefinitionId)
    properties: {
      principalId: managedIdentityPrincipalId
      roleDefinitionId: subscriptionResourceId(
        'Microsoft.Authorization/roleDefinitions',
        searchIndexDataContributorRoleDefinitionId
      )
      principalType: 'ServicePrincipal'
    }
  }

var searchServiceContributorRoleDefinitionId = '7ca78c08-252a-4471-8644-bb5ff32d4ba0'

resource managedIdentitySearchServiceContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' =
  if (useManagedIdentityResourceAccess) {
    name: guid(subscription().id, managedIdentityPrincipalId, searchServiceContributorRoleDefinitionId)
    properties: {
      principalId: managedIdentityPrincipalId
      roleDefinitionId: subscriptionResourceId(
        'Microsoft.Authorization/roleDefinitions',
        searchServiceContributorRoleDefinitionId
      )
      principalType: 'ServicePrincipal'
    }
  }

output id string = search.id
output endpoint string = 'https://${name}.search.windows.net/'
output name string = search.name
output searchKeySecretName string = searchKeySecretName
