using System.Text;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

public sealed class PointsCsvReaderTests
{
    private const string Header = "point_id,x_mm,y_mm,sequence,operation_group\n";

    private static Result<IReadOnlyList<PointRow>> Read(string text, AnalysisLimitsOptions? limits = null) =>
        PointsCsvReader.Read(Encoding.UTF8.GetBytes(text), "holes.csv", limits ?? new AnalysisLimitsOptions());

    private static void AssertRejected(string expectedCode, string text, AnalysisLimitsOptions? limits = null)
    {
        Result<IReadOnlyList<PointRow>> result = Read(text, limits);
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Outcome.Errors[0].Code);
    }

    [Fact]
    public void Read_ParsesSampleFile()
    {
        Result<IReadOnlyList<PointRow>> result = Read(Inputs.SquareCsv);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Count);
        Assert.Equal(100, result.Value[1].XMm);
        Assert.Equal("A", result.Value[0].OperationGroup);
    }

    [Fact]
    public void Read_AcceptsBomCrlfEmptyGroupAndExponent()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("point_id,x_mm,y_mm,sequence,operation_group\r\nP1,-1.5e2,+.5,1,\r\n")];

        Result<IReadOnlyList<PointRow>> result = PointsCsvReader.Read(bytes, "holes.csv", new AnalysisLimitsOptions());

        Assert.True(result.IsSuccess);
        Assert.Equal(-150, result.Value[0].XMm);
        Assert.Equal(0.5, result.Value[0].YMm);
        Assert.Equal(string.Empty, result.Value[0].OperationGroup);
    }

    [Theory]
    [InlineData(ErrorCodes.InputMalformedCsv, "id,x,y\nA,1,2\n")]
    [InlineData(ErrorCodes.InputUnitMismatch, "point_id,x_in,y_in,sequence,operation_group\nA,1,2,1,\n")]
    [InlineData(ErrorCodes.InputNoPoints, Header)]
    [InlineData(ErrorCodes.InputMalformedCsv, Header + "A,1,2,1\n")]
    [InlineData(ErrorCodes.InputMalformedCsv, Header + "A,1,2,1,,extra\n")]
    [InlineData(ErrorCodes.InputMalformedCsv, Header + "A,1,2,1,\n\nB,3,4,2,\n")]
    [InlineData(ErrorCodes.InputMalformedCsv, Header + "\"A\",1,2,1,\n")]
    [InlineData(ErrorCodes.InputInvalidIdentifier, Header + "=CMD,1,2,1,\n")]
    [InlineData(ErrorCodes.InputInvalidIdentifier, Header + "A,1,2,1,@grp\n")]
    [InlineData(ErrorCodes.InputInvalidNumber, Header + "A,1;5,2,1,\n")]
    [InlineData(ErrorCodes.InputInvalidNumber, Header + "A,0x10,2,1,\n")]
    [InlineData(ErrorCodes.InputInvalidNumber, Header + "A, 1,2,1,\n")]
    [InlineData(ErrorCodes.InputNonFiniteCoordinate, Header + "A,NaN,2,1,\n")]
    [InlineData(ErrorCodes.InputNonFiniteCoordinate, Header + "A,1,-Infinity,1,\n")]
    [InlineData(ErrorCodes.InputNonFiniteCoordinate, Header + "A,1e400,2,1,\n")]
    [InlineData(ErrorCodes.InputDuplicatePointId, Header + "A,1,2,1,\nA,3,4,2,\n")]
    [InlineData(ErrorCodes.InputSequenceInvalid, Header + "A,1,2,1,\nB,3,4,3,\n")]
    [InlineData(ErrorCodes.InputSequenceInvalid, Header + "A,1,2,1,\nB,3,4,1,\n")]
    [InlineData(ErrorCodes.InputSequenceInvalid, Header + "A,1,2,x,\n")]
    [InlineData(ErrorCodes.InputCoincidentPoints, Header + "A,1,2,1,\nB,1.0005,2,2,\n")]
    [InlineData(ErrorCodes.InputLimitExceeded, Header + "A,10000.5,2,1,\n")]
    public void Read_RejectsContractViolation(string expectedCode, string text)
    {
        AssertRejected(expectedCode, text);
    }

    [Fact]
    public void Read_RejectsInvalidUtf8()
    {
        byte[] bytes = [.. Encoding.UTF8.GetBytes(Header + "A,1,2,1,"), 0xFF, 0x0A];

        Result<IReadOnlyList<PointRow>> result = PointsCsvReader.Read(bytes, "holes.csv", new AnalysisLimitsOptions());

        Assert.Equal(ErrorCodes.InputInvalidEncoding, result.Outcome.Errors[0].Code);
    }

    [Fact]
    public void Read_AcceptsPointCountAtLimitAndRejectsOneMore()
    {
        var limits = new AnalysisLimitsOptions { MaxPoints = 2 };
        string two = Header + "A,1,2,1,\nB,3,4,2,\n";

        Assert.True(Read(two, limits).IsSuccess);
        AssertRejected(ErrorCodes.InputLimitExceeded, two + "C,5,6,3,\n", limits);
    }

    [Fact]
    public void Read_RejectsFileOverByteLimit()
    {
        AssertRejected(ErrorCodes.InputLimitExceeded, Inputs.SquareCsv, new AnalysisLimitsOptions { MaxPointsFileBytes = 10 });
    }

    [Fact]
    public void Read_TruncatesEchoedInputInMessages()
    {
        string longId = "=" + new string('x', 500);

        Result<IReadOnlyList<PointRow>> result = Read(Header + longId + ",1,2,1,\n");

        Assert.DoesNotContain(new string('x', 100), result.Outcome.Errors[0].Message, StringComparison.Ordinal);
        Assert.Contains("line 2", result.Outcome.Errors[0].Message, StringComparison.Ordinal);
    }
}
