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
    public class FirstController : ControllerBase
    {

        private readonly ApiDBContent _dbContext;
        public FirstController(ApiDBContent dbContext)
        {
            _dbContext = dbContext;
        }
        // GET api/values
        [HttpGet]
        public JsonResult Get()
        {
            return new JsonResult(_dbContext.CmNumberInfo.Take(2).ToList());
            //return new string[] { "value1", "value2" };
        }

    }
}