using RouteTrace.Web.Features.Commands;

namespace RouteTrace.Web.Tests;

public sealed class ApplicationCommandTests
{
    [Fact]
    public async Task DisabledCommandDoesNotExecute()
    {
        bool executed = false;
        var command = new ApplicationCommand(() => false, () =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        bool result = await command.TryExecuteAsync();

        result.ShouldBeFalse();
        executed.ShouldBeFalse();
    }

    [Fact]
    public async Task AvailabilityIsEvaluatedAtExecutionTime()
    {
        bool enabled = false;
        int executionCount = 0;
        var command = new ApplicationCommand(() => enabled, () =>
        {
            executionCount++;
            return Task.CompletedTask;
        });

        (await command.TryExecuteAsync()).ShouldBeFalse();
        enabled = true;
        (await command.TryExecuteAsync()).ShouldBeTrue();
        executionCount.ShouldBe(1);
    }
}
