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
using System.IO.Compression;
using Microsoft.Extensions.Options;
using SearchQueryService.Config;

namespace SearchQueryService.Indexes
{
    public class IndexesProcessor
    {
        private const int DefaultIndexSize = 4;
        private const string IndexesZipFileName = "Indexes.zip";

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly SolrService _solrService;
        private readonly SchemaMemory _schemaMemory;
        private readonly IOptions<IndexesProcessorOptions> _indexersOptions;
        private readonly ILogger _logger;

        public IndexesProcessor(
            SolrService solrService,
            SchemaMemory schemaMemory,
            IOptions<IndexesProcessorOptions> indexersOptions,
            ILogger<IndexesProcessor> logger)
        {
            _solrService = solrService;
            _schemaMemory = schemaMemory;
            _indexersOptions = indexersOptions;
            _logger = logger;
        }

        public async Task ProcessDirectory()
        {
            const string dir = "../srv/data";
            WaitForIndexesZipFile(dir);
            CheckIfZipExist(dir);

            string[] indexDirectories = Directory.GetDirectories(dir);
            _logger.LogInformation("Starting index creation process..");
            _logger.LogInformation("=== Creating {indexDirectoriesLength} indexes.", indexDirectories.Length);

            await _solrService.CheckAndThrowExceptionIfSolrIsNotAvailable();

            foreach (string indexDir in indexDirectories)
            {
                SearchIndex index = await ReadIndexAsync(indexDir);

                if (index == null)
                {
                    _logger.LogInformation("index.json not found in: \"{indexDir}\", skipping", indexDir);
                    continue;
                }

                _logger.LogInformation("====== Creating index: {indexName}", index.Name);

                var fieldsToAdd = GetFieldsFromIndex(index).ToList();

                if (!await CanCreateIndex(index))
                {
                    continue;
                }

                var postBody = CreateSchemaPostBody(fieldsToAdd);
                await _solrService.PostSchemaAsync(index.Name, postBody);

                await WaitUntilSchemaCreated(index.Name, fieldsToAdd.Count);

                await PostMockDataAsync(indexDir, index.Name);
            }

            _logger.LogInformation("Index creation finished");
        }

        private void WaitForIndexesZipFile(string indexDir)
        {
            if (!_indexersOptions.Value.WaitForIndexesZip)
            {
                return;
            }

            bool isFilesExist = Policy
                .HandleResult<bool>(isFilesExist => !isFilesExist)
                .WaitAndRetry(_indexersOptions.Value.WaitForFilesRetryCount, retryAttempt =>
                {
                    _logger.LogWarning("====== Waiting until files exist. Attempt: {retryAttempt}", retryAttempt);
                    return TimeSpan.FromSeconds(2);
                })
                .Execute(() => FilesExist(indexDir));

            if (!isFilesExist)
            {
                throw new InvalidOperationException($"Files not found in: {indexDir}");
            }
        }

        private static bool FilesExist(string indexDir)
            => File.Exists(Path.Combine(indexDir, IndexesZipFileName));

        private static void CheckIfZipExist(string dir)
        {
            string zipFileName = Path.Combine(dir, IndexesZipFileName);
            if (File.Exists(zipFileName))
            {
                ZipFile.ExtractToDirectory(zipFileName, dir, true);
            }
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

        private IEnumerable<AddField> GetFieldsFromIndex(SearchIndex index)
        {
            var rootNestedFields = index.Fields.Where(field => field.Fields is not null);

            foreach (var field in rootNestedFields)
            {
                if (field.Fields.Any(f => f.Retrievable))
                {
                    _schemaMemory.AddNestedItemToIndex(index.Name, field);
                    field.Retrievable = true;
                }
            }

            var fields = index.Fields
                .Where(field => !string.Equals(field.Name, "id", StringComparison.OrdinalIgnoreCase))
                .Select(field => AddField.Create(field.Name, field));

            return fields.Concat(rootNestedFields.SelectMany(field =>
                field.Fields.Select(nestedField =>
                    AddField.Create(field.Name + "." + nestedField.Name, nestedField))));
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
