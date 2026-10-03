using Microsoft.AspNetCore.Mvc;
using OopsBlazorStudyWebApp;

namespace OopsBlazorStudyWebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MenuMasterController : ControllerBase
{
    private readonly IMenuMasterRepository _repository;

    public MenuMasterController(IMenuMasterRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MenuMasterListDto>>> GetMenus(CancellationToken cancellationToken)
    {
        var menus = await _repository.GetMenusAsync(cancellationToken);
        return Ok(menus);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] MenuMasterSaveRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var createdBy = GetCreatedBy();
        var createdOn = GetCreatedOn();
        var id = await _repository.CreateMenuAsync(request, createdBy, createdOn, cancellationToken);
        return CreatedAtAction(nameof(GetMenus), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] MenuMasterUpdateRequest request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest("Menu Id is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _repository.UpdateMenuStatusAsync(id, request, GetCreatedBy(), DateTime.Now, cancellationToken);
        if (!updated)
        {
            return NotFound($"Menu entry with Id {id} was not found.");
        }

        return NoContent();
    }

    private string GetCreatedBy()
    {
        if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name))
        {
            return User.Identity.Name;
        }

        if (Request.Headers.TryGetValue("X-Created-By", out var createdByHeader) &&
            !string.IsNullOrWhiteSpace(createdByHeader.ToString()))
        {
            return createdByHeader.ToString();
        }

        return "System";
    }

    private DateTime GetCreatedOn()
    {
        var loginAtClaim = User.FindFirst("LoginAt")?.Value;
        if (!string.IsNullOrWhiteSpace(loginAtClaim) &&
            DateTime.TryParse(loginAtClaim, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var loginAt))
        {
            return loginAt;
        }

        if (Request.Headers.TryGetValue("X-Login-At", out var loginAtHeader) &&
            DateTime.TryParse(loginAtHeader.ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var headerLoginAt))
        {
            return headerLoginAt;
        }

        return DateTime.Now;
    }
}
