using Flurl;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SearchQueryService.Documents.Models;
using SearchQueryService.Indexes.Models;
using SearchQueryService.Helpers;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SearchQueryService.Services;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes('{indexName}')/docs")]
    public class SearchController : ControllerBase
    {
        private readonly Dictionary<string, string> _valueReplacements = new()
        {
            { @"\+[0-9]{2}:[0-9]{2}", "Z" } // Date format
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public SearchController(
            IHttpClientFactory httpClientFactory,
            ILogger<SearchController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpGet]
        public Task<AzSearchResponse> SearchGetAsync(
            [FromRoute] string indexName,
            [FromQuery(Name = "$top")] int? top,
            [FromQuery(Name = "$skip")] int? skip,
            [FromQuery] string search,
            [FromQuery(Name = "$filter")] string filter,
            //string searchMode = "", TODO find out how to set
            [FromQuery(Name = "$orderby")] string orderBy,
            [FromServices] ISearchQueryBuilder searchQueryBuilder
        )
        {
            var searchParams = new AzSearchParams
            {
                Top = top,
                Skip = skip,
                Search = search,
                Filter = filter,
                OrderBy = orderBy
            };

            return Search(indexName, searchParams, searchQueryBuilder);
        }

        [HttpPost("search.post.search")]
        public Task<AzSearchResponse> SearchPost(
            [FromRoute] string indexName,
            [FromBody] AzSearchParams searchParams,
            [FromServices] ISearchQueryBuilder searchQueryBuilder
        ) => Search(indexName, searchParams, searchQueryBuilder);

        [HttpPost("search.index")]
        public object Post(
            [FromRoute] string indexName,
            [FromBody] AzPost newDocs
        )
        {
            try
            {
                PostDocuments(indexName, newDocs);
                _logger.LogInformation("Documents indexed succesfully.");
                return CreateAzSearchResponse(newDocs);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError("Document indexing failed!", exception.Message);
                return exception;
            }
        }

        private static object CreateAzSearchResponse(AzPost newDocs)
        {
            var list = new List<object>();
            foreach (var doc in newDocs.Value)
            {
                list.Add(new
                {
                    key = doc["Id"],
                    status = true,
                    errorMessage = "",
                    StatusCode = 201
                });
            }

            return new
            {
                value = list
            };
        }

        private async void PostDocuments(string indexName, AzPost docs)
        {
            var uri = Tools.GetSearchUrl()
                        .AppendPathSegments(indexName, "update", "json", "docs")
                        .SetQueryParam("commit", "true");

            var options = new JsonSerializerOptions
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(ConvertAzDocs(docs), options));

            var response = await _httpClient.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<AzSearchResponse> Search(
            string indexName,
            AzSearchParams searchParams,
            ISearchQueryBuilder searchQueryBuilder)
        {
            var searchResponse = await _httpClient.GetAsync(searchQueryBuilder.Build(indexName, searchParams));
            var responseContent = await searchResponse.Content.ReadAsStringAsync();
            var searchResult = JsonConvert.DeserializeObject<SearchResponse>(responseContent);

            return new AzSearchResponse(searchResult.Response);
        }

        private List<Dictionary<string, object>> ConvertAzDocs(AzPost azDocs)
        {
            var newList = new List<Dictionary<string, object>>();
            foreach (var doc in azDocs.Value)
            {
                newList.Add(ConvertDocument(doc));
            }

            return newList;
        }

        private Dictionary<string, object> ConvertDocument(Dictionary<string, JsonElement> document)
        {
            var convertedDocument = new Dictionary<string, object>();
            foreach (var kv in document)
            {
                if (string.Equals(kv.Key, "id", System.StringComparison.CurrentCultureIgnoreCase))
                {
                    convertedDocument["id"] = kv.Value;
                }
                else
                {
                    convertedDocument[kv.Key] = new Dictionary<string, dynamic>
                    {
                        { "set", ConvertValues(kv.Value) }
                    };
                }
            }

            return convertedDocument;
        }

        private dynamic ConvertValues(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                string newVal = value.GetString();
                foreach (var kv in _valueReplacements)
                {
                    newVal = Regex.Replace(newVal, kv.Key, kv.Value);
                }
                return newVal;
            }

            return value;
        }
    }
}
