using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SearchQueryService.Indexes.Models
{
    public class AzResponse
    {
        [JsonPropertyName("@odata.count")]
        public int Count { get; set; }

        public List<AzDocument> Value { get; set; }

        public AzResponse() { }

        public AzResponse(Response solrResponse)
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
