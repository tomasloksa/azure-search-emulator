using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EshopDemo.Api.Config;
using System.Security.Cryptography.X509Certificates;
using SearchQueryService;
using System.Net.Http;

namespace EshopDemo.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.ConfigureOptions<ConnectionStringsOptions>(Configuration);

            services.AddHttpClient("Default")
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
