using System.Text.Json.Serialization;

namespace SearchQueryService.Documents.Models.Solr
{
    public class SolrDelete
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
