using FluentValidation;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain.Validators
{
    public class UpdateActivityCommandValidatorTests
    {
        private readonly UpdateActivityCommandValidator _validator = new UpdateActivityCommandValidator();

        [Fact]
        public void Valid_UpdateActivityCommand_Succeeds()
        {
            // Act
            var command = new UpdateActivityCommand(
                activityId: Guid.NewGuid(),
                status: "in progress",
                uri: "test/test");
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Null_UpdateActivityCommand_ThrowsArgumentNullException()
        {
            // Act
            UpdateActivityCommand command = null;
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public void Validate_Invalid_Commands_EndActivityCommand_ThrowsValidationException(UpdateActivityCommand invalidCommand)
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
                yield return new UpdateActivityCommand[]
                {
                    new UpdateActivityCommand(Guid.Empty, string.Empty, null)
                };
                yield return new UpdateActivityCommand[]
                {
                    new UpdateActivityCommand(Guid.NewGuid(), "completed", null)
                };
                yield return new UpdateActivityCommand[]
                {
                    new UpdateActivityCommand(Guid.NewGuid(), string.Empty, "uri")
                };
                yield return new UpdateActivityCommand[]
                {
                    new UpdateActivityCommand(Guid.Empty, "completed", "uri")
                };
            }
        }
    }
}
