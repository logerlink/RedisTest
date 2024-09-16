#!/bin/bash

#### s7005
mkdir -p /home/redis-test/test-redis-replica/s7005/conf

cat << EOF > /home/redis-test/test-redis-replica/s7005/conf/redis-sentinel.conf
bind 0.0.0.0
# 端口，默认26379
port 26379
# 最重要的。监控主节点获取主节点和从节点(info replication)的信息  2表示quorum，建议设置为哨兵节点个数的一半并向上取整
sentinel monitor masterName 119.45.100.200 7001 2
# 主从模式主节点的密码
sentinel auth-pass masterName 123456
# 故障转移，选举master时使用。从节点与主节点断开时间长短指定值
sentinel down-after-milliseconds masterName 10000 
sentinel failover-timeout masterName 50000

# 指定从节点为7002、7003
sentinel known-replica masterName 119.45.100.200 7002
sentinel known-replica masterName 119.45.100.200 7003

EOF

docker run -d -p 7005:26379 --name test-redis-s-7005 --restart=always \
        -v /home/redis-test/test-redis-replica/s7005/data:/data \
        -v /home/redis-test/test-redis-replica/s7005/conf:/etc/redis:rw \
        redis:7.0 redis-sentinel /etc/redis/redis-sentinel.conf

#### s7006
mkdir -p /home/redis-test/test-redis-replica/s7006/conf

cat << EOF > /home/redis-test/test-redis-replica/s7006/conf/redis-sentinel.conf
bind 0.0.0.0
# 端口，默认26379
port 26379
# 最重要的。监控主节点获取主节点和从节点(info replication)的信息  2表示quorum，建议设置为哨兵节点个数的一半并向上取整
sentinel monitor masterName 119.45.100.200 7001 2
# 主从模式主节点的密码
sentinel auth-pass masterName 123456
# 故障转移，选举master时使用。从节点与主节点断开时间长短指定值
sentinel down-after-milliseconds masterName 10000 
sentinel failover-timeout masterName 50000

# 指定从节点为7002、7003
sentinel known-replica masterName 119.45.100.200 7002
sentinel known-replica masterName 119.45.100.200 7003

EOF

docker run -d -p 7006:26379 --name test-redis-s-7006 --restart=always \
	-v /home/redis-test/test-redis-replica/s7006/data:/data \
	-v /home/redis-test/test-redis-replica/s7006/conf:/etc/redis:rw \
	redis:7.0 redis-sentinel /etc/redis/redis-sentinel.conf

#### s7007
mkdir -p /home/redis-test/test-redis-replica/s7007/conf

cat << EOF >/home/redis-test/test-redis-replica/s7007/conf/redis-sentinel.conf

bind 0.0.0.0
# 端口，默认26379
port 26379
# 最重要的。监控主节点获取主节点和从节点(info replication)的信息  2表示quorum，建议设置为哨兵节点个数的一半并向上取整
sentinel monitor masterName 119.45.100.200 7001 2
# 主从模式主节点的密码
sentinel auth-pass masterName 123456
# 故障转移，选举master时使用。从节点与主节点断开时间长短指定值
sentinel down-after-milliseconds masterName 10000 
sentinel failover-timeout masterName 50000

# 指定从节点为7002、7003
sentinel known-replica masterName 119.45.100.200 7002
sentinel known-replica masterName 119.45.100.200 7003


EOF

docker run -d -p 7007:26379 --name test-redis-s-7007 --restart=always \
	-v /home/redis-test/test-redis-replica/s7007/data:/data \
	-v /home/redis-test/test-redis-replica/s7007/conf:/etc/redis:rw \
	redis:7.0 redis-sentinel /etc/redis/redis-sentinel.conf
