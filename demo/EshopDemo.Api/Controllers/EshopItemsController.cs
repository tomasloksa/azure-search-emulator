using EshopDemo.Api.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;

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
            _httpClient = httpClientFactory.CreateClient();
            _connectionStrings = configuration.Value;
        }

        [HttpGet]
        public async Task<ContentResult> Get() => Content(
            await GetSearchResults(BuildSearchQuery(30, 0, "*:*", "not IsDeleted", "")), "application/json" );

        [HttpGet("search")]
        public async Task<ContentResult> Search(
            [FromQuery] string search,
            [FromQuery] string filter,
            [FromQuery] string orderBy,
            [FromQuery] int? top = null,
            [FromQuery] int? skip = null) => Content(
                await GetSearchResults(BuildSearchQuery(top, skip, search, filter, orderBy)), "application/json");

        public async Task<string> GetSearchResults(string uri)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(uri);

            Response.StatusCode = (int)response.StatusCode;
            return await response.Content.ReadAsStringAsync();
        }

        private string BuildSearchQuery(int? top, int? skip, string search, string filter, string orderBy)
            => _connectionStrings["SearchService"]
            .AppendPathSegments("indexes", "invoicingindex", "docs")
            .SetQueryParam("search", search)
            .SetQueryParam("$top", top)
            .SetQueryParam("$skip", skip)
            .SetQueryParam("$filter", filter)
            .SetQueryParam("$orderBy", orderBy);
    }
}
