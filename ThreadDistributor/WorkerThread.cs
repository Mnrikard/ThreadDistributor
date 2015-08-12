using System;

namespace ThreadDistributor
{
	/// <summary>
	/// Worker thread.
	/// </summary>
	public class WorkerThread
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ThreadDistributor.WorkerThread"/> class.
		/// </summary>
		/// <param name="workerAction">Worker action to be performed.</param>
		public WorkerThread (Action<object> workerAction)
		{
			WorkerAction = workerAction;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ThreadDistributor.WorkerThread"/> is busy.
		/// </summary>
		/// <value><c>true</c> if busy; otherwise, <c>false</c>.</value>
		public bool Busy { get; set; }

		/// <summary>
		/// Gets or sets the worker action which will act upon an object WorkItem returned from <see cref="Distributor.GetMoreWork"/>.
		/// </summary>
		public Action<object> WorkerAction { get; set; }

		/// <summary>
		/// Completes the work assigned to it.
		/// </summary>
		/// <param name="workItem">Work item to be acted upon.</param>
		internal void CompleteWork(object workItem)
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
				OnWorkItemComplete();
			}
		}

		/// <summary>
		/// Occurs when a WorkerThread throws an exception.
		/// </summary>
		internal event EventHandler<UnhandledExceptionEventArgs> WorkerException;

		/// <summary>
		/// Raises the worker exception event.
		/// </summary>
		/// <param name="ee">Ee.</param>
		private void OnWorkerException(Exception ee)
		{
			if(WorkerException != null)
			{
				WorkerException(this, new UnhandledExceptionEventArgs (ee, false));
			}
		}

		/// <summary>
		/// Occurs when work item completes.
		/// </summary>
		internal event EventHandler WorkItemComplete;
		/// <summary>
		/// Raises the work item complete event.
		/// </summary>
		private void OnWorkItemComplete()
		{
			if(WorkItemComplete != null)
			{
				WorkItemComplete(this, EventArgs.Empty);
			}
		}
	}
}

