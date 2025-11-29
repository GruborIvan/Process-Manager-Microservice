using FluentValidation;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain.Validators
{
    public class StartActivityCommandValidatorTests
    {
        private readonly StartActivityCommandValidator _validator = new StartActivityCommandValidator();

        [Fact]
        public void Valid_StartActivityCommand_Succeeds()
        {
            // Act
            var command = new StartActivityCommand(
                operationId: Guid.NewGuid(), 
                activityId: Guid.NewGuid(),
                name: "test",
                startDate: DateTime.UtcNow,
                uri: "test"
                );

            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Null_StartActivityCommand_ThrowsArgumentNullException()
        {
            // Act
            StartActivityCommand command = null;
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public void Validate_Invalid_Commands_StartActivityCommand_ThrowsValidationException(StartActivityCommand invalidCommand)
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
                yield return new StartActivityCommand[]
                {
                    new StartActivityCommand(Guid.Empty, Guid.Empty, string.Empty, DateTime.MinValue, string.Empty)
                };
                yield return new StartActivityCommand[]
                {
                    new StartActivityCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty, DateTime.MinValue, "test")
                };
                yield return new StartActivityCommand[]
                {
                    new StartActivityCommand(Guid.NewGuid(), Guid.Empty, "test", DateTime.UtcNow, "test")
                };
                yield return new StartActivityCommand[]
                {
                    new StartActivityCommand(Guid.Empty, Guid.NewGuid(), "test", DateTime.UtcNow, "test")
                };
            }
        }
    }
}
