// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FwCoreDlgs.FileDialog;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;
using SIL.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Class to support displaying the context(s) for a character string.
	/// </summary>
	internal sealed partial class CharContextCtrl : UserControl
	{
		private const int kiColRef = 0;
		private const int kiColContextBefore = 1;
		private const int kiColContextItem = 2;
		private const int kiColContextAfter = 3;

		/// <summary>When the current scripture project is scanned, then filename is null.</summary>
		internal delegate void BeforeTextTokenSubStringsLoadedHandler(string filename);

		/// <summary>Event fired before a data source is
		/// read to get all the token substrings.</summary>
		internal event BeforeTextTokenSubStringsLoadedHandler BeforeTextTokenSubStringsLoaded;

		/// <summary></summary>
		internal delegate void TextTokenSubStringsLoadedHandler(List<TextTokenSubstring> tokens);

		/// <summary>Event fired when a data source has been
		/// read to get all the token substrings.</summary>
		internal event TextTokenSubStringsLoadedHandler TextTokenSubStringsLoaded;

		/// <summary></summary>
		internal delegate void GetContextInfoHandler(int index, out string sKey, out string sConcordanceItem);

		/// <summary>Event fired when the control needs a
		/// list of context information.</summary>
		internal event GetContextInfoHandler GetContextInfo;

		private string[] m_fileData;
		private DataGridView m_tokenGrid;
		private string m_scrChecksDllFile;
		private List<ContextInfo> m_currContextInfoList;
		private Dictionary<string, List<ContextInfo>> m_contextInfoLists;
		private LcmCache m_cache;
		private IWritingSystemContainer m_wsContainer;
		private IApp m_app;
		private CoreWritingSystemDefinition m_ws;
		private int m_gridRowHeight;
		private readonly string m_sInitialScanMsgLabel;
		private Dictionary<string, string> m_chkParams = new Dictionary<string, string>();
		private string m_currContextItem;

		/// <summary />
		internal CharContextCtrl()
		{
			InitializeComponent();

			gridContext.AutoGenerateColumns = false;
			colRef.MinimumWidth = 2;
			colRef.Width = colRef.MinimumWidth;

			gridContext.GridColor = ColorHelper.CalculateColor(SystemColors.WindowText, SystemColors.Window, 35);
			m_sInitialScanMsgLabel = lblScanMsg.Text;
		}

		/// <summary>
		/// Initializes this CharContextCtrl
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsContainer">The writing system container.</param>
		/// <param name="ws">The language definition.</param>
		/// <param name="app">The application.</param>
		/// <param name="contextFont">The context font.</param>
		/// <param name="tokenGrid">The token grid.</param>
		internal void Initialize(LcmCache cache, IWritingSystemContainer wsContainer, CoreWritingSystemDefinition ws, IApp app, Font contextFont, DataGridView tokenGrid)
		{
			m_cache = cache;
			m_wsContainer = wsContainer;
			m_ws = ws;
			m_app = app;
			ContextFont = contextFont;
			TokenGrid = tokenGrid;

			var isOkToDisplayScripture = m_cache != null && m_cache.ServiceLocator.GetInstance<IScrBookRepository>().AllInstances().Any();
			if (isOkToDisplayScripture)
			{
				m_scrChecksDllFile = FwDirectoryFinder.BasicEditorialChecksDll;
			}

			if (m_ws != null)
			{
				if (m_ws.RightToLeftScript)
				{
					// Set the order of the columns for right-to-left text.
					colContextAfter.DisplayIndex = colContextBefore.DisplayIndex;
					colContextBefore.DisplayIndex = gridContext.ColumnCount - 1;
				}
			}
		}

		#region Public Properties

		/// <summary>
		/// Sets a value indicating what is type of data is being displayed in the list. This
		/// should be a localizable string, since it will be used in a message displayed to the
		/// user. It should use lowercase rather than title case (e.g., "characters",
		/// "punctuation patterns", etc.).
		/// patterns.
		/// </summary>
		internal string DisplayedListName { get; set; }

		/// <summary>
		/// Gets or sets the scan message label text.
		/// </summary>
		internal string ScanMsgLabelText
		{
			get => lblScanMsg.Text;
			set => lblScanMsg.Text = value;
		}

		/// <summary>
		/// Gets/sets the token grid. If a tokenGrid is already in use, it will be cleared first.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal DataGridView TokenGrid
		{
			get => m_tokenGrid;
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

		/// <summary />
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal Font ContextFont
		{
			get => colContextAfter.DefaultCellStyle.Font;
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

		/// <summary />
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal CheckType CheckToRun { get; set; }

		#endregion

		#region Public methods

		/// <summary />
		internal void RefreshContextGrid()
		{
			m_tokenGrid_RowEnter(null, new DataGridViewCellEventArgs(m_tokenGrid.CurrentCellAddress.X, m_tokenGrid.CurrentCellAddress.Y));
		}

		/// <summary />
		internal void ResetContextLists()
		{
			m_contextInfoLists = new Dictionary<string, List<ContextInfo>>();
			m_currContextInfoList = new List<ContextInfo>();
			gridContext.RowCount = 0;
			gridContext.Invalidate();
		}

		/// <summary />
		internal void AddContextInfo(ContextInfo contextInfo)
		{
			if (!m_contextInfoLists.TryGetValue(contextInfo.Key, out var list))
			{
				list = new List<ContextInfo>();
				m_contextInfoLists[contextInfo.Key] = list;
			}
			list.Add(contextInfo);
		}
		#endregion

		#region Event handlers and helper methods

		/// <summary>
		/// Adjust the width of the columns so they just fit inside the client area of the grid.
		/// </summary>
		private void gridContext_ClientSizeChanged(object sender, EventArgs e)
		{
			if (m_tokenGrid?.CurrentRow != null)
			{
				AdjustContextGridColumnWidths();
			}
		}

		/// <summary>
		/// Adjusts the column widths in the context grid.
		/// </summary>
		private void AdjustContextGridColumnWidths()
		{
			if (m_currContextItem == null || gridContext.RowCount == 0)
			{
				return;
			}
			using (var g = CreateGraphics())
			{
				var width = gridContext.ClientSize.Width;
				if (gridContext.DisplayedRowCount(false) < gridContext.RowCount)
				{
					width -= (SystemInformation.VerticalScrollBarWidth + 3);
				}
				gridContext.Columns[colContextItem.Index].Width =
					Math.Max(TextRenderer.MeasureText(g, m_currContextItem,
						colContextItem.DefaultCellStyle.Font,
						Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width,
						gridContext.Columns[colContextItem.Index].MinimumWidth);

				colContextBefore.Width = (width - colRef.Width - colContextItem.Width) / 2;
				colContextAfter.Width = width - colRef.Width - colContextItem.Width - colContextBefore.Width;
			}
		}

		/// <summary />
		private void m_tokenGrid_RowEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (GetContextInfo == null)
			{
				return;
			}

			GetContextInfo(e.RowIndex, out var sKey, out m_currContextItem);
			if (sKey == null || m_contextInfoLists == null || !m_contextInfoLists.TryGetValue(sKey, out m_currContextInfoList))
			{
				m_currContextInfoList = new List<ContextInfo>();
			}

			gridContext.RowCount = m_currContextInfoList.Count;
			AdjustContextGridColumnWidths();
			gridContext.Invalidate();
		}

		/// <summary />
		private void gridContext_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_currContextInfoList == null || e.RowIndex < 0 || e.RowIndex >= m_currContextInfoList.Count)
			{
				return;
			}
			var info = m_currContextInfoList[e.RowIndex];
			switch (e.ColumnIndex)
			{
				case kiColRef:
					e.Value = info.Reference;
					break;
				case kiColContextBefore:
					e.Value = info.Before;
					break;
				case kiColContextItem:
					e.Value = info.Character;
					break;
				case kiColContextAfter:
					e.Value = info.After;
					break;
			}
		}

		/// <summary>
		/// Handles the CellPainting event of the gridContext control.
		/// </summary>
		private void gridContext_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (colRef.Width == colRef.MinimumWidth)
			{
				colRef.Width = TextRenderer.MeasureText(e.Graphics, "MMM 00:000", gridContext.Font).Width;
			}
			e.AdvancedBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
			e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;

			if (e.ColumnIndex == kiColRef)
			{
				e.AdvancedBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.Single;
			}
			e.PaintBackground(e.CellBounds, false);

			var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPrefix;
			if (e.ColumnIndex == colRef.Index)
			{
				var rc = e.CellBounds;
				var adjForBaseline = (e.RowIndex == 0) ? 2 : 1;
				rc.Height -= adjForBaseline;
				rc.Y += adjForBaseline;
				flags |= TextFormatFlags.Left;
				TextRenderer.DrawText(e.Graphics, e.FormattedValue as string, e.CellStyle.Font, rc, gridContext.ForeColor, flags);
			}
			else
			{
				if (gridContext.Columns[e.ColumnIndex].DisplayIndex == 1)
				{
					flags |= TextFormatFlags.Right;
				}
				else if (e.ColumnIndex == colContextItem.Index)
				{
					flags |= TextFormatFlags.HorizontalCenter;
				}
				flags |= TextFormatFlags.NoPadding;
				TextRenderer.DrawText(e.Graphics, e.FormattedValue as string, e.CellStyle.Font, e.CellBounds, gridContext.ForeColor, flags);
			}

			e.Handled = true;
		}

		/// <summary>
		/// Handles the RowHeightInfoNeeded event of the gridCharInventory and gridContext grids.
		/// </summary>
		private void HandleRowHeightInfoNeeded(object sender, DataGridViewRowHeightInfoNeededEventArgs e)
		{
			e.Height = m_gridRowHeight;
		}

		/// <summary />
		private void btnScan_Click(object sender, EventArgs e)
		{
			// Show the context menu with options for what data source to scan.
			var pt = new Point(0, btnScan.Height);
			pt = btnScan.PointToScreen(pt);
			m_cmnuScan.Show(pt);
		}

		/// <summary />
		private void cmnuScanFile_Click(object sender, EventArgs e)
		{
			// Let the user specify a Paratext or Toolbox language file to scan.
			var languageFiles = ResourceHelper.GetResourceString("kstidToolboxLanguageFiles");
			var allFiles = ResourceHelper.GetResourceString("kstidAllFiles");
			using (IOpenFileDialog openFileDialog = new OpenFileDialogAdapter())
			{
				openFileDialog.Title = FwCoreDlgs.kstidLanguageFileBrowser;
				openFileDialog.InitialDirectory = ScriptureProvider.SettingsDirectory;
				openFileDialog.CheckFileExists = true;
				openFileDialog.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(string.Format("{0} ({1})|{1}|{2} ({3})|{3}", languageFiles, "*.lds;*.lng", allFiles, "*.*"));
				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					GetTokensSubStrings(openFileDialog.FileName);
				}
			}
		}

		/// <summary>
		/// Get the text token substrings from the data source and fire the event telling
		/// subscribers a list of text tokens is available.
		/// </summary>
		private void GetTokensSubStrings(string fileName)
		{
			if (TextTokenSubStringsLoaded == null)
			{
				return;
			}
			BeforeTextTokenSubStringsLoaded?.Invoke(fileName == string.Empty ? null : fileName);

			using (new WaitCursor(this))
			{
				var tokens = ReadFile(fileName);
				if (tokens == null || tokens.Count == 0)
				{
					var msg = fileName == null ?
						string.Format(FwCoreDlgs.kstidNoTokensFoundInCurrentScriptureProj, DisplayedListName) :
						string.Format(FwCoreDlgs.kstidNoTokensFoundInFile, DisplayedListName, fileName);
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

		/// <summary>
		/// Reads a file for text tokens.
		/// </summary>
		private List<TextTokenSubstring> ReadFile(string fileName)
		{
			if (!File.Exists(fileName))
			{
				return null;
			}
			List<TextTokenSubstring> tokens;
			try
			{
				m_fileData = File.ReadAllLines(fileName);
				NormalizeFileData();

				var data = new TextFileDataSource(m_scrChecksDllFile,
					CheckToRun == CheckType.Punctuation ? "PunctuationCheck" : "CharactersCheck",
					m_fileData,
					ResourceHelper.GetResourceString("kstidFileLineRef"), m_chkParams,
					new FwCharacterCategorizer(m_wsContainer.DefaultVernacularWritingSystem == null ? null : ValidCharacters.Load(m_wsContainer.DefaultVernacularWritingSystem)));

				tokens = data.GetReferences();
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format(FwCoreDlgs.kstidNonUnicodeFileError, e.Message), m_app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return null;
			}

			return tokens;
		}

		/// <summary>
		/// Normalizes the strings read from the file into D (compatible decomposed).
		/// </summary>
		private void NormalizeFileData()
		{
			// The following list of control characters should never appear in plain Unicode data.
			char[] controlChars =
			{
				'\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07', '\x08', '\x0E', '\x0F',
				'\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19',
				'\x1A', '\x1B', '\x7F'
			};
			for (var i = 0; i < m_fileData.Length; i++)
			{
				if (m_fileData[i].Length > 0)
				{
					if (m_fileData[i].IndexOfAny(controlChars) >= 0)
					{
						throw new Exception(FWCoreDlgsErrors.ksInvalidControlCharacterFound);
					}
					m_fileData[i] = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(m_fileData[i]);
				}
			}
		}
		#endregion

		/// <summary>
		/// A class representing a file that can be parsed to find characters
		/// </summary>
		private sealed class TextFileDataSource : IChecksDataSource
		{
			private string m_scrChecksDllFile;
			private string m_scrCheck;
			private List<ITextToken> m_tftList;
			private Dictionary<string, string> m_params;

			/// <summary />
			/// <param name="scrChecksDllFile">The DLL that contains the CharactersCheck class</param>
			/// <param name="scrCheck">Name of the scripture check to use</param>
			/// <param name="fileData">An array of strings with the lines of data from the file.</param>
			/// <param name="scrRefFormatString">Format string used to format scripture references.</param>
			/// <param name="parameters">Checking parameters to send the check.</param>
			/// <param name="categorizer">The character categorizer.</param>
			internal TextFileDataSource(string scrChecksDllFile, string scrCheck, string[] fileData, string scrRefFormatString, Dictionary<string, string> parameters, CharacterCategorizer categorizer)
			{
				m_scrChecksDllFile = scrChecksDllFile;
				m_scrCheck = scrCheck;
				CharacterCategorizer = categorizer ?? new CharacterCategorizer();
				m_params = parameters;
				m_tftList = new List<ITextToken>();
				var i = 1;
				foreach (var line in fileData)
				{
					m_tftList.Add(new TextFileToken(line, i++, scrRefFormatString));
				}
			}

			#region IChecksDataSource Members

			/// <summary>
			/// Gets the character categorizer.
			/// </summary>
			public CharacterCategorizer CharacterCategorizer { get; }

			/// <summary>
			/// Gets the parameter value.
			/// </summary>
			public string GetParameterValue(string key)
			{
				return m_params != null && m_params.TryGetValue(key, out var param) ? param : string.Empty;
			}

			/// <summary>
			/// Gets the text (not supported).
			/// </summary>
			public bool GetText(int bookNum, int chapterNum)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Saves this instance (not supported).
			/// </summary>
			public void Save()
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Sets the parameter value (not supported).
			/// </summary>
			public void SetParameterValue(string key, string value)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Gets the text tokens.
			/// </summary>
			public IEnumerable<ITextToken> TextTokens => m_tftList;

			/// <summary />
			public string GetLocalizedString(string strToLocalize)
			{
				return strToLocalize;
			}

			#endregion

			/// <summary>
			/// Gets the references.
			/// </summary>
			internal List<TextTokenSubstring> GetReferences()
			{
				try
				{
					var asm = Assembly.LoadFile(m_scrChecksDllFile);
					var type = asm.GetType("SIL.FieldWorks.Common.FwUtils." + m_scrCheck);
					var scrCharInventoryBldr = (IScrCheckInventory)Activator.CreateInstance(type, this);
					return scrCharInventoryBldr.GetReferences(m_tftList, string.Empty);
				}
				catch
				{
					return null;
				}
			}

			/// <summary>
			/// Text token object used for reading any, nondescript text file in order to discover
			/// all the characters therein.
			/// </summary>
			private sealed class TextFileToken : ITextToken
			{
				private int m_iLine;
				private string m_scrRefFmtString;

				/// <summary />
				internal TextFileToken(string text, int iLine, string scrRefFormatString)
				{
					Text = text;
					m_iLine = iLine;
					m_scrRefFmtString = scrRefFormatString;
				}

				#region ITextToken Members

				/// <summary>
				/// Not used.
				/// </summary>
				public bool IsNoteStart => false;

				/// <summary>
				/// Not used.
				/// </summary>
				public bool IsParagraphStart => true;

				/// <summary>
				/// Not used.
				/// </summary>
				public string Locale => null;

				/// <summary>
				/// Not used.
				/// </summary>
				public string ScrRefString
				{
					get => string.Format(m_scrRefFmtString, m_iLine);
					set { }
				}

				/// <summary>
				/// Not used.
				/// </summary>
				public string ParaStyleName => null;

				/// <summary>
				/// Not used.
				/// </summary>
				public string CharStyleName => null;

				/// <summary>
				/// Gets the text.
				/// </summary>
				public string Text { get; }

				/// <summary>
				/// Force the check to treat the text like verse text.
				/// </summary>
				public TextType TextType => TextType.Verse;

				/// <summary />
				public BCVRef MissingEndRef
				{
					get => null;
					set { }
				}

				/// <summary />
				public BCVRef MissingStartRef
				{
					get => null;
					set { }
				}

				/// <summary>
				/// Makes a deep copy of the specified text token.
				/// </summary>
				public ITextToken Clone()
				{
					return new TextFileToken(Text, m_iLine, m_scrRefFmtString);
				}
				#endregion
			}
		}
	}
}
