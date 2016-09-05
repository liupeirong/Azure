#!/bin/bash

set -e

storage_account="mystorageaccount"
resource_group="myrg"
afs_share_name="myshare"
location="myregion"

azure storage account create --sku-name LRS --location $location --kind=storage --resource-group $resource_group $storage_account_name
key=$(azure storage account keys list ${storage_account} -g ${resource_group} --json | jq '.[0].value' | tr -d \")
azure storage share create ${afs_share_name} -a $storage_account -k $key
