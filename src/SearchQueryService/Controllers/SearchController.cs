using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SearchQueryService.Documents.Models;
using SearchQueryService.Indexes.Models;
using SearchQueryService.Services;

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

        private readonly ILogger _logger;
        private readonly SolrService _solrService;

        public SearchController(
            ILogger<SearchController> logger,
            SolrService solrService)
        {
            _logger = logger;
            _solrService = solrService;
        }

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
            [FromBody] AzSearchParams searchParams)
            => Search(indexName, searchParams);

        [HttpPost("search.index")]
        public async Task<object> Post(
            [FromRoute] string indexName,
            [FromBody] AzPost newDocs
        )
        {
            try
            {
                await PostDocuments(indexName, newDocs);
                _logger.LogInformation("Documents indexed successfully");
                return CreateAzSearchResponse(newDocs);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError("Document indexing failed!", exception.Message);
                throw;
            }
        }

        private async Task<AzSearchResponse> Search(
           string indexName,
           AzSearchParams searchParams)
        {
            SearchResponse searchResult = await _solrService.SearchAsync(indexName, searchParams);

            FixIdCapitalization(searchResult);

            return new AzSearchResponse(searchResult.Response);
        }

        private static void FixIdCapitalization(SearchResponse searchResult) {
            var docs = searchResult.Response.Docs.ToList();
            if (docs.Any() && (char.IsUpper(docs.First().ElementAt(0).Key[0]) || char.IsUpper(docs.First().ElementAt(1).Key[0])))
            {
                foreach (var retrievedDoc in docs)
                {
                    retrievedDoc["Id"] = retrievedDoc["id"];
                    retrievedDoc.Remove("id");
                }
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

        private async Task PostDocuments(string indexName, AzPost docs)
            => await _solrService.PostDocumentAsync(ConvertAzDocs(docs), indexName);

        private List<Dictionary<string, object>> ConvertAzDocs(AzPost azDocs)
            => azDocs.Value.Select(ConvertDocument).ToList();

        private Dictionary<string, object> ConvertDocument(Dictionary<string, JsonElement> document)
        {
            var convertedDocument = new Dictionary<string, object>();
            foreach (var kv in document)
            {
                if (string.Equals(kv.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    convertedDocument["id"] = kv.Value;
                }
                else
                {
                    convertedDocument[kv.Key] = ConvertValues(kv.Value);
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
