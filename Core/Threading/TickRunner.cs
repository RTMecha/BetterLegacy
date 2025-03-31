using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Threading
{
    /// <summary>
    /// Class for running actions on a different thread.
    /// </summary>
    public class TickRunner : Exists, IDisposable
    {
        public TickRunner() { }

        public TickRunner(bool selfRun, HandleException handleException = HandleException.Continue, bool logException = false)
        {
            this.selfRun = selfRun;
            this.handleException = handleException;
            this.logException = logException;

            if (selfRun)
            {
                thread = new Thread(Run);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        /// <summary>
        /// Tick queue to run.
        /// </summary>
        public Queue<Action> tickQueue = new Queue<Action>();

        /// <summary>
        /// True if the runner is busy, otherwise false.
        /// </summary>
        public bool IsBusy { get; private set; }

        /// <summary>
        /// If the runner is runs itself on a different thread.
        /// </summary>
        public bool selfRun;

        /// <summary>
        /// How exceptions should be handled per-tick.
        /// </summary>
        public HandleException handleException = HandleException.Continue;

        /// <summary>
        /// If exceptions should be written to the console.
        /// </summary>
        public bool logException;

        readonly Thread thread;

        bool isRunning = true;

        void Run()
        {
            while (isRunning)
            {
                IsBusy = true;
                OnTick();
                IsBusy = false;
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Runs per-tick.
        /// </summary>
        public void OnTick()
        {
            while (tickQueue.Count > 0)
            {
                try
                {
                    tickQueue.Dequeue()?.Invoke();
                }
                catch (Exception ex)
                {
                    if (logException)
                        Console.WriteLine(ex);

                    if (handleException == HandleException.Break)
                        break;
                    if (handleException == HandleException.Continue)
                        continue;
                    if (handleException == HandleException.Throw)
                        throw;
                }
            }
        }

        public void Dispose()
        {
            tickQueue.Clear();
            isRunning = false;

            if (selfRun)
                thread.Join();
        }
    }
}
