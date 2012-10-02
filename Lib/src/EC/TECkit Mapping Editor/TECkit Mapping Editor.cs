using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ECInterfaces;
using SilEncConverters31;
using System.Drawing;               // for Font
using System.Diagnostics;           // Debug
using System.Runtime.InteropServices;   // DLLImport

namespace TECkit_Mapping_Editor
{
	public partial class TECkitMapEditorForm : Form
	{
		internal const string cstrCaption = "TECkit Map Editor";
		const string cstrHelpDocumentPath = @"\SIL\Help\TECkit\TECkit_Language_2.1.doc.pdf";    // (from \pf\cf...)
		const string cstrMyEncConverterName = "TECkitMappingEditorEncConverter";
		const string cstrOpeningHeader = "; This file was edited using TECkitMappingEditorU.exe ";
		const string cstrLhsEncAttrKey = "Left Hand Side EncodingID";
		const string cstrRhsEncAttrKey = "Right Hand Side EncodingID";
		const string cstrConvTypeClue = "Conversion Type = ";
		const string cstrLhsFontClue = "Left-hand side font = ";
		const string cstrRhsFontClue = "Right-hand side font = ";
		const string cstrMainFormClue = "Main Window Position = ";
		private const string cstrDeprecatedCodePointFormClue = "Code Point Window Position = ";
		internal const string cstrCodePointFormClueLhs = "Left-hand side Character Map Window Position = ";
		internal const string cstrCodePointFormClueRhs = "Right-hand side Character Map Window Position = ";
		const string cstrLhsCodePageClue = "Left-hand side code page = ";
		const string cstrRhsCodePageClue = "Right-hand side code page = ";

		string m_strTecNameReal = null, m_strTecNameTemp;
		string m_strMapNameReal = null, m_strMapNameTemp;
		Encoding m_enc;
		IEncConverter m_aEC = null;
		private ConvType m_eConvType = ConvType.Legacy_to_from_Unicode;
		internal DisplayUnicodeNamesForm m_formDisplayUnicodeNamesLhs = null;
		internal DisplayUnicodeNamesForm m_formDisplayUnicodeNamesRhs = null;
		protected FindReplaceForm m_formFindReplace = null;
		protected int m_nCodePageLegacyLhs = 0;
		protected int m_nCodePageLegacyRhs = 0;

		public ConvType ConversionType
		{
			get { return m_eConvType; }
			set
			{
				m_eConvType = value;
				IsLhsLegacy = (EncConverter.NormalizeLhsConversionType(m_eConvType) == NormConversionType.eLegacy);
				IsRhsLegacy = (EncConverter.NormalizeRhsConversionType(m_eConvType) == NormConversionType.eLegacy);
			}
		}

		public TECkitMapEditorForm(Encoding enc)
		{
			InitializeComponent();

			m_enc = enc;

			m_formFindReplace = new FindReplaceForm(this.richTextBoxMapEditor);
			m_formFindReplace.Owner = this;

			SetHelpProvider();
		}

		private void SetHelpProvider()
		{
			helpProvider.SetHelpString(textBoxCompilerResults, Properties.Resources.CompilerResults);
			helpProvider.SetHelpString(richTextBoxMapEditor, Properties.Resources.MainEditor);
			helpProvider.SetHelpString(tableLayoutPanelSamples, Properties.Resources.SampleBox);
			helpProvider.SetHelpString(this.dataGridViewCodePointValues, global::TECkit_Mapping_Editor.Properties.Resources.CPDataGridCodePoints);
		}

		private bool SetBounds(Form form, ref Rectangle rectBounds)
		{
			// do this in such a way that we take into consideration the size of the screen
			//  which may not be the same from machine to machine
			bool bChanged = false;
			Rectangle rectScreen = Screen.PrimaryScreen.WorkingArea;
			if (!rectScreen.Contains(rectBounds))
			{
				rectBounds.Intersect(rectScreen);
				bChanged = true;
			}

			form.Bounds = rectBounds;
			return bChanged;
		}

		protected void InitCharacterMapDialogs()
		{
			// clear out the old...
			if (m_formDisplayUnicodeNamesLhs != null)
				m_formDisplayUnicodeNamesLhs.Close();

			// bring in the new...
			m_formDisplayUnicodeNamesLhs = new DisplayUnicodeNamesForm(true);
			m_formDisplayUnicodeNamesLhs.Owner = this; // causes the form to surface whenever the main window activates

			if (m_formDisplayUnicodeNamesRhs != null)
				m_formDisplayUnicodeNamesRhs.Close();

			m_formDisplayUnicodeNamesRhs = new DisplayUnicodeNamesForm(false);
			m_formDisplayUnicodeNamesRhs.Owner = this; // causes the form to surface whenever the main window activates
		}

