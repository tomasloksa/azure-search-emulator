namespace SearchQueryService.Indexes.Models.Solr
{
    public class ResponseHeader
    {
        /// <summary>
        /// Request status.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Query time.
        /// </summary>
        public int QTime { get; set; }
    }
}
