using System.Text;
using ToolpathOptimizer.Application.Contracts.V1;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

public sealed class ReportCsvSummaryWriterTests
{
    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+x", "'+x")]
    [InlineData("-x", "'-x")]
    [InlineData("@x", "'@x")]
    [InlineData("H01", "H01")]
    [InlineData("", "")]
    public void EscapeText_NeutralisesFormulaPrefixes(string value, string expected)
    {
        Assert.Equal(expected, ReportCsvSummaryWriter.EscapeText(value));
    }

    [Fact]
    public void Write_ListsSelectedRoute()
    {
        byte[] report = new Handlers().Analyze.Handle(Inputs.Job(Inputs.SquareCsv), CancellationToken.None).Value.Content;

        string[] lines = Encoding.UTF8.GetString(ReportCsvSummaryWriter.Write(report)).TrimEnd('\n').Split('\n');

        Assert.Equal("position,point_id,x_mm,y_mm,operation_group", lines[0]);
        Assert.Equal("1,H01,0,0,A", lines[1]);
        Assert.Equal(5, lines.Length);
    }
}
