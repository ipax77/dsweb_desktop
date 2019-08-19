using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace sc2dsstats.Data
{
    public class Mobile
    {
    }

    public class ValuesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ValuesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("dummy2")]
        public async Task<ActionResult> Get()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://api.github.com");
            string result = await client.GetStringAsync("/");
            return Ok(result);
        }
    }

}
