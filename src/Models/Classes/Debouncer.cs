using System;
using System.Threading;

namespace TmCGPTD
{
    public class Debouncer
    {
        private readonly TimeSpan _debounceTime;
        private DateTime _lastInvokeTime;
        private System.Timers.Timer? _timer;

        public Debouncer(TimeSpan debounceTime)
        {
            _debounceTime = debounceTime;
        }

        public void Debounce(Action action)
        {
            _timer?.Stop();
            _timer?.Dispose();

            var elapsedSinceLastInvoke = DateTime.Now - _lastInvokeTime;
            var delay = elapsedSinceLastInvoke > _debounceTime ? 1 : (_debounceTime - elapsedSinceLastInvoke).TotalMilliseconds;

            _timer = new System.Timers.Timer(delay);
            _timer.Elapsed += (s, e) => action();
            _timer.AutoReset = false;
            _timer.Start();

            _lastInvokeTime = DateTime.Now;
        }
    }
}
