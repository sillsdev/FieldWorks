using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using ICSharpCode;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Text;
using System.IO;
using System.ServiceProcess;
using Microsoft.Win32;


namespace dbmt
{
	/// <summary>
	/// Summary description for Globals.
	/// </summary>
	public class Globals
	{
		static public string ksRegKeyRoot = @"Software\Darrell Zook\dbmt";
		static public string ksAppTitle = "Database Maintenance Tool";

		static private frmMain m_oMainForm = null;
		static public frmMain MainForm
		{
			get { return m_oMainForm; }
			set { m_oMainForm = value; }
		}

		static private string m_sGridNullValue = "[NULL]";
		static public string GridNullValue
		{
			get { return m_sGridNullValue; }
			set { m_sGridNullValue = value; }
		}

		static private int m_cTotalWindows = 0;
		static public int IncrementWindows()
		{
			return ++m_cTotalWindows;
		}

		/*static public void MaximizeMdiChild(System.Windows.Forms.Form oForm)
		{
			if (oForm.MdiParent != null)
				oForm.Bounds = GetClientRectangle(oForm.MdiParent);
		}

		static public Rectangle GetClientRectangle(System.Windows.Forms.Form oForm)
		{
			Rectangle rc = oForm.ClientRectangle;
			rc.Width -= SystemInformation.FrameBorderSize.Width;
			rc.Height -= SystemInformation.FrameBorderSize.Height;
			foreach (Control oControl in oForm.Controls)
			{
				if (oControl is ToolBar || oControl is StatusBar)
					rc.Height -= oControl.Height;
			}
			return rc;
		}*/

