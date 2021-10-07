using EshopDemo.Api.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Core.Pipeline;
using Azure.Search.Documents.Models;
using System.Collections.Generic;

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
        public ContentResult Get() => Content(
            GetSearchResults(new SearchParams { Search = "*:*", Filter = "not IsDeleted", OrderBy = "", Top = 30, Skip = 0 }).Result.ToString(), "application/json");

        [HttpGet("search")]
        public ContentResult Search(
            [FromQuery] string search,
            [FromQuery] string filter,
            [FromQuery] string orderBy,
            [FromQuery] int? top = null,
            [FromQuery] int? skip = null) => Content(
                GetSearchResults(new SearchParams { Search = search, Filter = filter, OrderBy = orderBy, Top = top, Skip = skip }).Result.ToString(), "application/json");

        [HttpPost]
        public void IndexDocument(
            [FromBody] Dictionary<string, dynamic> document
        )
        {
            var clientOptions = new SearchClientOptions { Transport = new HttpClientTransport(_httpClient) };
            var searchClient = new SearchClient(
                new System.Uri("https://loksa:8000"),
                "invoicingindex",
                new AzureKeyCredential("notNeeded"),
                clientOptions
            );

            var batch = IndexDocumentsBatch.Create(
                IndexDocumentsAction.Upload(document)
            );
            searchClient.IndexDocuments(batch);
        }

        public async Task<object> GetSearchResults(SearchParams searchParams)
        {
            var clientOptions = new SearchClientOptions { Transport = new HttpClientTransport(_httpClient) };
            var searchOptions = new SearchOptions
            {
                Skip = searchParams.Skip,
                Filter = searchParams.Filter,
                Size = searchParams.Top
            };
            searchOptions.OrderBy.Add(searchParams.OrderBy);

            var searchClient = new SearchClient(
                new System.Uri(_connectionStrings["SearchService"]),
                "invoicingindex",
                new AzureKeyCredential("notNeeded"),
                clientOptions
            );
            var searchResponse = await searchClient.SearchAsync<SearchDocument>(searchParams.Search, searchOptions);

            return searchResponse.Value.GetResults();
        }
    }
}
