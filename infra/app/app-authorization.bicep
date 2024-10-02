param storageAccountName string
param keyVaultName string
param appName string
param clientId string
@secure()
param clientSecret string
param clientIdAudience string
param clientIdScope string
param utcValue string = utcNow()
param clientSecretSecretName string

var tokenStoreSasSecretName = 'token-store-sas'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' existing = {
  name: storageAccountName
}

resource storageAccountBlobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  name: 'default'
  parent: storageAccount
}

var tokenStoreContainerName = 'token-store'

resource tokenStoreContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-06-01' = {
  name: tokenStoreContainerName
  parent: storageAccountBlobService
  properties: {
    publicAccess: 'None'
  }
}

var expiry = dateTimeAdd(utcValue, 'P1Y', 'yyyy-MM-ddTHH:mm:ssZ')

var sasConfig = {
  canonicalizedResource: '/blob/${storageAccountName}/${tokenStoreContainerName}'
  signedResource: 'c'
  signedProtocol: 'https'
  signedPermission: 'rwd'
  signedExpiry: expiry
  keyToSign: 'key1'
}

module tokenStoreSasSecret '../shared/keyvault-secret.bicep' = {
  name: tokenStoreSasSecretName
  dependsOn: [tokenStoreContainer]
  params: {
    keyVaultName: keyVaultName
    name: tokenStoreSasSecretName
    secretValue: 'https://${storageAccountName}.blob.${environment().suffixes.storage}/${tokenStoreContainerName}?${storageAccount.listServiceSas(storageAccount.apiVersion, sasConfig).serviceSasToken}'
    exp: dateTimeToEpoch(expiry)
  }
}

module clientSecretSecret '../shared/keyvault-secret.bicep' = {
  name: clientSecretSecretName
  params: {
    keyVaultName: keyVaultName
    name: clientSecretSecretName
    secretValue: clientSecret
  }
}

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
