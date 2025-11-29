using GraphQL.EntityFramework;
using GraphQL.Types;
using ProcessManager.Infrastructure.Models;
using System.Linq;

namespace ProcessManager.API.GraphQL.Graphs
{
    public class ProcessGraph : EfObjectGraphType<ProcesManagerDbContext, WorkflowRunDbo>
    {
        private const string _relationsNavigationName =
            nameof(WorkflowRunDbo.WorkflowRelations) + "." + nameof(WorkflowRelationDbo.Relation);

        private const string _activitiesNavigationName =
           nameof(WorkflowRunDbo.Activities);

        public ProcessGraph(IEfGraphQLService<ProcesManagerDbContext> graphQlService)
            : base(graphQlService)
        {
            Field(x => x.OperationId, type: typeof(IdGraphType));
            Field(x => x.WorkflowRunName);
            Field(x => x.Status);
            Field(x => x.CreatedBy, nullable: true);
            Field(x => x.CreatedDate, type: typeof(DateTimeGraphType));
            Field(x => x.ChangedBy, nullable: true);
            Field(x => x.ChangedDate, type: typeof(DateTimeGraphType));
            Field(x => x.EndDate, type: typeof(DateTimeGraphType), nullable: true);

            AddNavigationListField(
                name: "relations",
                resolve: context => context.Source.WorkflowRelations.Select(x => x.Relation),
                includeNames: new[] { _relationsNavigationName },
                graphType: typeof(RelationGraph)
                );

            AddNavigationListField(
                name: "activities",
                resolve: context => context.Source.Activities.Select(x => x),
                includeNames: new[] { _activitiesNavigationName },
                graphType: typeof(ActivityGraph)
                );
        }
    }
}
