using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace ThreadDistributor
{
	public class Distributor
	{
		public Distributor (Func<int, List<object>> getMoreWork, Action<object> workerAction, int threadCount, TimeSpan checkInterval)
		{
			GetMoreWork = getMoreWork;
			_timer = new Timer (DispatchThreads);
			_timerInterval = checkInterval;

			_availableThreads = new List<WorkerThread>(threadCount);
			for (int i=0; i<threadCount; i++) 
			{
				_availableThreads[i] = new WorkerThread(workerAction);
				_availableThreads[i].WorkerException += HandleWorkerException;
			}
		}

		public void StartDistribution()
		{
			_timer.Change (TimeSpan.FromSeconds(0), _timerInterval);
		}

		public void StopDistribution()
		{
			_timer.Change (Timeout.Infinite, Timeout.Infinite);
			_stopping = true;
		}


		public Func<int,List<object>> GetMoreWork { get; set; }

		private Timer _timer;
		private List<WorkerThread> _availableThreads;
		private TimeSpan _timerInterval;
		private bool _stopping;

		private void DispatchThreads(object evt)
		{
			((AutoResetEvent)evt).WaitOne();

			if(_stopping)
			{
				return;
			}

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

		public event EventHandler WorkItemsCleared;

		private void OnWorkItemsCleared()
		{
			if(WorkItemsCleared != null)
			{
				WorkItemsCleared(this, EventArgs.Empty);
			}
		}

		public event EventHandler<System.UnhandledExceptionEventArgs> ExceptionOccurred;

		private void OnExceptionOccurred(Exception ee)
		{
			if(ExceptionOccurred != null)
			{
				ExceptionOccurred (this, new UnhandledExceptionEventArgs(ee,false));
			}
		}

		private void HandleWorkerException(object sender, UnhandledExceptionEventArgs e)
		{
			if(ExceptionOccurred != null)
			{
				ExceptionOccurred (sender, e);
			}
		}

	}
}

