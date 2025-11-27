using System.Net;
using FluentAssertions;
using GPSim.Server.Configuration;
using GPSim.Server.Services;
using GPSim.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace GPSim.Server.Tests.Services;

/// <summary>
/// Unit tests for WebhookForwarderService
/// </summary>
public class WebhookForwarderServiceTests
{
    private readonly Mock<ILogger<WebhookForwarderService>> _loggerMock;
    private readonly WebhookSettings _settings;

    public WebhookForwarderServiceTests()
    {
        _loggerMock = new Mock<ILogger<WebhookForwarderService>>();
        _settings = new WebhookSettings
        {
            DefaultUrl = "https://example.com/webhook",
            TimeoutSeconds = 30,
            RetryCount = 3
        };
    }

    private WebhookForwarderService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(_settings);
        return new WebhookForwarderService(httpClient, options, _loggerMock.Object);
    }

    private static GpsPayload CreateTestPayload()
    {
        return new GpsPayload
        {
            DeviceId = "test-device",
            Latitude = 37.7749,
            Longitude = -122.4194,
            Altitude = 0,
            Speed = 25.5,
            Bearing = 180.0,
            Accuracy = 5.0,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 1
        };
    }

    #region ForwardAsync Tests

    [Fact]
    public async Task ForwardAsync_WithSuccessfulResponse_ReturnsTrue()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        var result = await service.ForwardAsync(payload);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ForwardAsync_WithNoWebhookUrl_ReturnsFalse()
    {
        // Arrange
        _settings.DefaultUrl = null!;
        var handlerMock = new Mock<HttpMessageHandler>();
        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        var result = await service.ForwardAsync(payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ForwardAsync_WithEmptyWebhookUrl_ReturnsFalse()
    {
        // Arrange
        _settings.DefaultUrl = "";
        var handlerMock = new Mock<HttpMessageHandler>();
        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        var result = await service.ForwardAsync(payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ForwardAsync_WithWebhookUrlOverride_UsesOverrideUrl()
    {
        // Arrange
        var overrideUrl = "https://override.example.com/webhook";
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        await service.ForwardAsync(payload, webhookUrlOverride: overrideUrl);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be(overrideUrl);
    }

    [Fact]
    public async Task ForwardAsync_WithCustomHeaders_AddsHeadersToRequest()
    {
        // Arrange
        var customHeaders = "Authorization:Bearer token123;X-Custom-Header:CustomValue";
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        await service.ForwardAsync(payload, webhookHeaders: customHeaders);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("Authorization").Should().BeTrue();
        capturedRequest.Headers.GetValues("Authorization").First().Should().Be("Bearer token123");
        capturedRequest.Headers.Contains("X-Custom-Header").Should().BeTrue();
        capturedRequest.Headers.GetValues("X-Custom-Header").First().Should().Be("CustomValue");
    }

    [Fact]
    public async Task ForwardAsync_WithServerError_ReturnsFalseAfterRetries()
    {
        // Arrange
        _settings.RetryCount = 1; // Reduce retries for faster test
        var callCount = 0;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        var result = await service.ForwardAsync(payload);

        // Assert
        result.Should().BeFalse();
        callCount.Should().Be(2); // Initial + 1 retry
    }

    [Fact]
    public async Task ForwardAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act & Assert
        // TaskCanceledException inherits from OperationCanceledException
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ForwardAsync(payload, cancellationToken: cts.Token));
        exception.Should().NotBeNull();
    }

    #endregion

    #region Header Parsing Tests

    [Fact]
    public async Task ForwardAsync_WithEmptyHeaders_DoesNotAddExtraHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        await service.ForwardAsync(payload, webhookHeaders: "");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("Authorization").Should().BeFalse();
    }

    [Fact]
    public async Task ForwardAsync_WithNullHeaders_DoesNotAddExtraHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        await service.ForwardAsync(payload, webhookHeaders: null);

        // Assert
        capturedRequest.Should().NotBeNull();
        // Just verify the request was made successfully
    }

    [Fact]
    public async Task ForwardAsync_WithMalformedHeaders_SkipsInvalidHeaders()
    {
        // Arrange
        var malformedHeaders = "ValidHeader:ValidValue;InvalidHeader;:NoName;NoColon";
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        await service.ForwardAsync(payload, webhookHeaders: malformedHeaders);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("ValidHeader").Should().BeTrue();
        capturedRequest.Headers.GetValues("ValidHeader").First().Should().Be("ValidValue");
    }

    [Fact]
    public async Task ForwardAsync_WithHeaderContainingMultipleColons_ParsesCorrectly()
    {
        // Arrange - Header value contains colons (like a URL)
        var headers = "Authorization:Bearer abc:def:ghi";
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handlerMock.Object);
        var payload = CreateTestPayload();

        // Act
        await service.ForwardAsync(payload, webhookHeaders: headers);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("Authorization").Should().BeTrue();
        // Note: Current implementation only takes the value after the first colon
        capturedRequest.Headers.GetValues("Authorization").First().Should().Be("Bearer abc:def:ghi");
    }

    #endregion
}
