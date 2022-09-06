using Microsoft.Extensions.Diagnostics.HealthChecks;
using SearchQueryService.Services;
using System.Threading;
using System.Threading.Tasks;

namespace SearchQueryService.Helpers
{
    public class SolrHealthCheck : IHealthCheck
    {
        private readonly SolrService _solrService;

        public SolrHealthCheck(SolrService solrService)
        {
            _solrService = solrService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            bool isHealthy = await _solrService.IsHealthy();

            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Solr is health.");
            }

            return new HealthCheckResult(context.Registration.FailureStatus, "Solr has some init failures.");
        }
    }
}
