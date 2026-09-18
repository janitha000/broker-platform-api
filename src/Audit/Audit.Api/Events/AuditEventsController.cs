using Audit.Api.Auth;
using Audit.Application.Events.ListAuditEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Api.Events;

[Authorize(Policy = AuditAuth.ReadPolicy)]
[ApiController]
[Route("audit")]
public sealed class AuditEventsController : ControllerBase
{
    private readonly ListAuditEventsHandler _list;

    public AuditEventsController(ListAuditEventsHandler list)
    {
        _list = list;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? caseId,
        [FromQuery] string? action,
        [FromQuery] string? outcome,
        [FromQuery] int take = ListAuditEventsHandler.DefaultTake,
        CancellationToken cancellationToken = default)
    {
        var result = await _list.Handle(
            new ListAuditEventsQuery(from, to, caseId, action, outcome, take),
            cancellationToken);
        return Ok(result);
    }
}
