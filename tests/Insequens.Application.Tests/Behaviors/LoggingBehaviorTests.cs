using FluentAssertions;
using Insequens.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Insequens.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_WithSuccessfulRequest_LogsStartAndCompletion()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, string>>();
        var behavior = new LoggingBehavior<TestRequest, string>(logger);
        var request = new TestRequest("example");
        var nextCalled = false;
        RequestHandlerDelegate<string> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult("response");
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be("response");
        nextCalled.Should().BeTrue();
        logger.Entries.Should().HaveCount(2);

        logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Be("Handling TestRequest");
        logger.Entries[0].StateValues.Should().Contain("RequestName", nameof(TestRequest));
        logger.Entries[0].StateValues.Should().Contain("{OriginalFormat}", "Handling {RequestName}");

        logger.Entries[1].LogLevel.Should().Be(LogLevel.Information);
        logger.Entries[1].Message.Should().StartWith("Handled TestRequest in ");
        logger.Entries[1].Message.Should().EndWith("ms");
        logger.Entries[1].StateValues.Should().Contain("RequestName", nameof(TestRequest));
        logger.Entries[1].StateValues.Should().ContainKey("ElapsedMs");
        logger.Entries[1].StateValues["ElapsedMs"].Should().BeOfType<long>();
        logger.Entries[1].StateValues.Should().Contain("{OriginalFormat}", "Handled {RequestName} in {ElapsedMs}ms");
    }

    [Fact]
    public async Task Handle_WhenNextThrows_LogsStartAndCompletionThenRethrows()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, string>>();
        var behavior = new LoggingBehavior<TestRequest, string>(logger);
        var request = new TestRequest("example");
        var expectedException = new InvalidOperationException("boom");
        RequestHandlerDelegate<string> next = cancellationToken => Task.FromException<string>(expectedException);

        var action = () => behavior.Handle(request, next, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        logger.Entries.Should().HaveCount(2);
        logger.Entries.Select(entry => entry.Message).Should().ContainInOrder(
            "Handling TestRequest",
            logger.Entries[1].Message);
        logger.Entries[1].Message.Should().StartWith("Handled TestRequest in ");
        logger.Entries[1].StateValues.Should().Contain("RequestName", nameof(TestRequest));
        logger.Entries[1].StateValues.Should().ContainKey("ElapsedMs");
        logger.Entries[1].StateValues["ElapsedMs"].Should().BeOfType<long>();
        logger.Entries[1].StateValues.Should().Contain("{OriginalFormat}", "Handled {RequestName} in {ElapsedMs}ms");
    }

    private sealed record TestRequest(string Name) : IRequest<string>;

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var stateValues = state is IEnumerable<KeyValuePair<string, object?>> structuredState
                ? structuredState.ToDictionary(pair => pair.Key, pair => pair.Value)
                : new Dictionary<string, object?>();

            Entries.Add(new LogEntry(logLevel, formatter(state, exception), stateValues));
        }
    }

    private sealed record LogEntry(
        LogLevel LogLevel,
        string Message,
        IReadOnlyDictionary<string, object?> StateValues);
}
