using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography.X509Certificates;

namespace EshopDemo.Api
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(options => options.ListenAnyIP(443, listenOptions => listenOptions.UseHttps(
                        adapterOptions =>
                        {
                            adapterOptions.ServerCertificate = new X509Certificate2("/https/aspnetapp.pfx", "password");
                        })));
                });
    }
}
