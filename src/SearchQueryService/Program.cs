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

            var directories = System.IO.Directory.GetDirectories("Indexes");
            foreach (string dir in directories)
            {
                var result = indexes.CreateIndex(dir).Result;
            }

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
