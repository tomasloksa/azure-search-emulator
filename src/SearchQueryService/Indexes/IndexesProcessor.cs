using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using System;
using SearchQueryService.Indexes.Models;
using SearchQueryService.Config;
using Microsoft.Extensions.Options;
using System.Threading;
using SearchQueryService.Exceptions;

namespace SearchQueryService.Indexes
{
    public class IndexesProcessor
    {
        private readonly HttpClient _httpClient;
        private readonly ConnectionStringOptions _connectionStrings;

        public IndexesProcessor(
            IHttpClientFactory httpClientFactory,
            IOptions<ConnectionStringOptions> configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _connectionStrings = configuration.Value;
        }

        public async Task ProcessDirectory()
        {
            foreach (string indexDir in Directory.GetDirectories("../srv/demo"))
            {
                SearchIndex index = ReadIndex(indexDir);

                if (await IsCorePopulated(index.Name))
                {
                    return;
                }

                var fieldsToAdd = GetFieldsFromIndex(index);
                var postBody = CreateSchemaPostBody(fieldsToAdd);
                CreateCoreSchema(postBody, index.Name);

                await WaitUntilSchemaCreated(4, 500, fieldsToAdd.Count(), index.Name);

                PostMockData(indexDir, index.Name);
            }
        }

        private async Task WaitUntilSchemaCreated(int tryCount, int sleepPeriod, int fieldCount, string indexName)
        {
            for (int i = 0; i < tryCount; i++)
            {
                Thread.Sleep(sleepPeriod);
                if (await GetSchemaSize(indexName) - 4 >= fieldCount)
                    return;

                sleepPeriod *= 2;
            }

            throw new SchemaNotCreatedException();
        }

        private async Task<int> GetSchemaSize(string indexName)
        {
            string url = _connectionStrings["Solr"] + indexName + "/schema/fields";
            var response = _httpClient.GetAsync(url).Result;
            var result = JsonConvert.DeserializeObject<SchemaFieldsResponse>(await response.Content.ReadAsStringAsync());

            return result.Fields.Count;
        }

        private async void CreateCoreSchema(Dictionary<string, IEnumerable<ISolrField>> postBody, string indexName) {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            string postJson = JsonConvert.SerializeObject(postBody, serializerSettings);
            StringContent data = new(postJson, Encoding.UTF8, "application/json");
            var indexUrl = _connectionStrings["Solr"] + indexName + "/schema";

            await _httpClient.PostAsync(indexUrl, data);
        }

        private static Dictionary<string, IEnumerable<ISolrField>> CreateSchemaPostBody(IEnumerable<AddField> fieldsToAdd) =>
            new()
            {
                {
                    "add-field",
                    fieldsToAdd
                },
                {
                    "add-copy-field",
                    fieldsToAdd.Where(item => item.Searchable).Select(item => new AddCopyField
                    {
                        Source = item.Name,
                        Dest = "_text_"
                    })
                },
                {
                    "add-dynamic-field",
                    new[] { new AddField
                    {
                        Name = "*",
                        Type = "text_general",
                        MultiValued = true,
                        Indexed = false,
                        Stored = false,
                        UseDocValuesAsStored = false
                    }}
                }
            };

        private static IEnumerable<AddField> GetFieldsFromIndex(SearchIndex index) {
            var fields = index.Fields.Select(field => AddField.Create(field.Name, field));

            var nestedFields = index.Fields
                .Where(field => field.Fields is not null)
                .SelectMany(field => field.Fields
                .Select(nestedField => AddField.Create(field.Name + "." + nestedField.Name, nestedField)));

            return fields.Concat(nestedFields);
        }

        private async Task<bool> IsCorePopulated(string indexName)
        {
            UriBuilder builder = new(_connectionStrings["Solr"] + indexName + "/query");
            builder.Query = "q=*:*";
            var docsResponse = await _httpClient.GetAsync(builder.Uri);
            var docsResult = await docsResponse.Content.ReadAsStringAsync();
            var finalResult = JsonConvert.DeserializeObject<SearchResponse>(docsResult);

            return finalResult.Response.NumFound != 0;
        }

        private async void PostMockData(string dataDir, string indexName)
        {
            string dataFile = $"{dataDir}/mockData.json";
            if (File.Exists(dataFile))
            {
                using (StreamReader r = new($"{dataDir}/mockData.json"))
                {
                    string json = r.ReadToEnd();
                    var data = new StringContent(json, Encoding.UTF8, "application/json");
                    var result = await _httpClient.PostAsync($"{_connectionStrings["Solr"]}{indexName}/update/json/docs?commit=true", data);
                    var readable = result.Content.ReadAsStringAsync();
                }
            }
        }

        private static SearchIndex ReadIndex(string indexDir)
        {
            using (StreamReader r = new($"{indexDir}/index.json"))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<SearchIndex>(json);
            }
        }
    }
}
