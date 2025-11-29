using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Moq;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class WorkflowRepositoryTests
    {
        private readonly DbContextOptions<ProcesManagerDbContext> _options =
            new DbContextOptionsBuilder<ProcesManagerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        private readonly IMapper _mapper;
        private readonly Mock<IMediator> _mediatorMock;

        public WorkflowRepositoryTests()
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<WorkflowRunDbo, WorkflowRun>().ReverseMap();
                cfg.CreateMap<RelationDbo, Relation>().ReverseMap();
            }).CreateMapper();
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task GetByOperationIdAsync_Returns_WorkflowRun()
        {
            // Arange  
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = "started",
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow
            };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.Add(expectedDbo);
            context.SaveChanges();

            var repository = new WorkflowRepository(context, _mapper);

            // Act 
            var workflow = await repository.GetAsync(expectedOperationId);

            // Assert  
            Assert.NotNull(workflow);
            Assert.Equal(expectedDbo.OperationId, workflow.OperationId);
        }

        [Fact]
        public async Task GetByOperationIdAsync_Returns_Null()
        {
            await Task.Run(() =>
            {
                // Arange  
                var expectedOperationId = Guid.NewGuid();
                var expectedDbo = new WorkflowRunDbo
                {
                    OperationId = expectedOperationId,
                    WorkflowRunName = "test",
                    Status = "started",
                    CreatedBy = "test",
                    CreatedDate = DateTime.UtcNow,
                    ChangedBy = "test",
                    ChangedDate = DateTime.UtcNow
                };
                using var context = new ProcesManagerDbContext(_options);
                context.WorkflowRuns.Add(expectedDbo);
                context.SaveChanges();

                var repository = new WorkflowRepository(context, _mapper);

                // Act 
                var error = Record.ExceptionAsync(async () => await repository.GetAsync(Guid.NewGuid()));

                // Assert  
                Assert.NotNull(error.Result);
                Assert.IsType<WorkflowNotFoundException>(error.Result);
            });
        }

        [Fact]
        public async Task AddAsync_Returns_WorkflowRun()
        {
            // Arange  
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = "started",
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow
            };
            using var context = new ProcesManagerDbContext(_options);

            var repository = new WorkflowRepository(context, _mapper);

            // Act 
            var workflow = await repository.AddAsync(_mapper.Map<WorkflowRun>(expectedDbo), default);

            // Assert  
            Assert.NotNull(workflow);
            Assert.Equal(expectedDbo.OperationId, workflow.OperationId);
        }

        [Fact]
        public async Task AddAsync_WithRelations_Returns_WorkflowRun()
        {
            // Arange  
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = "started",
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow,
            };

            var entityId = Guid.NewGuid();

            var expectedRelations = new List<Relation>
            {
                new Relation
                {
                    EntityId = entityId,
                    EntityType = "Person"
                },
                new Relation
                {
                    EntityId = Guid.NewGuid(),
                    EntityType = "Loan"
                },
            };

            using var context = new ProcesManagerDbContext(_options);

            context.Relations.Add(new RelationDbo
            {
                EntityId = entityId,
                EntityType = "Person"
            });
            context.SaveChanges();

            var unitOfWork = new UnitOfWork(context, null, null, null, new WorkflowRepository(context, _mapper), null, _mediatorMock.Object);

            // Act 
            await unitOfWork.WorkflowRepository.AddAsync(_mapper.Map<WorkflowRun>(expectedDbo), _mapper.Map<IEnumerable<Relation>>(expectedRelations));
            await unitOfWork.SaveChangesAsync();
            var workflow = context.WorkflowRuns.ToList();
            var relations = context.Relations.ToList();
            // Assert  
            Assert.NotNull(workflow);
            Assert.Equal(expectedDbo.OperationId, workflow.First().OperationId);
            Assert.Equal(expectedRelations.Count, relations.Count);
        }

        [Fact]
        public async Task UpdateAsync_Returns_WorkflowRun()
        {
            // Arange  
            var startingStatus = "started";
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = startingStatus,
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow
            };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.Add(expectedDbo);
            context.SaveChanges();

            var unitOfWork = new UnitOfWork(context, null, null, null, new WorkflowRepository(context, _mapper), null, _mediatorMock.Object);

            var existingRun = _mapper.Map<WorkflowRun>(expectedDbo);
            existingRun.UpdateWorkflowRun("succeeded", DateTime.Now, null, null);

            // Act 
            unitOfWork.WorkflowRepository.Update(existingRun);
            await unitOfWork.SaveChangesAsync();
            var workflow = context.WorkflowRuns.Single();
            // Assert  
            Assert.NotNull(workflow);
            Assert.Equal(expectedDbo.OperationId, workflow.OperationId);
            Assert.NotEqual(startingStatus, workflow.Status);
        }

        [Fact]
        public async Task UpdateAsync_WithRelations_Returns_WorkflowRun()
        {
            // Arange  
            var startingStatus = "started";
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = startingStatus,
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow,
                WorkflowRelations = new List<WorkflowRelationDbo>
                {
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test",
                        CreatedDate = DateTime.UtcNow,
                        ChangedBy = "test",
                        ChangedDate = DateTime.UtcNow,
                        OperationId = expectedOperationId,
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    }
                }
            };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.Add(expectedDbo);
            context.SaveChanges();

            var unitOfWork = new UnitOfWork(context, null, null, null, new WorkflowRepository(context, _mapper), null, _mediatorMock.Object);

            // Act 
            var existingRun = _mapper.Map<WorkflowRun>(expectedDbo);
            existingRun.UpdateWorkflowRun("succeeded", DateTime.Now, null, null);
            unitOfWork.WorkflowRepository.Update(existingRun);
            await unitOfWork.SaveChangesAsync();
            var workflow = context.WorkflowRuns.Single();
            var relations = context.WorkflowRelations.ToList();

            // Assert  
            Assert.NotNull(workflow);
            Assert.Equal(expectedDbo.OperationId, workflow.OperationId);
            Assert.Single(relations);
            Assert.NotEqual(startingStatus, workflow.Status);
        }

        [Fact]
        public async Task CheckIfExists_Returns_True()
        {
            // Arange  
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = "started",
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow
            };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.Add(expectedDbo);
            context.SaveChanges();

            var repository = new WorkflowRepository(context, _mapper);

            // Act 
            var workflowExists = await repository.CheckIfExists(expectedOperationId);

            // Assert  
            Assert.True(workflowExists);
        }

        [Fact]
        public async Task CheckIfExists_Returns_False()
        {
            // Arange  
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = "started",
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow
            };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.Add(expectedDbo);
            context.SaveChanges();

            var repository = new WorkflowRepository(context, _mapper);

            // Act 
            var workflowExists = await repository.CheckIfExists(Guid.NewGuid());

            // Assert  
            Assert.False(workflowExists);
        }

        [Fact]
        public async Task GetAsync_Returns_Workflow()
        {
            // Arange  
            var expectedOperationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "test",
                Status = "started",
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow
            };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.Add(expectedDbo);
            context.SaveChanges();

            var repository = new WorkflowRepository(context, _mapper);

            // Act 
            var workflow = await repository.GetAsync(expectedDbo.OperationId);

            // Assert  
            Assert.NotNull(workflow);
            Assert.Equal(expectedDbo.OperationId, workflow.OperationId);
        }

        [Fact]
        public async Task GetAsync_Returns_Null()
        {
            await Task.Run(() =>
            {
                // Arange  
                var nonExistentOperationId = Guid.NewGuid();
                using var context = new ProcesManagerDbContext(_options);

                var repository = new WorkflowRepository(context, _mapper);

                // Act 
                var error = Record.ExceptionAsync(async () => await repository.GetAsync(nonExistentOperationId));

                // Assert  
                Assert.NotNull(error.Result);
                Assert.IsType<WorkflowNotFoundException>(error.Result);
            });
        }

        [Fact]
        public void WorkflowRepository_DbContext_Null_Throws()
        {
            var error = Assert.Throws<ArgumentNullException>(() => new WorkflowRepository(null, _mapper));

            Assert.Equal("dbContext", error.ParamName);
        }

        [Fact]
        public void Update_WorkflowNotFoundException()
        {
            //Arrange
            using var context = new ProcesManagerDbContext(_options);

            var unitOfWork = new UnitOfWork(new ProcesManagerDbContext(_options), null, null, null, new WorkflowRepository(context, _mapper), null, _mediatorMock.Object);

            // Act 
            var error = Assert.Throws<WorkflowNotFoundException>(() => unitOfWork.WorkflowRepository.Update(new WorkflowRun(Guid.Empty, "", "", "", null)));

            // Assert  
            Assert.NotNull(error);
        }
    }
}
