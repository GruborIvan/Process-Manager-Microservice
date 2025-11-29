using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Models.Reporting;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class ReportingRepositoryTests
    {
        private readonly DbContextOptions<ProcesManagerDbContext> _options =
            new DbContextOptionsBuilder<ProcesManagerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        private readonly IMapper _autoMapperMock;

        public ReportingRepositoryTests()
        {
            _autoMapperMock = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ActivityDbo, ActivityReport>();
                cfg.CreateMap<RelationDbo, RelationReport>();
                cfg.CreateMap<WorkflowRelationDbo, WorkflowRelationReport>();
                cfg.CreateMap<WorkflowRunDbo, WorkflowRunReport>();
            }).CreateMapper();
        }

        [Fact]
        public async Task GetActivitiesAsync_Returns_Activities()
        {
            // Arange  
            var activityDbos = new List<ActivityDbo>
                {
                    new ActivityDbo
                    {
                        ActivityId = Guid.NewGuid(),
                        OperationId = Guid.NewGuid(),
                        Name = "test1",
                        Status = "in progress",
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow,
                        URI = "test/test"
                    },
                    new ActivityDbo
                    {
                        ActivityId = Guid.NewGuid(),
                        OperationId = Guid.NewGuid(),
                        Name = "test2",
                        Status = "in progress",
                        StartDate = DateTime.UtcNow.AddDays(-2),
                        EndDate = null,
                        URI = "test/test2"
                    }
                };
            using var context = new ProcesManagerDbContext(_options);
            context.Activities.AddRange(activityDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var activities = await repository.GetActivitiesAsync(null, null);

            // Assert  
            Assert.NotNull(activities);
            Assert.Equal(2, activities.Count());

            var expectedActivity1 = activityDbos.Single(x => x.StartDate > DateTime.UtcNow.AddDays(-1));
            var expectedActivity2 = activityDbos.Single(x => x.StartDate < DateTime.UtcNow.AddDays(-1));
            var activityObject1 = activities.Single(x => x.StartDate > DateTime.UtcNow.AddDays(-1));
            var activityObject2 = activities.Single(x => x.StartDate < DateTime.UtcNow.AddDays(-1));

            Assert.Equal(expectedActivity1.ActivityId, activityObject1.ActivityId);
            Assert.Equal(expectedActivity1.Name, activityObject1.Name);
            Assert.Equal(expectedActivity1.Status, activityObject1.Status);
            Assert.Equal(expectedActivity1.URI, activityObject1.URI);
            Assert.Equal(expectedActivity2.ActivityId, activityObject2.ActivityId);
            Assert.Equal(expectedActivity2.Name, activityObject2.Name);
            Assert.Equal(expectedActivity2.Status, activityObject2.Status);
            Assert.Equal(expectedActivity2.URI, activityObject2.URI);
        }

        [Fact]
        public async Task GetActivitiesAsync_FromDateYesterday_Returns_OneActivity()
        {
            // Arange  
            var activityDbos = new List<ActivityDbo>
                {
                    new ActivityDbo
                    {
                        ActivityId = Guid.NewGuid(),
                        OperationId = Guid.NewGuid(),
                        Name = "test1",
                        Status = "in progress",
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow,
                        URI = "test/test"
                    },
                    new ActivityDbo
                    {
                        ActivityId = Guid.NewGuid(),
                        OperationId = Guid.NewGuid(),
                        Name = "test2",
                        Status = "in progress",
                        StartDate = DateTime.UtcNow.AddDays(-2),
                        EndDate = null,
                        URI = "test/test2"
                    }
                };
            using var context = new ProcesManagerDbContext(_options);
            context.Activities.AddRange(activityDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var activities = await repository.GetActivitiesAsync(DateTime.Now.AddDays(-1), null);

            // Assert  
            Assert.NotNull(activities);
            Assert.Single(activities);
            var expectedActivity = activityDbos.Single(x => x.StartDate > DateTime.UtcNow.AddDays(-1));

            Assert.Equal(expectedActivity.ActivityId, activities.Single().ActivityId);
            Assert.Equal(expectedActivity.Name, activities.Single().Name);
            Assert.Equal(expectedActivity.Status, activities.Single().Status);
            Assert.Equal(expectedActivity.URI, activities.Single().URI);
        }

        [Fact]
        public async Task GetActivitiesAsync_FromDateInPast_ToDateYesterday_Returns_OneActivity()
        {
            // Arange  
            var activityDbos = new List<ActivityDbo>
                {
                    new ActivityDbo
                    {
                        ActivityId = Guid.NewGuid(),
                        OperationId = Guid.NewGuid(),
                        Name = "test1",
                        Status = "in progress",
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow,
                        URI = "test/test"
                    },
                    new ActivityDbo
                    {
                        ActivityId = Guid.NewGuid(),
                        OperationId = Guid.NewGuid(),
                        Name = "test2",
                        Status = "in progress",
                        StartDate = DateTime.UtcNow.AddDays(-2),
                        EndDate = DateTime.UtcNow.AddDays(-2),
                        URI = "test/test2"
                    },
                    new ActivityDbo
                    {
                        ActivityId = Guid.NewGuid(),
                        OperationId = Guid.NewGuid(),
                        Name = "test2",
                        Status = "in progress",
                        StartDate = DateTime.UtcNow.AddDays(-2),
                        EndDate = DateTime.UtcNow,
                        URI = "test/test2"
                    }
                };
            using var context = new ProcesManagerDbContext(_options);
            context.Activities.AddRange(activityDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var activities = await repository.GetActivitiesAsync(DateTime.Now.AddDays(-50), DateTime.Now.AddDays(-1));

            // Assert  
            Assert.NotNull(activities);
            Assert.Single(activities);
            var expectedActivity = activityDbos.Single(x => x.StartDate < DateTime.UtcNow.AddDays(-1) && x.EndDate < DateTime.UtcNow.AddDays(-1));

            Assert.Equal(expectedActivity.ActivityId, activities.Single().ActivityId);
            Assert.Equal(expectedActivity.Name, activities.Single().Name);
            Assert.Equal(expectedActivity.Status, activities.Single().Status);
            Assert.Equal(expectedActivity.URI, activities.Single().URI);
        }

        [Fact]
        public async Task GetWorkflowRunsAsync_Returns_WorkflowRuns()
        {
            // Arange  
            var workflowRunDbos = new List<WorkflowRunDbo>
                {
                    new WorkflowRunDbo
                    {
                        OperationId = Guid.NewGuid(),
                        WorkflowRunName = "test",
                        Status = "started",
                        CreatedBy = "test",
                        CreatedDate = DateTime.UtcNow,
                        ChangedBy = "test",
                        ChangedDate = DateTime.UtcNow
                    },
                    new WorkflowRunDbo
                    {
                        OperationId = Guid.NewGuid(),
                        WorkflowRunName = "test2",
                        Status = "started",
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow.AddDays(-2)
                    },
                };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.AddRange(workflowRunDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var workflowRuns = await repository.GetWorkflowRunsAsync(null, null);

            // Assert  
            Assert.NotNull(workflowRuns);
            Assert.Equal(2, workflowRuns.Count());

            var expectedWorkflowRun1 = workflowRunDbos.Single(x => x.ChangedDate > DateTime.UtcNow.AddDays(-1));
            var expectedWorkflowRun2 = workflowRunDbos.Single(x => x.ChangedDate < DateTime.UtcNow.AddDays(-1));
            var workflowRunObject1 = workflowRuns.Single(x => x.ChangedDate > DateTime.UtcNow.AddDays(-1));
            var workflowRunObject2 = workflowRuns.Single(x => x.ChangedDate < DateTime.UtcNow.AddDays(-1));

            Assert.Equal(expectedWorkflowRun1.OperationId, workflowRunObject1.OperationId);
            Assert.Equal(expectedWorkflowRun1.WorkflowRunName, workflowRunObject1.WorkflowRunName);
            Assert.Equal(expectedWorkflowRun1.Status, workflowRunObject1.Status);
            Assert.Equal(expectedWorkflowRun1.CreatedBy, workflowRunObject1.CreatedBy);
            Assert.Equal(expectedWorkflowRun1.CreatedDate, workflowRunObject1.CreatedDate);
            Assert.Equal(expectedWorkflowRun1.ChangedBy, workflowRunObject1.ChangedBy);
            Assert.Equal(expectedWorkflowRun1.ChangedDate, workflowRunObject1.ChangedDate);

            Assert.Equal(expectedWorkflowRun2.OperationId, workflowRunObject2.OperationId);
            Assert.Equal(expectedWorkflowRun2.WorkflowRunName, workflowRunObject2.WorkflowRunName);
            Assert.Equal(expectedWorkflowRun2.Status, workflowRunObject2.Status);
            Assert.Equal(expectedWorkflowRun2.CreatedBy, workflowRunObject2.CreatedBy);
            Assert.Equal(expectedWorkflowRun2.CreatedDate, workflowRunObject2.CreatedDate);
            Assert.Equal(expectedWorkflowRun2.ChangedBy, workflowRunObject2.ChangedBy);
            Assert.Equal(expectedWorkflowRun2.ChangedDate, workflowRunObject2.ChangedDate);
        }

        [Fact]
        public async Task GetWorkflowRunsAsync_FromDateYesterday_Returns_OneWorkflowRun()
        {
            var workflowRunDbos = new List<WorkflowRunDbo>
                {
                    new WorkflowRunDbo
                    {
                        OperationId = Guid.NewGuid(),
                        WorkflowRunName = "test",
                        Status = "started",
                        CreatedBy = "test",
                        CreatedDate = DateTime.UtcNow,
                        ChangedBy = "test",
                        ChangedDate = DateTime.UtcNow
                    },
                    new WorkflowRunDbo
                    {
                        OperationId = Guid.NewGuid(),
                        WorkflowRunName = "test2",
                        Status = "started",
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow.AddDays(-2)
                    },
                };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.AddRange(workflowRunDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var workflowRuns = await repository.GetWorkflowRunsAsync(DateTime.Now.AddDays(-1), null);

            // Assert  
            Assert.NotNull(workflowRuns);
            Assert.Single(workflowRuns);
            var expectedWorkflowRun = workflowRunDbos.Single(x => x.ChangedDate > DateTime.UtcNow.AddDays(-1));

            Assert.Equal(expectedWorkflowRun.OperationId, workflowRuns.Single().OperationId);
            Assert.Equal(expectedWorkflowRun.WorkflowRunName, workflowRuns.Single().WorkflowRunName);
            Assert.Equal(expectedWorkflowRun.Status, workflowRuns.Single().Status);
            Assert.Equal(expectedWorkflowRun.CreatedBy, workflowRuns.Single().CreatedBy);
            Assert.Equal(expectedWorkflowRun.CreatedDate, workflowRuns.Single().CreatedDate);
            Assert.Equal(expectedWorkflowRun.ChangedBy, workflowRuns.Single().ChangedBy);
            Assert.Equal(expectedWorkflowRun.ChangedDate, workflowRuns.Single().ChangedDate);
        }

        [Fact]
        public async Task GetWorkflowRunsAsync_FromDateInPast_ToDateYesterday_Returns_OneWorkflowRun()
        {
            var workflowRunDbos = new List<WorkflowRunDbo>
                {
                    new WorkflowRunDbo
                    {
                        OperationId = Guid.NewGuid(),
                        WorkflowRunName = "test",
                        Status = "started",
                        CreatedBy = "test",
                        CreatedDate = DateTime.UtcNow,
                        ChangedBy = "test",
                        ChangedDate = DateTime.UtcNow
                    },
                    new WorkflowRunDbo
                    {
                        OperationId = Guid.NewGuid(),
                        WorkflowRunName = "test2",
                        Status = "started",
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new WorkflowRunDbo
                    {
                        OperationId = Guid.NewGuid(),
                        WorkflowRunName = "test2",
                        Status = "started",
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow
                    },
                };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRuns.AddRange(workflowRunDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var workflowRuns = await repository.GetWorkflowRunsAsync(DateTime.Now.AddDays(-50), DateTime.Now.AddDays(-1));

            // Assert  
            Assert.NotNull(workflowRuns);
            Assert.Single(workflowRuns);
            var expectedWorkflowRun = workflowRunDbos.Single(x => x.ChangedDate.Date == DateTime.UtcNow.AddDays(-2).Date);

            Assert.Equal(expectedWorkflowRun.OperationId, workflowRuns.Single().OperationId);
            Assert.Equal(expectedWorkflowRun.WorkflowRunName, workflowRuns.Single().WorkflowRunName);
            Assert.Equal(expectedWorkflowRun.Status, workflowRuns.Single().Status);
            Assert.Equal(expectedWorkflowRun.CreatedBy, workflowRuns.Single().CreatedBy);
            Assert.Equal(expectedWorkflowRun.CreatedDate, workflowRuns.Single().CreatedDate);
            Assert.Equal(expectedWorkflowRun.ChangedBy, workflowRuns.Single().ChangedBy);
            Assert.Equal(expectedWorkflowRun.ChangedDate, workflowRuns.Single().ChangedDate);
        }

        [Fact]
        public async Task GetWorkflowRelationsAsync_Returns_WorkflowRelations()
        {
            // Arange  
            var workflowRelationDbos = new List<WorkflowRelationDbo>
                {
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test",
                        CreatedDate = DateTime.UtcNow,
                        ChangedBy = "test",
                        ChangedDate = DateTime.UtcNow,
                        OperationId = Guid.NewGuid(),
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    },
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow.AddDays(-2),
                        OperationId = Guid.NewGuid(),
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    }
                };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRelations.AddRange(workflowRelationDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var workflowRelations = await repository.GetWorkflowRelationsAsync(null, null);

            // Assert  
            Assert.NotNull(workflowRelations);
            Assert.Equal(2, workflowRelations.Count());

            var expectedWorkflowRelation1 = workflowRelationDbos.Single(x => x.ChangedDate > DateTime.UtcNow.AddDays(-1));
            var expectedWorkflowRelation2 = workflowRelationDbos.Single(x => x.ChangedDate < DateTime.UtcNow.AddDays(-1));
            var workflowRelationObject1 = workflowRelations.Single(x => x.ChangedDate > DateTime.UtcNow.AddDays(-1));
            var workflowRelationObject2 = workflowRelations.Single(x => x.ChangedDate < DateTime.UtcNow.AddDays(-1));

            Assert.Equal(expectedWorkflowRelation1.OperationId, workflowRelationObject1.OperationId);
            Assert.Equal(expectedWorkflowRelation1.CreatedBy, workflowRelationObject1.CreatedBy);
            Assert.Equal(expectedWorkflowRelation1.CreatedDate, workflowRelationObject1.CreatedDate);
            Assert.Equal(expectedWorkflowRelation1.ChangedBy, workflowRelationObject1.ChangedBy);
            Assert.Equal(expectedWorkflowRelation1.ChangedDate, workflowRelationObject1.ChangedDate);
            Assert.Equal(expectedWorkflowRelation1.EntityId, workflowRelationObject1.EntityId);

            Assert.Equal(expectedWorkflowRelation2.OperationId, workflowRelationObject2.OperationId);
            Assert.Equal(expectedWorkflowRelation2.CreatedBy, workflowRelationObject2.CreatedBy);
            Assert.Equal(expectedWorkflowRelation2.CreatedDate, workflowRelationObject2.CreatedDate);
            Assert.Equal(expectedWorkflowRelation2.ChangedBy, workflowRelationObject2.ChangedBy);
            Assert.Equal(expectedWorkflowRelation2.ChangedDate, workflowRelationObject2.ChangedDate);
            Assert.Equal(expectedWorkflowRelation2.EntityId, workflowRelationObject2.EntityId);
        }

        [Fact]
        public async Task GetWorkflowRelationsAsync_FromDateYesterday_Returns_OneWorkflowRelation()
        {
            // Arange  
            var workflowRelationDbos = new List<WorkflowRelationDbo>
                {
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test",
                        CreatedDate = DateTime.UtcNow,
                        ChangedBy = "test",
                        ChangedDate = DateTime.UtcNow,
                        OperationId = Guid.NewGuid(),
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    },
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow.AddDays(-2),
                        OperationId = Guid.NewGuid(),
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    }
                };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRelations.AddRange(workflowRelationDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var workflowRelations = await repository.GetWorkflowRelationsAsync(DateTime.Now.AddDays(-1), null);

            // Assert  
            Assert.NotNull(workflowRelations);
            Assert.Single(workflowRelations);

            var expectedWorkflowRelation = workflowRelationDbos.Single(x => x.ChangedDate > DateTime.UtcNow.AddDays(-1));

            Assert.Equal(expectedWorkflowRelation.OperationId, workflowRelations.Single().OperationId);
            Assert.Equal(expectedWorkflowRelation.CreatedBy, workflowRelations.Single().CreatedBy);
            Assert.Equal(expectedWorkflowRelation.CreatedDate, workflowRelations.Single().CreatedDate);
            Assert.Equal(expectedWorkflowRelation.ChangedBy, workflowRelations.Single().ChangedBy);
            Assert.Equal(expectedWorkflowRelation.ChangedDate, workflowRelations.Single().ChangedDate);
            Assert.Equal(expectedWorkflowRelation.EntityId, workflowRelations.Single().EntityId);
        }

        [Fact]
        public async Task GetWorkflowRelationsAsync_FromDateInPast_ToDateYesterday_Returns_OneWorkflowRelation()
        {
            // Arange  
            var workflowRelationDbos = new List<WorkflowRelationDbo>
                {
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test",
                        CreatedDate = DateTime.UtcNow,
                        ChangedBy = "test",
                        ChangedDate = DateTime.UtcNow,
                        OperationId = Guid.NewGuid(),
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    },
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow.AddDays(-2),
                        OperationId = Guid.NewGuid(),
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    },
                    new WorkflowRelationDbo
                    {
                        CreatedBy = "test2",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        ChangedBy = "test2",
                        ChangedDate = DateTime.UtcNow,
                        OperationId = Guid.NewGuid(),
                        WorkflowRun = null,
                        EntityId = Guid.NewGuid(),
                        Relation = null
                    }
                };
            using var context = new ProcesManagerDbContext(_options);
            context.WorkflowRelations.AddRange(workflowRelationDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var workflowRelations = await repository.GetWorkflowRelationsAsync(DateTime.Now.AddDays(-50), DateTime.Now.AddDays(-1));

            // Assert  
            Assert.NotNull(workflowRelations);
            Assert.Single(workflowRelations);

            var expectedWorkflowRelation = workflowRelationDbos.Single(x => x.ChangedDate.Date == DateTime.UtcNow.AddDays(-2).Date);

            Assert.Equal(expectedWorkflowRelation.OperationId, workflowRelations.Single().OperationId);
            Assert.Equal(expectedWorkflowRelation.CreatedBy, workflowRelations.Single().CreatedBy);
            Assert.Equal(expectedWorkflowRelation.CreatedDate, workflowRelations.Single().CreatedDate);
            Assert.Equal(expectedWorkflowRelation.ChangedBy, workflowRelations.Single().ChangedBy);
            Assert.Equal(expectedWorkflowRelation.ChangedDate, workflowRelations.Single().ChangedDate);
            Assert.Equal(expectedWorkflowRelation.EntityId, workflowRelations.Single().EntityId);
        }

        [Fact]
        public async Task GetRelationsAsync_Returns_Relations()
        {
            // Arange  
            var relationDbos = new List<RelationDbo>
            {
                new RelationDbo
                {
                    EntityId = Guid.NewGuid(),
                    EntityType = "Person"
                },
                new RelationDbo
                {
                    EntityId = Guid.NewGuid(),
                    EntityType = "Loan"
                },
            };
            using var context = new ProcesManagerDbContext(_options);
            context.Relations.AddRange(relationDbos);
            context.SaveChanges();

            var repository = new ReportingRepository(context, _autoMapperMock);

            // Act 
            var relations = await repository.GetRelationsAsync();

            // Assert  
            Assert.NotNull(relations);
            Assert.Equal(2, relations.Count());
        }

        [Fact]
        public void ReportingRepository_Invalid_DbContext_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => { new ReportingRepository(null, _autoMapperMock); });
        }
    }
}
