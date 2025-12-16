using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

using UnityEngine;

namespace BetterLegacy.Core.Threading
{
	// from https://github.com/seedov/AsyncAwaitUtil
	public static class IEnumeratorAwaitExtensions
	{
		public static SimpleCoroutineAwaiter GetAwaiter(this WaitForSeconds instruction) => GetAwaiterReturnVoid(instruction);

		public static SimpleCoroutineAwaiter GetAwaiter(this WaitForEndOfFrame instruction) => GetAwaiterReturnVoid(instruction);

		public static SimpleCoroutineAwaiter GetAwaiter(this WaitForFixedUpdate instruction) => GetAwaiterReturnVoid(instruction);

		public static SimpleCoroutineAwaiter GetAwaiter(this WaitForSecondsRealtime instruction) => GetAwaiterReturnVoid(instruction);

		public static SimpleCoroutineAwaiter GetAwaiter(this WaitUntil instruction) => GetAwaiterReturnVoid(instruction);

		public static SimpleCoroutineAwaiter GetAwaiter(this WaitWhile instruction) => GetAwaiterReturnVoid(instruction);

		public static SimpleCoroutineAwaiter<AsyncOperation> GetAwaiter(this AsyncOperation instruction) => GetAwaiterReturnSelf(instruction);

		public static SimpleCoroutineAwaiter<UnityObject> GetAwaiter(this ResourceRequest instruction)
		{
			var awaiter = new SimpleCoroutineAwaiter<UnityObject>();
			RunOnUnityScheduler(InstructionWrappers.ResourceRequest(awaiter, instruction).Start);
			return awaiter;
		}

		public static SimpleCoroutineAwaiter<WWW> GetAwaiter(this WWW instruction) => GetAwaiterReturnSelf(instruction);

		public static SimpleCoroutineAwaiter<AssetBundle> GetAwaiter(this AssetBundleCreateRequest instruction)
		{
			var awaiter = new SimpleCoroutineAwaiter<AssetBundle>();
			RunOnUnityScheduler(InstructionWrappers.AssetBundleCreateRequest(awaiter, instruction).Start);
			return awaiter;
		}

		public static SimpleCoroutineAwaiter<UnityObject> GetAwaiter(this AssetBundleRequest instruction)
		{
			var awaiter = new SimpleCoroutineAwaiter<UnityObject>();
			RunOnUnityScheduler(InstructionWrappers.AssetBundleRequest(awaiter, instruction).Start);
			return awaiter;
		}

		public static SimpleCoroutineAwaiter<T> GetAwaiter<T>(this IEnumerator<T> coroutine)
		{
			var awaiter = new SimpleCoroutineAwaiter<T>();
			RunOnUnityScheduler(new CoroutineWrapper<T>(coroutine, awaiter).Run().Start);
			return awaiter;
		}

		public static SimpleCoroutineAwaiter<object> GetAwaiter(this IEnumerator coroutine)
		{
			var awaiter = new SimpleCoroutineAwaiter<object>();
			RunOnUnityScheduler(new CoroutineWrapper<object>(coroutine, awaiter).Run().Start);
			return awaiter;
		}

		static SimpleCoroutineAwaiter GetAwaiterReturnVoid(object instruction)
		{
			var awaiter = new SimpleCoroutineAwaiter();
			RunOnUnityScheduler(InstructionWrappers.ReturnVoid(awaiter, instruction).Start);
			return awaiter;
		}

		static SimpleCoroutineAwaiter<T> GetAwaiterReturnSelf<T>(T instruction)
		{
			var awaiter = new SimpleCoroutineAwaiter<T>();
			RunOnUnityScheduler(InstructionWrappers.ReturnSelf(awaiter, instruction).Start);
			return awaiter;
		}

		static void RunOnUnityScheduler(Action action)
		{
			if (SynchronizationContext.Current == SyncContextUtil.UnitySynchronizationContext)
			{
				action();
				return;
			}
			SyncContextUtil.UnitySynchronizationContext.Post(_ => { action(); }, null);
		}

		static void Assert(bool condition)
		{
			if (!condition)
				throw new Exception("Assert hit in UnityAsyncUtil package!");
		}

		// Token: 0x020001A2 RID: 418
		public class SimpleCoroutineAwaiter<T> : INotifyCompletion
		{
			public bool IsCompleted => _isDone;

			public T GetResult()
			{
				Assert(_isDone);
				if (_exception != null)
					ExceptionDispatchInfo.Capture(_exception).Throw();
				return _result;
			}

