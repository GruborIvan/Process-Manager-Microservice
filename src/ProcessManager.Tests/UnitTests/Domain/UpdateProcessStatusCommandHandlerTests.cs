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
    public class UpdateProcessStatusCommandHandlerTests
    {
        private readonly Mock<UpdateProcessStatusCommandValidator> _mockValidator = new Mock<UpdateProcessStatusCommandValidator>();

        public UpdateProcessStatusCommandHandlerTests()
        {
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateProcessStatusCommand>()))
               .Verifiable();
        }

        [Fact]
        public async Task ValidCommand_UpdateProcessStatus_Succeeds()
        {
            await Task.Run(() =>
            {
                // Arrange
                var command = new UpdateProcessStatusCommand(
                    operationId: Guid.NewGuid(),
                    status: "succeeded",
                    endDate: DateTime.UtcNow, null, null);

                var workflow = new WorkflowRun
                (
                    command.OperationId,
                    "Name",
                    command.Status,
                    Guid.NewGuid().ToString(),
                    null
                );

                var mockUnitOfWork = new Mock<IUnitOfWork>();
                mockUnitOfWork.Setup(
                        repo => repo.WorkflowRepository.GetAsync(It.IsAny<Guid>(), default))
                    .ReturnsAsync(workflow);
                mockUnitOfWork.Setup(
                        repo => repo.WorkflowRepository.Update(workflow))
                    .Returns(workflow);
                mockUnitOfWork.Setup(
                        repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Verifiable();

                var handler = new UpdateProcessStatusCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

                // Act
                Func<Task> testCode = async () =>
                {
                    await handler.Handle(command, new System.Threading.CancellationToken());
                };

                var actionResult = Record.ExceptionAsync(testCode);

                // Assert
                Assert.Null(actionResult.Exception);
                Assert.Equal("RanToCompletion", actionResult.Status.ToString());

                mockUnitOfWork.VerifyAll();
            });
        }

        [Fact]
        public async Task NullCommand_Throws_ArgumentNullException()
        {
            // Arrange
            UpdateProcessStatusCommand invalidCommand = null;

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateProcessStatusCommand>()))
                .Throws(new ArgumentNullException());

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateProcessStatusCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(invalidCommand, default));
        }

        [Fact]
        public async Task CommandInvalid_Throws_ValidationException()
        {
            // Arrange
            var invalidCommand = new UpdateProcessStatusCommand(
                operationId: Guid.NewGuid(),
                status: string.Empty,
                endDate: DateTime.MinValue, null, null);

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateProcessStatusCommand>()))
                .Throws(new ValidationException(string.Empty));

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateProcessStatusCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, default));
        }

        [Fact]
        public async Task NullRepository_Throws_NullReferenceException()
        {
            // Arrange
            var command = new UpdateProcessStatusCommand(
                operationId: Guid.NewGuid(),
                status: "failed",
                endDate: DateTime.UtcNow, null, null
                );

            var handler = new UpdateProcessStatusCommandHandler(null, _mockValidator.Object);
            
            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));
        }

        [Fact]
        public async Task NullValidator_Throws_NullReferenceException()
        {
            // Arrange
            var command = new UpdateProcessStatusCommand(
                operationId: Guid.NewGuid(),
                status: "failed",
                endDate: DateTime.UtcNow, null, null);

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateProcessStatusCommandHandler(mockUnitOfWork.Object, null);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, default));
        }

        [Fact]
        public async Task UpdateProcess_CommandNull_Throws_Validation_Exception()
        {
            // Arrange
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateProcessStatusCommand>()))
                .Throws(new ArgumentNullException());

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateProcessStatusCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            UpdateProcessStatusCommand command = null;
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(command, default));
        }

        [Fact]
        public async Task UpdateProcess_Fails_Throws_Validation_Exception()
        {
            // Arrange
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<UpdateProcessStatusCommand>()))
                .Throws(new ValidationException(string.Empty));

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var handler = new UpdateProcessStatusCommandHandler(mockUnitOfWork.Object, _mockValidator.Object);

            // Assert
            var command = new UpdateProcessStatusCommand(
                operationId: Guid.Empty,
                status: "failed",
                endDate: DateTime.UtcNow, null, null);
            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, default));
        }
    }
}
