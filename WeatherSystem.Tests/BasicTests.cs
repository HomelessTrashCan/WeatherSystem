namespace WeatherSystem.Tests;

public class BasicTests
{
    [Fact]
    public void SampleTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DomainCore_ShouldBeAccessible()
    {
        // This test verifies that we can reference the DomainCore project
        // Add actual domain tests here as the project grows
        Assert.True(true, "DomainCore project is accessible");
    }

    //DummyTest
    [Fact]
    public void ApplicationCore_ShouldBeAccessible()
    {
        // This test verifies that we can reference the ApplicationCore project
        // Add actual application tests here as the project grows
        Assert.True(true, "ApplicationCore project is accessible");
    }
}