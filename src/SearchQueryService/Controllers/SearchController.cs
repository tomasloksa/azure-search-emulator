using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SearchQueryService.Indexes.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes/{indexName}/docs")]
    public class SearchController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public SearchController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _config = configuration;
        }

        [HttpGet]
        public async Task<object> GetAsync(
            [FromRoute] string indexName,
            [FromQuery(Name = "$top")] int? top,
            [FromQuery(Name = "$skip")] int? skip,
            [FromQuery] string search,
            [FromQuery] string filter,
            //string searchMode = "", TODO find out how to set
            [FromQuery(Name = "$orderby")] string orderBy
        )
        {
            var searchUrl = _config.GetConnectionString("SolrUri") + indexName + "/select?q=";
            if (!string.IsNullOrEmpty(search))
            {
                searchUrl += search;
            }

            if (top is not null)
            {
                searchUrl += "&rows=" + top;
            }

            if (skip is not null)
            {
                searchUrl += "&start=" + skip;
            }

            if (!string.IsNullOrEmpty(filter))
            {
                searchUrl += "&fq=" + filter;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                searchUrl += "&sort=" + orderBy;
            }

            var response = await _httpClient.GetAsync(searchUrl);
            dynamic result = JsonConvert.DeserializeObject<SearchResponse>(await response.Content.ReadAsStringAsync());

            return result.Response.Docs;
        }
    }
}
