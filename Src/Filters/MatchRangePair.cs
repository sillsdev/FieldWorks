// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Class to keep track of the ichMin/ichLim sets resulting from a FindIn() match.
	/// </summary>
	public struct MatchRangePair
	{
		public MatchRangePair(int ichMin, int ichLim)
		{
			IchMin = ichMin;
			IchLim = ichLim;
		}

		public void Reset()
		{
			IchMin = -1;
			IchLim = -1;
		}

		public int IchMin { get; internal set; }

		public int IchLim { get; internal set; }
	}
}