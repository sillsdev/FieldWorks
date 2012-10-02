using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// A dialog class for marking SOME of the text in a CChart cell as pre/postposed
	/// from the same or different rows of the chart. The logic is in a separate class.
	/// </summary>
	public partial class AdvancedMTDialog : Form
	{
		AdvancedMTDialogLogic m_AMTDLogic;
		int[] m_hvoSelectedWfics;

		internal AdvancedMTDialog(FdoCache cache, bool fPrepose, CChartSentenceElements ccSentElem)
		{
			InitializeComponent();

			this.SuspendLayout();

			m_AMTDLogic = new AdvancedMTDialogLogic(cache, fPrepose, ccSentElem);
			m_bottomStuff.SuspendLayout();
			m_bottomStuff.Controls.AddRange(new Control[] { m_AMTDLogic.DlgRibbon });
			m_bottomStuff.ResumeLayout();

			// Setup localized dialog
			SetCaption(fPrepose ? DiscourseStrings.ksAdvDlgPreposeCaption : DiscourseStrings.ksAdvDlgPostposeCaption);
			SetMainText(fPrepose ? DiscourseStrings.ksAdvDlgMainPreText : DiscourseStrings.ksAdvDlgMainPostText);
			SetPartialText(fPrepose ? DiscourseStrings.ksAdvDlgPartialPre : DiscourseStrings.ksAdvDlgPartialPost);

			this.ResumeLayout();

			InitLogicAndDialog();
		}

		#region LabelSettingMethods

		/// <summary>
		/// Sets the dialog box caption
		/// </summary>
		/// <param name="label"></param>
		private void SetCaption(string label)
		{
			this.Text = label;
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

		internal void SetRows(ICmIndirectAnnotation[] items)
		{
			// Convert ICmIndirectAnnotations to RowMenuItems
			RowMenuItem[] rows = new RowMenuItem[items.Length];
			for (int i = 0; i < items.Length; i++)
				rows[i] = new RowMenuItem(items[i]);
			m_rowsCombo.Items.Clear();
			m_rowsCombo.Items.AddRange(rows);
		}

		internal int[] SelectedWfics
		{
			get { return m_hvoSelectedWfics; }
			set { m_hvoSelectedWfics = value; }
		}

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
			// Must save selected Wfics before closing or the rootbox will close on them!
			SelectedWfics = DlgLogic.DlgRibbon.SelectedAnnotations;
			DlgLogic.SetAffectedCcas(SelectedWfics);
			this.Close();
		}

		private void m_cancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}

		/// <summary>
		/// Display AnotherClause dialog help here.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_helpButton_Click(object sender, EventArgs e)
		{
			if (this.Text.Equals(DiscourseStrings.ksAdvDlgPreposeCaption))
				ShowHelp.ShowHelpTopic(FwApp.App, "khtpAnotherClausePrepose");
			else
				ShowHelp.ShowHelpTopic(FwApp.App, "khtpAnotherClausePostposed");
		}

		private void m_rowsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Figure out if some column choices need to be changed.
			DlgLogic.CollectColumnsToCombo(DlgLogic.GetColumnChoices(SelectedRow.Row));
			SetColumns(DlgLogic.SentElem.ComboCols);
			m_columnsCombo.Refresh();
		}
	}

	class AdvancedMTDialogLogic
	{
		CChartSentenceElements m_ccSentElem;
		DialogInterlinRibbon m_ribbon;
		FdoCache m_cache;
		bool m_fPrepose;
		int m_hvoCca;

		public AdvancedMTDialogLogic(FdoCache cache, bool fPrepose, CChartSentenceElements ccSentElem)
		{
			m_cache = cache;
			m_fPrepose = fPrepose;
			m_ccSentElem = ccSentElem;

			m_ribbon = new DialogInterlinRibbon(Cache);
			m_ribbon.Dock = DockStyle.Fill; // fills the 'bottom stuff' panel
		}

		private void CacheCcaWficsForRibbon()
		{
			int[] ccaAppliesTo = Cache.GetVectorProperty(m_hvoCca, kflidAppliesTo, false);
			Cache.VwCacheDaAccessor.CacheVecProp(m_hvoCca, DlgRibbon.AnnotationListId,
				ccaAppliesTo, ccaAppliesTo.Length);
		}

		#region GetProperties

		/// <summary>
		/// Gets the FDO cache.
		/// </summary>
		public FdoCache Cache
		{
			get { return m_cache; }
		}

		/// <summary>
		/// Gets the dialog Ribbon.
		/// </summary>
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

		private const int kflidAppliesTo = (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo;

		internal void Init()
		{
			Debug.Assert(SentElem != null && SentElem.AffectedCcas.Count > 0, "No CCA Hvo set.");

			// Collect eligible rows
			int crows = CollectRowsToCombo();

			// Next collect all columns and create ColumnMenuItem List for ComboBox
			if (crows == 1)
				CollectColumnsToCombo(GetColumnChoices(SentElem.GetOriginRow));
			else
				CollectAllColumnsToCombo();

			// TODO GordonM: Eventually we want to check and see if AffectedCcas has more than one
			// and put them all in the Ribbon!
			// Review: Perhaps we need to build a temporary/dummy CCA with all the wfics of the AffectedCcas in it
			// for dialog Ribbon display purposes.
			m_hvoCca = SentElem.AffectedCcas[0];
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
			int crows = SentElem.EligibleRows.Length;
			RowMenuItem[] rows = new RowMenuItem[crows];
			for (int i = 0; i < crows; i++)
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
		/// <param name="hvoArray"></param>
		internal void CollectColumnsToCombo(int[] hvoArray)
		{
			int ccols = hvoArray.Length;
			ColumnMenuItem[] cols = new ColumnMenuItem[ccols];
			for (int i = 0; i < ccols; i++)
				cols[i] = new ColumnMenuItem(CmPossibility.CreateFromDBObject(Cache,
					hvoArray[i]));
			SentElem.ComboCols = cols;
		}

		/// <summary>
		/// Figure out what columns should be available, given a selected row.
		/// </summary>
		/// <returns></returns>
		internal int[] GetColumnChoices(ICmIndirectAnnotation row)
		{
			if (row.Hvo != SentElem.GetOriginRow.Hvo)
			{
				CollectAllColumnsToCombo();
				return SentElem.AllChartCols;
			}
			int ccols = SentElem.AllChartCols.Length;
			int icurCol = SentElem.GetOriginColumnIndex;
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
			int[] result = new int[Math.Max(0, ccols)];
			for (int i = 0; i < ccols; i++)
			{
				result[i] = SentElem.AllChartCols[i + icurCol];
			}
			return result;
		}

		/// <summary>
		/// Sets the text ribbon to display the wfics in the current CCA.
		/// </summary>
		/// <param name="hvoCca"></param>
		internal void SetRibbon()
		{
			// TODO GordonM: make it work for an array of Ccas?
			CacheCcaWficsForRibbon();
			m_ribbon.SetRoot(m_hvoCca);
		}

		/// <summary>
		/// Takes the list of AffectedCcas fed into the dialog and the list of user selected wfics
		/// and updates the AffectedCcas list in the parameter object
		/// </summary>
		internal void SetAffectedCcas(int[] selectedWfics)
		{
			Set<int> selWfics = new Set<int>();
			selWfics.AddRange(selectedWfics);
			List<int> ccas = new List<int>();
			foreach (int hvoCca in SentElem.AffectedCcas)
			{
				if (selWfics.Intersection(Cache.GetVectorProperty(hvoCca, kflidAppliesTo, false)).Count > 0)
					ccas.Add(hvoCca);
			}
			SentElem.AffectedCcas = ccas;
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
		List<int> m_affectedCcas; // To return to the CCLogic that calls the dialog.
		ICmIndirectAnnotation[] m_eligibleRows; // The eligible rows to put in the dialog's drop down list.
		int[] m_eligibleColumns; // Array of hvos of columns to be put into the dialog drop down list.
		int[] m_allColumns; // The complete array of columns in the chart

		#endregion

		public CChartSentenceElements(ChartLocation cellClicked, ICmIndirectAnnotation[] eligRows, int[] hvoEligColumns)
		{
			m_clickedCell = cellClicked;
			m_affectedCcas = new List<int>();
			m_eligibleRows = eligRows;
			m_eligibleColumns = hvoEligColumns;
			m_allColumns = m_eligibleColumns;
			m_rows = null;
			m_cols = null;
		}

		#region Properties

		/// <summary>
		/// Returns the object holding the row(annotation) and column(hvo) of the chart cell that was clicked.
		/// </summary>
		public ChartLocation OriginCell
		{
			get { return m_clickedCell; }
			set { m_clickedCell = value; }
		}

		/// <summary>
		/// Returns the annotation of the row of the chart cell that was clicked.
		/// </summary>
		public ICmIndirectAnnotation GetOriginRow
		{
			get { return OriginCell.RowAnn; }
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
		public int[] AllChartCols
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
		public ICmIndirectAnnotation[] EligibleRows
		{
			get { return m_eligibleRows; }
			set { m_eligibleRows = value; }
		}

		/// <summary>
		/// Array of hvos of columns to be put into the dialog drop down list.
		/// Starts out with all the columns in the chart.
		/// </summary>
		public int[] EligibleColumns
		{
			get { return m_eligibleColumns; }
			set { m_eligibleColumns = value; }
		}

		/// <summary>
		/// Starts life as an array of hvos of data Ccas from the clicked cell.
		/// When we return from the dialog, it should be an array of only those Ccas that
		/// need changing somehow (because some of their contents are now marked as movedText).
		/// </summary>
		public List<int> AffectedCcas
		{
			get { return m_affectedCcas; }
			set { m_affectedCcas = value; }
		}

		#endregion
	}
}