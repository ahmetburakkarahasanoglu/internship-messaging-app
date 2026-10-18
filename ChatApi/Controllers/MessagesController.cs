using ChatApi.Dtos;
using ChatApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApi.Controllers;

[ApiController]
[Route("api/conversations/{conversationId:int}/messages")]
public sealed class MessagesController(IMessageService messageService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MessageResponse>>> GetForConversation(
        int conversationId,
        [FromQuery] int? after,
        CancellationToken cancellationToken)
    {
        return Ok(await messageService.GetForConversationAsync(conversationId, after, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> Send(
        int conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await messageService.SendAsync(conversationId, request, cancellationToken);
        return CreatedAtAction(nameof(GetForConversation), new { conversationId, after = message.Id - 1 }, message);
    }
}
