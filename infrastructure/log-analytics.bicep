// --------------------------------------------------------------------------------
// Creates a Log Analytics Workspace
// --------------------------------------------------------------------------------
param workspaceName string
param location string = resourceGroup().location

// --------------------------------------------------------------------------------
resource logWorkspaceResource 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
        name: 'PerGB2018' // Standard
    }
  }
}

// --------------------------------------------------------------------------------
output id string = logWorkspaceResource.id
