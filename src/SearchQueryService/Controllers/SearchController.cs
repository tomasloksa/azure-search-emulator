using Microsoft.AspNetCore.Mvc;
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
        public async Task<string> GetAsync(
            [FromRoute] string indexName,
            int top = 10,
            int skip = 0,
            string search = "",
            string filter = "",
            //string searchMode = "", TODO find out if necessary
            string orderBy = ""
        )
        {
            var request = new HttpRequestMessage();
            var searchUrl = $"http://mocksearchresultsservice/solr/{indexName}/select?q=";
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

            request.RequestUri = new Uri(searchUrl);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _ = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return searchUrl;
        }
    }
}
