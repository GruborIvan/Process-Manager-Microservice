using Autofac;
using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Http;
using GraphQL.Instrumentation;
using GraphQL.Types;
using ProcessManager.API.GraphQL;
using System.Collections.Generic;
using System.Linq;

namespace ProcessManager.API.Modules
{
    public class GraphQLModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new EfDocumentExecuter()).As<IDocumentExecuter>();
            builder.RegisterInstance(new DocumentWriter()).As<IDocumentWriter>();

            // Captures metrics (might be important), not necessary for GraphQL.EntityFramework to work correctly
            // See https://github.com/graphql-dotnet/graphql-dotnet/blob/master/docs2/site/docs/getting-started/field-middleware.md
            builder.RegisterType<InstrumentFieldsMiddleware>().SingleInstance();

            RegisterGraphTypes(builder);

            builder.RegisterType<GraphQLSchema>().As<ISchema>().AsSelf();
            builder.RegisterType<GraphQLQuery>().AsSelf();

            builder.Register<IDependencyResolver>(c =>
            {
                var context = c.Resolve<IComponentContext>();
                return new FuncDependencyResolver(t => context.Resolve(t));
            });
        }

        static void RegisterGraphTypes(ContainerBuilder builder)
        {
            foreach (var type in GetGraphQlTypes())
            {
                builder.RegisterType(type).AsSelf();
            }
            builder.RegisterType(typeof(GuidGraphType)).AsSelf();
        }

        static IEnumerable<System.Type> GetGraphQlTypes()
        {
            return typeof(Startup).Assembly
                .GetTypes()
                .Where(x => !x.IsAbstract &&
                            (typeof(IObjectGraphType).IsAssignableFrom(x) ||
                             typeof(IInputObjectGraphType).IsAssignableFrom(x)));
        }
    }
}
