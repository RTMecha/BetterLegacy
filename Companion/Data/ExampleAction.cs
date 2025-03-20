using System;

using LSFunctions;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents an action Example can do.
    /// </summary>
    public class ExampleAction : Exists
    {
        public ExampleAction() { uniqueID = LSText.randomNumString(16); }

        public ExampleAction(string key, Action func) : this()
        {
            this.key = key;
            this.func = func;
        }
        
        public ExampleAction(string key, Func<bool> canDo, Action func) : this(key, func)
        {
            this.canDo = canDo;
        }

        public ExampleAction(string key, Func<bool> canDo, bool interruptible, Action func) : this(key, canDo, func)
        {
            this.interruptible = interruptible;
        }
        
        public ExampleAction(string key, Func<bool> canDo, Func<bool> interruptCheck, bool interruptible, Action func) : this(key, canDo, interruptible, func)
        {
            this.interruptCheck = interruptCheck;
        }
        
        public ExampleAction(string key, Func<bool> canDo, Func<bool> interruptCheck, bool interruptible, Action func, Action stopFunc) : this(key, canDo, interruptCheck, interruptible, func)
        {
            this.stopFunc = stopFunc;
        }
        
        public ExampleAction(string key, Func<bool> canDo, Func<bool> interruptCheck, bool interruptible, Action func, Action stopFunc, bool setAsCurrent) : this(key, canDo, interruptCheck, interruptible, func, stopFunc)
        {
            this.setAsCurrent = setAsCurrent;
        }

        public string uniqueID;

        /// <summary>
        /// Key of the action.
        /// </summary>
        public string key;

        /// <summary>
        /// If the function can run.
        /// </summary>
        public Func<bool> canDo;

        /// <summary>
        /// The check that interrupts this action.
        /// </summary>
        public Func<bool> interruptCheck;

        /// <summary>
        /// If the action is interruptible.
        /// </summary>
        public bool interruptible;

        /// <summary>
        /// The function to run.
        /// </summary>
        public Action func;

        /// <summary>
        /// The function to run when interrupted.
        /// </summary>
        public Action stopFunc;

        /// <summary>
        /// If the action is running.
        /// </summary>
        public bool running;

        /// <summary>
        /// If the action should be set as the current.
        /// </summary>
        public bool setAsCurrent = true;

        /// <summary>
        /// Checks if the action can run the function and runs it if it can.
        /// </summary>
        /// <returns>Returns true if the function can run, otherwise returns false.</returns>
        public bool Run()
        {
            var can = canDo?.Invoke() ?? false;
            if (can)
                Start();
            return can;
        }

        /// <summary>
        /// Checks if the action can be interrupted.
        /// </summary>
        /// <returns>Returns true if the action was interrupted, otherwise returns false.</returns>
        public bool Interrupt()
        {
            var interrupt = interruptible && interruptCheck?.Invoke() == true;
            if (interrupt)
                Stop();
            return interrupt;
        }

        /// <summary>
        /// Starts the action.
        /// </summary>
        public void Start()
        {
            func?.Invoke();
            running = true;
        }

        /// <summary>
        /// Stops the action.
        /// </summary>
        public void Stop()
        {
            stopFunc?.Invoke();
            running = false;
        }
    }
}
