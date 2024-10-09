param storageAccountName string
param keyVaultName string
@secure()
param clientSecret string
param utcValue string = utcNow()
param clientSecretSecretName string
param apimSubscriptionKey string
param apimSubscriptionKeySecretName string
param tokenStoreContainerName string
param tokenStoreSasSecretName string
param deploymentSuffix string = '-kv'

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

module apimSubscriptionKeySecret '../shared/keyvault-secret.bicep' = {
  name: apimSubscriptionKeySecretName
  params: {
    keyVaultName: keyVaultName
    name: apimSubscriptionKeySecretName
    secretValue: apimSubscriptionKey
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' existing = {
  name: storageAccountName
}

resource storageAccountBlobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  name: 'default'
  parent: storageAccount
}

resource tokenStoreContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-06-01' = {
  name: tokenStoreContainerName
  parent: storageAccountBlobService
  properties: {
    publicAccess: 'None'
  }
}
