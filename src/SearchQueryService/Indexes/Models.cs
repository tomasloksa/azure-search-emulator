using MMLib.ToString.Abstraction;
using Newtonsoft.Json;

namespace SearchQueryService.Indexes.InvoicingAzureSearch
{
    public class SearchIndex
    {
        public string Name;
        public Field[] Fields;
    }

    public class Field
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Field[] Fields { get; set; }
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

    public interface SolrField { }

    [ToString]
    public partial class SolrAddField : SolrField
    {
        /// <summary>
        /// Name of the field.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Type of the field.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Whether the field can be retrieved in search query.
        /// </summary>
        public bool Stored { get; set; }
        /// <summary>
        /// Whether the field can be searched.
        /// </summary>
        public bool Indexed { get; set; }
        /// <summary>
        /// Whether the field can contain multiple values.
        /// </summary>
        public bool MultiValued { get; set; }
        /// <summary>
        /// Whether search should return value even if stored=false. All basic field types are docValues=true by default.
        /// </summary>
        public bool UseDocValuesAsStored { get; set; }

        public static SolrAddField Create(string name, Field field)
            => new SolrAddField
            {
                Name = name,
                Type = Tools.GetSolrType(field.Type),
                Stored = field.Retrievable,
                Indexed = field.Searchable,
                MultiValued = name.Contains("."),
                UseDocValuesAsStored = false
            };
    }

    public class SolrAddCopyField : SolrField
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

    public class SolrSearchResponse
    {
        /// <summary>
        /// Number of documents found.
        /// </summary>
        public int NumFound { get; set; }
    }
}
