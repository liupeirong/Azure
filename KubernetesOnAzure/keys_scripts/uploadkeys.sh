#!/bin/bash

set -e
#
resource_group="myrg"
location="myregion"
keyvault_name="mykeyvault"

azure provider register Microsoft.KeyVault
azure group create $resource_group $location
azure keyvault create --vault-name $keyvault_name --resource-group $resource_group --location $location
azure keyvault set-policy $keyvault_name --enabled-for-template-deployment true
azure keyvault set-policy $keyvault_name --enabled-for-deployment true

keys=("./apiserver.pem" "./apiserver-key.pem" "./worker.pem" "./worker-key.pem" "./ca.pem")
secret_names=("apiserverpem" "apiserverkeypem" "workerpem" "workerkeypem" "capem")

mkdir -p B64
cd keys
for ((i = 0; i < ${#keys[@]}; i++)); do
  OUT=../B64/${keys[$i]}.out
  base64 --output $OUT ${keys[$i]}
  secret=$(cat $OUT)
  secret_name=${secret_names[$i]}
  azure keyvault secret set --vault-name $keyvault_name --secret-name $secret_name --value $secret
done
