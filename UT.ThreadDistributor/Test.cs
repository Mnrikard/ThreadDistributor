using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ThreadDistributor;

namespace UT.ThreadDistributor
{
	[TestFixture()]
	public class Test
	{
		private Queue<string> _worklist;

		[SetUp()]
		public void InitList()
		{
			_worklist = new Queue<string>();
			for(int i=0;i<100;i++)
			{
				_worklist.Enqueue(i.ToString());
			}
		}

		private List<object> GetMoreWork(int maxItems)
		{
			List<object> output = new List<object>();
			for(int i=0;i<maxItems;i++)
			{
				if(_worklist.Count == 0)
				{
					break;
				}

				output.Add(_worklist.Dequeue());
			}

			return output;
		}

		private void WorkItem(object o)
		{
			Trace.WriteLine(String.Concat("worked item:",o == null ? String.Empty : o.ToString()));
		}

		[Test()]
		public void CanDistributor ()
		{
			Distributor td = new Distributor(GetMoreWork, WorkItem, 2, TimeSpan.FromSeconds(1));
			Assert.IsInstanceOf<Distributor>(td);
		}

		[Test()]
		public void CanStartDistributor()
		{
			Distributor td = new Distributor(GetMoreWork, WorkItem, 2, TimeSpan.FromSeconds(1));
			// all we can do here is see it not fail.

			string expected = "z";
			string actual=null;
			td.GetMoreWork = (a => {actual="z";return new List<object>();});
			td.StartDistribution();
			td.DispatchThreads(new AutoResetEvent(true));
			Assert.AreEqual(expected,actual);
		}

		[Test()]
		public void CanStopDistributor()
		{
			Distributor td = new Distributor(GetMoreWork, WorkItem, 2, TimeSpan.FromSeconds(1));
			td.StopDistribution();
			Assert.IsTrue(td._stopping);

			string unexpected = "z";
			string actual=null;
			td.GetMoreWork = (a => {actual="z";return new List<object>();});
			td.DispatchThreads(new AutoResetEvent(true));
			Assert.AreNotEqual(unexpected,actual);
		}

		[Test()]
		public void CanItWork()
		{
			AutoResetEvent waiter = new AutoResetEvent(false);
			Distributor td = new Distributor(GetMoreWork,WorkItem,2,TimeSpan.FromHours(200));
			td.ExceptionOccurred += (a,b) => Trace.WriteLine(b.ExceptionObject.ToString());
			td.WorkItemsCleared += (a,b) => waiter.Set();

			td.StartDistribution();

			waiter.WaitOne();
			td.StopDistribution();

			Assert.IsEmpty(_worklist);
		}

		[Test()]
		public void CanItStopInTheMiddle()
		{
			AutoResetEvent waiter = new AutoResetEvent(false);
			int actualQueuesProcessed = 0;
			int expectedQueuesProcessed = 1;
			Distributor td = null;
			td = new Distributor(
				GetMoreWork,
				a => {td.StopDistribution();waiter.Set();actualQueuesProcessed++;},
				1,TimeSpan.FromHours(200));

			td.ExceptionOccurred += (a,b) => Trace.WriteLine(b.ExceptionObject.ToString());
			td.WorkItemsCleared += (a,b) => waiter.Set();

			td.StartDistribution();

			waiter.WaitOne();

			Assert.AreEqual(expectedQueuesProcessed,actualQueuesProcessed);
		}

		[Test()]
		public void CanItLogErrorsAndContinue()
		{
			AutoResetEvent waiter = new AutoResetEvent(false);
			int actualQueuesProcessed = 0;
			int expectedQueuesProcessed = _worklist.Count;
			int exceptionsThrown = 0;
			Distributor td = null;
			td = new Distributor(
				GetMoreWork,
				a => {actualQueuesProcessed++;throw new Exception("this failed");},
			1,TimeSpan.FromHours(200));

			td.ExceptionOccurred += (a,b) => {Trace.WriteLine(b.ExceptionObject.ToString());exceptionsThrown++;};
			td.WorkItemsCleared += (a,b) => waiter.Set();

			td.StartDistribution();

			waiter.WaitOne();
			td.StopDistribution();

			Assert.AreEqual(expectedQueuesProcessed,actualQueuesProcessed);
			Assert.AreEqual(actualQueuesProcessed, exceptionsThrown);
		}
	}
}

