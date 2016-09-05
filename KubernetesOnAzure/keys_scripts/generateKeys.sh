#!/bin/bash

MASTER_DNS_PREFIX=$1
LOCATION=canadaeast.cloudapp.azure.com
MASTER_IP=10.0.0.50
K8S_SERVICE_IP=10.3.0.1
WORKER_IP_PREFIX=10.0.1

APISERVER_CNF=./openssl.cnf
WORKER_CNF=./worker-openssl.cnf

openssl genrsa -out ca-key.pem 2048
openssl req -x509 -new -nodes -key ca-key.pem -days 10000 -out ca.pem -subj "/CN=kube-ca"

cat > $APISERVER_CNF << EOF
[req]
req_extensions = v3_req
distinguished_name = req_distinguished_name
[req_distinguished_name]
[ v3_req ]
basicConstraints = CA:FALSE
keyUsage = nonRepudiation, digitalSignature, keyEncipherment
subjectAltName = @alt_names
[alt_names]
DNS.1 = kubernetes
DNS.2 = kubernetes.default
DNS.3 = kubernetes.default.svc
DNS.4 = kubernetes.default.svc.cluster.local
DNS.5 = ${MASTER_DNS_PREFIX}1.${LOCATION}
DNS.6 = ${MASTER_DNS_PREFIX}2.${LOCATION}
DNS.7 = ${MASTER_DNS_PREFIX}3.${LOCATION}
DNS.8 = ${MASTER_DNS_PREFIX}4.${LOCATION}
IP.1 = ${K8S_SERVICE_IP}
IP.2 = ${MASTER_IP}
EOF

openssl genrsa -out apiserver-key.pem 2048
openssl req -new -key apiserver-key.pem -out apiserver.csr -subj "/CN=kube-apiserver" -config $APISERVER_CNF
openssl x509 -req -in apiserver.csr -CA ca.pem -CAkey ca-key.pem -CAcreateserial -out apiserver.pem -days 365 -extensions v3_req -extfile $APISERVER_CNF

cat > $WORKER_CNF << EOF
[req]
req_extensions = v3_req
distinguished_name = req_distinguished_name
[req_distinguished_name]
[ v3_req ]
basicConstraints = CA:FALSE
keyUsage = nonRepudiation, digitalSignature, keyEncipherment
subjectAltName = @alt_names
[alt_names]
IP.1 = ${WORKER_IP_PREFIX}.4
IP.2 = ${WORKER_IP_PREFIX}.5
IP.3 = ${WORKER_IP_PREFIX}.6
IP.4 = ${WORKER_IP_PREFIX}.7
IP.5 = ${WORKER_IP_PREFIX}.8
IP.6 = ${WORKER_IP_PREFIX}.9
IP.7 = ${WORKER_IP_PREFIX}.10
IP.8 = ${WORKER_IP_PREFIX}.11
IP.9 = ${WORKER_IP_PREFIX}.12
EOF

openssl genrsa -out worker-key.pem 2048
openssl req -new -key worker-key.pem -out worker.csr -subj "/CN=kube-worker" -config $WORKER_CNF
openssl x509 -req -in worker.csr -CA ca.pem -CAkey ca-key.pem -CAcreateserial -out worker.pem -days 365 -extensions v3_req -extfile $WORKER_CNF

openssl genrsa -out admin-key.pem 2048
openssl req -new -key admin-key.pem -out admin.csr -subj "/CN=kube-admin"
openssl x509 -req -in admin.csr -CA ca.pem -CAkey ca-key.pem -CAcreateserial -out admin.pem -days 365


