echo off

set location=%1
set group=%2

echo on

az group create -l %location% -n %group% && az group deployment create --resource-group %group% --template-file azuredeploy.json --parameters @azuredeploy.parameters.json