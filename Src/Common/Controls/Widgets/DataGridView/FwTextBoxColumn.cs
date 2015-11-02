// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwTextBoxColumn.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A column in a DataGridView that consists of FwTextBoxCells.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwTextBoxColumn : DataGridViewColumn
	{
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected FwTextBoxControl m_textBoxControl;
		/// <summary></summary>
		protected int m_ws;
		/// <summary></summary>
		protected Dictionary<string, Font> m_fontCache = new Dictionary<string,Font>();
		/// <summary></summary>
		private FwStyleSheet m_styleSheet;
		private bool m_DisposeCellTemplate;

		private float m_szOfFontAt100Pcnt = 10f;
		private readonly bool m_rowsAreMultiLing;
		private bool m_useTextPropsFontForCell;

		#region Constructor and Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwTextBoxColumn"/> class.
		/// </summary>
		/// <remarks>Used by Designer</remarks>
		/// ------------------------------------------------------------------------------------
		public FwTextBoxColumn() : this(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwTextBoxColumn"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FwTextBoxCell gets disposed in Dispose()")]
		public FwTextBoxColumn(FdoCache cache) : base(new FwTextBoxCell())
		{
			m_DisposeCellTemplate = true;
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwTextBoxColumn"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBoxColumn(FdoCache cache, bool rowsAreMultiLing) : this(cache)
		{
			m_rowsAreMultiLing = rowsAreMultiLing;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">set to <c>true</c> if called from the Dispose() method,
		/// set to <c>false</c> if called by GC. If this parameter is <c>false</c> we shouldn't
		/// access any managed objects since these might already have been destroyed.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			// Debug.WriteLineIf statement disabled because of a bug in .NET DataGridView:
			// DataGridView.AddRange() creates a temporary clone that it doesn't dispose, so we
			// will always get this warning message and we can't do anything about it.
			// Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_textBoxControl != null)
					m_textBoxControl.Dispose();
				if (m_fontCache != null)
				{
					foreach (Font fnt in m_fontCache.Values)
						fnt.Dispose();

					m_fontCache.Clear();
				}
				if (m_DisposeCellTemplate && CellTemplate != null)
				{
					CellTemplate.Dispose();
					CellTemplate = null;
					m_DisposeCellTemplate = false;
				}
			}

			m_textBoxControl = null;
			m_cache = null;
			m_fontCache = null;
			base.Dispose(disposing);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs when the DataGridView compares two cell values to perform a sort operation.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewSortCompareEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnSortCompare(object sender, DataGridViewSortCompareEventArgs e)
		{
			if (e.CellValue1 is ITsString && e.CellValue2 is ITsString)
			{
				if (m_cache != null)
				{
					CoreWritingSystemDefinition ws = GetWritingSystem(e.RowIndex1);
					Debug.Assert(ws == GetWritingSystem(e.RowIndex2));
					e.SortResult = ws.DefaultCollation.Collator.Compare(((ITsString)e.CellValue1).Text, ((ITsString)e.CellValue2).Text);
					e.Handled = true;
					return;
				}

				e.SortResult = ((ITsString)e.CellValue1).Text.CompareTo(
					((ITsString)e.CellValue2).Text);

				e.Handled = true;
				return;
			}

			e.Handled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system.
		/// </summary>
		/// <param name="rowIndex">Index of the row.</param>
		/// <returns>The writing system</returns>
		/// ------------------------------------------------------------------------------------
		private CoreWritingSystemDefinition GetWritingSystem(int rowIndex)
		{
			int ws = (m_rowsAreMultiLing ? GetWritingSystemHandle(rowIndex) : m_ws);
			return m_cache.ServiceLocator.WritingSystemManager.Get(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the cache.
		/// </summary>
		/// <value>The cache.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FdoCache Cache
		{
			get { return m_cache; }
			set
			{
				m_cache = value;
				// Set the internal FwTextBox's writing system factory if we can.  See TE-6969.
				if (m_textBoxControl != null && m_cache != null)
					m_textBoxControl.WritingSystemFactory = m_cache.WritingSystemFactory;
				SetCellStyleAlignment(m_ws, DefaultCellStyle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get { return m_ws; }
			set
			{
				m_ws = value;
				SetCellStyleAlignment(m_ws, DefaultCellStyle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style sheet associated with the column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwStylesheet StyleSheet
		{
			get { return m_styleSheet; }
			set
			{
				m_styleSheet = value as FwStyleSheet;
				if (m_textBoxControl != null)
					m_textBoxControl.StyleSheet = m_styleSheet;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float SizeOfFontAt100Percent
		{
			get { return m_szOfFontAt100Pcnt; }
			set { m_szOfFontAt100Pcnt = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font for the DefaultCellStyle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Font Font
		{
			get { return DefaultCellStyle.Font; }
			set { DefaultCellStyle.Font = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to use the font in the ITextProps
		/// of the ITsString values of each cell in the column. When false, the column's
		/// DefaultCellStyle font is used. Otherwise, the font is derived from the ITsString
		/// in the cell itself.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(false)]
		public bool UseTextPropsFontForCell
		{
			get { return m_useTextPropsFontForCell; }
			set
			{
				m_useTextPropsFontForCell = value;

				if (DataGridView != null)
				{
					DataGridView.CellFormatting -= HandleCellFormatting;
					if (value)
						DataGridView.CellFormatting += HandleCellFormatting;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the cell get's its font from it's ITsString's ITextProps when
		/// m_useTextPropsFontForCell is true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (!m_useTextPropsFontForCell || e.RowIndex < 0 || e.ColumnIndex != Index)
				return;

			int ws = GetWritingSystemHandleWithFallback(e.RowIndex);
			if (ws == 0)
				return;

			Font fnt = GetValuesFont(DataGridView[e.ColumnIndex, e.RowIndex].Value as ITsString, ws);
			if (fnt == null)
				return;

			SetCellStyleAlignment(ws, e.CellStyle);
			e.CellStyle.Font = fnt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Modifies the alignment of the specified cell style based on the specified writing
		/// system's RTL value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetCellStyleAlignment(int ws, DataGridViewCellStyle style)
		{
			if (m_cache == null || ws <= 0 || style == null)
				return;

			if (m_textBoxControl != null)
				m_textBoxControl.WritingSystemCode = ws;
			if (m_cache.ServiceLocator.WritingSystemManager.Get(ws).RightToLeftScript)
			{
				style.Alignment = DataGridViewContentAlignment.MiddleRight;
				if (m_textBoxControl != null)
					m_textBoxControl.RightToLeft = RightToLeft.Yes;
			}
			else
			{
				style.Alignment = DataGridViewContentAlignment.MiddleLeft;
				if (m_textBoxControl != null)
					m_textBoxControl.RightToLeft = RightToLeft.No;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system handle for the column. If that is 0 (i.e., not specified), attempt
		/// to get the writing system from the specified DataGridView row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int GetWritingSystemHandleWithFallback(int rowIndex)
		{
			if (m_ws > 0)
				return m_ws;

			if (DataGridView == null || rowIndex < 0 || rowIndex >= DataGridView.RowCount)
				return 0;

			var row = DataGridView.Rows[rowIndex] as FwTextBoxRow;
			return (row == null || row.WritingSystemHandle == 0 ? 0 : row.WritingSystemHandle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the font for the specified TsString and writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Font GetValuesFont(ITsString tss, int ws)
		{
			if (tss == null || tss.RunCount == 0 || m_cache == null)
				return null;

			ITsTextProps ttp = tss.get_Properties(0);

			// Figure out what the font size of the string should be.
			int var;
			int fontSize = ttp.GetIntPropValues((int)FwTextPropType.ktptFontSize, out var);
			if (fontSize == -1)
				fontSize = FontInfo.kDefaultFontSize;
			else
			{
				// Font size, at this point, is in millipoints, so convert it to points.
				fontSize /= 1000;
			}
			// Figure out what the style name is and use it to get the font face name.
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			string faceName;
			if (m_styleSheet != null)
			{
				// When there is a stylesheet, use it to get the font face name for the style
				// name specified in the text props. If there is no style name in the text props,
				// GetFaceNameFromStyle() should return the font face for the normal style.
				faceName = m_styleSheet.GetFaceNameFromStyle(styleName, ws, m_cache);
			}
			else
			{
				// When there is no stylesheet, use the default serif font for the writing
				// system.
				faceName = m_cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName;
			}

			if (string.IsNullOrEmpty(faceName))
				return null;

			// Check if the font is in our font cache. If not, add it.
			string key = string.Format("{0}{1}", faceName, fontSize);
			Font fnt;
			if (!m_fontCache.TryGetValue(key, out fnt))
			{
				fnt = new Font(faceName, fontSize, GraphicsUnit.Point);
				m_fontCache[key] = fnt;
			}

			return fnt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the template used to create new cells.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.Windows.Forms.DataGridViewCell"></see> that all
		/// other cells in the column are modeled after. The default is null.</returns>
		/// ------------------------------------------------------------------------------------
		public override DataGridViewCell CellTemplate
		{
			get { return base.CellTemplate; }
			set
			{
				if (value != null && !value.GetType().IsAssignableFrom(typeof(FwTextBoxCell)))
					throw new InvalidCastException("Must be a FwTextBoxCell");

				base.CellTemplate = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text box control.
		/// </summary>
		/// <value>The text box control.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FwTextBoxControl TextBoxControl
		{
			get
			{
				if (m_textBoxControl == null)
				{
					m_textBoxControl = new FwTextBoxControl();
					m_textBoxControl.BorderStyle = BorderStyle.None;
					if (m_cache != null)
						m_textBoxControl.WritingSystemFactory = m_cache.WritingSystemFactory;
				}

				return m_textBoxControl;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the band is associated with a different
		/// <see cref="T:System.Windows.Forms.DataGridView"></see>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDataGridViewChanged()
		{
			if (DataGridView != null)
			{
				// In case we already subscribed to the events we want to remove those handlers
				// so that we don't get called twice.
				DataGridView.ColumnWidthChanged -= OnColumnWidthChanged;
				DataGridView.RowHeightChanged -= OnRowHeightChanged;
				DataGridView.SortCompare -= OnSortCompare;
				DataGridView.CellFormatting -= HandleCellFormatting;
			}

			base.OnDataGridViewChanged();

			if (DataGridView != null)
			{
				DataGridView.ColumnWidthChanged += OnColumnWidthChanged;
				DataGridView.RowHeightChanged += OnRowHeightChanged;
				DataGridView.SortCompare += OnSortCompare;
				if (m_useTextPropsFontForCell)
					DataGridView.CellFormatting += HandleCellFormatting;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the column width changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewColumnEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			TextBoxControl.Width = Width;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a row in the grid changes heights. Make sure to adjust the
		/// FwTextBoxControls, if it's showing in the resized row in this column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void OnRowHeightChanged(object sender, DataGridViewRowEventArgs e)
		{
			var grid = sender as DataGridView;
			if (grid != null && grid.CurrentCell != null && grid.IsCurrentCellInEditMode &&
				grid.CurrentCellAddress.X == Index &&
				grid.CurrentCellAddress.Y == e.Row.Index)
			{
				TextBoxControl.Height = e.Row.Height;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the text box control.
		/// </summary>
		/// <param name="fwTxtBox">The FwTextBox control to initialize. When this is null, then
		/// the column's internal FwTextBox (i.e. m_textBoxControl) is initialized.</param>
		/// <param name="tss">The TsString fwTxtBox is initialized to.</param>
		/// <param name="rowIndex">Row whose writing system is used (and whose default value
		/// is used when necessary).</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeTextBoxControl(FwTextBox fwTxtBox, ITsString tss, int rowIndex)
		{
			if (rowIndex < 0 || (TextBoxControl == null && fwTxtBox == null))
				return;

			if (fwTxtBox == null)
				fwTxtBox = m_textBoxControl;

			if (m_cache != null)
				fwTxtBox.WritingSystemFactory =	m_cache.WritingSystemFactory;

			fwTxtBox.Size = new Size(Width, DataGridView.Rows[rowIndex].Height);
			fwTxtBox.StyleSheet = m_styleSheet;
			fwTxtBox.WritingSystemCode = GetWritingSystemHandle(rowIndex);
			fwTxtBox.Tss = tss ?? GetDefaultNewRowValue(rowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default value for a cell in the row for new records.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString GetDefaultNewRowValue(int rowIndex)
		{
			int ws = GetWritingSystemHandle(rowIndex);
			if (ws <= 0)
			{
				ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
				strBldr.Append(string.Empty);
				return strBldr.GetString();
			}

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(string.Empty, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system.
		/// </summary>
		/// <param name="rowIndex">Index of the row.</param>
		/// <returns>The HVO of the writing system</returns>
		/// ------------------------------------------------------------------------------------
		internal int GetWritingSystemHandle(int rowIndex)
		{
			if (m_rowsAreMultiLing && rowIndex >= 0)
			{
				var row = DataGridView.Rows[rowIndex] as FwTextBoxRow;
				if (row != null)
					return row.WritingSystemHandle;
			}

			return m_ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "fnt and family are references")]
		public void SetZoomFactor(float factor)
		{
			if (DefaultCellStyle != null)
			{
				Font fnt = (DefaultCellStyle.Font ?? InheritedStyle.Font);
				if (fnt != null)
				{
					FontFamily family = fnt.FontFamily;
					FontStyle style = fnt.Style;
					DefaultCellStyle.Font =
						new Font(family, m_szOfFontAt100Pcnt * factor, style, GraphicsUnit.Point);
				}
			}
		}
	}
}
