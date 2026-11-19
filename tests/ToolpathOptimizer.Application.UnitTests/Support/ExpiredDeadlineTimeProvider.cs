namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>A clock whose timers fire as soon as they are created, simulating a reached wall-clock deadline.</summary>
internal sealed class ExpiredDeadlineTimeProvider : TimeProvider
{
    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        callback(state);
        return System.CreateTimer(_ => { }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }
}
