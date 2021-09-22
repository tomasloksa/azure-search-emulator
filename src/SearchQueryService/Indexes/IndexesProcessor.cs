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

namespace SearchQueryService.Indexes
{
    public class IndexesProcessor
    {
        public const string SearchUri = "http://solr:8983/solr/";
        private readonly HttpClient _httpClient;

        public IndexesProcessor(IHttpClientFactory httpClientFactory) => _httpClient = httpClientFactory.CreateClient();

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

                // TODO find a better solution
                System.Threading.Thread.Sleep(5000);

                PostMockData(indexDir, index.Name);
            }
        }

        private async void CreateCoreSchema(Dictionary<string, IEnumerable<ISolrField>> postBody, string indexName) {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            string postJson = JsonConvert.SerializeObject(postBody, serializerSettings);
            StringContent data = new(postJson, Encoding.UTF8, "application/json");
            var indexUrl = $"{SearchUri}{indexName}/schema";

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
                    fieldsToAdd.Where(item => item.Indexed).Select(item => new AddCopyField
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
            UriBuilder builder = new(SearchUri + indexName + "/query");
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
                    var result = await _httpClient.PostAsync($"{SearchUri}{indexName}/update/json/docs?commit=true", data);
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
