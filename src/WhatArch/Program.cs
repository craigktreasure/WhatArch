using WhatArch;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: whatarch <path-to-binary>");
    return 1;
}

string filePath = args[0];

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"Error: File not found: {filePath}");
    return 1;
}

try
{
    string architecture = PeArchitectureReader.GetArchitecture(filePath);
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
