using System;
using System.Collections;

using UnityEngine;

using CielaSpike;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Awaiters helper class based on https://github.com/seedov/AsyncAwaitUtil
    /// </summary>
    public static class CoroutineHelper
    {
        #region Main

        /// <summary>
        /// Starts a coroutine from anywhere.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        /// <returns>Returns a generated Coroutine.</returns>
        public static Coroutine StartCoroutine(IEnumerator routine) => LegacyPlugin.inst.StartCoroutine(routine);

        /// <summary>
        /// Starts a coroutine from anywhere asynchronously.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        /// <returns>Returns a generated Coroutine.</returns>
        public static Coroutine StartCoroutineAsync(IEnumerator routine) => LegacyPlugin.inst.StartCoroutineAsync(routine);

        /// <summary>
        /// Stops a coroutine from running.
        /// </summary>
        /// <param name="coroutine">Generated coroutine.</param>
        public static void StopCoroutine(Coroutine coroutine) => LegacyPlugin.inst.StopCoroutine(coroutine);

        #endregion

        #region Awaiters

        /// <summary>
        /// Waits for fixed update.
        /// </summary>
        public static WaitForFixedUpdate FixedUpdate => waitForFixedUpdate;

        /// <summary>
        /// Waits for end of frame.
        /// </summary>
        public static WaitForEndOfFrame EndOfFrame => waitForEndOfFrame;

        /// <summary>
        /// Waits for a set amount of time.
        /// </summary>
        /// <param name="seconds">Seconds to wait.</param>
        /// <returns>Returns <see cref="WaitForSeconds"/> to yield by.</returns>
        public static WaitForSeconds Seconds(float seconds) => new WaitForSeconds(seconds);

        /// <summary>
        /// Waits for a set amount of real-time.
        /// </summary>
        /// <param name="seconds">Real-time seconds to wait.</param>
        /// <returns>Returns <see cref="WaitForSecondsRealtime"/> to yield by.</returns>
        public static WaitForSecondsRealtime SecondsRealtime(float seconds) => new WaitForSecondsRealtime(seconds);

        /// <summary>
        /// Waits until the predicate is true.
        /// </summary>
        /// <param name="predicate">Predicate to check.</param>
        /// <returns>Returns <see cref="WaitUntil"/> to yield by.</returns>
        public static WaitUntil Until(Func<bool> predicate) => new WaitUntil(predicate);

        /// <summary>
        /// Waits while the predicate is true.
        /// </summary>
        /// <param name="predicate">Predicate to check.</param>
        /// <returns>Returns <see cref="WaitWhile"/> to yield by.</returns>
        public static WaitWhile While(Func<bool> predicate) => new WaitWhile(predicate);

        /// <summary>
        /// Gets a specific <see cref="YieldInstruction"/> from a <see cref="YieldType"/>.
        /// </summary>
        /// <param name="yieldType">YieldType to get an instruction from.</param>
        /// <param name="delay">Delay reference for <see cref="WaitForSeconds"/>.</param>
        /// <returns>Returns a <see cref="YieldInstruction"/>.</returns>
        public static YieldInstruction GetYieldInstruction(YieldType yieldType, ref float delay)
        {
            switch (yieldType)
            {
                case YieldType.Delay: delay += 0.0001f; return Seconds(delay);
                case YieldType.EndOfFrame: return EndOfFrame;
                case YieldType.FixedUpdate: return FixedUpdate;
            }
            return null;
        }

        #region Internal

        static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

        static readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        #endregion

        #endregion

        #region Functions

        /// <summary>
        /// Performs an action after a set amount of time.
        /// </summary>
        /// <param name="t">Seconds to wait.</param>
        /// <param name="action">Action to run after waiting.</param>
        public static void PerformActionAfterSeconds(float t, Action action) => StartCoroutine(IPerformActionAfterSeconds(t, action));

        /// <summary>
        /// Performs an action after a set amount of time.
        /// </summary>
        /// <param name="t">Seconds to wait.</param>
        /// <param name="action">Action to run after waiting.</param>
        /// <returns>Returns a routine.</returns>
        public static IEnumerator IPerformActionAfterSeconds(float t, Action action)
        {
            yield return Seconds(t);
            action?.Invoke();
        }

        /// <summary>
        /// Does an action in a coroutine.
        /// </summary>
        /// <param name="action">Action to run.</param>
        /// <returns>Returns a routine.</returns>
        public static IEnumerator DoAction(Action action)
        {
            action?.Invoke();
            yield break;
        }

        /// <summary>
        /// Empty coroutine.
        /// </summary>
        /// <returns>Returns an empty routine.</returns>
        public static IEnumerator IEmpty() { yield break; }

        public static string DefaultYieldInstructionDescription => "Some options will run faster but freeze the game, while others run slower but allow you to see them update in real time.";

        #endregion
    }
}
