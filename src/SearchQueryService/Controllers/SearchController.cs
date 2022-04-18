using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SearchQueryService.Services;
using SearchQueryService.Documents.Models.Azure;
using SearchQueryService.Indexes.Models.Solr;
using SearchQueryService.Documents.Models.Solr;

namespace SearchQueryService.Controllers
{
    [ApiController]
    [Route("indexes('{indexName}')/docs")]
    public class SearchController : ControllerBase
    {
        private const string SearchAction = "@search.action";
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
            [FromQuery] string searchMode,
            [FromQuery] string searchFields,
            [FromQuery(Name = "$filter")] string filter,
            [FromQuery(Name = "$orderby")] string orderBy
        )
        {
            var searchParams = new AzSearchParams
            {
                Top = top,
                Skip = skip,
                Search = search,
                Filter = filter,
                OrderBy = orderBy,
                SearchMode = searchMode,
                SearchFields = searchFields
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
                _logger.LogInformation("Documents indexed successfully.");
                return CreateAzSearchResponse(newDocs);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError("Document indexing failed!", exception.Message);
                throw;
            }
        }

        private async Task<AzSearchResponse> Search(string indexName, AzSearchParams searchParams)
        {
            SearchResponse searchResult = await _solrService.SearchAsync(indexName, searchParams);

            FixIdCapitalization(searchResult);

            return new AzSearchResponse(searchResult.Response);
        }

        private static void FixIdCapitalization(SearchResponse searchResult) {
            var docs = searchResult.Response.Docs.ToList();
            if (docs.Any() && (char.IsUpper(docs[0].ElementAt(0).Key[0]) || char.IsUpper(docs[0].ElementAt(1).Key[0])))
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
                    key = doc.ContainsKey("Id")? doc["Id"] : doc["id"],
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
        {
            var delete = new AzPost { Value = new List<Dictionary<string, JsonElement>>() };
            var addOrUpdate = new AzPost { Value = new List<Dictionary<string, JsonElement>>() };

            foreach (var doc in docs.Value)
            {
                if (doc[SearchAction].GetString() == "delete")
                {
                    delete.Value.Add(doc);
                }
                else
                {
                    addOrUpdate.Value.Add(doc);
                }
            }

            var exceptions = new List<Exception>();
            if (delete.Value.Count > 0)
            {
                try
                {
                    await _solrService.DeleteDocumentsAsync(ConvertAzDocsForDelete(delete), indexName);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogError("Document Deletion failed!", exception.Message);
                    exceptions.Add(exception);
                }
            }

            if (addOrUpdate.Value.Count > 0)
            {
                try
                {
                    await _solrService.PostDocumentsAsync(ConvertAzDocs(addOrUpdate), indexName);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogError("Document Post failed!", exception.Message);
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Document operations failed", exceptions);
            }
        }

        private IEnumerable<SolrDelete> ConvertAzDocsForDelete(AzPost azDocs)
        {
            var parsedDocs = new List<SolrDelete>();
            foreach (var doc in azDocs.Value)
            {
                try
                {
                    string id = doc.First(i => i.Key.ToLower() == "id").Value.GetString();
                    var convertedDocument = new SolrDelete { Id = id };
                    parsedDocs.Add(convertedDocument);
                }
                catch (InvalidOperationException exception)
                {
                    _logger.LogError("Document does not contain an Id and will not be deleted.", exception);
                    throw;
                }
            }

            return parsedDocs;
        }

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
