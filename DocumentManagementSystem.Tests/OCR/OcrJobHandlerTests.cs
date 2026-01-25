using Amazon.S3;
using Amazon.S3.Model;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.OCR_Worker.Worker;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DocumentManagementSystem.Tests.OCR
{
    public class OcrJobHandlerTests
    {
        [Fact]
        public async Task HandleAsync_ThrowsException_WhenPdfNotFoundInS3()
        {
            var mockS3 = new Mock<IAmazonS3>();
            mockS3.Setup(s3 => s3.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new AmazonS3Exception("Not Found"));

            var handler = new OcrJobHandler(
                new Mock<ILogger<OcrJobHandler>>().Object,
                mockS3.Object,
                "documents",
                "connection_string",
                "eng+deu",
                300,
                null);

            var job = new OcrJob { DocumentId = Guid.NewGuid(), S3Key = "invalid.pdf" };

            var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() =>
                handler.HandleAsync(job, CancellationToken.None));

            Assert.Equal("Not Found", exception.Message);
        }
    }
}
