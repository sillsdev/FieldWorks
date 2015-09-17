// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ChartLocation.cs
// Responsibility: MartinG
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.FDO;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An object that keeps track of a chart cell location by row and column.
	/// Often used as a parameter object.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ChartLocation
	{
		private readonly int m_cellColumnIndex;
		private readonly IConstChartRow m_cellRow;

		public ChartLocation(IConstChartRow row, int icol)
		{
			m_cellColumnIndex = icol;
			m_cellRow = row;
		}

		/// <summary>
		/// Get Chart location (vertical) as ChartRow.
		/// </summary>
		public IConstChartRow Row
		{
			get { return m_cellRow; }
		}

		/// <summary>
		/// Get Chart location (vertical) as the hvo of the row.
		/// </summary>
		public int HvoRow
		{
			get { return m_cellRow.Hvo; }
		}

		/// <summary>
		/// Get Chart location (horizontal) as column index.
		/// </summary>
		public int ColIndex
		{
			get { return m_cellColumnIndex; }
		}

		/// <summary>
		/// Returns true if the row is not null and the column index is not negative.
		/// </summary>
		public bool IsValidLocation
		{
			get { return !(Row == null || ColIndex < 0); }
		}

		/// <summary>
		/// A chart location is the same as another if its row Hvo and column index are the same.
		/// Overriding Equals complained that I didn't override GetHashCode().
		/// </summary>
		/// <param name="obj1"></param>
		/// <returns></returns>
		public bool IsSameLocation(object obj1)
		{
			if (!(obj1 is ChartLocation))
				return false;
			var obj = obj1 as ChartLocation;
			if (IsValidLocation)
			{
				if (!obj.IsValidLocation)
					return false;
				// Both ChartLocation objects are 'valid', check for same location.
				return (this.Row.Hvo == obj.Row.Hvo && this.ColIndex == obj.ColIndex);
			}
			return !obj.IsValidLocation;
			// If both ChartLocation objects are 'invalid', by definition they are the same location
			// even if they are 'invalid' for different reasons.
		}
	}
}
