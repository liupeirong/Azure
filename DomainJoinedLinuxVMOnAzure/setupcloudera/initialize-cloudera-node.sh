#!/bin/bash
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# 
# See the License for the specific language governing permissions and
# limitations under the License.

LOG_FILE="/var/log/cloudera-azure-initialize.log"

NODETYPE=$1

# logs everything to the LOG_FILE
log() {
  echo "$(date) [${EXECNAME}]: $*" >> ${LOG_FILE}
}

log "------- initialize-node.sh starting -------"

# Mount and format the attached disks base on node type
log "Mount and format the attached disks for ${NODETYPE}"
if [ "$NODETYPE" == "masternode" ]
then
  bash ./prepare-masternode-disks.sh >> ${LOG_FILE} 2>&1
elif [ "$NODETYPE" == "datanode" ]
then
  bash ./prepare-datanode-disks.sh >> ${LOG_FILE} 2>&1
else
  log "Unknown node type : ${NODETYPE}, default to datanode"
  bash ./prepare-datanode-disks.sh >> ${LOG_FILE} 2>&1
fi

log "Done preparing disks. Now 'ls -la /' looks like this:"
ls -la / >> ${LOG_FILE} 2>&1

# Create Impala scratch directory
log "Create Impala scratch directories"
numDataDirs=$(ls -la / | grep data | wc -l)
log "numDataDirs: $numDataDirs"
let endLoopIter=$((numDataDirs - 1))
for x in $(seq 0 $endLoopIter)
do 
  echo mkdir -p /"data${x}"/impala/scratch 
  mkdir -p /"data${x}"/impala/scratch
  chmod 777 /"data${x}"/impala/scratch
done

# Disable iptables
log "Disable iptables"
/etc/init.d/iptables save
/etc/init.d/iptables stop
chkconfig iptables off

# Disable THP
log "Disable THP"
echo never | tee -a /sys/kernel/mm/transparent_hugepage/enabled
echo "echo never | tee -a /sys/kernel/mm/transparent_hugepage/enabled" | tee -a /etc/rc.local
echo never | tee -a /sys/kernel/mm/transparent_hugepage/defrag
echo "echo never | tee -a /sys/kernel/mm/transparent_hugepage/defrag" | tee -a /etc/rc.local

# Set swappiness to 1
log "Set swappiness to 1"
echo vm.swappiness=1 | tee -a /etc/sysctl.conf
echo 1 | tee /proc/sys/vm/swappiness

# Set system tuning params
log "Set system tuning params"
echo net.ipv4.tcp_timestamps=0 >> /etc/sysctl.conf
echo net.ipv4.tcp_sack=1 >> /etc/sysctl.conf
echo net.core.rmem_max=4194304 >> /etc/sysctl.conf
echo net.core.wmem_max=4194304 >> /etc/sysctl.conf
echo net.core.rmem_default=4194304 >> /etc/sysctl.conf
echo net.core.wmem_default=4194304 >> /etc/sysctl.conf
echo net.core.optmem_max=4194304 >> /etc/sysctl.conf
echo net.ipv4.tcp_rmem="4096 87380 4194304" >> /etc/sysctl.conf
echo net.ipv4.tcp_wmem="4096 65536 4194304" >> /etc/sysctl.conf
echo net.ipv4.tcp_low_latency=1 >> /etc/sysctl.conf
sed -i "s/defaults        1 1/defaults,noatime        0 0/" /etc/fstab

log "------- initialize-node.sh succeeded -------"

# always `exit 0` on success
exit 0
