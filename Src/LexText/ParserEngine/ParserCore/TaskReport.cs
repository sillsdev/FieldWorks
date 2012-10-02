// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TaskReport.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public delegate void TaskUpdateEventHandler(TaskReport topTask);

	/// <summary>
	/// Summary description for TaskReport.
	/// </summary>
	public sealed class TaskReport : MarshalByRefObject, IFWDisposable
	{
		public event TaskUpdateEventHandler m_taskUpdateEvent = delegate {};  // in theory, setting to delegate{} makes the event thread safe

		public enum TaskPhase {started, working, finished, errorEncountered};

		private List<TaskReport> m_subTasks;
		private string m_description;
		private long m_start;
		private long m_finish;
		private TaskPhase m_phase;
		private TaskReport m_OwningTask;
		private Exception m_currentError;
		private string m_notificationMessage;
		private bool m_isInDispose = false;		// ture if we're in the dispose method
		/// <summary>
		/// this was added to hold the results of a trace request
		/// </summary>
		private string m_details;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskReport"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TaskReport(string description, TaskUpdateEventHandler eventReceiver)
			:this(description, (TaskReport)null)
		{
			if (eventReceiver!= null)
						   m_taskUpdateEvent += eventReceiver;
			InformListeners (TaskPhase.started);//don't move this down to the other constructor
		}

//		public TaskReport(string description)
//		{
//			m_phase=TaskPhase.started;
//			m_description = description;
//			m_start = DateTime.Now.Ticks;
//		}
		internal TaskReport(string description, TaskReport owningTask)
		{
			m_OwningTask = owningTask;
			m_phase=TaskPhase.started;
			m_description = description;
			m_start = DateTime.Now.Ticks;
		}

		/// <summary>
		/// Set lifetime of the remoting object to infinite so we don't lose the connection (fixes LT-8597 and LT-8619)
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~TaskReport()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
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
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				m_isInDispose = true;
				// handle the case where the child is disposed of before the parent and not by the parent directly
				if (m_OwningTask != null)
					m_OwningTask.RemoveChildTaskReport(this);	// break assocations before disposing

				// Dispose managed resources here.
				if(m_subTasks != null)
				{
					foreach(TaskReport task in m_subTasks)
						task.Dispose();
				}
				Finish();
				if (m_subTasks != null)
					m_subTasks.Clear();
				m_isInDispose = false;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_OwningTask = null;
			m_currentError = null;
			m_details = null;
			m_description = null;
			m_notificationMessage = null;
			m_subTasks = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		internal TaskReport AddSubTask (string description)
		{
			CheckDisposed();

			Debug.Assert( m_phase != TaskPhase.finished);
			TaskReport t = new TaskReport(description, this);
			if (m_subTasks == null)
				m_subTasks = new List<TaskReport>();
			else
			{	//a new set task should not be added until the previous one is finished.
				TaskPhase  phase = m_subTasks[m_subTasks.Count - 1].Phase;
				Debug.Assert( phase == TaskPhase.finished);// || phase == TaskPhase.errorEncountered);
			}
			m_subTasks.Add(t);
			//this cannot be in the constructor because if the listener asks
			//for the most recent task, it will not get this one until after
			//this has been added to the subtasks list.
			t.InformListeners (TaskPhase.started);
			return t;
		}

		internal void Finish()
		{
			CheckDisposed();

			//this is called when we are disposed; it is possible that we were explicitly finished already.
			if( m_phase == TaskPhase.finished ||  m_phase == TaskPhase.errorEncountered)
				return;

			m_finish = DateTime.Now.Ticks;
			InformListeners(TaskPhase.finished);
		}

		public string Description
		{
			get
			{
				CheckDisposed();

				if (m_phase == TaskPhase.errorEncountered)
					return String.Format(ParserCoreStrings.ksX_error, m_description);
				else
					return m_description;
			}
		}

		/// <summary>
		/// this is used to hold the results of a trace request
		/// </summary>
		public string Details
		{
			set
			{
				CheckDisposed();

				 m_details= value;
			}
			get
			{
				CheckDisposed();

				return m_details;
			}
		}

		public long DurationTicks
		{
			get
			{
				CheckDisposed();
				return m_finish - m_start;
			}
		}

		public float DurationSeconds
		{
			get
			{
				CheckDisposed();
				return (float)(DurationTicks / 10000000.0);
			}
		}

		public List<TaskReport> SubTasks
		{
			get
			{
				CheckDisposed();
				return m_subTasks;
			}
		}

		internal void InformListeners(TaskPhase phase)
		{
			CheckDisposed();

			m_phase = phase;
			InformListeners(this);
		}

		private void InformListeners(TaskReport task)
		{
			lock (this)
			{
				//the root task executes this
				if (m_taskUpdateEvent != null)
					m_taskUpdateEvent(task);

					//all children tasks execute this
				else if (m_OwningTask != null)
					this.m_OwningTask.InformListeners(this);

				else
				{
					//this is not really an error situation.  It just means that no one subscribed to events.
					//This may happen, for example, when we are just executing unit tests.
					//Debug.Assert( false);
				}
			}
		}

		public void EncounteredError(Exception error)
		{
			CheckDisposed();

			m_phase = TaskPhase.errorEncountered;
			m_currentError = error;

			InformListeners(this);
		}

		public Exception CurrentError
		{
			get
			{
				CheckDisposed();

				return m_currentError;
			}
		}

		/// <summary>
		/// a message to pass to the client which is not quite an error, but which warrants telling the user in a
		/// non intrusive fashion.
		/// </summary>
		public string NotificationMessage
		{
			get
			{
				CheckDisposed();

				return m_notificationMessage;
			}
			set
			{
				CheckDisposed();

				m_notificationMessage= value;
				if(value !=null)
					InformListeners (TaskPhase.working);
			}
		}

		public TaskPhase Phase
		{
			get
			{
				CheckDisposed();
				return m_phase;
			}
		}

		public string PhaseDescription
		{
			get
			{
				CheckDisposed();

				switch (m_phase)
				{
					case TaskPhase.started:
						return ParserCoreStrings.ksStarted;
					case TaskPhase.finished:
						return ParserCoreStrings.ksFinished;
					case TaskPhase.errorEncountered :
						return ParserCoreStrings.ksErrorEncountered;
					default:
						return ParserCoreStrings.ksQuestions;
				}

			}
		}

		public TaskReport MostRecentTask
		{
			get
			{
				CheckDisposed();

				if (m_phase == TaskPhase.errorEncountered || m_phase==TaskPhase.finished || m_subTasks== null)
					return this;
				else // ask the last member of our subtasks
					return ((TaskReport)m_subTasks[m_subTasks.Count -1]).MostRecentTask;
			}
		}

		public int Depth
		{
			get
			{
				CheckDisposed();

				if (m_OwningTask== null)
					return 0;
				else return m_OwningTask.Depth+1;
			}
		}

		/// <summary>
		/// This method was added because child TaskReport objects were
		/// being disposed of while they were still contained in the parents
		/// subtask list.  Then when the parent was disposed of it would dispose
		/// of the child TaskReports that it had and one or more would already
		/// have been disposed.  the child
		/// would be attempted to dispose
		/// </summary>
		/// <param name="report"></param>
		private void RemoveChildTaskReport(TaskReport report)
		{
			// If we're in the Dispose, part of it will call dispose on the child
			// objects which will call back here to let the parent break the ties,
			// but when we're in the dispose we don't want that to happen as we're
			// most likely walking the subtask list and this isn't needed.
			if (m_isInDispose == false && m_subTasks != null)
			{
//				Debug.WriteLine("** Disposing subtask <"+report.GetHashCode()+"> owned by " + GetHashCode().ToString());
				m_subTasks.Remove(report);
				if (m_subTasks.Count == 0)	// other places in the code expect a non null value to mean data, so set it to null when empty.
					m_subTasks = null;
			}
		}
	}
}
