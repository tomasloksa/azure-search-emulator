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
using Microsoft.Extensions.Logging;
using System;
using Polly;

namespace SearchQueryService.Indexes
{
    public class IndexesProcessor
    {
        private const int DefaultIndexSize = 4;

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public IndexesProcessor(
            IHttpClientFactory httpClientFactory,
            ILogger<IndexesProcessor> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task ProcessDirectory()
        {
            var indexDirectories = Directory.GetDirectories("../srv/data");
            _logger.LogInformation("Starting index creation process..");
            _logger.LogInformation("Creating " + indexDirectories.Length + " indexes.");
            foreach (string indexDir in indexDirectories)
            {
                SearchIndex index = ReadIndex(indexDir);

                if (index == null)
                {
                    _logger.LogInformation("index.json not found in: \"" + indexDir + "\", skipping.");
                    continue;
                }

                _logger.LogInformation("Creating index: " + index.Name);

                if (await IsSchemaCorrectSize(index.Name, DefaultIndexSize + 1))
                {
                    _logger.LogInformation("Indexes already created, aborting.");
                    return;
                }

                var fieldsToAdd = GetFieldsFromIndex(index).ToList();
                var postBody = CreateSchemaPostBody(fieldsToAdd);
                CreateCoreSchema(postBody, index.Name);

                await WaitUntilSchemaCreated(3, 500, index.Name, fieldsToAdd.Count);

                PostMockData(indexDir, index.Name);
            }

            _logger.LogInformation("Index creation finished.");
        }

        private async Task<int> GetSchemaSize(string indexName)
        {
            var url = Tools.GetSearchUrl().AppendPathSegments(indexName, "schema", "fields");
            var response = await Policy
                .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(() => _httpClient.GetAsync(url));
            var result = JsonConvert.DeserializeObject<SchemaFieldsResponse>(await response.Content.ReadAsStringAsync());

            return result.Fields.Count;
        }

        private async Task WaitUntilSchemaCreated(int tryCount, int sleepPeriod, string indexName, int fieldCount)
        {
            for (int i = 0; i < tryCount; i++)
            {
                Thread.Sleep(sleepPeriod);
                if (await IsSchemaCorrectSize(indexName, fieldCount))
                {
                    return;
                }

                sleepPeriod *= 2;
            }

            throw new SchemaNotCreatedException();
        }

        private async Task<bool> IsSchemaCorrectSize(string indexName, int fieldCount)
            => await GetSchemaSize(indexName) - DefaultIndexSize >= fieldCount;

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
            string file = $"{indexDir}/index.json";
            if (File.Exists(file))
            {
                using (StreamReader r = new(file))
                {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<SearchIndex>(json);
                }
            }

            return null;
        }
    }
}
