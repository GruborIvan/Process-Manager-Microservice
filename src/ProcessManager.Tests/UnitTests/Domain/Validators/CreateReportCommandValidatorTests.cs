using FluentValidation;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain.Validators
{
    public class CreateReportCommandValidatorTests
    {
        private readonly CreateReportCommandValidator _validator = new CreateReportCommandValidator();

        [Fact]
        public void Valid_CreateReportCommand_Succeeds()
        {
            // Act
            var command = new CreateReportCommand(
                correlationId: Guid.NewGuid(),
                dboEntities: new List<string> { "Activity", "WorkflowRun" },
                fromDatetime: DateTime.UtcNow.AddDays(-1),
                toDatetime: DateTime.UtcNow);
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Null_CreateReportCommand_ThrowsArgumentNullException()
        {
            // Act
            CreateReportCommand command = null;
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public void Validate_Invalid_Commands_CreateReportCommand_ThrowsValidationException(CreateReportCommand invalidCommand)
        {
            // Act
            var exception = Record.Exception(() => _validator.ValidateAndThrow(invalidCommand));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ValidationException>(exception);
        }

        [Theory]
        [MemberData(nameof(ValidDates))]
        public void Validate_ValidDates_CreateReportCommand_Succeeds(CreateReportCommand validDates)
        {
            // Act
            var exception = Record.Exception(() => _validator.ValidateAndThrow(validDates));

            // Assert
            Assert.Null(exception);
        }

        [Theory]
        [MemberData(nameof(InvalidDates))]
        public void Validate_InvalidDates_CreateReportCommand_ThrowsValidationException(CreateReportCommand invalidDates)
        {
            // Act
            var exception = Record.Exception(() => _validator.ValidateAndThrow(invalidDates));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ValidationException>(exception);
        }

        public static IEnumerable<object[]> InvalidCommands
        {
            get
            {
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.Empty, new List<string> { "Activity", "WorkflowRun"}, DateTime.MinValue, null)
                };
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), null, DateTime.MinValue, null)
                };
            }
        }

        public static IEnumerable<object[]> ValidDates
        {
            get
            {
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, null, null)
                };
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, DateTime.Now.AddDays(-1), null)
                };
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(-1))
                };
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1))
                };
            }
        }

        public static IEnumerable<object[]> InvalidDates
        {
            get
            {
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, null, DateTime.Now.AddDays(-1))
                };
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2))
                };
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(-3))
                };
                yield return new CreateReportCommand[]
                {
                    new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity", "WorkflowRun"}, DateTime.Now.Date, DateTime.Now.Date)
                };
            }
        }
    }
}