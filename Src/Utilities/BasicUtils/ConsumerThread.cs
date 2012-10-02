using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace SIL.Utils
{
	#region IQueueAccessor interface
	/// <summary>
	/// This interface is used by a work delegate to retrieve work items
	/// from a <see cref="ConsumerThread{P,T}"/>
	/// </summary>
	/// <typeparam name="P">The queue priority type</typeparam>
	/// <typeparam name="T">The work item type</typeparam>
	public interface IQueueAccessor<P, T>
	{
		/// <summary>
		/// Gets the next work item.
		/// </summary>
		/// <param name="workItem">The work item.</param>
		/// <returns></returns>
		bool GetNextWorkItem(out T workItem);

		/// <summary>
		/// Gets the next work item with a priority that is greater than
		/// or equal to the specified priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		/// <param name="workItem">The work item.</param>
		/// <returns></returns>
		bool GetNextWorkItem(P lowestPriority, out T workItem);

		/// <summary>
		/// Gets the next work item and its corresponding priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="workItem">The work item.</param>
		/// <returns></returns>
		bool GetNextPriorityWorkItem(out P priority, out T workItem);

		/// <summary>
		/// Gets the next work item with a priority that is greater than
		/// or equal to the specified priority and its corresponding priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		/// <param name="priority">The priority.</param>
		/// <param name="workItem">The work item.</param>
		/// <returns></returns>
		bool GetNextPriorityWorkItem(P lowestPriority, out P priority, out T workItem);

		/// <summary>
		/// Gets all work items.
		/// </summary>
		/// <returns></returns>
		IEnumerable<T> GetAllWorkItems();

		/// <summary>
		/// Gets all work items with a priority that is greater than or
		/// equal to the specified priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		/// <returns></returns>
		IEnumerable<T> GetAllWorkItems(P lowestPriority);

		/// <summary>
		/// Gets all work items along with their corresponding priority.
		/// </summary>
		/// <returns></returns>
		IEnumerable<Tuple<P, T>> GetAllPriorityWorkItems();

		/// <summary>
		/// Gets all work items with a priority that is greater than or
		/// equal to the specified priority along with their corresponding
		/// priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		/// <returns></returns>
		IEnumerable<Tuple<P, T>> GetAllPriorityWorkItems(P lowestPriority);
	}
	#endregion

	#region ConsumerThread class
	/// <summary>
	/// This class is a consumer thread that processes queued work items. A specified
	/// delegate is responsible for processing queued work items. The delegate can
	/// retrieve work items from the thread's queue using <see cref="IQueueAccessor{P,T}"/>.
	/// </summary>
	/// <typeparam name="P">The queue priority type</typeparam>
	/// <typeparam name="T">The work item type</typeparam>
	public class ConsumerThread<P, T> : IQueueAccessor<P, T>, IDisposable
	{
		#region Member variables
		private readonly Thread m_thread;
		private readonly object m_syncRoot = new object();
		private readonly ManualResetEvent m_workEvent = new ManualResetEvent(false);
		private readonly ManualResetEvent m_stopEvent = new ManualResetEvent(false);
		private readonly ManualResetEvent m_idleEvent = new ManualResetEvent(true);
		private readonly PriorityQueue<P, T> m_queue;
		private readonly Action m_initHandler;
		private readonly Action<IQueueAccessor<P, T>> m_workHandler;
		private bool m_fWaitForNextRequest;
		private bool m_hasWork;
		private bool m_isIdle = true;
		private Exception m_unhandledException;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="ConsumerThread&lt;P, T&gt;"/> class.
		/// </summary>
		/// <param name="workHandler">The work handler.</param>
		public ConsumerThread(Action<IQueueAccessor<P, T>> workHandler) : this(workHandler, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsumerThread&lt;P, T&gt;"/> class.
		/// </summary>
		/// <param name="workHandler">The work handler.</param>
		/// <param name="initHandler">The init handler.</param>
		public ConsumerThread(Action<IQueueAccessor<P, T>> workHandler, Action initHandler)
			: this(workHandler, initHandler, Comparer<P>.Default, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsumerThread&lt;P, T&gt;"/> class.
		/// </summary>
		/// <param name="workHandler">The work handler.</param>
		/// <param name="initHandler">The init handler.</param>
		/// <param name="priorityComparer">The priority comparer.</param>
		/// <param name="workComparer">The work comparer.</param>
		public ConsumerThread(Action<IQueueAccessor<P, T>> workHandler, Action initHandler, IComparer<P> priorityComparer,
			IEqualityComparer<T> workComparer)
		{
			if (workHandler == null)
				throw new ArgumentNullException("workHandler");

			m_workHandler = workHandler;
			m_initHandler = initHandler;
			m_queue = new PriorityQueue<P, T>(priorityComparer, workComparer);
			m_thread = new Thread(WorkLoop);
			m_thread.Name = "Consumer Thread";
		}
		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~ConsumerThread()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				((IDisposable)m_workEvent).Dispose();
				((IDisposable)m_stopEvent).Dispose();
				((IDisposable)m_idleEvent).Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets a value indicating whether this is a background thread.
		/// NOTE: A foreground thread will exit when all of its work has been completed.
		/// </summary>
		/// <seealso cref="WaitForNextRequest"/>
		/// <value>
		/// 	<c>true</c> if this is a background thread, otherwise <c>false</c>.
		/// </value>
		public bool IsBackground
		{
			get { return m_thread.IsBackground; }
			set { m_thread.IsBackground = value; }
		}

		/// <summary>
		/// if there has been an error, this will return the exception that stopped it.
		/// </summary>
		public Exception UnhandledException
		{
			get
			{
				lock (SyncRoot)
					return m_unhandledException;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the thread is alive.
		/// </summary>
		/// <value><c>true</c> if the thread is alive, otherwise, <c>false</c>.</value>
		public bool IsAlive
		{
			get { return m_thread.IsAlive; }
		}

		/// <summary>
		/// Gets a value indicating whether the thread is idle.
		/// </summary>
		/// <value><c>true</c> if the thread is idle; otherwise, <c>false</c>.</value>
		public bool IsIdle
		{
			get
			{
				lock (SyncRoot)
					return m_isIdle;
			}
		}

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this scheduler.
		/// </summary>
		/// <value>The synchronization root.</value>
		private object SyncRoot
		{
			get { return m_syncRoot; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates to the thread that it should stay alive until it gets a request to enqueue
		/// work.
		/// </summary>
		/// <returns><c>true</c> if the thread is still alive and is waiting for work;
		/// <c>false</c> if the thread is dead</returns>
		/// ------------------------------------------------------------------------------------
		public bool WaitForNextRequest()
		{
			lock (SyncRoot)
			{
				if (!IsAlive)
					return false;
				m_fWaitForNextRequest = true;
				return true;
			}
		}

		/// <summary>
		/// Enqueues the work.
		/// </summary>
		/// <param name="work">The work.</param>
		public void EnqueueWork(T work)
		{
			EnqueueWork(default(P), work);
		}

		/// <summary>
		/// Enqueues the work.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="work">The work.</param>
		public void EnqueueWork(P priority, T work)
		{
			lock (SyncRoot)
			{
				m_queue.Enqueue(priority, work);
				WakeUp();
			}
		}

		/// <summary>
		/// Starts the thread.
		/// </summary>
		public void Start()
		{
			m_thread.Start();
		}

		/// <summary>
		/// Stops the thread. This method will block until the thread stops. If the thread is currently
		/// doing work on an item, it will not stop until the work has completed.
		/// </summary>
		/// <returns><c>true</c> if the thread stopped cleanly, otherwise <c>false</c>.</returns>
		public bool Stop()
		{
			m_stopEvent.Set();
			if (!m_thread.Join(60000))
			{
				m_thread.Abort();
				m_thread.Join(10000);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Stops the thread when it is idle. This method will block until the thread stops. The thread
		/// will not be stopped until all queued work has completed.
		/// </summary>
		/// <returns></returns>
		public bool StopOnIdle()
		{
			WaitUntilIdle();
			return Stop();
		}

		/// <summary>
		/// Wakes up the thread so that it can try to process any queued work. If there is no queued work
		/// the thread will become idle again.
		/// </summary>
		public void WakeUp()
		{
			lock (SyncRoot)
			{
				m_isIdle = false;
				m_idleEvent.Reset();
				m_hasWork = true;
				m_fWaitForNextRequest = false;
				m_workEvent.Set();
			}
		}

		/// <summary>
		/// Blocks until the thread is idle.
		/// </summary>
		public void WaitUntilIdle()
		{
			m_idleEvent.WaitOne();
		}
		#endregion

		#region IQueueAccessor implementation
		/// <summary>
		/// Gets the next work item.
		/// </summary>
		/// <param name="workItem">The work item.</param>
		bool IQueueAccessor<P, T>.GetNextWorkItem(out T workItem)
		{
			P priority;
			return GetNextPriorityWorkItem(out priority, out workItem);
		}

		/// <summary>
		/// Gets the next work item with a priority that is greater than
		/// or equal to the specified priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		/// <param name="workItem">The work item.</param>
		bool IQueueAccessor<P, T>.GetNextWorkItem(P lowestPriority, out T workItem)
		{
			P priority;
			return GetNextPriorityWorkItem(lowestPriority, out priority, out workItem);
		}

		/// <summary>
		/// Gets the next work item and its corresponding priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="workItem">The work item.</param>
		bool IQueueAccessor<P, T>.GetNextPriorityWorkItem(out P priority, out T workItem)
		{
			return GetNextPriorityWorkItem(out priority, out workItem);
		}

		/// <summary>
		/// Gets the next work item with a priority that is greater than
		/// or equal to the specified priority and its corresponding priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		/// <param name="priority">The priority.</param>
		/// <param name="workItem">The work item.</param>
		bool IQueueAccessor<P, T>.GetNextPriorityWorkItem(P lowestPriority, out P priority, out T workItem)
		{
			return GetNextPriorityWorkItem(lowestPriority, out priority, out workItem);
		}

		/// <summary>
		/// Gets all work items.
		/// </summary>
		IEnumerable<T> IQueueAccessor<P, T>.GetAllWorkItems()
		{
			return GetAllPriorityWorkItems().Select(workItem => workItem.Item2);
		}

		/// <summary>
		/// Gets all work items with a priority that is greater than or
		/// equal to the specified priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		IEnumerable<T> IQueueAccessor<P, T>.GetAllWorkItems(P lowestPriority)
		{
			return GetAllPriorityWorkItems(lowestPriority).Select(workItem => workItem.Item2);
		}

		/// <summary>
		/// Gets all work items along with their corresponding priority.
		/// </summary>
		/// <returns></returns>
		IEnumerable<Tuple<P, T>> IQueueAccessor<P, T>.GetAllPriorityWorkItems()
		{
			return GetAllPriorityWorkItems();
		}

		/// <summary>
		/// Gets all work items with a priority that is greater than or
		/// equal to the specified priority along with their corresponding
		/// priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		IEnumerable<Tuple<P, T>> IQueueAccessor<P, T>.GetAllPriorityWorkItems(P lowestPriority)
		{
			return GetAllPriorityWorkItems(lowestPriority);
		}
		#endregion

		#region Private methods
		private void WorkLoop()
		{
			try
			{
				if (m_initHandler != null)
					m_initHandler();
				var events = new WaitHandle[] { m_stopEvent, m_workEvent };
				while (WaitHandle.WaitAny(events) != 0)
				{
					m_workHandler((IQueueAccessor<P, T>)this);
					lock (SyncRoot)
					{
						// if there is no work, signal that the thread is idle
						if (!m_hasWork)
						{
							if (IsBackground)
							{
								m_isIdle = true;
								m_idleEvent.Set();
							}
							else if (!m_fWaitForNextRequest)
							{
								break;
							}
						}
					}
				}

				lock (SyncRoot)
				{
					// If another thread was waiting for the idle event, we need to make sure
					// we signal that thread that this thread is "idling" even though it's
					// technically exiting.
					m_isIdle = true;
					m_idleEvent.Set();
				}
			}
			catch (ThreadInterruptedException)
			{
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception error)
			{
				lock (SyncRoot)
					m_unhandledException = error;
			}
		}

		/// <summary>
		/// Gets the next work item and its corresponding priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="workItem">The work item.</param>
		private bool GetNextPriorityWorkItem(out P priority, out T workItem)
		{
			lock (SyncRoot)
			{
				if (!m_queue.IsEmpty)
				{
					workItem = m_queue.Dequeue(out priority);
					if (m_queue.IsEmpty)
						SetNoWork();
					return true;
				}
				else
				{
					// the queue is empty, so idle
					SetNoWork();
					priority = default(P);
					workItem = default(T);
					return false;
				}
			}
		}

		/// <summary>
		/// Gets the next work item with a priority that is greater than
		/// or equal to the specified priority and its corresponding priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		/// <param name="priority">The priority.</param>
		/// <param name="workItem">The work item.</param>
		private bool GetNextPriorityWorkItem(P lowestPriority, out P priority, out T workItem)
		{
			lock (SyncRoot)
			{
				if (!m_queue.IsEmpty)
				{
					// ensure that the next item has a high enough priority to be processed
					P nextPriority;
					m_queue.Peek(out nextPriority);
					if (m_queue.PriorityComparer.Compare(nextPriority, lowestPriority) <= 0)
					{
						// the next item has a high enough priority, so dequeue it
						workItem = m_queue.Dequeue(out priority);
						if (!m_queue.IsEmpty)
						{
							// check the next item's priority to see if we need to reset the work event
							m_queue.Peek(out nextPriority);
							if (m_queue.PriorityComparer.Compare(nextPriority, lowestPriority) > 0)
								SetNoWork();
						}
						else
						{
							// the queue is now empty, so idle
							SetNoWork();
						}
						return true;
					}
					else
					{
						// the next item has a lower priority than the lowest allowed priority, so idle
						SetNoWork();
						priority = default(P);
						workItem = default(T);
						return false;
					}
				}
				else
				{
					// the queue is empty, so idle
					SetNoWork();
					priority = default(P);
					workItem = default(T);
					return false;
				}
			}
		}

		/// <summary>
		/// Gets all work items along with their corresponding priority.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<Tuple<P, T>> GetAllPriorityWorkItems()
		{
			lock (SyncRoot)
			{
				var workItems = new List<Tuple<P, T>>();
				while (!m_queue.IsEmpty)
				{
					P priority;
					T workItem = m_queue.Dequeue(out priority);
					workItems.Add(Tuple.Create(priority, workItem));
				}
				SetNoWork();
				return workItems;
			}
		}

		/// <summary>
		/// Gets all work items with a priority that is greater than or
		/// equal to the specified priority along with their corresponding
		/// priority.
		/// </summary>
		/// <param name="lowestPriority">The lowest priority.</param>
		private IEnumerable<Tuple<P, T>> GetAllPriorityWorkItems(P lowestPriority)
		{
			lock (SyncRoot)
			{
				var workItems = new List<Tuple<P, T>>();
				while (!m_queue.IsEmpty)
				{
					P nextPriority;
					m_queue.Peek(out nextPriority);

					if (m_queue.PriorityComparer.Compare(nextPriority, lowestPriority) > 0)
						break;

					// the next item has a high enough priority, so dequeue it
					P priority;
					T workItem = m_queue.Dequeue(out priority);
					workItems.Add(Tuple.Create(priority, workItem));
				}
				SetNoWork();
				return workItems;
			}
		}

		private void SetNoWork()
		{
			m_hasWork = false;
			m_workEvent.Reset();
		}
		#endregion
	}
	#endregion
}
