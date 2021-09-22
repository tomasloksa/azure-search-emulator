using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SearchQueryService.Indexes.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes/{indexName}/docs")]
    public class SearchController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        public SearchController(IHttpClientFactory httpClientFactory) => _httpClient = httpClientFactory.CreateClient();

        [HttpGet]
        public async Task<object> GetAsync(
            [FromRoute] string indexName,
            [FromQuery(Name ="$top")]
            int top = 10,
            [FromQuery(Name ="$skip")]
            int skip = 0,
            string search = "",
            string filter = "",
            //string searchMode = "", TODO find out if necessary
            string orderBy = ""
        )
        {
            var searchUrl = $"http://solr:8983/solr/{indexName}/select?q=";
            if (search.Length > 0)
            {
                searchUrl += search;
            }

            searchUrl += "&rows=" + top;
            searchUrl += "&start=" + skip;
            if (filter.Length > 0)
            {
                searchUrl += "&fq=" + filter;
            }

            if (orderBy.Length > 0)
            {
                searchUrl += "%sort=" + orderBy;
            }

            var response = await _httpClient.GetAsync(searchUrl);
            dynamic result = JsonConvert.DeserializeObject<SearchResponse>(await response.Content.ReadAsStringAsync());

            return result.Response.Docs;
        }
    }
}