		internal bool OpenDocument(string strMapName)
		{
			if (Program.Modified)
				CheckForSaveDirtyFile();

			InitCharacterMapDialogs();

			bool bModified = false;

			// if we are dealing with a real file...
			m_strMapNameReal = strMapName;

			// check to see if it exists (the alternate exception isn't very helpful)
			if (!File.Exists(m_strMapNameReal))
			{
				MessageBox.Show(String.Format("The file '{0}' doesn't exist!", m_strMapNameReal, cstrCaption));
				return false;
			}

			// otherwise, determine the .tec filename
			m_strTecNameReal = m_strMapNameReal.Remove(m_strMapNameReal.Length - 3, 3) + "tec";
			Program.AddFilenameToTitle(m_strMapNameReal);

			// and put it's contents into the editor.
			this.richTextBoxMapEditor.Lines = File.ReadAllLines(m_strMapNameReal, m_enc);

			// see if our 'clues' are in the file
			ConvType eConvType = ConvType.Unknown;
			bool bLhsFontFound = false;
			bool bRhsFontFound = false;
			bool bCodePointFormSizeHasBeenSetLhs = false;
			bool bCodePointFormSizeHasBeenSetRhs = false;
			m_nCodePageLegacyLhs = 0;
			m_nCodePageLegacyRhs = 0;

			foreach (string strLine in richTextBoxMapEditor.Lines)
			{
				int nIndex = strLine.IndexOf(cstrConvTypeClue);
				if (nIndex != -1)
				{
					string strConvType = strLine.Substring(nIndex + cstrConvTypeClue.Length);

					foreach (string asName in Enum.GetNames(typeof(ConvType)))
					{
						if (asName == strConvType)
						{
							eConvType = (ConvType)Enum.Parse(typeof(ConvType), strConvType);
							break;
						}
					}
				}

				nIndex = strLine.IndexOf(cstrLhsFontClue);
				if (nIndex != -1)
				{
					int nDelimiter = strLine.LastIndexOf(';');
					int nLen = nDelimiter - (nIndex + cstrLhsFontClue.Length);
					string strFontName = strLine.Substring(nIndex + cstrLhsFontClue.Length, nLen);
					string strFontSize = strLine.Substring(nDelimiter + 1);
					float emSize = Convert.ToSingle(strFontSize);
					Font font = Program.GetSafeFont(strFontName, emSize);
					textBoxSample.Font = font;
					textBoxSampleReverse.Font = font;
					bLhsFontFound = true;
				}

				nIndex = strLine.IndexOf(cstrRhsFontClue);
				if (nIndex != -1)
				{
					int nDelimiter = strLine.LastIndexOf(';');
					int nLen = nDelimiter - (nIndex + cstrRhsFontClue.Length);
					string strFontName = strLine.Substring(nIndex + cstrRhsFontClue.Length, nLen);
					string strFontSize = strLine.Substring(nDelimiter + 1);
					float emSize = Convert.ToSingle(strFontSize);
					Font font = Program.GetSafeFont(strFontName, emSize);
					textBoxSampleForward.Font = font;
					bRhsFontFound = true;
				}

				nIndex = strLine.IndexOf(cstrLhsCodePageClue);
				if (nIndex != -1)
				{
					string strCodePage = strLine.Substring(nIndex + cstrLhsCodePageClue.Length);
					try
					{
						m_nCodePageLegacyLhs = Convert.ToInt32(strCodePage);
					}
					catch { }
				}

				nIndex = strLine.IndexOf(cstrRhsCodePageClue);
				if (nIndex != -1)
				{
					string strCodePage = strLine.Substring(nIndex + cstrRhsCodePageClue.Length);
					try
					{
						m_nCodePageLegacyRhs = Convert.ToInt32(strCodePage);
					}
					catch { }
				}

				nIndex = strLine.IndexOf(cstrMainFormClue);
				if (nIndex != -1)
				{
					try
					{
						string strXYWH = strLine.Substring(nIndex + cstrMainFormClue.Length);
						string[] aStrBounds = strXYWH.Split(new char[] { ',' });
						Rectangle rectBounds = new Rectangle
						(
							Int32.Parse(aStrBounds[0]),
							Int32.Parse(aStrBounds[1]),
							Int32.Parse(aStrBounds[2]),
							Int32.Parse(aStrBounds[3])
						);

						if (SetBounds(this, ref rectBounds))
							SetBoundsClue(cstrMainFormClue, rectBounds);
					}
					catch { }
				}

				// search for the character map window location (what used to be the 'code point' window)
				string strClue = cstrCodePointFormClueLhs;
				nIndex = strLine.IndexOf(cstrCodePointFormClueLhs);
				if (nIndex != -1)
				{
					try
					{
						string strXYWH = strLine.Substring(nIndex + strClue.Length);
						string[] aStrBounds = strXYWH.Split(new char[] { ',' });
						Rectangle rectBounds = new Rectangle
						(
							Int32.Parse(aStrBounds[0]),
							Int32.Parse(aStrBounds[1]),
							Int32.Parse(aStrBounds[2]),
							Int32.Parse(aStrBounds[3])
						);

						if (SetBounds(m_formDisplayUnicodeNamesLhs, ref rectBounds))
							SetBoundsClue(strClue, rectBounds);
						bCodePointFormSizeHasBeenSetLhs = true;
					}
					catch { }
				}

				nIndex = strLine.IndexOf(cstrCodePointFormClueRhs);
				if (nIndex != -1)
				{
					try
					{
						string strXYWH = strLine.Substring(nIndex + cstrCodePointFormClueRhs.Length);
						string[] aStrBounds = strXYWH.Split(new char[] { ',' });
						Rectangle rectBounds = new Rectangle
						(
							Int32.Parse(aStrBounds[0]),
							Int32.Parse(aStrBounds[1]),
							Int32.Parse(aStrBounds[2]),
							Int32.Parse(aStrBounds[3])
						);

						if (SetBounds(m_formDisplayUnicodeNamesRhs, ref rectBounds))
							SetBoundsClue(cstrCodePointFormClueRhs, rectBounds);
						bCodePointFormSizeHasBeenSetRhs = true;
					}
					catch { }
				}
			}

			// if we didn't find the ConvType, then query for it
			bool bUserCancelled = false;
			if (eConvType == ConvType.Unknown)
				bUserCancelled = !QueryConvType();
			else
				ConversionType = eConvType;

			InitTempVars();

			if (!bUserCancelled && !bLhsFontFound)
			{
				// for legacy encodings, prompt for the font.
				MessageBox.Show("Select the font for the left-hand side encoding", cstrCaption);
				bUserCancelled |= !SetFontLhs();
			}
			else if (unicodeValuesWindowToolStripMenuItem.Checked)
			{
				if (IsLhsLegacy && (m_nCodePageLegacyLhs == 0))
				{
					EncConverters aECs = new EncConverters(true);
					try
					{
						m_nCodePageLegacyLhs = aECs.CodePage(textBoxSample.Font.Name);
					}
					catch { }
				}

				m_formDisplayUnicodeNamesLhs.Initialize(IsLhsLegacy, textBoxSample.Font, m_nCodePageLegacyLhs);
			}

			if (!bUserCancelled && !bRhsFontFound)
			{
				// for legacy encodings, prompt for the font.
				MessageBox.Show("Select the font for the right-hand side encoding", cstrCaption);
				SetFontRhs();
			}
			else if (unicodeValuesWindowToolStripMenuItem.Checked)
			{
				if (IsRhsLegacy && (m_nCodePageLegacyRhs == 0))
				{
					EncConverters aECs = new EncConverters(true);
					try
					{
						m_nCodePageLegacyRhs = aECs.CodePage(textBoxSampleForward.Font.Name);
					}
					catch { }
				}

				m_formDisplayUnicodeNamesRhs.Initialize(IsRhsLegacy, textBoxSampleForward.Font, m_nCodePageLegacyRhs);
			}

			if (!bCodePointFormSizeHasBeenSetLhs)
			{
				Point ptLocation = new Point(Location.X + Bounds.Size.Width, Location.Y);
				m_formDisplayUnicodeNamesLhs.Location = ptLocation;
			}

			if (!bCodePointFormSizeHasBeenSetRhs)
			{
				Point ptLocation = new Point(m_formDisplayUnicodeNamesLhs.Location.X, m_formDisplayUnicodeNamesLhs.Location.Y + m_formDisplayUnicodeNamesLhs.Bounds.Size.Height);
				m_formDisplayUnicodeNamesRhs.Location = ptLocation;
			}

			if (!bLhsFontFound || !bRhsFontFound || !bCodePointFormSizeHasBeenSetLhs || !bCodePointFormSizeHasBeenSetRhs)
			{
				// initialize it with our clues
				string strPrefixHeader = cstrOpeningHeader + String.Format("v{1} on {2}.{0};   {3}{4}{0};   {5}{6};{7}{0};   {8}{9};{10}{0}{11}{0}{12}{0}{13}{0}",
					Environment.NewLine,
					Application.ProductVersion,
					DateTime.Now.ToShortDateString(),
					cstrConvTypeClue,
					m_eConvType.ToString(),
					cstrLhsFontClue,
					textBoxSample.Font.Name,
					textBoxSample.Font.Size,
					cstrRhsFontClue,
					textBoxSampleForward.Font.Name,
					textBoxSampleForward.Font.Size,
					BoundsClueString(cstrMainFormClue, Bounds),
					BoundsClueString(cstrCodePointFormClueLhs, m_formDisplayUnicodeNamesLhs.Bounds),
					BoundsClueString(cstrCodePointFormClueRhs, m_formDisplayUnicodeNamesRhs.Bounds));

				strPrefixHeader += AddCodePageClue(cstrLhsCodePageClue, m_nCodePageLegacyLhs);
				strPrefixHeader += AddCodePageClue(cstrRhsCodePageClue, m_nCodePageLegacyRhs);
				strPrefixHeader += String.Format("{0}", Environment.NewLine);

				this.richTextBoxMapEditor.Text = strPrefixHeader + SkipPast0900Header;
				bModified = true;
			}

			// so it re-creates for this "new" map
			m_aEC = null;
			Program.Modified = bModified;

			return true;
		}

		protected string SkipPast0900Header
		{
			get
			{
				// in v0.9.0.0, the header was like this:
				//  ; This file was edited using TECkitMappingEditorU.exe v0.9.0.0 on 6/23/2006.
				//  ;   Conversion Type = Legacy_to_from_Unicode
				//  ;   Left-hand side font = Annapurna;11.25
				//  ;   Right-hand side font = Arial Unicode MS;11.25
				//  ;   Main Window Position = 0,0,650,738
				//  ;   Code Point Window Position = 650,0,374,738
				if ((richTextBoxMapEditor.Lines[0].IndexOf(cstrOpeningHeader) >= 0)
					&& (richTextBoxMapEditor.Lines[1].IndexOf(cstrConvTypeClue) >= 0)
					&& (richTextBoxMapEditor.Lines[2].IndexOf(cstrLhsFontClue) >= 0)
					&& (richTextBoxMapEditor.Lines[3].IndexOf(cstrRhsFontClue) >= 0)
					&& (richTextBoxMapEditor.Lines[4].IndexOf(cstrMainFormClue) >= 0)
					&& (richTextBoxMapEditor.Lines[5].IndexOf(cstrDeprecatedCodePointFormClue) >= 0)
					&& (String.IsNullOrEmpty(richTextBoxMapEditor.Lines[6]))
					)
				{
					int i = 7;
					while (String.IsNullOrEmpty(richTextBoxMapEditor.Lines[i]))
						i++;
					int nIndex = richTextBoxMapEditor.Text.IndexOf(richTextBoxMapEditor.Lines[i]);
					if (nIndex >= 0)
						return richTextBoxMapEditor.Text.Substring(nIndex);
				}
				return this.richTextBoxMapEditor.Text;
			}
		}
		private bool m_bLhsLegacy = false;
		public bool IsLhsLegacy
		{
			get { return m_bLhsLegacy; }
			set { m_bLhsLegacy = value; }
		}

