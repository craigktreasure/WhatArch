namespace WhatArch.Tests.TestUtilities;

using System;
using WhatArch.Abstractions;

internal sealed class FakeEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    private const string PathVariableName = "PATH";

    private readonly Dictionary<string, string?> _variables = new(StringComparer.OrdinalIgnoreCase);

    public FakeEnvironmentVariableProvider SetPath(string pathValue)
        => SetEnvironmentVariable(PathVariableName, pathValue);

    public FakeEnvironmentVariableProvider AppendToPath(string pathValue)
    {
        if (_variables.TryGetValue(PathVariableName, out string? existingPathValue))
        {
            _variables[PathVariableName] = $"{existingPathValue}{Path.PathSeparator}{pathValue}";
        }
        else
        {
            _variables[PathVariableName] = pathValue;
        }

        return this;
    }

    public FakeEnvironmentVariableProvider SetEnvironmentVariable(string name, string? value)
    {
        if (value is null)
        {
            ClearEnvironmentVariable(name);
            return this;
        }

        _variables[name] = value;
        return this;
    }

    public FakeEnvironmentVariableProvider ClearEnvironmentVariable(string name)
    {
        _variables.Remove(name);
        return this;
    }

    public string? GetEnvironmentVariable(string name)
        => _variables.TryGetValue(name, out string? value) ? value : null;

    public static FakeEnvironmentVariableProvider CreateWithPath(string pathValue)
        => new FakeEnvironmentVariableProvider().SetPath(pathValue);
}
