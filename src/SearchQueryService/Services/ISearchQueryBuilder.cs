using SearchQueryService.Documents.Models;

namespace SearchQueryService.Services
{
    public interface ISearchQueryBuilder
    {
        string Build(string indexName, AzSearchParams searchParams);
    }
}
