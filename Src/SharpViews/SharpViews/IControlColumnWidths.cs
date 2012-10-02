using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// This interface may be implemented by creators of RowBox to control column widths.
	/// You may also use one of the default implementations (list here).
	///		- FixedColumnWidths just stores an array of column widths (and raises an event when they are changed)
	///		- one day: ProportionalColumnWidths stores a set of fractions and assigns each column that fraction of the available width.
	///		- later still: maybe a smart object that can give some columns fixed or minimum widths and distribute the rest of the space.
	/// </summary>
	public interface IControlColumnWidths
	{
		int[] ColumnWidths(int ncols, LayoutInfo layoutInfo);
		// event ColumnWidthsChanged
	}
}
