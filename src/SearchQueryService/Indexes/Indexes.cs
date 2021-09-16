using Newtonsoft.Json;
using SearchQueryService.Indexes.InvoicingAzureSearch;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace SearchQueryService.Indexes
{
    public class Indexes
    {
        public const string SearchUri = "http://solr:8983/solr/";
        private readonly HttpClient _httpClient;

        public Indexes(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateIndex()
        {
            SearchIndex index;
            using (StreamReader r = new StreamReader("Indexes/InvoicingAzureSearch/index.json"))
            {
                string json = r.ReadToEnd();
                index = JsonConvert.DeserializeObject<SearchIndex>(json);
            }

            var postBody = new Dictionary<string, IEnumerable<SolrField>> { { 
                "add-field", 
                index.Fields.Select(item => new SolrAddField
                {
                    Name = item.Name,
                    Type = getSolrType(item.Type),
                    Stored = item.Retrievable,
                    Indexed = item.Searchable
                }) }
                // TODO add-copy-field https://stackoverflow.com/questions/12833592/solr-query-over-all-fields-best-practice
            };

            //copy-field enables field-less search requests
            postBody.Add(
                "add-copy-field",
                index.Fields.Where(item => item.Searchable).Select(item => new SolrAddCopyField
                {
                    Source = item.Name,
                    Destination = "_text_"
                }));

            string postJson = JsonConvert.SerializeObject(postBody);
            StringContent data = new StringContent(postJson, Encoding.UTF8, "application/json");
            var indexUrl = $"{SearchUri}{index.Name}/schema";
            var indexResponse = await _httpClient.PostAsync(indexUrl, data);
            var output = await indexResponse.Content.ReadAsStringAsync();


            using (StreamReader r = new StreamReader("Indexes/CatalogAzureSearch/mockData.json"))
            {
                string json = r.ReadToEnd();
                data = new StringContent(json, Encoding.UTF8, "application/json");
                indexResponse = await _httpClient.PostAsync($"{SearchUri}{index.Name}/update?commit=true", data);
                output = await indexResponse.Content.ReadAsStringAsync();
            }

            return output;
        }

        public string getSolrType(string azType)
        => azType switch
        {
            "Edm.String" => "text_general",
            "Edm.Int32" => "pint",
            "Edm.Int64" => "plong",
            "Edm.Boolean" => "boolean",
            "Edm.Double" => "double",
            "Edm.DateTimeOffset" => "pdate",
            _ => throw new ArgumentOutOfRangeException($"Not expected index type value: {azType}") // TODO Collection(Edm.ComplexType)
        };
    }
}
