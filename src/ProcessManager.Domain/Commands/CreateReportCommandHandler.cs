using MediatR;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Validators;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class CreateReportCommandHandler : ICommandHandler<CreateReportCommand>
    {
        private readonly IReportingService _reportingService;
        private readonly CreateReportCommandValidator _validator;

        public CreateReportCommandHandler(IReportingService reportingService, CreateReportCommandValidator validator)
        {
            _reportingService = reportingService;
            _validator = validator;
        }

        public async Task<Unit> Handle(CreateReportCommand request, CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(request);

            var files = await _reportingService.GetReportingDataAsync(request.DboEntities, request.FromDatetime, request.ToDatetime, cancellationToken);
            await _reportingService.StoreReportAsync(request.CorrelationId, files, cancellationToken);
            return Unit.Value;
        }
    }
}
