using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using SearchQueryService.Helpers;
using SearchQueryService.Indexes;
using SearchQueryService.Services;
using System;

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
            services.AddTransient<IndexesProcessor>();
            services.AddTransient<ISearchQueryBuilder, SolrSearchQueryBuilder>();
            services.AddHttpClient();
            services.AddHttpClient("solr", httpClient =>
            {
                httpClient.BaseAddress = Tools.GetSearchUrl();
            }).AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(10, _ => TimeSpan.FromMilliseconds(500)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, IndexesProcessor indexes)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());

            await indexes.ProcessDirectory();
        }
    }
}