		static public bool OpenConnection(Form oParent, string sUsername, string sServer,
			string sConnectionString, bool fAutoStart, string sDatabaseName)
		{
			SqlConnection oConn = new SqlConnection(sConnectionString);

			if (fAutoStart)
			{
				string sServiceName = "MSSQLSERVER";
				string sMachineName = sServer;
				try
				{
					int ichSlash = sMachineName.IndexOf("\\");
					if (ichSlash != -1)
					{
						sServiceName = "MSSQL$" + sMachineName.Substring(ichSlash + 1);
						sMachineName = sMachineName.Substring(0, ichSlash);
					}

					ServiceController oController = new ServiceController(sServiceName, sMachineName);
					if (oController.Status == ServiceControllerStatus.Stopped)
					{
						oController.Start();
						try
						{
							oController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
							System.Threading.Thread.Sleep(2000); // Wait 2 seconds.
						}
						catch (System.ServiceProcess.TimeoutException oEx)
						{
							MessageBox.Show(oParent,
								"A timeout occurred while waiting for SQL Server on " + sMachineName + " to start.",
								Globals.ksAppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
							string s = oEx.ToString();
						}
					}
				}
				catch (Exception oEx)
				{
					MessageBox.Show(oParent,
						"Unable to retrieve the status of SQL Server on " + sMachineName + ":\n\n" +
						oEx.Message, Globals.ksAppTitle,
						MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
			}

			try
			{
				oConn.Open();

				if (oConn.Database.ToLower() != sDatabaseName.ToLower())
				{
					try
					{
						oConn.ChangeDatabase(sDatabaseName);
					}
					catch (Exception oEx)
					{
						MessageBox.Show(oParent,
							"Unable to connect to database " + sDatabaseName + ". The current database is " + oConn.Database + ".",
							Globals.ksAppTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				}

				frmConnection oForm = new frmConnection(oConn, sUsername);
				oForm.MdiParent = Globals.MainForm;
				oForm.Show();

				return true;
			}
			catch (Exception oEx)
			{
				MessageBox.Show(oParent,
					"Unable to connect to server " + sServer + ":\n\n" +
					oEx.Message, Globals.ksAppTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
		}

		static public void LoadAppSettings()
		{
			RegistryKey oKey = null;

			try
			{
				oKey = Registry.CurrentUser.OpenSubKey(Globals.ksRegKeyRoot);

				if (oKey == null)
					return;

				GridNullValue = oKey.GetValue("GridNullValue", "[NULL]").ToString();
			}
			finally
			{
				if (oKey != null)
					oKey.Close();
			}
		}

		public Globals()
		{
		}

		/*static bool WriteUTF16toANSIFile(string sFilename, string sContents)
		{
			try
			{
				bool fInvalid = false;

				int cch = sContents.Length;
				char [] rgchContents = sContents.ToCharArray();
				byte [] rgbContents = new byte[cch];
				for (int ich = 0; ich < cch; ich++)
				{
					char ch = rgchContents[ich];
					if (ch > 0xFF)
						fInvalid = true;
					rgbContents[ich] = (byte)ch;
				}

				FileStream oStream = File.OpenWrite(sFilename);
				oStream.Write(rgbContents, 0, cch);
				oStream.Close();

				if (fInvalid)
				{
					MessageBox.Show(this,
						"The file contained some characters that could not be " +
						"represented in an ANSI file. The top byte of these characters was ignored.",
						Globals.ksAppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return true;
			}
			catch (Exception oEx)
			{
				MessageBox.Show(this,
					"An error occurred while saving the file.\n\n" +
					oEx.ToString(), Globals.ksAppTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
		}

		static bool WriteUTF16toUTF8File(HANDLE hFile, HWND hwndProgress)
		{
			UINT cpr;
			UINT cch;
			UINT cchDst;
			DWORD dwBytesWritten;
			char rgchBuffer[6144];
			char * prgchBuffer;
			char * pFileOffsetPos = 0;
			DocPart * pdp = m_pdpFirst;

			while (pdp)
			{
				cpr = pdp->cpr;
				for (UINT ipr = 0; ipr < cpr; ipr++)
				{
					cch = CharsInPara(pdp, ipr);
					if (cch <= 1024)
						prgchBuffer = rgchBuffer;
					else if (!(prgchBuffer = new char[cch * 6]))
						return false;
					cchDst = UTF16ToUTF8((wchar *)pdp->rgpv[ipr], cch, prgchBuffer);
					if (cchDst > 0)
					{
						WriteFile(hFile, prgchBuffer, cchDst, &dwBytesWritten, NULL);
						if (cchDst == cch)
						{
							// The paragraph contained only ANSI characters.
							if (IsParaInMem(pdp, ipr))
								delete pdp->rgpv[ipr];
							pdp->rgpv[ipr] = pFileOffsetPos;
							pdp->rgcch[ipr] &= ~kpmParaInMem;
						}
						else
						{
							if (!IsParaInMem(pdp, ipr))
							{
								wchar * prgwchBuffer;
								if (!(prgwchBuffer = new wchar[cch]))
								{
									if (prgchBuffer != rgchBuffer)
										delete prgchBuffer;
									return false;
								}
								memmove(prgwchBuffer, pdp->rgpv[ipr], cch << 1);
								pdp->rgpv[ipr] = prgwchBuffer;
								pdp->rgcch[ipr] |= kpmParaInMem;
							}
						}
						if (prgchBuffer != rgchBuffer)
							delete prgchBuffer;
					}
					else
					{
						if (prgchBuffer != rgchBuffer)
							delete prgchBuffer;
						return false;
					}
					pFileOffsetPos += cchDst;
				}
				SendMessage(hwndProgress, PBM_DELTAPOS, pdp->cch, 0);
				pdp = pdp->pdpNext;
			}
			return true;
		}*/

		static public bool SaveFile(string sFilename, string sContents,
			System.Text.Encoding eEncoding)
		{
			try
			{
				StreamWriter oWriter = new StreamWriter(sFilename, false, eEncoding);
				oWriter.Write(sContents);
				oWriter.Close();
				return true;
			}
			catch (Exception oEx)
			{
				MessageBox.Show(Globals.MainForm,
					"An error occurred while saving the file.\n\n" +
					oEx.ToString(), Globals.ksAppTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
		}
	}


	public class CodeTextBox : System.Windows.Forms.Panel
	{
		protected ICSharpCode.TextEditor.TextEditorControl m_oEditor;
		private ICSharpCode.TextEditor.Document.HighlightRuleSet m_oRuleSet;
		private string m_sText;

		public bool CanUndo
		{
			get { return m_oEditor.Document.UndoStack.CanUndo; }
		}

		public bool CanRedo
		{
			get { return m_oEditor.Document.UndoStack.CanRedo; }
		}

		public event EventHandler PositionChanged;
		protected virtual void OnPositionChanged(EventArgs e)
		{
			if (PositionChanged != null)
				PositionChanged(this, e);
		}

		public event EventHandler DocumentChanged;
		protected virtual void OnDocumentChanged(EventArgs e)
		{
			if (DocumentChanged != null)
				DocumentChanged(this, e);
		}

		public CodeTextBox()
		{
			m_oEditor = new ICSharpCode.TextEditor.TextEditorControl();
			m_oEditor.Dock = DockStyle.Fill;

			m_oEditor.AllowCaretBeyondEOL = false;
			m_oEditor.ShowEOLMarkers = false;
			m_oEditor.ShowHRuler = false;
			m_oEditor.ShowLineNumbers = false;
			m_oEditor.ShowSpaces = false;
			m_oEditor.ShowTabs = false;
			m_oEditor.ShowVRuler = false;
			m_oEditor.ShowInvalidLines = false;
			m_oEditor.TextEditorProperties.IsIconBarVisible = false;
			m_oEditor.TextEditorProperties.LineViewerStyle = LineViewerStyle.None;
			m_oEditor.EnableFolding = false;
			m_oEditor.ShowMatchingBracket = false;

			TextArea oTextArea = m_oEditor.ActiveTextAreaControl.TextArea;
			oTextArea.MouseDown += new MouseEventHandler(TextArea_MouseDown);
			oTextArea.MouseUp += new MouseEventHandler(TextArea_MouseUp);
			oTextArea.MouseMove += new MouseEventHandler(TextArea_MouseMove);
			oTextArea.KeyDown += new System.Windows.Forms.KeyEventHandler(TextArea_KeyDown);
			oTextArea.KeyPress += new KeyPressEventHandler(TextArea_KeyPress);
			oTextArea.KeyUp += new System.Windows.Forms.KeyEventHandler(TextArea_KeyUp);
			oTextArea.DragEnter += new DragEventHandler(TextArea_DragEnter);
			oTextArea.DragLeave += new EventHandler(TextArea_DragLeave);
			oTextArea.DragDrop += new DragEventHandler(TextArea_DragDrop);
			oTextArea.DragOver += new DragEventHandler(TextArea_DragOver);

			m_oEditor.ActiveTextAreaControl.Caret.PositionChanged += new EventHandler(Caret_PositionChanged);
			m_oEditor.Document.DocumentChanged += new DocumentEventHandler(Document_DocumentChanged);

			// This is required for data binding to work correctly.
			m_oEditor.Validating += new CancelEventHandler(m_oEditor_Validating);

			this.Controls.Add(m_oEditor);
		}

		public override Font Font
		{
			get { return m_oEditor.Font; }
			set { m_oEditor.Font = value; }
		}

		[DefaultValue("")]
		public override string Text
		{
			get { return m_sText; }
			set
			{
				// Don't change the Text property if the text hasn't changed.
				// This keeps the textbox from scrolling when edits are saved.
				if (m_oEditor.Text != value)
					m_oEditor.Text = value;
				else
					m_sText = value;
			}
		}

		[DefaultValue(0)]
		public int CurrentLine
		{
			get { return m_oEditor.ActiveTextAreaControl.Caret.Line; }
		}

		[DefaultValue(0)]
		public int CurrentCol
		{
			get { return m_oEditor.ActiveTextAreaControl.Caret.Column; }
		}

		[DefaultValue(0)]
		public int SelectionStart
		{
			get { return m_oEditor.ActiveTextAreaControl.Caret.Offset; }
		}

		[DefaultValue(0)]
		public int SelectionLength
		{
			get { return m_oEditor.ActiveTextAreaControl.SelectionManager.SelectedText.Length; }
		}

		[DefaultValue("")]
		public string SelectedText
		{
			get { return m_oEditor.ActiveTextAreaControl.SelectionManager.SelectedText; }
			set { m_oEditor.Document.Replace(SelectionStart, SelectionLength, value); }
		}

		[DefaultValue(false)]
		public bool ReadOnly
		{
			get { return m_oEditor.IsReadOnly; }
			set { m_oEditor.IsReadOnly = value; }
		}

		public void SetSelection(int ich)
		{
			SetSelection(ich, 0);
		}

		public void SetSelection(int ich, int cch)
		{
			int cchMax = m_oEditor.Document.TextLength;
			if (ich > cchMax)
				ich = cchMax;
			if (ich + cch > cchMax)
				cch = cchMax - ich;

			Point ptStart = m_oEditor.Document.OffsetToPosition(ich);
			Point ptStop = (cch == 0) ? ptStart : m_oEditor.Document.OffsetToPosition(ich + cch);
			m_oEditor.ActiveTextAreaControl.SelectionManager.SetSelection(
				new DefaultSelection(m_oEditor.Document, ptStart, ptStop));
			m_oEditor.ActiveTextAreaControl.Caret.Position = ptStart;
		}

		public void SaveFile(string sFilename)
		{
			m_oEditor.SaveFile(sFilename);
		}

		public void Find(string sFindText)
		{
			int ich = m_oEditor.Text.IndexOf(sFindText);
			if (ich == -1)
				return;
			SetSelection(ich, sFindText.Length);
		}

		public void AddKeyword(string sWord, Color clr, bool fBold, bool fItalic)
		{
			if (m_oRuleSet == null)
				m_oRuleSet = m_oEditor.Document.HighlightingStrategy.GetRuleSet(null);
			if (m_oRuleSet != null)
				m_oRuleSet.KeyWords[sWord] = new HighlightColor(clr, fBold, fItalic);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			m_oEditor.Focus();
		}

		private void m_oEditor_Validating(object sender, CancelEventArgs e)
		{
			// This is required for data binding to work correctly.
			m_sText = m_oEditor.Text;
			OnValidating(e);
		}

		private void Caret_PositionChanged(object sender, EventArgs e)
		{
			OnPositionChanged(e);
		}

		private void TextArea_MouseDown(object sender, MouseEventArgs e)
		{
			OnMouseDown(e);
		}

		private void TextArea_MouseUp(object sender, MouseEventArgs e)
		{
			OnMouseUp(e);
		}

		private void TextArea_MouseMove(object sender, MouseEventArgs e)
		{
			OnMouseMove(e);
		}

		private void TextArea_KeyDown(object sender, KeyEventArgs e)
		{
			OnKeyDown(e);
		}

		private void TextArea_KeyPress(object sender, KeyPressEventArgs e)
		{
			OnKeyPress(e);
		}

		private void TextArea_KeyUp(object sender, KeyEventArgs e)
		{
			OnKeyUp(e);
		}

		private void Document_DocumentChanged(object sender, DocumentEventArgs e)
		{
			m_sText = m_oEditor.Text;
			OnDocumentChanged(e);
		}

		private void TextArea_DragEnter(object sender, DragEventArgs e)
		{
			OnDragEnter(e);
		}

		private void TextArea_DragLeave(object sender, EventArgs e)
		{
			OnDragLeave(e);
		}

		private void TextArea_DragDrop(object sender, DragEventArgs e)
		{
			OnDragDrop(e);
		}

		private void TextArea_DragOver(object sender, DragEventArgs e)
		{
			OnDragOver(e);
		}
	}


	/// <summary>
	/// A control derived from <see cref="CodeTextBox"/>, setting the HighlightingStrategy to VBNET.
	/// </summary>
	public class SqlTextBox : CodeTextBox
	{
		/// <summary>
		/// Initializes a new instance of <see cref="VBCodeTextBox"/>.
		/// </summary>
		public SqlTextBox()
		{
			m_oEditor.Document.HighlightingStrategy = HighlightingManager.Manager.FindHighlighterForFile("test.sql");
		}
	}


	public class DataGridReadOnlyColumn : System.Windows.Forms.DataGridTextBoxColumn
	{
		public DataGridReadOnlyColumn()
		{
			base.ReadOnly = true;
		}

		[DefaultValue(true)]
		public override bool ReadOnly
		{
			get { return true; }
			set { base.ReadOnly = true; }
		}

		protected override void Edit(System.Windows.Forms.CurrencyManager source, int rowNum, System.Drawing.Rectangle bounds, bool readOnly, string instantText, bool cellIsVisible)
		{
			// Don't call the base class here so the text box doesn't get created.
			return;
		}

		protected override void Paint(Graphics g, Rectangle bounds, CurrencyManager source, int rowNum, Brush backBrush, Brush foreBrush, bool alignToRight)
		{
			DataGrid oGrid = this.DataGridTableStyle.DataGrid;
			NumberedDataGrid oNumGrid = oGrid as NumberedDataGrid;
			int iCol = -1;
			bool fHilighted = false;
			if (oNumGrid != null)
			{
				oNumGrid.SetRowPaintIndex(rowNum);
				DataTable oTable = oNumGrid.GetDataTableFromDataSource();
				if (oTable != null)
				{
					DataColumn oCol = oTable.Columns[this.MappingName];
					if (oCol != null)
						iCol = oCol.Ordinal;
				}
				if (oNumGrid.AreColumnsSelected())
				{
					fHilighted = oNumGrid.IsColumnSelected(iCol);
				}
				else
				{
					fHilighted = oGrid.IsSelected(rowNum) ||
						(source.Position == rowNum && oGrid.CurrentCell.ColumnNumber == iCol);
				}
			}
			else
			{
				fHilighted = oGrid.IsSelected(rowNum) ||
					(source.Position == rowNum && oGrid.CurrentCell.ColumnNumber == iCol);
			}

			if (fHilighted)
			{
				foreBrush = SystemBrushes.HighlightText;
				backBrush = SystemBrushes.Highlight;
			}
			else
			{
				foreBrush = SystemBrushes.WindowText;
				backBrush = SystemBrushes.Window;
			}
			base.Paint(g, bounds, source, rowNum, backBrush, foreBrush, alignToRight);
		}
	}

	public class ByteConvert
	{
		static private char[] m_hexDigits = {
												'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

		static public string ToHexString(byte[] bytes)
		{
			char[] chars = new char[bytes.Length * 2];
			for (int i = 0; i < bytes.Length; i++)
			{
				int b = bytes[i];
				chars[i * 2] = m_hexDigits[b >> 4];
				chars[i * 2 + 1] = m_hexDigits[b & 0xF];
			}
			return "0x" + new string(chars);
		}
	}

	public class DataGridReadOnlyByteColumn : DataGridReadOnlyColumn
	{
		protected override object GetColumnValueAtRow(CurrencyManager source, int rowNum)
		{
			object o = base.GetColumnValueAtRow(source, rowNum);
			if (o.GetType() == Type.GetType("System.Byte[]"))
				return ByteConvert.ToHexString((byte[])o);
			return o;
		}

		protected override void Paint(Graphics g, Rectangle bounds, CurrencyManager source, int rowNum, Brush backBrush, Brush foreBrush, bool alignToRight)
		{
			NumberedDataGrid oGrid = this.DataGridTableStyle.DataGrid as NumberedDataGrid;
			if (oGrid != null)
				oGrid.SetRowPaintIndex(rowNum);
			base.Paint(g, bounds, source, rowNum, backBrush, foreBrush, alignToRight);
		}
	}


	public class NumberedDataGrid : System.Windows.Forms.DataGrid
	{
		private int m_iFirstRow;

		private int m_iLastColClicked = -1;
		private SortedList m_slSelColumns = new SortedList();
		private bool m_fInColClick = false;

		public bool AreColumnsSelected()
		{
			return m_slSelColumns.Count > 0;
		}

		public bool IsColumnSelected(int iColumn)
		{
			return m_slSelColumns.ContainsKey(iColumn);
		}

		public void SetRowPaintIndex(int iRow)
		{
			if (m_iFirstRow == -1)
				m_iFirstRow = iRow;
		}

		public DataTable GetDataTableFromDataSource()
		{
			if (this.DataSource is DataTable)
				return (DataTable)this.DataSource;
			else if (this.DataSource is DataView)
				return ((DataView)this.DataSource).Table;
			else
				return null;
		}

		protected System.Windows.Forms.DataGridTableStyle GetCurrentTableStyle()
		{
			object oDataSource = GetDataTableFromDataSource();

			if (oDataSource == null)
				return null;

			string strMappingName = ((DataTable)oDataSource).TableName;

			foreach (System.Windows.Forms.DataGridTableStyle oStyle in this.TableStyles)
			{
				if (oStyle.MappingName == strMappingName)
					return oStyle;
			}

			return null;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			try
			{
				// This will get set to the first row by the columns calling the
				// SetRowPaintIndex method.
				m_iFirstRow = -1;

				base.OnPaint(e);

				if (DesignMode)
					return;

				DataGridTableStyle oTableStyle = this.GetCurrentTableStyle();
				if (oTableStyle == null || oTableStyle.RowHeadersVisible == false)
					return;

				DataTable oTable = this.GetDataTableFromDataSource();
				int cMaxRows = oTable.Rows.Count;
				if (cMaxRows == 0)
					return;

				int iFirstRow = m_iFirstRow + 1;
				int cLastRow = iFirstRow + this.VisibleRowCount;
				if (cLastRow > cMaxRows)
					cLastRow = cMaxRows + 1;

				int dpxLargestWidth = 0;
				if (cMaxRows == 0)
				{
					dpxLargestWidth = 14;
				}
				else
				{
					Rectangle oRectFirstCell = this.GetCellBounds(0, 0);

					for (int iRow = iFirstRow; iRow < cLastRow; iRow++)
					{
						int dpxWidth = (int)e.Graphics.MeasureString("   " + iRow, this.Font, 999999, StringFormat.GenericTypographic).Width;
						if (dpxLargestWidth < dpxWidth)
							dpxLargestWidth = dpxWidth;
					}
				}

				if (oTableStyle.RowHeaderWidth != dpxLargestWidth)
				{
					oTableStyle.RowHeaderWidth = dpxLargestWidth;
					this.RowHeaderWidth = dpxLargestWidth;
				}

				Rectangle oRectF = this.ClientRectangle;
				oRectF.Inflate(-SystemInformation.BorderSize.Width * 2, -SystemInformation.BorderSize.Height * 2);
				e.Graphics.SetClip(oRectF);

				for (int iRow = iFirstRow; iRow < cLastRow; iRow++)
				{
					Rectangle oRect = this.GetCellBounds(iRow - 1, 0);

					oRect.X = 2;
					oRect.Width = dpxLargestWidth - 2;
					e.Graphics.FillRectangle(SystemBrushes.Control, oRect);

					oRect.Y += 2;
					e.Graphics.DrawString(" " + iRow.ToString(), this.Font, Brushes.Black, oRect, StringFormat.GenericTypographic);
				}
			}
			catch
			{
				// Do nothing.
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if ((Control.ModifierKeys & (Keys.Control | Keys.Shift)) == 0)
			{
				m_iLastColClicked = -1;
				m_slSelColumns.Clear();
			}

			HitTestInfo hti = this.HitTest(e.X, e.Y);
			if (hti.Type == HitTestType.ColumnHeader)
			{
				m_fInColClick = true;
				int iColumn = hti.Column;
				this.CurrentCell = new DataGridCell(0, iColumn);
				if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift && m_iLastColClicked != -1)
				{
					// Add all the columns between the last column and this column.
					if (m_iLastColClicked >= iColumn)
					{
						for (int i = m_iLastColClicked; --i >= iColumn; )
						{
							if (!m_slSelColumns.ContainsKey(i))
								m_slSelColumns.Add(i, i);
						}
					}
					else
					{
						for (int i = m_iLastColClicked; ++i <= iColumn; )
						{
							if (!m_slSelColumns.ContainsKey(i))
								m_slSelColumns.Add(i, i);
						}
					}
				}
				else
				{
					if (m_slSelColumns.ContainsKey(iColumn))
						m_slSelColumns.Remove(iColumn);
					else
						m_slSelColumns.Add(iColumn, iColumn);
				}
				m_iLastColClicked = iColumn;
				m_fInColClick = false;
				this.Invalidate();
			}
			else if (hti.Type == HitTestType.None)
			{
				DataGridTableStyle oTableStyle = this.GetCurrentTableStyle();
				if (oTableStyle != null && e.X <= oTableStyle.RowHeaderWidth)
					this.SelectAll();
			}
		}

		protected override void OnCurrentCellChanged(EventArgs e)
		{
			if (!m_fInColClick)
			{
				m_slSelColumns.Clear();
				// Redraw the whole grid so that the previous hilighted column is unhilighted.
				this.Invalidate();
			}
			base.OnCurrentCellChanged(e);
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == (Keys.C | Keys.Control))
				return !CopyToClipboard();
			if (keyData == (Keys.A | Keys.Control))
				return !SelectAll();
			return base.ProcessDialogKey(keyData);
		}

		public bool CopyToClipboard()
		{
			try
			{
				Cursor.Current = Cursors.WaitCursor;

				DataGridTableStyle oTableStyle = this.GetCurrentTableStyle();
				DataTable oDataTable = this.GetDataTableFromDataSource();
				if (oTableStyle == null || oDataTable == null)
					return false;

				int iCol, cCols;

				if (m_slSelColumns.Count > 0)
				{
					// Copy the selected columns to the clipboard.
					cCols = m_slSelColumns.Count;

					DataGridColumnStyle [] oCols = new DataGridColumnStyle[cCols];
					int [] iTableCols = new int[cCols];
					iCol = 0;
					foreach (object oKey in m_slSelColumns.Keys)
					{
						oCols[iCol] = oTableStyle.GridColumnStyles[(int)oKey];
						string sMappingName = oCols[iCol].MappingName;
						iTableCols[iCol++] = oDataTable.Columns[sMappingName].Ordinal;
					}

					StringBuilder oBuilder = new StringBuilder();

					// Get the column headings.
					for (iCol = 0; iCol < cCols; iCol++)
					{
						if (iCol == 0)
							oBuilder.Append(oCols[iCol].MappingName);
						else
							oBuilder.Append("\t" + oCols[iCol].MappingName);
					}
					oBuilder.Append("\r\n");

					// Loop through each row and add it to the string builder.
					foreach (DataRow oDataRow in oDataTable.Rows)
					{
						for (iCol = 0; iCol < cCols; iCol++)
						{
							if (iCol == 0)
								oBuilder.Append(GetCellValue(oDataRow[iTableCols[iCol]]));
							else
								oBuilder.Append("\t" + GetCellValue(oDataRow[iTableCols[iCol]]));
						}
						oBuilder.Append("\r\n");
					}

					Clipboard.SetDataObject(oBuilder.ToString(), true);
				}
				else
				{
					ArrayList oSelectedRows = GetSelectedRows();
					if (oSelectedRows.Count == 0)
					{
						// Copy the current cell to the clipboard.
						string sValue = GetCellValue(oDataTable.DefaultView[this.CurrentCell.RowNumber][this.CurrentCell.ColumnNumber]);
						Clipboard.SetDataObject(sValue, true);
					}
					else
					{
						// Copy the selected rows to the clipboard, including the column headers.

						GridColumnStylesCollection oColStyles = oTableStyle.GridColumnStyles;
						cCols = oColStyles.Count;

						StringBuilder oBuilder = new StringBuilder();

						// Get the column headings.
						for (iCol = 0; iCol < cCols; iCol++)
						{
							if (iCol == 0)
								oBuilder.Append(oColStyles[iCol].MappingName);
							else
								oBuilder.Append("\t" + oColStyles[iCol].MappingName);
						}
						oBuilder.Append("\r\n");

						// Loop through each row and add it to the string builder.
						foreach (DataRow oDataRow in oSelectedRows)
						{
							for (iCol = 0; iCol < cCols; iCol++)
							{
								if (iCol == 0)
									oBuilder.Append(GetCellValue(oDataRow[iCol]));
								else
									oBuilder.Append("\t" + GetCellValue(oDataRow[iCol]));
							}
							oBuilder.Append("\r\n");
						}

						Clipboard.SetDataObject(oBuilder.ToString(), true);
					}
				}
				Cursor.Current = Cursors.Default;
				return true;
			}
			catch (Exception oEx)
			{
				Cursor.Current = Cursors.Default;
				MessageBox.Show(this,
					"An error occurred while copying the selected text to the clipboard.\n\n" +
					oEx.ToString(), Globals.ksAppTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
		}

		public bool SelectAll()
		{
			m_slSelColumns.Clear();
			DataGridTableStyle oTableStyle = this.GetCurrentTableStyle();
			if (oTableStyle == null)
				return false;

			int cCol = oTableStyle.GridColumnStyles.Count;
			for (int iCol = 0; iCol < cCol; iCol++)
				m_slSelColumns.Add(iCol, iCol);

			this.Invalidate();

			return true;
		}

		public ArrayList GetSelectedRows()
		{
			ArrayList oRows = new ArrayList();
			DataTable oDataTable = this.GetDataTableFromDataSource();
			if (oDataTable != null)
			{
				DataView oView = oDataTable.DefaultView;
				int cRows = oView.Count;
				for (int iRow = 0; iRow < cRows; iRow++)
				{
					if (this.IsSelected(iRow))
						oRows.Add(oView[iRow].Row);
				}
			}
			return oRows;
		}

		private string GetCellValue(object oValue)
		{
			byte [] oByte = oValue as byte [];
			if (oByte == null)
				return oValue.ToString();
			return ByteConvert.ToHexString(oByte);
		}
	}

	public class ComboBoxEx : ComboBox
	{
		private class ComboBoxExItem
		{
			private string _text;
			public string Text
			{
				get {return _text;}
				set {_text = value;}
			}

			private int _imageIndex;
			public int ImageIndex
			{
				get {return _imageIndex;}
				set {_imageIndex = value;}
			}

			public ComboBoxExItem()
				: this("")
			{
			}

			public ComboBoxExItem(string text)
				: this(text, -1)
			{
			}

			public ComboBoxExItem(string text, int imageIndex)
			{
				_text = text;
				_imageIndex = imageIndex;
			}

			public override string ToString()
			{
				return _text;
			}
		}

		private ImageList m_imageList;

		public ImageList ImageList
		{
			get { return m_imageList; }
			set
			{
				m_imageList = value;
				if (m_imageList != null)
					this.ItemHeight = m_imageList.ImageSize.Height;
			}
		}

		public ComboBoxEx()
		{
			DrawMode = DrawMode.OwnerDrawFixed;
		}

		public int AddItem(string text, int imageIndex)
		{
			return Items.Add(new ComboBoxExItem(text, imageIndex));
		}

		protected override void OnDrawItem(DrawItemEventArgs ea)
		{
			ea.DrawBackground();
			ea.DrawFocusRectangle();

			Size imageSize = m_imageList == null ? new Size(0, 0) : m_imageList.ImageSize;
			Rectangle bounds = ea.Bounds;

			int xp = bounds.Left;
			int yp = bounds.Top;
			string sText = this.Text;

			if (ea.Index != -1)
			{
				object oItem = Items[ea.Index];
				if (oItem is ComboBoxExItem)
				{
					ComboBoxExItem oItemEx = (ComboBoxExItem)oItem;
					sText = oItemEx.Text;

					if (oItemEx.ImageIndex != -1)
					{
						m_imageList.Draw(ea.Graphics, bounds.Left, bounds.Top, oItemEx.ImageIndex);
						xp += imageSize.Width;
						int dypText = (int)ea.Graphics.MeasureString(sText, ea.Font).Height;
						yp += ((imageSize.Height - dypText) / 2);
					}
				}
				else
				{
					sText = this.Items[ea.Index].ToString();
				}
			}

			ea.Graphics.DrawString(sText, ea.Font, new SolidBrush(ea.ForeColor), xp, yp);

			base.OnDrawItem(ea);
		}
	}
}