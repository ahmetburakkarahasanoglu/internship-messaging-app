using ChatApi.Dtos;
using ChatApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApi.Controllers;

[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController(IConversationService conversationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ConversationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationResponse>> Create(
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, conversation);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ConversationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await conversationService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ConversationDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDetailResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await conversationService.GetByIdAsync(id, cancellationToken));
}
