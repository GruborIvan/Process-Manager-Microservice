using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProcessManager.API.Models;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace ProcessManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IWorkflowRepository _repository;

        public WorkflowsController(
            ILogger<WorkflowsController> logger,
            IMapper mapper,
            IWorkflowRepository repository)
        {
            _logger = logger;
            _mapper = mapper;
            _repository = repository;
        }

        /// <summary>
        /// Returns the workflow status for the specified operation ID
        /// </summary>
        /// <response code="200">If the workflow status with the specified operation ID exists</response>
        /// <response code="400">If the ID has non-GUID format</response> 
        /// <response code="404">If there is no  workflow status with the specified operation ID</response> 
        //[Authorize(Policy = "CanGetOperationStatus")]
        [HttpGet("{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkflowRunDto>> GetOperationStatusByOperationId(Guid operationId)
        {
            try
            {
                var workflowDbo = await _repository.GetAsync(operationId);

                _logger.LogInformation($"Workflow with {nameof(operationId)}={operationId} was found");

                return _mapper.Map<WorkflowRunDto>(workflowDbo);
            }
            catch (WorkflowNotFoundException e)
            {
                _logger.LogError(e, e.Message);

                return NotFound($"Workflow with {nameof(operationId)}={operationId} was not found");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
