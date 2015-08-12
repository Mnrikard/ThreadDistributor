using System;

namespace ThreadDistributor
{
	public class WorkerThread
	{
		public WorkerThread (Action<object> workerAction)
		{
			WorkerAction = workerAction;
		}

		public bool Busy { get; set; }

		public Action<object> WorkerAction { get; set; }

		public void CompleteWork(object workItem)
		{
			try
			{
				WorkerAction(workItem);
			}
			catch(Exception ee)
			{
				OnWorkerException(ee);
			}
			finally
			{
				Busy = false;
			}
		}

		internal event EventHandler<UnhandledExceptionEventArgs> WorkerException;

		private void OnWorkerException(Exception ee)
		{
			if(WorkerException != null)
			{
				WorkerException(this, new UnhandledExceptionEventArgs (ee, false));
			}
		}
	}
}

