using GraphQL.EntityFramework;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProcessManager.Infrastructure.Models;

namespace ProcessManager.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLEntityFramework(this IServiceCollection services, IMvcBuilder mvcBuilder)
        {
            // Newtonsoft supports handling reference loops, while System.Text.Json doesn't
            // See https://github.com/dotnet/runtime/issues/29900
            mvcBuilder.AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            // Allowed synchronous IO because Newtonsoft library in GraphQL requires it to work
            // See https://github.com/graphql-dotnet/graphql-dotnet/issues/1116
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            // Nessary for the complex registration of EfGraphQlService
            EfGraphQLConventions.RegisterInContainer<ProcesManagerDbContext>(services);
            // Necessary in order to use GraphQL connection types
            EfGraphQLConventions.RegisterConnectionTypesInContainer(services);

            return services;
        }
    }
}
