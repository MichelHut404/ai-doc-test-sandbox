using Moq;
using src.Infrastructure;
using src.Infrastructure.Interfaces;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class GitServiceTests
{
    [Fact]
    public async Task GetChangedFilesAsync_ReturnsResult_WhenCalled()
    {
        // Arrange
        var mockRunner = new Mock<ICMDProcessRunner>();
        mockRunner
            .Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD"))
            .ReturnsAsync("src/Foo.cs\n");

        var sut = new GitService(mockRunner.Object);

        // Act
        var result = await sut.GetChangedFilesAsync();

        // Assert
        Assert.NotNull(result);
    }
}