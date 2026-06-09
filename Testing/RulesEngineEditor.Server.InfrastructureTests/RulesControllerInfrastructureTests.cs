using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace RulesEngineEditor.Server.InfrastructureTests;

[Trait("Category", "Infrastructure")]
[ExcludeFromCodeCoverage]
public sealed class RulesControllerInfrastructureTests : IClassFixture<TestingWebAppFactory>
{
    private readonly HttpClient _client;

    public RulesControllerInfrastructureTests(TestingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllWorkflows_Returns200WithJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task DryRun_WithEmptyBody_Returns400WithValidationErrors()
    {
        // Arrange
        var jsonContent = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/rules/dry-run", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonDocument.Parse(body);

        // Verify it's a ProblemDetails response with model validation errors
        problemDetails.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        problemDetails.RootElement.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetInt32().Should().Be(400);

        // Should contain validation errors for required fields (RulesJson and/or FactsJson)
        problemDetails.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task DryRun_WithMalformedBody_Returns400()
    {
        // Arrange
        var jsonContent = new StringContent("{invalid}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/rules/dry-run", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
