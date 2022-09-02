using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(SB_Functions.Startup))]

namespace SB_Functions
{
    class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            //string cs = Environment.GetEnvironmentVariable("AppConfigConnectionString");
            //builder.ConfigurationBuilder.AddAzureAppConfiguration(cs);

            builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
            {
                options.Connect(Environment.GetEnvironmentVariable("AppConfigConnectionString"))
                        // Load all keys that start with `SB-Function:` and have no label
                        .Select("SB-Function:*")
                        // Configure to reload configuration if the registered sentinel key is modified
                        .ConfigureRefresh(refreshOptions =>
                            refreshOptions.Register("SB-Function:Sentinel", refreshAll: true));
            });
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAzureAppConfiguration();
        }
    }
}
