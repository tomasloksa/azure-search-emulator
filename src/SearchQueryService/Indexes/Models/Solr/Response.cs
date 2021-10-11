using System.Collections.Generic;

namespace SearchQueryService.Indexes.Models
{
    public class Response
    {
        /// <summary>
        /// Number of documents found.
        /// </summary>
        public int NumFound { get; set; }

        /// <summary>
        /// How many documents were skipped.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Filtered documents.
        /// </summary>
        public IEnumerable<Dictionary<string, object>> Docs { get; set; }
    }
}
