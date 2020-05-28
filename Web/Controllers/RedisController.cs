using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedisController : ControllerBase
    {
        /// <summary>
        /// 发布redis消息队列
        /// </summary>
        /// <param name="eqidPairs"></param>
        /// <returns></returns>
        [Route("batch")]
        [HttpPost]
        public async Task BatchPutEqidAndProfileIds([FromBody]List<CmNumberInfo> eqidPairs)
        {
            if (!ModelState.IsValid)
                throw new ArgumentException("Http Body Payload Error.");
            var redisKey = $"{DateTime.Now.ToString("yyyyMMdd")}";
            if (eqidPairs != null && eqidPairs.Any())
                RedisHelper.LPush(redisKey, eqidPairs.ToArray());
            await Task.CompletedTask;
        }
    }
}