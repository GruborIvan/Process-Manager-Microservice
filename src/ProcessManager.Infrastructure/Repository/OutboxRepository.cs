using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;

namespace ProcessManager.Infrastructure.Repository
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly ProcesManagerDbContext _dbContext;
        private readonly IMapper _mapper;

        public OutboxRepository(ProcesManagerDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper;
        }

        public async Task<OutboxMessage> AddAsync(Guid messageId, OutboxMessageType type, string data, CancellationToken ct = default)
        {
            var newMessage = (await _dbContext.OutboxMessages.AddAsync(new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = messageId,
                Data = data,
                Type = type
            }, ct)).Entity;

            return _mapper.Map<OutboxMessage>(newMessage);
        }

        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedEventsAsync(CancellationToken ct = default)
        {
            var unprocessedEvents = await _dbContext
                .OutboxMessages
                .Where(x => x.ProcessedDate == null && (x.Type == OutboxMessageType.EventGrid || x.Type == OutboxMessageType.EventHub))
                .AsNoTracking()
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<OutboxMessage>>(unprocessedEvents);
        }

        public async Task<bool> CheckIfExists(Guid messageId, CancellationToken ct = default)
        {
            return await _dbContext
                .OutboxMessages
                .AsNoTracking()
                .AnyAsync(x => x.MessageId == messageId, ct);
        }

        public async Task<IEnumerable<OutboxMessage>> GetLogicAppStartMessagesAsync(CancellationToken ct = default)
        {
            var logicApps = await _dbContext
                .OutboxMessages
                .Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.LogicApp && ((x.NextRetryDate != null && x.NextRetryDate <= DateTime.UtcNow) || x.NextRetryDate == null))
                .AsNoTracking()
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<OutboxMessage>>(logicApps);
        }

        public void Update(OutboxMessage outboxMessage)
        {
            var outboxMessageDbo = _dbContext
                                  .OutboxMessages
                                  .First(x => x.OutboxMessageId == outboxMessage.OutboxMessageId);

            outboxMessageDbo.ProcessedDate = outboxMessage.ProcessedDate;
            outboxMessageDbo.NextRetryDate = outboxMessage.NextRetryDate;
            outboxMessageDbo.RetryAttempt = outboxMessage.RetryAttempt;
            outboxMessageDbo.Type = outboxMessage.Type;
            outboxMessageDbo.Data = outboxMessage.Data;
            outboxMessageDbo.DomainEvents = outboxMessage.DomainEvents;
        }

        public async Task DeleteRangeOlderThanAsync(int retentionDays, CancellationToken ct = default)
        {
            var messages = await _dbContext
                .OutboxMessages
                .Where(x => x.CreatedDate < DateTime.Now.AddDays(-retentionDays))
                .AsNoTracking()
                .ToListAsync(ct);
            _dbContext.OutboxMessages.RemoveRange(messages);
        }
    }
}
