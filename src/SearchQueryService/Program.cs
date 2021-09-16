using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace SearchQueryService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var handler = new HttpClientHandler() { UseProxy = false };
            var indexes = new Indexes.Indexes(new HttpClient(handler));
            var test = indexes.CreateIndex().Result;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
