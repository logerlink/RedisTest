using RedisTest.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisTest.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly RedisTestDbContext _dbContext;

        public UnitOfWork(RedisTestDbContext redisTestDbContext)
        {
            _dbContext = redisTestDbContext;
        }

        public RedisTestDbContext GetDbContext()
        {
            return _dbContext;
        }

        public int SaveChanges()
        {
            return _dbContext.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }
    }
}
