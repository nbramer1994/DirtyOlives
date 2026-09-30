using DirtyOlives.Core.Models;
using DirtyOlives.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service) => _service = service;

    /// <summary>Every known name, for the login dropdown.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUser>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    /// <summary>Signs in by name, creating the user on first use.</summary>
    [HttpPost]
    public async Task<ActionResult<AppUser>> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
        {
            return BadRequest("A name is required.");
        }

        return Ok(await _service.GetOrCreateAsync(request.Name, cancellationToken));
    }

    public record SignInRequest(string Name);
}
