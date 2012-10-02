/*
 *
 * This file was taken from http://www.codeproject.com/KB/cs/tsnewlib.aspx
 * ("A New Task Scheduler Class Library for .NET" by Dennis Austin)
 * This file is licensed under The Code Project Open License (CPOL):
 * http://www.codeproject.com/info/cpol10.aspx
 *
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using TaskSchedulerInterop;

namespace TaskScheduler {
	/// <summary>
	/// ScheduledTasks represents the a computer's Scheduled Tasks folder.  Using a ScheduledTasks
	/// object, you can discover the names of the TaskNames in the folder and you can open a task
	/// to work with it.  You can also create or delete TaskNames.
	/// </summary>
	/// <remarks>
	/// A ScheduledTasks object holds a COM interface that can be released by calling <see cref="Dispose()"/>.
	/// </remarks>
	public class ScheduledTasks : IDisposable {
		/// <summary>
		/// Underlying COM interface.
		/// </summary>
		private ITaskScheduler its = null;

		// --- Contructors ---

		/// <summary>
		/// Constructor to use Scheduled TaskNames of a remote computer identified by a UNC
		/// name.  The calling process must have administrative privileges on the remote machine.
		/// May throw exception if the computer's task scheduler cannot be reached, and may
		/// give strange results if the argument is not in UNC format.
		/// </summary>
		/// <param name="computer">The remote computer's UNC name, e.g. "\\DALLAS".</param>
		/// <exception cref="ArgumentException">The Task Scheduler could not be accessed.</exception>
		public ScheduledTasks(string computer) : this() {
			its.SetTargetComputer(computer);
		}

		/// <summary>
		/// Constructor to use Scheduled Tasks of the local computer.
		/// </summary>
		public ScheduledTasks() {
			CTaskScheduler cts = new CTaskScheduler();
			its = (ITaskScheduler)cts;
		}

		// --- Methods ---

		private string[] GrowStringArray(string[] s, uint n) {
			string[] sl = new string[s.Length + n];
			for (int i=0; i<s.Length; i++) { sl[i] = s[i];}
			return sl;
		}

		/// <summary>
		/// Return the names of all scheduled TaskNames.  The names returned include the file extension ".job";
		/// methods requiring a task name can take the name with or without the extension.
		/// </summary>
		/// <returns>The names in a string array.</returns>
		public string[] GetTaskNames() {
			const int TASKS_TO_FETCH = 10;
			string[] taskNames = {};
			int nTaskNames = 0;

			IEnumWorkItems ienum;
			its.Enum(out ienum);

			uint nFetchedTasks;
			IntPtr pNames;

			while ( ienum.Next( TASKS_TO_FETCH, out pNames, out nFetchedTasks ) >= 0 &&
				nFetchedTasks > 0 ) {
				taskNames = GrowStringArray(taskNames, nFetchedTasks);
				while ( nFetchedTasks > 0 ) {
					IntPtr name = Marshal.ReadIntPtr( pNames, (int)--nFetchedTasks * IntPtr.Size );
					taskNames[nTaskNames++] = Marshal.PtrToStringUni(name);
					Marshal.FreeCoTaskMem(name);
				}
				Marshal.FreeCoTaskMem( pNames );
			}
			return taskNames;

		}

		/// <summary>
		/// Creates a new task on the system with the given <paramref name="name" />.
		/// </summary>
		/// <remarks>Task names follow normal filename character restrictions.  The name
		/// will be come the name of the file used to store the task (with .job added).</remarks>
		/// <param name="name">Name for the new task.</param>
		/// <returns>Instance of new task.</returns>
		/// <exception cref="ArgumentException">There is an existing task with the given name.</exception>
		public Task CreateTask(string name) {
			Task tester = OpenTask(name);
			if (tester != null) {
				tester.Close();
				throw new ArgumentException("The task \"" + name + "\" already exists.");
			}
			try {
				object o;
				its.NewWorkItem(name, ref CTaskGuid, ref ITaskGuid, out o);
				ITask iTask = (ITask)o;
				return new Task(iTask, name);
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Deletes the task of the given <paramref name="name" />.
		/// </summary>
		/// <remarks>If you delete a task that is open, a subsequent save will throw an
		/// exception.  You can save to a filename, however, to create a new task.</remarks>
		/// <param name="name">Name of task to delete.</param>
		/// <returns>True if the task was deleted, false if the task wasn'task found.</returns>
		public bool DeleteTask(string name) {
			try {
				its.Delete(name);
				return true;
			}
			catch {
				return false;
			}
		}

		/// <summary>
		/// Opens the task with the given <paramref name="name" />.  An open task holds COM interfaces
		/// which are released by the Task's Close() method.
		/// </summary>
		/// <remarks>If the task does not exist, null is returned.</remarks>
		/// <param name="name">Name of task to open.</param>
		/// <returns>An instance of a Task, or null if the task name couldn'task be found.</returns>
		public Task OpenTask(string name) {
			try {
				object o;
				its.Activate(name, ref ITaskGuid, out o);
				ITask iTask = (ITask)o;
				return new Task(iTask, name);
			}
			catch {
				return null;
			}
		}


		#region Implementation of IDisposable
		/// <summary>
		/// The internal COM interface is released.  Further access to the
		/// object will throw null reference exceptions.
		/// </summary>
		public void Dispose() {
			Marshal.ReleaseComObject(its);
			its = null;
		}
			#endregion

		// Two Guids for calls to ITaskScheduler methods Activate(), NewWorkItem(), and IsOfType()
		internal static Guid ITaskGuid;
		internal static Guid CTaskGuid;
		static ScheduledTasks() {
			ITaskGuid = Marshal.GenerateGuidForType(typeof(ITask));
			CTaskGuid = Marshal.GenerateGuidForType(typeof(CTask));
		}
	}

}