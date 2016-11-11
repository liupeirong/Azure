#!/bin/bash
set -e

(n=0; while true; do n=$(( n+1 )); echo "replication 1:$n"; (./runTeraGen.sh 1 && ./runTeraSort.sh 1 && ./runTeraValidate.sh 1); sleep 10; done) &
(n=0; while true; do n=$(( n+1 )); echo "replication 3:$n"; (./runTeraGen.sh 3 && ./runTeraSort.sh 3 && ./runTeraValidate.sh 3); sleep 10; done) &
(n=0; while true; do n=$(( n+1 )); echo "replication 5:$n"; (./runTeraGen.sh 5 && ./runTeraSort.sh 5 && ./runTeraValidate.sh 5); sleep 10; done) &
