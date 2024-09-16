#!/bin/bash
######  7001
mkdir -p /home/redis-test/test-redis-cluster/7001/conf
chmod 777 /home/redis-test/test-redis-cluster/7001/conf

cat << EOF > /home/redis-test/test-redis-cluster/7001/conf/redis-cluster.conf
# 指定端口
port 7001
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 开启集群
cluster-enabled yes
# 指定集群配置文件，无需创建，自动生成
cluster-config-file /etc/redis/conf/nodes.conf
# 节点心跳超时事件
cluster-node-timeout 5000
# 持久化文件存放目录
dir /data/
# 后台运行
daemonize no
# 保护模式
protected-mode no
# 日志文件
logfile /data/logs/run.log
# 指定密码
requirepass 123456
masterauth 123456
# 集群总线端口
cluster-announce-bus-port 17001
EOF

docker run -d -p 7001:7001 -p 17001:17001 --name test-cluster-7001 --restart=always \
        -v /home/redis-test/test-redis-cluster/7001/data:/data \
        -v /home/redis-test/test-redis-cluster/7001/conf:/etc/redis/conf \
        -v /home/redis-test/test-redis-cluster/7001/logs:/data/logs \
        redis:7.0 redis-server /etc/redis/conf/redis-cluster.conf

######  7002
mkdir -p /home/redis-test/test-redis-cluster/7002/conf
chmod 777 /home/redis-test/test-redis-cluster/7002/conf

cat << EOF > /home/redis-test/test-redis-cluster/7002/conf/redis-cluster.conf
# 指定端口
port 7002
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 开启集群
cluster-enabled yes
# 指定集群配置文件，无需创建，自动生成
cluster-config-file /etc/redis/conf/nodes.conf
# 节点心跳超时事件
cluster-node-timeout 5000
# 持久化文件存放目录
dir /data/
# 后台运行
daemonize no
# 保护模式
protected-mode no
# 日志文件
logfile /data/logs/run.log
# 指定密码
requirepass 123456
masterauth 123456
# 集群总线端口
cluster-announce-bus-port 17002
EOF

docker run -d -p 7002:7002 -p 17002:17002 --name test-cluster-7002 --restart=always \
        -v /home/redis-test/test-redis-cluster/7002/data:/data \
        -v /home/redis-test/test-redis-cluster/7002/conf:/etc/redis/conf \
        -v /home/redis-test/test-redis-cluster/7002/logs:/data/logs \
        redis:7.0 redis-server /etc/redis/conf/redis-cluster.conf

######  7003
mkdir -p /home/redis-test/test-redis-cluster/7003/conf
chmod 777 /home/redis-test/test-redis-cluster/7003/conf

cat << EOF > /home/redis-test/test-redis-cluster/7003/conf/redis-cluster.conf
# 指定端口
port 7003
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 开启集群
cluster-enabled yes
# 指定集群配置文件，无需创建，自动生成
cluster-config-file /etc/redis/conf/nodes.conf
# 节点心跳超时事件
cluster-node-timeout 5000
# 持久化文件存放目录
dir /data/
# 后台运行
daemonize no
# 保护模式
protected-mode no
# 日志文件
logfile /data/logs/run.log
# 指定密码
requirepass 123456
masterauth 123456
# 集群总线端口
cluster-announce-bus-port 17003
EOF

docker run -d -p 7003:7003 -p 17003:17003 --name test-cluster-7003 --restart=always \
        -v /home/redis-test/test-redis-cluster/7003/data:/data \
        -v /home/redis-test/test-redis-cluster/7003/conf:/etc/redis/conf \
        -v /home/redis-test/test-redis-cluster/7003/logs:/data/logs \
        redis:7.0 redis-server /etc/redis/conf/redis-cluster.conf

######  7004
mkdir -p /home/redis-test/test-redis-cluster/7004/conf
chmod 777 /home/redis-test/test-redis-cluster/7004/conf

cat << EOF > /home/redis-test/test-redis-cluster/7004/conf/redis-cluster.conf
# 指定端口
port 7004
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 开启集群
cluster-enabled yes
# 指定集群配置文件，无需创建，自动生成
cluster-config-file /etc/redis/conf/nodes.conf
# 节点心跳超时事件
cluster-node-timeout 5000
# 持久化文件存放目录
dir /data/
# 后台运行
daemonize no
# 保护模式
protected-mode no
# 日志文件
logfile /data/logs/run.log
# 指定密码
requirepass 123456
masterauth 123456
# 集群总线端口
cluster-announce-bus-port 17004
EOF

docker run -d -p 7004:7004 -p 17004:17004 --name test-cluster-7004 --restart=always \
        -v /home/redis-test/test-redis-cluster/7004/data:/data \
        -v /home/redis-test/test-redis-cluster/7004/conf:/etc/redis/conf \
        -v /home/redis-test/test-redis-cluster/7004/logs:/data/logs \
        redis:7.0 redis-server /etc/redis/conf/redis-cluster.conf

######  7005
mkdir -p /home/redis-test/test-redis-cluster/7005/conf
chmod 777 /home/redis-test/test-redis-cluster/7005/conf

cat << EOF > /home/redis-test/test-redis-cluster/7005/conf/redis-cluster.conf
# 指定端口
port 7005
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 开启集群
cluster-enabled yes
# 指定集群配置文件，无需创建，自动生成
cluster-config-file /etc/redis/conf/nodes.conf
# 节点心跳超时事件
cluster-node-timeout 5000
# 持久化文件存放目录
dir /data/
# 后台运行
daemonize no
# 保护模式
protected-mode no
# 日志文件
logfile /data/logs/run.log
# 指定密码
requirepass 123456
masterauth 123456
# 集群总线端口
cluster-announce-bus-port 17005
EOF

docker run -d -p 7005:7005 -p 17005:17005 --name test-cluster-7005 --restart=always \
        -v /home/redis-test/test-redis-cluster/7005/data:/data \
        -v /home/redis-test/test-redis-cluster/7005/conf:/etc/redis/conf \
        -v /home/redis-test/test-redis-cluster/7005/logs:/data/logs \
        redis:7.0 redis-server /etc/redis/conf/redis-cluster.conf

######  7006
mkdir -p /home/redis-test/test-redis-cluster/7006/conf
chmod 777 /home/redis-test/test-redis-cluster/7006/conf

cat << EOF > /home/redis-test/test-redis-cluster/7006/conf/redis-cluster.conf
# 指定端口
port 7006
# bind 127.0.0.1 -:1  0.0.0.0外网可访问
bind 0.0.0.0
# 开启集群
cluster-enabled yes
# 指定集群配置文件，无需创建，自动生成
cluster-config-file /etc/redis/conf/nodes.conf
# 节点心跳超时事件
cluster-node-timeout 5000
# 持久化文件存放目录
dir /data/
# 后台运行
daemonize no
# 保护模式
protected-mode no
# 日志文件
logfile /data/logs/run.log
# 指定密码
requirepass 123456
masterauth 123456
# 集群总线端口
cluster-announce-bus-port 17006
EOF

docker run -d -p 7006:7006 -p 17006:17006 --name test-cluster-7006 --restart=always \
        -v /home/redis-test/test-redis-cluster/7006/data:/data \
        -v /home/redis-test/test-redis-cluster/7006/conf:/etc/redis/conf \
        -v /home/redis-test/test-redis-cluster/7006/logs:/data/logs \
        redis:7.0 redis-server /etc/redis/conf/redis-cluster.conf
