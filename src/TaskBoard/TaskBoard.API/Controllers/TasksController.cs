using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskBoard.Application.Features.Tasks.Commands;
using TaskBoard.Application.Features.Tasks.Queries;
using TaskBoard.Domain.Enums;

namespace TaskBoard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;
        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("column/{columnId:guid}")]
        public async Task<IActionResult> GetTaskByColumn(
            Guid columnId,
            [FromQuery] Priority? priority = null)
        {
            var result = await _mediator.Send(new GetTasksByColumnQuery(columnId, priority));

            return Ok(result.Data);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTask(Guid id)
        {
            var result = await _mediator.Send(new GetTaskByIdQuery(id));

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return CreatedAtAction(
                nameof(GetTask),
                new { id = result.Data!.Id },
                result.Data);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTask(
            Guid id,
            [FromBody] UpdateTaskRequest request)
        {
            var result = await _mediator.Send(new UpdateTaskCommand(
                id,
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate,
                request.AssignedUserId));

            if (result.Failed)
                return BadRequest(new {errors = result.Errors});

            return Ok(result.Data);
        }

        [HttpPatch("{id:guid}/move")]
        public async Task<IActionResult> MoveTask(
       Guid id,
       [FromBody] MoveTaskRequest request)
        {
            await _mediator.Send(new MoveTaskCommand(id, request.TargetColumnId));
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            await _mediator.Send(new DeleteTaskCommand(id));
            return NoContent();
        }

    }

    public record UpdateTaskRequest(
        string Title,
        string? Description,
        Priority Priority,
        DateTime? DueDate,
        Guid? AssignedUserId);

    public record MoveTaskRequest(Guid TargetColumnId);
}
