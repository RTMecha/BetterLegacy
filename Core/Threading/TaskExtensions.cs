using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Threading
{
	// based on https://github.com/seedov/AsyncAwaitUtil
	public static class TaskExtensions
	{
		public static IEnumerator AsIEnumerator(this Task task)
		{
			while (!task.IsCompleted)
				yield return null;
			if (task.IsFaulted)
				ExceptionDispatchInfo.Capture(task.Exception).Throw();
			yield break;
		}

		// TODO: if UniTask gets implemented, reimplement this
		//public static IEnumerator AsIEnumerator(this UniTask task)
		//{
		//	while (task.Status != UniTaskStatus.Succeeded)
		//		yield return null;
		//	yield break;
		//}

		public static IEnumerator AsIEnumerator(this ParallelLoopResult parallelLoopResult)
		{
			while (!parallelLoopResult.IsCompleted)
				yield return null;
			yield break;
		}
	}
}
