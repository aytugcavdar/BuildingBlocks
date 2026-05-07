using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Logging.Enrichers;

public class CorrelationIdEnricher : ILogEventEnricher
{
    private const string CorrelationIdPropertyName = "CorrelationId";
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return;
        }

        var correlationId = GetOrCreateCorrelationId(httpContext);

        var correlationIdProperty = propertyFactory.CreateProperty(
            CorrelationIdPropertyName,
            correlationId);

        logEvent.AddPropertyIfAbsent(correlationIdProperty);
    }

    private string GetOrCreateCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
        {
            return correlationId.ToString();
        }

        // Eğer daha önce oluşturulup Item'lara eklendiyse oradan al
        if (httpContext.Items.TryGetValue(CorrelationIdHeaderName, out var existingCorrelationId))
        {
            return existingCorrelationId?.ToString() ?? Guid.NewGuid().ToString();
        }

        var newCorrelationId = Guid.NewGuid().ToString();
        httpContext.Items[CorrelationIdHeaderName] = newCorrelationId;

        // Response header'a da ekleyelim ki client alabilsin
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.Headers[CorrelationIdHeaderName] = newCorrelationId;
        }

        return newCorrelationId;
    }
}
