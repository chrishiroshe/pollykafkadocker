using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Application.Abstractions
{

    public interface IOutboxWriter
    {
        Task AddAsync(string type, string payload, string? traceId, CancellationToken ct);
    }
}
