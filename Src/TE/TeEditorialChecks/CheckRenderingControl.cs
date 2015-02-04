// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CheckRenderingControl.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.Framework;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Controls;
using Microsoft.Win32;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckRenderingControl : UserControl, ISelectableView, IChecksViewWrapperView,
		IVwNotifyChange, IMessageFilter
	{
		#region Member variables
		private CheckGrid m_dataGridView;
		private readonly float m_szOfFontAt100Pcnt = FontInfo.kDefaultFontSize;
		/// <summary></summary>
		protected string m_prevStatusBarText;
		/// <summary></summary>
		protected CheckGridListSorter m_gridSorter;
		/// <summary></summary>
		protected bool m_userChangedColumnWidth = true;
		/// <summary></summary>
		protected Persistence m_persistence;
		/// <summary></summary>
		protected TsStringComparer m_tsStrComparer;
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected FwMainWnd m_mainWnd;
		/// <summary></summary>
		protected List<ICheckGridRowObject> m_list;
		/// <summary></summary>
		protected DataGridViewColumn m_sortedColumn;
		/// <summary></summary>
		protected int m_prevResultRow = -1;
		private RegistryFloatSetting m_zoomFactor;

		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckRenderingControl(FdoCache cache, FwMainWnd mainWnd)
		{
			DoubleBuffered = true;
			m_cache = cache;
			m_mainWnd = mainWnd;

			if (m_mainWnd != null)
				m_mainWnd.ZoomPercentageChanged += HandleZoomPercentageChanged;

			// Setup a new TsString sort comparer.
			if (m_cache == null)
				m_tsStrComparer = new TsStringComparer();
			else
			{
				m_cache.MainCacheAccessor.AddNotification(this);
				WritingSystem ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				m_tsStrComparer = new TsStringComparer(ws);
			}
		}
		#endregion

		#region IMessageFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the user presses Alt-Down or Alt-Up, then we want to change the selected row
		/// in the grid to the previous or next.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			CheckDisposed();

			// Ignore the message if we're not handling a key down message.
			if (m.Msg != (int)Win32.WinMsgs.WM_SYSKEYDOWN && m.Msg != (int)Win32.WinMsgs.WM_KEYDOWN)
				return false;

			// Ignore the message if the Alt key isn't down or the containing EditorialChecksViewWrapper doesn't contain focus.
			if ((ModifierKeys & Keys.Alt) != Keys.Alt || !Parent.Parent.Parent.ContainsFocus)
				return false;

			Keys key = ((Keys)(int)m.WParam & Keys.KeyCode);

			if (key == Keys.Up)
			{
				// Move to the previous row in the grid (if we're not already at the top).
				int currRow = m_dataGridView.CurrentCellAddress.Y;
				if (currRow <= 0 || m_dataGridView.RowCount <= 0)
					System.Media.SystemSounds.Beep.Play();
				else
					m_dataGridView.CurrentCell = m_dataGridView[0, --currRow];

				return true;
			}

			if (key == Keys.Down)
			{
				// Move to the next row in the grid (if we're not already at the bottom).
				int currRow = m_dataGridView.CurrentCellAddress.Y;
				if (currRow >= m_dataGridView.RowCount - 1)
					System.Media.SystemSounds.Beep.Play();
				else
					m_dataGridView.CurrentCell = m_dataGridView[0, ++currRow];

				return true;
			}

			return false;
		}

		#endregion

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this before doing anything else.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");

			if (disposing)
			{
				Application.RemoveMessageFilter(this);

				if (m_list != null)
					m_list.Clear();

				if (m_dataGridView != null && !m_dataGridView.Disposing)
					m_dataGridView.Dispose();

				if (m_zoomFactor != null)
					m_zoomFactor.Dispose();
			}

			m_tsStrComparer = null;
			m_dataGridView = null;
			m_list = null;
			m_zoomFactor = null;

			base.Dispose(disposing);
		}

		#endregion // IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckGrid DataGridView
		{
			get { return m_dataGridView; }
			set
			{
				m_dataGridView = value;

				if (m_dataGridView == null)
					return;

				m_dataGridView.DefaultCellStyle.Font = SystemFonts.DialogFont;

				foreach (DataGridViewColumn col in m_dataGridView.Columns)
				{
					if (col is FwTextBoxColumn)
						((FwTextBoxColumn)col).SizeOfFontAt100Percent = m_szOfFontAt100Pcnt;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the status bar label from the main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ToolStripStatusLabel StatusBarLabel
		{
			get
			{
				if (m_mainWnd == null || m_mainWnd.StatusStrip.Items.Count == 0)
					return null;

				return m_mainWnd.StatusStrip.Items[0] as ToolStripStatusLabel;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Derived classes should override this to provide a format string specific to its
		/// implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string StatusBarTextFormat
		{
			get { return "{0}"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current zoom factor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected float ZoomFactor
		{
			get { return m_zoomFactor.Value; }
			private set { m_zoomFactor.Value = value; }
		}

		#endregion

		#region Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			if (m_dataGridView != null && m_persistence != null)
				m_dataGridView.SettingsKey = m_persistence.SettingsKey;

			base.OnHandleCreated(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the zoom percentage changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleZoomPercentageChanged(object sender, ZoomPercentageChangedEventArgs e)
		{
			if (Visible && m_persistence != null && m_persistence.SettingsKey != null)
			{
				ChangeZoomPercent(e.OldZoomFactor.Height, e.NewZoomFactor.Height);
				ZoomFactor = e.NewZoomFactor.Height;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "font is a reference")]
		protected virtual void ChangeZoomPercent(float oldFactor, float newFactor)
		{
			Font oldFont = m_dataGridView.DefaultCellStyle.Font;
			m_dataGridView.DefaultCellStyle.Font = new Font(oldFont.FontFamily,
				m_szOfFontAt100Pcnt * newFactor, oldFont.Style, GraphicsUnit.Point);
			oldFont.Dispose();

			int newRowHeight = 0;

			foreach (DataGridViewColumn col in m_dataGridView.Columns)
			{
				if (col is DataGridViewTextBoxColumn || col is FwTextBoxColumn)
				{
					var textBoxColumn = col as FwTextBoxColumn;
					if (textBoxColumn != null)
						textBoxColumn.SetZoomFactor(newFactor);

					var font = col.DefaultCellStyle.Font ?? m_dataGridView.DefaultCellStyle.Font;
					newRowHeight = Math.Max(font.Height + 4, newRowHeight);
				}
			}

			m_dataGridView.RowHeight = newRowHeight;
			m_dataGridView.Invalidate();
		}

		#endregion

		#region ISelectableView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BaseInfoBarCaption
		{
			get	{return string.Empty;}
			set	{ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ActivateView()
		{
			Application.AddMessageFilter(this);

			if (StatusBarLabel != null)
			{
				// Save the status bar text then combine it with a message telling the user
				// he can use Alt-up and Alt-Down to move through the grid.
				m_prevStatusBarText = StatusBarLabel.Text;
				StatusBarLabel.Text = string.Format(StatusBarTextFormat, m_prevStatusBarText);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeactivateView()
		{
			Application.RemoveMessageFilter(this);

			// Restore the status bar text to what it was originally.
			if (StatusBarLabel != null && !string.IsNullOrEmpty(m_prevStatusBarText))
				StatusBarLabel.Text = m_prevStatusBarText;
		}

		#endregion

		#region IChecksViewWrapperView Members and other persistence methods.
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the persistence object.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public Persistence Persistence
		{
			get { return m_persistence; }
			set
			{
				if (m_persistence != null)
				{
					m_persistence.LoadSettings -= OnLoadSettings;
					m_persistence.SaveSettings -= OnSaveSettings;
				}

				m_persistence = value;
				if (m_persistence != null)
				{
					m_persistence.LoadSettings += OnLoadSettings;
					m_persistence.SaveSettings += OnSaveSettings;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "col is a reference")]
		protected virtual void OnSaveSettings(RegistryKey key)
		{
			StringBuilder bldr = new StringBuilder();
			for (int i = 0; i < m_dataGridView.ColumnCount; i++)
			{
				DataGridViewColumn col = m_dataGridView.Columns[i];
				bldr.AppendFormat("{0},", col.DisplayIndex);

				// Skip status columns.
				if (col is CheckGridStatusColumn && ((CheckGridStatusColumn)col).AutoSize)
					continue;

				bool forFillWeight = (col.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill);
				key.SetValue(GetRegSettingColName(col, forFillWeight),
					(forFillWeight ? col.FillWeight : col.Width));
			}

			if (bldr.Length != 0)
				key.SetValue("ColumnsDisplayOrder", bldr.ToString().TrimEnd(','));

			key.SetValue("RenderingGridWidth", m_dataGridView.Width);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnLoadSettings(RegistryKey key)
		{
			if (m_dataGridView != null && key != null)
			{
				m_dataGridView.TMAdapter = (m_mainWnd == null ? null : m_mainWnd.TMAdapter);

				if (m_zoomFactor != null)
					m_zoomFactor.Dispose();
				m_zoomFactor = new RegistryFloatSetting(key, "ZoomFactor" + Name, 1.5f);
				ChangeZoomPercent(1.0f, ZoomFactor);
			}

			// Set the display order of columns.
			SetColOrderFromReg(key);

			// Keep track of each time the grid changes sizes until its width is at its
			// maximum. When that happens, then set the widths of each column from values
			// read from the registry.
			m_dataGridView.SizeChanged -= HandleRenderingGridSizeChanged;
			m_dataGridView.SizeChanged += HandleRenderingGridSizeChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This delegate is necessary because when the OnLoadSettings method is called, the
		/// rendering grid has no width, and because columns in the rendering grid may have
		/// an AutoSizeMode of fill, their widths should not be set from the registry until the
		/// grid has it's full width. Therefore, this delegate will keep checking the current
		/// width of the grid with the width of the grid as it was last saved in the registry.
		/// When the two are equal, then this method will call the method to set all the
		/// column widths to those saved in the registry. I know, this is sort of kludgy, but
		/// this is yet one more limitation in the .Net framework I don't know how else to
		/// work around. -- DDO
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleRenderingGridSizeChanged(object sender, EventArgs e)
		{
			if (m_persistence == null || m_persistence.SettingsKey == null)
				return;

			// Find the last saved with of the rendering grid.
			int width = (int)m_persistence.SettingsKey.GetValue("RenderingGridWidth", -1);

			// If the grid is now full width or it's width was not found in the registry,
			// then unsubscribe to this event so we don't get here anymore.
			if (m_dataGridView.Width == width || width == -1)
				m_dataGridView.SizeChanged -= HandleRenderingGridSizeChanged;

			// If the grid is now at it's full width, then set the widths of all the columns.
			if (m_dataGridView.Width == width)
				SetColWidthsFromReg(m_persistence.SettingsKey);
		}

		#endregion

		#region Methods for initializing grid from registry
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the grid's column widths by reading their saved values from the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetColWidthsFromReg(RegistryKey key)
		{
			if (key == null || m_dataGridView == null)
				return;

			// Get the column widths for non auto-fill columns by getting their column widths.
			foreach (DataGridViewColumn col in m_dataGridView.Columns)
			{
				// Skip status columns.
				if (col is CheckGridStatusColumn && ((CheckGridStatusColumn)col).AutoSize)
					continue;

				string value = key.GetValue(GetRegSettingColName(col, false), null) as string;
				int width;
				if (int.TryParse(value, out width))
					col.Width = width;
			}

			// Get the column widths for auto-fill columns by getting their fill weights.
			foreach (DataGridViewColumn col in m_dataGridView.Columns)
			{
				// This will catch those freak times when the reg. has an
				// outrageous value. See TE-7845.
				try
				{
					string value = key.GetValue(GetRegSettingColName(col, true), null) as string;
					float weight;
					if (float.TryParse(value, out weight))
						col.FillWeight = weight;
				}
				catch { }
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetColOrderFromReg(RegistryKey key)
		{
			if (key == null || m_dataGridView == null)
				return;

			string colsDispOrder = key.GetValue("ColumnsDisplayOrder", null) as string;

			if (!string.IsNullOrEmpty(colsDispOrder))
			{
				string[] indexes = colsDispOrder.Split(',');
				for (int i = 0; i < indexes.Length; i++)
				{
					int dispIndex;
					if (int.TryParse(indexes[i], out dispIndex) && i < m_dataGridView.ColumnCount)
						m_dataGridView.Columns[i].DisplayIndex = dispIndex;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected static string GetRegSettingColName(DataGridViewColumn col, bool forFillWeight)
		{
			string niceName = col.Name;
			if (niceName.StartsWith("m_"))
				niceName = niceName.Substring(2);

			return (forFillWeight ? "ColumnFillWeight" : "ColumnWidth") + niceName;
		}

		#endregion

		#region Sort method
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified column.
		/// </summary>
		/// <param name="column">The column on which to sort. When this is the column that
		/// is already the sorted column, then use toggleDirection to specify whether or
		/// not the column should be sorted in reverse order.</param>
		/// <param name="toggleDirection">true to toggle the sort direction when column is
		/// already the current sort column. Otherwise, false. When column is not the current
		/// sort column, then this flag is ignored.
		/// </param>
		/// <param name="defaultSortCol">The index of the column to sort on when there
		/// isn't any sort information that can be read from the registry.</param>
		/// ------------------------------------------------------------------------------------
		protected void Sort(DataGridViewColumn column, bool toggleDirection, int defaultSortCol)
		{
			if (m_dataGridView.Columns.Count == 0)
				return;

			using (new WaitCursor(Parent))
			{
				object prevSelectedRow = GetPreSortRow();

				// If the sort column is specified, then add that column to the sort order
				// and sort. If the column is not specified, then sort on the sort order
				// saved in the registry. If no values are found in the registry, then sort
				// on the reference column.
				if (column != null)
					m_gridSorter.Sort(column.DataPropertyName, toggleDirection);
				else if (m_persistence != null && !m_gridSorter.ReadSortInfoFromReg(m_persistence.SettingsKey))
				{
					column = m_dataGridView.Columns[defaultSortCol];
					m_gridSorter.Sort(column.DataPropertyName, toggleDirection);
				}
				else
				{
					m_gridSorter.Sort();

					// Find the column associated with the primary sort
					// field (i.e. the first field in the sort order.
					foreach (DataGridViewColumn col in m_dataGridView.Columns)
					{
						if (col.DataPropertyName == m_gridSorter.PrimarySortProperty)
						{
							column = col;
							break;
						}
					}
				}

				if (m_persistence != null)
					m_gridSorter.WriteSortInfoToReg(m_persistence.SettingsKey);

				// Force the grid to repaint now that the list is reordered.
				m_dataGridView.Refresh();

				// Update the glyph on the heading of the sorted column.
				column.HeaderCell.SortGlyphDirection = m_gridSorter.PrimarySortDirection;

				m_sortedColumn = column;

				// Need to clear the SortGlyph on the other columns
				foreach (DataGridViewColumn otherColumn in m_dataGridView.Columns)
				{
					if (otherColumn != column)
						otherColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
				}

				// Select the row that was previously selected.
				RestorePreSortRow(prevSelectedRow);
			}

			m_dataGridView.Cursor = Cursors.Default;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method should be overridden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual object GetPreSortRow()
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method should be overridden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void RestorePreSortRow(object restoreRow)
		{
		}

		#endregion

		#region IVwNotifyChange Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
		}

		#endregion
	}
}
