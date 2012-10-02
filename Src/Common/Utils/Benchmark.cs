#if DEBUG
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
// Last reviewed:
//
// <remarks>
// Handy benchmarking util
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.Utils
{
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
}
#endif