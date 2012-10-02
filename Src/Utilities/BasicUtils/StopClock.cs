// Copyright (c) 2011, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2011-09-02 StopClock.cs

using System;
using System.Diagnostics;

namespace SIL.Utils
{
	/// <summary>
	/// Performance measurement tool.
	/// </summary>
	/// <example>
	/// using (new StopClock("A"))
	/// {
	/// 	Foo();
	/// 	using (new StopClock("B1"))
	/// 		Bar();
	/// 	using (new StopClock("B2"))
	/// 		Baz();
	/// }
	/// </example>
	/// <seealso cref="TimeRecorder"/>
	public class StopClock : FwDisposableBase
	{
		private string m_activity;
		private Stopwatch m_timer;
		private int m_minimumMilliseconds;

		/// <summary>
		/// Measure how long it takes activityName to run, reporting when over.
		/// </summary>
		public StopClock(string activityName) : this(activityName, 0)
		{
		}

		/// <summary>
		/// Measure how long it takes activityName to run, not bothering to report if less than
		/// minimumMilliseconds.
		/// </summary>
		public StopClock(string activityName, int minimumMilliseconds)
		{
			m_activity = activityName;
			m_minimumMilliseconds = minimumMilliseconds;
			m_timer = new Stopwatch();
			m_timer.Start();
		}

		/// <summary/>
		protected override void DisposeManagedResources()
		{
			m_timer.Stop();
			var elapsed = m_timer.ElapsedMilliseconds;
			if (elapsed >= m_minimumMilliseconds)
				Debug.WriteLine(String.Format("StopClock: Running {0} took {1} ms.", m_activity,
					elapsed));
			base.DisposeManagedResources();
		}
	}
}