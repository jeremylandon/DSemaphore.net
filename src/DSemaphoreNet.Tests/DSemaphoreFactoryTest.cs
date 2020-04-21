using System;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace DSemaphoreNet.Tests
{
    public class DSemaphoreFactoryTest
    {
        [Fact]
        public void Create_ReturnFactory()
        {
            var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();

            Action action = () => DSemaphoreFactory.Create(connectionMultiplexerMock.Object);

            action.Should().NotThrow();
        }

        [Fact]
        public void Create_WithoutConnection_ThrowArgumentNullException()
        {
            Action action = () => DSemaphoreFactory.Create(null);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CreateSemaphore_ReturnSemaphore()
        {
            var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
            var databaseMock = new Mock<IDatabase>();
            connectionMultiplexerMock.Setup(multiplexer => multiplexer.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(databaseMock.Object);
            var factory = DSemaphoreFactory.Create(connectionMultiplexerMock.Object);

            Action action = () => factory.CreateSemaphore("a", 1);

            action.Should().NotThrow();
        }
    }
}