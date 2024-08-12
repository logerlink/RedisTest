using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockData;
using RedisTest.DataAccess;
using RedisTest.Entities;
using RedisTest.Service.IService;
using RedisTest.Share;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace RedisTest.Service.Impl
{
    public class UserService : BaseService, IUserService
    {
        private readonly IBaseRepository<User> _userRepository;
        private readonly ILogger<UserService> _logger;
        public UserService(RedisHelper redis, IUnitOfWork unitOfWork, ILogger<UserService> logger,
            IBaseRepository<User> userRepository) : base(redis, unitOfWork)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        private string GetNow()
        {
            return DateTime.Now.ToString("HH:mm:ss:fff");
        }
        /// <summary>
        /// 请求数据库——不使用互斥锁
        /// </summary>
        /// <param name="firstName">查询条件</param>
        /// <param name="logGuid">日志Id标识</param>
        /// <returns></returns>
        public async Task<int> GetCountWithoutLockAsync(string firstName, string? logGuid = null)
        {
            logGuid ??= Guid.NewGuid().ToString();
            string key = $"user:count";
            _logger.LogInformation($"GetCount:{logGuid}:开始执行。" + GetNow());
            var countResult = _redis.HashGet(key, firstName);
            if (!countResult.IsNull)
            {
                _logger.LogInformation($"GetCount:{logGuid}:返回缓存。" + GetNow());
                return int.Parse(countResult!);
            }

            await Task.Delay(5000);     // 模拟耗时  
            var countdb = await _userRepository.Get(x => x.Name != null && x.Name.StartsWith(firstName.Trim())).CountAsync();
            _redis.HashSet(key, firstName, countdb.ToString());
            _redis.KeyExpire(key, TimeSpan.FromSeconds(60));
            _logger.LogInformation($"GetCount:{logGuid}:设置缓存，返回数据库查询结果。" + GetNow());
            return countdb;
        }
        /// <summary>
        /// 请求数据库——使用互斥锁
        /// </summary>
        /// <param name="firstName">查询条件</param>
        /// <param name="logGuid">日志Id标识</param>
        /// <returns></returns>
        public async Task<int> GetCountWithLockAsync(string firstName, string? logGuid = null)
        {
            logGuid ??= Guid.NewGuid().ToString();
            string key = $"user:count";
            _logger.LogInformation($"GetCount:{logGuid}:开始执行。"+ GetNow());
            var countResult = _redis.HashGet(key, firstName);
            if (!countResult.IsNull)
            {
                _logger.LogInformation($"GetCount:{logGuid}:返回缓存。" + GetNow());
                return int.Parse(countResult!);
            }
            string lockKey = $"lock:{key}:{firstName}";
            var lockValue = Guid.NewGuid().ToString();
            try
            {
                if (await _redis.TryLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(120)))   // 根据数据库逻辑时长设置锁时间，可以设置久一点，反正会主动释放
                {
                    // 获取到锁，缓存重建
                    _logger.LogInformation($"GetCount:{logGuid}:获取到锁。" + GetNow());
                    countResult = _redis.HashGet(key, firstName);
                    if (!countResult.IsNull)
                    {
                        _logger.LogInformation($"GetCount:{logGuid}:返回缓存2");
                        return int.Parse(countResult!);
                    }
                    await Task.Delay(5000);     // 模拟耗时  
                    var countdb = await _userRepository.Get(x => x.Name != null && x.Name.StartsWith(firstName.Trim())).CountAsync();
                    _redis.HashSet(key, firstName, countdb.ToString());
                    _redis.KeyExpire(key, TimeSpan.FromSeconds(60));
                    _logger.LogInformation($"GetCount:{logGuid}:设置缓存，返回数据库查询结果。" + GetNow());
                    return countdb;
                }
                else
                {
                    // 未获取到锁
                    _logger.LogWarning($"GetCount:{logGuid}:未获取到锁");
                    await Task.Delay(1000);
                    return await GetCountWithLockAsync(firstName, logGuid);      // Todo：限制递归次数
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求失败");
                return -1;
            }
            finally
            {
                await _redis.ReleaseLockAsync(lockKey, lockValue);
            }
        }

        public int RegistBatch(int count)
        {
            var list = Enumerable.Range(0, count).Select(x => new User()
            {
                Id = Guid.NewGuid(),
                Age = MockData.Number.Get(10, 100),
                Name = MockData.UserInfo.GetFullName()
            }).ToList();
            _userRepository.AddItems(list);
            return _unitOfWork.SaveChanges();
        }

        private class ExpireData<T>
        {
            /// <summary>
            /// 真实数据
            /// </summary>
            public T? Data { get; set; }
            /// <summary>
            /// 指定过期时间
            /// </summary>
            public DateTime ExpireDate { get; set; }
        }

        /// <summary>
        /// 模拟预备数据
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="logGuid"></param>
        /// <returns></returns>
        public void SetCacheCountexpire(string firstName, string? logGuid = null)
        {
            logGuid ??= Guid.NewGuid().ToString();
            string key = $"user:countexpire";
            _logger.LogInformation($"SetCacheCountexpire:{logGuid}:设置缓存，开始执行。" + GetNow());
            var expireData = new ExpireData<int>() { Data = 1000, ExpireDate = DateTime.Now.AddSeconds(-30) };
            _redis.HashSet(key, firstName, expireData);
            _logger.LogInformation($"SetCacheCountexpire:{logGuid}:设置缓存，成功。" + GetNow());
        }

        /// <summary>
        /// 请求数据库——逻辑过期
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="logGuid"></param>
        /// <returns></returns>
        public async Task<int> GetCountWithExpirAsync(string firstName, string? logGuid = null)
        {
            logGuid ??= Guid.NewGuid().ToString();
            string key = $"user:countexpire";
            _logger.LogInformation($"GetCountWithExpirAsync:{logGuid}:开始执行。" + GetNow());
            var countResult = _redis.HashGet<ExpireData<int>>(key, firstName);
            if (countResult == null)
            {
                _logger.LogInformation($"GetCountWithExpirAsync:{logGuid}:没有缓存，返回空或预设值。" + GetNow());
                // 如果读取缓存为空，表示不是“热点数据”可以直接返回空或预设值即可。不过，也可以在此处走"已过期的逻辑"，具体按业务来决定
                return -1;
            }
            // 未过期
            if (countResult.ExpireDate >= DateTime.Now)
            {
                _logger.LogInformation($"GetCountWithExpirAsync:{logGuid}:未过期，返回缓存-{countResult.Data}。" + GetNow());
                return countResult.Data;
            }
            
            // 已过期
            string lockKey = $"lock:{key}:{firstName}";
            var lockValue = Guid.NewGuid().ToString();
            try
            {
                if (await _redis.TryLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(100)))   // 根据数据库逻辑时长设置锁时间，可以设置久一点，反正会主动释放
                {
                    // 获取到锁，缓存重建
                    _logger.LogInformation($"GetCountWithExpirAsync:{logGuid}:已过期，获取到锁，缓存重建。" + GetNow());

                    await Task.Delay(5000);     // 模拟耗时  
                    var countdb = await _userRepository.Get(x => x.Name != null && x.Name.StartsWith(firstName.Trim())).CountAsync();
                    var expireData = new ExpireData<int>() { Data = countdb, ExpireDate = DateTime.Now.AddMinutes(2) };
                    _redis.HashSet(key, firstName, expireData);
                    // 不设置过期时间，或者设置久一点。确保读取的缓存不为空即可
                    _logger.LogInformation($"GetCountWithExpirAsync:{logGuid}:设置缓存，返回数据库查询结果——{countdb}。" + GetNow());
                    return countdb;
                }
                else
                {
                    // 未获取到锁
                    _logger.LogWarning($"GetCountWithExpirAsync:{logGuid}:已过期且未获取到锁，返回过期数据——{countResult.Data}");
                    return countResult.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求失败");
                return -1;
            }
            finally
            {
                await _redis.ReleaseLockAsync(lockKey, lockValue);
            }
        }

        /// <summary>
        /// 根据年龄查询数据库——模拟缓存穿透，age=-1
        /// </summary>
        /// <param name="age"></param>
        /// <param name="logGuid"></param>
        /// <returns></returns>
        public async Task<List<User>?> GetUserByAgeAsync(int age, string? logGuid = null)
        {
            if (age < 0) return null;
            logGuid ??= Guid.NewGuid().ToString();
            string key = $"user:age:{age}";
            _logger.LogInformation($"GetUserByAge:{logGuid}:开始执行。参数age={age}。" + GetNow());
            var users = _redis.StringGet<List<User>>(key);
            if (users?.Any() == true)
            {
                _logger.LogInformation($"GetUserByAge:{logGuid}:有缓存，返回缓存。" + GetNow());
                return users;
            }
            // Todo:使用互斥锁
            await Task.Delay(5000);     // 模拟耗时
            users = _userRepository.Get(x => x.Age == age).ToList();
            if (users?.Any() == true)
            {
                _logger.LogInformation($"GetUserByAge:{logGuid}:查询数据库，结果不为空，设置缓存。" + GetNow());
                _redis.StringSet(key, users);
            }
            else
            {
                _logger.LogInformation($"GetUserByAge:{logGuid}:查询数据库，结果为空，不设置缓存。" + GetNow());
            }
            return users;
        }

       

        /// <summary>
        /// 根据年龄查询数据库——模拟处理缓存穿透，age=99，缓存空值
        /// </summary>
        /// <param name="age"></param>
        /// <param name="logGuid"></param>
        /// <returns></returns>
        public async Task<List<User>?> GetUserByAgeNullAsync(int age, string? logGuid = null)
        {
            // 年龄不能小于0，否则返回空或预设值
            if (age < 0)
            {
                _logger.LogWarning($"GetUserByAge:{logGuid}:检查参数。参数age={age}。年龄不能小于0" + GetNow());
                return null;
            }
            logGuid ??= Guid.NewGuid().ToString();
            string key = $"user:age:{age}";
            _logger.LogInformation($"GetUserByAge:{logGuid}:开始执行。参数age={age}。" + GetNow());
            // 判断缓存key是否存在
            if (_redis.KeyExists(key))
            {
                var users = _redis.StringGet<List<User>>(key);
                _logger.LogInformation($"GetUserByAge:{logGuid}:有缓存，返回缓存。数据为空：{users?.Any() != true}。" + GetNow());
                return users;
            }

            // Todo:使用互斥锁
            await Task.Delay(5000);     // 模拟耗时
            var dbUsers = _userRepository.Get(x => x.Age == age).ToList();
            _logger.LogInformation($"GetUserByAge:{logGuid}:查询数据库，数据为空：{dbUsers?.Any() != true}。设置缓存。" + GetNow());
            _redis.StringSet(key, dbUsers);
            return dbUsers;
        }



        #region 布隆过滤器
        /// <summary>
        /// 布隆过滤器使用
        /// </summary>
        private async void UseBloomAsync()
        {
            var bloomKey = "bloom:user:age";
            var xxx = await _redis.BloomMAddAsync(bloomKey, Enumerable.Range(0, 150).Select(x => x.ToString()).Distinct());

            var q = await _redis.BloomAddAsync(bloomKey, "jj");
            var w = await _redis.BloomExistAsync(bloomKey, "jj");
            var e = await _redis.BloomMExistAsync(bloomKey, new List<string>() { "jj", "oo" });

            var r = await _redis.BloomInsertAsync(bloomKey + 1, new List<string>() { "jj", "oo" });
            try
            {
                var t = await _redis.BloomInsertAsync(bloomKey + 2, new List<string>() { "jj", "oo" }, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);   // 布隆过滤器key不存在，无法插入。ERR not found
            }

            try
            {
                var y = await _redis.BloomInsertAsync(bloomKey + 2, Enumerable.Range(0, 150).Select(x => x.ToString()), isNscal: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);   // 布隆过滤器key不存在，无法插入。ERR not found
            }

            var u = await _redis.BloomInsertAsync(bloomKey + 3, new List<string>() { "jj", "oo" }, 0.001, 100);
        }

        /// <summary>
        /// 批量添加用户
        /// </summary>
        /// <param name="count"></param>
        /// <param name="logGuid"></param>
        /// <returns></returns>
        public async Task<int> AddUserBloomAsync(int count, string? logGuid = null)
        {
            logGuid ??= Guid.NewGuid().ToString();

            var list = Enumerable.Range(0, count).Select(x => new User()
            {
                Id = Guid.NewGuid(),
                Age = MockData.Number.Get(10, 100),
                Name = MockData.UserInfo.GetFullName()
            }).ToList();
            _userRepository.AddItems(list);

            var bloomKey = "bloom:user:age";
            // 判断过滤器是否存在
            var keyInfo = await _redis.BloomInfoAsync(bloomKey);
            if (keyInfo == null)
            {
                // 不存在则创建，错误率：0.01，初始容量：200
                await _redis.BloomReserveAsync(bloomKey, 0.01, 200);
            }
            // 往布隆过滤器插入年龄，
            await _redis.BloomMAddAsync(bloomKey, list.Select(x => x.Age.ToString()).Distinct());
            // 保存数据库

            // todo：保证数据库和缓存的最终一致性
            return _unitOfWork.SaveChanges();
        }

        /// <summary>
        /// 根据年龄查询数据库——模拟处理缓存穿透，age=99，布隆过滤器
        /// </summary>
        /// <param name="age"></param>
        /// <param name="logGuid"></param>
        /// <returns></returns>
        public async Task<List<User>?> GetUserByAgeBloomAsync(int age, string? logGuid = null)
        {
            // 年龄不能小于0，否则返回空或预设值
            if (age < 0)
            {
                _logger.LogWarning($"GetUserByAgeBloomAsync:{logGuid}:检查参数。参数age={age}。年龄不能小于0" + GetNow());
                return null;
            }
            logGuid ??= Guid.NewGuid().ToString();
            string key = $"user:age:{age}";
            _logger.LogInformation($"GetUserByAgeBloomAsync:{logGuid}:开始执行。参数age={age}。" + GetNow());
            // 判断缓存key是否存在
            var users = _redis.StringGet<List<User>>(key);
            if (users?.Any() == true)
            {
                _logger.LogInformation($"GetUserByAgeBloomAsync:{logGuid}:有缓存，返回缓存。" + GetNow());
                return users;
            }
            var bloomKey = "bloom:user:age";
            // 判断布隆过滤器是否存在指定年龄age
            if (!(await _redis.BloomExistAsync(bloomKey, age.ToString())))
            {
                // 不存在年龄age，返回空或预设值
                _logger.LogInformation($"GetUserByAgeBloomAsync:{logGuid}:布隆过滤器没有值，返回空或预设值。" + GetNow());
                return null;
            }
            // 存在年龄age，再查询数据库
            _logger.LogInformation($"GetUserByAgeBloomAsync:{logGuid}:布隆过滤器有值，开始查询数据库。" + GetNow());
            // Todo:使用互斥锁
            await Task.Delay(5000);     // 模拟耗时
            var dbUsers = _userRepository.Get(x => x.Age == age).ToList();
            if(dbUsers?.Any() != true)
            {
                // 虽然加了一层布隆过滤器，但还是会有误判率或者有人把布隆过滤器的key删掉了
                _logger.LogInformation($"GetUserByAgeBloomAsync:{logGuid}:查询数据库。没有值，返回空或预设值。" + GetNow());
                return null;
            }

            _logger.LogInformation($"GetUserByAgeBloomAsync:{logGuid}:查询数据库。有值，设置缓存。" + GetNow());
            _redis.StringSet(key, dbUsers);
            return dbUsers;

        }
        /// <summary>
        /// 将现有数据加入到布隆过滤器中
        /// </summary>
        /// <param name="logGuid"></param>
        /// <returns></returns>
        public async Task LoadAgeBloomAsync(string? logGuid = null)
        {
            var bloomKey = "bloom:user:age";
            // 判断过滤器是否存在
            var keyInfo = await _redis.BloomInfoAsync(bloomKey);
            if (keyInfo == null)
            {
                // 不存在则创建，错误率：0.01，初始容量：200
                await _redis.BloomReserveAsync(bloomKey, 0.01, 200);
            }
            var ages = _userRepository.Get(x => true).GroupBy(x => x.Age).Select(x => x.Key.ToString()).ToList();
            _logger.LogInformation($"LoadAgeBloomAsync:{logGuid}:开始执行。查询数据库。" + GetNow());
            await _redis.BloomMAddAsync(bloomKey, ages);
            _logger.LogInformation($"LoadAgeBloomAsync:{logGuid}:重新设置布隆过滤器。" + GetNow());
        }
        #endregion
        /// <summary>
        /// MemoryCache简单演示
        /// </summary>
        public void CacheTest()
        {
            // using System.Runtime.Caching;
            var cache = MemoryCache.Default;
            // 根据key获取缓存，重启程序后首次执行都不会有值。
            var cachedUser = cache.Get("key") as User;
            // 添加缓存
            cache.Add("key", new User(), DateTime.Now.AddMinutes(1));
            // 根据key获取缓存
            cachedUser = cache.Get("key") as User;
            if (cachedUser == null)
            {
                // 没有缓存
            }
            // 添加缓存newKey，若不存在key则添加返回null，若已存在不添加并返回缓存
            var newValue = cache.AddOrGetExisting("newKey", new User(), null);
            // 获取缓存对象
            var cacheItem = cache.GetCacheItem("newKey");
            // 删除缓存key
            cache.Remove("newKey");

        }

    }
}
