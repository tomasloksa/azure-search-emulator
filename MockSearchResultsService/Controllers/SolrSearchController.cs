using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MockSearchResultsService.Controllers
{
    [ApiController]
    [Route("solr/{indexName}/select")]
    public class SolrSearchController : ControllerBase
    {
        [HttpGet]
        public WeatherForecast Get(string q = "")
        {
            var rng = new Random();
            return new WeatherForecast
            {
                Date = DateTime.Now.AddDays(1),
                TemperatureC = rng.Next(-20, 55),
                Summary = q
            };
        }
    }
}
