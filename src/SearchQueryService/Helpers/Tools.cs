using System;

namespace SearchQueryService.Helpers
{
    public static class Tools
    {
        public static string GetSolrType(string azType)
            => azType switch
            {
                "Edm.String" => "text_general",
                "Edm.Int32" => "pint",
                "Edm.Int64" => "plong",
                "Edm.Boolean" => "boolean",
                "Edm.Double" => "pdouble",
                "Edm.DateTimeOffset" => "pdate",
                "Collection(Edm.ComplexType)" => "string",
                "Collection(Edm.Int64)" => "plongs",
                "Collection(Edm.Int32)" => "pints",
                "Collection(Edm.Double)" => "pdoubles",
                "Collection(Edm.DateTimeOffset)" => "pdates",
                _ => throw new ArgumentOutOfRangeException($"Not expected index type value: {azType}")
            };

        public static string ToCamelCase(this string str)
            => char.ToLowerInvariant(str[0]) + str[1..];

        public static string GetSearchUrl()
            => Environment.GetEnvironmentVariable("SEARCH_URL");
    }
}
