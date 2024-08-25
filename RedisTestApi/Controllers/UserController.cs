using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedisTest.Entities;
using RedisTest.Service.Impl;
using RedisTest.Service.IService;

namespace RedisTest.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("hello")]
        public string Hello()
        {
            return "Hello World";
        }

        [HttpGet("regist")]
        public int RegistBatch([FromQuery] int count = 10)
        {
            return _userService.RegistBatch(count);
        }

        [HttpGet("countWithoutLock")]
        public async Task<int> GetCountWithoutLockAsync([FromQuery] string firstName = "")
        {
            return await _userService.GetCountWithoutLockAsync(firstName);
        }

        [HttpGet("countWithLock")]
        public async Task<int> GetCountWithLockAsync([FromQuery] string firstName = "")
        {
            return await _userService.GetCountWithLockAsync(firstName);
        }

        [HttpGet("SetCacheCountexpire")]
        public void SetCacheCountexpire([FromQuery] string firstName = "")
        {
            _userService.SetCacheCountexpire(firstName);
        }

        [HttpGet("countWithExpire")]
        public async Task<int> GetCountWithExpirAsync([FromQuery] string firstName = "")
        {
            return await _userService.GetCountWithExpirAsync(firstName);
        }

        [HttpGet("userByAge")]
        public async Task<List<User>?> GetUserByAgeAsync([FromQuery] int age)
        {
            return await _userService.GetUserByAgeAsync(age);
        }

        [HttpGet("userByAgeNull")]
        public async Task<List<User>?> GetUserByAgeNullAsync([FromQuery] int age)
        {
            return await _userService.GetUserByAgeNullAsync(age);
        }

        #region 布隆过滤器
        [HttpGet("addUserBloom")]
        public async Task<int> AddUserBloomAsync([FromQuery] int age)
        {
            return await _userService.AddUserBloomAsync(age);
        }

        [HttpGet("userByAgeBloom")]
        public async Task<List<User>?> GetUserByAgeBloomAsync([FromQuery] int age)
        {
            return await _userService.GetUserByAgeBloomAsync(age);
        }

        [HttpPost("loadAgeBloom")]
        public async Task LoadAgeBloomAsync()
        {
            await _userService.LoadAgeBloomAsync();
        }
        #endregion

        [HttpPost("cacheTest")]
        public void CacheTest()
        {
            _userService.CacheTest();
        }
        [HttpGet("redisReplicaTest")]
        public void RedisReplicaTest()
        {
            _userService.RedisReplicaTest();
        }

    }


}
