using EndpointMaxResponseTime.Models;

namespace EndpointMaxResponseTime.Services;

public class SubmissionsService(ApiClient client, SubmissionsRepository repository)
{
    public async Task<Guid> CreateAsync(CancellationToken cancellationToken)
    {
        var submission = new Submission(Guid.NewGuid(), null, null);
        try
        {
            await ProcessAsync(submission, cancellationToken).WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The request timeout elapsed; processing may continue if it
            // already reached phase 2, which uses CancellationToken.None.
        }
        return submission.Id;
    }

    private async Task ProcessAsync(Submission submission, CancellationToken cancellationToken)
    {
        var phase1CompletedAt = await client.ProcessPhase1Async(submission.Id, cancellationToken);
        submission = submission with { Phase1CompletedAt = phase1CompletedAt };
        repository.Add(submission);

        var phase2CompletedAt = await client.ProcessPhase2Async(
            submission.Id,
            CancellationToken.None
        );
        submission = submission with { Phase2CompletedAt = phase2CompletedAt };
        repository.Add(submission);
    }
}
