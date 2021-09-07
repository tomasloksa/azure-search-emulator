using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes/{indexName}/docs")]
    public class SearchController : ControllerBase
    {
        private readonly HttpClient httpClient;
        SearchController()
        {
            httpClient = new HttpClient();
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get(
            [FromRoute]string indexName,
            int top = 30,
            int skip = 0,
            string search = "",
            string filter = "",
            string searchMode = "",
            string orderBy = ""
        )
        {
            var rng = new Random();

            httpClient.BaseAddress = new Uri("https://mocksearchresultsservice/searchQuery");

            return Enumerable.Range(1, 1).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = indexName + " " + search
            })
            .ToArray();
        }
    }
}
