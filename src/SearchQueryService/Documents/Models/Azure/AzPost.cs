using System.Collections.Generic;

namespace SearchQueryService.Documents.Models
{
    /// <summary>
    /// Azure.Search.Documents sends Documents for indexing in this format.
    /// </summary>
    public class AzPost
    {
        public List<Dictionary<string, dynamic>> Value { get; set; }
    }
}
