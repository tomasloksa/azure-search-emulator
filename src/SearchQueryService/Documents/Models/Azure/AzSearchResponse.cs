using SearchQueryService.Indexes.Models.Solr;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SearchQueryService.Documents.Models.Azure
{
    /// <summary>
    /// Response format required by Azure.Search.Documents.
    /// </summary>
    public class AzSearchResponse
    {
        [JsonPropertyName("@odata.count")]
        public int Count { get; set; }

        public List<Dictionary<string, dynamic>> Value { get; set; }

        public AzSearchResponse() { }

        public AzSearchResponse(Response solrResponse)
        {
            Count = solrResponse.NumFound;
            Value = new List<Dictionary<string, dynamic>>();

            foreach (var solrDoc in solrResponse.Docs)
            {
                solrDoc.Remove("_version_");
                solrDoc.Add("@search.score", 0);
                Value.Add(solrDoc);
            }
        }
    }
}
