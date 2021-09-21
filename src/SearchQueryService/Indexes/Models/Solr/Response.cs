using System.Collections.Generic;

namespace SearchQueryService.Indexes.Models
{
    public class Response
    {
        public int NumFound { get; set; }

        public int Start { get; set; }

        public List<object> Docs { get; set; }
    }
}
