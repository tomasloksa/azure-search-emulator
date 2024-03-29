﻿using System;
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
using SearchQueryService.Documents.Models.Solr;
using SearchQueryService.Helpers;
using SearchQueryService.Indexes;
using SearchQueryService.Indexes.Models.Solr;
using Microsoft.Extensions.Options;
using SearchQueryService.Config;

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
        private readonly SchemaMemory _schemaMemory;
        private readonly DocumentOptions _documentOptions;

        public SearchController(
            ILogger<SearchController> logger,
            IOptions<DocumentOptions> documentOptions,
            SchemaMemory schemaMemory,
            SolrService solrService)
        {
            _logger = logger;
            _documentOptions = documentOptions?.Value;
            _solrService = solrService;
            _schemaMemory = schemaMemory;
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
                _logger.LogError(exception, "Document indexing failed!");
                throw;
            }
        }

        private async Task<AzSearchResponse> Search(string indexName, AzSearchParams searchParams)
        {
            SearchResponse searchResult = await _solrService.SearchAsync(indexName, searchParams);

            searchResult.Response.Docs = searchResult.Response.Docs.Select(Tools.JsonUnflatten);

            FixIdCapitalization(searchResult);
            AddEmptyNestedFields(indexName, searchResult);

            return new AzSearchResponse(searchResult.Response);
        }

        private void AddEmptyNestedFields(string indexName, SearchResponse searchResult)
        {
            var nestedItemsSchema = _schemaMemory.GetNestedItemsOrDefault(indexName);
            if (nestedItemsSchema != default)
            {
                foreach (var retrievedDoc in searchResult.Response.Docs)
                {
                    foreach (var nested in nestedItemsSchema)
                    {
                        if (!retrievedDoc.ContainsKey(nested.Key))
                        {
                            retrievedDoc.Add(nested.Key, new List<object>());
                        }
                    }
                }
            }
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

            searchResult.Response.Docs = docs;
        }

        private static object CreateAzSearchResponse(AzPost newDocs)
        {
            var list = new List<object>();
            
            foreach (var doc in newDocs.Value)
            {
                list.Add(new
                {
                    key = doc.ContainsKey("Id") ? doc["Id"] : doc["id"],
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
            var add = new AzPost { Value = new List<Dictionary<string, JsonElement>>() };
            var addOrUpdate = new AzPost { Value = new List<Dictionary<string, JsonElement>>() };

            foreach (var doc in docs.Value)
            {
                switch (doc[SearchAction].GetString())
                {
                    case "delete":
                        delete.Value.Add(doc);
                        break;

                    case "mergeOrUpload":
                        addOrUpdate.Value.Add(doc);
                        break;

                    default:
                        add.Value.Add(doc);
                        break;
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
                    _logger.LogError(exception, "Document Delete failed!");
                    exceptions.Add(exception);
                }
            }

            if (addOrUpdate.Value.Count > 0)
            {
                try
                {
                    var transformed = addOrUpdate.Value.Select(doc => ConvertAzDocsForAddOrUpdate(doc, indexName));
                    await _solrService.AddOrUpdateDocumentsAsync(transformed, indexName);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogError(exception, "Document AddOrUpdate failed!");
                    exceptions.Add(exception);
                }
            }

            if (add.Value.Count > 0)
            {
                try
                {
                    await _solrService.AddDocumentsAsync(ConvertAzDocsForAdd(add), indexName);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogError(exception, "Document Add failed!");
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

        private Dictionary<string, JsonElement> ConvertAzDocsForAddOrUpdate(Dictionary<string, JsonElement> json, string indexName)
        {
            var converted = new Dictionary<string, JsonElement>();

            foreach (var item in Tools.JsonFlatten(json, _schemaMemory.GetNestedItemsOrDefault(indexName)))
            {
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    converted["id"] = JsonSerializer.SerializeToElement(item.Value[0]);
                }
                else
                {
                    if (!_documentOptions.RemoveNullFieldsOnMerge &&
                        item.Value.Count == 1 && item.Value[0].ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }

                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        item.Value[i] = ConvertValue(item.Value[i]);
                    }
                    converted[item.Key] = JsonSerializer.SerializeToElement(new SolrSetProperty(item.Value));
                }
            }

            return converted;
        }

        private List<Dictionary<string, JsonElement>> ConvertAzDocsForAdd(AzPost azDocs)
        {
            foreach (var doc in azDocs.Value)
            {
                FixPrimaryKeyForAdd(doc);

                foreach (var prop in doc)
                {
                    doc[prop.Key] = ConvertValue(prop.Value);
                }
            }

            return azDocs.Value;
        }

        private static void FixPrimaryKeyForAdd(Dictionary<string, JsonElement> document)
        {
            if (document.ContainsKey("Id"))
            {
                JsonElement id = document["Id"];
                document.Remove("Id");
                document["id"] = id;
            }
        }

        private JsonElement ConvertValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                string newVal = value.GetString();
                foreach (var kv in _valueReplacements)
                {
                    newVal = Regex.Replace(newVal, kv.Key, kv.Value);
                }
                return JsonSerializer.SerializeToElement(newVal);
            }

            return value;
        }
    }
}
