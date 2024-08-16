param name string
param location string = resourceGroup().location
param subnetId string
param privateLinkServiceId string
param groupIds array

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2021-02-01' = {
  name: name
  location: location
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: name
        properties: {
          privateLinkServiceId: privateLinkServiceId
          groupIds: groupIds
        }
      }
    ]
  }
}

output privateEndpointId string = privateEndpoint.id
