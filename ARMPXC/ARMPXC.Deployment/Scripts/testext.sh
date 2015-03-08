#!/bin/bash

# This script is only tested on CentOS 6.5 with Percona XtraDB Cluster 5.6.
# You can customize variables such as MOUNTPOINT, RAIDCHUNKSIZE and so on to your needs.
# You can also customize it to work with other Linux flavours and versions.
# If you customize it, copy it to either Azure blob storage or Github so that Azure
# custom script Linux VM extension can access it, and specify its location in the 
# parameters of DeployPXC powershell script or runbook.   

CLUSTERADDRESS=${1}
NODEADDRESS=${2}
NODENAME=$(hostname)

echo "${CLUSTERADDRESS}, ${NODEADDRESS}, ${NODENAME}" >/tmp/testext.log
