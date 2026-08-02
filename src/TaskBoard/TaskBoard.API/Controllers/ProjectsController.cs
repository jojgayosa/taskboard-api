using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using TaskBoard.Application.Features.Projects.Commands;
using TaskBoard.Application.Features.Projects.Queries;

namespace TaskBoard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProjectsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            var result = await _mediator.Send(new GetProjectsQuery());
            return Ok(result.Data);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProject(Guid id)
        {
            var result = await _mediator.Send(new GetProjectByIdQuery(id));
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return CreatedAtAction(
                nameof(GetProject),
                new { id = result.Data!.Id },
                result.Data);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectCommand command)
        {
            //Ensure the route id matches the command id
            if (id != command.Id)
                return BadRequest(new { errors = new[] { "Route id does not match body id" } });

            var result = await _mediator.Send(command);

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            await _mediator.Send(new DeleteProjectCommand(id));

            return NoContent();
        }
    }
}
