using FluentValidation;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain.Validators
{
    public class StartProcessCommandValidatorTests
    {
        private readonly StartProcessCommandValidator _validator = new StartProcessCommandValidator();

        [Fact]
        public void Valid_StartProcessCommand_Succeeds()
        {
            // Act
            var command = new StartProcessCommand("key", "name", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null);
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Null_StartProcessCommand_ThrowsArgumentNullException()
        {
            // Act
            StartProcessCommand command = null;
            var exception = Record.Exception(() => _validator.ValidateAndThrow(command));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public void Validate_Invalid_Commands_StartProcessCommand_ThrowsValidationException(StartProcessCommand invalidCommand)
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
                yield return new StartProcessCommand[]
                {
                    new StartProcessCommand(string.Empty, string.Empty, Guid.Empty, new { }, Guid.Empty, null, null)
                };
                yield return new StartProcessCommand[]
                {
                    new StartProcessCommand(string.Empty, "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null)
                };
                yield return new StartProcessCommand[]
                {
                    new StartProcessCommand("key", string.Empty, Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null)
                };
                yield return new StartProcessCommand[]
                {
                    new StartProcessCommand("key", "process", Guid.NewGuid(), null, Guid.NewGuid(), null, null)
                };
            }
        }
    }
}
