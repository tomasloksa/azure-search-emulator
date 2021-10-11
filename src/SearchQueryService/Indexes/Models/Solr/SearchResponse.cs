namespace SearchQueryService.Indexes.Models
{
    /// <summary>
    /// Response recieved from Solr Search.
    /// </summary>
    public class SearchResponse
    {
        public ResponseHeader ResponseHeader { get; set; }

        public Response Response { get; set; }
    }
}
