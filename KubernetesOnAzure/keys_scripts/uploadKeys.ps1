$env:resourcegroup="myrg"
$env:location="myregion"
$env:keyvaultName="mykeyvault"

azure provider register Microsoft.KeyVault
azure group create %resourcegroup% %location%
azure keyvault create --vault-name %keyvaultName% --resource-group %resourcegroup% --location %location%
azure keyvault set-policy %keyvaultName% --enabled-for-template-deployment true
azure keyvault set-policy %keyvaultName% --enabled-for-deployment true

$keys = @("./apiserver.pem", "./apiserver-key.pem", "./worker.pem", "./worker-key.pem", "./ca.pem")
$secretNames = @("apiserverpem", "apiserverkeypem", "workerpem", "workerkeypem", "capem")
cd keys
for ($i=0; $i -lt $keys.length; $i++) 
{
    $fileContentBytes = get-content $keys[$i] -Encoding Byte
    $fileContentEncoded = [System.Convert]::ToBase64String($fileContentBytes)
    $env:SECRET=$fileContentEncoded
    $env:SECRETNAME=$secretNames[$i]
    azure keyvault secret set --vault-name %keyvaultName% --secret-name %SECRETNAME% --value %SECRET%
}