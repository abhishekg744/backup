using AutoMapper;
using BlendMonitor.Entities;
using BlendMonitor.Repository;
using BlendMonitor.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlendMonitor
{
    public class AppConfiguration
    {

        private static IConfiguration _iconfiguration;

        public static IHostBuilder Configure()
        {
            return new HostBuilder()
            .ConfigureAppConfiguration((hostContext, configBuilder) =>
            {
                configBuilder.SetBasePath(Directory.GetCurrentDirectory());
                configBuilder.AddJsonFile("appsettings.json", optional: false);
                configBuilder.AddEnvironmentVariables();
                _iconfiguration = configBuilder.Build();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var connection = _iconfiguration.GetConnectionString("ABC_BlendMonitorDB");
                services.AddDbContext<BlendMonitorContext>(option => option.UseSqlServer(connection))
                .AddScoped<IHostedService, TimedHostedService>();

                services.AddAutoMapper(typeof(Program));
               
                services.AddScoped<IBlendMonitorRepository, BlendMonitorRepository>();
                services.AddScoped<IBlendMonitorService, BlendMonitorService>();

            });

        }

    }
}
