param virtualNetworkName string
param location string = resourceGroup().location
param containerAppSubnetName string
param containerAppSubnetAddressPrefix string
param containerAppSubnetNsgName string
param privateEndpointSubnetName string
param privateEndpointSubnetAddressPrefix string
param privateEndpointSubnetNsgName string

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-01-01' existing = {
  name: virtualNetworkName
}

resource containerAppSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' = {
  name: containerAppSubnetName
  parent: virtualNetwork
  properties: {
    addressPrefix: containerAppSubnetAddressPrefix
    networkSecurityGroup: {
      id: containerAppSubnetNsg.id
    }
    delegations:[
      {
        name: 'Microsoft.App/environments'
        properties: {
          serviceName: 'Microsoft.App/environments'
        }
      }
    ]
  }
}

resource containerAppSubnetNsg 'Microsoft.Network/networkSecurityGroups@2024-01-01' = {
  name: containerAppSubnetNsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowAllInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowAllOutbound'
        properties: {
          priority: 100
          direction: 'Outbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' = {
  name: privateEndpointSubnetName
  parent: virtualNetwork
  properties: {
    addressPrefix: privateEndpointSubnetAddressPrefix
    networkSecurityGroup: {
      id: privateEndpointSubnetNsg.id
    }
  }
}

resource privateEndpointSubnetNsg 'Microsoft.Network/networkSecurityGroups@2024-01-01' = {
  name: privateEndpointSubnetNsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowAllInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowAllOutbound'
        properties: {
          priority: 100
          direction: 'Outbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

output containerAppSubnetId string = containerAppSubnet.id
output privateEndpointSubnetId string = privateEndpointSubnet.id
