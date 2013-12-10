using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SILUBS.SharedScrUtils;

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
		private IWritingSystemContainer m_wsContainer;
		private ILgCharacterPropertyEngine m_charPropEng;
		private IApp m_app;
		private IWritingSystem m_ws;
		private int m_gridRowHeight;
		private CheckType m_checkToRun;
		private string m_sListName;
		private readonly string m_sInitialScanMsgLabel;
		private Dictionary<string, string> m_chkParams = new Dictionary<string, string>();
		private string m_currContextItem;
		private ValidateList m_listValidator;
		private OpenFileDialogAdapter m_openFileDialog;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the CharContextCtrl.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharContextCtrl()
		{
			InitializeComponent();
			m_openFileDialog = new OpenFileDialogAdapter();
			m_openFileDialog.DefaultExt = "lds";
			m_openFileDialog.Title = FwCoreDlgs.kstidLanguageFileBrowser;

			gridContext.AutoGenerateColumns = false;
			colRef.MinimumWidth = 2;
			colRef.Width = colRef.MinimumWidth;

			gridContext.GridColor = ColorHelper.CalculateColor(SystemColors.WindowText,
				SystemColors.Window, 35);
			m_sInitialScanMsgLabel = lblScanMsg.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character property engine to use for this control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ILgCharacterPropertyEngine CharPropEngine
		{
			get
			{
				if (m_wsContainer.DefaultVernacularWritingSystem != null)
					return m_wsContainer.DefaultVernacularWritingSystem.CharPropEngine;
				if (m_charPropEng == null)
					m_charPropEng = LgIcuCharPropEngineClass.Create();
				return m_charPropEng;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this CharContextCtrl
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsContainer">The writing system container.</param>
		/// <param name="ws">The language definition.</param>
		/// <param name="app">The application.</param>
		/// <param name="contextFont">The context font.</param>
		/// <param name="tokenGrid">The token grid.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(FdoCache cache, IWritingSystemContainer wsContainer,
			IWritingSystem ws, IApp app, Font contextFont, DataGridView tokenGrid)
		{
			m_cache = cache;
			m_wsContainer = wsContainer;
			m_ws = ws;
			m_app = app;
			ContextFont = contextFont;
			TokenGrid = tokenGrid;

			if (FwUtils.IsOkToDisplayScriptureIfPresent)
				m_scrChecksDllFile = FwDirectoryFinder.BasicEditorialChecksDll;

			if (m_ws != null)
			{
				bool modifyingVernWs = (m_wsContainer.DefaultVernacularWritingSystem != null &&
					m_ws.Id == m_wsContainer.DefaultVernacularWritingSystem.Id);

				// If TE isn't installed, we can't support creating an inventory
				// based on Scripture data. Likewise if we don't yet have any books (which also guards
				// against showing the option in the SE edition, unless it has been paired with Paratext).
				cmnuScanScripture.Visible = (FwUtils.IsOkToDisplayScriptureIfPresent &&
					File.Exists(m_scrChecksDllFile) &&
					m_cache != null && m_cache.LanguageProject.TranslatedScriptureOA != null
					&& m_cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS.Count > 0
					&& modifyingVernWs);

				if (m_ws.RightToLeftScript)
				{
					// Set the order of the columns for right-to-left text.
					colContextAfter.DisplayIndex = colContextBefore.DisplayIndex;
					colContextBefore.DisplayIndex = gridContext.ColumnCount - 1;
				}
			}
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
		/// Gets/sets the token grid. If a tokenGrid is already in use, it will be cleared first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DataGridView TokenGrid
		{
			get { return m_tokenGrid; }
			private set
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
			private set
			{
				if (value != null)
				{
					colContextAfter.DefaultCellStyle.Font = value;
					colContextBefore.DefaultCellStyle.Font = value;
					colContextItem.DefaultCellStyle.Font = value;
					m_gridRowHeight = Math.Max(value.Height, gridContext.Font.Height) + 2;
				}
			}
		}

		/// <summary>
		/// Kinds of checks this control might run if activated.
		/// </summary>
		public enum CheckType
		{
			/// <summary>
			/// Use PunctuationCheck
			/// </summary>
			Punctuation,
			/// <summary>
			/// Use CharactersCheck
			/// </summary>
			Characters
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CheckType CheckToRun
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
			set	{ m_chkParams = value ?? new Dictionary<string, string>();	}
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
				if (m_wsContainer.DefaultVernacularWritingSystem == null)
					return null;
				return ValidCharacters.Load(m_wsContainer.DefaultVernacularWritingSystem, LoadException, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets Unicode character categorizer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private CharacterCategorizer CharacterCategorizer
		{
			get { return new FwCharacterCategorizer(ValidCharacters, CharPropEngine); }
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
		/// Reports a load exception in the scrDataSource.
		/// </summary>
		/// <param name="e">The exception.</param>
		/// ------------------------------------------------------------------------------------
		void LoadException(ArgumentException e)
		{
			ErrorReporter.ReportException(e, m_app.SettingsKey,
				m_app.SupportEmailAddress, ParentForm, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the width of the columns so they just fit inside the client area of the grid.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
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
					Math.Max(TextRenderer.MeasureText(g, m_currContextItem,
						colContextItem.DefaultCellStyle.Font,
						Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width,
						gridContext.Columns[colContextItem.Index].MinimumWidth);

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
		/// <see cref="T:System.Windows.Forms.DataGridViewCellPaintingEventArgs"/> instance
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
		/// The <see cref="T:System.Windows.Forms.DataGridViewRowHeightInfoNeededEventArgs"/>
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
			var pt = new Point(0, btnScan.Height);
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
					MessageBox.Show(msg, m_app.ApplicationName);
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
			var scrDataSource = new ScrChecksDataSource(m_cache, ResourceHelper.GetResourceString("kstidPunctCheckWhitespaceChar"),
				FwDirectoryFinder.LegacyWordformingCharOverridesFile, FwDirectoryFinder.TeStylesPath);

			scrDataSource.LoadException += scrDataSource_LoadException;

			IScrCheckInventory scrCharInventoryBldr = CreateScrCharInventoryBldr(FwDirectoryFinder.BasicEditorialChecksDll,
				scrDataSource, m_checkToRun == CheckType.Punctuation ?
				"SILUBS.ScriptureChecks.PunctuationCheck" : "SILUBS.ScriptureChecks.CharactersCheck");

			var tokens = new List<ITextToken>();
			var scr = m_cache.LangProject.TranslatedScriptureOA;
			if (scr == null || scr.ScriptureBooksOS.Count == 0)
				return null;

			foreach (var book in scr.ScriptureBooksOS)
			{
				if (scrDataSource.GetText(book.CanonicalNum, 0))
					tokens.AddRange(scrDataSource.TextTokens());
			}

			foreach (KeyValuePair<string, string> kvp in m_chkParams)
				scrDataSource.SetParameterValue(kvp.Key, kvp.Value);

			scrDataSource.SetParameterValue("PreferredLocale", string.Empty);

			return tokens.Count == 0 ? null : GetTokenSubstrings(scrCharInventoryBldr, tokens);
		}

		private static IScrCheckInventory CreateScrCharInventoryBldr(string checksDll, IChecksDataSource scrDataSource, string checkType)
		{
			var scrCharInventoryBldr = (IScrCheckInventory)ReflectionHelper.CreateObject(checksDll,
				checkType, new object[] { scrDataSource });
			return scrCharInventoryBldr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports a load exception in the scrDataSource.
		/// </summary>
		/// <param name="e">The exception.</param>
		/// ------------------------------------------------------------------------------------
		void scrDataSource_LoadException(ArgumentException e)
		{
			ErrorReporter.ReportException(e, m_app.SettingsKey, m_app.SupportEmailAddress,
				ParentForm, false);
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

			List<TextTokenSubstring> tokens;

			try
			{
				m_fileData = File.ReadAllLines(fileName);
				NormalizeFileData();

				var data = new TextFileDataSource(m_scrChecksDllFile,
					m_checkToRun == CheckType.Punctuation ? "PunctuationCheck" : "CharactersCheck",
					m_fileData,
					ResourceHelper.GetResourceString("kstidFileLineRef"), m_chkParams, CharacterCategorizer);

				tokens = data.GetReferences();
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format(FwCoreDlgs.kstidNonUnicodeFileError, e.Message),
					m_app.ApplicationName, MessageBoxButtons.OK,MessageBoxIcon.Information);
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
			// The following list of control characters should never appear in plain Unicode
			// data.
			char[] controlChars = new char[] {
				'\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07', '\x08', '\x0E', '\x0F',
				'\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19',
				'\x1A', '\x1B', '\x7F' };
			for (int i = 0; i < m_fileData.Length; i++)
			{
				if (m_fileData[i].Length > 0)
				{
					if (m_fileData[i].IndexOfAny(controlChars) >= 0)
						throw new Exception(FWCoreDlgsErrors.ksInvalidControlCharacterFound);
					m_fileData[i] = CharPropEngine.NormalizeD(m_fileData[i]);
				}
			}
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
