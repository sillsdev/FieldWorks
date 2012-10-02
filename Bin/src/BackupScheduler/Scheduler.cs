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
using System.Runtime.InteropServices;
using TaskSchedulerInterop;

namespace TaskScheduler
{
	/// <summary>
	/// Deprecated.  For V1 compatibility only.
	/// </summary>
	/// <remarks>
	/// <p>Scheduler is just a wrapper around the TaskList class.</p>
	/// <p><i>Provided for compatibility with version one of the library.  Use of Scheduler
	/// and TaskList will normally result in COM memory leaks.</i></p>
	/// </remarks>
	public class Scheduler
	{
		/// <summary>
		/// Internal field which holds TaskList instance
		/// </summary>
		private readonly TaskList tasks = null;

		/// <summary>
		/// Creates instance of task scheduler on local machine
		/// </summary>
		public Scheduler()
		{
			tasks = new TaskList();
		}

		/// <summary>
		/// Creates instance of task scheduler on remote machine
		/// </summary>
		/// <param name="computer">Name of remote machine</param>
		public Scheduler(string computer)
		{
			tasks = new TaskList();
			TargetComputer = computer;
		}

		/// <summary>
		/// Gets/sets name of target computer. Null or emptry string specifies local computer.
		/// </summary>
		public string TargetComputer
		{
			get
			{
				return tasks.TargetComputer;
			}
			set
			{
				tasks.TargetComputer = value;
			}
		}

		/// <summary>
		/// Gets collection of system TaskNames
		/// </summary>
		public TaskList Tasks
		{
			get
			{
				return tasks;
			}
		}

	}
}
