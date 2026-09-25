using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using EndpointMaxResponseTime.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace EndpointMaxResponseTime.Tests;

public class SubmissionsTests
{
    private WebApplicationFactory<Program> _factory;
    private WireMockServer _server;

    [SetUp]
    public void Setup()
    {
        _server = WireMockServer.Start();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(
                (_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?> { ["BaseAddress"] = _server.Url }
                    );
                }
            );
        });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
        _server.Dispose();
    }

    [Test]
    public async Task ImmediateResponseTest()
    {
        SetUpApiResponses(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        using var httpClient = _factory.CreateClient();

        var stopwatch = Stopwatch.StartNew();
        var response = await httpClient.PostAsync("/submissions", new StringContent(string.Empty));
        stopwatch.Stop();
        response.EnsureSuccessStatusCode();
        var submissionFromCreate = await response.Content.ReadFromJsonAsync<Submission>();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500));
        Assert.That(submissionFromCreate, Is.Not.Null);
        Assert.That(submissionFromCreate.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(submissionFromCreate.Phase1CompletedAt, Is.Not.Null);
        Assert.That(submissionFromCreate.Phase2CompletedAt, Is.Not.Null);

        var submissionFromGet = await httpClient.GetFromJsonAsync<Submission>(
            $"/submissions/{submissionFromCreate.Id}"
        );
        Assert.That(submissionFromGet, Is.EqualTo(submissionFromCreate).UsingPropertiesComparer());
    }

    [Test]
    public async Task SlowResponseAfterPointOfNoReturnTest()
    {
        SetUpApiResponses(TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(0.6));

        using var httpClient = _factory.CreateClient();

        var stopwatch = Stopwatch.StartNew();
        var response = await httpClient.PostAsync("/submissions", new StringContent(string.Empty));
        stopwatch.Stop();
        response.EnsureSuccessStatusCode();
        var submissionFromCreate = await response.Content.ReadFromJsonAsync<Submission>();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.InRange(1000, 1100));
        Assert.That(submissionFromCreate, Is.Not.Null);
        Assert.That(submissionFromCreate.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(submissionFromCreate.Phase1CompletedAt, Is.Not.Null);
        Assert.That(submissionFromCreate.Phase2CompletedAt, Is.Null);

        await Task.Delay(TimeSpan.FromSeconds(1));

        var submissionFromGet = await httpClient.GetFromJsonAsync<Submission>(
            $"/submissions/{submissionFromCreate.Id}"
        );
        Assert.That(submissionFromGet, Is.Not.Null);
        Assert.That(submissionFromGet.Id, Is.EqualTo(submissionFromCreate.Id));
        Assert.That(submissionFromGet.Phase1CompletedAt, Is.Not.Null);
        Assert.That(submissionFromGet.Phase2CompletedAt, Is.Not.Null);
    }

    [Test]
    public async Task SlowResponseBeforePointOfNoReturnTest()
    {
        SetUpApiResponses(TimeSpan.FromSeconds(1.1), TimeSpan.FromSeconds(1.1));

        using var httpClient = _factory.CreateClient();

        var allSubmissionsBefore = await httpClient.GetFromJsonAsync<ICollection<Submission>>(
            $"/submissions"
        );
        Assert.That(allSubmissionsBefore, Is.Not.Null);

        var stopwatch = Stopwatch.StartNew();
        var response = await httpClient.PostAsync("/submissions", new StringContent(string.Empty));
        stopwatch.Stop();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.GatewayTimeout));
        Assert.That(stopwatch.ElapsedMilliseconds, Is.InRange(1000, 1100));

        await Task.Delay(TimeSpan.FromSeconds(2));

        var allSubmissionsAfter = await httpClient.GetFromJsonAsync<ICollection<Submission>>(
            $"/submissions"
        );

        Assert.That(allSubmissionsAfter, Is.Not.Null);
        Assert.That(allSubmissionsAfter.Count, Is.EqualTo(allSubmissionsBefore.Count));
    }

    private void SetUpApiResponses(TimeSpan phase1Delay, TimeSpan phase2Delay)
    {
        _server
            .Given(Request.Create().WithPath("/phase1"))
            .RespondWith(
                Response
                    .Create()
                    .WithBodyAsJson(_ => new ApiResponse(DateTime.Now))
                    .WithDelay(phase1Delay)
            );

        _server
            .Given(Request.Create().WithPath("/phase2"))
            .RespondWith(
                Response
                    .Create()
                    .WithBodyAsJson(_ => new ApiResponse(DateTime.Now))
                    .WithDelay(phase2Delay)
            );
    }
}
