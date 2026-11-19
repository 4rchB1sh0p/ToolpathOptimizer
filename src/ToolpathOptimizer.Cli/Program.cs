using ToolpathOptimizer.Cli;

return await CliApplication.RunAsync(args, Console.Out, configureServices: null, CancellationToken.None).ConfigureAwait(false);
