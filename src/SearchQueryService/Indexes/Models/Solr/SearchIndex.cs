namespace SearchQueryService.Indexes.Models
{
    public class SearchIndex
    {
        public string Name { get; set; }
        public AzField[] Fields { get; set; }
    }
}
