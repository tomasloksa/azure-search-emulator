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
using SearchQueryService.Services;
using System.Net.Http.Json;

namespace SearchQueryService.Indexes
{
    internal class IndexesProcessor
    {
        private const int DefaultIndexSize = 4;

        private readonly SolrService _solrService;
        private readonly ILogger _logger;

        public IndexesProcessor(
            SolrService solrService,
            ILogger<IndexesProcessor> logger)
        {
            _solrService = solrService;
            _logger = logger;
        }

        public async Task ProcessDirectory()
        {
            string[] indexDirectories = Directory.GetDirectories("../srv/data");
            _logger.LogInformation("Starting index creation process..");
            _logger.LogInformation($"=== Creating {indexDirectories.Length} indexes");

            foreach (string indexDir in indexDirectories)
            {
                SearchIndex index = await ReadIndex(indexDir);

                if (index == null)
                {
                    _logger.LogInformation($"index.json not found in: \"{indexDir}\", skipping");
                    continue;
                }

                _logger.LogInformation($"====== Creating index: {index.Name}");

                if (await IsSchemaCorrectSize(index.Name, DefaultIndexSize + 1))
                {
                    _logger.LogInformation("====== Indexes already created, continues to the next index");
                    continue;
                }

                var fieldsToAdd = GetFieldsFromIndex(index).ToList();
                var postBody = CreateSchemaPostBody(fieldsToAdd);
                await _solrService.PostSchemaAsync(index.Name, postBody);

                await WaitUntilSchemaCreated(index.Name, fieldsToAdd.Count);

                PostMockData(indexDir, index.Name);
            }

            _logger.LogInformation("Index creation finished");
        }

        private async Task<int> GetSchemaSize(string indexName)
        {
            SchemaFieldsResponse response = await _httpClient
                .GetFromJsonAsync<SchemaFieldsResponse>(Url.Combine(indexName, "schema", "fields"));

            return response!.Fields.Count;
        }

        private async Task WaitUntilSchemaCreated(string indexName, int fieldCount)
            => await IsSchemaCorrectSize(indexName, fieldCount);

        private async Task<bool> IsSchemaCorrectSize(string indexName, int fieldCount)
            => await GetSchemaSize(indexName) - DefaultIndexSize >= fieldCount;

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

        private static IEnumerable<AddField> GetFieldsFromIndex(SearchIndex index)
        {
            var fields = index.Fields
                .Where(field => !string.Equals(field.Name, "id", StringComparison.OrdinalIgnoreCase))
                .Select(field => AddField.Create(field.Name, field));

            var nestedFields = index.Fields
                .Where(field => field.Fields is not null)
                .SelectMany(field => field.Fields
                .Select(nestedField =>
                    AddField.Create(field.Name + "." + nestedField.Name, nestedField)));

            return fields.Concat(nestedFields);
        }

        private async Task PostMockData(string dataDir, string indexName)
        {
            string dataFile = $"{dataDir}/mockData.json";
            if (!File.Exists(dataFile))
            {
                return;
            }

            using (StreamReader r = new($"{dataDir}/mockData.json"))
            {
                var deserialized = JsonConvert.DeserializeObject<List<ExpandoObject>>(await r.ReadToEndAsync());
                foreach (var value in deserialized)
                {
                    var map = (IDictionary<string, object>)value;
                    if (map.ContainsKey("Id"))
                    {
                        map["id"] = map["Id"];
                        map.Remove("Id");
                    }
                }
                var serialized = JsonConvert.SerializeObject(deserialized);

                using (var content = new StringContent(serialized, Encoding.UTF8, "application/json"))
                {
                    await _solrService.PostDocumentAsync(content, indexName);
                }
            }
        }

        private static async Task<SearchIndex> ReadIndex(string indexDir)
        {
            string file = $"{indexDir}/index.json";
            if (File.Exists(file))
            {
                using (StreamReader r = new(file))
                {
                    string json = await r.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<SearchIndex>(json);
                }
            }

            return null;
        }
    }
}
