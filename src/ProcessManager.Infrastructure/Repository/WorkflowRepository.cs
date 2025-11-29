using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;

namespace ProcessManager.Infrastructure.Repository
{
    public class WorkflowRepository : IWorkflowRepository
    {
        private readonly ProcesManagerDbContext _dbContext;
        private readonly IMapper _mapper;

        public WorkflowRepository(ProcesManagerDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper;
        }

        public async Task<WorkflowRun> AddAsync(WorkflowRun workflowRun, IEnumerable<Relation> relations, CancellationToken ct = default)
        {
            var newWorkflowDbo = _mapper.Map<WorkflowRunDbo>(workflowRun);

            if (relations != null && relations.Any())
            {
                foreach (var relation in relations)
                {
                    if (_dbContext.Relations.Any(x => x.EntityId == relation.EntityId))
                    {
                        _dbContext.WorkflowRelations.Add(
                            new WorkflowRelationDbo
                            {
                                WorkflowRun = newWorkflowDbo,
                                EntityId = relation.EntityId
                            });
                    }
                    else
                    {
                        _dbContext.WorkflowRelations.Add(
                            new WorkflowRelationDbo
                            {
                                WorkflowRun = newWorkflowDbo,
                                Relation = new RelationDbo
                                {
                                    EntityId = relation.EntityId,
                                    EntityType = relation.EntityType
                                }
                            });
                    }
                }
            }

            var newWorkflow = (await _dbContext.WorkflowRuns.AddAsync(newWorkflowDbo, ct)).Entity;

            return _mapper.Map<WorkflowRun>(newWorkflow);
        }

        public async Task<WorkflowRun> GetAsync(Guid operationId, CancellationToken ct = default)
        {
            var workflowDbo = await _dbContext
                .WorkflowRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.OperationId == operationId, ct)
                ?? throw new WorkflowNotFoundException(operationId);

            return _mapper.Map<WorkflowRun>(workflowDbo);
        }

        public async Task<bool> CheckIfExists(Guid operationId, CancellationToken ct = default)
        {
            return await _dbContext
                .WorkflowRuns
                .AsNoTracking()
                .AnyAsync(x => x.OperationId == operationId, ct);
        }

        public WorkflowRun Update(WorkflowRun workflowRun)
        {
            var workflowDbo = _dbContext
                                  .WorkflowRuns
                                  .FirstOrDefault(c => c.OperationId == workflowRun.OperationId)
                              ?? throw new WorkflowNotFoundException(workflowRun.OperationId);

            workflowDbo.Status = workflowRun.Status;
            workflowDbo.EndDate = workflowRun.EndDate;
            workflowDbo.DomainEvents = workflowRun.DomainEvents;

            return _mapper.Map<WorkflowRun>(workflowDbo);
        }

        public async Task UpdateWorkflowId(Guid operationId, string msRequestId)
        {
            var workflowDbo = _dbContext
                                  .WorkflowRuns
                                  .FirstOrDefault(c => c.OperationId == operationId)
                              ?? throw new WorkflowNotFoundException(operationId);

            workflowDbo.WorkflowRunId = msRequestId;
            _dbContext.Entry<WorkflowRunDbo>(workflowDbo).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
    }
}
