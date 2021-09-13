using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SearchQueryService.Indexes.InvoicingAzureSearch
{
    public class SearchIndex
    {
        public string Name;
        public Field[] Fields;
    }

    public class Field
    {
        public string Name;
        public string Type;
        public Field[] Items;
        public bool Searchable;
        public bool Filterable;
        public bool Retrievable;
        public bool Sortable;
        public bool Facetable;
        public bool Key;
        public string IndexAnalyzer;
        public string SearchAnalyzer;
        public string Analyzer;
        public string[] SynonymMaps;
    }

    public class SolrField
    {
        [JsonProperty(PropertyName = "name")]
        public string Name;
        [JsonProperty(PropertyName = "type")]
        public string Type;
        [JsonProperty(PropertyName = "stored")]
        public bool Stored;
    }
}
