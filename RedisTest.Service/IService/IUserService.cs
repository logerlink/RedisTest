using RedisTest.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisTest.Service.IService
{
    public interface IUserService : IBaseService
    {
        Task<int> GetCountWithoutLockAsync(string firstName, string? logGuid = null);
        Task<int> GetCountWithLockAsync(string firstName, string? logGuid = null);
        int RegistBatch(int count);
        Task<int> GetCountWithExpirAsync(string firstName, string? logGuid = null);
        void SetCacheCountexpire(string firstName, string? logGuid = null);
        Task<List<User>?> GetUserByAgeAsync(int age, string? logGuid = null);
        Task<List<User>?> GetUserByAgeNullAsync(int age, string? logGuid = null);

        Task<int> AddUserBloomAsync(int count, string? logGuid = null);
        Task<List<User>?> GetUserByAgeBloomAsync(int age, string? logGuid = null);
        Task LoadAgeBloomAsync(string? logGuid = null);

        void CacheTest();
        void RedisReplicaTest();
        void RedisReplicaSentinelTest();

    }
}
