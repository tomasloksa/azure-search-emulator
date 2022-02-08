namespace SearchQueryService.Indexes.Models.Solr
{
    public class AddCopyField : ISolrField
    {
        /// <summary>
        /// Source Field, from which the value is copied.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Destination Field, where the value is copied.
        /// </summary>
        public string Dest { get; set; }
    }
}
