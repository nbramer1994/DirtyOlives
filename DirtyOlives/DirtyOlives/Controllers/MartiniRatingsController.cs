using DirtyOlives.Core.Models;
using DirtyOlives.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MartiniRatingsController : ControllerBase
{
    private readonly MartiniRatingService _service;

    public MartiniRatingsController(MartiniRatingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MartiniRating>>> GetForUser(
        [FromQuery] int userId = MartiniRating.DefaultUserId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetForUserAsync(userId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<MartiniRating>> Add(MartiniRating rating, CancellationToken cancellationToken)
    {
        var saved = await _service.AddAsync(rating, cancellationToken);
        return Ok(saved);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] int userId = MartiniRating.DefaultUserId,
        CancellationToken cancellationToken = default)
    {
        return await _service.DeleteAsync(id, userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
