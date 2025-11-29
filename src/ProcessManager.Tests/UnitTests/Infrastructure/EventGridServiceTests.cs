using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Moq;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Infrastructure.Services;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class EventGridServiceTests
    {
        private readonly Mock<IEventGridClient> _eventGridClientMock;
        private readonly Mock<IContextAccessor> _contextAccessorMock;

        public EventGridServiceTests()
        {
            _eventGridClientMock = new Mock<IEventGridClient>();
            _contextAccessorMock = new Mock<IContextAccessor>();
        }

        [Fact]
        public async Task SendAsync_EventIsNull_ArgumentNullException()
        {
            var eventGridService = new EventGridService(_eventGridClientMock.Object, "http://topic.com");

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridService.SendAsync(null, "subject"));

            Assert.Equal("Value cannot be null. (Parameter 'event')", exception.Message);
        }

        [Fact]
        public async Task SendAsync_SubjectIsNull_ArgumentNullException()
        {
            var eventGridService = new EventGridService(_eventGridClientMock.Object, "http://topic.com");

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridService.SendAsync("event", null));

            Assert.Equal("Value cannot be null. (Parameter 'subject')", exception.Message);
        }
    }
}
