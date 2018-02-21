#!/bin/bash

logStorageAccountName=${1}
logStorageAccountKey=${2}

set -e

config_td_agent_log_to_storage() {
cat >> /etc/td-agent/td-agent.conf <<EOF
<source>
  @type tail
  path /var/log/azure/Microsoft.ManagedIdentity.ManagedIdentityExtensionForLinux/1.0.0.10/*
  pos_file /var/log/td-agent/buffer/msi.pos
  <parse>
    @type none
  </parse>
  tag msi.extension
</source>
<match msi.extension>
  @type azurestorage
  azure_storage_account    ${logStorageAccountName}
  azure_storage_access_key ${logStorageAccountKey}
  azure_container          msicontainer
  azure_storage_type       blob
  store_as                 gzip
  auto_create_container    true
  path                     logs/
  azure_object_key_format  %{path}%{time_slice}_%{index}.%{file_extension}
  time_slice_format        %Y%m%d%H%M  #write file every minute, default hour
  <buffer>
    @type file
    path /var/log/td-agent/buffer/azurestorage
    timekey 300 # 5 minute partition
    timekey_wait 1m # 1 minute wait to flush
    timekey_use_utc true # use utc
  </buffer>
</match>
#<match msi.extension>
#  @type s3
#  aws_key_id your_aws_key_id
#  aws_sec_key your_aws_key
#  s3_bucket msibucket
#  s3_region us-west-2
#  path logs/
#  s3_object_key_format  %{path}%{time_slice}_%{index}.%{file_extension}
#  time_slice_format %Y%m%d%H%M  #write file every minute, default hour
#  <buffer>
#    @type file
#    path /var/log/td-agent/buffer/s3
#    timekey 300 # 5 minute partition
#    timekey_wait 1m # 1 minute wait to flush
#    timekey_use_utc true # use utc
#  </buffer>
#</match>
EOF
}

curl -L https://toolbelt.treasuredata.com/sh/install-redhat-td-agent3.sh | sh
td-agent-gem install fluent-plugin-azurestorage
config_td_agent_log_to_storage
#by default, azure log, including extension log, is only readable by root, not by td_agent
chmod a+r /var/log/azure
systemctl start td-agent.service
                                              