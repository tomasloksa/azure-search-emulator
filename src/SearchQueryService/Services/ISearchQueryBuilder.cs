using SearchQueryService.Documents.Models;

namespace SearchQueryService.Services
{
    internal interface ISearchQueryBuilder
    {
        string Build(string indexName, AzSearchParams searchParams);
    }
}
