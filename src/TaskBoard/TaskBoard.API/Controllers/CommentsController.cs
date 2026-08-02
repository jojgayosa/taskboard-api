using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskBoard.Application.Features.Comments.Commands;
using TaskBoard.Application.Features.Comments.Queries;

namespace TaskBoard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class CommentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CommentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentCommand command)
        {
            var result = await _mediator.Send(command);
            if(result.Failed)
                return BadRequest(new {errors = result.Errors});

            return Ok(result.Data);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateComment(
            Guid id,
            [FromBody] UpdateCommentRequest request)
        {
            var result = await _mediator.Send(new UpdateCommentCommand(id, request.Message));

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var result = await _mediator.Send(new DeleteCommentCommand(id));
            return NoContent();
        }

        [HttpGet("task/{taskId:guid}")]
        public async Task<IActionResult> GetComments(Guid taskId)
        {
            var result = await _mediator.Send(new GetCommentsByTaskQuery(taskId));
            return Ok(result.Data);
        }
    }

    public record UpdateCommentRequest(string Message);
}
