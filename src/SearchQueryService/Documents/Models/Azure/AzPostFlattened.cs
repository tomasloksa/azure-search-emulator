using System.Collections.Generic;
using System.Text.Json;

namespace SearchQueryService.Documents.Models.Azure
{
    /// <summary>
    /// Azure.Search.Documents sends Documents for indexing in this format.
    /// </summary>
    public class AzPostFlattened
    {
        public IEnumerable<IDictionary<string, List<JsonElement>>> Value { get; set; }
    }
}
