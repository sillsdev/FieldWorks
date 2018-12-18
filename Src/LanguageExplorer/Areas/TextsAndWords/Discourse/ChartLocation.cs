// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// An object that keeps track of a chart cell location by row and column.
	/// Often used as a parameter object.
	/// </summary>
	public class ChartLocation
	{
		public ChartLocation(IConstChartRow row, int icol)
		{
			ColIndex = icol;
			Row = row;
		}

		/// <summary>
		/// Get Chart location (vertical) as ChartRow.
		/// </summary>
		public IConstChartRow Row { get; }

		/// <summary>
		/// Get Chart location (vertical) as the hvo of the row.
		/// </summary>
		public int HvoRow => Row.Hvo;

		/// <summary>
		/// Get Chart location (horizontal) as column index.
		/// </summary>
		public int ColIndex { get; }

		/// <summary>
		/// Returns true if the row is not null and the column index is not negative.
		/// </summary>
		public bool IsValidLocation => !(Row == null || ColIndex < 0);

		/// <summary>
		/// A chart location is the same as another if its row Hvo and column index are the same.
		/// Overriding Equals complained that I didn't override GetHashCode().
		/// </summary>
		public bool IsSameLocation(object obj1)
		{
			if (!(obj1 is ChartLocation))
			{
				return false;
			}
			// If both ChartLocation objects are 'invalid', by definition they are the same location
			// even if they are 'invalid' for different reasons.
			var obj = (ChartLocation)obj1;
			if (!IsValidLocation)
			{
				return !obj.IsValidLocation;
			}
			if (!obj.IsValidLocation)
			{
				return false;
			}
			// Both ChartLocation objects are 'valid', check for same location.
			return Row.Hvo == obj.Row.Hvo && ColIndex == obj.ColIndex;
		}
	}
}