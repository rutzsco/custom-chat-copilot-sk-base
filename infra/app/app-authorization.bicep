param appName string
param clientId string
param clientIdScope string
param clientSecretSecretName string
param tokenStoreSasSecretName string

resource app 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: appName
}

resource authSettings 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  name: 'current'
  parent: app
  properties: {
    globalValidation: {
      redirectToProvider: 'azureactivedirectory'
      unauthenticatedClientAction: 'RedirectToLoginPage'
    }
    identityProviders: {
      azureActiveDirectory: {
        login: {
          loginParameters: [
            'scope=openid profile offline_access ${clientIdScope}'
          ]
        }
        registration: {
          clientId: clientId
          clientSecretSettingName: clientSecretSecretName
          openIdIssuer: 'https://sts.windows.net/${subscription().tenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [
            '${clientIdScope}'
          ]
          defaultAuthorizationPolicy: {
            allowedApplications: [
              '${clientId}'
            ]
          }
        }
      }
    }
    login: {
      tokenStore: {
        azureBlobStorage: {
          sasUrlSettingName: tokenStoreSasSecretName
        }
        enabled: true
      }
    }
    platform: {
      enabled: true
    }
  }
}
