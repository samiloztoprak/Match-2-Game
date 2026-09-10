using System;

namespace Match2.Utils
{
    /// <summary>
    /// Invokes <paramref name="onAllComplete"/> once exactly as many
    /// completions have been reported as were expected. Used so a batch of
    /// per-block animations playing in parallel can signal a single "batch
    /// finished" callback without each caller re-implementing a counter.
    /// </summary>
    public class CallbackBarrier
    {
        private readonly Action onAllComplete;
        private int remaining;

        public CallbackBarrier(int expectedCompletions, Action onAllComplete)
        {
            this.onAllComplete = onAllComplete;
            remaining = expectedCompletions;

            if (remaining <= 0)
                onAllComplete?.Invoke();
        }

        public void ReportComplete()
        {
            remaining--;
            if (remaining == 0)
                onAllComplete?.Invoke();
        }
    }
}
