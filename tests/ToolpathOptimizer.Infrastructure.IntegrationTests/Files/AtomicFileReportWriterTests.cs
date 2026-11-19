using System.Text;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Infrastructure.Files;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Files;

public sealed class AtomicFileReportWriterTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly AtomicFileReportWriter _writer = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task WriteAsync_CreatesFileAndLeavesNoTemp()
    {
        OperationOutcome outcome = await _writer.WriteAsync(_temp.File("r.json"), Encoding.UTF8.GetBytes("{}"), ReportWriteMode.CreateNew, CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.Equal("{}", await File.ReadAllTextAsync(_temp.File("r.json")));
        Assert.Single(Directory.GetFiles(_temp.Path));
    }

    [Fact]
    public async Task WriteAsync_RefusesExistingFileWithoutOverwrite()
    {
        await File.WriteAllTextAsync(_temp.File("r.json"), "old");

        OperationOutcome outcome = await _writer.WriteAsync(_temp.File("r.json"), Encoding.UTF8.GetBytes("new"), ReportWriteMode.CreateNew, CancellationToken.None);

        Assert.Equal(ErrorCodes.IoOutputExists, outcome.Errors[0].Code);
        Assert.Equal("old", await File.ReadAllTextAsync(_temp.File("r.json")));
    }

    [Fact]
    public async Task WriteAsync_ReplacesExistingFileWithOverwrite()
    {
        await File.WriteAllTextAsync(_temp.File("r.json"), "old");

        OperationOutcome outcome = await _writer.WriteAsync(_temp.File("r.json"), Encoding.UTF8.GetBytes("new"), ReportWriteMode.Overwrite, CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.Equal("new", await File.ReadAllTextAsync(_temp.File("r.json")));
    }

    [Fact]
    public async Task WriteAsync_FailsWhenDirectoryMissing()
    {
        OperationOutcome outcome = await _writer.WriteAsync(_temp.File("missing/r.json"), Encoding.UTF8.GetBytes("{}"), ReportWriteMode.CreateNew, CancellationToken.None);

        Assert.Equal(ErrorCodes.IoWriteFailed, outcome.Errors[0].Code);
    }

    [Fact]
    public async Task WriteAsync_CancelledLeavesNoPartialOrTempFile()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        OperationOutcome outcome = await _writer.WriteAsync(_temp.File("r.json"), new byte[1024], ReportWriteMode.CreateNew, cancelled.Token);

        Assert.Equal(ErrorCodes.Cancelled, outcome.Errors[0].Code);
        Assert.Empty(Directory.GetFiles(_temp.Path));
    }
}
