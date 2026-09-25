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
    private WebApplicationUnderTest _application;

    [SetUp]
    public void Setup()
    {
        _application = new WebApplicationUnderTest();
    }

    [TearDown]
    public void TearDown()
    {
        _application.Dispose();
    }

    [Test]
    public async Task ImmediateResponseTest()
    {
        var scenario = new SubmissionScenario(
            _application,
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(1)
        );

        var submissionFromCreate = await scenario.CreateSubmissionAsync();
        Assert.That(scenario.LastResponseTimeMilliseconds, Is.LessThan(500));
        Assert.That(submissionFromCreate, Is.Not.Null);
        Assert.That(submissionFromCreate.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(submissionFromCreate.Phase1CompletedAt, Is.Not.Null);
        Assert.That(submissionFromCreate.Phase2CompletedAt, Is.Not.Null);

        var submissionFromGet = await scenario.GetSubmissionAsync();
        Assert.That(submissionFromGet, Is.EqualTo(submissionFromCreate).UsingPropertiesComparer());
    }

    [Test]
    public async Task SlowResponseAfterPointOfNoReturnTest()
    {
        var scenario = new SubmissionScenario(
            _application,
            TimeSpan.FromSeconds(0.6),
            TimeSpan.FromSeconds(0.6)
        );

        var submissionFromCreate = await scenario.CreateSubmissionAsync();
        Assert.That(scenario.LastResponseTimeMilliseconds, Is.InRange(1000, 1100));
        Assert.That(submissionFromCreate, Is.Not.Null);
        Assert.That(submissionFromCreate.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(submissionFromCreate.Phase1CompletedAt, Is.Not.Null);
        Assert.That(submissionFromCreate.Phase2CompletedAt, Is.Null);

        await Task.Delay(TimeSpan.FromSeconds(1));

        var submissionFromGet = await scenario.GetSubmissionAsync();
        Assert.That(submissionFromGet, Is.Not.Null);
        Assert.That(submissionFromGet.Id, Is.EqualTo(submissionFromCreate.Id));
        Assert.That(submissionFromGet.Phase1CompletedAt, Is.Not.Null);
        Assert.That(submissionFromGet.Phase2CompletedAt, Is.Not.Null);
    }

    [Test]
    public async Task SlowResponseBeforePointOfNoReturnTest()
    {
        var scenario = new SubmissionScenario(
            _application,
            TimeSpan.FromSeconds(1.1),
            TimeSpan.FromSeconds(1.1)
        );

        var allSubmissionsBefore = await scenario.GetAllSubmissionsAsync();
        Assert.That(allSubmissionsBefore, Is.Not.Null);

        await scenario.CreateSubmissionAsync();
        Assert.That(scenario.LastResponseStatusCode, Is.EqualTo(HttpStatusCode.GatewayTimeout));
        Assert.That(scenario.LastResponseTimeMilliseconds, Is.InRange(1000, 1100));

        await Task.Delay(TimeSpan.FromSeconds(2));

        var allSubmissionsAfter = await scenario.GetAllSubmissionsAsync();

        Assert.That(allSubmissionsAfter, Is.Not.Null);
        Assert.That(allSubmissionsAfter.Count, Is.EqualTo(allSubmissionsBefore.Count));
    }
}
