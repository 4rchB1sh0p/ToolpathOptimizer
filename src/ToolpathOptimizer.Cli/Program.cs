using ToolpathOptimizer.Cli;

return await CliApplication.RunAsync(args, Console.Out, CancellationToken.None).ConfigureAwait(false);
