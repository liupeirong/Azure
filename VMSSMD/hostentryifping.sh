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

for j in {0..0}    # third octet ip range
do
  for i in {3..110} # fourth octet ip range
  do
    ping -c1 -w1 10.0.$j.$i > /dev/null
    if [ $? -eq 0 ]
    then
      ssh 10.0.0.$i -i id_rsa -o "StrictHostKeyChecking no" 'echo `hostname -i` `hostname -f` `hostname -s`'
    fi
  done
done
