using ApiGateway.Configuration;
using Microsoft.Extensions.Options;

namespace ApiGateway.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityConfig _config;
    
    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<GatewayOptions> options)
    {
        _next = next;
        _config = options.Value.Security;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        
        if (!_config.EnableSecurityHeaders)
            return;
        
        var headers = context.Response.Headers;
        
        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";
        
        // Prevent clickjacking
        headers["X-Frame-Options"] = "DENY";
        
        // Enable XSS protection
        headers["X-XSS-Protection"] = "1; mode=block";
        
        // HSTS for HTTPS
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
        
        // Remove server identification headers
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
    }
}
