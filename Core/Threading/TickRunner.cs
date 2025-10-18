using System;
using System.Collections.Generic;
using System.Threading;

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
        /// Single tick function to run.
        /// </summary>
        public Action onTick;

        /// <summary>
        /// True if the runner is busy, otherwise false.
        /// </summary>
        public bool IsBusy { get; private set; }

        /// <summary>
        /// If the runner is runs itself on a different thread.
        /// </summary>
        public bool selfRun;

        /// <summary>
        /// If <see cref="onTick"/> should clear when it's finished running.
        /// </summary>
        public bool clearTick;

        /// <summary>
        /// How exceptions should be handled per-tick.
        /// </summary>
        public HandleException handleException = HandleException.Continue;

        /// <summary>
        /// If exceptions should be written to the console.
        /// </summary>
        public bool logException;

        Queue<Action> tickQueue = new Queue<Action>();

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

            try
            {
                onTick?.Invoke();
            }
            catch (Exception ex)
            {
                if (logException)
                    Console.WriteLine(ex);

                if (handleException == HandleException.Throw)
                {
                    if (clearTick)
                        onTick = null;
                    throw;
                }
            }
            if (clearTick)
                onTick = null;
        }

        /// <summary>
        /// Queues an action.
        /// </summary>
        /// <param name="action">Action to queue.</param>
        public void Enqueue(Action action) => tickQueue.Enqueue(action);

        /// <summary>
        /// Adds an action to the onTick function.
        /// </summary>
        /// <param name="action">Action to add.</param>
        public void AddAction(Action action) => onTick += action;

        public void Dispose()
        {
            tickQueue.Clear();
            onTick = null;
            isRunning = false;

            if (selfRun)
                thread.Join();
        }

        public static TickRunner operator +(TickRunner a, Action b)
        {
            a.Enqueue(b);
            return a;
        }
    }
}
