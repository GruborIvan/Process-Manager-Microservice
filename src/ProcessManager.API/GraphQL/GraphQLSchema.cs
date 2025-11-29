using GraphQL;
using GraphQL.Types;

namespace ProcessManager.API.GraphQL
{
    public class GraphQLSchema : Schema
    {
        public GraphQLSchema(IDependencyResolver resolver) : base(resolver)
        {
            Query = resolver.Resolve<GraphQLQuery>();
        }
    }
}
