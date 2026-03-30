using System.Reflection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SupportOS.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        var properties = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.Name.Equals("Password", StringComparison.OrdinalIgnoreCase))
            .Select(p => $"{p.Name}={p.GetValue(request)}");

        _logger.LogInformation("Handling {RequestName} [{Properties}]", requestName, string.Join(", ", properties));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        _logger.LogInformation("Handled {RequestName} → {ResponseType} in {ElapsedMs}ms",
            requestName, typeof(TResponse).Name, sw.ElapsedMilliseconds);

        return response;
    }
}
