#!/bin/bash

######  7001
mkdir -p /home/redis-test/test-redis-replica/7001/conf
chmod 777 /home/redis-test/test-redis-replica/7001/conf

cat << EOF > /home/redis-test/test-redis-replica/7001/conf/redis-re.conf
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 指定端口
port 6379
# 指定节点密码
requirepass 123456
# 指定主节点的密码
masterauth 123456
# 永久指定主从关系，重启后仍可用
# replicaof 0.0.0.0 7001

# 开启rdb
save 3600 1 300 100 60 10000
# 关闭AOF 
appendonly no
EOF

docker run -d -p 7001:6379 --name test-redis-7001 --restart=always \
	-v /home/redis-test/test-redis-replica/7001/data:/data \
	-v /home/redis-test/test-redis-replica/7001/conf:/etc/redis/conf \
	redis:7.0 redis-server /etc/redis/conf/redis-re.conf

######  7002 
mkdir -p /home/redis-test/test-redis-replica/7002/conf
chmod 777 /home/redis-test/test-redis-replica/7002/conf

cat << EOF > /home/redis-test/test-redis-replica/7002/conf/redis-re.conf
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 指定端口
port 6379
# 指定节点密码
requirepass 123456
# 指定主节点的密码
masterauth 123456
# 永久指定主从关系，重启后仍可用
replicaof 119.45.100.200 7001

# 开启rdb
save 3600 1 300 100 60 10000
# 关闭AOF 
appendonly no
EOF

docker run -d -p 7002:6379 --name test-redis-7002 --restart=always \
	        -v /home/redis-test/test-redis-replica/7002/data:/data \
		        -v /home/redis-test/test-redis-replica/7002/conf:/etc/redis/conf \
			        redis:7.0 redis-server /etc/redis/conf/redis-re.conf

######  7003
mkdir -p /home/redis-test/test-redis-replica/7003/conf
chmod 777 /home/redis-test/test-redis-replica/7003/conf
# vim补充上面的配置文件

cat << EOF > /home/redis-test/test-redis-replica/7003/conf/redis-re.conf
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 指定端口
port 6379
# 指定节点密码
requirepass 123456
# 指定主节点的密码
masterauth 123456
# 永久指定主从关系，重启后仍可用
replicaof 119.45.100.200 7001

# 开启rdb
save 3600 1 300 100 60 10000
# 关闭AOF 
appendonly no
EOF

# 启动容器
docker run -d -p 7003:6379 --name test-redis-7003 --restart=always \
	-v /home/redis-test/test-redis-replica/7003/data:/data \
	-v /home/redis-test/test-redis-replica/7003/conf:/etc/redis/conf \
	redis:7.0 redis-server /etc/redis/conf/redis-re.conf

chmod 777 /home/redis-test/test-redis-replica/7003/conf/redis-re.conf
