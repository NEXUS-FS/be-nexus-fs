namespace NexusFS.Tests;

public class UnitTest1
{
    [Fact]
    public void Test_ShouldPass()
    {
        // Arrange
        var result = 1 + 1;
        
        // Act & Assert
        Assert.Equal(2, result);
    }
}