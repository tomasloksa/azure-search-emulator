using Flurl;
using SearchQueryService.Documents.Models;
using SearchQueryService.Helpers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SearchQueryService.Services
{
    public class SolrSearchQueryBuilder : ISearchQueryBuilder
    {
        public static readonly Dictionary<string, string> _replacements = new()
        {
            { @"(\w+)\s+ge\s+([^\s]+)", "$1:[$2 TO *]" },
            { @"(\w+)\s+gt\s+([^\s]+)", "$1:{$2 TO *}" },
            { @"(\w+)\s+le\s+([^\s]+)", "$1:[* TO $2]" },
            { @"(\w+)\s+lt\s+([^\s]+)", "$1:{* TO $2}" },
            { @"\(not\s(\w+)\)", "($1: false)" },
            { @"\((\w+)\)", "($1: true)" },
            { @"(\w+)\s+ne", "NOT $1:" },
        };

        public string Build(string indexName, AzSearchParams searchParams)
            => Tools.GetSearchUrl()
            .AppendPathSegments(indexName, "select")
            .SetQueryParam("q.op", searchParams.SearchMode == "all" ? "AND" : "OR")
            .SetQueryParams(new
            {
                q = ConvertAZSearchQuery(searchParams.Search),
                rows = searchParams.Top,
                start = searchParams.Skip,
                fq = string.IsNullOrEmpty(searchParams.Filter) ? searchParams.Filter : ConvertAzFilterQuery(searchParams.Filter),
                sort = searchParams.OrderBy
            });

        private string ConvertAZSearchQuery(string search) 
           => search.Replace("+", " AND ")
                    .Replace("|", " OR ")
                    .Replace("-", " NOT");

        private static string ConvertAzFilterQuery(string filter)
        {
            foreach (var kv in _replacements)
            {
                filter = Regex.Replace(filter, kv.Key, kv.Value);
            }

            return filter.Replace(" eq", ":")
                         .Replace(" and ", " AND ")
                         .Replace(" or ", " OR ")
                         .Replace(" not ", " NOT ");
        }
    }
}
