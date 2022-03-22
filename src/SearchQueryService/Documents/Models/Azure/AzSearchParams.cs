namespace SearchQueryService.Documents.Models.Azure
{
    /// <summary>
    /// Search parameters received from Azure.Search.Docs.
    /// </summary>
    public class AzSearchParams
    {
        public string Search { get; set; }

        public string SearchMode { get; set; }
        public string SearchFields { get; set; }

        public int? Skip { get; set; }

        public int? Top { get; set; }

        public string Filter { get; set; }

        public string OrderBy { get; set; }
    }
}
