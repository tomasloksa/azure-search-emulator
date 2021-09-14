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
        private readonly HttpClient _httpClient;
        public Indexes(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateIndex()
        {
            SearchIndex index;
            using (StreamReader r = new StreamReader("Indexes/CatalogAzureSearch/index.json"))
            {
                string json = r.ReadToEnd();
                index = JsonConvert.DeserializeObject<SearchIndex>(json);
            }

            var postBody = new Dictionary<string, IEnumerable<SolrField>> { { "add-field", 
                index.Fields.Select(item => new SolrField
                {
                    Name = item.Name,
                    Type = getSolrType(item.Type),
                    Stored = item.Retrievable
                    // TODO ďalšie parametre
                }) } 
                // TODO add-copy-field https://stackoverflow.com/questions/12833592/solr-query-over-all-fields-best-practice
            };

            string postJson = JsonConvert.SerializeObject(postBody);
            StringContent data = new StringContent(postJson, Encoding.UTF8, "application/json");
            var indexUrl = $"http://localhost:8983/solr/{index.Name}/schema";
            var indexResponse = await _httpClient.PostAsync(indexUrl, data);
            var output = await indexResponse.Content.ReadAsStringAsync();
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
