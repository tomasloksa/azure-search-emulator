using Newtonsoft.Json;

namespace SearchQueryService.Indexes.Models
{
    public class AddField : ISolrField
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

        [JsonIgnore]
        public bool Searchable { get; set; }

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

        public static AddField Create(string name, AzField field)
            => new()
            {
                Name = name,
                Type = Tools.GetSolrType(field.Type),
                Stored = field.Retrievable,
                Searchable = field.Searchable,
                Indexed = field.Searchable || field.Filterable,
                MultiValued = name.Contains("."),
                UseDocValuesAsStored = false
            };
    }
}
