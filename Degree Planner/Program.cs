using Degree_Planner.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Degree_Planner {
    public class Program {
        public static void Main(string[] args) {
            IWebHost host = CreateWebHostBuilder(args).Build();

            //Run the web site
            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
	            .UseWebRoot("wwwroot")
                .UseStartup<Startup>();
    }
}
