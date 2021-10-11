namespace SearchQueryService.Indexes.Models
{
    public class SearchIndex
    {
        /// <summary>
        /// Name of index.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Index fields.
        /// </summary>
        public AzField[] Fields { get; set; }
    }
}
