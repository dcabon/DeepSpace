using DeepSpace.Infrastructure.Data;

namespace DeepSpace.Web.Services;

public class HttpAuditActorProvider : IAuditActorProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuditActorProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditActor? Current
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            var login = context?.User.Identity is { IsAuthenticated: true } identity ? identity.Name : null;

            return context == null || string.IsNullOrEmpty(login)
                ? null
                : new AuditActor(login, DescribeRequest(context), DescribeIp(context));
        }
    }

    public static string DescribeRequest(HttpContext context)
    {
        var request = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
        return request.Length > 300 ? request[..300] : request;
    }

    public static string? DescribeIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString();
}
