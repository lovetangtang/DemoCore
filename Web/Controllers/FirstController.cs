using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    /// <summary>
    /// 测试控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FirstController : ControllerBase
    {

        private readonly ApiDBContent _dbContext;

        /// <summary>
        /// 测试
        /// </summary>
        /// <param name="dbContext"></param>
        public FirstController(ApiDBContent dbContext)
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// 获取测试数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult Get()
        {
            return new JsonResult(_dbContext.CmNumberInfo.Take(2).ToList());
            //return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// 新增
        /// </summary>
        [HttpPost]
        public  void Post(CmNumberInfo cmNumberInfo)
        {
            _dbContext.CmNumberInfo.Add(cmNumberInfo);
            _dbContext.SaveChanges();
        }

        /// <summary>
        /// 修改
        /// </summary>

        [HttpPut]
        public void Put()
        {

        }

        /// <summary>
        /// 删除
        /// </summary>

        [HttpDelete]
        public void Delete()
        {

        }
    }
}