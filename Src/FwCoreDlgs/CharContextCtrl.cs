using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using SILUBS.SharedScrUtils;
using System.IO;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.Utils;
using System.Reflection;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to support displaying the context(s) for a character string.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CharContextCtrl : UserControl
	{
		private const int kiColRef = 0;
		private const int kiColContextBefore = 1;
		private const int kiColContextItem = 2;
		private const int kiColContextAfter = 3;

		/// <summary>When the current scripture project is scanned, then filename is null.</summary>
		public delegate void BeforeTextTokenSubStringsLoadedHandler(string filename);

		/// <summary>Event fired before a data source is
		/// read to get all the token substrings.</summary>
		public event BeforeTextTokenSubStringsLoadedHandler BeforeTextTokenSubStringsLoaded;

		/// <summary></summary>
		public delegate void TextTokenSubStringsLoadedHandler(List<TextTokenSubstring> tokens);

		/// <summary>Event fired when a data source has been
		/// read to get all the token substrings.</summary>
		public event TextTokenSubStringsLoadedHandler TextTokenSubStringsLoaded;

		/// <summary></summary>
		public delegate void GetContextInfoHandler(int index, out string sKey,
			out string sConcordanceItem);

		/// <summary>Event fired when the control needs a
		/// list of context information.</summary>
		public event GetContextInfoHandler GetContextInfo;

		/// <summary>list validator used to remove bogus items from the list of
		/// TextTokenSubstrings returned from the check
		/// </summary>
		public delegate void ValidateList(List<TextTokenSubstring> list);

		private string[] m_fileData;
		private DataGridView m_tokenGrid;
		private string m_scrChecksDllFile;
		private List<ContextInfo> m_currContextInfoList;
		private Dictionary<string, List<ContextInfo>> m_contextInfoLists;
		private FdoCache m_cache;
		private LanguageDefinition m_langDef;
		private int m_gridRowHeight;
		private string m_checkToRun;
		private string m_sListName;
		private string m_sInitialScanMsgLabel;
		private Dictionary<string, string> m_chkParams = new Dictionary<string, string>();
		private string m_currContextItem;
		private ValidateList m_listValidator = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the CharContextCtrl.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharContextCtrl()
		{
			InitializeComponent();
			gridContext.AutoGenerateColumns = false;
			colRef.MinimumWidth = 2;
			colRef.Width = colRef.MinimumWidth;

			gridContext.GridColor = ColorHelper.CalculateColor(SystemColors.WindowText,
				SystemColors.Window, 35);
			m_sInitialScanMsgLabel = lblScanMsg.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the data source combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckForScrProject()
		{
			if (m_cache == null || m_langDef == null)
				return;

			m_scrChecksDllFile = DirectoryFinder.BasicEditorialChecksDll;

			ILgWritingSystemFactory lgwsf = m_cache.LanguageWritingSystemFactoryAccessor;
			string strLocale = m_langDef.HasChangedIcuLocale ? m_langDef.LocaleAbbr :
				m_langDef.IcuLocaleOriginal;
			bool modifyingVernWs =
				lgwsf.GetWsFromStr(strLocale) == m_cache.DefaultVernWs;

			// If TE isn't installed, we can't support creating an inventory
			// based on Scripture data.
			cmnuScanScripture.Visible = (MiscUtils.IsTEInstalled &&
				File.Exists(m_scrChecksDllFile) &&
				m_cache.LangProject.TranslatedScriptureOA != null && modifyingVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the order of the columns for right-to-left text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetRightToLeft()
		{
			colContextAfter.DisplayIndex = colContextBefore.DisplayIndex;
			colContextBefore.DisplayIndex = gridContext.ColumnCount - 1;
		}

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating what is type of data is being displayed in the list. This
		/// should be a localizable string, since it will be used in a message dislayed to the
		/// user. It should use lowercase rather than title case (e.g., "characters",
		/// "punctuation patterns", etc.).
		/// patterns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DisplayedListName
		{
			get { return m_sListName; }
			set { m_sListName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the initial directory to use if the user chooses to scan a file.
		/// </summary>
		/// <value>The initial directory for file scan.</value>
		/// ------------------------------------------------------------------------------------
		public string InitialDirectoryForFileScan
		{
			get { return m_openFileDialog.InitialDirectory; }
			set { m_openFileDialog.InitialDirectory = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scan message label text.
		/// </summary>
		/// <value>The scan message label text.</value>
		/// ------------------------------------------------------------------------------------
		public string ScanMsgLabelText
		{
			get { return lblScanMsg.Text; }
			set { lblScanMsg.Text = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the database cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FdoCache Cache
		{
			get { return m_cache; }
			set
			{
				m_cache = value;
				CheckForScrProject();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the language definition.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public LanguageDefinition LanguageDefinition
		{
			get { return m_langDef; }
			set
			{
				m_langDef = value;
				CheckForScrProject();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the token grid. If a tokenGrid is already in use, it will be cleared first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DataGridView TokenGrid
		{
			get { return m_tokenGrid; }
			set
			{
				if (m_tokenGrid != null)
				{
					m_tokenGrid.RowEnter -= m_tokenGrid_RowEnter;
					m_tokenGrid.RowHeightInfoNeeded -= HandleRowHeightInfoNeeded;
					pnlTokenGrid.Controls.Remove(m_tokenGrid);
				}

				m_tokenGrid = value;

				if (m_tokenGrid != null)
				{
					m_tokenGrid.Dock = DockStyle.Fill;
					pnlTokenGrid.Controls.Add(m_tokenGrid);
					m_tokenGrid.RowHeightInfoNeeded += HandleRowHeightInfoNeeded;
					m_tokenGrid.RowEnter += m_tokenGrid_RowEnter;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Font ContextFont
		{
			get { return colContextAfter.DefaultCellStyle.Font; }
			set
			{
				colContextAfter.DefaultCellStyle.Font = value;
				colContextBefore.DefaultCellStyle.Font = value;
				colContextItem.DefaultCellStyle.Font = value;
				m_gridRowHeight = Math.Max(value.Height, gridContext.Font.Height) + 2;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string CheckToRun
		{
			get { return m_checkToRun; }
			set { m_checkToRun = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the list validator used to remove bogus items from the list of
		/// TextTokenSubstrings returned from the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ValidateList ListValidator
		{
			set { m_listValidator = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Dictionary<string, string> CheckParameters
		{
			get { return m_chkParams; }
			set	{ m_chkParams = (value == null ? new Dictionary<string, string>() : value);	}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not there are any contexts for tokens that
		/// have been loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ContextInfoExists
		{
			get { return (m_contextInfoLists != null && m_contextInfoLists.Count > 0); }
		}
		#endregion

		#region Private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the valid characters for the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ValidCharacters ValidCharacters
		{
			get
			{
				// Get the writing system and valid characters list
				ILgWritingSystemFactory lgwsf = m_cache.LanguageWritingSystemFactoryAccessor;
				return ValidCharacters.Load(lgwsf.get_EngineOrNull(m_cache.DefaultVernWs));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets Unicode character categorizer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private CharacterCategorizer CharacterCategorizer
		{
			get
			{
				ILgCharacterPropertyEngine charPropEngine =
					m_cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(
				m_cache.DefaultVernWs);
				return new FwCharacterCategorizer(ValidCharacters, charPropEngine);
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshContextGrid()
		{
			m_tokenGrid_RowEnter(null,
				new DataGridViewCellEventArgs(m_tokenGrid.CurrentCellAddress.X,
					m_tokenGrid.CurrentCellAddress.Y));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResetContextLists()
		{
			m_contextInfoLists = new Dictionary<string, List<ContextInfo>>();
			m_currContextInfoList = new List<ContextInfo>();
			gridContext.RowCount = 0;
			gridContext.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddContextInfo(ContextInfo contextInfo)
		{
			List<ContextInfo> list;
			if (!m_contextInfoLists.TryGetValue(contextInfo.Key, out list))
			{
				list = new List<ContextInfo>();
				m_contextInfoLists[contextInfo.Key] = list;
			}

			list.Add(contextInfo);
		}
		#endregion

		#region Event handlers and helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the width of the columns so they just fit inside the client area of the grid.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridContext_ClientSizeChanged(object sender, EventArgs e)
		{
			if (m_tokenGrid != null && m_tokenGrid.CurrentRow != null)
				AdjustContextGridColumnWidths();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the column widths in the context grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdjustContextGridColumnWidths()
		{
			if (m_currContextItem == null || gridContext.RowCount == 0)
				return;

			using (Graphics g = CreateGraphics())
			{
				int width = gridContext.ClientSize.Width;
				if (gridContext.DisplayedRowCount(false) < gridContext.RowCount)
					width -= (SystemInformation.VerticalScrollBarWidth + 3);

				gridContext.Columns[colContextItem.Index].Width =
					TextRenderer.MeasureText(g, m_currContextItem,
						colContextItem.DefaultCellStyle.Font,
						Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;

				colContextBefore.Width = (width - colRef.Width - colContextItem.Width) / 2;
				colContextAfter.Width = width - colRef.Width -
					colContextItem.Width - colContextBefore.Width;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_tokenGrid_RowEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (GetContextInfo == null)
				return;

			string sKey;
			GetContextInfo(e.RowIndex, out sKey, out m_currContextItem);

			if (sKey == null || m_contextInfoLists == null ||
				!m_contextInfoLists.TryGetValue(sKey, out m_currContextInfoList))
			{
				m_currContextInfoList = new List<ContextInfo>();
			}

			gridContext.RowCount = m_currContextInfoList.Count;
			AdjustContextGridColumnWidths();
			gridContext.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridContext_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_currContextInfoList == null || e.RowIndex < 0 ||
				e.RowIndex >= m_currContextInfoList.Count)
			{
				return;
			}

			ContextInfo info = m_currContextInfoList[e.RowIndex];

			switch (e.ColumnIndex)
			{
				case kiColRef: e.Value = info.Reference; break;
				case kiColContextBefore: e.Value = info.Before; break;
				case kiColContextItem: e.Value = info.Character; break;
				case kiColContextAfter: e.Value = info.After; break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellPainting event of the gridContext control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The
		/// <see cref="System.Windows.Forms.DataGridViewCellPaintingEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridContext_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (colRef.Width == colRef.MinimumWidth)
				colRef.Width = TextRenderer.MeasureText(e.Graphics, "MMM 00:000", gridContext.Font).Width;

			e.AdvancedBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
			e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;

			if (e.ColumnIndex == kiColRef)
				e.AdvancedBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.Single;

			e.PaintBackground(e.CellBounds, false);

			TextFormatFlags flags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPrefix;

			if (e.ColumnIndex == colRef.Index)
			{
				Rectangle rc = e.CellBounds;
				int adjForBaseline = (e.RowIndex == 0) ? 2 : 1;
				rc.Height -= adjForBaseline;
				rc.Y += adjForBaseline;
				flags |= TextFormatFlags.Left;
				TextRenderer.DrawText(e.Graphics, e.FormattedValue as string,
					e.CellStyle.Font, rc, gridContext.ForeColor, flags);
			}
			else
			{
				if (gridContext.Columns[e.ColumnIndex].DisplayIndex == 1)
					flags |= TextFormatFlags.Right;
				else if (e.ColumnIndex == colContextItem.Index)
					flags |= TextFormatFlags.HorizontalCenter;

				flags |= TextFormatFlags.NoPadding;
				TextRenderer.DrawText(e.Graphics, e.FormattedValue as string,
					e.CellStyle.Font, e.CellBounds, gridContext.ForeColor, flags);
			}

			e.Handled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RowHeightInfoNeeded event of the gridCharInventory and gridContext grids.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">
		/// The <see cref="System.Windows.Forms.DataGridViewRowHeightInfoNeededEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleRowHeightInfoNeeded(object sender, DataGridViewRowHeightInfoNeededEventArgs e)
		{
			e.Height = m_gridRowHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnScan_Click(object sender, EventArgs e)
		{
			// Show the context menu with options for what data source to scan.
			Point pt = new Point(0, btnScan.Height);
			pt = btnScan.PointToScreen(pt);
			m_cmnuScan.Show(pt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void cmnuScanScripture_Click(object sender, EventArgs e)
		{
			// Scan the current scripture project.
			GetTokensSubStrings(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void cmnuScanFile_Click(object sender, EventArgs e)
		{
			// Use an open file dialog to let the user specify a file to scan.
			m_openFileDialog.CheckFileExists = true;
			m_openFileDialog.Filter = ResourceHelper.FileFilter(FileFilterType.AllFiles);

			if (m_openFileDialog.ShowDialog() == DialogResult.OK)
				GetTokensSubStrings(m_openFileDialog.FileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text token substrings from the data source and fire the event telling
		/// subscribers a list of text tokens is available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetTokensSubStrings(string fileName)
		{
			if (TextTokenSubStringsLoaded == null)
				return;

			if (BeforeTextTokenSubStringsLoaded != null)
				BeforeTextTokenSubStringsLoaded(fileName == string.Empty ? null : fileName);

			using (new WaitCursor(this))
			{
				List<TextTokenSubstring> tokens = (string.IsNullOrEmpty(fileName) ?
					ReadTEScripture() : ReadFile(fileName));

				if (tokens == null || tokens.Count == 0)
				{
					string msg = (fileName == null) ?
						String.Format(FwCoreDlgs.kstidNoTokensFoundInCurrentScriptureProj, m_sListName) :
						String.Format(FwCoreDlgs.kstidNoTokensFoundInFile, m_sListName, m_openFileDialog.FileName);
					MessageBox.Show(msg, Application.ProductName);
					ResetContextLists();
					lblScanMsg.Text = m_sInitialScanMsgLabel;
					m_tokenGrid.Invalidate();
				}
				else
				{
					TextTokenSubStringsLoaded(tokens);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the current TE scripture project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<TextTokenSubstring> ReadTEScripture()
		{
			IChecksDataSource scrDataSource = ReflectionHelper.CreateObject("ScrFDO.dll",
				"SIL.FieldWorks.FDO.Scripture.ScrChecksDataSource",
				new object[] { m_cache }) as IChecksDataSource;

			if (scrDataSource == null)
				return null;

			Assembly asm = Assembly.LoadFile(m_scrChecksDllFile);
			if (asm == null)
				return null;
			Type type = asm.GetType("SILUBS.ScriptureChecks." + m_checkToRun);

			IScrCheckInventory scrCharInventoryBldr =
				Activator.CreateInstance(type, scrDataSource) as IScrCheckInventory;

			if (scrCharInventoryBldr == null)
				return null;

			List<ITextToken> tokens = new List<ITextToken>();
			IScripture scr = m_cache.LangProject.TranslatedScriptureOA;
			if (scr == null || scr.ScriptureBooksOS.Count == 0)
				return null;

			foreach (IScrBook book in scr.ScriptureBooksOS)
			{
				if (scrDataSource.GetText(book.CanonicalNum, 0))
					tokens.AddRange(scrDataSource.TextTokens());
			}

			foreach (KeyValuePair<string, string> kvp in m_chkParams)
				scrDataSource.SetParameterValue(kvp.Key, kvp.Value);

			scrDataSource.SetParameterValue("PreferredLocale", string.Empty);

			if (tokens.Count == 0)
				return null;

			return GetTokenSubstrings(scrCharInventoryBldr, tokens);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the token substrings.
		/// </summary>
		/// <param name="inventory">The inventory used to parse the tokens.</param>
		/// <param name="tokens">The tokens (runs of text).</param>
		/// ------------------------------------------------------------------------------------
		private List<TextTokenSubstring> GetTokenSubstrings(IScrCheckInventory inventory,
			List<ITextToken> tokens)
		{
			List<TextTokenSubstring> list = inventory.GetReferences(tokens, string.Empty);
			if (m_listValidator != null)
				m_listValidator(list);
			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads a file for text tokens.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>tokens located in the file</returns>
		/// ------------------------------------------------------------------------------------
		private List<TextTokenSubstring> ReadFile(string fileName)
		{
			if (!File.Exists(fileName))
				return null;

			List<TextTokenSubstring> tokens = null;

			try
			{
				m_fileData = File.ReadAllLines(fileName);
				NormalizeFileData();

				TextFileDataSource data = new TextFileDataSource(m_scrChecksDllFile,
					m_checkToRun, m_fileData,
					ResourceHelper.GetResourceString("kstidFileLineRef"), m_chkParams, CharacterCategorizer);

				tokens = data.GetReferences();
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format(FwCoreDlgs.kstidNonUnicodeFileError, e.Message),
					Application.ProductName, MessageBoxButtons.OK,MessageBoxIcon.Information);
				return null;
			}

			return tokens;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Normalizes the strings read from the file into D (compatible decomposed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void NormalizeFileData()
		{
			for (int i = 0; i < m_fileData.Length; i++)
				m_fileData[i] = m_cache.UnicodeCharProps.NormalizeD(m_fileData[i]);
		}
		#endregion
	}

	#region ContextInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that enables the context grid to get the text before and after the character
	/// being displayed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ContextInfo
	{
		private string m_ref;
		private string m_contextBefore;
		private string m_contextAfter;
		private string m_chr;
		private ContextPosition m_position = ContextPosition.Undefined;

		/// <summary>Single character representing a whitespace character (or possibly multiple
		/// contiguous whitespace characters) when displayed in the punctuation patterns grid
		/// and the punctuation check. Typically, this is the space character (U+0032); for
		/// greater visibility, an underscore (_) character could be used.</summary>
		public static char s_chPunctWhitespace = ResourceHelper.GetResourceString("kstidPunctCheckWhitespaceChar")[0];

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ContextInfo"/> class.
		/// </summary>
		/// <param name="pattern">The punctuation pattern.</param>
		/// <param name="tts">The TextTokenSubstring.</param>
		/// ------------------------------------------------------------------------------------
		internal ContextInfo(PuncPattern pattern, TextTokenSubstring tts) :
			this(pattern, tts.Offset, tts.FullTokenText, tts.FirstToken.ScrRefString)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ContextInfo"/> class.
		/// </summary>
		/// <param name="chr">The character or pattern to which this context applies.</param>
		/// <param name="tts">The TextTokenSubstring.</param>
		/// ------------------------------------------------------------------------------------
		internal ContextInfo(string chr, TextTokenSubstring tts)
			: this(chr,	tts.Offset, tts.FullTokenText, tts.FirstToken.ScrRefString)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ContextInfo"/> class.
		/// </summary>
		/// <param name="pattern">The punctuation pattern.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="context">The context (a string with the line contents).</param>
		/// <param name="reference">The reference (line number).</param>
		/// ------------------------------------------------------------------------------------
		internal ContextInfo(PuncPattern pattern, int offset, string context, string reference)
		{
			m_position = pattern.ContextPos;
			string chr = pattern.Pattern;

			// For punctuation patterns the position indicated by offset refers to the place where
			// the first punctuation character occurs. There can be a leading character indicating
			// that the pattern was found preceded by a space or at the start of a paragraph.
			if (pattern.Pattern.Length > 1)
			{
				if (m_position == ContextPosition.WordInitial || m_position == ContextPosition.Isolated)
				{
					Debug.Assert(context[offset] == chr[1]);
					// Adjust offset to account for leading space which is actually in the data
					offset--;
				}
			}
			Initialize(chr, offset, context, reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ContextInfo"/> class.
		/// </summary>
		/// <param name="chr">The character or pattern to which this context applies.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="context">The context (a string with the line contents).</param>
		/// <param name="reference">The reference (line number).</param>
		/// ------------------------------------------------------------------------------------
		internal ContextInfo(string chr, int offset, string context, string reference)
		{
			Initialize(chr, offset, context, reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ContextInfo"/> class.
		/// </summary>
		/// <param name="chr">The character or pattern to which this context applies.</param>
		/// <param name="offset">The offset (can be negative!).</param>
		/// <param name="context">The context (a string with the line contents).</param>
		/// <param name="reference">The reference (line number).</param>
		/// ------------------------------------------------------------------------------------
		private void Initialize(string chr, int offset, string context, string reference)
		{
			m_chr = chr;
			m_ref = reference;

			int startPos = Math.Max(0, offset - 50);
			int length = Math.Max(0, (startPos == 0 ? offset : offset - startPos));
			m_contextBefore = context.Substring(startPos, length);

			// Since the pattern may come from multiple contiguous tokens, it's still possible
			// that the context isn't long enough to account for all the characters in the
			// pattern, in which case we will not be able to display any context after.
			startPos = Math.Min(context.Length, offset + chr.Length);
			m_contextAfter = (startPos + 50 >= context.Length ?
				context.Substring(startPos) :
				context.Substring(startPos, 50));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the reference (line number).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Reference
		{
			get { return m_ref; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character to which this context applies.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Character
		{
			get { return m_chr; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context before the offset.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Before
		{
			get { return m_contextBefore; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context after the offset.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string After
		{
			get { return m_contextAfter; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serves as a lookup key for the Context Info type. Note that this will return an
		/// identical hash code for different ContextInfo objects whose Character and Position
		/// are the same.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Key
		{
			get
			{
				return m_chr + ((m_position == ContextPosition.Undefined) ?
					String.Empty : m_position.ToString());
			}
		}
	}

	#endregion

	#region ContextGrid class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Subclass the grid so we can make it double-buffered.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ContextGrid : DataGridView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ContextGrid"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ContextGrid()
		{
			DoubleBuffered = true;
		}
	}

	#endregion
}
