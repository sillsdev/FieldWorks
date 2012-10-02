// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChartLocation.cs
// Responsibility: MartinG
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Discourse
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An object that keeps track of a chart cell location by row and column.
	/// Often used as a parameter object.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ChartLocation
	{
		private int m_cellColumnIndex;
		private ICmIndirectAnnotation m_cellRow;

		public ChartLocation(int icol, ICmIndirectAnnotation row)
		{
			m_cellColumnIndex = icol;
			m_cellRow = row;
		}

		/// <summary>
		/// Get Chart location (vertical) as row annotation.
		/// </summary>
		public ICmIndirectAnnotation RowAnn
		{
			get { return m_cellRow; }
		}

		/// <summary>
		/// Get Chart location (vertical) as the hvo of the row annotation.
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
		/// Returns true if the row annotation is not null and the column index is not negative.
		/// </summary>
		public bool IsValidLocation
		{
			get { return !(RowAnn == null || ColIndex < 0); }
		}

		/// <summary>
		/// A chart location is the same as another if its row Hvo and column index are the same.
		/// Overriding Equals complained that I didn't override GetHashCode().
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool IsSameLocation(object obj1)
		{
			if (!(obj1 is ChartLocation))
				return false;
			ChartLocation obj = obj1 as ChartLocation;
			if (this.IsValidLocation)
			{
				if (!obj.IsValidLocation)
					return false;
				// Both ChartLocation objects are 'valid', check for same location.
				return (this.RowAnn.Hvo == obj.RowAnn.Hvo && this.ColIndex == obj.ColIndex);
			}
			else
			{
				if (obj.IsValidLocation)
					return false;
				// Both ChartLocation objects are 'invalid', by definition they are the same location
				// even if they are 'invalid' for different reasons.
				return true;
			}
		}
	}
}
