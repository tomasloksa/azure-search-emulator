using Flurl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchQueryService.Config;
using SearchQueryService.Indexes.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes/{indexName}/docs/")]
    public class SearchController : ControllerBase
    {
        static readonly Dictionary<string, string> _replacements = new()
        {
            { @"(\w+)\s+(ge)\s+([^\s]+)", "$1:[$3 TO *]"},
            { @"(\w+)\s+(gt)\s+([^\s]+)", "$1:{$3 TO *}"},
            { @"(\w+)\s+(le)\s+([^\s]+)", "$1:[* TO $3]"},
            { @"(\w+)\s+(lt)\s+([^\s]+)", "$1:{* TO $3}"},
            { @"(\w+)\s+(ne)", "NOT $1:"}
        };

        private readonly HttpClient _httpClient;
        private readonly ConnectionStringsOptions _connectionStrings;

        public SearchController(
            IHttpClientFactory httpClientFactory,
            IOptions<ConnectionStringsOptions> configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _connectionStrings = configuration.Value;
        }

        [HttpGet("search")]
        public async Task<object> GetAsync(
            [FromRoute] string indexName,
            [FromQuery(Name = "$top")] int? top,
            [FromQuery(Name = "$skip")] int? skip,
            [FromQuery] string search,

            [FromQuery(Name = "$filter")] string filter,
            //string searchMode = "", TODO find out how to set
            [FromQuery(Name = "$orderby")] string orderBy
        )
        {
            var response = await _httpClient.GetAsync(BuildSearchQuery(indexName, top, skip, search, filter, orderBy));
            dynamic result = JsonConvert.DeserializeObject<SearchResponse>(await response.Content.ReadAsStringAsync());

            return result.Response.Docs;
        }

        [HttpPost("index")]
        public async void PostAsync([FromBody] JObject value)
        {


            return;
        }

        private string BuildSearchQuery(string indexName, int? top, int? skip, string search, string filter, string orderBy)
        {
            return _connectionStrings["Solr"]
                .AppendPathSegments(indexName, "select")
                .SetQueryParams(new
                {
                    q = search,
                    rows = top,
                    start = skip,
                    fq = string.IsNullOrEmpty(filter) ? filter : AzToSolrQuery(filter),
                    sort = orderBy
                });

        }

        private static string AzToSolrQuery(string filter)
        {
            foreach (var kv in _replacements)
            {
                filter = Regex.Replace(filter, kv.Key, kv.Value);
            }

            var sb = new StringBuilder(filter);
            sb.Replace(" eq", ":");
            sb.Replace("and", "AND");
            sb.Replace("or", "OR");
            sb.Replace("not", "NOT");

            return sb.ToString();
        }
    }
}
