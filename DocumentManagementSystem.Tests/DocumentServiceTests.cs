using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using Moq;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.BL.Documents;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Tests
{
    public class DocumentServiceTests
    {
        [Fact]
        public async Task CreateAsync_CreatesDocumentWithTitleAndDescription()
        {
            var docRepo = new Mock<IDocumentRepository>();
            var tagRepo = new Mock<ITagRepository>();
            var logger = new Mock<ILogger<DocumentService>>();
            var mq = new Mock<RabbitMqService>();
            docRepo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Document d, CancellationToken _) => d);

            var service = new DocumentService(docRepo.Object, tagRepo.Object, logger.Object, mq.Object);

            var result = await service.CreateAsync("Mein Titel", "Meine Beschreibung", null);

            Assert.Equal("Mein Titel", result.Title);
            Assert.Equal("Meine Beschreibung", result.Description);
        }

        [Fact]
        public async Task GetAsync_ReturnsDocument()
        {
            var docId = Guid.NewGuid();
            var doc = new Document { Id = docId, Title = "Doc" };
            var docRepo = new Mock<IDocumentRepository>();
            var tagRepo = new Mock<ITagRepository>();
            var logger = new Mock<ILogger<DocumentService>>();
            var mq = new Mock<RabbitMqService>();
            docRepo.Setup(r => r.GetAsync(docId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(doc);

            var service = new DocumentService(docRepo.Object, tagRepo.Object, logger.Object, mq.Object);

            var result = await service.GetAsync(docId);

            Assert.NotNull(result);
            Assert.Equal(docId, result?.Id);
        }
    }
}