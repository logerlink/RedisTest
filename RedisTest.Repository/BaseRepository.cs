using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using RedisTest.Entities;

namespace RedisTest.DataAccess
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        #region 构造函数
        protected RedisTestDbContext _db;

        public BaseRepository(RedisTestDbContext context)
        {
            this._db = context;
        }


        #endregion

        #region 增删改
        public void AddItems(IEnumerable<T> items)
        {
            _db.Set<T>().AddRange(items);
        }

        public void AddItemsAsync(IEnumerable<T> items)
        {
            _db.Set<T>().AddRangeAsync(items);
        }
        #endregion

        #region 查询
        public IQueryable<T> Get(Expression<Func<T, bool>> condition)
        {
            return _db.Set<T>().Where(condition);
        }

        public T? GetOne(Expression<Func<T, bool>> condition)
        {
            return _db.Set<T>().FirstOrDefault(condition);
        }
        #endregion




    }
}
