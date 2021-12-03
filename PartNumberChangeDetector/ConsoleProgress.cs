using System;

namespace PartNumberChangeDetector
{
    public class ConsoleProgress<T> : IProgress<T>
    {
        readonly Action<T> handler;

        public ConsoleProgress(Action<T> handler)
        {
            this.handler = handler;
        }

        public void Report(T value) => handler?.Invoke(value);
    }
}