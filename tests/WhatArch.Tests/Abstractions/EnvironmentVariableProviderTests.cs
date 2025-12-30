namespace WhatArch.Tests.Abstractions;

using WhatArch.Abstractions;

public sealed class EnvironmentVariableProviderTests
{
    [Fact]
    public void GetEnvironmentVariable_VariableExists_ReturnsValue()
    {
        // Arrange
        EnvironmentVariableProvider environmentVariableProvider = new();

        // Act
        string? pathVariable = environmentVariableProvider.GetEnvironmentVariable("PATH");

        // Assert
        Assert.NotNull(pathVariable);
        Assert.NotEmpty(pathVariable);
    }

    [Fact]
    public void GetEnvironmentVariable_VariableNotExists_ReturnsNull()
    {
        // Arrange
        EnvironmentVariableProvider environmentVariableProvider = new();

        // Act
        string? pathVariable = environmentVariableProvider.GetEnvironmentVariable("I_DO_NOT_EXIST");

        // Assert
        Assert.Null(pathVariable);
    }
}
