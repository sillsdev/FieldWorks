// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary />
	public sealed class TaskReport : IDisposable
	{
		private readonly Action<TaskReport> m_taskUpdate;
		private string m_description;
		private readonly long m_start;
		private long m_finish;
		private TaskReport m_owningTask;
		private string m_notificationMessage;
		// true if we're in the dispose method
		private bool m_isInDispose;

		/// <summary />
		public TaskReport(string description, Action<TaskReport> eventReceiver)
			: this(description, (TaskReport)null)
		{
			m_taskUpdate = eventReceiver;
			InformListeners(TaskPhase.Started); // don't move this down to the other constructor
		}

		internal TaskReport(string description, TaskReport owningTask)
		{
			m_owningTask = owningTask;
			Phase = TaskPhase.Started;
			m_description = description;
			m_start = DateTime.Now.Ticks;
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

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
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				m_isInDispose = true;
				// handle the case where the child is disposed of before the parent and not by the parent directly
				m_owningTask?.RemoveChildTaskReport(this);   // break associations before disposing

				// Dispose managed resources here.
				if (SubTasks != null)
				{
					foreach (var task in SubTasks)
					{
						task.Dispose();
					}
				}
				Finish();
				SubTasks?.Clear();
				m_isInDispose = false;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_owningTask = null;
			Details = null;
			m_description = null;
			m_notificationMessage = null;
			SubTasks = null;

			IsDisposed = true;
		}

		internal void Finish()
		{
			//this is called when we are disposed; it is possible that we were explicitly finished already.
			if (Phase == TaskPhase.Finished || Phase == TaskPhase.ErrorEncountered)
			{
				return;
			}
			m_finish = DateTime.Now.Ticks;
			InformListeners(TaskPhase.Finished);
		}

		public string Description => Phase == TaskPhase.ErrorEncountered
			? string.Format(ParserCoreStrings.ksX_error, m_description)
			: m_description;

		/// <summary>
		/// this is used to hold the results of a trace request
		/// </summary>
		public XDocument Details { set; get; }

		public long DurationTicks => m_finish - m_start;

		public float DurationSeconds => (float)(DurationTicks / 10000000.0);

		public List<TaskReport> SubTasks { get; private set; }

		internal void InformListeners(TaskPhase phase)
		{
			Phase = phase;
			InformListeners(this);
		}

		private void InformListeners(TaskReport task)
		{
			//the root task executes this
			if (m_taskUpdate != null)
			{
				m_taskUpdate(task);
			}
			//all children tasks execute this
			else
			{
				m_owningTask?.InformListeners(this);
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
				return m_notificationMessage;
			}
			set
			{
				m_notificationMessage = value;
				if (value != null)
				{
					InformListeners(TaskPhase.Working);
				}
			}
		}

		public TaskPhase Phase { get; private set; }

		public string PhaseDescription
		{
			get
			{
				switch (Phase)
				{
					case TaskPhase.Started:
						return ParserCoreStrings.ksStarted;
					case TaskPhase.Finished:
						return ParserCoreStrings.ksFinished;
					case TaskPhase.ErrorEncountered:
						return ParserCoreStrings.ksErrorEncountered;
					default:
						return ParserCoreStrings.ksQuestions;
				}

			}
		}

		public TaskReport MostRecentTask => Phase == TaskPhase.ErrorEncountered || Phase == TaskPhase.Finished || SubTasks == null
				? this : SubTasks[SubTasks.Count - 1].MostRecentTask;

		public int Depth => m_owningTask?.Depth + 1 ?? 0;

		/// <summary>
		/// This method was added because child TaskReport objects were
		/// being disposed of while they were still contained in the parents
		/// subtask list.  Then when the parent was disposed of it would dispose
		/// of the child TaskReports that it had and one or more would already
		/// have been disposed.  the child
		/// would be attempted to dispose
		/// </summary>
		private void RemoveChildTaskReport(TaskReport report)
		{
			// If we're in the Dispose, part of it will call dispose on the child
			// objects which will call back here to let the parent break the ties,
			// but when we're in the dispose we don't want that to happen as we're
			// most likely walking the subtask list and this isn't needed.
			if (m_isInDispose == false && SubTasks != null)
			{
				SubTasks.Remove(report);
				if (SubTasks.Count == 0)  // other places in the code expect a non null value to mean data, so set it to null when empty.
				{
					SubTasks = null;
				}
			}
		}
	}
}