using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Services
{
    public class ReportingService : IReportingService
    {
        private readonly IReportingRepository _reportingRepository;
        private readonly ILogger<ReportingService> _logger;
        private readonly BlobServiceClient _storageAccountClient;
        private readonly string _fileSystemName;

        public ReportingService(
            IReportingRepository reportingRepository,
            BlobServiceClient blobServiceClient,
            string fileSystemName,
            ILogger<ReportingService> logger
            )
        {
            _reportingRepository = reportingRepository;
            _logger = logger;
            _fileSystemName = fileSystemName;
            _storageAccountClient = blobServiceClient;
        }

        public async Task StoreReportAsync(Guid correlationId, Dictionary<string, byte[]> files, CancellationToken ct = default)
        {
            try
            {
                foreach (var (key, value) in files)
                {
                    _logger.LogInformation($"Uploading {_fileSystemName} report: {correlationId}");

                    var containerClient = _storageAccountClient.GetBlobContainerClient(_fileSystemName);

                    await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

                    await using (var stream = new MemoryStream(value))
                    {
                        await containerClient.UploadBlobAsync($"{key}/{correlationId}.json", stream, ct);
                    }

                    _logger.LogInformation($"Uploaded {_fileSystemName} report: {correlationId}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Upload reports failed for: {_fileSystemName}. Exception: {e.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, byte[]>> GetReportingDataAsync(
            IEnumerable<string> dboEntities,
            DateTime? fromDate,
            DateTime? toDatetime,
            CancellationToken ct = default
        )
        {
            Dictionary<string, byte[]> result = new Dictionary<string, byte[]>();

            try
            {
                foreach (var dboEntity in dboEntities)
                {
                    IEnumerable<object> dbObject = await GetDboEntityContentAsync(dboEntity, fromDate, toDatetime, ct);

                    string jsonResult = JsonConvert.SerializeObject(dbObject.ToList(),
                            Formatting.None,
                            new JsonSerializerSettings()
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            });

                    var byteArray = Encoding.UTF8.GetBytes(jsonResult);
                    result.Add(dboEntity, byteArray);
                }
                return result;
            }
            catch
            {
                throw new ReportingException(dboEntities, fromDate, toDatetime);
            }
        }

        private async Task<IEnumerable<object>> GetDboEntityContentAsync(string dboEntity, DateTime? fromDate,DateTime? toDatetime, CancellationToken ct)
        {
            return dboEntity switch
            {
                "Activity" => await _reportingRepository.GetActivitiesAsync(fromDate, toDatetime, ct),
                "Relation" => await _reportingRepository.GetRelationsAsync(ct),
                "WorkflowRelation" => await _reportingRepository.GetWorkflowRelationsAsync(fromDate, toDatetime, ct),
                "WorkflowRun" => await _reportingRepository.GetWorkflowRunsAsync(fromDate, toDatetime, ct)
            };
        }
    }
}
