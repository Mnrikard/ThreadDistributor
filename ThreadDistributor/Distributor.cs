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
			_timer = new Timer(WaitToAssignWork, _dispatchResetEvent, Timeout.Infinite, Timeout.Infinite);
			_timerInterval = checkInterval;

			_availableThreads = new List<WorkerThread>(threadCount);
			for (int i=0; i<threadCount; i++) 
			{
				WorkerThread wt = new WorkerThread(workerAction);
				wt.WorkerException += WorkerEncounteredException;
				wt.WorkItemComplete += ImmediatelyWorkNextItem;

				_availableThreads.Add(wt);
			}
		}
		
		/// <summary>
		/// Starts the Distributor.
		/// </summary>
		public void StartDistribution()
		{
			_stopping = false;
			_timer.Change(TimeSpan.FromSeconds(0), _timerInterval);
		}

		/// <summary>
		/// Stops the Distributor.
		/// </summary>
		public void StopDistribution()
		{
			_stopping = true;
			_timer.Dispose();
		}

		/// <summary>
		/// Pauses the Distributor.
		/// </summary>
		public void PauseDistribution()
		{
			_stopping = true;
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
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

		internal void WaitToAssignWork(object resetEvent)
		{
			if(_stopping)
			{
				return;
			}
			((AutoResetEvent)resetEvent).WaitOne();

			try
			{
				AssignWorkToAvailableThreads();
			}
			catch(Exception ee) 
			{
				FireExceptionOccurredEvent(ee);
			}
			finally 
			{
				((AutoResetEvent)resetEvent).Set();
			}
		}


		private void AssignWorkToAvailableThreads()
		{
			List<WorkerThread> availableWorkers = _availableThreads.Where(t => !t.Busy).ToList();

			int i=0;
			foreach(object workItem in AttemptToGetNWorkItems(availableWorkers.Count))
			{
				WorkerThread worker = availableWorkers[i++];
				worker.CompleteWorkItemTask(workItem);
			}
		}

		private List<object> AttemptToGetNWorkItems(int amountOfWorkItems)
		{
			List<object> workItems = GetMoreWork(amountOfWorkItems);			
			if(workItems.Count == 0)
			{
				FireWorkItemsClearedEvent();
			}
			return workItems;
		}

		/// <summary>
		/// Occurs when there are available threads, but no more work.
		/// </summary>
		public event EventHandler WorkItemsCleared;

		/// <summary>
		/// Raises the work items cleared event.
		/// </summary>
		private void FireWorkItemsClearedEvent()
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

		private void FireExceptionOccurredEvent(Exception ee)
		{
			if(ExceptionOccurred != null)
			{
				ExceptionOccurred (this, new UnhandledExceptionEventArgs(ee,false));
			}
		}

		private void WorkerEncounteredException(object workerThread, UnhandledExceptionEventArgs exceptionArgs)
		{
			if(ExceptionOccurred != null)
			{
				ExceptionOccurred (workerThread, exceptionArgs);
			}
		}

		private void ImmediatelyWorkNextItem(object sender, EventArgs e)
		{
			WaitToAssignWork(_dispatchResetEvent);		
		}

	}
}

