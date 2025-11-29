using System;
using FluentValidation;
using ProcessManager.Domain.Commands;

namespace ProcessManager.Domain.Validators
{
    public class CreateReportCommandValidator : Validator<CreateReportCommand>
    {
        public CreateReportCommandValidator()
        {
            RuleFor(x => x.CommandId).NotEmpty();
            RuleFor(x => x.CorrelationId).NotEmpty();
            RuleFor(x => x.DboEntities).NotEmpty();
            RuleFor(x => x)
                .Must(createReport =>
                {
                    bool bothDatesEmpty = 
                        !createReport.FromDatetime.HasValue && !createReport.ToDatetime.HasValue;
                    bool fromInPastToEmpty =
                        (createReport.FromDatetime.HasValue && createReport.FromDatetime.Value < DateTime.UtcNow) &&
                        !createReport.ToDatetime.HasValue;
                    bool fromInPastToLessThanFrom =
                        (createReport.FromDatetime.HasValue && createReport.FromDatetime.Value < DateTime.UtcNow) &&
                        (createReport.ToDatetime.HasValue &&
                         createReport.ToDatetime.Value > createReport.FromDatetime.Value);

                    return bothDatesEmpty || fromInPastToEmpty || fromInPastToLessThanFrom;
                })
                .WithMessage("Invalid Datetime Range");
        }
    }
}
