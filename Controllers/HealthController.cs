using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Controllers;

/// <summary>
/// 健康检查：桌面客户端启动时可以调这个接口确认后台服务是否就绪。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var canConnect = await _db.Database.CanConnectAsync(ct);

        var payload = new
        {
            status = canConnect ? "healthy" : "unhealthy",
            database = canConnect ? "up" : "down",
            version = typeof(Program).Assembly.GetName().Version?.ToString(),
            serverTime = DateTimeOffset.UtcNow
        };

        return canConnect
            ? Ok(payload)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
    }
}
