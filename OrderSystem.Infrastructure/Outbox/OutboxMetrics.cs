namespace Order.Api.Infraestruture
{
    using System.Threading;

    public class OutboxMetrics
    {
        private int _processed;
        private int _failed;
        private int _deadLettered;

        public int Processed => _processed;
        public int Failed => _failed;
        public int DeadLettered => _deadLettered;

        public void IncrementProcessed()
            => Interlocked.Increment(ref _processed);

        public void IncrementFailed()
            => Interlocked.Increment(ref _failed);

        public void IncrementDeadLettered()
            => Interlocked.Increment(ref _deadLettered);
    }
}
