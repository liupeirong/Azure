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

open_ports() {
    iptables -A INPUT -p tcp -m tcp --dport 3306 -j ACCEPT
    iptables -A INPUT -p tcp -m tcp --dport 4444 -j ACCEPT
    iptables -A INPUT -p tcp -m tcp --dport 4567 -j ACCEPT
    iptables -A INPUT -p tcp -m tcp --dport 4568 -j ACCEPT
    iptables -A INPUT -p tcp -m tcp --dport 9200 -j ACCEPT
    iptables-save
}

disable_apparmor() {
	/etc/init.d/apparmor teardown
	update-rc.d -f apparmor remove
}

configure_network() {
    open_ports
    disable_apparmor
}

configure_network