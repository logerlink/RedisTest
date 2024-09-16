using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RedisTest.Share
{
    /// <summary>
    /// Redis 助手
    /// </summary>
    public class RedisHelper
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        private readonly string ConnectionString;

        /// <summary>
        /// redis 连接对象
        /// </summary>
        private IConnectionMultiplexer _connMultiplexer;

        /// <summary>
        /// 默认的 Key 值（用来当作 RedisKey 的前缀）
        /// </summary>
        private readonly string DefaultKey;

        /// <summary>
        /// 锁
        /// </summary>
        private static readonly object Locker = new object();

        /// <summary>
        /// 数据库
        /// </summary>
        private readonly IDatabase _db;

        /// <summary>
        /// 获取 Redis 连接对象
        /// </summary>
        /// <returns></returns>
        public IConnectionMultiplexer GetConnectionRedisMultiplexer()
        {
            if ((_connMultiplexer == null) || !_connMultiplexer.IsConnected)
            {
                lock (Locker)
                {
                    if ((_connMultiplexer == null) || !_connMultiplexer.IsConnected)
                        _connMultiplexer = ConnectionMultiplexer.Connect(ConnectionString);
                }
            }

            return _connMultiplexer;
        }

        #region 其它

        public ITransaction GetTransaction()
        {
            return _db.CreateTransaction();
        }
        /// <summary>
        /// 加锁
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TryLockAsync(string lockKey, string guidStr, TimeSpan lockTimeout)
        {
            var hasLock = await _db.StringSetAsync(AddKeyPrefix(lockKey), guidStr, lockTimeout, When.NotExists);
            return hasLock;
        }
        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockKey"></param>
        /// <param name="guidStr"></param>
        /// <returns></returns>
        public async Task<bool> ReleaseLockAsync(string lockKey, string guidStr)
        {
            // 释放锁，需要确保是锁的拥有者才能释放
            var value = await _db.StringGetAsync(AddKeyPrefix(lockKey));
            if (value.HasValue && value == guidStr)
            {
                return await _db.KeyDeleteAsync(AddKeyPrefix(lockKey));
            }
            return false;
        }
        #endregion 其它

        #region 构造函数

        public RedisHelper(string connectionString, string defaultKey)
        {  
            ConnectionString = connectionString;
            _connMultiplexer = ConnectionMultiplexer.Connect(ConnectionString);
            _db = _connMultiplexer.GetDatabase();
            DefaultKey = defaultKey;
            AddRegisterEvent();
        }

        public RedisHelper(int db = -1)
        {
            if (_connMultiplexer == null) throw new Exception("未连接redis");
            _db = _connMultiplexer.GetDatabase(db);
        }

        #endregion 构造函数

        #region String 操作

        /// <summary>
        /// 设置 key 并保存字符串（如果 key 已存在，则覆盖值）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public bool StringSet(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.StringSet(redisKey, redisValue, expiry);
        }

        /// <summary>
        /// 保存多个 Key-value
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public bool StringSet(IEnumerable<KeyValuePair<RedisKey, RedisValue>> keyValuePairs)
        {
            keyValuePairs =
                keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key), x.Value));
            return _db.StringSet(keyValuePairs.ToArray());
        }

        /// <summary>
        /// 获取字符串
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public string? StringGet(string redisKey, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.StringGet(redisKey, CommandFlags.PreferReplica);
        }

        /// <summary>
        /// 存储一个对象（该对象会被序列化保存）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public bool StringSet<T>(string redisKey, T redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = JSONSerialize(redisValue);
            return _db.StringSet(redisKey, json, expiry);
        }

        /// <summary>
        /// 获取一个对象（会进行反序列化）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public T? StringGet<T>(string redisKey, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(_db.StringGet(redisKey, CommandFlags.PreferReplica));
        }

        #region async

        /// <summary>
        /// 保存一个字符串值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<bool> StringSetAsync(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.StringSetAsync(redisKey, redisValue, expiry);
        }

        /// <summary>
        /// 保存一组字符串值
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public async Task<bool> StringSetAsync(IEnumerable<KeyValuePair<RedisKey, RedisValue>> keyValuePairs)
        {
            keyValuePairs =
                keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key), x.Value));
            return await _db.StringSetAsync(keyValuePairs.ToArray());
        }

        /// <summary>
        /// 获取单个值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<string?> StringGetAsync(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.StringGetAsync(redisKey, CommandFlags.PreferReplica);
        }

        /// <summary>
        /// 存储一个对象（该对象会被序列化保存）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<bool> StringSetAsync<T>(string redisKey, T redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = JSONSerialize(redisValue);
            return await _db.StringSetAsync(redisKey, json, expiry);
        }

        /// <summary>
        /// 获取一个对象（会进行反序列化）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<T?> StringGetAsync<T>(string redisKey, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(await _db.StringGetAsync(redisKey, CommandFlags.PreferReplica));
        }

        #endregion async

        #endregion String 操作

        #region Hash 操作

        /// <summary>
        /// 判断该字段是否存在 hash 中
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public bool HashExists(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashExists(redisKey, hashField);
        }

        /// <summary>
        /// 从 hash 中移除指定字段
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public bool HashDelete(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashDelete(redisKey, hashField);
        }

        /// <summary>
        /// 从 hash 中移除指定字段
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public long HashDelete(string redisKey, IEnumerable<RedisValue> hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashDelete(redisKey, hashField.ToArray());
        }

        /// <summary>
        /// 在 hash 设定值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HashSet(string redisKey, string hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashSet(redisKey, hashField, value);
        }

        /// <summary>
        /// 在 hash 中设定值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashFields"></param>
        public void HashSet(string redisKey, IEnumerable<HashEntry> hashFields)
        {
            redisKey = AddKeyPrefix(redisKey);
            _db.HashSet(redisKey, hashFields.ToArray());
        }

        /// <summary>
        /// 在 hash 中获取值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public RedisValue HashGet(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashGet(redisKey, hashField, CommandFlags.PreferReplica);
        }

        /// <summary>
        /// 在 hash 中获取值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public RedisValue[] HashGet(string redisKey, RedisValue[] hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashGet(redisKey, hashField, CommandFlags.PreferReplica);
        }

        /// <summary>
        /// 从 hash 返回所有的字段值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public IEnumerable<RedisValue> HashKeys(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashKeys(redisKey);
        }

        /// <summary>
        /// 返回 hash 中的所有值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public RedisValue[] HashValues(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.HashValues(redisKey);
        }

        /// <summary>
        /// 在 hash 设定值（序列化）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HashSet<T>(string redisKey, string hashField, T value)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = JSONSerialize(value);
            return _db.HashSet(redisKey, hashField, json);
        }

        /// <summary>
        /// 在 hash 中获取值（反序列化）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public T? HashGet<T>(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(_db.HashGet(redisKey, hashField, CommandFlags.PreferReplica));
        }

        #region async

        /// <summary>
        /// 判断该字段是否存在 hash 中
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<bool> HashExistsAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashExistsAsync(redisKey, hashField);
        }

        /// <summary>
        /// 从 hash 中移除指定字段
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<bool> HashDeleteAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashDeleteAsync(redisKey, hashField);
        }

        /// <summary>
        /// 从 hash 中移除指定字段
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<long> HashDeleteAsync(string redisKey, IEnumerable<RedisValue> hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashDeleteAsync(redisKey, hashField.ToArray());
        }

        /// <summary>
        /// 在 hash 设定值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> HashSetAsync(string redisKey, string hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashSetAsync(redisKey, hashField, value);
        }

        /// <summary>
        /// 在 hash 中设定值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashFields"></param>
        public async Task HashSetAsync(string redisKey, IEnumerable<HashEntry> hashFields)
        {
            redisKey = AddKeyPrefix(redisKey);
            await _db.HashSetAsync(redisKey, hashFields.ToArray());
        }

        /// <summary>
        /// 在 hash 中获取值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<RedisValue> HashGetAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashGetAsync(redisKey, hashField, CommandFlags.PreferReplica);
        }

        /// <summary>
        /// 在 hash 中获取值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> HashGetAsync(string redisKey, RedisValue[] hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashGetAsync(redisKey, hashField, CommandFlags.PreferReplica);
        }

        /// <summary>
        /// 从 hash 返回所有的字段值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> HashKeysAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashKeysAsync(redisKey);
        }

        /// <summary>
        /// 返回 hash 中的所有值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> HashValuesAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.HashValuesAsync(redisKey);
        }

        /// <summary>
        /// 在 hash 设定值（序列化）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> HashSetAsync<T>(string redisKey, string hashField, T value)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = JSONSerialize(value);
            return await _db.HashSetAsync(redisKey, hashField, json);
        }

        /// <summary>
        /// 在 hash 中获取值（反序列化）
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<T?> HashGetAsync<T>(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(await _db.HashGetAsync(redisKey, hashField, CommandFlags.PreferReplica));
        }

        #endregion async

        #endregion Hash 操作

        #region List 操作

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public string? ListLeftPop(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListLeftPop(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public string? ListRightPop(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListRightPop(redisKey);
        }

        /// <summary>
        /// 移除列表指定键上与该值相同的元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListRemove(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListRemove(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListRightPush(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListRightPush(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListLeftPush(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListLeftPush(redisKey, redisValue);
        }

        /// <summary>
        /// 返回列表上该键的长度，如果不存在，返回 0
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public long ListLength(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListLength(redisKey);
        }

        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public IEnumerable<RedisValue> ListRange(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListRange(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public T? ListLeftPop<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(_db.ListLeftPop(redisKey));
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public T? ListRightPop<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(_db.ListRightPop(redisKey));
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListRightPush<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListRightPush(redisKey, JSONSerialize(redisValue));
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListLeftPush<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.ListLeftPush(redisKey, JSONSerialize(redisValue));
        }

        /// <summary>
        /// 列表批量插入 cluster集群下不要使用
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListPushAsync(string redisKey, IEnumerable<RedisValue> values)
        {
            redisKey = AddKeyPrefix(redisKey);
            return (long)await _db.ExecuteAsync("LPUSH", values.Cast<object>().Prepend(redisKey).ToArray());
        }

        /// <summary>
        /// 无序列表批量插入 cluster集群下不要使用
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> SetAddAsync(string redisKey, IEnumerable<RedisValue> values)
        {
            redisKey = AddKeyPrefix(redisKey);
            return (long)await _db.ExecuteAsync("SADD", values.Cast<object>().Prepend(redisKey).ToArray());
        }


        #region List-async

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<string?> ListLeftPopAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListLeftPopAsync(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<string?> ListRightPopAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListRightPopAsync(redisKey);
        }

        /// <summary>
        /// 移除列表指定键上与该值相同的元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListRemoveAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListRemoveAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListRightPushAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListRightPushAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListLeftPushAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListLeftPushAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 返回列表上该键的长度，如果不存在，返回 0
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<long> ListLengthAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListLengthAsync(redisKey);
        }

        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> ListRangeAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListRangeAsync(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<T?> ListLeftPopAsync<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(await _db.ListLeftPopAsync(redisKey));
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<T?> ListRightPopAsync<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return JSONDeSerialize<T>(await _db.ListRightPopAsync(redisKey));
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListRightPushAsync<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListRightPushAsync(redisKey, JSONSerialize(redisValue));
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListLeftPushAsync<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.ListLeftPushAsync(redisKey, JSONSerialize(redisValue));
        }

        #endregion List-async

        #endregion List 操作

        #region SortedSet 操作

        /// <summary>
        /// SortedSet 新增
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public bool SortedSetAdd(string redisKey, string member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.SortedSetAdd(redisKey, member, score);
        }

        /// <summary>
        /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public IEnumerable<RedisValue> SortedSetRangeByRank(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.SortedSetRangeByRank(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public long SortedSetLength(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.SortedSetLength(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="memebr"></param>
        /// <returns></returns>
        public bool SortedSetLength(string redisKey, string memebr)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.SortedSetRemove(redisKey, memebr);
        }

        /// <summary>
        /// SortedSet 新增
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public bool SortedSetAdd<T>(string redisKey, T member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = JSONSerialize(member);

            return _db.SortedSetAdd(redisKey, json, score);
        }

        #region SortedSet-Async

        /// <summary>
        /// SortedSet 新增
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetAddAsync(string redisKey, string member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.SortedSetAddAsync(redisKey, member, score);
        }

        /// <summary>
        /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> SortedSetRangeByRankAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.SortedSetRangeByRankAsync(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<long> SortedSetLengthAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.SortedSetLengthAsync(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="memebr"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetRemoveAsync(string redisKey, string memebr)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.SortedSetRemoveAsync(redisKey, memebr);
        }

        /// <summary>
        /// SortedSet 新增
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetAddAsync<T>(string redisKey, T member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = JSONSerialize(member);

            return await _db.SortedSetAddAsync(redisKey, json, score);
        }

        #endregion SortedSet-Async

        #endregion SortedSet 操作

        #region key 操作

        /// <summary>
        /// 移除指定 Key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public bool KeyDelete(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.KeyDelete(redisKey);
        }

        /// <summary>
        /// 移除指定 Key
        /// </summary>
        /// <param name="redisKeys"></param>
        /// <returns></returns>
        public long KeyDelete(IEnumerable<string> redisKeys)
        {
            var keys = redisKeys.Select(x => (RedisKey)AddKeyPrefix(x));
            return _db.KeyDelete(keys.ToArray());
        }

        /// <summary>
        /// 校验 Key 是否存在
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public bool KeyExists(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.KeyExists(redisKey);
        }

        /// <summary>
        /// 重命名 Key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisNewKey"></param>
        /// <returns></returns>
        public bool KeyRename(string redisKey, string redisNewKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.KeyRename(redisKey, redisNewKey);
        }

        /// <summary>
        /// 设置 Key 的时间
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public bool KeyExpire(string redisKey, TimeSpan? expiry)
        {
            redisKey = AddKeyPrefix(redisKey);
            return _db.KeyExpire(redisKey, expiry);
        }

        #region key-async

        /// <summary>
        /// 移除指定 Key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<bool> KeyDeleteAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.KeyDeleteAsync(redisKey);
        }

        /// <summary>
        /// 移除指定 Key
        /// </summary>
        /// <param name="redisKeys"></param>
        /// <returns></returns>
        public async Task<long> KeyDeleteAsync(IEnumerable<string> redisKeys)
        {
            var keys = redisKeys.Select(x => (RedisKey)AddKeyPrefix(x));
            return await _db.KeyDeleteAsync(keys.ToArray());
        }

        /// <summary>
        /// 校验 Key 是否存在
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<bool> KeyExistsAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.KeyExistsAsync(redisKey);
        }

        /// <summary>
        /// 重命名 Key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisNewKey"></param>
        /// <returns></returns>
        public async Task<bool> KeyRenameAsync(string redisKey, string redisNewKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.KeyRenameAsync(redisKey, redisNewKey);
        }

        /// <summary>
        /// 设置 Key 的时间
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<bool> KeyExpireAsync(string redisKey, TimeSpan? expiry)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await _db.KeyExpireAsync(redisKey, expiry);
        }

        #endregion key-async

        #endregion key 操作

        #region 发布订阅

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handle)
        {
            var sub = _connMultiplexer.GetSubscriber();
            sub.Subscribe(channel, handle);
        }

        /// <summary>
        /// 发布
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public long Publish(RedisChannel channel, RedisValue message)
        {
            var sub = _connMultiplexer.GetSubscriber();
            return sub.Publish(channel, message);
        }

        /// <summary>
        /// 发布（使用序列化）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public long Publish<T>(RedisChannel channel, T message)
        {
            var sub = _connMultiplexer.GetSubscriber();
            return sub.Publish(channel, JSONSerialize(message));
        }

        #region 发布订阅-async

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        public async Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handle)
        {
            var sub = _connMultiplexer.GetSubscriber();
            await sub.SubscribeAsync(channel, handle);
        }

        /// <summary>
        /// 发布
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<long> PublishAsync(RedisChannel channel, RedisValue message)
        {
            var sub = _connMultiplexer.GetSubscriber();
            return await sub.PublishAsync(channel, message);
        }

        /// <summary>
        /// 发布（使用序列化）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<long> PublishAsync<T>(RedisChannel channel, T message)
        {
            var sub = _connMultiplexer.GetSubscriber();
            return await sub.PublishAsync(channel, JSONSerialize(message));
        }

        #endregion 发布订阅-async

        #endregion 发布订阅

        #region private method

        /// <summary>
        /// 添加 Key 的前缀
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string AddKeyPrefix(string? key)
        {
            return $"{DefaultKey}:{key}";
        }

        #region 注册事件

        /// <summary>
        /// 添加注册事件
        /// </summary>
        private void AddRegisterEvent()
        {
            _connMultiplexer.ConnectionRestored += ConnMultiplexer_ConnectionRestored;
            _connMultiplexer.ConnectionFailed += ConnMultiplexer_ConnectionFailed;
            _connMultiplexer.ErrorMessage += ConnMultiplexer_ErrorMessage;
            _connMultiplexer.ConfigurationChanged += ConnMultiplexer_ConfigurationChanged;
            _connMultiplexer.HashSlotMoved += ConnMultiplexer_HashSlotMoved;
            _connMultiplexer.InternalError += ConnMultiplexer_InternalError;
            _connMultiplexer.ConfigurationChangedBroadcast += ConnMultiplexer_ConfigurationChangedBroadcast;
        }

        /// <summary>
        /// 重新配置广播时（通常意味着主从同步更改）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
        {
            Console.WriteLine($"{nameof(ConnMultiplexer_ConfigurationChangedBroadcast)}: {e.EndPoint}");
        }

        /// <summary>
        /// 发生内部错误时（主要用于调试）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_InternalError(object sender, InternalErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(ConnMultiplexer_InternalError)}: {e.Exception}");
        }

        /// <summary>
        /// 更改集群时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_HashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            Console.WriteLine(
                $"{nameof(ConnMultiplexer_HashSlotMoved)}: {nameof(e.OldEndPoint)}-{e.OldEndPoint} To {nameof(e.NewEndPoint)}-{e.NewEndPoint}, ");
        }

        /// <summary>
        /// 配置更改时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConfigurationChanged(object sender, EndPointEventArgs e)
        {
            Console.WriteLine($"{nameof(ConnMultiplexer_ConfigurationChanged)}: {e.EndPoint}");
        }

        /// <summary>
        /// 发生错误时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(ConnMultiplexer_ErrorMessage)}: {e.Message}");
        }

        /// <summary>
        /// 物理连接失败时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine($"{nameof(ConnMultiplexer_ConnectionFailed)}: {e.Exception}");
        }

        /// <summary>
        /// 建立物理连接时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine($"{nameof(ConnMultiplexer_ConnectionRestored)}: {e.Exception}");
        }

        #endregion 注册事件

        /// <summary>
        /// 二进制序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [Obsolete("System.Runtime.Serialization.Formatters.Binary 在.net core下，不可使用。替代方案——protobuf-net")]
        private static byte[]? Serialize(object obj)
        {
            //if (obj == null)
            //    return null;

            //var binaryFormatter = new BinaryFormatter();
            //using (var memoryStream = new MemoryStream())
            //{
            //    binaryFormatter.JSONSerialize(memoryStream, obj);
            //    var data = memoryStream.ToArray();
            //    return data;
            //}
            return default;
        }

        /// <summary>
        /// 二进制反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        [Obsolete("System.Runtime.Serialization.Formatters.Binary 在.net core下，不可使用。替代方案——protobuf-net")]
        private static T? DeSerialize<T>(byte[] data)
        {
            //if (data == null)
            //    return default(T);

            //var binaryFormatter = new BinaryFormatter();
            //using (var memoryStream = new MemoryStream(data))
            //{
            //    var result = (T)binaryFormatter.JSONDeSerialize(memoryStream);
            //    return result;
            //}
            return default(T);
        }

        /// <summary>
        /// json序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string? JSONSerialize(object? obj)
        {
            if (obj == null)
                return default;

            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// json反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        private static T? JSONDeSerialize<T>(string? data)
        {
            if (data == null)
                return default(T);

            return JsonConvert.DeserializeObject<T>(data);
        }

        #endregion private method

        #region Bloom
        /// <summary>
        /// 布隆过滤器添加值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>新增插入返回1，覆盖插入返回0</returns>
        public async Task<bool> BloomAddAsync(string key, string value)
        {
            return await RedisBloomExtensions.BloomAddAsync(_db, key, value);
        }
        /// <summary>
        /// 布隆过滤器添加值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>数组：新增插入返回1，覆盖插入返回0</returns>
        public async Task<bool[]?> BloomMAddAsync(string key, IEnumerable<string> values)
        {
            return (bool[]?)await RedisBloomExtensions.BloomMAddAsync(_db, key, values.Select(x => new RedisValue(x)));
        }
        /// <summary>
        /// 将多个元素插入到布隆过滤器
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="errorRate">误判率</param>
        /// <param name="initialCapacity">容量</param>
        /// <param name="notCreateIfExit">NOCREATE：如果布隆过滤器key不存在，则不进行插入，并报错</param>
        /// <param name="isNscal">当过滤器达到最大容量时，不创建子过滤器，并报错</param>
        /// <param name="values">值</param>
        /// <returns>数组：新增插入返回1，覆盖插入返回0</returns>
        public async Task<bool[]?> BloomInsertAsync(string key, IEnumerable<string> values, double errorRate, int initialCapacity, bool notCreateIfExit = false, bool isNscal = false)
        {
            return await RedisBloomExtensions.BloomInsertAsync(_db, key, values.Select(x => new RedisValue(x)), errorRate, initialCapacity, notCreateIfExit, isNscal);
        }
        /// <summary>
        /// 将多个元素插入到布隆过滤器
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="errorRate">误判率</param>
        /// <param name="initialCapacity">容量</param>
        /// <param name="notCreateIfExit">NOCREATE：如果布隆过滤器key不存在，则不进行插入，并报错</param>
        /// <param name="isNscal">当过滤器达到最大容量时，不创建子过滤器，并报错</param>
        /// <param name="values">值</param>
        /// <returns>数组：新增插入返回1，覆盖插入返回0</returns>
        public async Task<bool[]?> BloomInsertAsync(string key, IEnumerable<string> values, bool notCreateIfExit = false, bool isNscal = false)
        {
            return await RedisBloomExtensions.BloomInsertAsync(_db, key, values.Select(x => new RedisValue(x)), notCreateIfExit, isNscal);
        }
        /// <summary>
        /// 布隆过滤器验证值是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>存在返回1，不存在返回0</returns>
        public async Task<bool> BloomExistAsync(string key, string value)
        {
            return await RedisBloomExtensions.BloomExistsAsync(_db, key, value);
        }

        /// <summary>
        /// 布隆过滤器验证值是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>数组：存在返回1，不存在返回0</returns>
        public async Task<bool[]?> BloomMExistAsync(string key, IEnumerable<string> value)
        {
            return await RedisBloomExtensions.BloomMExistsAsync(_db, key, value.Select(x=>new RedisValue(x)));
        }


        /// <summary>
        /// 设置布隆过滤器key的错误率和初始容量
        /// </summary>
        /// <param name="key"></param>
        /// <param name="errorRate"></param>
        /// <param name="initialCapacity"></param>
        /// <returns></returns>
        public async Task BloomReserveAsync(RedisKey key, double errorRate, int initialCapacity)
        {
            await RedisBloomExtensions.BloomReserveAsync(_db, key, errorRate, initialCapacity);
        }
        /// <summary>
        /// 查看布隆过滤器key的信息，null表示没有这个key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<object?> BloomInfoAsync(RedisKey key)
        {
            try
            {
                return await RedisBloomExtensions.BloomInfoAsync(_db, key);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ERR not found")) return null;
                throw;
            }
        }
        #endregion
    }


    /// <summary>
    /// 布隆过滤器扩展
    /// </summary>
    public static class RedisBloomExtensions
    {
        /*
         
Command	Description
BF.ADD	添加一个元素到布隆过滤器，不存在key时自动创建key。
BF.EXISTS	判断元素是否在布隆过滤器，不存在key时返回0
BF.INFO	返回有关布隆过滤器的信息。若不存在会报错——(error) ERR not found
BF.INSERT	将多个元素添加到过滤器，不存在key时自动创建key。
BF.MADD	添加多个元素到布隆过滤器，不存在key时自动创建key，
BF.MEXISTS	判断多个元素是否在布隆过滤器，不存在key时返回0
BF.RESERVE	创建一个布隆过滤器。设置误判率和容量。若已存在会报错——(error) ERR item exists
BF.SCANDUMP	开始增量保存 Bloom 过滤器。
BF.LOADCHUNK	恢复之前使用BF.SCANDUMP保存的布隆过滤器。

        可以使用del key。删除布隆过滤器，但是不支持移除成员
        BF.ADD、BF.MADD、BF.INSERT(不指定NOCREATE参数)执行时，不存在key时自动创建布隆过滤器key，默认值：Capacity: 100; error_rate：0.01。也可以主动执行 BF.RESERVE 创建key，自定义初始容量和误判率
        BF.RESERVE error_rate越低，需要的空间越大。当过滤器内存在的数量超过指定 Capacity，会创建子过滤器误判率会增加。所以一开始要准备好 Capacity
        BF.INSERT 若指定NOCREATE参数，不存在key时则不创建key并报错——(error) ERR not found。若指定NONSCALING参数，当过滤器达到最大容量时，不创建子过滤器，并报错——ERR non scaling filter is full

         */
        /// <summary>
        /// 创建一个布隆过滤器
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="errorRate">误判率</param>
        /// <param name="initialCapacity">容量</param>
        /// <returns></returns>
        public static async Task BloomReserveAsync(this IDatabaseAsync db, RedisKey key, double errorRate, int initialCapacity)
        {
            // BF.RESERVE key 0.01 100
            await db.ExecuteAsync("BF.RESERVE", key, errorRate, initialCapacity);
        }

        /// <summary>
        /// 添加一个元素
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="value">元素</param>
        /// <returns>新增插入返回1，覆盖插入返回0</returns>
        public static async Task<bool> BloomAddAsync(this IDatabaseAsync db, RedisKey key, RedisValue value)
        {
            // BF.ADD key value
            return (bool)await db.ExecuteAsync("BF.ADD", key, value);
        }
        /// <summary>
        /// 添加多个元素
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="values">多个元素</param>
        /// <returns>数组：新增插入返回1，覆盖插入返回0</returns>
        public static async Task<bool[]?> BloomMAddAsync(this IDatabaseAsync db, RedisKey key, IEnumerable<RedisValue> values)
        {
            // bf.madd key value value2 value3
            return (bool[]?)await db.ExecuteAsync("BF.MADD", values.Cast<object>().Prepend(key).ToArray());
        }

        /// <summary>
        /// 将多个元素插入到布隆过滤器
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="errorRate">误判率</param>
        /// <param name="initialCapacity">容量</param>
        /// <param name="notCreateIfExit">NOCREATE：如果布隆过滤器key不存在，则不进行插入，并报错</param>
        /// <param name="isNscal">当过滤器达到最大容量时，不创建子过滤器，并报错</param>
        /// <param name="values">值</param>
        /// <returns>数组：新增插入返回1，覆盖插入返回0</returns>
        public static async Task<bool[]?> BloomInsertAsync(this IDatabaseAsync db, RedisKey key, IEnumerable<RedisValue> values, double errorRate, int initialCapacity, bool notCreateIfExit, bool isNscal)
        {
            // BF.INSERT key CAPACITY 10 ERROR 0.01 ITEMS "value1" value2
            // BF.INSERT key CAPACITY 10 ERROR 0.01 NOCREATE ITEMS "value1" "value2" value3

            var paramValues = new List<RedisValue>() { "CAPACITY", initialCapacity, "ERROR", errorRate};
            return await BloomInsertAsync(db, key, values, notCreateIfExit, isNscal, paramValues);
        }
        /// <summary>
        /// 将多个元素插入到布隆过滤器
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="notCreateIfExit">NOCREATE：如果布隆过滤器key不存在，则不进行插入，并报错</param>
        /// <param name="isNscal">当过滤器达到最大容量时，不创建子过滤器，并报错</param>
        /// <param name="values">值</param>
        /// <returns>数组：新增插入返回1，覆盖插入返回0</returns>
        internal static async Task<bool[]?> BloomInsertAsync(IDatabaseAsync db, RedisKey key, IEnumerable<RedisValue> values, bool notCreateIfExit, bool isNscal)
        {
            // BF.INSERT key ITEMS "value1" value2
            // BF.INSERT key NOCREATE NONSCALING ITEMS "value1" value2

            return await BloomInsertAsync(db, key, values, notCreateIfExit, isNscal, null);
        }
        /// <summary>
        /// 将多个元素插入到布隆过滤器
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="notCreateIfExit">NOCREATE：如果布隆过滤器key不存在，则不进行插入，并报错</param>
        /// <param name="isNscal">当过滤器达到最大容量时，不创建子过滤器，并报错</param>
        /// <param name="values">值</param>
        /// <param name="paramValues">初始化值</param>
        /// <returns>数组：新增插入返回1，覆盖插入返回0</returns>
        private static async Task<bool[]?> BloomInsertAsync(IDatabaseAsync db, RedisKey key, IEnumerable<RedisValue> values, bool notCreateIfExit, bool isNscal, List<RedisValue>? paramValues = null)
        {
            try
            {
                paramValues ??= new List<RedisValue>();
                if (isNscal) paramValues.Add("NONSCALING");       // 超过容量不创建子过滤器
                if (notCreateIfExit) paramValues.Add("NOCREATE");
                paramValues.Add("ITEMS");   // 也可以使用ITEM
                paramValues.AddRange(values);

                return (bool[]?)await db.ExecuteAsync("BF.INSERT", paramValues.Cast<object>().Prepend(key).ToArray());
            }
            catch (Exception ex)
            {
                if (notCreateIfExit && ex.Message.Contains("ERR not found")) throw new Exception("指定 NOCREATE 参数，布隆过滤器key不存在，无法插入。" + ex.Message, ex);
                if (isNscal && ex.Message.Contains("ERR non scaling filter is full")) throw new Exception("指定 NONSCALING 参数，当前容量超出指定容量，无法插入。" + ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// 判断元素是否存在
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="value">元素</param>
        /// <returns>存在返回1，不存在返回0</returns>
        public static async Task<bool> BloomExistsAsync(this IDatabaseAsync db, RedisKey key, RedisValue value)
        {
            // bf.exists key value
            return (bool)await db.ExecuteAsync("BF.EXISTS", key, value);
        }

        /// <summary>
        /// 判断多个元素是否存在
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="values">多个元素</param>
        /// <returns>存在返回1，不存在返回0</returns>
        public static async Task<bool[]?> BloomMExistsAsync(this IDatabaseAsync db, RedisKey key, IEnumerable<RedisValue> values)
        {
            // bf.mexists key value values
            return (bool[]?)await db.ExecuteAsync("BF.MEXISTS", values.Cast<object>().Prepend(key).ToArray());
        }

        /// <summary>
        /// 查看key
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="key">键</param>
        /// <param name="values">多个元素</param>
        /// <returns></returns>
        public static async Task<object> BloomInfoAsync(this IDatabaseAsync db, RedisKey key)
        {
            // bf.info key
            return await db.ExecuteAsync("BF.INFO", key);
        }

        
    }
}
