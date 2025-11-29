using FluentValidation;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain.Validators
{
    public class EndActivityCommandValidatorTests
    {
        private readonly EndActivityCommandValidator _validator = new EndActivityCommandValidator();

        [Fact]
        public void Valid_EndActivityCommand_Succeeds()
        {
            // Act
            var command = new EndActivityCommand(
                activityId: Guid.NewGuid(),
                status: "completed",
                endDate: DateTime.UtcNow,
                uri: "test/test");
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Null_EndActivityCommand_ThrowsArgumentNullException()
        {
            // Act
            EndActivityCommand command = null;
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public void Validate_Invalid_Commands_EndActivityCommand_ThrowsValidationException(EndActivityCommand invalidCommand)
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
                yield return new EndActivityCommand[]
                {
                    new EndActivityCommand(Guid.Empty, string.Empty, DateTime.MinValue, null)
                };
                yield return new EndActivityCommand[]
                {
                    new EndActivityCommand(Guid.NewGuid(), string.Empty, DateTime.MinValue, null)
                };
                yield return new EndActivityCommand[]
                {
                    new EndActivityCommand(Guid.Empty, "completed", DateTime.MinValue, null)
                };
                yield return new EndActivityCommand[]
                {
                    new EndActivityCommand(Guid.NewGuid(), "completed", DateTime.MinValue, null)
                };
            }
        }
    }
}
