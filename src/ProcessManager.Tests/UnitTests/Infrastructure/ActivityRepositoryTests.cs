using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Moq;
using ProcessManager.Domain.Models;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class ActivityRepositoryTests
    {
        private const string _inProgressStatus = "in progress";
        private const string _completedStatus = "completed";

        private readonly DbContextOptions<ProcesManagerDbContext> _options =
            new DbContextOptionsBuilder<ProcesManagerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        private readonly IMapper _mapper;
        private readonly Mock<IMediator> _mediatorMock;

        public ActivityRepositoryTests()
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ActivityDbo, Activity>().ReverseMap();
                cfg.CreateMap<WorkflowRunDbo, WorkflowRun>().ReverseMap();
                cfg.CreateMap<RelationDbo, Relation>().ReverseMap();
            }).CreateMapper();
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task GetAsync_Returns_Activity()
        {
            // Arange  
            var expectedActivityId = Guid.NewGuid();
            var expectedDbo = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = Guid.NewGuid(),
                Name = "test",
                Status = _inProgressStatus,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                URI = "test/test"
            };
            using var context = new ProcesManagerDbContext(_options);
            context.Activities.Add(expectedDbo);
            context.SaveChanges();

            var repository = new ActivityRepository(context, _mapper);

            // Act 
            var activity = await repository.GetAsync(expectedActivityId);

            // Assert  
            Assert.NotNull(activity);
            Assert.Equal(expectedDbo.ActivityId, activity.ActivityId);
            Assert.Equal(expectedDbo.Name, activity.Name);
            Assert.Equal(expectedDbo.Status, activity.Status);
            Assert.Equal(expectedDbo.URI, activity.URI);
        }

        [Fact]
        public async Task GetAsync_Returns_Null()
        {
            await Task.Run(() =>
            {
                // Arange  
                var nonExistentActivityId = Guid.NewGuid();
                using var context = new ProcesManagerDbContext(_options);

                var repository = new ActivityRepository(context, _mapper);

                // Act 
                var error = Record.ExceptionAsync(async () => await repository.GetAsync(nonExistentActivityId));

                // Assert  
                Assert.NotNull(error.Result);
                Assert.IsType<ActivityNotFoundException>(error.Result);
            });
        }

        [Fact]
        public async Task AddAsync_Returns_Activity()
        {
            // Arange  
            var expectedActivityId = Guid.NewGuid();
            var command = new StartActivityCommand(Guid.NewGuid(), expectedActivityId, "test", DateTime.UtcNow, "test");
            var activity = new Activity(
                command.ActivityId,
                Guid.NewGuid(),
                "Test",
                _completedStatus,
                command.URI,
                DateTime.UtcNow,
                null,
                new WorkflowRun(
                    default,
                    null,
                    null,
                    null,
                    null));

            using var context = new ProcesManagerDbContext(_options);
            var unitOfWork = new UnitOfWork(context, new ActivityRepository(context, _mapper), null, null, null, null, _mediatorMock.Object);

            // Act 
            await unitOfWork.ActivityRepository.AddAsync(activity, default);
            await unitOfWork.SaveChangesAsync();
            var newActivity = context.Activities.Single();

            // Assert  
            Assert.NotNull(newActivity);
            Assert.Equal(activity.ActivityId, newActivity.ActivityId);
            Assert.Equal(activity.Name, newActivity.Name);
            Assert.Equal(activity.Status, newActivity.Status);
        }

        [Fact]
        public async Task UpdateAsync_Returns_WorkflowRun()
        {
            // Arange  
            var expectedActivityId = Guid.NewGuid();
            var dboToSave = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = Guid.NewGuid(),
                Name = "test",
                Status = _inProgressStatus,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                URI = "test/test"
            };
            using var context = new ProcesManagerDbContext(_options);
            context.Activities.Add(dboToSave);
            context.SaveChanges();

            var unitOfWork = new UnitOfWork(context, new ActivityRepository(context, _mapper), null, null, null, null, _mediatorMock.Object);
            var existingActivity = _mapper.Map<Activity>(dboToSave);
            existingActivity.UpdateActivity(_completedStatus, "test/test2");

            // Act 
            unitOfWork.ActivityRepository.Update(existingActivity);
            await unitOfWork.SaveChangesAsync();
            var activity = context.Activities.Single();

            // Assert  
            Assert.NotNull(activity);
            Assert.Equal(dboToSave.ActivityId, activity.ActivityId);
            Assert.Equal(_completedStatus, activity.Status);
            Assert.Equal("test/test2", activity.URI);
            Assert.Equal(dboToSave.Name, activity.Name);
        }

        [Fact]
        public async Task UpdateAsync_With_UpdateActivityCommand_Returns_WorkflowRun()
        {
            // Arange  
            var expectedActivityId = Guid.NewGuid();
            var dboToSave = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = Guid.NewGuid(),
                Name = "test",
                Status = _inProgressStatus,
                StartDate = DateTime.UtcNow,
                EndDate = null,
                URI = "test/test"
            };
            using var context = new ProcesManagerDbContext(_options);
            context.Activities.Add(dboToSave);
            context.SaveChanges();

            var unitOfWork = new UnitOfWork(context, new ActivityRepository(context, _mapper), null, null, null, null, _mediatorMock.Object);

            // Act 
            var existingActivity = _mapper.Map<Activity>(dboToSave);
            existingActivity.UpdateActivity(_completedStatus, "test/test2");
            unitOfWork.ActivityRepository.Update(existingActivity);
            await unitOfWork.SaveChangesAsync();
            var activity = context.Activities.Single();

            // Assert  
            Assert.NotNull(activity);
            Assert.Equal(dboToSave.ActivityId, activity.ActivityId);
            Assert.Equal(_completedStatus, activity.Status);
            Assert.Equal("test/test2", activity.URI);
            Assert.Equal(dboToSave.Name, activity.Name);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingActivity_Throws_ActivityNotFoundException()
        {
            await Task.Run(() =>
            {
                // Arange  
                var nonExistentActivityId = Guid.NewGuid();
                var dboToSave = new ActivityDbo
                {
                    ActivityId = nonExistentActivityId,
                    OperationId = Guid.NewGuid(),
                    Name = "test",
                    Status = _inProgressStatus,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                    URI = "test/test"
                };

                using var context = new ProcesManagerDbContext(_options);
                var unitOfWork = new UnitOfWork(context, new ActivityRepository(context, _mapper), null, null, null, null, _mediatorMock.Object);

                // Act 
                var error = Record.Exception(()
                    => unitOfWork.ActivityRepository.Update(_mapper.Map<Activity>(dboToSave)));

                // Assert  
                Assert.NotNull(error);
                Assert.IsType<ActivityNotFoundException>(error);
            });
        }

        [Fact]
        public async Task UpdateAsync_With_UpdateActivityCommand_Throws_ActivityNotFoundException()
        {  
            await Task.Run(() =>
            {
                // Arange
                var dboToSave = new ActivityDbo
                 {
                     ActivityId = Guid.NewGuid(),
                     OperationId = Guid.NewGuid(),
                     Name = "test",
                     Status = _inProgressStatus,
                     StartDate = DateTime.UtcNow,
                     EndDate = null,
                     URI = "test/test"
                 };
                 using var context = new ProcesManagerDbContext(_options);

                 var unitOfWork = new UnitOfWork(context, new ActivityRepository(context, _mapper), null, null, null, null, _mediatorMock.Object);

                 // Act 
                 Assert.Throws<ActivityNotFoundException>(() => unitOfWork.ActivityRepository.Update(_mapper.Map<Activity>(dboToSave)));
            });
        }

        [Fact]
        public void ActivityRepository_DbContext_Null_Throws()
        {
            var error = Assert.Throws<ArgumentNullException>(() => new ActivityRepository(null, _mapper));

            Assert.Equal("dbContext", error.ParamName);
        }
    }
}
