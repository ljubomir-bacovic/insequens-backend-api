using System.Text.Json;
using FluentAssertions;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;

namespace Insequens.Api.Tests.V2;

/// <summary>How the v2 contract types read and write JSON with the web defaults the API uses.</summary>
public class V2JsonTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void UpdateTaskRequest_TellsAnAbsentFieldFromAnExplicitNull()
    {
        var absent = JsonSerializer.Deserialize<UpdateTaskRequest>("{}", Web)!;
        var cleared = JsonSerializer.Deserialize<UpdateTaskRequest>("""{ "description": null, "dueDate": null }""", Web)!;
        var set = JsonSerializer.Deserialize<UpdateTaskRequest>("""{ "description": "Text", "dueDate": "2026-10-10" }""", Web)!;

        absent.Description.HasValue.Should().BeFalse();
        absent.DueDate.HasValue.Should().BeFalse();
        cleared.Description.HasValue.Should().BeTrue();
        cleared.Description.Value.Should().BeNull();
        cleared.DueDate.HasValue.Should().BeTrue();
        cleared.DueDate.Value.Should().BeNull();
        set.Description.Value.Should().Be("Text");
        set.DueDate.Value.Should().Be(new DateOnly(2026, 10, 10));
    }

    [Theory]
    [InlineData(TaskPriority.None, "\"none\"")]
    [InlineData(TaskPriority.High, "\"high\"")]
    public void TaskPriority_IsACamelCaseString(TaskPriority priority, string json)
    {
        JsonSerializer.Serialize(priority, Web).Should().Be(json);
        JsonSerializer.Deserialize<TaskPriority>(json.ToUpperInvariant(), Web).Should().Be(priority);
    }

    [Fact]
    public void TaskPriority_RejectsNumbers()
    {
        var read = () => JsonSerializer.Deserialize<TaskPriority>("3", Web);

        read.Should().Throw<JsonException>();
    }

    [Fact]
    public void Optional_WritesItsValueOrNull()
    {
        JsonSerializer.Serialize(new Optional<string?>("Text"), Web).Should().Be("\"Text\"");
        JsonSerializer.Serialize(default(Optional<string?>), Web).Should().Be("null");
    }
}
