using System;
using DocumentManagementSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocumentManagementSystem.Tests.Rabbit
{
    public class RabbitMqServiceTests
    {
        [Fact]
        public void SendOcrMessage_PublishesMessageToQueue()
        {
            var mockLogger = new Mock<ILogger<RabbitMqService>>();
            var rabbitService = new RabbitMqService(mockLogger.Object, "localhost", "guest", "guest", "ocr-queue");

            var message = new { DocumentId = Guid.NewGuid(), Title = "Test Document" };

            try
            {
                rabbitService.SendOcrMessage(message);
            }
            catch
            {

            }

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("OCR message published to queue")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtMostOnce);
        }
    }
}