		private bool m_bRhsLegacy = false;
		public bool IsRhsLegacy
		{
			get { return m_bRhsLegacy; }
			set { m_bRhsLegacy = value; }
		}

		internal void NewDocument()
		{
			// reset the name
			m_strTecNameReal = m_strMapNameReal = null;

			InitCharacterMapDialogs();
			Program.AddFilenameToTitle(m_strMapNameReal);

			// query the user for the ConvType (kind of need it)
			bool bUserCancelled = !QueryConvType();

			InitTempVars();

			// reset vars in case they were something else
			m_nCodePageLegacyLhs = 0;
			m_nCodePageLegacyRhs = 0;

			if (!bUserCancelled)
			{
				// for legacy encodings, prompt for the font.
#if !DEBUG
				MessageBox.Show("Select the font for the left-hand side encoding", cstrCaption);
#endif
				bUserCancelled |= !SetFontLhs();
			}

			if (!bUserCancelled)
			{
				// for legacy encodings, prompt for the font.
#if !DEBUG
				MessageBox.Show("Select the font for the right-hand side encoding", cstrCaption);
#endif
				SetFontRhs();
			}

			Point ptLocation = new Point(Location.X + Bounds.Size.Width, Location.Y);
			m_formDisplayUnicodeNamesLhs.Location = ptLocation;

			ptLocation = new Point(m_formDisplayUnicodeNamesLhs.Location.X, m_formDisplayUnicodeNamesLhs.Location.Y + m_formDisplayUnicodeNamesLhs.Bounds.Size.Height);
			m_formDisplayUnicodeNamesRhs.Location = ptLocation;

			// initialize it with our clues
			this.richTextBoxMapEditor.Text = String.Format("; This file was created by <author> using TECkitMappingEditorU.exe v{1} on {2}.{0};   {3}{4}{0};   {5}{6};{7}{0};   {8}{9};{10}{0}{11}{0}{12}{0}{13}{0}{0}",
				Environment.NewLine,
				Application.ProductVersion,
				DateTime.Now.ToShortDateString(),
				cstrConvTypeClue,
				m_eConvType.ToString(),
				cstrLhsFontClue,
				textBoxSample.Font.Name,
				textBoxSample.Font.Size,
				cstrRhsFontClue,
				textBoxSampleForward.Font.Name,
				textBoxSampleForward.Font.Size,
				BoundsClueString(cstrMainFormClue, Bounds),
				BoundsClueString(cstrCodePointFormClueLhs, m_formDisplayUnicodeNamesLhs.Bounds),
				BoundsClueString(cstrCodePointFormClueRhs, m_formDisplayUnicodeNamesRhs.Bounds));

			if (IsLhsLegacy)
			{
				if (IsRhsLegacy)
				{
					this.richTextBoxMapEditor.Text += String.Format("LHSName                 \"canonical name of the 'source' encoding or left-hand side of the conversion\"{0}RHSName                 \"canonical name of the 'target' encoding or right-hand side of the conversion\"{0}LHSDescription          \"description for the left-hand side of the mapping\"{0}RHSDescription          \"description for the right-hand side of the mapping\"{0}Version                 \"1\"{0}Contact                 \"mailto:user@addr\"{0}RegistrationAuthority   \"the organization responsible for the encoding\"{0}RegistrationName        \"the name and version of the mapping, as recognized by that authority\"{0}Copyright               \"© {1} <CompanyName>. All rights reserved.\"{0}LHSFlags                (){0}RHSFlags                (){0}{0}pass(Byte){0}{0}; type a 'k' in the 'Left-side Sample' box below to see how this works.{0}107     <>  110     ; 'k' <> 'n'{0}", Environment.NewLine, DateTime.Today.Year);
				}
				else
				{
					this.richTextBoxMapEditor.Text += String.Format("EncodingName            \"a canonical name that uniquely identifies this mapping table from all others\"{0}DescriptiveName         \"a string that describes the mapping\"{0}Version                 \"1\"{0}Contact                 \"mailto:user@addr\"{0}RegistrationAuthority   \"the organization responsible for the encoding\"{0}RegistrationName        \"the name and version of the mapping, as recognized by that authority\"{0}Copyright               \"© {1} <CompanyName>. All rights reserved.\"{0}LHSFlags                (){0}RHSFlags                (){0}{0}pass(Byte_Unicode){0}{0}; type a 'k' in the 'Left-side Sample' box below to see how this works.{0}107     <>  LATIN_SMALL_LETTER_N    ; 'k' <> 'n'{0}", Environment.NewLine, DateTime.Today.Year);
				}
			}
			else
			{
				if (IsRhsLegacy)
				{
					this.richTextBoxMapEditor.Text += String.Format("LHSName                 \"canonical name of the 'source' encoding or left-hand side of the conversion\"{0}RHSName                 \"canonical name of the 'target' encoding or right-hand side of the conversion\"{0}LHSDescription          \"description for the left-hand side of the mapping\"{0}RHSDescription          \"description for the right-hand side of the mapping\"{0}Version                 \"1\"{0}Contact                 \"mailto:user@addr\"{0}RegistrationAuthority   \"the organization responsible for the encoding\"{0}RegistrationName        \"the name and version of the mapping, as recognized by that authority\"{0}Copyright               \"© {1} <CompanyName>. All rights reserved.\"{0}LHSFlags                (){0}RHSFlags                (){0}{0}pass(Unicode_Byte){0}{0}; type a 'k' in the 'Left-side Sample' box below to see how this works.{0}LATIN_SMALL_LETTER_K    <>  110 ; 'k' <> 'n'{0}", Environment.NewLine, DateTime.Today.Year);
				}
				else
				{
					this.richTextBoxMapEditor.Text += String.Format("LHSName                 \"canonical name of the 'source' encoding or left-hand side of the conversion\"{0}RHSName                 \"canonical name of the 'target' encoding or right-hand side of the conversion\"{0}LHSDescription          \"description for the left-hand side of the mapping\"{0}RHSDescription          \"description for the right-hand side of the mapping\"{0}Version                 \"1\"{0}Contact                 \"mailto:user@addr\"{0}RegistrationAuthority   \"the organization responsible for the encoding\"{0}RegistrationName        \"the name and version of the mapping, as recognized by that authority\"{0}Copyright               \"© {1} <CompanyName>. All rights reserved.\"{0}LHSFlags                (){0}RHSFlags                (){0}{0}pass(Unicode){0}{0}; type a 'k' in the 'Left-side Sample' box below to see how this works.{0}LATIN_SMALL_LETTER_K    <>  LATIN_SMALL_LETTER_N    ; 'k' <> 'n'{0}", Environment.NewLine, DateTime.Today.Year);
				}
			}

			this.richTextBoxMapEditor.Select(richTextBoxMapEditor.Text.Length, 0);

			// so it re-creates for this "new" map
			m_aEC = null;
			Program.Modified = false;   // until the user does something, this doesn't need to be saved
		}

		protected bool QueryConvType()
		{
			// query the user for the ConvType (kind of need it)
			QueryConvTypeDlg aQuery = new QueryConvTypeDlg();
			bool bSelected = (aQuery.ShowDialog() == DialogResult.OK);
			ConversionType = aQuery.ConversionType;
			return bSelected;
		}

