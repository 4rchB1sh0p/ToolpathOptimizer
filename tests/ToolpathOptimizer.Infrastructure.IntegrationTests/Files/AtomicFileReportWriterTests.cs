using System.Text;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Infrastructure.Files;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Files;

/// <summary>Tests for <see cref="AtomicFileReportWriter"/> against the real file system.</summary>
public sealed class AtomicFileReportWriterTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly AtomicFileReportWriter _writer = new();

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    private Task<OperationOutcome> WriteAsync(string name, string content, ReportWriteMode mode) =>
        _writer.WriteAsync(_temp.PathOf(name), Encoding.UTF8.GetBytes(content), mode, CancellationToken.None);

    [Fact]
    public async Task WriteAsync_NewFile_WritesContent()
    {
        await WriteAsync("r.json", "{}", ReportWriteMode.CreateNew);

        Assert.Equal("{}", await File.ReadAllTextAsync(_temp.PathOf("r.json")));
    }

    [Fact]
    public async Task WriteAsync_NewFile_LeavesNoTempFile()
    {
        await WriteAsync("r.json", "{}", ReportWriteMode.CreateNew);

        Assert.Single(Directory.GetFiles(_temp.Path));
    }

    [Fact]
    public async Task WriteAsync_ExistingFileWithCreateNew_ReturnsOutputExists()
    {
        await File.WriteAllTextAsync(_temp.PathOf("r.json"), "old");

        OperationOutcome outcome = await WriteAsync("r.json", "new", ReportWriteMode.CreateNew);

        Assert.Equal(ErrorCodes.IoOutputExists, outcome.Errors[0].Code);
    }

    [Fact]
    public async Task WriteAsync_ExistingFileWithCreateNew_KeepsOldContent()
    {
        await File.WriteAllTextAsync(_temp.PathOf("r.json"), "old");

        await WriteAsync("r.json", "new", ReportWriteMode.CreateNew);

        Assert.Equal("old", await File.ReadAllTextAsync(_temp.PathOf("r.json")));
    }

    [Fact]
    public async Task WriteAsync_ExistingFileWithOverwrite_ReplacesContent()
    {
        await File.WriteAllTextAsync(_temp.PathOf("r.json"), "old");

        await WriteAsync("r.json", "new", ReportWriteMode.Overwrite);

        Assert.Equal("new", await File.ReadAllTextAsync(_temp.PathOf("r.json")));
    }

    [Fact]
    public async Task WriteAsync_MissingDirectory_ReturnsWriteFailed()
    {
        OperationOutcome outcome = await WriteAsync("missing/r.json", "{}", ReportWriteMode.CreateNew);

        Assert.Equal(ErrorCodes.IoWriteFailed, outcome.Errors[0].Code);
    }

    [Fact]
    public async Task WriteAsync_CancelledToken_ReturnsCancelled()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        OperationOutcome outcome = await _writer.WriteAsync(_temp.PathOf("r.json"), new byte[1024], ReportWriteMode.CreateNew, cancelled.Token);

        Assert.Equal(ErrorCodes.Cancelled, outcome.Errors[0].Code);
    }

    [Fact]
    public async Task WriteAsync_CancelledToken_LeavesNoFiles()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await _writer.WriteAsync(_temp.PathOf("r.json"), new byte[1024], ReportWriteMode.CreateNew, cancelled.Token);

        Assert.Empty(Directory.GetFiles(_temp.Path));
    }
}
