using Microsoft.Extensions.Logging;
using RedisTest.DataAccess;
using RedisTest.Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisTest.Service
{
    public class BaseService
    {
        protected readonly RedisHelper _redis;
        protected readonly IUnitOfWork _unitOfWork;

        public BaseService(RedisHelper redis, IUnitOfWork unitOfWork)
        {
            _redis = redis;
            _unitOfWork = unitOfWork;
        }
    }
}
