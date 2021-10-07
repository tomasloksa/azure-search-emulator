using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SearchQueryService.Indexes.Models
{
    public class AzDocument
    {
        [JsonPropertyName("@search.score")]
        public double Score { get; set; }

        public Dictionary<string, dynamic> Fields { get; set; }

        public AzDocument(Dictionary<string, object> docFound)
        {
            Score = 0;
            docFound.Remove("_version_");
            Fields = docFound;
        }
    }
}
