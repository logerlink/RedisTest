using RedisTest.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisTest.DataAccess
{
    public interface IUnitOfWork
    {
        RedisTestDbContext GetDbContext();
        /// <summary>
        /// 保存更改
        /// </summary>
        /// <returns></returns>
        int SaveChanges();
        /// <summary>
        /// 保存更改
        /// </summary>
        /// <returns></returns>
        Task<int> SaveChangesAsync();
    }
}
