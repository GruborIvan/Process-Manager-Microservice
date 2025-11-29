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
    public class UpdateActivityCommandHandlerTests
    {
        private readonly Mock<UpdateActivityCommandValidator> _mockValidator = new Mock<UpdateActivityCommandValidator>();

        public UpdateActivityCommandHandlerTests()
        {
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateActivityCommand>()))
                .Verifiable();
        }

        [Fact]
        public async Task ValidCommand_UpdateActivityCommand_Succeeds()
        {
            await Task.Run(() =>
            {
                // Arrange
                var command = new UpdateActivityCommand(
                    activityId: Guid.NewGuid(),
                    status: "completed",
                    uri: "test/test");

                var activity = new Activity(
                    command.ActivityId,
                    Guid.NewGuid(),
                    "Test",
                    "in progress",
                    command.URI,
                    DateTime.UtcNow,
                    null,
                    new WorkflowRun(
                        default,
                        null,
                        null,
                        null,
                        null));

                var mockUnitOfWork = new Mock<IUnitOfWork>();
                mockUnitOfWork.Setup(
                        repo => repo.ActivityRepository.GetAsync(It.IsAny<Guid>(), default))
                    .ReturnsAsync(activity)
                    .Verifiable();
                mockUnitOfWork.Setup(
                        repo => repo.ActivityRepository.Update(activity))
                    .Returns(activity)
                    .Verifiable();
                mockUnitOfWork.Setup(
                        repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Verifiable();

                var handler = new UpdateActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

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
            UpdateActivityCommand invalidCommand = null;

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateActivityCommand>()))
                .Throws(new ArgumentNullException());

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(invalidCommand, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task CommandInvalid_Throws_ValidationException()
        {
            // Arrange
            var invalidCommand = new UpdateActivityCommand(
                 activityId: Guid.NewGuid(),
                 status: string.Empty,
                 uri: "test/test");

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateActivityCommand>()))
                .Throws(new ValidationException(string.Empty))
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task NullRepository_Throws_NullReferenceException()
        {
            // Arrange
            var command = new UpdateActivityCommand(
                 activityId: Guid.NewGuid(),
                 status: "completed",
                 uri: "test/test");

            var handler = new UpdateActivityCommandHandler(null, _mockValidator.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task NullValidator_Throws_NullReferenceException()
        {
            // Arrange
            var command = new UpdateActivityCommand(
                activityId: Guid.NewGuid(),
                status: "status",
                uri: "uri"
                );

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateActivityCommandHandler(mockUnitOfWork.Object, null);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));

            mockUnitOfWork.Verify();
        }
    }
}
