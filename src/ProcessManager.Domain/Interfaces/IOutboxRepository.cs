using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.Interfaces
{
    public interface IOutboxRepository
    {
        public Task<IEnumerable<OutboxMessage>> GetUnprocessedEventsAsync(CancellationToken ct = default);
        public Task<IEnumerable<OutboxMessage>> GetLogicAppStartMessagesAsync(CancellationToken ct = default);
        public Task<OutboxMessage> AddAsync(Guid messageId, OutboxMessageType type, string data, CancellationToken ct = default);
        public void Update(OutboxMessage outboxMessage);
        Task<bool> CheckIfExists(Guid messageId, CancellationToken ct = default);
        public Task DeleteRangeOlderThanAsync(int retentionDays, CancellationToken ct = default);
    }
}
