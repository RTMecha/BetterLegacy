using System.Threading;

namespace BetterLegacy.Core.Threading
{
	public static class SyncContextUtil
	{
		public static void Init()
		{
			UnitySynchronizationContext = SynchronizationContext.Current;
			UnityThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public static int UnityThreadId { get; private set; }

		public static SynchronizationContext UnitySynchronizationContext { get; private set; }
	}
}
