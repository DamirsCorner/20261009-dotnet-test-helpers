using EndpointMaxResponseTime.Models;

namespace EndpointMaxResponseTime.Services;

public class ApiClient(HttpClient httpClient)
{
    public Task<DateTime> ProcessPhase1Async(Guid submissionId, CancellationToken cancellationToken)
    {
        return ProcessAsync("/phase1", submissionId, cancellationToken);
    }

    public Task<DateTime> ProcessPhase2Async(Guid submissionId, CancellationToken cancellationToken)
    {
        return ProcessAsync("/phase2", submissionId, cancellationToken);
    }

    private async Task<DateTime> ProcessAsync(
        string requestUri,
        Guid submissionId,
        CancellationToken cancellationToken
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            requestUri,
            new ApiRequest(submissionId),
            cancellationToken: cancellationToken
        );
        response.EnsureSuccessStatusCode();
        var parsedResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(
            cancellationToken: cancellationToken
        );
        return parsedResponse!.CompletedAt;
    }
}
