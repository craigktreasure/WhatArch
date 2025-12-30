namespace WhatArch.Abstractions;

using System;

internal class EnvironmentVariableProvider : IEnvironmentVariableProvider
{
    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);
}
