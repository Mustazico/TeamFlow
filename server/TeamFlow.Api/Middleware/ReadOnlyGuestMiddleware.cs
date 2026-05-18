namespace TeamFlow.Api.Middleware;

/// <summary>
/// Hard read-only guard for accounts in the "Guest" role.
/// Any non-safe HTTP method is rejected with 403 BEFORE controllers run,
/// so guest demo users physically cannot mutate state regardless of
/// service-level role checks.
/// </summary>
public class ReadOnlyGuestMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "HEAD",
        "OPTIONS",
    };

    private readonly RequestDelegate _next;

    public ReadOnlyGuestMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true
            && ctx.User.IsInRole("Guest")
            && !SafeMethods.Contains(ctx.Request.Method))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                "{\"error\":\"This is a read-only guest account on the public demo. Sign in with a real account to make changes.\"}");
            return;
        }

        await _next(ctx);
    }
}
