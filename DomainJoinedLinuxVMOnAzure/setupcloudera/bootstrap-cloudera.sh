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
# Usage: bootstrap-cloudera-1.0.sh {clusterName} {managment_node} {cluster_nodes} {isHA} {sshUserName} [{sshPassword}]

LOG_FILE="/var/log/cloudera-azure-initialize.log"

EXECNAME=$0

# Fill in this section for your environment
NAMEPREFIX='vmName parameter in the azuredeploy template'
ADMINUSER='adminUserName parameter in the azuredeploy template'
PASSWORD='adminPassword parameter in the azuredeploy template'
VMSIZE='vmSize variable in the azuredeploy template'
mip="private IP address of the master node, ex. 10.1.0.4"
worker_ip="private IP address list of data nodes, ex. 10.1.0.5,10.1.0.6,10.1.0.7"
CMUSER='cloudera manager admin username'
CMPASSWORD='cloudera manager admin password'

EMAILADDRESS='bob@contoso.com'
BUSINESSPHONE='1112223333'
FIRSTNAME='p'
LASTNAME='l'
JOBROLE='VP'
JOBFUNCTION='IT'
COMPANY='contoso'

# logs everything to the $LOG_FILE
log() {
  echo "$(date) [${EXECNAME}]: $*" >> "${LOG_FILE}"
}

log "------- bootstrap-cloudera.sh starting -------"

# if you have a valid ssh key, the installation script will try to use the ssh key and will fail, so purposely pointing to an invalid private key file here.
file="/home/$ADMINUSER/.ssh/id_rsa1"
key="/tmp/id_rsa.pem"
openssl rsa -in "$file" -outform PEM > $key

log "BEGIN: Starting detached script to finalize initialization"
if ! sh initialize-cloudera-server.sh "$NAMEPREFIX" "$key" "$mip" "$worker_ip" "false" "$ADMINUSER" "$PASSWORD" "$CMUSER" "$CMPASSWORD" "$EMAILADDRESS" "$BUSINESSPHONE" "$FIRSTNAME" "$LASTNAME" "$JOBROLE" "$JOBFUNCTION" "$COMPANY" "$VMSIZE">/dev/null 2>&1
then
  log "initialize-cloudera-server.sh returned non-zero exit code"
  log "------- bootstrap-cloudera.sh failed -------"
  exit 1
fi
log "initialize-cloudera-server.sh returned exit code 0"
log "END: Detached script to finalize initialization running. PID: $!"

log "------- bootstrap-cloudera.sh succeeded -------"

# always `exit 0` on success
exit 0
