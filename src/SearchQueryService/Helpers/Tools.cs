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

        public static Dictionary<string, List<JsonElement>> JsonFlatten(Dictionary<string, JsonElement> json)
        {
            var flattened = new Dictionary<string, List<JsonElement>>();
            foreach (var kv in json)
            {
                if (kv.Value.ValueKind == JsonValueKind.Array)
                {
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

        public static Dictionary<string, object> JsonUnflatten(Dictionary<string, object> dotNotation)
        {
            var root = new Dictionary<string, object>();

            foreach (var dotObject in dotNotation)
            {
                var hierarcy = dotObject.Key.Split('.');

                Dictionary<string, object> current = root;

                for (int i = 0; i < hierarcy.Length; i++)
                {
                    var key = hierarcy[i];

                    if (i == hierarcy.Length - 1) // Last key
                    {
                        current.Add(key, dotObject.Value);
                    }
                    else
                    {
                        if (!current.ContainsKey(key))
                        {
                            current.Add(key, new Dictionary<string, object>());
                        }

                        current = (Dictionary<string, object>)current[key];
                    }
                }
            }

            return root;
        }
    }
}
