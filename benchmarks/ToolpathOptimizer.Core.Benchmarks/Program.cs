using BenchmarkDotNet.Running;

// Run with: dotnet run -c Release --project benchmarks/ToolpathOptimizer.Core.Benchmarks
// Record hardware, dataset and commit with any published number (QUALITY_SCORE.md); never invent results.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
