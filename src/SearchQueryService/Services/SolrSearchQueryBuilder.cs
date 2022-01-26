using Flurl;
using SearchQueryService.Documents.Models;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SearchQueryService.Services
{
    public class SolrSearchQueryBuilder : ISearchQueryBuilder
    {
        private readonly Dictionary<string, string> _replacements = new()
        {
            { @"(\w+)\s+(ge)\s+([^\s]+)", "$1:[$3 TO *]" },
            { @"(\w+)\s+(gt)\s+([^\s]+)", "$1:{$3 TO *}" },
            { @"(\w+)\s+(le)\s+([^\s]+)", "$1:[* TO $3]" },
            { @"(\w+)\s+(lt)\s+([^\s]+)", "$1:{* TO $3}" },
            { @"(\w+)\s+(ne)", "NOT $1:" }
        };

        public string Build(string indexName, AzSearchParams searchParams)
            => indexName.AppendPathSegments("select")
            .SetQueryParams(new
            {
                q = searchParams.Search,
                rows = searchParams.Top,
                start = searchParams.Skip,
                fq = string.IsNullOrEmpty(searchParams.Filter) ? searchParams.Filter : ConvertAzQuery(searchParams.Filter),
                sort = searchParams.OrderBy
            });

        private string ConvertAzQuery(string filter)
        {
            foreach (var kv in _replacements)
            {
                filter = Regex.Replace(filter, kv.Key, kv.Value);
            }

            var sb = new StringBuilder(filter);
            sb.Replace(" eq", ":");

            sb.Replace("and", "AND");
            sb.Replace("&", " AND ");
            sb.Replace("+", " AND ");

            sb.Replace("or", "OR");
            sb.Replace("|", " OR ");

            sb.Replace("not", "NOT");
            sb.Replace("!", " NOT ");
            sb.Replace("-", " NOT ");

            return sb.ToString();
        }
    }
}