		protected void InitTempVars()
		{
			// use temporary files as a scratch area
			string strTempName = Path.GetTempFileName();
			strTempName = strTempName.Remove(strTempName.Length - 3, 3);
			m_strMapNameTemp = strTempName + "map";
			m_strTecNameTemp = strTempName + "tec";
		}

		public void SaveTempAndCompile()
		{
			File.WriteAllLines(m_strMapNameTemp, this.richTextBoxMapEditor.Lines, m_enc);

			try
			{
				string strTecName = m_strTecNameTemp;
				TECkitDllWrapper.CompileMap(m_strMapNameTemp, ref strTecName);

				// if the compiler had to use a different file, then clear the reference to rebuild it
				// if (m_strTecNameTemp != strTecName)
				m_aEC = null;

				this.textBoxCompilerResults.Text = "Compiled successfully!";

				// now that we have something we can compile successfully, create a converter of it
				if (m_aEC == null)
				{
					m_strTecNameTemp = strTecName;
					m_aEC = new TecEncConverter();
					string strDummy = null;
					int nProcType = (int)ProcessTypeFlags.DontKnow;
					m_aEC.Initialize(cstrMyEncConverterName, m_strTecNameTemp,
						ref strDummy, ref strDummy, ref m_eConvType, ref nProcType, m_nCodePageLegacyLhs, m_nCodePageLegacyRhs, true);
				}

				if (this.textBoxSample.Text.Length > 0)
				{
					UpdateTextChanged(this.textBoxSample, this.textBoxSampleForward, m_aEC, true);
					UpdateUnicodeDetails(this.textBoxSample);
				}
			}
			catch (Exception ex)
			{
				this.textBoxCompilerResults.Text = ex.Message;

				// clear the converter so we don't accidentally call it while we don't have a good map
				m_aEC = null;
			}
		}

		internal void AddStringToEditor(string str)
		{
			richTextBoxMapEditor.SelectedText = str;
			richTextBoxMapEditor.Focus();
		}

		private void AddCharToTextBox(TextBox tb, char ch)
		{
			tb.SelectionStart = tb.Text.Length;
			tb.SelectedText = new string(ch, 1);
			tb.Focus();
		}

		internal void AddCharToSampleBox(char ch, bool bLhs)
		{
			if (bLhs)
				AddCharToTextBox(textBoxSample, ch);
			else
				AddCharToTextBox(textBoxSampleForward, ch);
		}

		private void compileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Program.Modified)
				SaveClicked();  // implicit save

