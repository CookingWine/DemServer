using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Server.Infrastructure;

/// <summary>
/// 全局异常兜底：把未处理异常统一转成 RFC 7807 ProblemDetails，
/// 避免把堆栈直接吐给桌面客户端。
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "请求 {Method} {Path} 时发生未处理异常",
            httpContext.Request.Method, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Title = "服务器内部错误",
            Status = StatusCodes.Status500InternalServerError,
            Detail = _env.IsDevelopment()
                ? exception.ToString()
                : "服务处理请求时发生错误，请稍后重试。",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
