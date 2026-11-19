using System.Text;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

/// <summary>Tests for <see cref="PointsCsvReader"/>: one rejection rule per case.</summary>
public sealed class PointsCsvReaderTests
{
    private const string Header = "point_id,x_mm,y_mm,sequence,operation_group\n";

    private static Result<IReadOnlyList<PointRow>> Read(string text) => Read(text, new AnalysisLimitsOptions());

    private static Result<IReadOnlyList<PointRow>> Read(string text, AnalysisLimitsOptions limits) =>
        PointsCsvReader.Read(Encoding.UTF8.GetBytes(text), "holes.csv", limits);

    private static string FirstCode(Result<IReadOnlyList<PointRow>> result) =>
        result.IsSuccess ? "(success)" : result.Outcome.Errors[0].Code;

    [Fact]
    public void Read_SampleFile_ReturnsAllRows()
    {
        Assert.Equal(4, Read(TestInputs.SquareCsv).Value.Count);
    }

    [Fact]
    public void Read_SampleFile_ParsesCoordinatesAndGroup()
    {
        PointRow second = Read(TestInputs.SquareCsv).Value[1];

        Assert.Equal((100.0, 0.0, "A"), (second.XMm, second.YMm, second.OperationGroup));
    }

    [Fact]
    public void Read_BomCrlfEmptyGroupAndExponent_ParsesRow()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("point_id,x_mm,y_mm,sequence,operation_group\r\nP1,-1.5e2,+.5,1,\r\n")];

        PointRow row = PointsCsvReader.Read(bytes, "holes.csv", new AnalysisLimitsOptions()).Value[0];

        Assert.Equal((-150.0, 0.5, string.Empty), (row.XMm, row.YMm, row.OperationGroup));
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
    public void Read_ContractViolation_ReturnsStableCode(string expectedCode, string text)
    {
        Assert.Equal(expectedCode, FirstCode(Read(text)));
    }

    [Fact]
    public void Read_InvalidUtf8_ReturnsEncodingError()
    {
        byte[] bytes = [.. Encoding.UTF8.GetBytes(Header + "A,1,2,1,"), 0xFF, 0x0A];

        Result<IReadOnlyList<PointRow>> result = PointsCsvReader.Read(bytes, "holes.csv", new AnalysisLimitsOptions());

        Assert.Equal(ErrorCodes.InputInvalidEncoding, FirstCode(result));
    }

    [Fact]
    public void Read_PointCountAtLimit_Succeeds()
    {
        Result<IReadOnlyList<PointRow>> result = Read(Header + "A,1,2,1,\nB,3,4,2,\n", new AnalysisLimitsOptions { MaxPoints = 2 });

        Assert.True(result.IsSuccess, FirstCode(result));
    }

    [Fact]
    public void Read_PointCountOverLimit_ReturnsLimitExceeded()
    {
        Result<IReadOnlyList<PointRow>> result = Read(Header + "A,1,2,1,\nB,3,4,2,\nC,5,6,3,\n", new AnalysisLimitsOptions { MaxPoints = 2 });

        Assert.Equal(ErrorCodes.InputLimitExceeded, FirstCode(result));
    }

    [Fact]
    public void Read_FileOverByteLimit_ReturnsLimitExceeded()
    {
        Assert.Equal(ErrorCodes.InputLimitExceeded, FirstCode(Read(TestInputs.SquareCsv, new AnalysisLimitsOptions { MaxPointsFileBytes = 10 })));
    }

    [Fact]
    public void Read_LongInvalidIdentifier_TruncatesEchoedInput()
    {
        string message = Read(Header + "=" + new string('x', 500) + ",1,2,1,\n").Outcome.Errors[0].Message;

        Assert.DoesNotContain(new string('x', 100), message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_InvalidRow_NamesLineInMessage()
    {
        string message = Read(Header + "=bad,1,2,1,\n").Outcome.Errors[0].Message;

        Assert.Contains("line 2", message, StringComparison.Ordinal);
    }
}
