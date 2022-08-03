using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using System;
using SearchQueryService.Services;
using Polly;
using SearchQueryService.Exceptions;
using SearchQueryService.Indexes.Models.Solr;

namespace SearchQueryService.Indexes
{
    public class IndexesProcessor
    {
        private const int DefaultIndexSize = 4;

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
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

            await _solrService.CheckAndThrowExceptionIfSolrIsNotAvailable();

            foreach (string indexDir in indexDirectories)
            {
                SearchIndex index = await ReadIndexAsync(indexDir);

                if (index == null)
                {
                    _logger.LogInformation($"index.json not found in: \"{indexDir}\", skipping");
                    continue;
                }

                _logger.LogInformation($"====== Creating index: {index.Name}");
                if (!await CanCreateIndex(index))
                {
                    continue;
                }

                var fieldsToAdd = GetFieldsFromIndex(index).ToList();
                var postBody = CreateSchemaPostBody(fieldsToAdd);
                await _solrService.PostSchemaAsync(index.Name, postBody);

                await WaitUntilSchemaCreated(index.Name, fieldsToAdd.Count);

                await PostMockDataAsync(indexDir, index.Name);
            }

            _logger.LogInformation("Index creation finished");
        }

        private async Task<bool> CanCreateIndex(SearchIndex index)
        {
            int schemaSize = await _solrService.GetSchemaSizeAsync(index.Name);
            switch (schemaSize)
            {
                case < 0:
                    LogCoreDoesNotExist(index);
                    return false;
                case > DefaultIndexSize + 1:
                    LogIndexAlreadyExist();
                    return false;
            }

            return true;
        }

        private async Task WaitUntilSchemaCreated(string indexName, int fieldCount)
        {
            bool isSchemaCorrect = await Policy
                .HandleResult<bool>(isSchemaCorrect => !isSchemaCorrect)
                .WaitAndRetryAsync(10, retryAttempt =>
                {
                    _logger.LogWarning($"====== Waiting until schema is created. Attempt: {retryAttempt}");
                    return TimeSpan.FromMilliseconds(500);
                })
                .ExecuteAsync(async () => await IsSchemaCorrectSize(indexName, fieldCount));

            if (!isSchemaCorrect)
            {
                throw new SchemaNotCreatedException(indexName);
            }
        }

        private async Task<bool> IsSchemaCorrectSize(string indexName, int fieldCount)
            => await _solrService.GetSchemaSizeAsync(indexName) - DefaultIndexSize >= fieldCount;

        private static Dictionary<string, IEnumerable<object>> CreateSchemaPostBody(IEnumerable<AddField> fieldsToAdd) =>
            new()
            {
                { "replace-field",
                    new[]
                    {
                        new AddField
                        {
                            Name = "_text_",
                            Type = "strings",
                            Indexed = true,
                            Searchable = true,
                            Stored = false
                        }
                    }
                },
                { "add-field", fieldsToAdd },
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
                    new[]
                    {
                        new AddField
                        {
                            Name = "*",
                            Type = "text_general",
                            Indexed = false,
                            Stored = false,
                            UseDocValuesAsStored = false
                        }
                    }
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

        private async Task PostMockDataAsync(string dataDir, string indexName)
        {
            string dataFile = $"{dataDir}/mockData.json";
            if (!File.Exists(dataFile))
            {
                return;
            }

            using StreamReader r = new($"{dataDir}/mockData.json");
            List<ExpandoObject> documents = JsonSerializer.Deserialize<List<ExpandoObject>>(await r.ReadToEndAsync(), _jsonOptions);

            FixIdCapitalization(documents);

            await _solrService.AddDocumentsAsync(documents, indexName);
        }

        private static void FixIdCapitalization(List<ExpandoObject> documents)
        {
            foreach (var value in documents)
            {
                var map = (IDictionary<string, object>)value;
                if (map.ContainsKey("Id"))
                {
                    map["id"] = map["Id"];
                    map.Remove("Id");
                }
            }
        }

        private static async Task<SearchIndex> ReadIndexAsync(string indexDir)
        {
            string file = $"{indexDir}/index.json";
            if (!File.Exists(file))
            {
                return null;
            }

            using StreamReader r = new(file);
            string json = await r.ReadToEndAsync();
            return JsonSerializer.Deserialize<SearchIndex>(json, _jsonOptions);
        }

        private void LogCoreDoesNotExist(SearchIndex index) =>
            _logger.LogError(@$"====== Solr doesn't contain a definition of core for index `{index.Name}`.
Call `precreate-core {index.Name};` in entry point definition of your Solr docker image in docker-compose file.");

        private void LogIndexAlreadyExist()
            => _logger.LogInformation("====== Indexes already created, continues to the next index");
    }
}
