﻿using SearchQueryService.Helpers;
using SearchQueryService.Indexes.Models.Azure;
using System.Text.Json.Serialization;

namespace SearchQueryService.Indexes.Models.Solr
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
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? MultiValued { get; set; }

        /// <summary>
        /// Whether search should return value even if stored=false. All basic field types are docValues=true by default.
        /// </summary>
        public bool UseDocValuesAsStored { get; set; }

        public static AddField Create(string name, AzField field)
        {
            bool isNested = name.Contains('.');
            var newField = new AddField()
            {
                Name = name,
                Type = Tools.GetSolrType(field.Type, isNested),
                Stored = field.Retrievable,
                Searchable = field.Searchable,
                Indexed = field.Searchable || field.Filterable,
                UseDocValuesAsStored = false
            };

            if (newField.Type == "text_general" && !isNested)
            {
                newField.MultiValued = false;
            }

            return newField;
        }
    }
}
