# ------------------------------------------------------------------------------------
# Assign Data Contributor Role in a Cosmos Database for one principal
# You must supply the Cosmos Database Name, Resource Group it lives in, and the Principal Id that needs access
# Example Usage:
# ./AddCosmosRole.ps1 -databaseName '<cosmosDbName>' -resourceGroup '<resourceGroupName>' -principalId '999999-9999-9999-9999-9999999999'
# ------------------------------------------------------------------------------------

param(
    [Parameter(Mandatory = $true)] [string] $databaseName,
    [Parameter(Mandatory = $true)] [string] $resourceGroup,
    [Parameter(Mandatory = $true)] [string] $principalId
)

Write-Host "----------------------------------------------------------------------------------------------------" -ForegroundColor Yellow
Write-Host "** $(Get-Date -Format HH:mm:ss) - Starting Cosmos Data Contributor Grant with the following parameters:" -ForegroundColor Yellow
Write-Host "** ResourceGroupName: $resourceGroup" -ForegroundColor Yellow
Write-Host "** Cosmos Database: $databaseName" -ForegroundColor Yellow
Write-Host "** Principal Id: $principalId" -ForegroundColor Yellow
Write-Host "----------------------------------------------------------------------------------------------------" -ForegroundColor Yellow
Write-Host "`n"

Write-Host "Querying Cosmos $databaseName for Data Contributor Role" -ForegroundColor Yellow
$roleIdLine = az cosmosdb sql role definition list `
  --account-name $dataBaseName `
  --resource-group $resourceGroup `
  |  Out-String -Stream | Select-String -Pattern "sqlroleDefinitions" | Select-String -Pattern "000002"
$roleIdLine = $roleIdLine -replace '"', ''
$roleIdLine = $roleIdLine -replace ',', ''
$roleObject = $roleIdLine | ConvertFrom-String -Delimiter ": " -PropertyNames Title, Value
$cosmosWriterRoleId = $roleObject.Value
Write-Host "Found Data Contributor Role Id: $cosmosWriterRoleId" -ForegroundColor Green

Write-Host ""
Write-Host "Querying Cosmos $databaseName for Scope Id" -ForegroundColor Yellow
$cosmosScopeLine = az cosmosdb show `
 --name $dataBaseName `
 --resource-group $resourceGroup --query "{id:id}"
$cosmosScopeLine = $cosmosScopeLine -replace '"', ''
$cosmosScopeLine = $cosmosScopeLine -replace ',', ''
$cosmosScopeObject = $cosmosScopeLine | ConvertFrom-String -Delimiter ": " -PropertyNames Title, Value
$cosmosScopeId = $cosmosScopeObject.Value
Write-Host "Found Cosmos Scope Id: $cosmosScopeId" -ForegroundColor Green

Write-Host ""
Write-Host "Adding Role Assignment for Principal $principalId" -ForegroundColor Yellow
Write-Host "Executing: az cosmosdb sql role assignment create --account-name '$dataBaseName' --resource-group '$resourceGroup' --role-definition-id '$cosmosWriterRoleId' --principal-id '$principalId' --scope '$cosmosScopeId'"
az cosmosdb sql role assignment create `
 --account-name $dataBaseName `
 --resource-group $resourceGroup `
 --role-definition-id $cosmosWriterRoleId `
 --principal-id $principalId `
 --scope $cosmosScopeId

Write-Host "Finished!" -ForegroundColor Green
