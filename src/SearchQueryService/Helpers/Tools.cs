using SearchQueryService.Indexes.Models.Azure;
using System;
using System.Collections.Generic;
using System.Text.Json;

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

        public static Dictionary<string, List<JsonElement>> JsonFlatten(
            Dictionary<string, JsonElement> json,
            Dictionary<string, AzField> nestedSchema)
        {
            var flattened = new Dictionary<string, List<JsonElement>>();
            foreach (var kv in json)
            {
                if (kv.Value.ValueKind == JsonValueKind.Array)
                {
                    if (kv.Value.GetArrayLength() == 0 && nestedSchema.TryGetValue(kv.Key, out AzField root))
                    {
                        foreach (var nestedItem in root.Fields)
                        {
                            flattened.Add(kv.Key + "." + nestedItem.Name, new List<JsonElement>());
                        }

                        continue;
                    }

                    foreach (var arrayItem in kv.Value.EnumerateArray())
                    {
                        if (arrayItem.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var property in arrayItem.EnumerateObject())
                            {
                                string propName = kv.Key + "." + property.Name;
                                if (flattened.ContainsKey(propName))
                                {
                                    flattened[propName].Add(property.Value);
                                }
                                else
                                {
                                    flattened.Add(propName, new List<JsonElement>() { property.Value });
                                }
                            }
                        }
                        else
                        {
                            flattened.Add(kv.Key, kv.Value.Deserialize<List<JsonElement>>());
                            break;
                        }
                    }
                }
                else
                {
                    flattened.Add(kv.Key, new List<JsonElement>() { kv.Value });
                }
            }

            return flattened;
        }

        public static Dictionary<string, object> JsonUnflatten(Dictionary<string, object> jsonDoc)
        {
            var unflattened = new Dictionary<string, object>();

            foreach (var property in jsonDoc)
            {
                if (!property.Key.Contains('.'))
                {
                    unflattened.Add(property.Key, property.Value);
                    continue;
                }

                var parts = property.Key.Split('.');

                if (!unflattened.ContainsKey(parts[0]))
                {
                    unflattened.Add(parts[0], new List<Dictionary<string, object>>());
                }

                if (property.Value is JsonElement val && val.ValueKind == JsonValueKind.Array)
                {
                    var dest = unflattened[parts[0]] as List<Dictionary<string, object>>;
                    int i = 0;
                    foreach (var value in val.EnumerateArray())
                    {
                        if (i >= dest.Count)
                        {
                            dest.Add(new Dictionary<string, object>());
                        }
                        dest[i++].Add(parts[1], value);
                    }
                }
            }

            return unflattened;
        }
    }
}
