// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// <summary>
	/// Idle queue priority
	/// </summary>
	public enum IdleQueuePriority
	{
		/// <summary>
		/// High
		/// </summary>
		High = 0,
		/// <summary>
		/// Medium
		/// </summary>
		Medium = 1,
		/// <summary>
		/// Low
		/// </summary>
		Low = 2
	}

	/// <summary>
	/// This class represents a task that will be executed when
	/// the application is idle.
	/// </summary>
	public struct IdleQueueTask
	{
		private readonly IdleQueuePriority m_priority;
		private readonly Func<object, bool> m_del;
		private readonly object m_parameter;

		/// <summary>
		/// Initializes a new instance of the <see cref="IdleQueueTask"/> struct.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="del">The delegate.</param>
		/// <param name="parameter">The parameter.</param>
		public IdleQueueTask(IdleQueuePriority priority, Func<object, bool> del, object parameter)
		{
			m_priority = priority;
			m_del = del;
			m_parameter = parameter;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IdleQueueTask"/> struct.
		/// </summary>
		/// <param name="del">The delegate.</param>
		public IdleQueueTask(Func<object, bool> del)
			: this(IdleQueuePriority.Medium, del, null)
		{
		}

		/// <summary>
		/// Gets the priority.
		/// </summary>
		/// <value>The priority.</value>
		public IdleQueuePriority Priority
		{
			get
			{
				return m_priority;
			}
		}

		/// <summary>
		/// Gets the delegate.
		/// </summary>
		/// <value>The delegate.</value>
		public Func<object, bool> Delegate
		{
			get
			{
				return m_del;
			}
		}

		/// <summary>
		/// Gets the parameter.
		/// </summary>
		/// <value>The parameter.</value>
		public object Parameter
		{
			get
			{
				return m_parameter;
			}
		}

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">Another object to compare to.</param>
		/// <returns>
		/// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (!(obj is IdleQueueTask))
				return false;

			return Equals((IdleQueueTask) obj);
		}

		/// <summary>
		/// Determines if this task equals the specified task.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(IdleQueueTask other)
		{
			return m_del == other.m_del;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this instance.
		/// </returns>
		public override int GetHashCode()
		{
			return m_del.GetHashCode();
		}
	}

	/// <summary>
	/// This class provides a simple way of scheduling tasks to be run while
	/// the application is idle. This queue must be created and disposed on the UI thread. It is thread-safe
	/// within individual methods and properties, but not across method and property calls.
	/// </summary>
	public class IdleQueue : ICollection<IdleQueueTask>, IFWDisposable
	{
		private readonly PriorityQueue<IdleQueuePriority, IdleQueueTask> m_queue = new PriorityQueue<IdleQueuePriority, IdleQueueTask>();
		private readonly object m_syncRoot = new object();
		private bool m_paused;

		/// <summary>
		/// Initializes a new instance of the <see cref="IdleQueue"/> class.
		/// </summary>
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

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Method can be called several times,
			// but the main code must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				Application.Idle -= Application_Idle;
				m_queue.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			IsDisposed = true;
		}

		#endregion

		#region Implementation of IFWDisposable

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		public bool IsDisposed
		{
			get; private set;
		}

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("IdleQueue", "This object is being used after it has been disposed: this is an Error.");
		}

		#endregion

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking. Used to lock m_queue.
		/// </summary>
		/// <value>The synchronization root.</value>
		private object SyncRoot
		{
			get
			{
				return m_syncRoot;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is paused.
		/// </summary>
		/// <value><c>true</c> if this instance is paused; otherwise, <c>false</c>.</value>
		public bool IsPaused
		{
			get
			{
				CheckDisposed();
				return m_paused;
			}

			set
			{
				CheckDisposed();
				if (value && !m_paused)
					Application.Idle -= Application_Idle;
				else if (!value && m_paused)
					Application.Idle += Application_Idle;
				m_paused = value;
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
			CheckDisposed();
			lock (SyncRoot)
				m_queue.Enqueue(priority, new IdleQueueTask(priority, del, parameter), update);
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
			CheckDisposed();
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
			CheckDisposed();
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
			CheckDisposed();
			Add(priority, del, null);
		}

		/// <summary>
		/// Removes the specified delegate from the queue.
		/// </summary>
		/// <param name="del">The delegate.</param>
		public bool Remove(Func<object, bool> del)
		{
			CheckDisposed();
			lock (SyncRoot)
				return m_queue.Remove(new IdleQueueTask(del));
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
			CheckDisposed();
			lock (SyncRoot)
				return m_queue.Contains(new IdleQueueTask(del));
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
						if (!m_queue.IsEmpty)
						{
							task = m_queue.Dequeue();
						}
						else
						{
							break;
						}
					}
					// if it is not complete, queue it up to run when the application is idle again.
					if (!task.Delegate(task.Parameter))
						incompleteTasks.Add(task);
#if __MonoCS__ // FWNX-348
					// ShouldAbort() always == false on mono because of no PeekMessage
					// so run one cycle and wait for the next Application Idle event
					break;
#endif


				}
			}
			finally
			{
				lock (SyncRoot)
				{
					foreach (var task in incompleteTasks)
						m_queue.Enqueue(task.Priority, task);
				}
			}
		}

		static bool ShouldAbort()
		{
#if !__MonoCS__ // FWNX-348
			var msg = new Win32.MSG();
			return Win32.PeekMessage(ref msg, IntPtr.Zero, 0, 0, (uint)Win32.PeekFlags.PM_NOREMOVE);
#else
			// The above code is examining the Windows Message Pump to
			// see if there are any queued messages if there are it returns true
			// to cancel the idle.

			return false;
#endif
		}

		#region Implementation of IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the queue.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the queue.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public IEnumerator<IdleQueueTask> GetEnumerator()
		{
			CheckDisposed();
			IdleQueueTask[] tasks;
			lock (SyncRoot)
				tasks = m_queue.ToArray();
			foreach (var task in tasks)
				yield return task;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Implementation of ICollection<IdleQueueTask>

		/// <summary>
		/// Schedules the specified delegate to be invoked when the application
		/// is idle.
		/// </summary>
		/// <param name="task">The task.</param>
		public void Add(IdleQueueTask task)
		{
			CheckDisposed();
			lock (SyncRoot)
				m_queue.Enqueue(task.Priority, task);
		}

		/// <summary>
		/// Removes all tasks from the queue.
		/// </summary>
		public void Clear()
		{
			CheckDisposed();
			lock (SyncRoot)
				m_queue.Clear();
		}

		/// <summary>
		/// Determines whether the queue contains a specific task.
		/// </summary>
		/// <returns>
		/// true if <paramref name="task"/> is found in the queue; otherwise, false.
		/// </returns>
		/// <param name="task">The task.</param>
		public bool Contains(IdleQueueTask task)
		{
			CheckDisposed();
			lock (SyncRoot)
				return m_queue.Contains(task);
		}

		/// <summary>
		/// Copies the tasks of the queue to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
		///                     -or-
		///                    <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
		///                     -or-
		///                     The number of elements in the source queue is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
		/// </exception>
		public void CopyTo(IdleQueueTask[] array, int arrayIndex)
		{
			CheckDisposed();
			lock (SyncRoot)
				m_queue.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Removes the first occurrence of a specific task from the queue.
		/// </summary>
		/// <returns>
		/// true if <paramref name="task"/> was successfully removed from the queue; otherwise, false. This method also returns false if <paramref name="task"/> is not found in the original queue.
		/// </returns>
		/// <param name="task">The task.</param>
		public bool Remove(IdleQueueTask task)
		{
			CheckDisposed();
			lock (SyncRoot)
				return m_queue.Remove(task);
		}

		/// <summary>
		/// Gets the number of tasks contained in the queue.
		/// </summary>
		/// <returns>
		/// The number of tasks contained in the queue.
		/// </returns>
		public int Count
		{
			get
			{
				CheckDisposed();
				lock (SyncRoot)
					return m_queue.Count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the queue is read-only.
		/// </summary>
		/// <returns>
		/// true if the queue is read-only; otherwise, false.
		/// </returns>
		public bool IsReadOnly
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		#endregion
	}
}
