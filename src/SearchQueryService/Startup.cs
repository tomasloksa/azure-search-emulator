using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SearchQueryService.Config;
using SearchQueryService.Indexes;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

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
            services.ConfigureOptions<ConnectionStringsOptions>(Configuration);
            services
                .AddHttpClient("Default")
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var certificate = new X509Certificate2("../srv/certs/solr-ssl.keystore.pfx", "123SecureSolr!");
                    var certificateValidator = new CertValidator(certificate);

                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = certificateValidator.Validate
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IndexesProcessor indexes)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());

            _ = indexes.ProcessDirectory();
        }
    }
}
