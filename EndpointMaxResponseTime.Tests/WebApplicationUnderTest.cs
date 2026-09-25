using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Server;

namespace EndpointMaxResponseTime.Tests;

public class WebApplicationUnderTest : IDisposable
{
    public WebApplicationFactory<Program> Factory { get; }
    public WireMockServer Server { get; }

    public WebApplicationUnderTest()
    {
        Server = WireMockServer.Start();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(
                (_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?> { ["BaseAddress"] = Server.Url }
                    );
                }
            );
        });
    }

    public void Dispose()
    {
        Factory.Dispose();
        Server.Dispose();
    }
}
