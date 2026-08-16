using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SensorApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Mgr.SM.Init();
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
