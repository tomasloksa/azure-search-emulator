using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchApi.Controllers
{
    [ApiController]
    [Route("")]
    public class EshopItemsController : ControllerBase
    {
        public const string SearchUri = "http://searchqueryservice:80/";
        private readonly HttpClient _httpClient;

        public EshopItemsController(IHttpClientFactory httpClientFactory) => _httpClient = httpClientFactory.CreateClient();

        [HttpGet]
        public async Task<object> Get()
        {
            string uri = SearchUri + "indexes/invoicingindex/docs?$top=30&$skip=0&search=*:*";
            var result = await _httpClient.GetAsync(uri);

            return await result.Content.ReadAsStringAsync();
        }

        [HttpGet("search")]
        public async Task<object> Search(
            int top = 10,
            int skip = 0,
            string search = "*:*")
        {
            string uri = SearchUri + $"indexes/invoicingindex/docs?$top={top}&$skip={skip}&search={search}";
            var result = await _httpClient.GetAsync(uri);

            return await result.Content.ReadAsStringAsync();
        }
    }
}
