namespace SearchQueryService.Indexes.Models
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
