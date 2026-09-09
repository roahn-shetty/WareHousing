using Microsoft.AspNetCore.Mvc;
using OopsBlazorStudyWebApp;

namespace OopsBlazorStudyWebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UserMasterController : ControllerBase
{
    private readonly IUserMasterRepository _repository;

    public UserMasterController(IUserMasterRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleLookupDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _repository.GetActiveRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserMasterListDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _repository.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{userMasterId:int}")]
    public async Task<ActionResult<UserMasterDetailDto>> GetUser(int userMasterId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdAsync(userMasterId, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] UserMasterSaveRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            ModelState.AddModelError(nameof(request.Password), "Password is required.");
            ModelState.AddModelError(nameof(request.ConfirmPassword), "Confirm Password is required.");
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(request.ConfirmPassword), "Password and Confirm Password must match.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userMasterId = await _repository.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { userMasterId }, new { userMasterId });
    }

    [HttpPut("{userMasterId:int}")]
    public async Task<ActionResult> Update(int userMasterId, [FromBody] UserMasterSaveRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _repository.UpdateUserAsync(userMasterId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
