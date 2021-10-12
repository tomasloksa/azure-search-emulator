using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using SearchQueryService.Indexes.Models;
using System.Threading;
using SearchQueryService.Exceptions;
using Flurl;
using System.Dynamic;
using SearchQueryService.Helpers;

namespace SearchQueryService.Indexes
{
    public class IndexesProcessor
    {
        private readonly HttpClient _httpClient;

        public IndexesProcessor(IHttpClientFactory httpClientFactory)
            => _httpClient = httpClientFactory.CreateClient();

        public async Task ProcessDirectory()
        {
            foreach (string indexDir in Directory.GetDirectories("../srv/demo"))
            {
                SearchIndex index = ReadIndex(indexDir);

                if (await IsCorePopulated(index.Name))
                {
                    return;
                }

                var fieldsToAdd = GetFieldsFromIndex(index).ToList();
                var postBody = CreateSchemaPostBody(fieldsToAdd);
                CreateCoreSchema(postBody, index.Name);

                await WaitUntilSchemaCreated(4, 500, fieldsToAdd.Count, index.Name);

                PostMockData(indexDir, index.Name);
            }
        }

        private async Task WaitUntilSchemaCreated(int tryCount, int sleepPeriod, int fieldCount, string indexName)
        {
            for (int i = 0; i < tryCount; i++)
            {
                Thread.Sleep(sleepPeriod);
                if (await GetSchemaSize(indexName) - 4 >= fieldCount)
                {
                    return;
                }

                sleepPeriod *= 2;
            }

            throw new SchemaNotCreatedException();
        }

        private async Task<int> GetSchemaSize(string indexName)
        {
            var url = Tools.GetSearchUrl().AppendPathSegments(indexName, "schema", "fields");
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

            using (StringContent data = new(postJson, Encoding.UTF8, "application/json"))
            {
                var indexUrl = Tools.GetSearchUrl().AppendPathSegments(indexName, "schema");
                await _httpClient.PostAsync(indexUrl, data);
            }
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
            var fields = index.Fields
                .Where(field => !string.Equals(field.Name, "id", System.StringComparison.OrdinalIgnoreCase))
                .Select(field => AddField.Create(field.Name.ToCamelCase(), field));

            var nestedFields = index.Fields
                .Where(field => field.Fields is not null)
                .SelectMany(field => field.Fields
                .Select(nestedField =>
                    AddField.Create(field.Name.ToCamelCase() + "." + nestedField.Name.ToCamelCase(), nestedField)));

            return fields.Concat(nestedFields);
        }

        private async Task<bool> IsCorePopulated(string indexName)
        {
            var uri = Tools.GetSearchUrl()
                .AppendPathSegments(indexName, "query")
                .SetQueryParam("q", "*:*");
            var docsResponse = await _httpClient.GetAsync(uri);
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
                    var jsonSerializerSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };

                    var deserialized = JsonConvert.DeserializeObject<List<ExpandoObject>>(r.ReadToEnd());
                    var serialized = JsonConvert.SerializeObject(deserialized, jsonSerializerSettings);

                    using (var content = new StringContent(serialized, Encoding.UTF8, "application/json"))
                    {
                        var uri = Tools.GetSearchUrl()
                                    .AppendPathSegments(indexName, "update", "json", "docs")
                                    .SetQueryParam("commit", "true");

                        await _httpClient.PostAsync(uri, content);
                    }
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
