param name string
param location string = resourceGroup().location
param tags object = {}
param containerRegistryName string
param containerAppsEnvironmentName string
param containerAppsEnvironmentWorkloadProfileName string
param applicationInsightsName string
param exists bool
@secure()
param appDefinition object
param identityName string
param clientId string
param clientIdScope string
param clientSecretSecretName string
param tokenStoreSasSecretName string

var appSettingsArray = filter(array(appDefinition.settings), i => i.name != '')
var secrets = map(filter(appSettingsArray, i => i.?secret != null), i => {
  name: i.name
  value: i.value
  secretRef: i.?secretRef ?? take(replace(replace(toLower(i.name), '_', '-'), '.', '-'), 32)
})
var env = map(filter(appSettingsArray, i => i.?secret == null), i => {
  name: i.name
  value: i.value
})
var port = 8080

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource userIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
}

module fetchLatestImage '../shared/fetch-container-image.bicep' = {
  name: '${name}-fetch-image'
  params: {
    exists: exists
    name: name
  }
}

resource app 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: name
  location: location
  tags: union(tags, {'azd-service-name':  'web' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities:  { '${userIdentity.id}': {} }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress:  {
        external: true
        targetPort: port
        transport: 'auto'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'acrpassword'
        }
      ]
      secrets: union([
      ],
      map(secrets, secret => {
        name: secret.secretRef
        keyVaultUrl: secret.value
        identity: userIdentity.id
      }))
    }
    template: {
      containers: [
        {
          image: fetchLatestImage.outputs.?containers[?0].?image ?? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: 'main'
          env: union([
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
            {
              name: 'PORT'
              value: '${port}'
            }
          ],
          env,
          map(secrets, secret => {
            name: secret.name
            secretRef: secret.secretRef
          }))
          resources: {
            cpu: json('1.0')
            memory: '2.0Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz/live'
                port: port
              }
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/healthz/ready'
                port: port
              }
            }
            {
              type: 'Startup'
              httpGet: {
                path: '/healthz/startup'
                port: port
              }
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
    workloadProfileName: containerAppsEnvironmentWorkloadProfileName
  }
}

module appAuthorization './app-authorization.bicep' = if (clientId != '') {
  name: 'app-authorization'
  params: {
    appName: app.name
    clientId: clientId
    clientIdScope: clientIdScope
    clientSecretSecretName: clientSecretSecretName
    tokenStoreSasSecretName: tokenStoreSasSecretName
  }
}

output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
output name string = app.name
output uri string = 'https://${app.properties.configuration.ingress.fqdn}'
output id string = app.id
