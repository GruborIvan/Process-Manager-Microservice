using GraphQL.EntityFramework;
using Microsoft.EntityFrameworkCore;
using ProcessManager.API.GraphQL.Graphs;
using ProcessManager.Infrastructure.Models;

namespace ProcessManager.API.GraphQL
{
    public class GraphQLQuery : QueryGraphType<ProcesManagerDbContext>
    {
        public GraphQLQuery(IEfGraphQLService<ProcesManagerDbContext> graphQLService)
            : base(graphQLService)
        {
            AddQueryConnectionField(
                name: "processes",
                resolve: context => context.DbContext.WorkflowRuns.Include(x => x.Activities),
                pageSize: 999,
                graphType: typeof(ProcessGraph));

            AddQueryConnectionField(
               name: "relations",
               resolve: context => context.DbContext.Relations,
               pageSize: 999,
               graphType: typeof(RelationGraph));

            AddQueryConnectionField(
               name: "activities",
               resolve: context => context.DbContext.Activities,
               pageSize: 999,
               graphType: typeof(ActivityGraph));
        }
    }
}
