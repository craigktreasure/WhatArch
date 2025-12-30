using System.CommandLine;
using System.IO.Abstractions;
using WhatArch;
using WhatArch.Abstractions;

Argument<string> pathArgument = new("path")
{
    Description = "Path to the binary file to analyze (searches current directory, then PATH)"
};

RootCommand rootCommand = new("Detect the target architecture of Windows PE binaries");
rootCommand.Arguments.Add(pathArgument);

rootCommand.SetAction(parseResult =>
{
    string path = parseResult.GetValue(pathArgument)!;
    FileSystem fileSystem = new();
    EnvironmentVariableProvider environmentVariableProvider = new();
    var result = WhatArchRunner.Run(fileSystem, path, environmentVariableProvider);

    if (result.Output is not null)
    {
        Console.WriteLine(result.Output);
    }

    if (result.Error is not null)
    {
        Console.Error.WriteLine(result.Error);
    }

    return result.ExitCode;
});

return rootCommand.Parse(args).Invoke();
