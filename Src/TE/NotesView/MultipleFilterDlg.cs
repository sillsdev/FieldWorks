// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MultipleFilterDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using SIL.CoreImpl;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class MultipleFilterDlg : Form
	{
		private IHelpTopicProvider m_helpTopicProvider;
		private FdoCache m_cache;
		private IScripture m_scr;
		private ICmFilter m_filter;
		private ICmCellFactory m_cellFactory;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MultipleFilterDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private MultipleFilterDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MultipleFilterDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MultipleFilterDlg(FdoCache cache, IHelpTopicProvider helpTopicProviderp,
			ICmFilter filter) : this()
		{
			m_helpTopicProvider = helpTopicProviderp;
			m_cache = cache;
			m_cellFactory = m_cache.ServiceLocator.GetInstance<ICmCellFactory>();
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_filter = filter;

			// Initialize the enabled status of the group boxes.
			chkStatus_CheckedChanged(null, null);
			chkType_CheckedChanged(null, null);
			chkScrRange_CheckedChanged(null, null);

			// Initialize the beginning and ending default Scripture references.
			int firstBook = 1;
			int lastBook = BCVRef.LastBook;
			if (m_scr.ScriptureBooksOS.Count > 0)
			{
				firstBook = m_scr.ScriptureBooksOS[0].CanonicalNum;
				lastBook = m_scr.ScriptureBooksOS[m_scr.ScriptureBooksOS.Count - 1].CanonicalNum;
			}

			scrBookFrom.Initialize(new ScrReference(firstBook,
				1, 1, m_scr.Versification),	m_scr, false);

			scrBookTo.Initialize(new ScrReference(lastBook,
				1, 0, m_scr.Versification).LastReferenceForBook, m_scr, false);

			// Update the controls from the filter in the database.
			InitializeFromFilter();
			chkCategory.Checked = tvCatagories.Load(m_cache, m_filter, null);
			chkCategory_CheckedChanged(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the form's controls from the settings in the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeFromFilter()
		{
			if (m_filter == null || m_filter.RowsOS.Count == 0 || m_filter.RowsOS[0].CellsOS.Count == 0)
				return;

			// Get the pairs of class ids and flids.
			string[] pairs = m_filter.ColumnInfo.Split('|');
			Debug.Assert(m_filter.RowsOS[0].CellsOS.Count == pairs.Length);

			for (int i = 0; i < pairs.Length; i++)
			{
				ICmCell cell = m_filter.RowsOS[0].CellsOS[i];

				// Get the flid for this cell.
				string[] pair = pairs[i].Split(',');
				int flid = 0;
				int.TryParse(pair[1], out flid);

				switch (flid)
				{
					case ScrScriptureNoteTags.kflidResolutionStatus:
						chkStatus.Checked = true;
						cell.ParseIntegerMatchCriteria();
						rbResolved.Checked = (cell.MatchValue == 1);
						rbUnresolved.Checked = (cell.MatchValue == 0);
						break;

					case CmAnnotationTags.kflidAnnotationType:
						chkType.Checked = true;
						cell.ParseObjectMatchCriteria();
						Guid guid = TsStringUtils.GetGuidFromRun(cell.Contents, 1);
						rbConsultant.Checked = (guid == CmAnnotationDefnTags.kguidAnnConsultantNote);
						rbTranslator.Checked = (guid == CmAnnotationDefnTags.kguidAnnTranslatorNote);
						break;

					case CmBaseAnnotationTags.kflidBeginRef:
						chkScrRange.Checked = true;
						cell.ParseIntegerMatchCriteria();
						ScrReference scrRef = new ScrReference(cell.MatchValue, m_scr.Versification);
						// If the reference was adjusted to 0:0 to include notes in the title and
						// introduction, adjust it back to 1:1 so we don't confuse the user.
						if (scrRef.Chapter == 0)
							scrRef.Chapter = 1;
						if (scrRef.Verse == 0)
							scrRef.Verse = 1;

						if (cell.ComparisonType == ComparisonTypes.kGreaterThanEqual)
							scrBookFrom.ScReference = scrRef;
						else
							scrBookTo.ScReference = scrRef;
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnOk control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, EventArgs e)
		{
			using (NonUndoableUnitOfWorkHelper undoHelper = new NonUndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor))
			{
				ICmCell cell;
				string fmtClassFlid = ScrScriptureNoteTags.kClassId + ",{0}|";
				StringBuilder bldr = new StringBuilder();
				m_filter.RowsOS[0].CellsOS.Clear();

				if (chkStatus.Checked)
				{
					bldr.AppendFormat(fmtClassFlid, ScrScriptureNoteTags.kflidResolutionStatus);

					cell = m_cellFactory.Create();
					m_filter.RowsOS[0].CellsOS.Add(cell);
					int value = (rbResolved.Checked ? 1 : 0);
					cell.SetIntegerMatchCriteria(ComparisonTypes.kEquals, value);
				}

				if (chkType.Checked)
				{
					bldr.AppendFormat(fmtClassFlid, CmAnnotationTags.kflidAnnotationType);

					cell = m_cellFactory.Create();
					m_filter.RowsOS[0].CellsOS.Add(cell);
					ICmAnnotationDefn type =
						m_cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(
						rbConsultant.Checked ? CmAnnotationDefnTags.kguidAnnConsultantNote :
						CmAnnotationDefnTags.kguidAnnTranslatorNote);
					cell.SetObjectMatchCriteria(type, false, false);
				}

				if (chkScrRange.Checked)
				{
					bldr.AppendFormat(fmtClassFlid, CmBaseAnnotationTags.kflidBeginRef);
					bldr.AppendFormat(fmtClassFlid, CmBaseAnnotationTags.kflidBeginRef);

					ScrReference startRef = (scrBookFrom.ScReference.Chapter == 1 && scrBookFrom.ScReference.Verse == 1) ?
						new ScrReference(scrBookFrom.ScReference.Book, 0, 0, m_scr.Versification) :
						scrBookFrom.ScReference;

					cell = m_cellFactory.Create();
					m_filter.RowsOS[0].CellsOS.Add(cell);
					cell.SetIntegerMatchCriteria(ComparisonTypes.kGreaterThanEqual, startRef);
					cell = m_cellFactory.Create();
					m_filter.RowsOS[0].CellsOS.Add(cell);
					cell.SetIntegerMatchCriteria(ComparisonTypes.kLessThanEqual, scrBookTo.ScReference);
				}

				m_filter.ColumnInfo = bldr.ToString().TrimEnd('|');

				if (chkCategory.Checked)
					tvCatagories.UpdateFilter(m_filter);

				undoHelper.RollBack = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the m_chkType control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkType_CheckedChanged(object sender, EventArgs e)
		{
			grpType.Enabled = chkType.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the m_chkStatus control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkStatus_CheckedChanged(object sender, EventArgs e)
		{
			grpStatus.Enabled = chkStatus.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the m_chkCategory control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkCategory_CheckedChanged(object sender, EventArgs e)
		{
			grpCategory.Enabled = chkCategory.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the chkScrRange control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkScrRange_CheckedChanged(object sender, EventArgs e)
		{
			grpScrRange.Enabled = chkScrRange.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpScrNoteMultiCriteriaFilterChooser");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the event when the Scripture passage reference changes in the scrBookFrom
		/// control.
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		private void scrBookFrom_PassageChanged(ScrReference newReference)
		{
			if (newReference > scrBookTo.ScReference)
				scrBookTo.ScReference = newReference;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the event when the Scripture passage reference changes in the scrBookTo
		/// control.
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		private void scrBookTo_PassageChanged(ScrReference newReference)
		{
			if (newReference != ScrReference.Empty && newReference < scrBookFrom.ScReference)
				scrBookFrom.ScReference = newReference;
		}
	}
}