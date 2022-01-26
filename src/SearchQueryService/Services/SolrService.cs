using Flurl;
using SearchQueryService.Helpers;
using SearchQueryService.Indexes.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SearchQueryService.Services
{
    internal class SolrService
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _baseUrl = Tools.GetSearchUrl();

        public SolrService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task PostDocumentAsync(StringContent content, string indexName)
        {
            Url uri = _baseUrl
                .AppendPathSegments(indexName, "update", "json", "docs")
                .SetQueryParam("commit", "true");

            HttpResponseMessage response = await _httpClient.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task PostSchemaAsync(string indexName, Dictionary<string, IEnumerable<ISolrField>> schema)
        {
            Url url = _baseUrl.AppendPathSegments(indexName, "schema");
            await _httpClient.PostAsJsonAsync(url, schema,
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
}
