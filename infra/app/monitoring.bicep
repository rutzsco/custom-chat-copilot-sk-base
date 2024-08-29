param logAnalyticsName string
param applicationInsightsName string
param location string = resourceGroup().location
param tags object = {}
param azureMonitorPrivateLinkScopeName string = ''
param azureMonitorPrivateLinkScopeResourceGroupName string = ''
param privateEndpointSubnetId string = ''
param privateEndpointName string = ''
param publicNetworkAccessForIngestion string = 'Enabled'
param publicNetworkAccessForQuery string = 'Enabled'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
    publicNetworkAccessForIngestion: publicNetworkAccessForIngestion
    publicNetworkAccessForQuery: publicNetworkAccessForQuery
  })
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: publicNetworkAccessForIngestion
    publicNetworkAccessForQuery: publicNetworkAccessForQuery
  }
}

resource azureMonitorPrivateLinkScope 'Microsoft.Insights/privateLinkScopes@2021-07-01-preview' existing = if(!empty(azureMonitorPrivateLinkScopeName)) {
  name: azureMonitorPrivateLinkScopeName
  scope: resourceGroup(azureMonitorPrivateLinkScopeResourceGroupName)
}

module azureMonitorPrivateLinkScopePrivateEndpoint '../shared/private-endpoint.bicep' = if(!empty(privateEndpointSubnetId)) {
  name: 'azure-monitor-private-link-scope-private-endpoint'
  params: {
    name: privateEndpointName
    groupIds: ['azuremonitor']
    privateLinkServiceId: azureMonitorPrivateLinkScope.id
    subnetId: privateEndpointSubnetId
  }
}

output applicationInsightsName string = applicationInsights.name
output logAnalyticsWorkspaceId string = logAnalytics.id
output logAnalyticsWorkspaceName string = logAnalytics.name
