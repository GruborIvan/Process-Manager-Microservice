using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ProcessManager.Infrastructure.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class ConfigurationServiceTests
    {
        private readonly ILogger<ConfigurationService> _mockLoggerObject = new Mock<ILogger<ConfigurationService>>().Object;
        private const string BASE_URL = "https://www.test.com";

        [Fact]
        public async Task GetConfiguration_EverythingOK_ReturnsConfiguration()
        {
            //Arrange
            var mockHttpClientFactory = GetMocks(HttpStatusCode.OK, new StringContent("{}"));
            Mock<IAzureTokenProvider> mockAzureTokenProvider = new Mock<IAzureTokenProvider>();

            var service = new ConfigurationService(_mockLoggerObject, mockHttpClientFactory.Object, mockAzureTokenProvider.Object, Guid.NewGuid().ToString(), "config", "cmBaseUrl", BASE_URL, Guid.NewGuid().ToString(), "authScope");

            //Act
            var response = await service.GetConfigurationAsync("");

            //Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GetConfiguration_HttpCallFails_ThrowsException()
        {
            //Arrange
            var mockHttpClientFactory = GetMocks(HttpStatusCode.InternalServerError, null);
            Mock<IAzureTokenProvider> mockAzureTokenProvider = new Mock<IAzureTokenProvider>();

            var service = new ConfigurationService(_mockLoggerObject, mockHttpClientFactory.Object, mockAzureTokenProvider.Object, "", "", "", BASE_URL, "", "");

            //Act
            var exception = await Record.ExceptionAsync(async () => await service.GetConfigurationAsync(""));

            //Assert
            Assert.NotNull(exception);
        }

        private Mock<IHttpClientFactory> GetMocks(HttpStatusCode statusCode, HttpContent content)
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHttpHandler
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = statusCode,
                   Content = content
               });
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(mockHttpHandler.Object) { BaseAddress = new Uri(BASE_URL) });
            return mockHttpClientFactory;
        }
    }
}
