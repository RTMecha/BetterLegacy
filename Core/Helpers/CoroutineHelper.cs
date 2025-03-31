using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using CielaSpike;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Awaiters helper class based on https://github.com/modesttree/Unity3dAsyncAwaitUtil
    /// </summary>
    public static class CoroutineHelper
    {
        public static WaitForFixedUpdate FixedUpdate => waitForFixedUpdate;

        public static WaitForEndOfFrame EndOfFrame => waitForEndOfFrame;

        public static WaitForSeconds Seconds(float seconds) => new WaitForSeconds(seconds);

        public static WaitForSecondsRealtime SecondsRealtime(float seconds) => new WaitForSecondsRealtime(seconds);

        public static WaitUntil Until(Func<bool> predicate) => new WaitUntil(predicate);

        public static WaitWhile While(Func<bool> predicate) => new WaitWhile(predicate);

        static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

        static readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

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

        public static void PerformActionAfterSeconds(float t, Action action) => StartCoroutine(IPerformActionAfterSeconds(t, action));

        public static IEnumerator IPerformActionAfterSeconds(float t, Action action)
        {
            yield return new WaitForSeconds(t);
            action?.Invoke();
        }

        public static void WaitUntil(Func<bool> func, Action action) => StartCoroutine(IWaitUntil(func, action));

        public static IEnumerator IWaitUntil(Func<bool> func, Action action)
        {
            yield return new WaitUntil(func);
            action?.Invoke();
        }

        public static IEnumerator DoAction(Action action)
        {
            action?.Invoke();
            yield break;
        }

        public static void ReturnToUnity(Action action) => StartCoroutine(IReturnToUnity(action));

        public static IEnumerator IReturnToUnity(Action action)
        {
            yield return Ninja.JumpToUnity;
            action?.Invoke();
        }

        public static void LogOnMainThread(string message) => ReturnToUnity(() => CoreHelper.Log(message));

        public static string DefaultYieldInstructionDescription => "Some options will run faster but freeze the game, while others run slower but allow you to see them update in real time.";

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
                case YieldType.Delay: delay += 0.0001f; return new WaitForSeconds(delay);
                case YieldType.EndOfFrame: return EndOfFrame;
                case YieldType.FixedUpdate: return FixedUpdate;
            }
            return null;
        }

    }
}
