using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using EndpointMaxResponseTime.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace EndpointMaxResponseTime.Tests;

public class SubmissionScenario : IDisposable
{
    private readonly WebApplicationUnderTest _application;
    private readonly HttpClient _httpClient;

    private Guid _lastSubmissionId;

    public HttpStatusCode LastResponseStatusCode { get; private set; }
    public long LastResponseTimeMilliseconds { get; private set; }

    public SubmissionScenario(
        WebApplicationUnderTest application,
        TimeSpan phase1Delay,
        TimeSpan phase2Delay
    )
    {
        _application = application;
        _httpClient = _application.Factory.CreateClient();
        SetUpApiResponses(phase1Delay, phase2Delay);
    }

    private void SetUpApiResponses(TimeSpan phase1Delay, TimeSpan phase2Delay)
    {
        _application.Server.ResetMappings();
        _application
            .Server.Given(Request.Create().WithPath("/phase1"))
            .RespondWith(
                Response
                    .Create()
                    .WithBodyAsJson(_ => new ApiResponse(DateTime.Now))
                    .WithDelay(phase1Delay)
            );

        _application
            .Server.Given(Request.Create().WithPath("/phase2"))
            .RespondWith(
                Response
                    .Create()
                    .WithBodyAsJson(_ => new ApiResponse(DateTime.Now))
                    .WithDelay(phase2Delay)
            );
    }

    public async Task<Submission?> CreateSubmissionAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _httpClient.PostAsync("/submissions", new StringContent(string.Empty));
        stopwatch.Stop();
        LastResponseStatusCode = response.StatusCode;
        LastResponseTimeMilliseconds = stopwatch.ElapsedMilliseconds;
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var submission = await response.Content.ReadFromJsonAsync<Submission>();
        if (submission != null)
        {
            _lastSubmissionId = submission.Id;
        }
        return submission;
    }

    public async Task<Submission?> GetSubmissionAsync()
    {
        return await _httpClient.GetFromJsonAsync<Submission>($"/submissions/{_lastSubmissionId}");
    }

    public async Task<ICollection<Submission>?> GetAllSubmissionsAsync()
    {
        return await _httpClient.GetFromJsonAsync<ICollection<Submission>>($"/submissions");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
