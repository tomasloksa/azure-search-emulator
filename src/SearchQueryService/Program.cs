using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SearchQueryService.Indexes;
using System.Threading.Tasks;

namespace SearchQueryService
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            IHost webHost = CreateHostBuilder(args).Build();
            using (var scope = webHost.Services.CreateScope())
            {
                var indexer = scope.ServiceProvider.GetRequiredService<IndexesProcessor>();
                await indexer.ProcessDirectory();
            }
            await webHost.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
