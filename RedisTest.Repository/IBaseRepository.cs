using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisTest.DataAccess
{
    public interface IBaseRepository<T>
    {
        #region 查询
        /// <summary>
        /// 查询单个记录
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        T? GetOne(Expression<Func<T, bool>> condition);

        /// <summary>
        /// 查询多个记录
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        IQueryable<T> Get(Expression<Func<T, bool>> condition);

        #endregion

        #region 增删改
        void AddItems(IEnumerable<T> items);
        void AddItemsAsync(IEnumerable<T> items);
        #endregion
    }
}
