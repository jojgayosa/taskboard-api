using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskBoard.Application.Features.Columns.Commands;
using TaskBoard.Application.Features.Columns.DTOs;
using TaskBoard.Application.Features.Columns.Queries;

namespace TaskBoard.API.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId:guid}/columns")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class ColumnsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ColumnsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetColumns(Guid projectId)
        {
            var result = await _mediator.Send(new GetColumnsByProjectQuery(projectId));

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateColumn(
            Guid projectId,
            [FromBody] CreateColumnRequest request)
        {
            var result = await _mediator.Send(new CreateColumnCommand(projectId, request.Name));

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateColumn(
            Guid id,
            Guid projectId,
            [FromBody] UpdateColumnRequest request)
        {
            var result = await _mediator.Send(new UpdateColumnCommand(id, projectId, request.Name));

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteColumn(
            Guid id,
            Guid projectId)
        {
            await _mediator.Send(new DeleteColumnCommand(id, projectId));

            return NoContent();
        }

        [HttpPatch("reorder")]
        public async Task<IActionResult> ReorderColumns(
            Guid projectId,
            [FromBody] List<ReorderColumnDto> columns)
        {
            await _mediator.Send(new ReorderColumnsCommand(projectId, columns));

            return NoContent();
        }
    }

    // Small request models — keeps route params separate from body params
    public record CreateColumnRequest(string Name);
    public record UpdateColumnRequest(string Name);
}
