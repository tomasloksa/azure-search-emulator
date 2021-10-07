namespace SearchQueryService.Indexes.Models
{
    public class AzSearchParams
    {
        public string Search { get; set; }

        public int? Skip { get; set; }

        public int? Top { get; set; }

        public string Filter { get; set; }

        public string OrderBy { get; set; }
    }
}
