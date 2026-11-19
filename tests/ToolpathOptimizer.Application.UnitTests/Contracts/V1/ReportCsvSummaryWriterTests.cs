using System.Text;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

/// <summary>Tests for <see cref="ReportCsvSummaryWriter"/>.</summary>
public sealed class ReportCsvSummaryWriterTests
{
    private static string[] WriteSquareSummaryLines()
    {
        byte[] report = new TestHandlers().Analyze.Handle(TestInputs.CreateJob(TestInputs.SquareCsv), CancellationToken.None).Value.Content;
        return Encoding.UTF8.GetString(ReportCsvSummaryWriter.Write(report)).TrimEnd('\n').Split('\n');
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+x", "'+x")]
    [InlineData("-x", "'-x")]
    [InlineData("@x", "'@x")]
    [InlineData("H01", "H01")]
    [InlineData("", "")]
    public void EscapeText_FormulaPrefix_IsNeutralised(string value, string expected)
    {
        Assert.Equal(expected, ReportCsvSummaryWriter.EscapeText(value));
    }

    [Fact]
    public void Write_SquareReport_StartsWithHeader()
    {
        Assert.Equal("position,point_id,x_mm,y_mm,operation_group", WriteSquareSummaryLines()[0]);
    }

    [Fact]
    public void Write_SquareReport_ListsSelectedRouteInOrder()
    {
        Assert.Equal(["1,H01,0,0,A", "2,H02,100,0,A", "3,H03,100,100,A", "4,H04,0,100,A"], WriteSquareSummaryLines()[1..]);
    }
}
