using FluentValidation;
using Moq;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Domain.Validators;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class StartActivityCommandHandlerTests
    {
        private const string _inProgressStatus = "in progress";
        private readonly Mock<StartActivityCommandValidator> _mockValidator = new Mock<StartActivityCommandValidator>();

        public StartActivityCommandHandlerTests()
        {
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<StartActivityCommand>()))
                .Verifiable();
        }

        [Fact]
        public async Task ValidCommand_StartActivity_Succeeds()
        {
            await Task.Run(() =>
            {
                // Arrange
                var command = new StartActivityCommand(
                    operationId: Guid.NewGuid(),
                    activityId: Guid.NewGuid(),
                    name: "test",
                    startDate: DateTime.UtcNow,
                    uri: "test"
                    );

                var activity = new Activity(
                    command.ActivityId,
                    Guid.NewGuid(),
                    "Test",
                    _inProgressStatus,
                    command.URI,
                    DateTime.UtcNow);

                var mockUnitOfWork = new Mock<IUnitOfWork>();
                mockUnitOfWork.Setup(
                        repo => repo.ActivityRepository.AddAsync(activity, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(activity);
                mockUnitOfWork.Setup(
                        repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Verifiable();

                var handler = new StartActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

                // Act
                Func<Task> testCode = async () =>
                {
                    await handler.Handle(command, new System.Threading.CancellationToken());
                };

                var actionResult = Record.ExceptionAsync(testCode);

                // Assert
                Assert.Null(actionResult.Exception);
                Assert.Equal("RanToCompletion", actionResult.Status.ToString());

                mockUnitOfWork.Verify();
                _mockValidator.Verify();
            });
        }

        [Fact]
        public async Task NullCommand_Throws_ArgumentNullException()
        {
            // Arrange
            StartActivityCommand invalidCommand = null;

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<StartActivityCommand>()))
                .Throws(new ArgumentNullException());

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new StartActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(invalidCommand, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task CommandInvalid_Throws_ValidationException()
        {
            // Arrange
            var invalidCommand = new StartActivityCommand(
                operationId: Guid.NewGuid(),
                activityId: Guid.NewGuid(),
                name: string.Empty,
                startDate: DateTime.UtcNow,
                uri: string.Empty
                );

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<StartActivityCommand>()))
                .Throws(new ValidationException(string.Empty))
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new StartActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task NullRepository_Throws_NullReferenceException()
        {
            // Arrange
            var command = new StartActivityCommand(
                operationId: Guid.NewGuid(),
                activityId: Guid.NewGuid(),
                name: "test",
                startDate: DateTime.UtcNow,
                uri: "test"
                );

            var handler = new StartActivityCommandHandler(null, _mockValidator.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));
        }

        [Fact]
        public async Task NullValidator_Throws_NullReferenceException()
        {
            // Arrange
            var command = new StartActivityCommand(
                operationId: Guid.NewGuid(),
                activityId: Guid.NewGuid(),
                name: "test",
                startDate: DateTime.UtcNow,
                uri: "test"
                );

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new StartActivityCommandHandler(mockUnitOfWork.Object, null);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));
        }
    }
}
