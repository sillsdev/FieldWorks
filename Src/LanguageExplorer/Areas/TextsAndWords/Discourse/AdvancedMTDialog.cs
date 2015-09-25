// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// A dialog class for marking SOME of the text in a CChart cell as pre/postposed
	/// from the same or different rows of the chart. The logic is in a separate class.
	/// </summary>
	public partial class AdvancedMTDialog : Form
	{
		private AdvancedMTDialogLogic m_AMTDLogic;
		private HelpProvider helpProvider;

		internal AdvancedMTDialog(FdoCache cache, bool fPrepose, CChartSentenceElements ccSentElem, IHelpTopicProvider helpTopicProvidor)
		{
			InitializeComponent();

			SuspendLayout();

			m_helpTopicProvider = helpTopicProvidor;
			if (m_helpTopicProvider != null)
			{
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
				helpProvider.SetShowHelp(this, true);
			}
			m_AMTDLogic = new AdvancedMTDialogLogic(cache, fPrepose, ccSentElem);
			m_bottomStuff.SuspendLayout();
			m_bottomStuff.Controls.AddRange(new Control[] { m_AMTDLogic.DlgRibbon });

			m_bottomStuff.ResumeLayout();

			// Setup localized dialog
			SetCaption(fPrepose ? LanguageExplorerResources.ksAdvDlgPreposeCaption : LanguageExplorerResources.ksAdvDlgPostposeCaption);
			SetMainText(fPrepose ? LanguageExplorerResources.ksAdvDlgMainPreText : LanguageExplorerResources.ksAdvDlgMainPostText);
			SetPartialText(fPrepose ? LanguageExplorerResources.ksAdvDlgPartialPre : LanguageExplorerResources.ksAdvDlgPartialPost);

			ResumeLayout();

			InitLogicAndDialog();
		}

		#region LabelSettingMethods

		/// <summary>
		/// Sets the dialog box caption
		/// </summary>
		/// <param name="label"></param>
		private void SetCaption(string label)
		{
			Text = label;
		}

		/// <summary>
		/// Sets the main text of the dialog box.
		/// </summary>
		/// <param name="label"></param>
		private void SetMainText(string label)
		{
			m_mainText.Text = label;
		}

		/// <summary>
		/// Sets the second text box of the dialog box (explains choosing part of a cell for moved text).
		/// </summary>
		/// <param name="label"></param>
		private void SetPartialText(string label)
		{
			m_partialText.Text = label;
		}

		#endregion

		internal void InitLogicAndDialog()
		{
			m_AMTDLogic.Init();
			SetRows(DlgLogic.SentElem.EligibleRows);
			// Preselect the row closest to sender
			if (DlgLogic.Prepose)
				SelectedRow = m_rowsCombo.Items[0] as RowMenuItem;
			else
				SelectedRow = m_rowsCombo.Items[DlgLogic.SentElem.EligibleRows.Length - 1] as RowMenuItem;
			SetColumns(DlgLogic.SentElem.ComboCols);
		}

		internal void SetRows(IConstChartRow[] items)
		{
			// Convert ConstChartRows to RowMenuItems
			var rows = new RowMenuItem[items.Length];
			for (var i = 0; i < items.Length; i++)
				rows[i] = new RowMenuItem(items[i]);
			m_rowsCombo.Items.Clear();
			m_rowsCombo.Items.AddRange(rows);
		}

		internal AnalysisOccurrence[] SelectedOccurrences { get; set; }

		/// <summary>
		/// Row selected by user in combobox
		/// </summary>
		internal RowMenuItem SelectedRow
		{
			get { return (RowMenuItem)m_rowsCombo.SelectedItem; }
			set { m_rowsCombo.SelectedItem = value; }
		}

		/// <summary>
		/// Sets the dialog combobox for columns according to what the logic filled in the parameter object.
		/// Also selects the first column as selected initially, since we have no idea what might be wanted.
		/// </summary>
		/// <param name="items"></param>
		internal void SetColumns(ColumnMenuItem[] items)
		{
			m_columnsCombo.Items.Clear();
			if (items.Length > 0)
			{
				m_columnsCombo.Items.AddRange(items);
				SelectedColumn = items[0]; // No way of knowing which column might be wanted, select the first.
			}
		}

		// Column selected by user in combobox
		internal ColumnMenuItem SelectedColumn
		{
			get { return (ColumnMenuItem)m_columnsCombo.SelectedItem; }
			set { m_columnsCombo.SelectedItem = value; }
		}

		internal AdvancedMTDialogLogic DlgLogic
		{
			get { return m_AMTDLogic; }
		}

		private void m_OkButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			// Must save selected occurrences before closing or the rootbox will close on them!
			SelectedOccurrences = DlgLogic.DlgRibbon.SelectedOccurrences;
			DlgLogic.SetAffectedWordGroups(SelectedOccurrences);
			Close();
		}

		private void m_cancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		/// <summary>
		/// Display AnotherClause dialog help here.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		private void m_rowsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Figure out if some column choices need to be changed.
			DlgLogic.CollectColumnsToCombo(DlgLogic.GetColumnChoices(SelectedRow.Row));
			SetColumns(DlgLogic.SentElem.ComboCols);
			m_columnsCombo.Refresh();
		}
	}

	class AdvancedMTDialogLogic: IDisposable
	{
		private readonly CChartSentenceElements m_ccSentElem;
		private readonly DialogInterlinRibbon m_ribbon;
		private readonly FdoCache m_cache;
		private readonly bool m_fPrepose;
		private IConstChartWordGroup m_wordGroup;

		public AdvancedMTDialogLogic(FdoCache cache, bool fPrepose, CChartSentenceElements ccSentElem)
		{
			m_cache = cache;
			m_fPrepose = fPrepose;
			m_ccSentElem = ccSentElem;

			m_ribbon = new DialogInterlinRibbon(Cache) {Dock = DockStyle.Fill};
		}

		#region Disposable stuff
#if DEBUG
		~AdvancedMTDialogLogic()
		{
			Dispose(false);
		}
#endif

		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_ribbon.Dispose();
			}

			IsDisposed = true;
		}
		#endregion

		private void CacheWordGroupOccurrencesForRibbon()
		{
			var words = m_wordGroup.GetOccurrences();
			m_ribbon.CacheRibbonItems(words);
		}

		#region GetProperties

		/// <summary>
		/// Gets the FDO cache.
		/// </summary>
		public FdoCache Cache
		{
			get { return m_cache; }
		}

		///// <summary>
		///// Gets the dialog Ribbon.
		///// </summary>
		public DialogInterlinRibbon DlgRibbon
		{
			get { return m_ribbon; }
		}

		/// <summary>
		/// Gets the Dialog parameter object.
		/// </summary>
		internal CChartSentenceElements SentElem
		{
			get { return m_ccSentElem; }
		}

		/// <summary>
		/// Gets the Prepose/Postpose flag.
		/// </summary>
		internal bool Prepose
		{
			get { return m_fPrepose; }
		}

		#endregion

		internal void Init()
		{
			Debug.Assert(SentElem != null && SentElem.AffectedWordGroups.Count > 0, "No WordGroup Hvo set.");

			// Collect eligible rows
			var crows = CollectRowsToCombo();

			// Next collect all columns and create ColumnMenuItem List for ComboBox
			if (crows == 1)
				CollectColumnsToCombo(GetColumnChoices(SentElem.GetOriginRow));
			else
				CollectAllColumnsToCombo();

			// TODO GordonM: Eventually we want to check and see if AffectedWordGroups has more than one
			// and put them all in the Ribbon!
			// Review: Perhaps we need to build a temporary/dummy WordGroup with all the occurrences of
			// the AffectedWordGroups in it for dialog Ribbon display purposes.
			m_wordGroup = SentElem.AffectedWordGroups[0];
			SetRibbon();
		}

		/// <summary>
		/// Collect the eligible rows (as passed in by the Chart Logic) into the ComboBox items array.
		/// Returns the number of rows collected.
		/// </summary>
		/// <returns></returns>
		private int CollectRowsToCombo()
		{
			Debug.Assert(SentElem.EligibleRows != null);
			var crows = SentElem.EligibleRows.Length;
			var rows = new RowMenuItem[crows];
			for (var i = 0; i < crows; i++)
				rows[i] = new RowMenuItem(SentElem.EligibleRows[i]);
			SentElem.ComboRows = rows;
			return crows;
		}

		/// <summary>
		/// Collect all columns from hvo array and create List of ColumnMenuItems for ComboBox.
		/// </summary>
		private void CollectAllColumnsToCombo()
		{
			CollectColumnsToCombo(SentElem.AllChartCols);
		}

		/// <summary>
		/// Create a List of ColumnMenuItems for ComboBox based on an array of column hvos.
		/// </summary>
		/// <param name="colArray"></param>
		internal void CollectColumnsToCombo(ICmPossibility[] colArray)
		{
			var ccols = colArray.Length;
			var cols = new ColumnMenuItem[ccols];
			for (var i = 0; i < ccols; i++)
				cols[i] = new ColumnMenuItem(colArray[i]);
			SentElem.ComboCols = cols;
		}

		/// <summary>
		/// Figure out what columns should be available, given a selected row.
		/// </summary>
		/// <returns></returns>
		internal ICmPossibility[] GetColumnChoices(IConstChartRow row)
		{
			if (row.Hvo != SentElem.GetOriginRow.Hvo)
			{
				CollectAllColumnsToCombo();
				return SentElem.AllChartCols;
			}
			var ccols = SentElem.AllChartCols.Length;
			var icurCol = SentElem.GetOriginColumnIndex;
			if (Prepose)
			{
				//   Collect columns following clicked one
				ccols = ccols - icurCol - 1;
				icurCol++;
			}
			else
			{
				//   Collect columns preceding clicked one
				ccols = icurCol;
				icurCol = 0;
			}
			var result = new ICmPossibility[Math.Max(0, ccols)];
			for (var i = 0; i < ccols; i++)
			{
				result[i] = SentElem.AllChartCols[i + icurCol];
			}
			return result;
		}

		/// <summary>
		/// Sets the text ribbon to display the occurrences in the current WordGroup.
		/// </summary>
		internal void SetRibbon()
		{
			// TODO GordonM: make it work for an array of WordGroups?
			CacheWordGroupOccurrencesForRibbon();
			m_ribbon.SetRoot(m_wordGroup.Hvo);
		}

		/// <summary>
		/// Takes the list of AffectedWordGroups fed into the dialog and the list of user selected words
		/// and updates the AffectedWordGroups list in the parameter object
		/// </summary>
		internal void SetAffectedWordGroups(AnalysisOccurrence[] selectedWords)
		{
			var selWords = new Set<AnalysisOccurrence>();
			selWords.AddRange(selectedWords);
			var affectedWordGrps = (from wordGroup in SentElem.AffectedWordGroups
									let wordGrpPoints = wordGroup.GetOccurrences()
									where selWords.Intersection(wordGrpPoints).Count > 0
									select wordGroup).ToList();
			SentElem.AffectedWordGroups = affectedWordGrps;
		}
	}

	/// <summary>
	/// Mostly a parameter object for passing CChart cell/sentence info to the AdvancedMTDialog
	/// and its attendant Logic. Also used to return responses to CChartLogic for it to process.
	/// </summary>
	internal class CChartSentenceElements
	{

		#region MemberData

		ChartLocation m_clickedCell; // The cell the user clicked
		RowMenuItem[] m_rows; // To build the dialog drop down list of rows.
		ColumnMenuItem[] m_cols; // To build the dialog drop down list of columns.
		IConstChartRow[] m_eligibleRows; // The eligible rows to put in the dialog's drop down list.
		ICmPossibility[] m_eligibleColumns; // Array of columns to be put into the dialog drop down list.
		ICmPossibility[] m_allColumns; // The complete array of columns in the chart

		#endregion

		public CChartSentenceElements(ChartLocation cellClicked, IConstChartRow[] eligRows, ICmPossibility[] eligColumns)
		{
			m_clickedCell = cellClicked;
			AffectedWordGroups = new List<IConstChartWordGroup>();
			m_eligibleRows = eligRows;
			m_eligibleColumns = eligColumns;
			m_allColumns = m_eligibleColumns;
			m_rows = null;
			m_cols = null;
		}

		#region Properties

		/// <summary>
		/// Returns the object holding the ChartRow and column(hvo) of the chart cell that was clicked.
		/// </summary>
		public ChartLocation OriginCell
		{
			get { return m_clickedCell; }
			set { m_clickedCell = value; }
		}

		/// <summary>
		/// Returns the row of the chart cell that was clicked.
		/// </summary>
		public IConstChartRow GetOriginRow
		{
			get { return OriginCell.Row; }
		}

		/// <summary>
		/// Returns the index of the column of the chart cell that was clicked.
		/// </summary>
		public int GetOriginColumnIndex
		{
			get { return OriginCell.ColIndex; }
		}

		/// <summary>
		/// The complete array of column hvos in the chart.
		/// </summary>
		public ICmPossibility[] AllChartCols
		{
			get { return m_allColumns; }
			set { m_allColumns = value; }
		}

		/// <summary>
		/// Holds the current values for the column drop down list of the dialog. (Built in AMTDLogic.)
		/// </summary>
		public ColumnMenuItem[] ComboCols
		{
			get { return m_cols; }
			set { m_cols = value; }
		}

		/// <summary>
		/// Holds the current values for the row drop down list of the dialog. (Built in AMTDLogic.)
		/// </summary>
		public RowMenuItem[] ComboRows
		{
			get { return m_rows; }
			set { m_rows = value; }
		}

		/// <summary>
		/// The eligible rows to be put in the dialog's drop down list.
		/// </summary>
		public IConstChartRow[] EligibleRows
		{
			get { return m_eligibleRows; }
			set { m_eligibleRows = value; }
		}

		/// <summary>
		/// Array of hvos of columns to be put into the dialog drop down list.
		/// Starts out with all the columns in the chart.
		/// </summary>
		public ICmPossibility[] EligibleColumns
		{
			get { return m_eligibleColumns; }
			set { m_eligibleColumns = value; }
		}

		/// <summary>
		/// Starts life as an array of hvos of WordGroups from the clicked cell.
		/// When we return from the dialog, it should be an array of only those WordGroups that
		/// need changing somehow (because some of their contents are now marked as movedText).
		/// </summary>
		public List<IConstChartWordGroup> AffectedWordGroups { get; set; }

		#endregion
	}
}
