using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using DocumentManagementSystem.Exceptions;
using Moq;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.BL.Documents;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Tests
{
    public class DocumentServiceTests
    {
        private static DocumentService CreateService(
            Mock<IDocumentRepository>? docRepo = null,
            Mock<ITagRepository>? tagRepo = null,
            ILogger<DocumentService>? logger = null)
        {
            docRepo ??= new Mock<IDocumentRepository>();
            tagRepo ??= new Mock<ITagRepository>();
            logger ??= new LoggerFactory().CreateLogger<DocumentService>();
            return new DocumentService(docRepo.Object, tagRepo.Object, logger);
        }

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

        [Fact]
        public async Task CreateAsync_ThrowsValidationException_WhenTitleTooShort()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateAsync("ab", "desc", new List<string> { "Tag1" }));
        }

        [Fact]
        public async Task CreateAsync_ThrowsValidationException_WhenTooManyTags()
        {
            var service = CreateService();
            var tags = Enumerable.Range(1, 11).Select(i => $"Tag{i}").ToList();
            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateAsync("ValidTitle", "desc", tags));
        }

        [Fact]
        public async Task CreateAsync_CallsTagRepositoryForEachTag()
        {
            var docRepo = new Mock<IDocumentRepository>();
            docRepo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Document d, CancellationToken _) => d);
            var tagRepo = new Mock<ITagRepository>();
            tagRepo.Setup(r => r.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Tag());
            var logger = new LoggerFactory().CreateLogger<DocumentService>();
            var service = CreateService(docRepo, tagRepo, logger);

            var tags = new List<string> { "Tag1", "Tag2" };
            await service.CreateAsync("ValidTitle", "desc", tags);

            tagRepo.Verify(r => r.GetOrCreateAsync("Tag1", It.IsAny<CancellationToken>()), Times.Once);
            tagRepo.Verify(r => r.GetOrCreateAsync("Tag2", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}