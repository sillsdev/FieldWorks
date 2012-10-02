/*
 *
 * This file was taken from http://www.codeproject.com/KB/cs/tsnewlib.aspx
 * ("A New Task Scheduler Class Library for .NET" by Dennis Austin)
 * This file is licensed under The Code Project Open License (CPOL):
 * http://www.codeproject.com/info/cpol10.aspx
 *
 */

using System;
using System.Collections;

namespace TaskScheduler
{
	/// <summary>
	/// Deprecated.  Provided for V1 compatibility only.
	/// </summary>
	/// <remarks>
	/// <p>Presents the Scheduled Tasks folder as a Task Collection. </p>
	///
	/// <p>A TaskList is indexed by name rather than position.
	/// You can'task add, remove, or assign TaskNames in TaskList.  Accessing
	/// a Task in the list by indexing, or by enumeration, is equivalent to opening a task
	/// by calling the ScheduledTasks Open() method.</p>
	/// <p><i>Provided for compatibility with version one of the library.  Use of Scheduler
	/// and TaskList will normally result in COM memory leaks.</i></p>
	/// </remarks>
	public class TaskList : IEnumerable, IDisposable
	{
		/// <summary>
		/// Scheduled Tasks folder supporting this TaskList.
		/// </summary>
		private ScheduledTasks st = null;

		/// <summary>
		/// Name of the target computer whose Scheduled Tasks are to be accessed.
		/// </summary>
		private string nameComputer;

		/// <summary>
		/// Constructors - marked internal so you have to create using Scheduler class.
		/// </summary>
		internal TaskList()
		{
			st = new ScheduledTasks();
		}

		internal TaskList(string computer) {
			st = new ScheduledTasks(computer);
		}

		/// <summary>
		/// Enumerator for <c>TaskList</c>
		/// </summary>
		private class Enumerator : IEnumerator
		{
			private ScheduledTasks outer;
			private string[] nameTask;
			private int curIndex;
			private Task curTask;

			/// <summary>
			/// Internal constructor - Only accessable through <see cref="IEnumerable.GetEnumerator()"/>
			/// </summary>
			/// <param name="st">ScheduledTasks object</param>
			internal Enumerator(ScheduledTasks st)
			{
				outer = st;
				nameTask = st.GetTaskNames();
				Reset();
			}

			/// <summary>
			/// Moves to the next task. See <see cref="IEnumerator.MoveNext()"/> for more information.
			/// </summary>
			/// <returns>true if next task found, false if no more TaskNames.</returns>
			public bool MoveNext()
			{
				bool ok = ++curIndex < nameTask.Length;
				if (ok) curTask = outer.OpenTask(nameTask[curIndex]);
				return ok;
			}

			/// <summary>
			/// Reset task enumeration. See <see cref="IEnumerator.Reset()"/> for more information.
			/// </summary>
			public void Reset()
			{
				curIndex = -1;
				curTask = null;
			}

			/// <summary>
			/// Retrieves the current task.  See <see cref="IEnumerator.Current"/> for more information.
			/// </summary>
			public object Current
			{
				get
				{
					return curTask;
				}
			}
		}

		/// <summary>
		/// Name of target computer
		/// </summary>
		internal string TargetComputer
		{
			get
			{
				return nameComputer;
			}
			set
			{
				st.Dispose();
				st = new ScheduledTasks(value);
				nameComputer = value;
			}
		}

		/// <summary>
		/// Creates a new task on the system with the supplied <paramref name="name" />.
		/// </summary>
		/// <param name="name">Unique display name for the task. If not unique, an ArgumentException will be thrown.</param>
		/// <returns>Instance of new task</returns>
		/// <exception cref="ArgumentException">There is already a task of the same name as the one supplied for the new task.</exception>
		public Task NewTask(string name)
		{
			return st.CreateTask(name);
		}

		/// <summary>
		/// Deletes the task of the given <paramref name="name" />.
		/// </summary>
		/// <param name="name">Name of task to delete</param>
		public void Delete(string name)
		{
			st.DeleteTask(name);
		}

		/// <summary>
		/// Indexer which retrieves task of given <paramref name="name" />.
		/// </summary>
		/// <param name="name">Name of task to retrieve</param>
		public Task this[string name]
		{
			get
			{
				return st.OpenTask(name);
			}
		}

		#region Implementation of IEnumerable
		/// <summary>
		/// Gets a TaskList enumerator
		/// </summary>
		/// <returns>Enumerator for TaskList</returns>
		public System.Collections.IEnumerator GetEnumerator()
		{
			return new Enumerator(st);
		}
			#endregion

		#region Implementation of IDisposable
		/// <summary>
		/// Disposes TaskList
		/// </summary>
		public void Dispose()
		{
			st.Dispose();
		}
			#endregion

	}


}