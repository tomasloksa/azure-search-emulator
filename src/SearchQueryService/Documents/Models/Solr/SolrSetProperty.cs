using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchQueryService.Documents.Models.Solr
{
    class SolrSetProperty
    {
        public SolrSetProperty(List<JsonElement> value)
        {
            Values = value;
        }

        [JsonPropertyName("set")]
        public List<JsonElement> Values { get; set; }
    }
}
