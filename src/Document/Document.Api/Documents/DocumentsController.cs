using Document.Api.Auth;
using Document.Application.Documents.CompleteDocumentUpload;
using Document.Application.Documents.CreateDocumentUpload;
using Document.Application.Documents.ListDocuments;
using Document.Application.Documents.RequestDocumentDownload;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Document.Api.Documents;

[Authorize]
[ApiController]
[Route("documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly CreateDocumentUploadHandler _create;
    private readonly CompleteDocumentUploadHandler _complete;
    private readonly ListDocumentsHandler _list;
    private readonly RequestDocumentDownloadHandler _download;

    public DocumentsController(
        CreateDocumentUploadHandler create,
        CompleteDocumentUploadHandler complete,
        ListDocumentsHandler list,
        RequestDocumentDownloadHandler download)
    {
        _create = create;
        _complete = complete;
        _list = list;
        _download = download;
    }

    [Authorize(Policy = DocumentAuth.UploadPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentUploadCommand command,
        CancellationToken cancellationToken)
    {
        var outcome = await _create.Handle(command, cancellationToken);
        return outcome.Kind switch
        {
            CreateDocumentUploadKind.Succeeded => Ok(outcome.Result),
            CreateDocumentUploadKind.Invalid => BadRequest(TitleProblem(outcome.Error)),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [Authorize(Policy = DocumentAuth.UploadPolicy)]
    [HttpPost("{documentId:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var outcome = await _complete.Handle(
            new CompleteDocumentUploadCommand(documentId),
            cancellationToken);

        return outcome.Kind switch
        {
            CompleteDocumentUploadKind.Succeeded => Ok(outcome.Result),
            CompleteDocumentUploadKind.NotFound => NotFound(),
            CompleteDocumentUploadKind.Invalid => BadRequest(TitleProblem(outcome.Error)),
            CompleteDocumentUploadKind.Conflict => Conflict(TitleProblem(outcome.Error)),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [Authorize(Policy = DocumentAuth.ReadPolicy)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid caseId,
        CancellationToken cancellationToken)
    {
        if (caseId == Guid.Empty)
            return BadRequest(TitleProblem("caseId is required."));

        var result = await _list.Handle(new ListDocumentsQuery(caseId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = DocumentAuth.ReadPolicy)]
    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var outcome = await _download.Handle(
            new RequestDocumentDownloadQuery(documentId),
            cancellationToken);

        return outcome.Kind switch
        {
            RequestDocumentDownloadKind.Succeeded => Ok(outcome.Result),
            RequestDocumentDownloadKind.NotFound => NotFound(),
            RequestDocumentDownloadKind.Forbidden => Forbid(),
            RequestDocumentDownloadKind.Conflict => Conflict(TitleProblem(outcome.Error)),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private static ProblemDetails TitleProblem(string? title) =>
        new() { Title = title };
}