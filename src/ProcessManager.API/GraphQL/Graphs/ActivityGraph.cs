using GraphQL.EntityFramework;
using GraphQL.Types;
using ProcessManager.Infrastructure.Models;

namespace ProcessManager.API.GraphQL.Graphs
{
    public class ActivityGraph : EfObjectGraphType<ProcesManagerDbContext, ActivityDbo>
    {
        public ActivityGraph(IEfGraphQLService<ProcesManagerDbContext> efGraphQlService) : base(efGraphQlService)
        {
            Field(x => x.ActivityId, type: typeof(IdGraphType));
            Field(x => x.OperationId, type: typeof(IdGraphType));
            Field(x => x.Name);
            Field(x => x.Status);
            Field(x => x.URI, nullable: true);
            Field(x => x.StartDate, type: typeof(DateTimeGraphType));
            Field(x => x.EndDate, type: typeof(DateTimeGraphType));
        }
    }
}
