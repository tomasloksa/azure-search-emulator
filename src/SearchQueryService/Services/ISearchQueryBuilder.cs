using SearchQueryService.Documents.Models.Azure;

namespace SearchQueryService.Services
{
    public interface ISearchQueryBuilder
    {
        string Build(string indexName, AzSearchParams searchParams);
    }
}
