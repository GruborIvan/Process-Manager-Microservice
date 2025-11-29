using FluentValidation;
using Moq;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProcessManager.Domain.Exceptions;
using Xunit;
using Rebus.Pipeline;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class StartProcessCommandHandlerTests
    {
        private readonly Mock<StartProcessCommandValidator> _mockValidator = new Mock<StartProcessCommandValidator>();
        private readonly Mock<IFeatureFlagService> _mockFeatureFlagService = new Mock<IFeatureFlagService>();

        public StartProcessCommandHandlerTests()
        {
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<StartProcessCommand>()))
               .Verifiable();
        }

        [Fact]
        public async Task ValidCommand_StartsProcess_Succeeds()
        {
            await Task.Run(() =>
            {
                // Arrange
                var mockProcessManager = new Mock<IProcessService>();
                mockProcessManager.Setup(
                        service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                    .ReturnsAsync(new Process())
                    .Verifiable();
                mockProcessManager.Setup(
                        service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(Guid.NewGuid)
                    .Verifiable();

                Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
                _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

                var mockUnitOfWork = new Mock<IUnitOfWork>();
                mockUnitOfWork.Setup(x => x.WorkflowRepository.AddAsync(It.IsAny<WorkflowRun>(), It.IsAny<IEnumerable<Relation>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new WorkflowRun(Guid.NewGuid(), "test", "in progress", "", null))
                    .Verifiable();
                mockUnitOfWork.Setup(x => x.WorkflowRepository.CheckIfExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false)
                    .Verifiable();
                mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Verifiable();

                // add setup for repo

                var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

                var command = new StartProcessCommand("key", string.Empty, Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), default, default);

                // Act
                Func<Task> testCode = async () =>
                {
                    await handler.Handle(command, new CancellationToken());
                };

                var actionResult = Record.ExceptionAsync(testCode);

                // Assert
                Assert.Null(actionResult.Exception);
                Assert.Equal("RanToCompletion", actionResult.Status.ToString());

                mockProcessManager.Verify();
            });
        }

        [Fact]
        public async Task ValidCommand_GetProcessWithMessageAsync_Throws_ArgumentException()
        {
            // Arrange
            var mockProcessManager = new Mock<IProcessService>();
            mockProcessManager.Setup(
                    service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .Throws(new ArgumentException())
                .Verifiable();
            mockProcessManager.Setup(
                    service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(x => x.WorkflowRepository.CheckIfExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            var command = new StartProcessCommand("key", "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), default, default);

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));

            mockProcessManager.Verify();
        }

        [Fact]
        public async Task ValidCommand_GetPrincipalIdAsync_Throws_ArgumentException()
        {
            // Arrange
            var mockProcessManager = new Mock<IProcessService>();
            mockProcessManager.Setup(
                    service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new Process())
                .Verifiable();
            mockProcessManager.Setup(
                    service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException())
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(x => x.WorkflowRepository.CheckIfExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            var command = new StartProcessCommand("key", "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), default, default);

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));

            mockProcessManager.Verify(x => x.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Never);
            mockProcessManager.Verify(x => x.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ValidCommand_ProcessService_Null_Throws_NullReferenceException()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(null, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            var command = new StartProcessCommand("key", "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task ValidCommand_Repository_Null_Throws_NullReferenceException()
        {
            // Arrange
            var mockProcessManager = new Mock<IProcessService>();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, null, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            var command = new StartProcessCommand("key", "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));

            mockProcessManager.Verify();
        }

        [Fact]
        public async Task ValidCommand_Validator_Null_Throws_NullReferenceException()
        {
            // Arrange
            var mockProcessManager = new Mock<IProcessService>();

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, null, _contextAccessor.Object);

            var command = new StartProcessCommand("key", "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null);

            // Act
            // Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task InvalidCommand_Throws_ValidationException()
        {
            // Arrange
            Process process = null;
            var invalidCommand = new StartProcessCommand(string.Empty, string.Empty, Guid.Empty, new { }, Guid.Empty, null, null);

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<StartProcessCommand>()))
                .Throws(new ValidationException(string.Empty));

            var mockProcessManager = new Mock<IProcessService>();
            mockProcessManager.Setup(
                    service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(process)
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task StartProcess_GetProcessWithMessage_Fails_Throws()
        {
            // Arrange
            var mockProcessManager = new Mock<IProcessService>();
            mockProcessManager.Setup(
                    service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception())
                .Verifiable();
            mockProcessManager.Setup(
                    service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(x => x.WorkflowRepository.CheckIfExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            var contextAccessor = new Mock<IContextAccessor>();
            contextAccessor.Setup(x => x.GetRequestId()).Returns(Guid.NewGuid());

            var mockMsgContext = new Mock<IMessageContext>();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            // Assert
            var command = new StartProcessCommand("key", "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null);

            await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task StartProcess_GetProcessWithEmptyCommand_Throws_Validation_Exception()
        {
            // Arrange
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<StartProcessCommand>()))
                .Throws(new ArgumentNullException());

            var mockProcessManager = new Mock<IProcessService>();
            mockProcessManager.Setup(
                    service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception())
                .Verifiable();
            mockProcessManager.Setup(
                    service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            // Assert
            StartProcessCommand command = null;
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task StartProcess_GetProcessWithMessage_Fails_Throws_Validation_Exception()
        {
            // Arrange
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<StartProcessCommand>()))
                .Throws(new ValidationException(string.Empty));

            var mockProcessManager = new Mock<IProcessService>();
            mockProcessManager.Setup(service => service.GetProcessWithMessageAsync(null, null, It.IsAny<object>(), It.IsAny<string>()));
            mockProcessManager.Setup(
                    service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            // Assert
            var command = new StartProcessCommand("key", null, Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), null, null);
            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task StartsProcess_WorlflowExists_ProcessAlreadyStartedException()
        {
            // Arrange
            var mockProcessManager = new Mock<IProcessService>();
            mockProcessManager.Setup(
                    service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new Process())
                .Verifiable();
            mockProcessManager.Setup(
                    service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
                .Verifiable();

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(x => x.WorkflowRepository.AddAsync(It.IsAny<WorkflowRun>(), It.IsAny<IEnumerable<Relation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkflowRun(Guid.NewGuid(), "test", "in progress", "", null))
                .Verifiable();
            mockUnitOfWork.Setup(x => x.WorkflowRepository.CheckIfExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .Verifiable();
            mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Verifiable();

            Mock<IContextAccessor> _contextAccessor = new Mock<IContextAccessor>();
            _contextAccessor.Setup(c => c.GetRequestId()).Returns(Guid.NewGuid());

            var handler = new StartProcessCommandHandler(mockProcessManager.Object, mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockValidator.Object, _contextAccessor.Object);

            var command = new StartProcessCommand("key", "process", Guid.NewGuid(), new { Amount = 108, Message = "Hello" }, Guid.NewGuid(), default, default);

            // Assert
            await Assert.ThrowsAsync<ProcessAlreadyStartedException>(() => handler.Handle(command, It.IsAny<CancellationToken>()));
        }
    }
}
