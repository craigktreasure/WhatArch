using System.CommandLine;

using WhatArch;

Argument<FileInfo> pathArgument = new("path")
{
    Description = "Path to the binary file to analyze"
};

RootCommand rootCommand = new("Detect the target architecture of Windows PE binaries");
rootCommand.Arguments.Add(pathArgument);

rootCommand.SetAction(parseResult =>
{
    FileInfo file = parseResult.GetValue(pathArgument)!;

    if (!file.Exists)
    {
        Console.Error.WriteLine($"Error: File not found: {file.FullName}");
        return 1;
    }

    try
    {
        string architecture = PeArchitectureReader.GetArchitecture(file.FullName);
        Console.WriteLine(architecture);
        return 0;
    }
#pragma warning disable CA1031 // Do not catch general exception types
    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
});

return rootCommand.Parse(args).Invoke();
