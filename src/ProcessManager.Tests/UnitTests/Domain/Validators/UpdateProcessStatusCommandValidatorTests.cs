using FluentValidation;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain.Validators
{
    public class UpdateProcessStatusCommandValidatorTests
    {
        private readonly UpdateProcessStatusCommandValidator _validator = new UpdateProcessStatusCommandValidator();

        [Fact]
        public void Valid_UpdateProcessCommand_Succeeds()
        {
            // Act
            var command = new UpdateProcessStatusCommand(
                operationId: Guid.NewGuid(),
                status: "succeeded",
                endDate: DateTime.UtcNow, null, null);
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Null_UpdateProcessCommand_ThrowsArgumentNullException()
        {
            // Act
            UpdateProcessStatusCommand command = null;
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public void Validate_Invalid_Commands_UpdateProcessCommand_ThrowsValidationException(UpdateProcessStatusCommand invalidCommand)
        {
            // Act
            var exception = Record.Exception(() => _validator.ValidateAndThrow(invalidCommand));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ValidationException>(exception);
        }

        public static IEnumerable<object[]> InvalidCommands
        {
            get
            {
                yield return new UpdateProcessStatusCommand[]
                {
                    new UpdateProcessStatusCommand(
                        operationId: Guid.Empty, status: string.Empty, endDate: default, null, null)
                };
                yield return new UpdateProcessStatusCommand[]
                {
                    new UpdateProcessStatusCommand(
                        operationId: Guid.NewGuid(), status: "succeeded", endDate: default, null, null)
                };
                yield return new UpdateProcessStatusCommand[]
                {
                    new UpdateProcessStatusCommand(
                        operationId: Guid.Empty, status: "succeeded", endDate: default, null, null)
                };
                yield return new UpdateProcessStatusCommand[]
                {
                    new UpdateProcessStatusCommand(
                        operationId: Guid.NewGuid(), status: string.Empty, endDate: default, null, null)
                };
            }
        }
    }
}
