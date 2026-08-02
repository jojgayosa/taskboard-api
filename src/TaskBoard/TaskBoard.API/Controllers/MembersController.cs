using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskBoard.Application.Features.Members.Commands;
using TaskBoard.Application.Features.Members.Queries;
using TaskBoard.Domain.Enums;

namespace TaskBoard.API.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId:guid}/members")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class MembersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public MembersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers(Guid projectId)
        {
            var result = await _mediator.Send(new GetMembersByProjectQuery(projectId));
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> AddMember(Guid projectId, [FromBody] AddMemberRequest request)
        {
            var result = await _mediator.Send(
                new AddMemberCommand(
                    projectId,
                    request.UserId,
                    request.Role
                    ));

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpPut("{userId:guid}/role")]
        public async Task<IActionResult> UpdateMember(Guid projectId, Guid userId, [FromBody] UpdateRoleRequest request)
        {
            var result = await _mediator.Send(new UpdateMemberRoleCommand(projectId, userId, request.NewRole));

            if (result.Failed)
                return BadRequest(new { errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid projectId, Guid userId)
        {
            var result = await _mediator.Send(new RemoveMemberCommand(projectId, userId));

            if(result.Failed)
                return BadRequest(new {errors = result.Errors});

            return NoContent();
        }
    }
    public record AddMemberRequest(Guid UserId, ProjectRole Role = ProjectRole.Member);
    public record UpdateRoleRequest(ProjectRole NewRole);
}
