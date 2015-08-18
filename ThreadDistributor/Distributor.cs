using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace ThreadDistributor
{
	/// <summary>
	/// Obtains work items and distributes them to available threads.
	/// </summary>
	public class Distributor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ThreadDistributor.Distributor"/> class.
		/// </summary>
		/// <param name="getMoreWork">Get more work function 
		/// <para>Takes an integer max number of work items to retrieve and</para>
		/// <para>returns a list of objects representing the work items.</para>
		/// </param>
		/// <param name="workerAction">Action method which takes a single object work item and acts upon it.</param>
		/// <param name="threadCount">The desired number of worker threads</param>
		/// <param name="checkInterval">A TimeSpan describing how often to check for work.
		/// <para>On successfully completing a WorkerAction, GetMoreWork is called again before the timer elapses.</para>
		/// </param>
		public Distributor (Func<int, List<object>> getMoreWork, Action<object> workerAction, int threadCount, TimeSpan checkInterval)
		{
			GetMoreWork = getMoreWork;
			_timer = new Timer(DispatchThreads, _dispatchResetEvent, Timeout.Infinite, Timeout.Infinite);
			_timerInterval = checkInterval;

			_availableThreads = new List<WorkerThread>(threadCount);
			for (int i=0; i<threadCount; i++) 
			{
				WorkerThread wt = new WorkerThread(workerAction);
				wt.WorkerException += HandleWorkerException;
				wt.WorkItemComplete += HandleWorkerItemComplete;

				_availableThreads.Add(wt);
			}
		}

		/// <summary>
		/// Starts the distribution.
		/// </summary>
		public void StartDistribution()
		{
			_timer.Change(TimeSpan.FromSeconds(0), _timerInterval);
		}

		/// <summary>
		/// Stops the distribution.
		/// </summary>
		public void StopDistribution()
		{
			_timer.Dispose();
			_stopping = true;
		}

		/// <summary>
		/// Gets or sets the function which will get more work for the <see cref="WorkerThread"/>s.
		/// </summary>
		/// <remarks>
		/// <para>Takes an integer max number of work items to retrieve and</para>
		/// <para>returns a list of objects representing the work items.</para>
		/// </remarks>
		public Func<int,List<object>> GetMoreWork { get; set; }

		private Timer _timer;
		private List<WorkerThread> _availableThreads;
		private TimeSpan _timerInterval;
		internal bool _stopping;
		private AutoResetEvent _dispatchResetEvent = new AutoResetEvent(true);

		/// <summary>
		/// Dispatchs the threads, managing the AutoResetEvent.
		/// </summary>
		/// <param name="evt">The AutoResetEvent.</param>
		internal void DispatchThreads(object evt)
		{
			if(_stopping)
			{
				return;
			}

			((AutoResetEvent)evt).WaitOne();

			try
			{
				DispatchThreads();
			}
			catch(Exception ee) 
			{
				OnExceptionOccurred (ee);
			}
			finally 
			{
				((AutoResetEvent)evt).Set();
			}
		}

		/// <summary>
		/// Dispatchs the threads.
		/// </summary>
		private void DispatchThreads()
		{
			List<WorkerThread> availableWorkers = _availableThreads.Where (t => !t.Busy).ToList ();

			if (availableWorkers.Any ()) 
			{
				List<object> workItems = GetMoreWork(availableWorkers.Count);

				if(workItems.Count == 0)
				{
					OnWorkItemsCleared();
				}

				for (int i=0; i<workItems.Count; i++) 
				{
					availableWorkers[i].Busy = true;
					ThreadPool.QueueUserWorkItem(availableWorkers[i].CompleteWork, workItems[i]);
				}
			}
		}

		/// <summary>
		/// Occurs when there are available threads, but no more work.
		/// </summary>
		public event EventHandler WorkItemsCleared;

		/// <summary>
		/// Raises the work items cleared event.
		/// </summary>
		private void OnWorkItemsCleared()
		{
			if(WorkItemsCleared != null)
			{
				WorkItemsCleared(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs when an the <see cref="Distributor"/> or the <see cref="WorkerThread"/> encounters and error.
		/// </summary>
		public event EventHandler<System.UnhandledExceptionEventArgs> ExceptionOccurred;

		/// <summary>
		/// Raises the exception occurred event.
		/// </summary>
		/// <param name="ee">Ee.</param>
		private void OnExceptionOccurred(Exception ee)
		{
			if(ExceptionOccurred != null)
			{
				ExceptionOccurred (this, new UnhandledExceptionEventArgs(ee,false));
			}
		}

		/// <summary>
		/// Handles the worker exception.
		/// </summary>
		/// <param name="sender">The Sender.</param>
		/// <param name="e">The exception</param>
		private void HandleWorkerException(object sender, UnhandledExceptionEventArgs e)
		{
			if(ExceptionOccurred != null)
			{
				ExceptionOccurred (sender, e);
			}
		}

		/// <summary>
		/// Handles the worker item completing.
		/// </summary>
		/// <param name="sender">The Sender.</param>
		/// <param name="e">Empty</param>
		private void HandleWorkerItemComplete(object sender, EventArgs e)
		{
			DispatchThreads(_dispatchResetEvent);
		}

	}
}

