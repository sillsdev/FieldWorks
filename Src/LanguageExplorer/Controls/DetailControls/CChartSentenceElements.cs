// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Mostly a parameter object for passing CChart cell/sentence info to the AdvancedMTDialog
	/// and its attendant Logic. Also used to return responses to CChartLogic for it to process.
	/// </summary>
	internal sealed class CChartSentenceElements
	{
		internal CChartSentenceElements(ChartLocation cellClicked, IConstChartRow[] eligRows, ICmPossibility[] eligColumns)
		{
			OriginCell = cellClicked;
			AffectedWordGroups = new List<IConstChartWordGroup>();
			EligibleRows = eligRows;
			EligibleColumns = eligColumns;
			AllChartCols = EligibleColumns;
			ComboRows = null;
			ComboCols = null;
		}

		#region Properties

		/// <summary>
		/// Returns the object holding the ChartRow and column(hvo) of the chart cell that was clicked.
		/// </summary>
		internal ChartLocation OriginCell { get; set; }

		/// <summary>
		/// Returns the row of the chart cell that was clicked.
		/// </summary>
		internal IConstChartRow GetOriginRow => OriginCell.Row;

		/// <summary>
		/// Returns the index of the column of the chart cell that was clicked.
		/// </summary>
		internal int GetOriginColumnIndex => OriginCell.ColIndex;

		/// <summary>
		/// The complete array of column hvos in the chart.
		/// </summary>
		internal ICmPossibility[] AllChartCols { get; set; }

		/// <summary>
		/// Holds the current values for the column drop down list of the dialog. (Built in AMTDLogic.)
		/// </summary>
		internal ColumnMenuItem[] ComboCols { get; set; }

		/// <summary>
		/// Holds the current values for the row drop down list of the dialog. (Built in AMTDLogic.)
		/// </summary>
		internal RowMenuItem[] ComboRows { get; set; }

		/// <summary>
		/// The eligible rows to be put in the dialog's drop down list.
		/// </summary>
		internal IConstChartRow[] EligibleRows { get; set; }

		/// <summary>
		/// Array of hvos of columns to be put into the dialog drop down list.
		/// Starts out with all the columns in the chart.
		/// </summary>
		internal ICmPossibility[] EligibleColumns { get; set; }

		/// <summary>
		/// Starts life as an array of hvos of WordGroups from the clicked cell.
		/// When we return from the dialog, it should be an array of only those WordGroups that
		/// need changing somehow (because some of their contents are now marked as movedText).
		/// </summary>
		internal List<IConstChartWordGroup> AffectedWordGroups { get; set; }

		#endregion
	}
}