			public void Complete(T result, Exception e)
			{
				Assert(!_isDone);
				_isDone = true;
				_exception = e;
				_result = result;
				if (_continuation != null)
					RunOnUnityScheduler(_continuation);
			}

			void INotifyCompletion.OnCompleted(Action continuation)
			{
				Assert(_continuation == null);
				Assert(!_isDone);
				_continuation = continuation;
			}

			bool _isDone;

			Exception _exception;

			Action _continuation;

			T _result;
		}

		public class SimpleCoroutineAwaiter : INotifyCompletion
		{
			public bool IsCompleted => _isDone;

			public void GetResult()
			{
				Assert(_isDone);
				if (_exception != null)
				{
					ExceptionDispatchInfo.Capture(_exception).Throw();
				}
			}

			public void Complete(Exception e)
			{
				Assert(!_isDone);
				_isDone = true;
				_exception = e;
				if (_continuation != null)
				{
					RunOnUnityScheduler(_continuation);
				}
			}

			void INotifyCompletion.OnCompleted(Action continuation)
			{
				Assert(_continuation == null);
				Assert(!_isDone);
				_continuation = continuation;
			}

			bool _isDone;

			Exception _exception;

			Action _continuation;
		}

		class CoroutineWrapper<T>
		{
			public CoroutineWrapper(IEnumerator coroutine, SimpleCoroutineAwaiter<T> awaiter)
			{
				_processStack = new Stack<IEnumerator>();
				_processStack.Push(coroutine);
				_awaiter = awaiter;
			}

			public IEnumerator Run()
			{
				IEnumerator enumerator;
				for (; ; )
				{
					enumerator = _processStack.Peek();
					bool notMoveNext;

					try
					{
						notMoveNext = !enumerator.MoveNext();
					}
					catch (Exception ex)
					{
						var list = GenerateObjectTrace(_processStack);
						_awaiter.Complete(default, list.Any() ? new Exception(GenerateObjectTraceMessage(list), ex) : ex);
						yield break;
					}

					if (notMoveNext)
					{
						_processStack.Pop();
						if (_processStack.Count == 0)
							break;
					}

					if (enumerator.Current is IEnumerator enumerator1)
						_processStack.Push(enumerator1);
					else
						yield return enumerator.Current;
				}

				_awaiter.Complete((T)enumerator.Current, null);
				yield break;
			}

			string GenerateObjectTraceMessage(List<Type> objTrace)
			{
				var stringBuilder = new StringBuilder();
				foreach (var type in objTrace)
				{
					if (stringBuilder.Length != 0)
						stringBuilder.Append(" -> ");
					stringBuilder.Append(type.ToString());
				}
				stringBuilder.AppendLine();
				return "Unity Coroutine Object Trace: " + stringBuilder.ToString();
			}

			static List<Type> GenerateObjectTrace(IEnumerable<IEnumerator> enumerators)
			{
				var list = new List<Type>();
				foreach (var enumerator2 in enumerators)
				{
					var field = enumerator2.GetType().GetField("$this", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field != null)
					{
						object value = field.GetValue(enumerator2);
						if (value != null)
						{
							var type = value.GetType();
							if (!list.Any() || type != list.Last())
								list.Add(type);
						}
					}
				}
				list.Reverse();
				return list;
			}

			readonly SimpleCoroutineAwaiter<T> _awaiter;

			readonly Stack<IEnumerator> _processStack;
		}

		static class InstructionWrappers
		{
			public static IEnumerator ReturnVoid(SimpleCoroutineAwaiter awaiter, object instruction)
			{
				yield return instruction;
				awaiter.Complete(null);
				yield break;
			}

			public static IEnumerator AssetBundleCreateRequest(SimpleCoroutineAwaiter<AssetBundle> awaiter, AssetBundleCreateRequest instruction)
			{
				yield return instruction;
				awaiter.Complete(instruction.assetBundle, null);
				yield break;
			}

			public static IEnumerator ReturnSelf<T>(SimpleCoroutineAwaiter<T> awaiter, T instruction)
			{
				yield return instruction;
				awaiter.Complete(instruction, null);
				yield break;
			}

			public static IEnumerator AssetBundleRequest(SimpleCoroutineAwaiter<UnityObject> awaiter, AssetBundleRequest instruction)
			{
				yield return instruction;
				awaiter.Complete(instruction.asset, null);
				yield break;
			}

			public static IEnumerator ResourceRequest(SimpleCoroutineAwaiter<UnityObject> awaiter, ResourceRequest instruction)
			{
				yield return instruction;
				awaiter.Complete(instruction.asset, null);
				yield break;
			}
		}
	}
}
