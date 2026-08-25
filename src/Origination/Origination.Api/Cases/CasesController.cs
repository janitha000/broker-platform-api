using Origination.Application.Cases.CreateCase;
using Origination.Application.Cases.GetCase;
using Microsoft.AspNetCore.Mvc;
using Origination.Application.Cases.CompleteFactFind;

namespace Origination.Api.Cases;

[ApiController]
[Route("cases")]
public sealed class CasesController : ControllerBase
{
    private readonly CreateCaseHandler _createCaseHandler;
    private readonly GetCaseHandler _getCaseHandler;
    private readonly CompleteFactFindHandler _completeFactFindHandler;

    public CasesController(CreateCaseHandler createCaseHandler, GetCaseHandler getCaseHandler, CompleteFactFindHandler completeFactFindHandler)
    {
        _createCaseHandler = createCaseHandler;
        _getCaseHandler = getCaseHandler;
        _completeFactFindHandler = completeFactFindHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCase([FromBody] CreateCaseCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _createCaseHandler.Handle(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { caseId = result.CaseId }, result);

    }

    [HttpGet("{caseId:guid}")]
    public async Task<IActionResult> Get(Guid caseId, CancellationToken cancellationToken = default)
    {
        var result = await _getCaseHandler.Handle(new GetCaseQuery(caseId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPut("{caseId:guid}/fact-find")]
    public async Task<IActionResult> CompleteFactFind(Guid caseId, [FromBody] CompleteFactFindCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _completeFactFindHandler.Handle(command with { CaseId = caseId }, cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}