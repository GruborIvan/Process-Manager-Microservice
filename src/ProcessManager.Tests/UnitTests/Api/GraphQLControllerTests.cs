using GraphQL;
using GraphQL.Types;
using Moq;
using ProcessManager.API.Controllers;
using System.Threading.Tasks;
using Xunit;
using static ProcessManager.API.Controllers.GraphQlController;

namespace ProcessManager.Tests.UnitTests.Api
{
    public class GraphQLControllerTests
    {
        private GraphQlController _graphqlController { get; set; }

        public GraphQLControllerTests()
        {
            var documentExecutor = new Mock<IDocumentExecuter>();
            documentExecutor.Setup(x => x.ExecuteAsync(It.IsAny<ExecutionOptions>()))
                .Returns(Task.FromResult(new ExecutionResult()));

            var schema = new Mock<ISchema>();

            _graphqlController = new GraphQlController(schema.Object, documentExecutor.Object);
        }

        [Fact]
        public async Task ReturnNotNullExecutionResult()
        {
            // Arrange
            var query = new PostBody { Query = @"{ ""query"": ""query { processes { workFlowRunName } }""" };

            // Act
            var result = await _graphqlController.Post(query, default);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ExecutionResult>(result);
        }
    }
}
