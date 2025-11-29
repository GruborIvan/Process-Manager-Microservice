using ProcessManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IWorkflowRepository
    {
        public Task<WorkflowRun> GetAsync(Guid operationId, CancellationToken ct = default);
        public Task<WorkflowRun> AddAsync(WorkflowRun workflowRun, IEnumerable<Relation> relations, CancellationToken ct = default);
        public WorkflowRun Update(WorkflowRun workflowRun);
        public Task<bool> CheckIfExists(Guid operationId, CancellationToken ct = default);
        public Task UpdateWorkflowId(Guid operationId, string msRequestId);
    }
}
