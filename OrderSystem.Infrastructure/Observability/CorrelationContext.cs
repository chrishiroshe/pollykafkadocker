using System.Diagnostics;

namespace OrderSystem.Infrastructure.Observability;

public static class CorrelationContext
{
    public static string GetOrCreate()
        => Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
}