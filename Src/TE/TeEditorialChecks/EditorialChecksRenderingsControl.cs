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
// File: EditorialChecksRenderingsControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using XCore;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using Microsoft.Win32;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.Controls;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The data grid for the Editorial Checks
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class EditorialChecksRenderingsControl : CheckRenderingControl, IxCoreColleague
	{
		#region Constants
		/// <summary>Column indeces for the values in the datagrid</summary>
		internal const int kRefCol = 0;
		internal const int kTypeCol = 1;
		internal const int kMessageCol = 2;
		internal const int kDetailsCol = 3;
		internal const int kStatusCol = 4;
		#endregion

		#region Data members
		private FilteredScrBooks m_BookFilter;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// When rows are added or removed from the list of errors, then the value of error
		/// passed is CheckingError.Empty. When the status of a specific CheckingError changes,
		/// then error will be a reference to that CheckingError object.
		/// </summary>
		public delegate void CheckErrorsListHandler(object sender, CheckingError error);

		/// <summary>Event raised when errors are added or removed from the
		/// grid or when the status of one of them changes.</summary>
		public event CheckErrorsListHandler ErrorListContentChanged;

		/// <summary>Event raised when the focused reference changes.</summary>
		public event CheckErrorsListHandler ReferenceChanged;

		internal event ValidCharacters.LoadExceptionDelegate ValidCharsLoadException;

		#endregion

		#region Construct and Initialize
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:EditorialChecksRenderingsControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksRenderingsControl(): this(null, null, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:EditorialChecksRenderingsControl"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="bookFilter">The book filter.</param>
		/// <param name="mainWnd">The FwMainWnd that owns this control.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksRenderingsControl(FdoCache cache, FilteredScrBooks bookFilter,
			FwMainWnd mainWnd, IHelpTopicProvider helpTopicProvider) : base(cache, mainWnd)
		{
			InitializeComponent();
			DataGridView = m_dataGridView;
			m_BookFilter = bookFilter;
			m_helpTopicProvider = helpTopicProvider;

			if (m_cache == null)
				return;

			m_Details.Cache = m_cache;
			m_Details.WritingSystemCode = m_cache.DefaultVernWs;

			if (mainWnd != null)
			{
				m_Details.Font = new Font(
					mainWnd.StyleSheet.GetNormalFontFaceName(m_cache, m_cache.DefaultVernWs),
						FontInfo.kDefaultFontSize / 1000);
			}

			m_list = new List<ICheckGridRowObject>();
			m_gridSorter = new CheckGridListSorter(m_list);
			m_gridSorter.AddComparer(m_TypeOfCheck.DataPropertyName, StringComparer.CurrentCulture);
			m_gridSorter.AddComparer(m_Message.DataPropertyName, StringComparer.CurrentCulture);
			m_gridSorter.AddComparer(m_Details.DataPropertyName, m_tsStrComparer);
			m_gridSorter.AddComparer(m_Status.DataPropertyName, new CheckingStatusComparer());
			m_gridSorter.AddComparer(m_Reference.DataPropertyName,
				new ScrReferencePositionComparer(m_cache.LangProject.TranslatedScriptureOA.ScrProjMetaDataProvider, false));

			m_dataGridView.Cache = m_cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}

			components = null;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads all the checking errors into the grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LoadCheckingErrors()
		{
			LoadCheckingErrors(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads into the grid, the checking errors for the specified checks.
		/// </summary>
		/// <param name="selectedCheckIds">The list of checkIds for which to show results.
		/// When this is null, results for all the checks are shown.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadCheckingErrors(List<Guid> selectedCheckIds)
		{
			m_list.Clear();

			// Unsubscribe so we don't get reference changed events (which happens in the
			// grid's RowEnter event delegate) while we are loading the grid.
			m_dataGridView.RowEnter -= m_dataGridView_RowEnter;
			m_dataGridView.RowCount = 0;
			m_dataGridView.ResetFonts();
			m_dataGridView.IsStale = false;

			if (m_BookFilter == null || m_BookFilter.BookIds == null)
				return;

			IFdoOwningSequence<IScrBookAnnotations> booksAnnotations =
				m_cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS;

			foreach (int bookId in m_BookFilter.BookIds)
			{
				IScrBookAnnotations annotations = booksAnnotations[bookId - 1];
				foreach (IScrScriptureNote note in annotations.NotesOS)
				{
					CheckingError error = CheckingError.Create(note);
					if (error != null && (selectedCheckIds == null || selectedCheckIds.Count == 0 ||
						selectedCheckIds.Contains(error.MyNote.AnnotationTypeRA.Guid)))
					{
						// ignore errors in process of being deleted - will be removed next time checks are run.
						if (error.Status != (int)CheckingStatus.StatusEnum.Irrelevant)
							m_list.Add(error);
					}
				}
			}

			m_dataGridView.RowCount = m_list.Count;
			m_dataGridView.CheckingErrors = m_list;
			m_dataGridView.StyleSheet = StyleSheet;
			m_dataGridView.TMAdapter = TMAdapter;
			Sort(m_sortedColumn, false, kRefCol);

			m_prevResultRow = -1;

			if (m_persistence != null)
				OnLoadSettings(m_persistence.SettingsKey);

			m_dataGridView.RowEnter += m_dataGridView_RowEnter;

			if (m_dataGridView.RowCount > 0)
			{
				m_dataGridView.CurrentCell = m_dataGridView[0, 0];

				// Do this in case the current row didn't change by setting the current cell.
				m_dataGridView_RowEnter(this, new DataGridViewCellEventArgs(0, 0));
			}
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string StatusBarTextFormat
		{
			get { return TeResourceHelper.GetResourceString("kstidEditorialChkVwStatusTextFmt"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the results in the grid are stale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsStale
		{
			get { return m_dataGridView.IsStale; }
			set { m_dataGridView.IsStale = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style sheet from the main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FwStyleSheet StyleSheet
		{
			get
			{
				FwMainWnd mainWnd = FindForm() as FwMainWnd;
				return (mainWnd == null ? null : mainWnd.StyleSheet);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Toolbar Menu adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ITMAdapter TMAdapter
		{
			get
			{
				FwMainWnd mainWnd = TopLevelControl as FwMainWnd;
				return (mainWnd == null || mainWnd.TMAdapter == null) ? null : mainWnd.TMAdapter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the status column in the editorial checks grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckGridStatusColumn StatusColumn
		{
			get { return m_Status; }
		}

		#endregion

		#region Event handling
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ChangeZoomPercent(float oldFactor, float newFactor)
		{
			base.ChangeZoomPercent(oldFactor, newFactor);

			// Find the height of the largest status image.
			int maxStatusHeight = 0;
			maxStatusHeight = Math.Max(maxStatusHeight, EditorialChecksControl.IgnoredInconsistenciesImage.Height);
			maxStatusHeight = Math.Max(maxStatusHeight, EditorialChecksControl.InconsistenciesImage.Height);
			maxStatusHeight = Math.Max(maxStatusHeight, TeResourceHelper.CheckErrorIrrelevant.Height);

			// Make sure the row height hasn't shrunk below the height of the status icons.
			if (maxStatusHeight > m_dataGridView.RowHeight)
				m_dataGridView.RowHeight = maxStatusHeight;

			m_dataGridView.ResetFonts();
			m_dataGridView.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add ourselves from the xCoreColleagues list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// This should only be null when running tests.
			FwMainWnd mainWnd = FindForm() as FwMainWnd;
			if (mainWnd != null)
				mainWnd.Mediator.AddColleague(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove ourselves from the xCoreColleagues list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
			// This should only be null when running tests.
			FwMainWnd mainWnd = FindForm() as FwMainWnd;
			if (mainWnd != null && mainWnd.Mediator != null)
				mainWnd.Mediator.RemoveColleague(this);

			base.OnHandleDestroyed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected checking error.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal CheckingError SelectedCheckingError
		{
			get
			{
				if (m_dataGridView.CurrentRow == null)
					return CheckingError.Empty;

				int rowIndex = m_dataGridView.CurrentRow.Index;
				return (m_list != null && m_list.Count > rowIndex &&
					rowIndex >= 0 ? m_list[rowIndex] as CheckingError :
					CheckingError.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RowEnter event of the m_dataGridView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_RowEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (ReferenceChanged != null && e != null && e.RowIndex != m_prevResultRow)
			{
				ReferenceChanged(this, GetCheckingError(e.RowIndex));
				m_prevResultRow = e.RowIndex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellClick event of the m_dataGridView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewCellEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void m_dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (ReferenceChanged != null && e != null && e.RowIndex >= 0 && e.RowIndex == m_prevResultRow)
				ReferenceChanged(this, GetCheckingError(e.RowIndex));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ColumnHeaderMouseClick event of the m_dataGridView control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;

			DataGridViewColumn column = m_dataGridView.Columns[e.ColumnIndex];
			if (column.SortMode == DataGridViewColumnSortMode.Programmatic)
				Sort(column, true, kRefCol);
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		private void m_dataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			if (ErrorListContentChanged != null)
				ErrorListContentChanged(this, CheckingError.Empty);
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		private void m_dataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			if (ErrorListContentChanged != null)
				ErrorListContentChanged(this, CheckingError.Empty);
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the guid of the current row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override object GetPreSortRow()
		{
			CheckingError error = SelectedCheckingError;
			return (error != null && error.MyNote != null) ? error.MyNote.Guid : Guid.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use the supplied information to set the current checking error row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void RestorePreSortRow(object restoreRow)
		{
			if (restoreRow != null && restoreRow.GetType() == typeof(Guid))
				SelectCheckingError((Guid)restoreRow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the checking error from the specified row.
		/// </summary>
		/// <param name="rowIndex">Index of the row.</param>
		/// <returns>checking error at the specified row, or null if the checking error does
		/// not exist for the specified row</returns>
		/// ------------------------------------------------------------------------------------
		private CheckingError GetCheckingError(int rowIndex)
		{

			return (rowIndex < 0 || rowIndex >= m_list.Count ?
				null : m_list[rowIndex] as CheckingError);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the checking error for the specified guid. If a checking error
		/// for the guid cannot be found, then -1 is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int GetCheckingError(Guid guid)
		{
			for (int i = 0; i < m_list.Count; i++)
			{
				CheckingError error = m_list[i] as CheckingError;
				if (error != null && error.MyNote.Guid == guid)
					return i;
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the checking error in the grid to the one whose guid is the same as that
		/// specified. If the specified guid cannot be found, then the first checking error
		/// in the grid is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SelectCheckingError(Guid guid)
		{
			if (m_dataGridView.RowCount > 0)
			{
				int i = (guid == Guid.Empty ? 0 : GetCheckingError(guid));
				m_dataGridView.CurrentCell =
					m_dataGridView[0, (i < 0 || i >= m_dataGridView.RowCount ? 0 : i)];
			}
		}

		#endregion

		#region Message handlers
		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnScrChecksIgnored(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != TopLevelControl)
				return false;

			CheckingError error = SelectedCheckingError;
			if (error != CheckingError.Empty && error.MyNote.ResolutionStatus != NoteStatus.Closed)
			{
				string undo;
				string redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoResolutionStatusChange",
					out undo, out redo);
				using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
					null, undo, redo))
				{
					error.MyNote.ResolutionStatus = NoteStatus.Closed;
					undoHelper.RollBack = false;
				}
			}

			return true;
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnUpdateScrChecksIgnored(object args)
		{
			if (!FindForm().ContainsFocus)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != FindForm())
				return false;

			itemProps.Update = true;
			CheckingError error = SelectedCheckingError;
			itemProps.Enabled = (error != CheckingError.Empty && error.MyNote.ResolutionStatus == NoteStatus.Open);
			return true;
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnScrChecksIgnoredWAnnotation(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != TopLevelControl)
				return false;

			CheckingError error = SelectedCheckingError;
			if (error != CheckingError.Empty && error.MyNote.ResolutionStatus != NoteStatus.Closed)
				EditCheckingErrorComment(error, "kstidUndoRedoResolutionStatusChange", true);

			return true;
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		/// For a characters check result, add the "invalid" character as a valid character.
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnScrChecksAddAsValidCharacter(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != TopLevelControl)
				return false;

			CheckingError error = SelectedCheckingError;
			if (error != CheckingError.Empty && error.MyNote.ResolutionStatus != NoteStatus.Closed)
				AddAsValidCharacter(error);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the "invalid" character as a valid character.
		/// </summary>
		/// <param name="addedCharError">The checking error containing the character that will
		/// be added to the valid character inventory.</param>
		/// ------------------------------------------------------------------------------------
		private void AddAsValidCharacter(CheckingError addedCharError)
		{
			Debug.Assert(addedCharError.CheckId == StandardCheckIds.kguidCharacters,
				"Checking error should be from the valid characters check");

			IWritingSystem ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			if (StringUtils.IsCharacterDefined(addedCharError.MyNote.CitedText))
			{
				using (new WaitCursor(Parent))
				{
					// Get the valid characters from the database
					ValidCharacters validChars = ValidCharacters.Load(ws, ValidCharsLoadException);
					if (validChars != null)
					{
						validChars.AddCharacter(addedCharError.MyNote.CitedText);
						ws.ValidChars = validChars.XmlString;
						m_cache.ServiceLocator.WritingSystemManager.Save();
					}
					// Mark all data grid view rows containing the newly-defined valid character to irrelevant.
					for (int iRow = 0; iRow < m_list.Count; iRow++)
					{
						CheckingError checkError = GetCheckingError(iRow);
						if (((IStTxtPara)checkError.MyNote.QuoteOA.ParagraphsOS[0]).Contents.Text ==
							addedCharError.MyNote.CitedText)
						{
							// We don't want to create an undoable action, so we suppress subtasks.
							using (var unitOfWork =
									new NonUndoableUnitOfWorkHelper(m_cache.ServiceLocator.GetInstance<IActionHandler>()))
							{
								checkError.Status = CheckingStatus.StatusEnum.Irrelevant;
								unitOfWork.RollBack = false;
							}
						}
					}

					IsStale = true;

					m_dataGridView.Invalidate();
				}
			}
			else
			{
				string msg = ResourceHelper.GetResourceString("kstidUndefinedCharacterMsg");
				MessageBox.Show(this, msg, m_mainWnd.App.ApplicationName,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnUpdateScrChecksIgnoredWAnnotation(object args)
		{
			return OnUpdateScrChecksIgnored(args);
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnScrChecksInconsistency(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != TopLevelControl)
				return false;

			CheckingError error = SelectedCheckingError;
			if (error != CheckingError.Empty && error.MyNote.ResolutionStatus != NoteStatus.Open)
			{
				string undo;
				string redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoResolutionStatusChange",
					out undo, out redo);
				using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
					null, undo, redo))
				{
					error.MyNote.ResolutionStatus = NoteStatus.Open;
					undoHelper.RollBack = false;
				}
			}

			return true;
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnUpdateScrChecksInconsistency(object args)
		{
			if (!FindForm().ContainsFocus)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != FindForm())
				return false;

			itemProps.Update = true;
			CheckingError error = SelectedCheckingError;
			itemProps.Enabled = (error != CheckingError.Empty && error.MyNote.ResolutionStatus == NoteStatus.Closed);
			return true;
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnScrChecksEditAnnotation(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			CheckingError error = SelectedCheckingError;
			if (itemProps == null || itemProps.ParentForm != TopLevelControl || error == null)
				return false;

			EditCheckingErrorComment(error, "kstidUndoEditScrChkErrAnnotation", false);
			return true;
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the dialog in which the user may edit the resolution comment for an ignored
		/// checking error result.
		/// </summary>
		/// --------------------------------------------------------------------------------------
		private void EditCheckingErrorComment(CheckingError error, string undoRedoLabel,
			bool fResolve)
		{
			if (StyleSheet == null)
				return;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels(undoRedoLabel, out undo, out redo);
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
				null, undo, redo))
			{
				if (fResolve)
					error.MyNote.ResolutionStatus = NoteStatus.Closed;

				using (CheckingErrorCommentDlg dlg = new CheckingErrorCommentDlg(error, StyleSheet, m_helpTopicProvider))
				{
					if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					{
						// Force the cell to repaint so the red corner glyph gets painted.
						int iRow = m_dataGridView.CurrentCellAddress.Y;
						m_dataGridView.InvalidateCell(kStatusCol, iRow);
						undoHelper.RollBack = false;
					}
				}
			}
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool OnUpdateScrChecksEditAnnotation(object args)
		{
			if (!FindForm().ContainsFocus)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != FindForm())
				return false;

			itemProps.Update = true;
			CheckingError error = SelectedCheckingError;
			itemProps.Enabled = (error != CheckingError.Empty && error.MyNote.ResolutionStatus == NoteStatus.Closed);
			return true;
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		/// Update the Add As Valid Character menu. It should only be visible for checking errors
		/// generated by the valid characters check.
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected bool	OnUpdateScrChecksAddAsValidCharacter(object args)
		{
			if (!FindForm().ContainsFocus)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || itemProps.ParentForm != FindForm())
				return false;

			itemProps.Update = true;
			CheckingError error = SelectedCheckingError;
			itemProps.Visible = (error != CheckingError.Empty &&
				error.CheckId == StandardCheckIds.kguidCharacters); // characters check

			if (itemProps.Visible)
			{
				// FWR-1948, the CitedText can sometimes be null, shouldn't ever really happen, but seemed
				// best to just prevent the crash and allow the next run of the checks to fix things.
				if (error.MyNote.CitedText != null)
				{
					List<string> validChars = GetValidCharacters();

					// If the checking error is for an invalid character, we only want to enable the
					// menu option if the character has not already been added.
					itemProps.Enabled = (StringUtils.IsValidChar(error.MyNote.CitedText, m_cache.ServiceLocator.UnicodeCharProps) &&
										 validChars != null && !validChars.Contains(error.MyNote.CitedText));
				}
				else
					itemProps.Enabled = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of valid characters from the vernacular writing system in the database.
		/// </summary>
		/// <returns>list of valid characters</returns>
		/// ------------------------------------------------------------------------------------
		private List<string> GetValidCharacters()
		{
			IWritingSystem ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			ValidCharacters validChars = ValidCharacters.Load(ws, ValidCharsLoadException);
			return (validChars != null ? validChars.AllCharacters : null);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();
			base.OnSaveSettings(key);

			CheckingError error = SelectedCheckingError;

			// Sometimes we get here when the cache has already been disposed but the error.
			// is still an object. In that case referencing the guid will cause a crash,
			// therefore ignore exceptions.
			try
			{
				if (key != null && error != null && error != CheckingError.Empty)
					key.SetValue("SelectedCheckingError", error.MyNote.Guid);
			}
			catch { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoadSettings(Microsoft.Win32.RegistryKey key)
		{
			base.OnLoadSettings(key);

			if (m_list == null || key == null)
				return;

			string value = key.GetValue("SelectedCheckingError", null) as string;
			if (value != null)
			{
				Guid guid = new Guid(value);
				SelectCheckingError(guid);
			}
		}

		#region IVwNotifyChange Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == ScrScriptureNoteTags.kflidResolutionStatus &&
				ErrorListContentChanged != null)
			{
				CheckingError error = CheckingError.Create(m_cache, hvo);
				if (error != null)
					ErrorListContentChanged(this, error);
			}
		}

		#endregion

		#region IxCoreColleague Members
		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] {this};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			// not used in TE.
		}

		#endregion
	}

	#region EditorialChecksGrid class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// EditorialChecksGrid encapsulates, primarily, behavior for the comments tooltip. For some
	/// reason, when the comments tooltip is displayed, the grid doesn't always receive mouse
	/// move (including CellMouseEnter and CellMouseLeave) events. However, the overridden
	/// WndProc method in this class, does receive the necessary mouse mouse events.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EditorialChecksGrid : CheckGrid
	{
		#region Member variables
		/// <summary>Bla bla bla</summary>
		public event EventHandler BookFilterChanged;

		/// <summary>DataGridViewCell which currently has the mouse over it.</summary>
		protected DataGridViewCell m_mouseOverCell;
		/// <summary>DataGridViewCell which currently has the tooltip up.</summary>
		protected DataGridViewCell m_toolTipCell;
		/// <summary>Tooltip for the comment field</summary>
		private CommentToolTip m_commentToolTip;
		/// <summary>stylesheet from the main window</summary>
		private FwStyleSheet m_styleSheet;

		private FwTextBoxColumn m_detailsCol;

		private bool m_detailCellIsForNonPrintableChar;
		private bool m_contentsStale;
		private string m_waterMark = "!";

		/// <summary>This tool is used only for checking errors for the characters check
		/// and displays information about the character being hovered over in the check
		/// result grid.</summary>
		private readonly CharacterInfoToolTip m_charChkResultToolTip;

		private Dictionary<int, Font> m_detailsColFonts = new Dictionary<int, Font>();

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:EditorialChecksGrid"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksGrid()
		{
			m_charChkResultToolTip = new CharacterInfoToolTip();
		}

		#region Disposing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing && !IsDisposed)
			{
				ResetFonts();

				if (m_commentToolTip != null)
					m_commentToolTip.Dispose();
				if (m_detailsColFonts != null)
				{
					foreach (KeyValuePair<int, Font> kvp in m_detailsColFonts)
						kvp.Value.Dispose();
					m_detailsColFonts.Clear();
				}
				m_charChkResultToolTip.Dispose();
			}

			m_detailsColFonts = null;
			m_commentToolTip = null;
			m_list = null;
			base.Dispose(disposing);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the checking errors.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal List<ICheckGridRowObject> CheckingErrors
		{
			set { m_list = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the style sheet to the one used in the main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStyleSheet StyleSheet
		{
			set { m_styleSheet = value; }
		}

		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the specified row in the grid is ignored
		/// and has an annotation associated with it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsRowIgnoredWithAnnotation(int iRow)
		{
			if (m_list == null || iRow < 0 || iRow >= m_list.Count)
				return false;

			CheckingError error = m_list[iRow] as CheckingError;

			// If the checking error is null or the error
			// is resolved, there is no annotation.
			if (error == null || error.MyNote.ResolutionStatus != NoteStatus.Closed)
				return false;

			// Go through the paragraphs and make sure
			// there is some text in at least one of them.
			foreach (IStTxtPara para in error.MyNote.ResolutionOA.ParagraphsOS)
			{
				if (para.Contents.Text != null)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the cached fonts used for the details column when there detail cells whose
		/// writing system is not the default vernacular. This cache automatically gets
		/// rebuilt as needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResetFonts()
		{
			m_detailsColFonts.Clear();
		}

		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a value indicating whether or not the specified row and column correspond
		/// to the status column in a row that has an ignored result a resolution annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool ShouldDrawCornerGlyph(int iCol, int iRow)
		{
			return (iCol == EditorialChecksRenderingsControl.kStatusCol &&
				IsRowIgnoredWithAnnotation(iRow));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we have a toolbar menu adapter, we want to show it as a context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellRightMouseUp(DataGridViewCellMouseEventArgs e)
		{
			base.OnCellRightMouseUp(e);

			if (m_tmAdapter != null)
			{
				// If the tool tip is currently showing, hide it before bringing up the context menu.
				HideToolTip();
				m_tmAdapter.PopupMenu("cmnuScrChecks", MousePosition.X, MousePosition.Y);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse moves over the details column and the row is for a characters
		/// check error, then show a tooltip displaying info. about the invalid character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellMouseEnter(DataGridViewCellEventArgs e)
		{
			base.OnCellMouseEnter(e);

			if (m_detailsCol != null && e.RowIndex >= 0 &&
				e.ColumnIndex == EditorialChecksRenderingsControl.kDetailsCol)
			{
				// Only show the tooltip if the check result is for the characters check.
				CheckingError error = m_list[e.RowIndex] as CheckingError;
				if (error != null && error.CheckId == StandardCheckIds.kguidCharacters)
				{
					// get the writing system of the invalid character so that we can get
					// the font in which the character should be displayed. The font is
					// sent to the tooltip in order for the tooltip to determine whether
					// or not the invalid character has a representative glyph in its font.
					int ws = StringUtils.GetWsAtOffset(error.Details, 0);
					Font fnt;
					if (!m_detailsColFonts.TryGetValue(ws, out fnt))
						fnt = m_detailsCol.Font;

					m_charChkResultToolTip.Show(this, error.MyNote.CitedText, fnt);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the tooltip is showing then make sure tooltips for the cell we're leaving are
		/// hidden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellMouseLeave(DataGridViewCellEventArgs e)
		{
			base.OnCellMouseLeave(e);

			// This won't always be necessary, but it doesn't hurt to do it always.
			m_charChkResultToolTip.Hide();

			if (m_toolTipCell != null)
				HideToolTip();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
				e.ColumnIndex > EditorialChecksRenderingsControl.kStatusCol ||
				m_list == null || m_list.Count == 0)
			{
				e.Value = null;
				base.OnCellValueNeeded(e);
				return;
			}

			CheckingError error = m_list[e.RowIndex] as CheckingError;
			if (error != null)
			{
				switch (e.ColumnIndex)
				{
					case EditorialChecksRenderingsControl.kRefCol:
						e.Value = error.DisplayReference;
						break;
					case EditorialChecksRenderingsControl.kTypeCol:
						e.Value = error.TypeOfCheck;
						break;
					case EditorialChecksRenderingsControl.kMessageCol:
						e.Value = error.Message;
						break;
					case EditorialChecksRenderingsControl.kDetailsCol:
						e.Value = error.Details;
						break;
					case EditorialChecksRenderingsControl.kStatusCol:
						e.Value = error.Status;
						break;
				}
			}

			base.OnCellValueNeeded(e);
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		/// Show checking errors with a status of irrelevant in gray text. Also make sure the
		/// font of font for the details column matches the font for the writing system of the
		/// cited text that goes in the current cell of that column. Finally, if the cited text
		/// is for a characters check and the character is a non-printable character, then
		/// show the character's name in red.
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			// Under certain circumstances, this gets set at the bottom of this method.
			// This will make sure it's cleared before anything else.
			m_detailCellIsForNonPrintableChar = false;

			if (e.RowIndex < 0 || e.ColumnIndex < 0 || m_list.Count <= e.RowIndex)
				return;

			// Make sure we have a checking error.
			CheckingError error = m_list[e.RowIndex] as CheckingError;
			if (error == null)
				return;

			// If the check error status is no longer relevant, then make the text
			// color gray to indicate that fact.
			if (error.Status == CheckingStatus.StatusEnum.Irrelevant)
				e.CellStyle.ForeColor = SystemColors.GrayText;

			// If we're not dealing with the details column,
			// then no more format handling is necessary.
			if (e.ColumnIndex != EditorialChecksRenderingsControl.kDetailsCol)
				return;

			if (m_detailsCol == null)
				m_detailsCol = Columns[e.ColumnIndex] as FwTextBoxColumn;

			Font fnt = m_detailsCol.Font;

			// Get the writing system for the cited text that will be displayed in the cell.
			// If that writing system is the same as the column's (which should be default
			// vernacular), then no more formatting necessary.
			if (error.Details == null)
				return; // can't get Ws of null string
			int ws = StringUtils.GetWsAtOffset(error.Details, 0);
			if (ws <= 0)
				return; // Something bad happened (TE-8559)

			if (ws != m_detailsCol.WritingSystemCode)
			{
				// At this point, we know the detail columm's writing system (i.e. the default
				// vernacular) is different from the writing system for the current cell's cited
				// text. Therefore, get the font for the cited text's writing system and return
				// that in the cell's style object.
				if (!m_detailsColFonts.TryGetValue(ws, out fnt) || fnt.Size != m_detailsCol.Font.Size)
				{
					string fontFace = m_styleSheet.GetNormalFontFaceName(m_cache, ws);
					fnt = new Font(fontFace, m_detailsCol.Font.Size);
					m_detailsColFonts[ws] = fnt;
				}

				e.CellStyle.Font = fnt;
				m_detailsCol.SetCellStyleAlignment(ws, e.CellStyle);
			}

			// Do nothing else if the check result is not for the characters check.
			if (error.CheckId != StandardCheckIds.kguidCharacters)
				return;

			string chr = error.MyNote.CitedText;
			if (string.IsNullOrEmpty(chr))
				return;

			// If the character is a space or control character, then show the character name
			// rather than the character itself since, obviously, it couldn't be seen otherwise.
			if (Win32.AreCharGlyphsInFont(chr, fnt) && (Icu.IsControl(chr) || Icu.IsSpace(chr)))
			{
				e.Value = Icu.GetPrettyICUCharName(chr);
				e.CellStyle.ForeColor = Color.Red;
				e.CellStyle.Font = DefaultCellStyle.Font;
				m_detailCellIsForNonPrintableChar = true;
			}
		}

		/// --------------------------------------------------------------------------------------
		/// <summary>
		/// For checking errors that have become irrelevant (e.g. the user decided that a character
		/// cited as invalid is really valid), paint them with a strike-through line. Also draw
		/// a special icon if the cell is in a characters check result row, is the cited text
		/// and the cited text is a character that does not have a corresponding glyph in its
		/// writing system font.
		/// </summary>
		/// --------------------------------------------------------------------------------------
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			base.OnCellPainting(e);

			if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= m_list.Count)
				return;

			// Make sure we have a checking error.
			CheckingError error = m_list[e.RowIndex] as CheckingError;
			if (error == null)
				return;

			// Draw everything but the content.
			DataGridViewPaintParts parts = e.PaintParts;
			parts &= ~DataGridViewPaintParts.ContentForeground;
			e.Paint(e.ClipBounds, parts);
			e.Handled = true;

			// Set the color depending on the selected state of the cell.
			bool selected = ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected);
			Color clr = (selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor);

			// Draw the content if DrawMissingGlyphImage does not.
			if (!DrawMissingGlyphImage(error, e))
			{
				if (e.ColumnIndex == EditorialChecksRenderingsControl.kRefCol &&
					error.MyNote.BeginObjectRA != null && error.MyNote.BeginObjectRA.Owner is IStFootnote)
				{
					using (SolidBrush brush = new SolidBrush(clr))
					{
						Rectangle rect = e.CellBounds;
						rect.Y += (rect.Height - e.CellStyle.Font.Height) / 2;
						// Use the restore window icon character from Marlett as the
						// footnote reference indicator.
						float fontSize = e.CellStyle.Font.SizeInPoints;
#if __MonoCS__
						string fontname = "OpenSymbol";
						string symbol = "\u2042";	// ASTERISM
#else
						string fontname = "Marlett";
						string symbol = "\u0032";	// restore window icon
#endif
						using (Font marFont = new Font(fontname, fontSize))
						{
							e.Graphics.DrawString(symbol, marFont, brush, rect.X, rect.Y);
							Size charSize = TextRenderer.MeasureText(e.Graphics, symbol, marFont,
								Size.Empty, TextFormatFlags.NoPadding);
							rect.X += charSize.Width;
							rect.Width -= charSize.Width;
						}
						e.Graphics.DrawString(e.Value.ToString(), e.CellStyle.Font, brush, rect.X, rect.Y);
					}
				}
				else
				{
					e.Paint(e.ClipBounds, DataGridViewPaintParts.ContentForeground);
				}
			}

			if (error.Status != CheckingStatus.StatusEnum.Irrelevant)
				return;

			// At this point, all we're going to do is draw a line through the cell
			// if it's for a characters checking error whose character was added to
			// the valid characters list (using the right-click menu option) but
			// before the check results have been refreshed.
			// Draw a strike-through line.
			using (Pen pen = new Pen(clr))
			{
				Rectangle rc = e.CellBounds;
				int x1 = (e.ColumnIndex == 0) ? rc.X + 2 : rc.X;
				int x2 = (e.ColumnIndex == ColumnCount - 1) ? rc.Right - 2 : rc.Right;
				Point pt1 = new Point(x1, rc.Top + (rc.Height / 2));
				Point pt2 = new Point(x2, rc.Top + (rc.Height / 2));
				e.Graphics.DrawLine(pen, pt1, pt2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines wheter or not the contents of the details cell (specified by
		/// e.ColumnIndex) represents the cited text for a characters checking error. If so
		/// and the character does not have a glyph in the cited text's font, then just
		/// show image indicating that fact.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool DrawMissingGlyphImage(CheckingError error, DataGridViewCellPaintingEventArgs e)
		{
			// Don't do anything if we're not in the detials column or if we're not on a row for
			// a characters check result or the cell's content is for a non printable character.
			if (e.ColumnIndex != EditorialChecksRenderingsControl.kDetailsCol ||
				error.CheckId != StandardCheckIds.kguidCharacters || m_detailCellIsForNonPrintableChar)
			{
				return false;
			}

			// Check if the character has a glyph in its font.
			// the writing system font.
			if (!Win32.AreCharGlyphsInFont(error.MyNote.CitedText, e.CellStyle.Font))
			{
				Image img = CharacterGrid.MissingGlyphImage;
				if (img != null)
				{
					// Paint the image that was found in the tag property.
					int dy = (int)((e.CellBounds.Height - img.Height) / 2);
					e.Graphics.DrawImageUnscaled(img, e.CellBounds.X + 4, e.CellBounds.Y + dy);
					return true;
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For some reason, when the comments tooltip is displayed, the grid doesn't always
		/// receive mouse move (including CellMouseEnter and CellMouseLeave) events. Therefore,
		/// we take what we can get to determine where the mouse is when the tooltip is
		/// opened, and the only reliable message we appear to get is the WM_NCHITTEST message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == (int)Win32.WinMsgs.WM_NCHITTEST)
			{
				int x = MiscUtils.LoWord(m.LParam);
				int y = MiscUtils.HiWord(m.LParam);

				Point pt = PointToClient(new Point(x, y));
				HitTestInfo hti = HitTest(pt.X, pt.Y);

				// if the mouse is over a data grid view cell...
				if (hti.ColumnIndex >= 0 && hti.RowIndex >= 0 && hti.RowIndex < RowCount)
				{
					m_mouseOverCell = this[hti.ColumnIndex, hti.RowIndex];

					// if the cell that the mouse is over is the same as the cell that has the comment
					// showing in the tooltip, we don't need to do anything.
					if (m_mouseOverCell != m_toolTipCell)
						HandleToolTip();
				}
				else
				{
					m_mouseOverCell = null;
					HideToolTip();
				}
			}

			base.WndProc(ref m);
		}

		#endregion

		#region Watermark handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the water mark when the grid changes size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the water mark when the grid scrolls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnScroll(ScrollEventArgs e)
		{
			base.OnScroll(e);
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints a water mark when the results are stale (i.e. the query settings have been
		/// changed since the results were shown).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!m_contentsStale || string.IsNullOrEmpty(m_waterMark))
				return;

			Rectangle rc = WaterMarkRectangle;
			using (GraphicsPath path = new GraphicsPath())
			{
			FontFamily family = FontFamily.GenericSerif;

			// Find the first font size equal to or smaller than 256 that
			// fits in the water mark rectangle.
			for (int size = 256; size >= 0; size -= 2)
			{
				using (Font fnt = new Font(family, size, FontStyle.Bold))
				{
					int height = TextRenderer.MeasureText(m_waterMark, fnt).Height;
					if (height < rc.Height)
					{
						using (StringFormat sf = (StringFormat)StringFormat.GenericDefault.Clone())
						{
							sf.Alignment = StringAlignment.Center;
							sf.LineAlignment = StringAlignment.Center;
							sf.Trimming = StringTrimming.EllipsisCharacter;
							sf.FormatFlags |= StringFormatFlags.NoWrap;
							path.AddString(m_waterMark, family, (int)FontStyle.Bold, size, rc, sf);
						}

						break;
					}
				}
			}

			path.AddEllipse(rc);

			using (SolidBrush br = new SolidBrush(Color.FromArgb(80, DefaultCellStyle.ForeColor)))
				e.Graphics.FillRegion(br, new Region(path));
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rectangle in which the watermark is drawn.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Rectangle WaterMarkRectangle
		{
			get
			{
				Rectangle rc;
				int clientWidth = ClientSize.Width;

				if (Rows.Count > 0 && Columns.Count > 0 && FirstDisplayedCell != null)
				{
					// Determine whether or not the vertical scroll bar is showing.
					int visibleRows = Rows.GetRowCount(DataGridViewElementStates.Visible);
					rc = GetCellDisplayRectangle(0, FirstDisplayedCell.RowIndex, false);
					if (rc.Height * visibleRows >= ClientSize.Height)
						clientWidth -= SystemInformation.VerticalScrollBarWidth;
				}

				// Modify the client rectangle so it doesn't
				// include the vertical scroll bar width.
				rc = ClientRectangle;
				rc.Width = (int)(clientWidth * 0.5f);
				rc.Height = (int)(rc.Height * 0.5f);
				rc.X = (clientWidth - rc.Width) / 2;
				rc.Y = (ClientRectangle.Height - rc.Height) / 2;
				return rc;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the grid's contents are dirty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsStale
		{
			get { return true; }
			set
			{
				m_contentsStale = value;
				Invalidate();
			}
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TimerTick event of the m_commentToolTip control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void m_commentToolTip_TimerTick(object sender, EventArgs e)
		{
			if (m_toolTipCell != null && !ClientRectangle.Contains(PointToClient(MousePosition)))
				HideToolTip();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opened event of the m_commentToolTip control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void m_commentToolTip_Opened(object sender, EventArgs e)
		{
			m_toolTipCell = m_mouseOverCell;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Closed event of the m_commentToolTip control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.ToolStripDropDownClosedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_commentToolTip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			m_toolTipCell = null;
		}

		#endregion

		#region Methods for showing and hiding the comments tooltip.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the tooltip that shows the checking error comment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleToolTip()
		{
			if (m_mouseOverCell == null || !IsRowIgnoredWithAnnotation(m_mouseOverCell.RowIndex))
			{
				HideToolTip();
				return;
			}

			CheckingError error = m_list[m_mouseOverCell.RowIndex] as CheckingError;

			// If the context menu is showing, we don't want to bring up the tooltip.
			TMItemProperties itemProps = m_tmAdapter.GetItemProperties("cmnuStatusIgnored");
			if (itemProps != null && itemProps.IsDisplayed)
				return;

			Form frm = FindForm();
			if (frm != null && !frm.ContainsFocus)
				return;

			if (Columns[m_mouseOverCell.ColumnIndex].DataPropertyName != "Status")
				HideToolTip();
			else
			{
				if (m_commentToolTip == null)
				{
					m_commentToolTip = new CommentToolTip(m_styleSheet);
					m_commentToolTip.Closed += m_commentToolTip_Closed;
					m_commentToolTip.Opened += m_commentToolTip_Opened;
					m_commentToolTip.TimerTick += m_commentToolTip_TimerTick;
				}

				m_toolTipCell = this[m_mouseOverCell.ColumnIndex, m_mouseOverCell.RowIndex];

				m_commentToolTip.Show(error.MyNote.ResolutionOA,
					this[m_mouseOverCell.ColumnIndex, m_mouseOverCell.RowIndex]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the tool tip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HideToolTip()
		{
			// close the tooltip
			if (m_commentToolTip != null)
				m_commentToolTip.Hide();
			m_toolTipCell = null;
		}

		#endregion

		#region IVwNotifyChange Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			switch (tag)
			{
				case ScriptureTags.kflidScriptureBooks:
					if (BookFilterChanged != null)
						BookFilterChanged(this, EventArgs.Empty);
					break;

				case ScrScriptureNoteTags.kflidResolutionStatus:
					InvalidateColumn(EditorialChecksRenderingsControl.kStatusCol);
					break;

				case ScrBookAnnotationsTags.kflidNotes:
					if (cvDel <= 0 || m_list == null)
						break;

					bool deletionsOccurred = false;

					// Go through the list of checking errors and remove the ones whose
					// owner is null (because those are the ones that were deleted).
					for (int i = m_list.Count - 1; i >= 0; i--)
					{
						CheckingError error = m_list[i] as CheckingError;
						if (error != null && error.MyNote.Owner == null)
						{
							m_list.RemoveAt(i);
							deletionsOccurred = true;
						}
					}

					// Nothing to do if no checking errors were removed from the list.
					if (!deletionsOccurred || RowCount == m_list.Count)
						break;

					// If the number of rows in the list is different from the grid's row count,
					// then set it's number of rows to the new size of the list.
					int prevRow = CurrentCellAddress.Y;
					RowCount = m_list.Count;
					Invalidate();
					if (prevRow == CurrentCellAddress.Y)
					{
						OnRowEnter(new DataGridViewCellEventArgs(CurrentCellAddress.X,
							CurrentCellAddress.Y));
					}

					break;
			}
		}

		#endregion
	}

	#endregion
}
