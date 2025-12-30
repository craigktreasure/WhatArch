namespace WhatArch.Abstractions;

internal interface IEnvironmentVariableProvider
{
    /// <summary>
    /// Retrieves the value of the environment variable with the specified name.
    /// </summary>
    /// <param name="name">The name of the environment variable to retrieve. Cannot be null or empty.</param>
    /// <returns>The value of the environment variable specified by name, or null if the environment variable is not found.</returns>
    string? GetEnvironmentVariable(string name);
}
