using Flurl;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SearchQueryService.Documents.Models;
using SearchQueryService.Indexes.Models;
using SearchQueryService.Helpers;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes('{indexName}')/docs")]
    public class SearchController : ControllerBase
    {
        private static readonly Dictionary<string, string> _replacements = new()
        {
            { @"(\w+)\s+(ge)\s+([^\s]+)", "$1:[$3 TO *]" },
            { @"(\w+)\s+(gt)\s+([^\s]+)", "$1:{$3 TO *}" },
            { @"(\w+)\s+(le)\s+([^\s]+)", "$1:[* TO $3]" },
            { @"(\w+)\s+(lt)\s+([^\s]+)", "$1:{* TO $3}" },
            { @"(\w+)\s+(ne)", "NOT $1:" }
        };

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
            [FromQuery(Name = "$orderby")] string orderBy
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

            return Search(indexName, searchParams);
        }

        [HttpPost("search.post.search")]
        public Task<AzSearchResponse> SearchPost(
            [FromRoute] string indexName,
            [FromBody] AzSearchParams searchParams
        ) => Search(indexName, searchParams);

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

        private async Task<AzSearchResponse> Search(string indexName, AzSearchParams searchParams)
        {
            var searchResponse = await _httpClient.GetAsync(BuildSearchQuery(indexName, searchParams));
            var responseContent = await searchResponse.Content.ReadAsStringAsync();
            var searchResult = JsonConvert.DeserializeObject<SearchResponse>(responseContent);

            return new AzSearchResponse(searchResult.Response);
        }

        private static string BuildSearchQuery(string indexName, AzSearchParams searchParams)
            => Tools.GetSearchUrl()
            .AppendPathSegments(indexName, "select")
            .SetQueryParams(new
            {
                q = searchParams.Search,
                rows = searchParams.Top,
                start = searchParams.Skip,
                fq = string.IsNullOrEmpty(searchParams.Filter) ? searchParams.Filter : ConvertAzQuery(searchParams.Filter),
                sort = searchParams.OrderBy
            });

        private static string ConvertAzQuery(string filter)
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
