using System.Collections.Concurrent;
using EndpointMaxResponseTime.Models;

namespace EndpointMaxResponseTime.Services;

public class SubmissionsRepository
{
    private readonly ConcurrentDictionary<Guid, Submission> _submissions = new();

    public ICollection<Submission> GetAll()
    {
        return _submissions.Values;
    }

    public Submission? Get(Guid id)
    {
        return _submissions.GetValueOrDefault(id);
    }

    public void Add(Submission submission)
    {
        _submissions.AddOrUpdate(submission.Id, submission, (id, old) => submission);
    }
}
