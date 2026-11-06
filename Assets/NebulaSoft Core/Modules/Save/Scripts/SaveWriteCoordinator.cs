using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft
{
    // Owns write ordering only. Snapshot creation and Unity callbacks stay on the caller thread.
    internal sealed class SaveWriteCoordinator
    {
        private readonly object gate = new object();
        private Task pending = Task.CompletedTask;

        public void Execute(Action operation, bool background = false)
        {
            lock (gate)
            {
                if (background)
                {
                    pending = pending.ContinueWith(previous =>
                    {
                        // A failed request must not prevent later writes from running.
                        if (previous.IsFaulted) _ = previous.Exception;
                        operation();
                    }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                }
                else
                {
                    CompletePending();
                    operation();
                }
            }
        }

        public void Complete()
        {
            lock (gate) CompletePending();
        }

        private void CompletePending()
        {
            try { pending.GetAwaiter().GetResult(); }
            catch (Exception) { /* Each save request reports its own failure. */ }
        }
    }
}
