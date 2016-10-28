// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This base class is used to aggregate the shared methods for formatted string classes,
	/// such as <see cref="TsString"/> and <see cref="TsStrBldr"/>.
	/// </summary>
	public abstract class TsStrBase
	{
		/// <summary>
		/// Gets the text.
		/// </summary>
		public abstract string Text { get; }

		/// <summary>
		/// Gets the list of runs.
		/// </summary>
		internal abstract IList<TsRun> Runs { get; }

		/// <summary>
		/// Gets the length of the text.
		/// </summary>
		public int Length
		{
			get { return Text == null ? 0 : Text.Length; }
		}

		/// <summary>
		/// Gets the number of runs.
		/// </summary>
		public int RunCount
		{
			get { return Runs.Count; }
		}

		/// <summary>
		/// Gets the run at the specified character offset. If the offset is equal to the length of text,
		/// then the index of the last run is returned.
		/// </summary>
		public int get_RunAt(int ich)
		{
			ThrowIfCharOffsetOutOfRange("ich", ich, Length);

			if (ich == Length)
				return Runs.Count - 1;

			int lower = 0;
			int upper = Runs.Count - 1;
			while (lower <= upper)
			{
				int middle = lower + (upper - lower) / 2;
				if (ich < GetRunIchMin(middle))
					upper = middle - 1;
				else if (ich >= Runs[middle].IchLim)
					lower = middle + 1;
				else
					return middle;
			}

			throw new InvalidOperationException("The TsString does not contain contiguous runs.");
		}

		/// <summary>
		/// Gets the bounds of the specified run.
		/// </summary>
		public void GetBoundsOfRun(int irun, out int ichMin, out int ichLim)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			ichMin = GetRunIchMin(irun);
			ichLim = Runs[irun].IchLim;
		}

		/// <summary>
		/// Fetches the run information at the specified character offset.
		/// </summary>
		public ITsTextProps FetchRunInfoAt(int ich, out TsRunInfo tri)
		{
			ThrowIfCharOffsetOutOfRange("ich", ich, Length);

			return FetchRunInfo(get_RunAt(ich), out tri);
		}

		/// <summary>
		/// Fetches the information for the specified run.
		/// </summary>
		public ITsTextProps FetchRunInfo(int irun, out TsRunInfo tri)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			TsRun run = Runs[irun];
			tri = new TsRunInfo {ichMin = GetRunIchMin(irun), ichLim = run.IchLim, irun = irun};
			return run.TextProps;
		}

		/// <summary>
		/// Gets the text for the specified run.
		/// </summary>
		public string get_RunText(int irun)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			return GetChars(GetRunIchMin(irun), Runs[irun].IchLim);
		}

		/// <summary>
		/// Gets the substring for the specified range.
		/// </summary>
		public string GetChars(int ichMin, int ichLim)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			int len = ichLim - ichMin;
			return len == 0 ? null : Text.Substring(ichMin, len);
		}

		/// <summary>
		/// Fetches the characters for the specified range.
		/// </summary>
		public void FetchChars(int ichMin, int ichLim, ArrayPtr rgch)
		{
			if (rgch.IntPtr == IntPtr.Zero)
				throw new ArgumentNullException("rgch");
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			string str = GetChars(ichMin, ichLim);
			if (str != null)
				MarshalEx.StringToNative(rgch, str.Length, str, true);
		}

		/// <summary>
		/// Gets the text properties at the specified character offset.
		/// </summary>
		public ITsTextProps get_PropertiesAt(int ich)
		{
			ThrowIfCharOffsetOutOfRange("ich", ich, Length);

			return get_Properties(get_RunAt(ich));
		}

		/// <summary>
		/// Gets the text properties for the specified run.
		/// </summary>
		public ITsTextProps get_Properties(int irun)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			return Runs[irun].TextProps;
		}

		/// <summary>
		/// Gets the starting character offset for the run at the specified index.
		/// </summary>
		protected int GetRunIchMin(int irun)
		{
			return irun == 0 ? 0 : Runs[irun - 1].IchLim;
		}

		/// <summary>
		/// Throws an exception if the parameter is null.
		/// </summary>
		protected void ThrowIfParamNull(string paramName, object param)
		{
			if (param == null)
				throw new ArgumentNullException(paramName);
		}

		/// <summary>
		/// Throws an exception if the run index is out of range.
		/// </summary>
		protected void ThrowIfRunIndexOutOfRange(string paramName, int irun)
		{
			if (irun < 0 || irun >= Runs.Count)
				throw new ArgumentOutOfRangeException(paramName);
		}


		/// <summary>
		/// Throws an exception if the character offset is out of range.
		/// </summary>
		protected void ThrowIfCharOffsetOutOfRange(string paramName, int ichMin, int ichLim)
		{
			if (ichMin < 0 || ichMin > ichLim)
				throw new ArgumentOutOfRangeException(paramName);
		}
	}
}
