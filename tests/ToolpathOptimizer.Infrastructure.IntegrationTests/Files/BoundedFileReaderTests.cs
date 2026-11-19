using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Infrastructure.Files;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Files;

/// <summary>Tests for <see cref="BoundedFileReader"/> against the real file system.</summary>
public sealed class BoundedFileReaderTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task ReadAsync_FileWithinLimit_ReturnsBytes()
    {
        await File.WriteAllBytesAsync(_temp.PathOf("a.csv"), [1, 2, 3]);

        Result<byte[]> result = await new BoundedFileReader().ReadAsync(_temp.PathOf("a.csv"), 3, CancellationToken.None);

        Assert.Equal([1, 2, 3], result.Value);
    }

    [Fact]
    public async Task ReadAsync_FileOverLimit_ReturnsLimitExceeded()
    {
        await File.WriteAllBytesAsync(_temp.PathOf("a.csv"), [1, 2, 3, 4]);

        Result<byte[]> result = await new BoundedFileReader().ReadAsync(_temp.PathOf("a.csv"), 3, CancellationToken.None);

        Assert.Equal(ErrorCodes.InputLimitExceeded, result.Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task ReadAsync_MissingFile_ReturnsReadFailed()
    {
        Result<byte[]> result = await new BoundedFileReader().ReadAsync(_temp.PathOf("nope.csv"), 3, CancellationToken.None);

        Assert.Equal(ErrorCodes.IoReadFailed, result.Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task ReadAsync_MissingFile_DoesNotEchoDirectory()
    {
        Result<byte[]> result = await new BoundedFileReader().ReadAsync(_temp.PathOf("nope.csv"), 3, CancellationToken.None);

        Assert.DoesNotContain(_temp.Path, result.Outcome.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }
}
