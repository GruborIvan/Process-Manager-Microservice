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
    public class EndActivityCommandHandlerTests
    {
        private const string _completedStatus = "completed";
        private readonly Mock<EndActivityCommandValidator> _mockValidator = new Mock<EndActivityCommandValidator>();

        public EndActivityCommandHandlerTests()
        {
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<EndActivityCommand>()))
                .Verifiable();
        }

        [Fact]
        public async Task ValidCommand_EndActivity_Succeeds()
        {
            await Task.Run(() =>
            {
                // Arrange
                var command = new EndActivityCommand(
                    activityId: Guid.NewGuid(),
                    status: "completed",
                    endDate: DateTime.UtcNow,
                    uri: "test/test");

                var activity = new Activity(
                    command.ActivityId,
                    Guid.NewGuid(), 
                    "Test",
                    _completedStatus,
                    command.URI,
                    DateTime.UtcNow,
                    command.EndDate,
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

                var handler = new EndActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

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
            EndActivityCommand invalidCommand = null;

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<EndActivityCommand>()))
                .Throws(new ArgumentNullException());

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new EndActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(invalidCommand, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task CommandInvalid_Throws_ValidationException()
        {
            // Arrange
            var invalidCommand = new EndActivityCommand(
                 activityId: Guid.NewGuid(),
                 status: string.Empty,
                 endDate: DateTime.UtcNow,
                 uri: "test/test");

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<EndActivityCommand>()))
                .Throws(new ValidationException(string.Empty))
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new EndActivityCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task NullRepository_Throws_NullReferenceException()
        {
            // Arrange
            var command = new EndActivityCommand(
                 activityId: Guid.NewGuid(),
                 status: "completed",
                 endDate: DateTime.UtcNow,
                 uri: "test/test");

            var handler = new EndActivityCommandHandler(null, _mockValidator.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));

            _mockValidator.Verify();
        }

        [Fact]
        public async Task NullValidator_Throws_NullReferenceException()
        {
            // Arrange
            var command = new EndActivityCommand(
                activityId: Guid.NewGuid(),
                status: "status",
                endDate: DateTime.Now,
                uri: null
                );

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new EndActivityCommandHandler(mockUnitOfWork.Object, null);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));

            mockUnitOfWork.Verify();
        }
    }
}
