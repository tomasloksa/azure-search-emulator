using Flurl;
using Kros.Extensions;
using SearchQueryService.Documents.Models.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SearchQueryService.Services
{
    public class SolrSearchQueryBuilder : ISearchQueryBuilder
    {
        private static readonly Dictionary<string, string> _replacements = new()
        {
            // Dates
            { @"(\w+)\s+ge\s+([^\s)]+)", "$1:[$2 TO *]" },
            { @"(\w+)\s+gt\s+([^\s)]+)", "$1:{$2 TO *}" },
            { @"(\w+)\s+le\s+([^\s)]+)", "$1:[* TO $2]" },
            { @"(\w+)\s+lt\s+([^\s)]+)", "$1:{* TO $2}" },

            // Booleans and logic
            { @"\(not\s(\w+)\)", "($1: false)" },
            { @"\((\w+)\)", "($1: true)" },
            { @"(\w+)\s+ne", "NOT $1:" },

            // Id
            { @"\(Id:\s?'(\d*)'\)", "(id: $1)" },

            // (non-)Empty fields
            { @"NOT\s(\w+:)\s?''", "$1['' TO * ]" },
            { @"(\w+:)\s?''", "-$1['' TO * ]" }
        };

        public string Build(string indexName, AzSearchParams searchParams)
            => indexName.AppendPathSegments("select")
            .SetQueryParam("q.op", searchParams.SearchMode == "all" ? "AND" : "OR")
            .SetQueryParams(new
            {
                q = searchParams.Search.IsNullOrEmpty()
                    ? "*:*"
                    : ConvertAzSearchQuery(searchParams.Search, searchParams.SearchFields),
                rows = searchParams.Top,
                start = searchParams.Skip,
                fq = searchParams.Filter.IsNullOrEmpty() ? searchParams.Filter : ConvertAzFilterQuery(searchParams.Filter),
                sort = searchParams.OrderBy.IsNullOrEmpty()
                    ? searchParams.OrderBy
                    : AddDefaultSortDirection(searchParams.OrderBy)
            });

        private static string AddDefaultSortDirection(string orderBy)
            => Regex.Replace(orderBy, @"(\w+\b(?<!\basc|desc))(?!\b asc| desc)(?=,|$|\s)", "$1 asc")
                    .Replace("Id", "id");

        private static string ConvertAzSearchQuery(string search, string searchFields)
        {
            search = search.Replace("+", " AND ")
                           .Replace("|", " OR ");

            search = Regex.Replace(search, @"(?<!\\)-", " NOT ");

            if (!string.IsNullOrWhiteSpace(searchFields))
            {
                string[] fields = searchFields
                    .Replace(" ", "")
                    .Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                return string.Join(" OR ", fields.Select(field => $"{field}: ({search})"));
            }

            return search;
        }

        private static string ConvertAzFilterQuery(string filter)
        {
            filter = filter.Replace(" eq", ":")
                           .Replace(" and ", " AND ")
                           .Replace(" or ", " OR ")
                           .Replace(" not ", " NOT ");

            foreach (var kv in _replacements)
            {
                filter = Regex.Replace(filter, kv.Key, kv.Value);
            }

            return filter;
        }
    }
}
