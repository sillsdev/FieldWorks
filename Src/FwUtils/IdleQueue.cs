// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class provides a simple way of scheduling tasks to be run while
	/// the application is idle. This queue must be created and disposed on the UI thread. It is thread-safe
	/// within individual methods and properties, but not across method and property calls.
	/// </summary>
	public class IdleQueue : ICollection<IdleQueueTask>, IApplicationIdleEventHandler, IDisposable
	{
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int _countSuspendIdleProcessing;
		private readonly PriorityQueue<IdleQueuePriority, IdleQueueTask> _queue = new PriorityQueue<IdleQueuePriority, IdleQueueTask>();
		private bool _paused;

		/// <summary />
		public IdleQueue()
		{
			Application.Idle += Application_Idle;
		}

		#region Implementation of IDisposable

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. IsDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~IdleQueue()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the underlying issue.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Application.Idle -= Application_Idle;
				_queue.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			IsDisposed = true;
		}

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		private bool IsDisposed { get; set; }

		#endregion

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking. Used to lock m_queue.
		/// </summary>
		/// <value>The synchronization root.</value>
		private object SyncRoot { get; } = new object();

		/// <summary>
		/// Gets or sets a value indicating whether this instance is paused.
		/// </summary>
		/// <value><c>true</c> if this instance is paused; otherwise, <c>false</c>.</value>
		public bool IsPaused
		{
			get
			{
				return _paused;
			}

			set
			{
				if (value && !_paused)
				{
					Application.Idle -= Application_Idle;
				}
				else if (!value && _paused)
				{
					Application.Idle += Application_Idle;
				}
				_paused = value;
			}
		}

		/// <summary>
		/// Schedules the specified delegate to be invoked when the application
		/// is idle.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="del">The delegate.</param>
		/// <param name="parameter">The parameter.</param>
		/// <param name="update">if set to <c>true</c> any existing tasks with the
		/// delegate that are in the queue will be updated instead of a new task being added</param>
		public void Add(IdleQueuePriority priority, Func<object, bool> del, object parameter, bool update)
		{
			lock (SyncRoot)
			{
				_queue.Enqueue(priority, new IdleQueueTask(priority, del, parameter), update);
			}
		}

		/// <summary>
		/// Schedules the specified delegate to be invoked when the application
		/// is idle. A new task will be scheduled only if there is not an existing
		/// tasks with the delegate in the queue.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="del">The delegate.</param>
		/// <param name="parameter">The parameter.</param>
		public void Add(IdleQueuePriority priority, Func<object, bool> del, object parameter)
		{
			Add(priority, del, parameter, true);
		}

		/// <summary>
		/// Schedules the specified delegate to be invoked when the application
		/// is idle.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="del">The delegate.</param>
		/// <param name="update">if set to <c>true</c> any existing tasks with the
		/// delegate that are in the queue will be updated instead of a new task being added</param>
		public void Add(IdleQueuePriority priority, Func<object, bool> del, bool update)
		{
			Add(priority, del, null, update);
		}

		/// <summary>
		/// Schedules the specified delegate to be invoked when the application
		/// is idle. A new task will be scheduled only if there is not an existing
		/// tasks with the delegate in the queue.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="del">The delegate.</param>
		public void Add(IdleQueuePriority priority, Func<object, bool> del)
		{
			Add(priority, del, null);
		}

		/// <summary>
		/// Removes the specified delegate from the queue.
		/// </summary>
		/// <param name="del">The delegate.</param>
		public bool Remove(Func<object, bool> del)
		{
			lock (SyncRoot)
			{
				return _queue.Remove(new IdleQueueTask(del));
			}
		}

		/// <summary>
		/// Determines whether the queue contains a task with the specified delegate.
		/// </summary>
		/// <returns>
		/// true if a task with <paramref name="del"/> is found in the queue; otherwise, false.
		/// </returns>
		/// <param name="del">The delegate.</param>
		public bool Contains(Func<object, bool> del)
		{
			lock (SyncRoot)
			{
				return _queue.Contains(new IdleQueueTask(del));
			}
		}

		void Application_Idle(object sender, EventArgs e)
		{
			var incompleteTasks = new List<IdleQueueTask>();
			try
			{
				// Dispatch all of the items on the queue
				// we bail if there is a message waiting to be processed in the message pump
				while (!ShouldAbort())
				{
					IdleQueueTask task;
					lock (SyncRoot)
					{
						if (!_queue.IsEmpty)
						{
							task = _queue.Dequeue();
						}
						else
						{
							break;
						}
					}
					// if it is not complete, queue it up to run when the application is idle again.
					if (!task.Delegate(task.Parameter))
					{
						incompleteTasks.Add(task);
					}
					if (Platform.IsMono)
					{
						// FWNX-348
						// ShouldAbort() always == false on mono because of no PeekMessage
						// so run one cycle and wait for the next Application Idle event
						break;
					}
				}
			}
			finally
			{
				lock (SyncRoot)
				{
					foreach (var task in incompleteTasks)
					{
						_queue.Enqueue(task.Priority, task);
					}
				}
			}
		}

		private static bool ShouldAbort()
		{
			if (Platform.IsMono)
			{
				// FWNX-348
				// The below code is examining the Windows Message Pump to
				// see if there are any queued messages if there are it returns true
				// to cancel the idle.

				return false;
			}

			var msg = new Win32.MSG();
			return Win32.PeekMessage(ref msg, IntPtr.Zero, 0, 0, (uint)Win32.PeekFlags.PM_NOREMOVE);
		}

		#region Implementation of IEnumerable

		/// <inheritdoc />
		public IEnumerator<IdleQueueTask> GetEnumerator()
		{
			IdleQueueTask[] tasks;
			lock (SyncRoot)
			{
				tasks = _queue.ToArray();
			}
			foreach (var task in tasks)
			{
				yield return task;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Implementation of ICollection<IdleQueueTask>

		/// <inheritdoc />
		public void Add(IdleQueueTask task)
		{
			lock (SyncRoot)
			{
				_queue.Enqueue(task.Priority, task);
			}
		}

		/// <inheritdoc />
		public void Clear()
		{
			lock (SyncRoot)
			{
				_queue.Clear();
			}
		}

		/// <inheritdoc />
		public bool Contains(IdleQueueTask task)
		{
			lock (SyncRoot)
			{
				return _queue.Contains(task);
			}
		}

		/// <inheritdoc />
		public void CopyTo(IdleQueueTask[] array, int arrayIndex)
		{
			lock (SyncRoot)
			{
				_queue.CopyTo(array, arrayIndex);
			}
		}

		/// <inheritdoc />
		public bool Remove(IdleQueueTask task)
		{
			lock (SyncRoot)
			{
				return _queue.Remove(task);
			}
		}

		/// <inheritdoc />
		public int Count
		{
			get
			{
				lock (SyncRoot)
				{
					return _queue.Count;
				}
			}
		}

		/// <inheritdoc />
		public bool IsReadOnly => false;

		#endregion

		#region Implementation of IApplicationIdleEventHandler

		/// <inheritdoc />
		public void SuspendIdleProcessing()
		{
			_countSuspendIdleProcessing++;
			if (_countSuspendIdleProcessing == 1)
			{
				Application.Idle -= Application_Idle;
			}
		}

		/// <inheritdoc />
		public void ResumeIdleProcessing()
		{
			FwUtils.CheckResumeProcessing(_countSuspendIdleProcessing, GetType().Name);
			if (_countSuspendIdleProcessing > 0)
			{
				_countSuspendIdleProcessing--;
				if (_countSuspendIdleProcessing == 0)
				{
					Application.Idle += Application_Idle;
				}
			}
		}
		#endregion
	}
}