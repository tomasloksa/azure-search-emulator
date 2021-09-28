using SearchQueryService.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchApi.Controllers
{
    [ApiController]
    [Route("")]
    public class EshopItemsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ConnectionStringOptions _connectionStrings;

        public EshopItemsController(
            IHttpClientFactory httpClientFactory,
            IOptions<ConnectionStringOptions> configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _connectionStrings = configuration.Value;
        }

        [HttpGet]
        public async Task<ContentResult> Get() => Content(
            await GetSearchResults(BuildSearchQuery(30, 0, "*:*", "")), "application/json" );

        [HttpGet("search")]
        public async Task<ContentResult> Search(
            [FromQuery] string search,
            [FromQuery] string filter,
            [FromQuery] string orderBy,
            [FromQuery] int? top = null,
            [FromQuery] int? skip = null) => Content(
                await GetSearchResults(BuildSearchQuery(top, skip, search, orderBy)), "application/json");

        public async Task<string> GetSearchResults(string uri)
        {
            var response = await _httpClient.GetAsync(uri);

            Response.StatusCode = (int)response.StatusCode;
            string text = await response.Content.ReadAsStringAsync();
            return text;
        }

        private string BuildSearchQuery(int? top, int? skip, string search, string orderBy)
        {
            string uri = _connectionStrings["SearchService"] + $"indexes/invoicingindex/docs?search={search}";

            if (top is not null)
            {
                uri += "&$top=" + top;
            }

            if (skip is not null)
            {
                uri += "&$skip=" + skip;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                uri += "&$orderby=" + orderBy;
            }

            return uri;
        }
    }
}
