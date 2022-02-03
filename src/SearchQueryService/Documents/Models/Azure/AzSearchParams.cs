namespace SearchQueryService.Documents.Models
{
    /// <summary>
    /// Search parameters recieved from Azure.Search.Docs.
    /// </summary>
    public class AzSearchParams
    {
        public string Search { get; set; }

        public string SearchMode { get; set; }

        public int? Skip { get; set; }

        public int? Top { get; set; }

        public string Filter { get; set; }

        public string OrderBy { get; set; }
    }
}
