using GraphQL.EntityFramework;
using GraphQL.Types;
using ProcessManager.Infrastructure.Models;
using System.Linq;

namespace ProcessManager.API.GraphQL.Graphs
{
    public class RelationGraph : EfObjectGraphType<ProcesManagerDbContext, RelationDbo>
    {
        private const string _navigationName =
            nameof(RelationDbo.WorkflowRelations) + "." + nameof(WorkflowRelationDbo.WorkflowRun);

        public RelationGraph(IEfGraphQLService<ProcesManagerDbContext> efGraphQlService) : base(efGraphQlService)
        {
            Field(x => x.EntityId, type: typeof(IdGraphType));
            Field(x => x.EntityType);

            AddNavigationListField(
                name: "processes",
                resolve: context => context.Source.WorkflowRelations.Select(x => x.WorkflowRun),
                includeNames: new[] { _navigationName },
                graphType: typeof(ProcessGraph));
        }
    }
}
