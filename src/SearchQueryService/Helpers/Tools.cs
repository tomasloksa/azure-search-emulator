using System;

namespace SearchQueryService.Helpers
{
    public static class Tools
    {
        public static string GetSolrType(string azType, bool nested)
            => azType switch
            {
                "Edm.String" => "text_general",
                "Edm.Int32" => nested ? "pints" : "pint",
                "Edm.Int64" => nested ? "plongs" : "plong",
                "Edm.Boolean" => nested ? "booleans" : "boolean",
                "Edm.Double" => nested ? "pdoubles" : "pdouble",
                "Edm.DateTimeOffset" => nested ? "pdates" : "pdate",
                "Edm.ComplexType" => nested ? "strings" : "string",
                "Collection(Edm.ComplexType)" => "strings",
                "Collection(Edm.Int64)" => "plongs",
                "Collection(Edm.Int32)" => "pints",
                "Collection(Edm.Double)" => "pdoubles",
                "Collection(Edm.DateTimeOffset)" => "pdates",
                _ => throw new ArgumentOutOfRangeException($"Not expected index type value: {azType}")
            };

        public static Uri GetSearchUrl()
            => new(Environment.GetEnvironmentVariable("SEARCH_URL")
                   ?? throw new InvalidOperationException("'SEARCH_URL' is required parameter."));
    }
}
