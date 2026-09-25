using System.Net;
using EndpointMaxResponseTime.Services;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace EndpointMaxResponseTime.Controllers;

[ApiController]
[Route("[controller]")]
public class SubmissionsController(
    SubmissionsService submissionsService,
    SubmissionsRepository submissionsRepository
) : ControllerBase
{
    [HttpPost]
    [RequestTimeout(1000)]
    public async Task<IActionResult> Create()
    {
        var submissionId = await submissionsService.CreateAsync(HttpContext.RequestAborted);
        var submission = submissionsRepository.Get(submissionId);
        if (submission == null)
        {
            return StatusCode((int)HttpStatusCode.GatewayTimeout);
        }
        if (!submission.Phase2CompletedAt.HasValue)
        {
            return Accepted(submission);
        }
        return Ok(submission);
    }

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
    {
        var submission = submissionsRepository.Get(id);

        if (submission == null)
        {
            return NotFound();
        }

        return Ok(submission);
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(submissionsRepository.GetAll());
    }
}
