using EshopDemo.Api.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Azure;
using Azure.Search.Documents;
using Azure.Core.Pipeline;
using SearchQueryService.Indexes.Models;
using Newtonsoft.Json;

namespace EshopDemo.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class EshopItemsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ConnectionStringsOptions _connectionStrings;

        public EshopItemsController(
            IHttpClientFactory httpClientFactory,
            IOptions<ConnectionStringsOptions> configuration)
        {
            _httpClient = httpClientFactory.CreateClient("Default");
            _connectionStrings = configuration.Value;
        }

        [HttpGet]
        public async Task<ContentResult> Get() => Content(
            GetSearchResults(BuildSearchQuery(30, 0, "*:*", "not IsDeleted", "")).Result.ToString(), "application/json" );

        [HttpGet("search")]
        public async Task<ContentResult> Search(
            [FromQuery] string search,
            [FromQuery] string filter,
            [FromQuery] string orderBy,
            [FromQuery] int? top = null,
            [FromQuery] int? skip = null) => Content(
                GetSearchResults(BuildSearchQuery(top, skip, search, filter, orderBy)).Result.ToString(), "application/json");

        public async Task<object> GetSearchResults(string uri)
        {
            var credential = new AzureKeyCredential("abc");
            var options = new SearchClientOptions();
            options.Transport = new HttpClientTransport(_httpClient);
            var so = new SearchOptions();
            so.Skip = 1;
            var sc = new SearchClient(new System.Uri("https://loksa:8000"), "invoicingindex", credential, options);
            var doc = await sc.SearchAsync<SearchResponse>("abc", so);
            return JsonConvert.SerializeObject(doc.Value.GetResults());
        }

        private string BuildSearchQuery(int? top, int? skip, string search, string filter, string orderBy)
            => _connectionStrings["SearchService"]
            .AppendPathSegments("indexes", "invoicingindex", "docs", "search")
            .SetQueryParam("search", search)
            .SetQueryParam("$top", top)
            .SetQueryParam("$skip", skip)
            .SetQueryParam("$filter", filter)
            .SetQueryParam("$orderBy", orderBy);
    }
}
