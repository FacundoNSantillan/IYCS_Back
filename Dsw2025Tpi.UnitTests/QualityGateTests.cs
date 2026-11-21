namespace Dsw2025Tpi.UnitTests;

public class QualityGateTests
{
    [Fact]
    public void QualityGate_DeliberatelyFailingTest_BlocksDockerBuildAndDeploy()
    {
        var expected = 1;
        var actual = 2;

        Assert.Equal(expected, actual);
    }
}
