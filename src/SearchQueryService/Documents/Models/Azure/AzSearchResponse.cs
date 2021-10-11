using SearchQueryService.Indexes.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SearchQueryService.Documents.Models
{
    /// <summary>
    /// Response format required by Azure.Search.Documents.
    /// </summary>
    public class AzSearchResponse
    {
        [JsonPropertyName("@odata.count")]
        public int Count { get; set; }

        public List<AzDocument> Value { get; set; }

        public AzSearchResponse() { }

        public AzSearchResponse(Response solrResponse)
        {
            Count = solrResponse.NumFound - solrResponse.Start;
            Value = new List<AzDocument>();

            foreach (var solrDoc in solrResponse.Docs)
            {
                Value.Add(new AzDocument(solrDoc));
            }
        }
    }
}
