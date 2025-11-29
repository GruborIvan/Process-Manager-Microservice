using Autofac;
using Autofac.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using ProcessManager.BackgroundWorker;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.BackgroundWorker.Helpers;
using ProcessManager.BackgroundWorker.Modules;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Modules;
using Rebus.Bus;
using Rebus.Config;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Rebus.TestHelpers;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{
    public abstract class TestFixture
    {
        private static readonly IConfiguration _config;

        protected readonly IHostBuilder hostBuilder;
        protected (string blobName, Stream stream) blobData;

        protected const string mediaType = "application/json";
        protected static readonly Encoding encoding = Encoding.UTF8;

        protected Mock<IEventNotificationService> mockEventGridService;

        static TestFixture()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }

        protected TestFixture()
        {
            hostBuilder = new HostBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(builder =>
            {
                builder.RegisterModule(new AutoMapperModule(typeof(InfrastructureModule).Assembly, new RebusContextAccessor()));
                builder.RegisterModule(new InfrastructureModule(_config));
                builder.RegisterModule<MediatRModule>();
                builder.RegisterHandlersFromAssemblyOf<StartProcessMessageHandler>();

                builder.Register(c => new FakeBus()).As<IBus>();

                // Mock the event service so no events are sent to EventHub
                var mockEventService = new Mock<IEventStreamingService>().Object;
                builder.Register(c => mockEventService).As<IEventStreamingService>();

                mockEventGridService = new Mock<IEventNotificationService>();
                builder.Register(c => mockEventGridService.Object).As<IEventNotificationService>();

                mockEventGridService = new Mock<IEventNotificationService>();
                builder.Register(c => mockEventGridService.Object).As<IEventNotificationService>();

                var mockProcessManager = new Mock<IProcessService>();
                builder.Register(c => mockProcessManager.Object).As<IProcessService>();

                var mockBlobContainerClient = new Mock<BlobContainerClient>();
                mockBlobContainerClient
                    .Setup(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Callback((string name, Stream content, CancellationToken ct) =>
                    {
                        blobData.blobName = name;
                        blobData.stream = content;
                    });
                var mockBlobServiceClientService = new Mock<BlobServiceClient>();
                mockBlobServiceClientService
                    .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                    .Returns(mockBlobContainerClient.Object);
                builder.Register(c => mockBlobServiceClientService.Object).As<BlobServiceClient>();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
                services.TryAddScoped<IContextAccessor, RebusContextAccessor>();

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<ProcesManagerDbContext>(options =>
                    options.UseInMemoryDatabase(dbName), ServiceLifetime.Singleton);

                services.Configure<TelemetryConfiguration>(o =>
                    o.ConnectionString = _config.GetConnectionString("AppInsightsConnectionString"));
            })
            .ConfigureWebHost(conf =>
            {
                conf.UseTestServer();
                conf.Configure(_ => { });
                conf.UseConfiguration(_config);
            });
        }
    }
}
