namespace ProjectNamePlaceholder.UnitTests;

public class SolutionSmokeTests
{
    [Fact]
    public void Layers_AllLoad()
    {
        foreach (var layer in new[] { "Domain", "Application", "Infrastructure", "Persistence" })
        {
            Assert.NotNull(System.Reflection.Assembly.Load($"ProjectNamePlaceholder.{layer}"));
        }
    }
}
