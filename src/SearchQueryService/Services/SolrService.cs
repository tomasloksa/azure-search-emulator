﻿using Flurl;
using SearchQueryService.Documents.Models;
using SearchQueryService.Indexes.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SearchQueryService.Services
{
    public class SolrService
    {
        private readonly HttpClient _httpClient;
        private readonly ISearchQueryBuilder _searchQueryBuilder;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyProperties = true
        };

        public SolrService(
            HttpClient httpClient,
            ISearchQueryBuilder searchQueryBuilder)
        {
            _httpClient = httpClient;
            _searchQueryBuilder = searchQueryBuilder;
        }

        public async Task CheckAndThrowExceptionIfSolrIsNotAvailable()
        {
            using HttpResponseMessage response = await _httpClient.GetAsync("admin/cores?action=STATUS");

            response.EnsureSuccessStatusCode();
        }

        public async Task PostDocumentAsync<TDocument>(TDocument document, string indexName)
        {
            Url uri = indexName
                .AppendPathSegments("update", "json", "docs")
                .SetQueryParam("commit", "true");

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(uri, document, _jsonOptions);
            response.EnsureSuccessStatusCode();
        }

        public async Task PostSchemaAsync(string indexName, Dictionary<string, IEnumerable<object>> schema)
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(GetSchemeUrl(indexName), schema, _jsonOptions);

            response.EnsureSuccessStatusCode();
        }

        public async Task<int> GetSchemaSizeAsync(string indexName)
        {
            using HttpResponseMessage response = await _httpClient
                .GetAsync(GetSchemeUrl(indexName, "fields"));

            if (!response.IsSuccessStatusCode)
            {
                return -1;
            }

            SchemaFieldsResponse schemaResponse = await response.Content!.ReadFromJsonAsync<SchemaFieldsResponse>();

            return schemaResponse!.Fields.Count;
        }

        private static Url GetSchemeUrl(string indexName, string segment = "")
            => Url.Combine(indexName, "schema", segment);

        public async Task<SearchResponse> SearchAsync(string indexName, AzSearchParams searchParams)
            => await _httpClient.GetFromJsonAsync<SearchResponse>(_searchQueryBuilder.Build(indexName, searchParams));
    }
}
