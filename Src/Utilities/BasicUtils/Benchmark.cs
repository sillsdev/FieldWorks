#define PROFILING
// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Benchmark.cs
// Responsibility: BobA
//
// <remarks>
// Handy benchmarking util
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIL.Utils
{
#if DEBUG
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Benchmark
	{
		private static Dictionary<string, long> counters = new Dictionary<string, long>();
		private static Dictionary<string, DateTime> timers = new Dictionary<string, DateTime>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		public static void BeginTimedTask(string key)
		{
			DateTime time = DateTime.Now;
			while (time == DateTime.Now)
			{
			}

			timers[key] = DateTime.Now;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int EndTimedTask(string key)
		{
			int returnVal = (DateTime.Now - timers[key]).Milliseconds;
			timers.Remove(key);
			return returnVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		public static void IncrementCounter(string key)
		{
			if (!counters.ContainsKey(key))
			{
				counters.Add(key, 1);
			}
			else
			{
				counters[key] = counters[key] + 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static long GetCounter(string key)
		{
			return counters[key];
		}
	}

#endif
#if PROFILING
	/// <summary>
	/// Use like this:
	/// try
	/// {
	/// #if PROFILING
	///		TimeRecorder.Begin("BlockName");
	/// #endif
	///		...
	///	}
	///	finally
	///	{
	/// #if PROFILING
	///		TimeRecorder.End("BlockName");
	/// #endif
	///	}
	///
	///	(If there are no other exists from the relevant code, you can just use begin and end.)
	///	...somewhere it's appropriate to make the report
	///	TimeRecorder.Report();
	/// </summary>
	public class TimeRecorder
	{
		static Dictionary<string, TimeVal> s_dict = new Dictionary<string, TimeVal>();
		static string s_blockname = "";

		class TimeVal
		{
			public int start;
			public int duration = 0;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="blockname"></param>
		public static void Begin(string blockname)
		{

			s_blockname += "." + blockname;
			if (!s_dict.ContainsKey(s_blockname))
			{
				s_dict[s_blockname] = new TimeVal();
			}
			TimeVal tv = s_dict[s_blockname];
			tv.start = Environment.TickCount;
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="blockname"></param>
		public static void End(string blockname)
		{
			int end = Environment.TickCount; // exclude lookup from time.
			if (blockname != s_blockname.Substring(s_blockname.Length - blockname.Length, blockname.Length))
			{
				Debug.WriteLine("unmatched end for block " + blockname);
			}
			if (s_dict.ContainsKey(s_blockname))
			{
				TimeVal tv = s_dict[s_blockname];
				tv.duration += end - tv.start;
			}
			else
			{
				Debug.WriteLine("missing begin for block " + blockname);
			}
			s_blockname = s_blockname.Substring(0, s_blockname.Length - blockname.Length - 1);
		}

		/// <summary>
		/// output the results of the time durations.
		/// </summary>
		public static void Report()
		{
			// Can't use StringCollection, because it can't sort.
			List<string> items = new List<string>();
			foreach (KeyValuePair<string, TimeVal> kvp in s_dict)
			{
				items.Add(kvp.Key);
			}
			items.Sort();
			foreach (string key in items)
			{
				Debug.WriteLine(key + ": " + s_dict[key].duration.ToString());
			}
			s_dict.Clear();
			s_blockname = ""; // just to be sure
		}
	}
#endif

}