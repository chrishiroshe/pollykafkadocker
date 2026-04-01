using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prometheus;
using System.Diagnostics.Metrics;

namespace OrderSystem.Infrastructure.Observability
{
    public static class MetricsRegistry
    {
        public static readonly Prometheus.Counter OutboxProcessed =
            Metrics.CreateCounter("outbox_processed_total", "Total processed messages");

        public static readonly Prometheus.Counter OutboxFailed =
            Metrics.CreateCounter("outbox_failed_total", "Total failed messages");

        public static readonly Counter OutboxDeadLetter =
            Metrics.CreateCounter("outbox_deadletter_total", "Total dead letter messages");

    }
}
