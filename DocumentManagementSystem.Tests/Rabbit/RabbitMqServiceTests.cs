using System;
using System.Text;
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

            rabbitService.SendOcrMessage(message);

            // Wir können hier die Logs überprüfen oder auch die RabbitMQ-Verbindung mocken, um sicherzustellen, dass die Nachricht gesendet wurde
            mockLogger.Verify(log => log.LogInformation(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        // Weitere Tests hier
    }
}
