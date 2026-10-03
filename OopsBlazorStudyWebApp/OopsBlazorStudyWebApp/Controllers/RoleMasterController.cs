using Microsoft.AspNetCore.Mvc;
using OopsBlazorStudyWebApp;

namespace OopsBlazorStudyWebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RoleMasterController : ControllerBase
{
    private readonly IRoleMasterRepository _repository;

    public RoleMasterController(IRoleMasterRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleMasterListDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _repository.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet("{roleId:int}")]
    public async Task<ActionResult<RoleMasterDetailDto>> GetRole(int roleId, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByIdAsync(roleId, cancellationToken);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] RoleMasterSaveRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var roleId = await _repository.CreateRoleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRole), new { roleId }, new { roleId });
    }

    [HttpPut("{roleId:int}")]
    public async Task<ActionResult> Update(int roleId, [FromBody] RoleMasterSaveRequest request, CancellationToken cancellationToken)
    {
        if (roleId <= 0)
        {
            return BadRequest("Role Id is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _repository.UpdateRoleAsync(roleId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
