using Flurl;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SearchQueryService.Documents.Models;
using SearchQueryService.Indexes.Models;
using SearchQueryService.Helpers;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SearchQueryService.Services;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes('{indexName}')/docs")]
    public class SearchController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public SearchController(IHttpClientFactory httpClientFactory)
            => _httpClient = httpClientFactory.CreateClient();

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
            PostDocuments(indexName, newDocs);
            return CreateAzSearchResponse(newDocs);
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
                        .AppendPathSegments(indexName, "update", "json")
                        .SetQueryParam("commit", "true");

            var options = new JsonSerializerOptions
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(ConvertAzDocs(docs), options));

            await _httpClient.PostAsync(uri, content);
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

        private static List<Dictionary<string, object>> ConvertAzDocs(AzPost azDocs)
        {
            var newList = new List<Dictionary<string, object>>();
            foreach (var doc in azDocs.Value)
            {
                newList.Add(ConvertDocument(doc));
            }

            return newList;
        }

        private static Dictionary<string, object> ConvertDocument(Dictionary<string, dynamic> document)
        {
            var newDict = new Dictionary<string, object>();
            foreach (var kv in document)
            {
                if (string.Equals(kv.Key, "id", System.StringComparison.CurrentCultureIgnoreCase))
                {
                    newDict["id"] = kv.Value;
                }
                else
                {
                    newDict[kv.Key] = new Dictionary<string, dynamic>
                    {
                        { "set", kv.Value }
                    };
                }
            }

            return newDict;
        }
    }
}
