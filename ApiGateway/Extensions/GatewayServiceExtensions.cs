using ApiGateway.Configuration;
using ApiGateway.Health;
using ApiGateway.Observability;
using ApiGateway.Services;
using ApiGateway.Transformers;
using BuildingBlocks.CrossCutting.Caching.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;

namespace ApiGateway.Extensions;

public static class GatewayServiceExtensions
{
    public static IServiceCollection AddApiGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind GatewayOptions from configuration
        var gatewayOptions = new GatewayOptions();
        configuration.GetSection("Gateway").Bind(gatewayOptions);
        services.Configure<GatewayOptions>(configuration.GetSection("Gateway"));
        
        // Add YARP reverse proxy with dynamic route builder
        services.AddSingleton<YarpRouteBuilder>();
        services.AddSingleton<Yarp.ReverseProxy.Configuration.IProxyConfigProvider>(sp => 
            sp.GetRequiredService<YarpRouteBuilder>());
        services.AddReverseProxy();
        
        // Add HttpClient for downstream calls with resilience policies
        // Create a logger for resilience events
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var resilienceLogger = loggerFactory.CreateLogger("ApiGateway.Resilience");
        
        services.AddHttpClient("downstream")
            .AddStandardResilienceHandler(options =>
            {
                // Configure retry policy with exponential backoff
                options.Retry.MaxRetryAttempts = gatewayOptions.Resilience.RetryCount;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(gatewayOptions.Resilience.RetryDelaySeconds);
                
                // Configure retry conditions (transient HTTP errors and timeouts)
                options.Retry.ShouldHandle = new Polly.PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response => 
                        response.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                        response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout);
                
                // Add retry attempt logging
                options.Retry.OnRetry = args =>
                {
                    resilienceLogger.LogWarning(
                        "Retry attempt {AttemptNumber} for {Method} {Uri} after {Delay}ms. Reason: {Outcome}",
                        args.AttemptNumber,
                        args.Outcome.Result?.RequestMessage?.Method,
                        args.Outcome.Result?.RequestMessage?.RequestUri,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return ValueTask.CompletedTask;
                };
                
                // Configure circuit breaker policy
                options.CircuitBreaker.FailureRatio = gatewayOptions.Resilience.CircuitBreakerFailureRatio;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(
                    gatewayOptions.Resilience.CircuitBreakerBreakDurationSeconds);
                
                // Add circuit breaker state transition logging
                options.CircuitBreaker.OnOpened = args =>
                {
                    resilienceLogger.LogError(
                        "Circuit breaker opened for downstream service. Break duration: {BreakDuration}s. Reason: {Outcome}",
                        args.BreakDuration.TotalSeconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return ValueTask.CompletedTask;
                };
                
                options.CircuitBreaker.OnClosed = args =>
                {
                    resilienceLogger.LogInformation("Circuit breaker closed for downstream service. Service is healthy again.");
                    return ValueTask.CompletedTask;
                };
                
                options.CircuitBreaker.OnHalfOpened = args =>
                {
                    resilienceLogger.LogInformation("Circuit breaker half-opened for downstream service. Testing if service recovered.");
                    return ValueTask.CompletedTask;
                };
                
                // Configure timeout policy
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(
                    gatewayOptions.Resilience.TimeoutSeconds);
            });
        
        // Add HttpClient for health checks
        services.AddHttpClient("health");
        
        // Add JWT Authentication
        if (gatewayOptions.Authentication.Enabled)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = gatewayOptions.Authentication.Authority;
                    options.Audience = gatewayOptions.Authentication.Audience;
                    options.RequireHttpsMetadata = false; // For development
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = gatewayOptions.Authentication.ValidateIssuer,
                        ValidateAudience = gatewayOptions.Authentication.ValidateAudience,
                        ValidateLifetime = gatewayOptions.Authentication.ValidateLifetime,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromSeconds(gatewayOptions.Authentication.ClockSkewSeconds)
                    };
                });
        }
        
        // Add Authorization
        services.AddAuthorization();
        
        // Add CORS
        if (gatewayOptions.Security.EnableCors)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    if (gatewayOptions.Security.AllowedOrigins.Any())
                    {
                        builder.WithOrigins(gatewayOptions.Security.AllowedOrigins.ToArray());
                    }
                    else
                    {
                        builder.AllowAnyOrigin();
                    }
                    
                    builder.AllowAnyMethod()
                           .AllowAnyHeader();
                    
                    if (gatewayOptions.Security.AllowCredentials)
                    {
                        builder.AllowCredentials();
                    }
                });
            });
        }
        
        // Add Rate Limiting (per-route rate limiting is handled by PerRouteRateLimitingMiddleware)
        // No global rate limiter configured - each route has its own policy
        
        // Add Distributed Cache
        if (gatewayOptions.Cache.Enabled)
        {
            services.AddDistributedCaching(configuration);
        }
        
        // Register gateway services
        services.AddSingleton<IServiceDiscovery, KubernetesServiceDiscovery>();
        services.AddScoped<IRequestAggregator, RequestAggregator>();
        services.AddScoped<IRequestTransformer, JsonTransformer>();
        services.AddScoped<IResponseTransformer, JsonTransformer>();
        
        // Register GatewayMetrics as singleton
        services.AddSingleton<GatewayMetrics>();
        
        // Add Health Checks
        services.AddHealthChecks()
            .AddCheck<GatewayHealthCheck>("gateway")
            .AddCheck<DownstreamHealthCheck>("downstream");
        
        // Add OpenTelemetry
        if (gatewayOptions.Observability.EnableTracing || gatewayOptions.Observability.EnableMetrics)
        {
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService("ApiGateway");
            
            if (gatewayOptions.Observability.EnableTracing)
            {
                services.AddOpenTelemetry()
                    .WithTracing(builder =>
                    {
                        builder.SetResourceBuilder(resourceBuilder)
                            .AddAspNetCoreInstrumentation(options =>
                            {
                                options.RecordException = true;
                            })
                            .AddHttpClientInstrumentation()
                            .AddSource(GatewayActivitySource.SourceName) // Add custom ActivitySource
                            .AddConsoleExporter();
                    });
            }
            
            if (gatewayOptions.Observability.EnableMetrics)
            {
                services.AddOpenTelemetry()
                    .WithMetrics(builder =>
                    {
                        builder.SetResourceBuilder(resourceBuilder)
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddMeter(GatewayMetrics.MeterName) // Add custom Meter
                            .AddPrometheusExporter(); // Add Prometheus exporter
                    });
            }
        }
        
        // Add Exception Handler
        services.AddExceptionHandler<Middleware.GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        // Add Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "API Gateway",
                Version = "v1",
                Description = "API Gateway/BFF for BuildingBlocks microservices"
            });
        });
        
        return services;
    }
}
