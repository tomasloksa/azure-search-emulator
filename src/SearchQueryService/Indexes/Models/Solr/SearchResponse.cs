namespace SearchQueryService.Indexes.Models.Solr
{
    /// <summary>
    /// Response received from Solr Search.
    /// </summary>
    public class SearchResponse
    {
        public ResponseHeader ResponseHeader { get; set; }

        public Response Response { get; set; }
    }
}
