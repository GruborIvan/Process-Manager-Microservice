using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProcessManager.API;
using ProcessManager.API.Modules;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Modules;
using System;
using System.IO;
using System.Net.Http;

namespace ProcessManager.Tests.IntegrationTests.API
{
    public class TestFixture
    {
        protected readonly IHostBuilder hostBuilder;

        protected IHost host;
        protected HttpClient client;

        public TestFixture()
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .Build();

            hostBuilder = new HostBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterModule(new AutoMapperModule());
                    builder.RegisterModule(new InfrastructureModule(config));
                    builder.RegisterModule(new GraphQLModule());
                })
                .ConfigureWebHost(conf =>
                {
                    conf.UseTestServer();
                    conf.UseStartup<TestStartup>();
                    conf.UseConfiguration(config);

                    // Ignore the StartupStaging class assembly as the "entry point" and instead point it to this app
                    conf.UseSetting(WebHostDefaults.ApplicationKey, typeof(Program).Assembly.FullName);
                });
        }

        private class TestStartup : Startup
        {
            public TestStartup(IConfiguration configuration)
                : base(configuration)
            {
            }

            protected override void RegisterDbContext(IServiceCollection services)
            {
                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<ProcesManagerDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            }

            protected override void ConfigureAuditMiddleware(IApplicationBuilder app)
            {
            }
        }
    }
}
