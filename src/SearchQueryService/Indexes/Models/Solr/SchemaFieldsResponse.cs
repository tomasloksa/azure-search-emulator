using System.Collections.Generic;

namespace SearchQueryService.Indexes.Models
{
    public class SchemaFieldsResponse
    {
        public ResponseHeader ResponseHeader { get; set; }

        public List<object> Fields { get; set; }
    }
}
