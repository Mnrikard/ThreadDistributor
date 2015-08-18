using System;
using System.Diagnostics;
using NUnit.Framework;
using ThreadDistributor;

namespace UT.ThreadDistributor
{
	[TestFixture]
	public class WorkerThreadTest
	{
		[Test]
		public void WillItWorkerThread ()
		{
			WorkerThread wt = new WorkerThread(a=>Trace.WriteLine("yes, it will"));
			Assert.IsInstanceOf<WorkerThread>(wt);
		}

		[Test]
		public void CanItBeBusy()
		{
			WorkerThread wt = new WorkerThread(a=>Trace.WriteLine("yes, it can"));
			Assert.AreEqual(wt.Busy,false);
			wt.Busy = true;
			Assert.AreEqual(wt.Busy,true);
		}

		[Test]
		public void CanCompleteWork()
		{
			bool actualWorkDone = false;
			bool expectedWorkDone = true;

			WorkerThread wt = new WorkerThread(a=>Trace.WriteLine("yeah, I'm not doing this"));
			wt.WorkerAction = a => {actualWorkDone = (bool)a;};

			wt.CompleteWork(expectedWorkDone);
			Assert.AreEqual(expectedWorkDone,actualWorkDone);
		}

		[Test]
		public void CanEventOnError()
		{
			object o = null;
			bool expectedErrorCaught = true;
			bool actualErrorCaught = false;
			WorkerThread wt = new WorkerThread(a => {throw new Exception("this won't work");});
			wt.WorkerException += (sender, e) => actualErrorCaught = true;

			wt.CompleteWork(o);

			Assert.AreEqual(expectedErrorCaught,actualErrorCaught);
		}

		[Test]
		public void CanEventOnDone()
		{
			string workItem = String.Empty;
			bool expectedDoneSet = true;
			bool actualDoneSet = false;

			WorkerThread wt = new WorkerThread(a=>Trace.WriteLine("yeah, I'm not doing this"));
			wt.WorkItemComplete += (sender, e) => actualDoneSet = true;

			wt.CompleteWork(workItem);

			Assert.AreEqual(expectedDoneSet,actualDoneSet);
		}
	}
}

