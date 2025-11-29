using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Repository;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class OutboxRepositoryTests
    {
        private readonly DbContextOptions<ProcesManagerDbContext> _options =
            new DbContextOptionsBuilder<ProcesManagerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        private readonly IMapper _mapper;

        public OutboxRepositoryTests()
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<OutboxMessageDbo, OutboxMessage>().ReverseMap();
            }).CreateMapper();
        }

        [Fact]
        public async Task CheckIfExists_Returns_True()
        {
            // Arange  
            var expectedMessageId = Guid.NewGuid();
            var expectedDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = expectedMessageId,
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.EventGrid,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            using var context = new ProcesManagerDbContext(_options);
            context.OutboxMessages.Add(expectedDbo);
            context.SaveChanges();

            var repository = new OutboxRepository(context, _mapper);

            // Act 
            var outboxMessageExists = await repository.CheckIfExists(expectedMessageId);

            // Assert  
            Assert.True(outboxMessageExists);
        }

        [Fact]
        public async Task GetUnprocessedEventsAsync_Returns_OutboxMessages()
        {
            // Arange  
            var expectedMessageId = Guid.NewGuid();
            var expectedDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = expectedMessageId,
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.EventGrid,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            var expectedMessageId2 = Guid.NewGuid();
            var expectedDbo2 = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = expectedMessageId2,
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.EventGrid,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            var laMessageId = Guid.NewGuid();
            var laDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = laMessageId,
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            var processedMessage = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.EventGrid,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = DateTime.Now
            };
            using var context = new ProcesManagerDbContext(_options);
            context.OutboxMessages.AddRange(expectedDbo, expectedDbo2, processedMessage, laDbo);
            context.SaveChanges();

            var repository = new OutboxRepository(context, _mapper);

            // Act 
            var outboxMessages = await repository.GetUnprocessedEventsAsync();

            // Assert  
            Assert.NotNull(outboxMessages);
            Assert.Equal(2, outboxMessages.Count());
            Assert.Equal(expectedDbo.MessageId, outboxMessages.Single(x => x.MessageId == expectedMessageId).MessageId);
            Assert.Equal(expectedDbo.CreatedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId).CreatedDate);
            Assert.Equal(expectedDbo.Type, outboxMessages.Single(x => x.MessageId == expectedMessageId).Type);
            Assert.Equal(expectedDbo.Data, outboxMessages.Single(x => x.MessageId == expectedMessageId).Data);
            Assert.Equal(expectedDbo.ProcessedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId).ProcessedDate);
            Assert.Equal(expectedDbo2.MessageId, outboxMessages.Single(x => x.MessageId == expectedMessageId2).MessageId);
            Assert.Equal(expectedDbo2.CreatedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId2).CreatedDate);
            Assert.Equal(expectedDbo2.Type, outboxMessages.Single(x => x.MessageId == expectedMessageId2).Type);
            Assert.Equal(expectedDbo2.Data, outboxMessages.Single(x => x.MessageId == expectedMessageId2).Data);
            Assert.Equal(expectedDbo2.ProcessedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId2).ProcessedDate);
        }

        [Fact]
        public async Task GetLogicAppStartMessagesAsync_Returns_OutboxMessages()
        {
            // Arange  
            var expectedMessageId = Guid.NewGuid();
            var expectedDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = expectedMessageId,
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            var expectedMessageId2 = Guid.NewGuid();
            var expectedDbo2 = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = expectedMessageId2,
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            var eventMessageId = Guid.NewGuid();
            var eventDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = eventMessageId,
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.EventGrid,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            var processedMessage = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = DateTime.Now
            };
            using var context = new ProcesManagerDbContext(_options);
            context.OutboxMessages.AddRange(expectedDbo, expectedDbo2, processedMessage, eventDbo);
            context.SaveChanges();

            var repository = new OutboxRepository(context, _mapper);

            // Act 
            var outboxMessages = await repository.GetLogicAppStartMessagesAsync();

            // Assert  
            Assert.NotNull(outboxMessages);
            Assert.Equal(2, outboxMessages.Count());
            Assert.Equal(expectedDbo.MessageId, outboxMessages.Single(x => x.MessageId == expectedMessageId).MessageId);
            Assert.Equal(expectedDbo.CreatedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId).CreatedDate);
            Assert.Equal(expectedDbo.Type, outboxMessages.Single(x => x.MessageId == expectedMessageId).Type);
            Assert.Equal(expectedDbo.Data, outboxMessages.Single(x => x.MessageId == expectedMessageId).Data);
            Assert.Equal(expectedDbo.ProcessedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId).ProcessedDate);
            Assert.Equal(expectedDbo2.MessageId, outboxMessages.Single(x => x.MessageId == expectedMessageId2).MessageId);
            Assert.Equal(expectedDbo2.CreatedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId2).CreatedDate);
            Assert.Equal(expectedDbo2.Type, outboxMessages.Single(x => x.MessageId == expectedMessageId2).Type);
            Assert.Equal(expectedDbo2.Data, outboxMessages.Single(x => x.MessageId == expectedMessageId2).Data);
            Assert.Equal(expectedDbo2.ProcessedDate, outboxMessages.Single(x => x.MessageId == expectedMessageId2).ProcessedDate);
        }

        [Fact]
        public async Task AddAsync_Returns_OutboxMessage()
        {
            // Arange  
            var expectedMessageId = Guid.NewGuid();
            var expectedDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = expectedMessageId,
                Type = OutboxMessageType.EventGrid,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            using var context = new ProcesManagerDbContext(_options);

            var unitOfWork = new UnitOfWork(context, null, new OutboxRepository(context, _mapper), null, null, null, null);

            // Act 
            await unitOfWork.OutboxRepository.AddAsync(expectedDbo.MessageId, expectedDbo.Type, expectedDbo.Data);
            await unitOfWork.SaveChangesAsync();
            var outboxMessage = context.OutboxMessages.Single();

            // Assert  
            Assert.NotNull(outboxMessage);
            Assert.Equal(expectedDbo.MessageId, outboxMessage.MessageId);
            Assert.Equal(expectedDbo.Type, outboxMessage.Type);
            Assert.Equal(expectedDbo.Data, outboxMessage.Data);
            Assert.Equal(expectedDbo.ProcessedDate, outboxMessage.ProcessedDate);
        }

        [Fact]
        public async Task Update_ProcessMessage_Returns_OutboxMessage()
        {
            // Arange  
            var expectedOutboxMessageId = Guid.NewGuid();
            var expectedDbo = new OutboxMessageDbo
            {
                OutboxMessageId = expectedOutboxMessageId,
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Type = OutboxMessageType.EventGrid,
                Data = "{\"testData\" : \"exampe json\"}",
                ProcessedDate = null
            };
            using var context = new ProcesManagerDbContext(_options);
            context.OutboxMessages.Add(expectedDbo);
            context.SaveChanges();

            var unitOfWork = new UnitOfWork(context, null, new OutboxRepository(context, _mapper), null, null, null, null);

            // Act 
            var outboxMessageChanged = _mapper.Map<OutboxMessage>(expectedDbo);
            outboxMessageChanged.ProcessMessage();

            unitOfWork.OutboxRepository.Update(outboxMessageChanged);
            await unitOfWork.SaveChangesAsync();
            var outboxMessage = context.OutboxMessages.Single();

            // Assert  
            Assert.NotNull(outboxMessage);
            Assert.Equal(expectedDbo.OutboxMessageId, outboxMessage.OutboxMessageId);
            Assert.Equal(expectedDbo.MessageId, outboxMessage.MessageId);
            Assert.Equal(expectedDbo.CreatedDate, outboxMessage.CreatedDate);
            Assert.Equal(expectedDbo.Type, outboxMessage.Type);
            Assert.Equal(expectedDbo.Data, outboxMessage.Data);
            Assert.Equal(DateTime.UtcNow.Date, outboxMessage.ProcessedDate.Value.Date);
        }
    }
}
