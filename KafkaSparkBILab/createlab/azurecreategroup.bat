echo off

set location=%1
set clusterName=%2
set group="ill301rg"

echo on

az group create -l %location% -n %group% && az group deployment create --resource-group %group% --template-file azuredeploy.json --parameters clusterName=%clusterName%