			CompileRealMap();
		}

		private bool m_bSampleChangedByTyping = false;

		private void textBoxSample_TextChanged(object sender, EventArgs e)
		{
			// sample data has changed, so do the forward and reverse conversions and update the panes
			m_bSampleChangedByTyping = true;    // prevent Unicode update unless explicitly typing in B
			UpdateTextChanged(this.textBoxSample, this.textBoxSampleForward, m_aEC, true);
			UpdateUnicodeDetails(this.textBoxSample);
		}

		const string cstrRoundTripDefaultLabel = "R&ound-trip:";

		private void textBoxSampleForward_TextChanged(object sender, EventArgs e)
		{
			UpdateTextChanged(this.textBoxSampleForward, this.textBoxSampleReverse, m_aEC, false);
			if (!m_bSampleChangedByTyping)
				UpdateUnicodeDetails(this.textBoxSampleForward);
			m_bSampleChangedByTyping = false;

			string strTooltip = null;
			if (textBoxSample.Text == textBoxSampleReverse.Text)
			{
				this.labelRoundtrip.Text = cstrRoundTripDefaultLabel;
				strTooltip = "Round-trip value matches!";
			}
			else
			{
				labelRoundtrip.Text = "*** " + cstrRoundTripDefaultLabel;
				strTooltip = "Round-trip value doesn't match!";
			}

			strTooltip += String.Format("{0}{0}Left-side:\t{1}{0}Right-side:\t{2}{0}Roundtrip:\t{3}",
					Environment.NewLine,
					GetCodePointsForToolTip(IsLhsLegacy, true, textBoxSample.Text),
					GetCodePointsForToolTip(IsRhsLegacy, false, textBoxSampleForward.Text),
					GetCodePointsForToolTip(IsLhsLegacy, true, textBoxSampleReverse.Text));

			toolTip.SetToolTip(labelRoundtrip, strTooltip);
		}

		private string GetCodePointsForToolTip(bool bLegacy, bool bLhs, string strInput)
		{
			string strWhole = null;
			if (bLegacy)
			{
				// convert it to byte format first
				int nCP = (bLhs) ? m_nCodePageLegacyLhs : m_nCodePageLegacyRhs;
				byte[] abyValues = EncConverters.GetBytesFromEncoding(nCP, strInput, true);

				for (int i = 0; i < abyValues.Length; i++)
					strWhole += String.Format("{0:D3}\t", abyValues[i]);
			}
			else
			{
				for (int i = 0; i < strInput.Length; i++)
					strWhole += String.Format("u{0:X4}\t", (int)strInput[i]);
			}
			return strWhole;
		}

		void textBoxSample_UpdateUnicodeValues(object sender, System.EventArgs e)
		{
			UpdateUnicodeDetails(this.textBoxSample);
		}

		void textBoxSampleForward_UpdateUnicodeValues(object sender, System.EventArgs e)
		{
			m_bSampleChangedByTyping = false;
			UpdateUnicodeDetails(this.textBoxSampleForward);
		}

		void textBoxSampleReverse_UpdateUnicodeChars(object sender, System.EventArgs e)
		{
			m_bSampleChangedByTyping = false;
			UpdateUnicodeDetails(this.textBoxSampleReverse);
		}

		private void UpdateTextChanged(TextBox tbSrc, TextBox tbDst, IEncConverter aEC, bool bDirectionForward)
		{
			if (tbSrc.Text.Length > 0)
			{
				if (aEC != null)
				{
					try
					{
						tbDst.Text = null;  // to force the "TextChanged"
						aEC.DirectionForward = bDirectionForward;
						tbDst.Text = aEC.Convert(tbSrc.Text);
					}
					catch (ECException e)
					{
						if (e.ErrorCode != (int)ErrStatus.InvalidForm)
							MessageBox.Show(e.Message);
					}
				}
			}
			else
				tbDst.Text = null;
		}

		private void UpdateUnicodeDetails(TextBox tb)
		{
			/*
			if ((m_formDisplayUnicodeNames != null) && (m_formDisplayUnicodeNames.Visible))
			*/
			{
				string strInput = tb.SelectedText;

				if (strInput.Length == 0)
					strInput = tb.Text;

				// first clear the current contents
				Reset();

				if (strInput.Length > 0)
				{
					bool bShowLhs = (tb != textBoxSampleForward);
					ShowSide(bShowLhs, tb.Font);
					Add(strInput, bShowLhs);
				}
			}
		}

		private void unicodeValuesWindowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (unicodeValuesWindowToolStripMenuItem.Checked)
			{
				m_formDisplayUnicodeNamesLhs.Show();
				m_formDisplayUnicodeNamesRhs.Show();
			}
			else
			{
				m_formDisplayUnicodeNamesLhs.Hide();
				m_formDisplayUnicodeNamesRhs.Hide();
			}
		}

		private string m_strAddlStatusBarMessage = null;

		internal void UpdateStatusBar()
		{
			int nFirstChar = richTextBoxMapEditor.GetFirstCharIndexOfCurrentLine();
			int nLineNumber = richTextBoxMapEditor.GetLineFromCharIndex(nFirstChar);
			string strText = String.Format("Line: {0}", nLineNumber + 1);
			if (m_strAddlStatusBarMessage != null)
				strText += "; " + m_strAddlStatusBarMessage;
			this.toolStripStatusLabel.Text = strText;
		}

		private void richTextBoxMapEditor_TextChanged(object sender, EventArgs e)
		{
			if (Program.myTimer.Enabled)
				Program.RestartTimer();
			else
				Program.myTimer.Enabled = true;
			Program.Modified = true;
		}

		private void setSampleDataFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetFontLhs();
		}

		private void setConvertedDataFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetFontRhs();
		}

		private void labelSampleLhs_DoubleClick(object sender, EventArgs e)
		{
			SetFontLhs();
		}

		private void labelRhsSample_DoubleClick(object sender, EventArgs e)
		{
			SetFontRhs();
		}

		public void DisplayUnicodeCodePointForms()
		{
			m_formDisplayUnicodeNamesLhs.Show();
			m_formDisplayUnicodeNamesRhs.Show();
			/*
			m_formDisplayUnicodeNamesLhs.Initialize(IsLhsLegacy, textBoxSample.Font);
			m_formDisplayUnicodeNamesRhs.Initialize(IsRhsLegacy, textBoxSampleForward.Font);
			*/
		}

		private bool SetFontLhs()
		{
#if false   // DEBUG
			fontDialog.Font = new Font("Annapurna", 12);
#else
			fontDialog.Font = textBoxSample.Font;
			if (fontDialog.ShowDialog() != DialogResult.Cancel)
			{
#endif
				textBoxSample.Font = fontDialog.Font;
				textBoxSampleReverse.Font = fontDialog.Font;
				SetFontClue(cstrLhsFontClue, fontDialog.Font);
				if (unicodeValuesWindowToolStripMenuItem.Checked)
				{
					if (IsLhsLegacy)
					{
						EncConverters aECs = new EncConverters(true);
						try
						{
							m_nCodePageLegacyLhs = aECs.CodePage(fontDialog.Font.Name);
						}
						catch { }
					}

					m_formDisplayUnicodeNamesLhs.Initialize(IsLhsLegacy, textBoxSample.Font, m_nCodePageLegacyLhs);
				}

				return true;
#if true    // !DEBUG
			}
			else
				return false;
#endif
		}

		private void SetFontRhs()
		{
#if false   // DEBUG
			fontDialog.Font = new Font("Arial Unicode MS", 12);
#else
			fontDialog.Font = textBoxSampleForward.Font;
			if (fontDialog.ShowDialog() != DialogResult.Cancel)
#endif
			{
				textBoxSampleForward.Font = fontDialog.Font;
				SetFontClue(cstrRhsFontClue, fontDialog.Font);

				// only do this if the lhs is *not* legacy and the rhs is
				if (unicodeValuesWindowToolStripMenuItem.Checked)
				{
					if (IsRhsLegacy)
					{
						EncConverters aECs = new EncConverters(true);
						try
						{
							m_nCodePageLegacyRhs = aECs.CodePage(fontDialog.Font.Name);
						}
						catch { }
					}

					m_formDisplayUnicodeNamesRhs.Initialize(IsRhsLegacy, textBoxSampleForward.Font, m_nCodePageLegacyRhs);
				}
			}
		}

		private void SetFontClue(string cstrClueHeader, Font font)
		{
			string[] aStrLines = richTextBoxMapEditor.Lines;
			for (int i = 0; i < aStrLines.Length; i++)
			{
				int nIndex = aStrLines[i].IndexOf(cstrClueHeader);
				if (nIndex != -1)
				{
					aStrLines[i] = String.Format(";   {0}{1};{2}", cstrClueHeader, font.Name, font.Size);
					richTextBoxMapEditor.Lines = aStrLines;
					Program.Modified = true;
					break;
				}
			}
		}

		internal bool DoAutoCompile()
		{
			return this.autoCompileToolStripMenuItem.Checked;
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveClicked();
		}

		private void SaveClicked()
		{
			if (m_strMapNameReal == null)
				SaveAsClicked();
			else
				SaveFile(m_strMapNameReal);
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsClicked();
		}

		protected void SaveAsClicked()
		{
			if (this.saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				m_strMapNameReal = saveFileDialog.FileName;
				m_strTecNameReal = m_strMapNameReal.Remove(m_strMapNameReal.Length - 3, 3) + "tec";
				SaveFile(m_strMapNameReal);
			}
		}

		DateTime m_dtLastSave = DateTime.Now;
		TimeSpan m_tsBetweenBackups = new TimeSpan(0, 0, 0);    // every time

		protected void SaveFile(string strFilename)
		{
			try
			{
				File.WriteAllLines(strFilename, this.richTextBoxMapEditor.Lines, m_enc);
			}
			catch (UnauthorizedAccessException)
			{
				MessageBox.Show(String.Format("The map file '{0}' is locked. Is it read-only? Or opened in some other program? Unlock it and try again.", strFilename), cstrCaption);
				return;
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("Unable to save the map file '{1}'{0}{0}{2}", Environment.NewLine, strFilename, ex.Message), cstrCaption);
				return;
			}

			Program.Modified = (m_strMapNameReal != strFilename);
			Program.AddFilenameToTitle(strFilename);

			// if it's been 5 minutes since our last backup...
			if ((DateTime.Now - m_dtLastSave) > m_tsBetweenBackups)
			{
				// ... hide a copy in the user's Application Data file
				File.Copy(m_strMapNameReal, GetBackupFilename(strFilename), true);
			}
		}

		private string GetBackupFilename(string strFilename)
		{
			return Application.UserAppDataPath + @"\Backup of " + Path.GetFileName(strFilename);
		}

		private void revertTolastSavedCopyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenDocument(GetBackupFilename(m_strMapNameReal));
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.openFileDialog.ShowDialog() == DialogResult.OK)
			{
				this.OpenDocument(this.openFileDialog.FileName);
				Debug.Assert(m_strMapNameReal == openFileDialog.FileName);
				this.SaveTempAndCompile();
			}
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Program.Modified)
				CheckForSaveDirtyFile();

			this.NewDocument();
			this.SaveTempAndCompile();
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.richTextBoxMapEditor.Undo();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.richTextBoxMapEditor.Cut();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.richTextBoxMapEditor.Copy();
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.richTextBoxMapEditor.SelectAll();
		}

		private void findToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_formFindReplace.InsertionPoint = richTextBoxMapEditor.SelectionStart;
			m_formFindReplace.FindWhat = richTextBoxMapEditor.SelectedText;
			m_formFindReplace.Show();
		}

		private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_formFindReplace.FindWhat.Length == 0)
				findToolStripMenuItem_Click(sender, e);
			else
				m_formFindReplace.CallFindNext();
		}

		private void openHelpDocumentToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// F:\Program Files\Common Files\SIL\Help\TECkit\
			string strPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + cstrHelpDocumentPath;
			Program.LaunchProgram(strPath, "");
		}

		protected bool CompileRealMap()
		{
			// we have to have a file saved to add it
			if (m_strMapNameReal == null)
				saveAsToolStripMenuItem_Click(null, null);

			if (m_strMapNameReal == null)
				return false;

			string strTecName = m_strTecNameReal;
			try
			{
				TECkitDllWrapper.CompileMap(m_strMapNameReal, ref strTecName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
				return false;
			}

			if (strTecName != m_strTecNameReal)
			{
				MessageBox.Show(String.Format("oops... the output tec file '{0}' is locked. Close and re-open the program and try again.", m_strTecNameReal), cstrCaption);
				return false;
			}

			return true;
		}

		private void addToSystemRepositoryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// we have to have a file saved to add it
			if (m_strMapNameReal == null)
				saveAsToolStripMenuItem_Click(sender, e);

			// check again, just in case they cancelled.
			if (m_strMapNameReal != null)
			{
				m_aEC = null;   // free it so we can re-create it with the "real" content
				string strFriendlyName = null, strLhsEncodingID = null, strRhsEncodingID = null;
				if (CompileRealMap())
				{
					m_aEC = new TecEncConverter();
					int nProcType = (int)ProcessTypeFlags.DontKnow;
					m_aEC.Initialize(cstrMyEncConverterName, m_strTecNameReal,
						ref strLhsEncodingID, ref strRhsEncodingID, ref m_eConvType, ref nProcType, 0, 0, true);
				}
				else
					return; // compile failed, so just quit

				if (m_aEC != null)
				{
					try
					{
						strFriendlyName = String.Format("{0}{2}{1}",
							strLhsEncodingID,
							strRhsEncodingID,
							(EncConverters.IsUnidirectional(m_aEC.ConversionType)) ? ">" : "<>");
					}
					catch { }

					EncConverters aECs = new EncConverters();
					IEncConverterConfig rConfigurator = m_aEC.Configurator;

					// call its Configure method to do the UI
					rConfigurator.Configure(aECs, strFriendlyName, m_eConvType, strLhsEncodingID, strRhsEncodingID);
				}
			}
		}

		private void TECkitMapEditorForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = (CheckForSaveDirtyFile() == DialogResult.Cancel);
		}

		private DialogResult CheckForSaveDirtyFile()
		{
			DialogResult res = DialogResult.None;
			if (Program.Modified)
			{
				res = MessageBox.Show("Do you want to save this file before quitting?", cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.Yes)
					SaveClicked();
			}
			return res;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void richTextBoxMapEditor_KeyPress(object sender, KeyPressEventArgs e)
		{
			// turn tabs into spaces
			if (e.KeyChar == (char)Keys.Tab)
			{
				int nCharIndex = richTextBoxMapEditor.SelectionStart;
				int nLineIndex = richTextBoxMapEditor.GetLineFromCharIndex(nCharIndex);
				int nCharIndexOfLine = richTextBoxMapEditor.GetFirstCharIndexFromLine(nLineIndex);
				int nCharIndexInLine = nCharIndex - nCharIndexOfLine;
				int nNumSpacesToNextTabPos = 4 - (nCharIndexInLine % 4);
				string strSpaceTab = "";
				while (nNumSpacesToNextTabPos-- > 0) strSpaceTab += ' ';
				richTextBoxMapEditor.SelectedText = strSpaceTab;
				e.Handled = true;
			}
			else if (e.KeyChar == (char)Keys.Escape)
			{
				if (m_formFindReplace.Visible)
				{
					m_formFindReplace.Close();  // this won't actually close it, but just 'hide' it
					e.Handled = true;
				}
			}
		}

		void recentFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripDropDownItem aRecentFile = (ToolStripDropDownItem)sender;
			try
			{
				OpenDocument(aRecentFile.Text);
			}
			catch (Exception ex)
			{
				// probably means the file doesn't exist anymore, so remove it from the recent used list
				Properties.Settings.Default.RecentFiles.Remove(aRecentFile.Text);
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			recentFilesToolStripMenuItem.DropDownItems.Clear();
			foreach (string strRecentFile in Properties.Settings.Default.RecentFiles)
				recentFilesToolStripMenuItem.DropDownItems.Add(strRecentFile, null, recentFilesToolStripMenuItem_Click);
			recentFilesToolStripMenuItem.Enabled = (recentFilesToolStripMenuItem.DropDownItems.Count > 0);
		}

		private void toggleCodePointToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (textBoxSample.Focused)
			{
				ConvertNumberToChar(textBoxSample, IsLhsLegacy);
			}
			else if (textBoxSampleForward.Focused)
			{
				ConvertNumberToChar(textBoxSampleForward, IsRhsLegacy);
			}
			else if (textBoxSampleReverse.Focused)
			{
				ConvertNumberToChar(textBoxSampleReverse, IsLhsLegacy);
			}
		}

		protected void ConvertNumberToChar(TextBox tb, bool bIsLegacy)
		{
			int nCharsToLook = 4;   // assume unicode
			if (bIsLegacy)
				nCharsToLook = 3;
			int nCaretLocation = tb.SelectionStart;
			tb.Select(Math.Max(0, nCaretLocation - nCharsToLook), nCharsToLook);
			string str = null;
			int nVal = 0;
			try
			{
				if (bIsLegacy)
				{
					if (tb.SelectedText[0] == 'x') // hex format
					{
						tb.Select(tb.SelectionStart - 1, 4);    // grab the preceding '0' as well
						nVal = Convert.ToInt32(tb.SelectedText, 16);
					}
					else
						nVal = Convert.ToInt32(tb.SelectedText, 10);

					// however, the value is a byte value and for display we need Unicode (word) values
					byte[] aby = new byte[1] { (byte)nVal };
					char[] ach = Encoding.GetEncoding(0).GetChars(aby);
					nVal = ach[0];
				}
				else
					nVal = Convert.ToInt32(tb.SelectedText, 16);
				str += (char)nVal;
				tb.SelectedText = str;
			}
			catch (FormatException)
			{
				// give up and go back to the way it was
				if (nCaretLocation > 0)
				{
					tb.Select(nCaretLocation - 1, 1);
					if (tb.SelectedText.Length > 0)
					{
						char ch = tb.SelectedText[0];
						string strHex = String.Format("{0:X4}", (int)ch);
						tb.SelectedText = strHex;
						return;
					}
				}

				tb.Select(nCaretLocation, 0);
			}
		}
		/*
		private void textBoxSample_DragDrop(object sender, DragEventArgs e)
		{
			textBoxSample.SelectedText = (string)e.Data.GetData(typeof(string));
			ConvertNumberToChar(textBoxSample, IsLhsLegacy);
		}

		private void textBoxSample_DragOver(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(typeof(string)))
				e.Effect = DragDropEffects.All;
		}

		private void textBoxSampleForward_DragDrop(object sender, DragEventArgs e)
		{
			textBoxSampleForward.SelectedText = (string)e.Data.GetData(typeof(string));
			ConvertNumberToChar(textBoxSampleForward, IsRhsLegacy);
		}
		*/
		void textBoxCompilerResults_JumpToCompilerError(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			int nCharIndex = textBoxCompilerResults.GetCharIndexFromPosition(e.Location);
			if (nCharIndex >= 0)
			{
				int nLineIndex = textBoxCompilerResults.GetLineFromCharIndex(nCharIndex);
				if ((nLineIndex >= 0) && (nLineIndex < textBoxCompilerResults.Lines.Length))
				{
					string strLine = textBoxCompilerResults.Lines[nLineIndex];
					if ((strLine != null) && (strLine.Length > 0))
					{
						int nLineNumIndex = strLine.LastIndexOf(TECkitDllWrapper.cstrLineNumClue);
						if (nLineNumIndex != -1)
						{
							string strLineNumber = strLine.Substring(nLineNumIndex + TECkitDllWrapper.cstrLineNumClue.Length);
							int nLineNumber;
							if (Int32.TryParse(strLineNumber, out nLineNumber))
							{
								// index is zero-based, so subtract 1
								nLineNumber--;

								// sometimes the compiler gives us a bad line number
								if (nLineNumber >= richTextBoxMapEditor.Lines.Length)
									nLineNumber = richTextBoxMapEditor.Lines.Length - 1;

								// see if we can find the actual bad parameter (enclosed in "s)
								int nSelectStart = this.richTextBoxMapEditor.GetFirstCharIndexFromLine(nLineNumber);
								string strMapLine = this.richTextBoxMapEditor.Lines[nLineNumber];
								int nSelectLength = strMapLine.Length;
								try
								{
									int nRightQuote = strLine.LastIndexOf('"');
									int nLeftQuote = strLine.LastIndexOf('"', nRightQuote - 1);
									string strBadParam = strLine.Substring(nLeftQuote + 1, nRightQuote - nLeftQuote - 1);
									int nBadParamIndex = strMapLine.LastIndexOf(strBadParam);
									if (nBadParamIndex != -1)
									{
										nSelectStart += nBadParamIndex;
										nSelectLength = strBadParam.Length;
									}
								}
								catch { }
								richTextBoxMapEditor.Select(nSelectStart, nSelectLength);
								richTextBoxMapEditor.Focus();
							}
						}
					}
				}
			}
		}

		// keep track of the time we get a ctrl+arrow so that if it's adjacent to an underscore,
		//  we'll skip to the end of the word.
		private long m_ticksSinceLastPreviewKeyDown = 0;
		private bool m_bRightArrow = false;
		private void richTextBoxMapEditor_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.Control)
			{
				if ((e.KeyValue == (int)Keys.Left) || (e.KeyValue == (int)Keys.Right))
				{
					m_bRightArrow = (e.KeyValue == (int)Keys.Right);
					m_ticksSinceLastPreviewKeyDown = DateTime.Now.Ticks;
				}
			}

			System.Diagnostics.Trace.WriteLine(String.Format("PreviewKeyDown: KeyValue: {0} at: {1}", e.KeyValue, m_ticksSinceLastPreviewKeyDown));
		}

		// keep track of how long it's been since our last text change and if we almost immediately
		//  get a double-click following (i.e. the change in selection is a result of the double-click)
		//  then trim the selection (to behave more like the VS.Net IDE).
		// The events come in like this:
		//  SelectionChanged (from the initial click)
		//  MouseClick (from the initial click)
		//  SelectionChanged (from the control selecting the word)
		//      it is here that we re-do the selection to get what we want
		//  SelectionChanged (as a result of what we re-do)
		//  MouseDoubleClick (but by then, too much time has elapsed and the selection looks weird)
		//
		//  So, take a time snapshop during the MouseClick and if the subsequent SelectionChanged is
		//  within a minimum amount of time, then assume that we're in this situation.
		private long m_ticksSinceLastMouseClick = 0;
		const long m_ticksMinForDoubleClick = 1500000;
		private char[] m_achTrim = new char[] { ' ', '\t', ',', '\n', '\'', '\"' }; // chars to delimit a word
		private char[] m_achExtend = new char[] { '_', '.', '-', '/', ',', '@' };      // chars to expand across

		private void richTextBoxMapEditor_MouseClick(object sender, MouseEventArgs e)
		{
			m_ticksSinceLastMouseClick = DateTime.Now.Ticks;
			System.Diagnostics.Debug.WriteLine(String.Format("got MouseClick at: {0}", m_ticksSinceLastMouseClick));
		}

		private void richTextBoxMapEditor_SelectionChanged(object sender, EventArgs e)
		{
			long lTicks = DateTime.Now.Ticks;
			System.Diagnostics.Debug.WriteLine(String.Format("got SelectionChanged at: {0}", lTicks));
#if !TurnOffSpecialSelectionCode
			try
			{
				if ((lTicks - m_ticksSinceLastPreviewKeyDown) < m_ticksMinForDoubleClick)
				{
					m_ticksSinceLastPreviewKeyDown = 0; // reset so it doesn't re-occur

					// if either the next or preceding character is an "_", then keep skipping to the next word
					int nCharIndex = richTextBoxMapEditor.SelectionStart;
					if (m_bRightArrow)
						nCharIndex += richTextBoxMapEditor.SelectionLength;

					nCharIndex = Math.Min(nCharIndex, richTextBoxMapEditor.Text.Length - 1);
					string strRight = richTextBoxMapEditor.Text.Substring(nCharIndex, 1);
					char chLeft = (nCharIndex > 0) ? richTextBoxMapEditor.Text[nCharIndex - 1] : (char)0;

					// several characters we only want to deal with hitting one side of them, but the
					//  underscore is special: we don't want to stop at either side if it.
					if ((strRight.IndexOfAny(m_achExtend) != -1) || (chLeft == '_'))
					{
						if (m_bRightArrow)
							SendKeys.Send("{RIGHT}");
						else
							SendKeys.Send("{LEFT}");
					}
				}

				else if ((lTicks - m_ticksSinceLastMouseClick) < m_ticksMinForDoubleClick)
				{
					m_ticksSinceLastMouseClick = 0;   // reset so it doesn't re-occur

					// trim up and extend the selected text to include full words only
					string strSelection = richTextBoxMapEditor.SelectedText.TrimEnd(m_achTrim);
					int nSelStart = richTextBoxMapEditor.SelectionStart;
					int nTextLen = richTextBoxMapEditor.Text.Length;
					int nSelLen = strSelection.Length;

					// see if this is a Unicode Name (i.e. extend to a space/nl/etc if "_"s present)
					const int nLongestUnicodeName = 60;
					int nSearchStart = Math.Min(nSelStart + nSelLen, nTextLen - 1);
					int nSearchLen = Math.Min(nLongestUnicodeName, nTextLen - nSearchStart);

					// check if the following character is an underscore
					if (richTextBoxMapEditor.Text.IndexOfAny(m_achExtend, nSearchStart, 1) != -1)
					{
						// read the following stuff into a string to search it (the Text.Find isn't finding '\n')
						string strForwardText = richTextBoxMapEditor.Text.Substring(nSearchStart, nSearchLen);

						int nIndexEnd = strForwardText.IndexOfAny(m_achTrim);
						if (nIndexEnd != -1)
							nSelLen += nIndexEnd;   // extend to the edge of the word
					}

					// see if the preceding character is an underscore
					if ((nSelStart > 0) && (richTextBoxMapEditor.Text.IndexOfAny(m_achExtend, nSelStart - 1, 1) != -1))
					{
						nSearchStart = Math.Max(0, nSelStart - nLongestUnicodeName);

						// read in the preceding stuff into a string to search it.
						string strBackText = richTextBoxMapEditor.Text.Substring(nSearchStart, nSelStart - nSearchStart);

						int nIndexStart = strBackText.LastIndexOfAny(m_achTrim);

						// if it was at the beginning, we might not find a delimiting char
						int nImplicitExtend = 1;
						if ((nIndexStart == -1) && (nSearchStart == 0))
							nIndexStart = nImplicitExtend = 0;

						if (nIndexStart != -1)
						{
							int nLeftExtendLen = (strBackText.Length - nIndexStart);
							nSelStart -= nLeftExtendLen - nImplicitExtend;
							nSelLen += nLeftExtendLen - nImplicitExtend;
						}
					}

					richTextBoxMapEditor.Select(nSelStart, nSelLen);
				}
			}
			catch { }   // the above is just gravy; don't crash because of it
#endif
			this.UpdateStatusBar();
		}

		private void TECkitMapEditorForm_ResizeEnd(object sender, EventArgs e)
		{
			// everytime we are moved or resized, re-write the Bounds rectangle
			SetBoundsClue(cstrMainFormClue, Bounds);
		}

		private string BoundsClueString(string strClueHeader, Rectangle rect)
		{
			return String.Format(";   {0}{1},{2},{3},{4}",
						strClueHeader,
						rect.X,
						rect.Y,
						rect.Width,
						rect.Height);
		}

		protected string AddCodePageClue(string strClueHeader, int nCodePage)
		{
			if (nCodePage != 0)
				return String.Format(";   {1}{2}{0}", Environment.NewLine, strClueHeader, nCodePage);
			else
				return null;
		}

		internal void SetBoundsClue(string strClueHeader, Rectangle rectBounds)
		{
			// keep track of the bottom line visible in the editor (so we can reset it after changing
			//  the file).
			Point ptBottomLine = new Point(3, richTextBoxMapEditor.Bounds.Bottom - 30);
			int nCharIndexOfBottomVisibleLine = richTextBoxMapEditor.GetCharIndexFromPosition(ptBottomLine);
			int nSelectionStart = richTextBoxMapEditor.SelectionStart;
			int nSelectionLength = richTextBoxMapEditor.SelectionLength;

			string[] aStrLines = richTextBoxMapEditor.Lines;
			for (int i = 0; i < aStrLines.Length; i++)
			{
				string strClue = strClueHeader;
				int nIndex = aStrLines[i].IndexOf(strClue);
				if (nIndex == -1)
				{
					// deal with an old file which used a different clue
					strClue = cstrDeprecatedCodePointFormClue;
					nIndex = aStrLines[i].IndexOf(strClue);
				}

				if (nIndex != -1)
				{
					aStrLines[i] = BoundsClueString(strClueHeader, rectBounds);
					richTextBoxMapEditor.Lines = aStrLines;
					Program.Modified = true;

					// restore the position (first select the bottom visible line, then whatever was selected
					richTextBoxMapEditor.Select(nCharIndexOfBottomVisibleLine, 0);
					richTextBoxMapEditor.Select(nSelectionStart, nSelectionLength);
					break;
				}
			}
		}

		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult res = DialogResult.OK;
			if (Program.Modified)
				res = CheckForSaveDirtyFile();

			if (res != DialogResult.Cancel)
			{
				this.NewDocument();
				this.SaveTempAndCompile();
			}
		}

		private void autoCompileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.autoCompileToolStripMenuItem.Checked)
			{
				m_strAddlStatusBarMessage = null;
				Program.RestartTimer();
			}
			else
			{
				m_strAddlStatusBarMessage = "Auto-compile paused! Press F5 to resume.";
			}

			UpdateStatusBar();
		}

		private void viewToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			unicodeValuesWindowToolStripMenuItem.Checked = m_formDisplayUnicodeNamesLhs.Visible;
		}

		const int cnColumnIndexHex = 0;
		const int cnColumnIndexDec = 1;
		const int cnColumnIndexUName = 2;
		const int cnColumnIndexUValue = 3;
		const int cnColumnIndexChar = 4;

		public void Reset()
		{
			this.dataGridViewCodePointValues.Rows.Clear();
			// hide all the rows
			dataGridViewCodePointValues.Columns[cnColumnIndexHex].Visible = false;
			dataGridViewCodePointValues.Columns[cnColumnIndexDec].Visible = false;
			dataGridViewCodePointValues.Columns[cnColumnIndexUName].Visible = false;
			dataGridViewCodePointValues.Columns[cnColumnIndexUValue].Visible = false;
			dataGridViewCodePointValues.Columns[cnColumnIndexChar].Visible = false;
		}

		public void Add(string str, bool bLhs)
		{
			toolTip.SetToolTip(dataGridViewCodePointValues, null);
			const string strTooltipText = "Click a cell to have its contents inserted into the map";

			if (IsShowingLegacy)
			{
				dataGridViewCodePointValues.Columns[cnColumnIndexHex].Visible = true;
				dataGridViewCodePointValues.Columns[cnColumnIndexDec].Visible = true;
				dataGridViewCodePointValues.Columns[cnColumnIndexChar].Visible = true;

				int nCP = (bLhs) ? m_nCodePageLegacyLhs : m_nCodePageLegacyRhs;
				byte[] aby = EncConverters.GetBytesFromEncoding(nCP, str, true);
				int nLength = aby.Length;
				int i = 0;
				foreach (byte by in aby)
				{
					int nRowIndex = dataGridViewCodePointValues.Rows.Add();
					DataGridViewRow aRow = dataGridViewCodePointValues.Rows[nRowIndex];

					int nValue = (int)by;
					aRow.Cells[cnColumnIndexHex].Value = String.Format("0x{0:X2}", nValue);
					aRow.Cells[cnColumnIndexHex].ToolTipText = strTooltipText;

					aRow.Cells[cnColumnIndexChar].Value = String.Format("{0}", str[i++]);
					aRow.Cells[cnColumnIndexChar].ToolTipText = strTooltipText;

					aRow.Cells[cnColumnIndexDec].Value = String.Format("{0:D}", nValue);
					aRow.Cells[cnColumnIndexDec].ToolTipText = strTooltipText;

					dataGridViewCodePointValues.Tag = str;
				}
			}
			else
			{
				dataGridViewCodePointValues.Columns[cnColumnIndexUName].Visible = true;
				dataGridViewCodePointValues.Columns[cnColumnIndexChar].Visible = true;
				dataGridViewCodePointValues.Columns[cnColumnIndexUValue].Visible = true;

				foreach (char ch in str)
				{
					int nRowIndex = dataGridViewCodePointValues.Rows.Add();
					DataGridViewRow aRow = dataGridViewCodePointValues.Rows[nRowIndex];

					string strOutput = null;
					try
					{
						strOutput = Program.GetUnicodeName(ch);
					}
					catch (Exception)
					{
						strOutput = "No Unicode Name Found!";
					}

					aRow.Cells[cnColumnIndexUName].Value = strOutput;
					aRow.Cells[cnColumnIndexUName].ToolTipText = strTooltipText;

					aRow.Cells[cnColumnIndexChar].Value = String.Format("{0}", ch);
					aRow.Cells[cnColumnIndexChar].ToolTipText = strTooltipText;

					aRow.Cells[cnColumnIndexUValue].Value = String.Format("U+{0:X4}", (int)ch);
					aRow.Cells[cnColumnIndexUValue].ToolTipText = strTooltipText;
				}
			}

			dataGridViewCodePointValues.Tag = str;
		}

		private bool IsShowingLegacy
		{
			get
			{
				if (m_bShowingLhs)
					return m_bLhsLegacy;
				else
					return m_bRhsLegacy;
			}
		}

		private bool m_bShowingLhs = true;
		// private bool m_bSuspendEvents = true;

		public void ShowSide(bool bShowLhs, Font font)
		{
			m_bShowingLhs = bShowLhs;
			// m_bSuspendEvents = true;

			// first turn off the checked state
			/*
			this.checkBoxUnicodeNames.CheckState = CheckState.Unchecked;
			this.checkBoxChars.CheckState = CheckState.Unchecked;
			this.checkBoxUnicodeValues.CheckState = CheckState.Unchecked;
			this.checkBoxHex.CheckState = CheckState.Unchecked;
			this.checkBoxDecimalValues.CheckState = CheckState.Unchecked;

			// quoted chars doesn't work for non-Unicode
			if (IsShowingLegacy)
			{
				this.checkBoxChars.Checked = false;
				this.checkBoxChars.Enabled = false;
			}
			else
				this.checkBoxChars.Enabled = true;

			if (m_bShowingLhs)
			{
				if ((m_lhsDisplayBits & DisplayBits.UnicodeNames) == DisplayBits.UnicodeNames)
					this.checkBoxUnicodeNames.CheckState = CheckState.Checked;
				if ((m_lhsDisplayBits & DisplayBits.Chars) == DisplayBits.Chars)
					this.checkBoxChars.CheckState = CheckState.Checked;
				if ((m_lhsDisplayBits & DisplayBits.UnicodeValues) == DisplayBits.UnicodeValues)
					this.checkBoxUnicodeValues.CheckState = CheckState.Checked;
				if ((m_lhsDisplayBits & DisplayBits.HexValues) == DisplayBits.HexValues)
					this.checkBoxHex.CheckState = CheckState.Checked;
				if ((m_lhsDisplayBits & DisplayBits.DecValues) == DisplayBits.DecValues)
					this.checkBoxDecimalValues.CheckState = CheckState.Checked;
			}
			else
			{
				if ((m_rhsDisplayBits & DisplayBits.UnicodeNames) == DisplayBits.UnicodeNames)
					this.checkBoxUnicodeNames.CheckState = CheckState.Checked;
				if ((m_rhsDisplayBits & DisplayBits.Chars) == DisplayBits.Chars)
					this.checkBoxChars.CheckState = CheckState.Checked;
				if ((m_rhsDisplayBits & DisplayBits.UnicodeValues) == DisplayBits.UnicodeValues)
					this.checkBoxUnicodeValues.CheckState = CheckState.Checked;
				if ((m_rhsDisplayBits & DisplayBits.HexValues) == DisplayBits.HexValues)
					this.checkBoxHex.CheckState = CheckState.Checked;
				if ((m_rhsDisplayBits & DisplayBits.DecValues) == DisplayBits.DecValues)
					this.checkBoxDecimalValues.CheckState = CheckState.Checked;
			}
			*/
			dataGridViewCodePointValues.Columns[cnColumnIndexChar].DefaultCellStyle.Font = font;

			// m_bSuspendEvents = false;
		}

		private void dataGridViewCodePointValues_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if ((e.RowIndex >= 0) && (e.ColumnIndex >= 0))
			{
				string str = (string)dataGridViewCodePointValues.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
				if (str != null)
					Program.AddStringToEditor(str);
			}
		}

		private void textBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			if (e.Control)
			{
				if (e.KeyCode == Keys.C)
					tb.Copy();
				else if (e.KeyCode == Keys.X)
					tb.Cut();
			}
		}

	}
}