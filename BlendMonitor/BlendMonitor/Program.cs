using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace BlendMonitor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = AppConfiguration.Configure();
            await hostBuilder.RunConsoleAsync();
        }
    }
}
