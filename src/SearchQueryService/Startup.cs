using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using SearchQueryService.Helpers;
using SearchQueryService.Indexes;
using SearchQueryService.Services;
using System;
using System.Net.Http;

namespace SearchQueryService
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddTransient<SolrService>();
            services.AddTransient<IndexesProcessor>();

            services.AddSingleton<ISearchQueryBuilder, SolrSearchQueryBuilder>();
            services.AddSingleton<SchemaMemory>();

            services.AddHttpClient();
            services.AddHttpClient<SolrService>(httpClient =>
            {
                httpClient.BaseAddress = Tools.GetSearchUrl();
            }).AddPolicyHandler(GetRetryPolicy());
            services
                .AddHealthChecks()
                .AddCheck<SolrHealthCheck>("Solr");

            services.AddHttpLogging(options =>
            {
                options.LoggingFields = HttpLoggingFields.ResponsePropertiesAndHeaders |
                                        HttpLoggingFields.ResponseBody |
                                        HttpLoggingFields.RequestPropertiesAndHeaders |
                                        HttpLoggingFields.RequestBody;
            });
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
            => HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(10, retryAttempt =>
                {
                    Console.WriteLine($"====== Sending request to Solr. Attempt: {retryAttempt}");
                    return TimeSpan.FromMilliseconds(500);
                });

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpLogging();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });
        }
    }
}

