using System.Collections.Generic;

namespace SearchQueryService.Indexes.Models
{
    public class Response
    {
        public int NumFound { get; set; }

        public int Start { get; set; }

        public IEnumerable<Dictionary<string, object>> Docs { get; set; }
    }
}
