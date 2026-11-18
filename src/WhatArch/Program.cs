using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using WhatArch;
using WhatArch.Abstractions;

internal class Program
{
    [ExcludeFromCodeCoverage]
    private static int Main(string[] args)
        => Invoke(args, new FileSystem(), new EnvironmentVariableProvider(), Console.Out, Console.Error);

    /// <summary>
    /// Parses the command line, runs the analysis, and writes the result.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="environmentVariableProvider">The environment variable provider.</param>
    /// <param name="output">The writer for the result.</param>
    /// <param name="error">The writer for error messages.</param>
    /// <returns>The exit code.</returns>
    internal static int Invoke(
        string[] args,
        IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariableProvider,
        TextWriter output,
        TextWriter error)
    {
        Argument<string> pathArgument = new("path")
        {
            Description = "Path to the binary file to analyze (searches current directory, then PATH)"
        };

        Option<bool> shortOption = new("--short", "-s")
        {
            Description = "For shims, print only the target's architecture"
        };

        RootCommand rootCommand = new("Detect the target architecture of Windows PE binaries");
        rootCommand.Arguments.Add(pathArgument);
        rootCommand.Options.Add(shortOption);

        rootCommand.SetAction(parseResult =>
        {
            string path = parseResult.GetValue(pathArgument)!;
            bool shortOutput = parseResult.GetValue(shortOption);
            var result = WhatArchRunner.Run(fileSystem, path, environmentVariableProvider);

            string? formatted = result.FormatOutput(shortOutput);
            if (formatted is not null)
            {
                output.WriteLine(formatted);
            }

            if (result.Error is not null)
            {
                error.WriteLine(result.Error);
            }

            return result.ExitCode;
        });

        return rootCommand.Parse(args).Invoke();
    }
}
