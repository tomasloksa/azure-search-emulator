using Newtonsoft.Json;
using SearchQueryService.Indexes.InvoicingAzureSearch;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using System;

namespace SearchQueryService.Indexes
{
    public class Indexes
    {
        public const string SearchUri = "http://solr:8983/solr/";
        private readonly HttpClient _httpClient;

        public Indexes(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task CreateIndex(string indexDir)
        {
            SearchIndex index;
            using (StreamReader r = new($"{indexDir}/index.json"))
            {
                string json = r.ReadToEnd();
                index = JsonConvert.DeserializeObject<SearchIndex>(json);
            }

            UriBuilder builder = new(SearchUri + index.Name + "/query");
            builder.Query = "q=*:*";
            var docsResponse = _httpClient.GetAsync(builder.Uri).Result;
            var docsResult = docsResponse.Content.ReadAsStringAsync().Result;
            var finalResult = JsonConvert.DeserializeObject<SolrSearchResponse>(docsResult);

            if (finalResult.Response.NumFound != 0)
                return;

            var a = index.Fields.Select(field => SolrAddField.Create(field.Name, field));

            var b = index.Fields.Where(field => field.Fields is not null).SelectMany(field => field.Fields
                .Select(nestedField => SolrAddField.Create(field.Name + "." + nestedField.Name, nestedField)));

            var add = a.Concat(b);

            var postBody = new Dictionary<string, IEnumerable<ISolrField>> {
                {
                    "add-field",
                    add
                },
                {   //copy-field enables field-less search requests
                    "add-copy-field",
                    add.Where(item => item.Indexed).Select(item => new SolrAddCopyField
                    {
                        Source = item.Name,
                        Dest = "_text_"
                    })
                }
            };

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            string postJson = JsonConvert.SerializeObject(postBody, serializerSettings);
            StringContent data = new(postJson, Encoding.UTF8, "application/json");
            var indexUrl = $"{SearchUri}{index.Name}/schema";

            var indexResponse = await _httpClient.PostAsync(indexUrl, data).ConfigureAwait(false);
            var output = await indexResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            using (StreamReader r = new($"{indexDir}/mockData.json"))
            {
                string json = r.ReadToEnd();
                data = new StringContent(json, Encoding.UTF8, "application/json");
                indexResponse = await _httpClient.PostAsync($"{SearchUri}{index.Name}/update/json/docs?commit=true", data).ConfigureAwait(false);
                output = await indexResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            return;
        }
    }
}
