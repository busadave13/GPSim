using FluentAssertions;
using GPSim.Server.Configuration;
using GPSim.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GPSim.Server.Tests.Controllers;

/// <summary>
/// Unit tests for ConfigurationController
/// </summary>
public class ConfigurationControllerTests
{
    private static IOptions<WebhookSettings> CreateDefaultWebhookOptions() =>
        Options.Create(new WebhookSettings());

    [Fact]
    public void GetMapboxConfig_ReturnsOkWithAccessToken()
    {
        // Arrange
        var settings = new MapboxSettings
        {
            AccessToken = "test-access-token",
            CircleRadiusMiles = 0.5
        };
        var options = Options.Create(settings);
        var controller = new ConfigurationController(options, CreateDefaultWebhookOptions());

        // Act
        var result = controller.GetMapboxConfig();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<MapboxConfigResponse>().Subject;
        response.AccessToken.Should().Be("test-access-token");
        response.CircleRadiusMiles.Should().Be(0.5);
    }

    [Fact]
    public void GetMapboxConfig_WithDefaultCircleRadius_ReturnsDefaultValue()
    {
        // Arrange
        var settings = new MapboxSettings
        {
            AccessToken = "test-token"
            // CircleRadiusMiles not set, should use default
        };
        var options = Options.Create(settings);
        var controller = new ConfigurationController(options, CreateDefaultWebhookOptions());

        // Act
        var result = controller.GetMapboxConfig();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<MapboxConfigResponse>().Subject;
        response.CircleRadiusMiles.Should().Be(0.1); // Default value
    }

    [Fact]
    public void GetMapboxConfig_WithEmptyAccessToken_ReturnsEmptyString()
    {
        // Arrange
        var settings = new MapboxSettings
        {
            AccessToken = "",
            CircleRadiusMiles = 0.25
        };
        var options = Options.Create(settings);
        var controller = new ConfigurationController(options, CreateDefaultWebhookOptions());

        // Act
        var result = controller.GetMapboxConfig();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<MapboxConfigResponse>().Subject;
        response.AccessToken.Should().BeEmpty();
        response.CircleRadiusMiles.Should().Be(0.25);
    }

    [Fact]
    public void GetMapboxConfig_WithSmallRadius_ReturnsConfiguredValue()
    {
        // Arrange
        var settings = new MapboxSettings
        {
            AccessToken = "token",
            CircleRadiusMiles = 0.1 // 1/10th of a mile
        };
        var options = Options.Create(settings);
        var controller = new ConfigurationController(options, CreateDefaultWebhookOptions());

        // Act
        var result = controller.GetMapboxConfig();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<MapboxConfigResponse>().Subject;
        response.CircleRadiusMiles.Should().Be(0.1);
    }

    [Fact]
    public void GetMapboxConfig_WithLargeRadius_ReturnsConfiguredValue()
    {
        // Arrange
        var settings = new MapboxSettings
        {
            AccessToken = "token",
            CircleRadiusMiles = 5.0 // 5 miles
        };
        var options = Options.Create(settings);
        var controller = new ConfigurationController(options, CreateDefaultWebhookOptions());

        // Act
        var result = controller.GetMapboxConfig();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<MapboxConfigResponse>().Subject;
        response.CircleRadiusMiles.Should().Be(5.0);
    }
}
