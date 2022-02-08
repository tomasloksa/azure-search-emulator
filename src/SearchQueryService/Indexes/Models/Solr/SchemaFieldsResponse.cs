using System.Collections.Generic;

namespace SearchQueryService.Indexes.Models.Solr
{
    /// <summary>
    /// Response recieved from Solr with index structure.
    /// </summary>
    public class SchemaFieldsResponse
    {
        public ResponseHeader ResponseHeader { get; set; }

        public List<object> Fields { get; set; }
    }
}
