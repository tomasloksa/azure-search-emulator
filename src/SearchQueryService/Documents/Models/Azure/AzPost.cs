using System.Collections.Generic;
using System.Text.Json;

namespace SearchQueryService.Documents.Models.Azure
{
    /// <summary>
    /// Azure.Search.Documents sends Documents for indexing in this format.
    /// </summary>
    public class AzPost
    {
        public List<Dictionary<string, JsonElement>> Value { get; set; }
    }
}
