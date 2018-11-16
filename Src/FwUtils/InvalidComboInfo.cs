// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Stores information returned by the InvalidOpenerCloserCombinations property to
	/// indicate what levels contain the same quotation mark as an opener and a closer.
	/// </summary>
	public class InvalidComboInfo
	{
		/// <summary />
		public readonly int LowerLevel;
		/// <summary />
		public readonly bool LowerLevelIsOpener;
		/// <summary />
		public readonly int UpperLevel;
		/// <summary />
		public readonly bool UpperLevelIsOpener;
		/// <summary />
		public readonly string QMark;

		internal InvalidComboInfo(int level1, bool level1IsOpener, int level2, bool level2IsOpener, string qmark)
		{
			LowerLevel = Math.Min(level1, level2);
			LowerLevelIsOpener = (LowerLevel == level1 ? level1IsOpener : level2IsOpener);
			UpperLevel = Math.Max(level1, level2);
			UpperLevelIsOpener = (UpperLevel == level1 ? level1IsOpener : level2IsOpener);
			QMark = qmark;
		}
	}
}