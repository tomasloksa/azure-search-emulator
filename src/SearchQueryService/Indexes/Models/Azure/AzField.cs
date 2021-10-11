namespace SearchQueryService.Indexes.Models
{
    /// <summary>
    /// Azure Index model, read from index files.
    /// </summary>
    public class AzField
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public AzField[] Fields { get; set; }

        public bool Searchable { get; set; }

        public bool Filterable { get; set; }

        public bool Retrievable { get; set; }

        public bool Sortable { get; set; }

        public bool Facetable { get; set; }

        public bool Key { get; set; }

        public string IndexAnalyzer { get; set; }

        public string SearchAnalyzer { get; set; }

        public string Analyzer { get; set; }

        public string[] SynonymMaps { get; set; }
    }
}
