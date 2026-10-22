namespace Locally.Examples.Orders.Tests;

public class HealthTests
{
    [Fact]
    public async Task Healthz_ReturnsOk()
    {
        var body = await Api.Client.GetStringAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.Equal("ok", body);
    }
}
