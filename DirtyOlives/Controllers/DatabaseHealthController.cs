using DirtyOlives.Core.Models;
using DirtyOlives.Data;
using DirtyOlives.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class DatabaseHealthController : ControllerBase
{
    /// <summary>User id reserved for connectivity probes so real ratings are never touched.</summary>
    private const int TestUserId = 999_999;

    private readonly MartiniDbContext _db;
    private readonly MartiniRatingService _service;
    private readonly DatabaseStartupReport _startupReport;
    private readonly ILogger<DatabaseHealthController> _logger;

    public DatabaseHealthController(
        MartiniDbContext db,
        MartiniRatingService service,
        DatabaseStartupReport startupReport,
        ILogger<DatabaseHealthController> logger)
    {
        _db = db;
        _service = service;
        _startupReport = startupReport;
        _logger = logger;
    }

    /// <summary>Outcome of the check performed during application startup.</summary>
    [HttpGet("startup")]
    public ActionResult<DatabaseStatus> GetStartupStatus() => Ok(_startupReport.Status);

    /// <summary>Opens a connection right now, without writing anything.</summary>
    [HttpGet]
    public async Task<ActionResult<DatabaseStatus>> GetCurrentStatus(CancellationToken cancellationToken)
    {
        var status = NewStatus();

        try
        {
            if (await _db.Database.CanConnectAsync(cancellationToken))
            {
                status.IsHealthy = true;
                status.Message = "Database connection OK.";
            }
            else
            {
                status.Message = "Database did not accept the connection.";
            }
        }
        catch (Exception ex)
        {
            Fail(status, ex, "Database connection failed.");
        }

        return Ok(status);
    }

    /// <summary>
    /// Full round trip: insert a throwaway rating, read it back, then delete it.
    /// Proves the connection, the schema and write permissions all work.
    /// </summary>
    [HttpPost("write-test")]
    public async Task<ActionResult<DatabaseStatus>> RunWriteTest(CancellationToken cancellationToken)
    {
        var status = NewStatus();
        MartiniRating? saved = null;

        try
        {
            saved = await _service.AddAsync(new MartiniRating
            {
                UserId = TestUserId,
                Location = "Connection test",
                Vodka = "Connection test",
                OliveType = "Connection test",
                OliveCount = 3,
                GlassStyle = GlassStyle.Classic,
                GlassRating = 5,
                OlivesRating = 5,
                MixtureRating = 5,
                VodkaRating = 5,
                DateRated = DateTime.Today
            }, cancellationToken);

            var readBack = await _db.Ratings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == saved.Id, cancellationToken);

            if (readBack is null)
            {
                status.Message = "Write succeeded but the row could not be read back.";
                return Ok(status);
            }

            status.IsHealthy = true;
            status.Message = "Write test passed - saved, read back and cleaned up.";
        }
        catch (Exception ex)
        {
            Fail(status, ex, "Write test failed.");
        }
        finally
        {
            if (saved is not null)
            {
                try
                {
                    await _service.DeleteAsync(saved.Id, TestUserId, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx.GetBaseException(),
                        "Could not remove the test rating {Id}.", saved.Id);
                }
            }
        }

        return Ok(status);
    }

    private DatabaseStatus NewStatus() => new()
    {
        Provider = _db.Database.ProviderName ?? "unknown",
        DataSource = _db.Database.GetDbConnection().DataSource ?? "unknown",
        CheckedAt = DateTimeOffset.UtcNow
    };

    private void Fail(DatabaseStatus status, Exception ex, string message)
    {
        var root = ex.GetBaseException();
        status.IsHealthy = false;
        status.Message = message;
        status.Error = $"{root.GetType().Name}: {root.Message}";
        _logger.LogError(root, "{Message}", message);
    }
}
