using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;

namespace ProcessManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphQlController : ControllerBase
    {
        private readonly IDocumentExecuter _executer;
        private readonly ISchema _schema;

        public GraphQlController(ISchema schema, IDocumentExecuter executer)
        {
            _schema = schema;
            _executer = executer;
        }

        // [Authorize(Policy = "CanGetProcesses")]
        [HttpGet]
        public Task<ExecutionResult> Get(
        [FromQuery] string query,
        [FromQuery] string? variables,
        [FromQuery] string? operationName,
        CancellationToken cancellation)
        {
            var jObject = ParseVariables(variables);
            return Execute(query, operationName, jObject, cancellation);
        }

        // [Authorize(Policy = "CanGetProcesses")]
        [HttpPost]
        public Task<ExecutionResult> Post(
        [BindRequired, FromBody] PostBody body,
        CancellationToken cancellation)
        {
            return Execute(body.Query, body.OperationName, body.Variables, cancellation);
        }

        public class PostBody
        {
            public string? OperationName;
            public string Query = null!;
            public JObject? Variables;
        }

        Task<ExecutionResult> Execute(
        string query,
        string? operationName,
        JObject? variables,
        CancellationToken cancellation)
        {
            var options = new ExecutionOptions
            {
                Schema = _schema,
                Query = query,
                OperationName = operationName,
                Inputs = variables?.ToInputs(),
                CancellationToken = cancellation,
            #if (DEBUG)
                ExposeExceptions = true,
                EnableMetrics = true,
            #endif
            };

            return _executer.ExecuteAsync(options);
        }

        static JObject? ParseVariables(string? variables)
        {
            if (variables == null)
            {
                return null;
            }

            try
            {
                return JObject.Parse(variables);
            }
            catch (Exception exception)
            {
                throw new Exception("Could not parse variables.", exception);
            }
        }
    }
}
