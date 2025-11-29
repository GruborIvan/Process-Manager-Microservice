using System;
using System.Collections.Generic;
using MediatR;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Validators;
using System.Threading;
using System.Threading.Tasks;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Models;
using Rebus.Pipeline;
using System.Linq;

namespace ProcessManager.Domain.Commands
{
    public class StartProcessCommandHandler : ICommandHandler<StartProcessCommand>
    {
        private const string _inProgressStatus = "in progress";

        private readonly IProcessService _processManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly IContextAccessor _contextAccessor;
        private readonly StartProcessCommandValidator _validator;

        public StartProcessCommandHandler(
            IProcessService processManager,
            IUnitOfWork unitOfWork,
            IFeatureFlagService featureFlagService,
            StartProcessCommandValidator validator,
            IContextAccessor contextAccessor)
        {
            _processManager = processManager;
            _validator = validator;
            _unitOfWork = unitOfWork;
            _featureFlagService = featureFlagService;
            _contextAccessor = contextAccessor;
        }

        public async Task<Unit> Handle(StartProcessCommand request, CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(request);

            request.OperationId = _contextAccessor.GetRequestId();

            var workflowExists = await _unitOfWork.WorkflowRepository.CheckIfExists(request.OperationId, cancellationToken);

            if (workflowExists)
                throw new ProcessAlreadyStartedException(request.OperationId);

            var featureFlags = await _featureFlagService.GetFeatureFlagsAsync(request.ProcessKey, cancellationToken);

            var principalId = await _processManager.GetPrincipalIdAsync(request.ProcessName, request.EnvironmentShortName);

            var startMessage = CreateStartMessage(request, featureFlags, principalId);

            var process = await _processManager.GetProcessWithMessageAsync(request.ProcessKey, request.ProcessName, startMessage, request.EnvironmentShortName);

            var newWorkflowRun = new WorkflowRun(
                request.OperationId,
                request.ProcessKey,
                _inProgressStatus,
                request.CreatedBy.ToString(),
                process
            );

            newWorkflowRun.WorkflowRunId = String.Empty;

            await _unitOfWork.WorkflowRepository.AddAsync(newWorkflowRun, request.Relations, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        private object CreateStartMessage(StartProcessCommand command, List<string> featureFlags, Guid principalId)
            => new { startMessage = command.StartMessage, command.CreatedBy, ExternalId = principalId, command.OperationId, featureFlags };
    }
}
