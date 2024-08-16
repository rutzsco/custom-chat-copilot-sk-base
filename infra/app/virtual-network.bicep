param virtualNetworkName string
param containerAppSubnetAddressPrefix string
param privateEndpointSubnetAddressPrefix string

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-01-01' existing = {
  name: virtualNetworkName
}

resource containerAppSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' = {
  name: 'container-app'
  parent: virtualNetwork
  properties: {
    addressPrefix: containerAppSubnetAddressPrefix
  }
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' = {
  name: 'private-endpoint'
  parent: virtualNetwork
  properties: {
    addressPrefix: privateEndpointSubnetAddressPrefix
  }
}

output containerAppSubnetId string = containerAppSubnet.id
output privateEndpointSubnetId string = privateEndpointSubnet.id
