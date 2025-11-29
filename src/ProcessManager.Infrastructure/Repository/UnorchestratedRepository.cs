using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Repository
{
    public class UnorchestratedRepository : IUnorchestratedRepository
    {
        private readonly ProcesManagerDbContext _dbContext;
        private readonly IMapper _mapper;

        public UnorchestratedRepository(ProcesManagerDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper;
        }

        public async Task<UnorchestratedRun> AddAsync(UnorchestratedRun unorchestratedRun, CancellationToken ct = default)
        {
            var newUnorchestratedRunDbo = _mapper.Map<UnorchestratedRunDbo>(unorchestratedRun);
            var newUnorchestratedRun = (await _dbContext.UnorchestratedRuns.AddAsync(newUnorchestratedRunDbo, ct)).Entity;
            return _mapper.Map<UnorchestratedRun>(newUnorchestratedRun);
        }

        public async Task<UnorchestratedRun> GetAsync(Guid unorchestratedRunId, CancellationToken ct = default)
        {
            var unorchestratedRun = await _dbContext
                .UnorchestratedRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UnorchestratedRunId == unorchestratedRunId, ct)
                ?? throw new UnorchestratedRunNotFoundException(unorchestratedRunId);

            return _mapper.Map<UnorchestratedRun>(unorchestratedRun);
        }
    }
}
