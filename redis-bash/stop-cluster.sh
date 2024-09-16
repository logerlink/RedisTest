#!/bin/bash

rm -rf /home/redis-test/test-redis-cluster

mkdir /home/redis-test/test-redis-cluster

for port in $(seq 7001 7006);
do
	docker rm -f test-cluster-${port}
done
