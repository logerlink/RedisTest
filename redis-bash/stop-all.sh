#!/bin/bash

rm -rf /home/redis-test/test-redis-replica

mkdir /home/redis-test/test-redis-replica

docker rm -f test-redis-7001 test-redis-7002 test-redis-7003 test-redis-s-7005 test-redis-s-7006 test-redis-s-7007
