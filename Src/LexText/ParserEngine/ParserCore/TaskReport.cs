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

using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for TaskReport.
	/// </summary>
	public sealed class TaskReport : IFWDisposable
	{
		public enum TaskPhase {Started, Working, Finished, ErrorEncountered};

		private readonly Action<TaskReport> m_taskUpdate;
		private List<TaskReport> m_subTasks;
		private string m_description;
		private readonly long m_start;
		private long m_finish;
		private TaskPhase m_phase;
		private TaskReport m_owningTask;
		private string m_notificationMessage;
		private bool m_isInDispose;		// ture if we're in the dispose method
		/// <summary>
		/// this was added to hold the results of a trace request
		/// </summary>
		private string m_details;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskReport"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TaskReport(string description, Action<TaskReport> eventReceiver)
			: this(description, (TaskReport) null)
		{
			m_taskUpdate = eventReceiver;
			InformListeners(TaskPhase.Started); // don't move this down to the other constructor
		}

		internal TaskReport(string description, TaskReport owningTask)
		{
			m_owningTask = owningTask;
			m_phase = TaskPhase.Started;
			m_description = description;
			m_start = DateTime.Now.Ticks;
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
		private bool m_isDisposed;

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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				m_isInDispose = true;
				// handle the case where the child is disposed of before the parent and not by the parent directly
				if (m_owningTask != null)
					m_owningTask.RemoveChildTaskReport(this);	// break assocations before disposing

				// Dispose managed resources here.
				if (m_subTasks != null)
				{
					foreach (TaskReport task in m_subTasks)
						task.Dispose();
				}
				Finish();
				if (m_subTasks != null)
					m_subTasks.Clear();
				m_isInDispose = false;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_owningTask = null;
			m_details = null;
			m_description = null;
			m_notificationMessage = null;
			m_subTasks = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		internal TaskReport AddSubTask(string description)
		{
			CheckDisposed();

			Debug.Assert(m_phase != TaskPhase.Finished);
			var t = new TaskReport(description, this);
			if (m_subTasks == null)
				m_subTasks = new List<TaskReport>();
			else
			{	//a new set task should not be added until the previous one is finished.
				TaskPhase phase = m_subTasks[m_subTasks.Count - 1].Phase;
				Debug.Assert(phase == TaskPhase.Finished);// || phase == TaskPhase.ErrorEncountered);
			}
			m_subTasks.Add(t);
			//this cannot be in the constructor because if the listener asks
			//for the most recent task, it will not get this one until after
			//this has been added to the subtasks list.
			t.InformListeners(TaskPhase.Started);
			return t;
		}

		internal void Finish()
		{
			CheckDisposed();

			//this is called when we are disposed; it is possible that we were explicitly finished already.
			if (m_phase == TaskPhase.Finished || m_phase == TaskPhase.ErrorEncountered)
				return;

			m_finish = DateTime.Now.Ticks;
			InformListeners(TaskPhase.Finished);
		}

		public string Description
		{
			get
			{
				CheckDisposed();

				return m_phase == TaskPhase.ErrorEncountered
						? string.Format(ParserCoreStrings.ksX_error, m_description)
						: m_description;
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

				 m_details = value;
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
				return (float) (DurationTicks / 10000000.0);
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
			//the root task executes this
			if (m_taskUpdate != null)
				m_taskUpdate(task);
			//all children tasks execute this
			else if (m_owningTask != null)
				m_owningTask.InformListeners(this);
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

				m_notificationMessage = value;
				if (value != null)
					InformListeners(TaskPhase.Working);
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
					case TaskPhase.Started:
						return ParserCoreStrings.ksStarted;
					case TaskPhase.Finished:
						return ParserCoreStrings.ksFinished;
					case TaskPhase.ErrorEncountered :
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

				if (m_phase == TaskPhase.ErrorEncountered || m_phase==TaskPhase.Finished || m_subTasks== null)
					return this;

				return m_subTasks[m_subTasks.Count -1].MostRecentTask;
			}
		}

		public int Depth
		{
			get
			{
				CheckDisposed();

				return m_owningTask == null ? 0 : m_owningTask.Depth + 1;
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
