#define TurnOffSpellFixer30

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization;                 // for SerializationException
using System.Runtime.Serialization.Formatters.Soap; // for soap formatter
using System.Diagnostics;
using System.Xml;                                   // for XmlException

using ECInterfaces;
using SilEncConverters31;

#if !TurnOffSpellFixer30
using SpellingFixer30;
#endif

namespace SFMConv
{
	public partial class SCConvForm : Form
	{
		internal const string cstrCaption = "SFM File Converter";
		protected const string cstrClickMsg = "Click here to define a converter";
		protected const string cstrOutputFileAddn = " (SFMConvert'd)";
		protected const string cstrDots = "...";
		protected const string cstrCscClickMsg = "Click to check spelling";
		protected const string cstrCscFieldClickMsg = "Field to check";
		protected int cnMaxConverterName = 30;
		protected const int nMaxRecentFiles = 15;

		protected Dictionary<string, string> m_mapSfmData = new Dictionary<string, string>();
		protected Hashtable m_mapEncConverters = new Hashtable();
		protected Dictionary<string, Font> mapName2FontExampleData = new Dictionary<string, Font>();
		protected Dictionary<string, Font> mapName2FontExampleResult = new Dictionary<string, Font>();

		protected Dictionary<string, string[]> m_mapFile2Contents = new Dictionary<string, string[]>();
		protected Dictionary<string, int> m_mapSfmFileLastDataSmpl = new Dictionary<string, int>();
		protected Dictionary<string, int> m_mapSfmIndexLastDataSmpl = new Dictionary<string, int>();
		protected List<string> m_lstFilesOpen = new List<string>();
		protected Encoding m_encOpen = Encoding.UTF8;

		protected DateTime m_dtStarted = DateTime.Now;
		TimeSpan m_timeMinStartup = new TimeSpan(0, 0, 1);

		protected DirectableEncConverter m_aECLast = null;
		protected Font m_aFontDefaultResult = null;
		protected Font m_aFontDefaultData = null;

		const int cnSfmMarkerColumn = 0;
		const int cnExampleDataColumn = 1;
		const int cnEncConverterColumn = 2;
		const int cnExampleOutputColumn = 3;

		bool m_bLastFontSetWasDataColumn = false;

		protected void CheckForFontHelps(string strSfm, DirectableEncConverter aEC, DataGridViewRow theRow)
		{
			if (!mapName2FontExampleData.ContainsKey(strSfm) && !mapName2FontExampleResult.ContainsKey(strSfm))
			{
				EncConverters aECs = GetEncConverters;
				if (aECs != null)
				{
					string strLhsName, strRhsName;
					if (aECs.GetFontMappingFromMapping(aEC.Name, out strLhsName, out strRhsName))
					{
						bool bDirForward = aEC.GetEncConverter.DirectionForward;
						Font fontData = CreateFontSafe((bDirForward) ? strLhsName : strRhsName);
						AddDataFont(strSfm, fontData);
						AdjustRowHeight(theRow, theRow.Cells[cnExampleDataColumn], fontData);

						Font fontTarget = CreateFontSafe((bDirForward) ? strRhsName : strLhsName);
						AddTargetFont(strSfm, fontTarget);
						AdjustRowHeight(theRow, theRow.Cells[cnExampleOutputColumn], fontTarget);
					}
				}
			}
		}

		// the creation of a Font can throw an exception if, for example, you try to construct one with
		//  the default style 'Regular' when the font itself doesn't have a Regular style. So this method
		//  can be called to create one and it'll try different styles if it fails.
		protected int cnDefaultFontSize = 14;
		protected Font CreateFontSafe(string strFontName)
		{
			Font font = null;
			try
			{
				font = new Font(strFontName, cnDefaultFontSize);
			}
			catch (Exception ex)
			{
				if (ex.Message.IndexOf("' does not support style '") != -1)
				{
					try
					{
						font = new Font(strFontName, cnDefaultFontSize, FontStyle.Bold);
					}
					catch
					{
						if (ex.Message.IndexOf("' does not support style '") != -1)
						{
							try
							{
								font = new Font(strFontName, cnDefaultFontSize, FontStyle.Italic);
							}
							catch { }
						}
					}
				}
			}
			finally
			{
				if (font == null)
					font = dataGridView.Font;
			}

			return font;
		}

		public SCConvForm()
		{
			InitializeComponent();
			m_aFontDefaultResult = dataGridView.Columns[cnExampleOutputColumn].DefaultCellStyle.Font;
			m_aFontDefaultData = dataGridView.Columns[cnExampleDataColumn].DefaultCellStyle.Font;
			this.helpProvider.SetHelpString(this.dataGridView, Properties.Resources.dataGridViewHelp);
		}

		protected void Reset()
		{
			m_lstFilesOpen.Clear();
			m_mapSfmData.Clear();
			m_mapFile2Contents.Clear();
			m_mapSfmFileLastDataSmpl.Clear();
			m_mapSfmIndexLastDataSmpl.Clear();
			dataGridView.Rows.Clear();
		}

		private void openSFMDocuments(Encoding enc)
		{
			DialogResult res = this.openSFMFileDialog.ShowDialog();
			if (res == DialogResult.OK)
			{
				OpenDocuments(openSFMFileDialog.FileNames, enc);
			}
		}

		protected void OpenDocuments(string [] astrFilenames, Encoding enc)
		{
			// in case it was already open and the user clicks the "Open SFM document" again.
			Reset();
			GetSfmList(astrFilenames, enc);
			PopulateGrid();
			AddFilenameToTitle(astrFilenames, enc);
		}

		public void AddFilenameToTitle(string[] astrFilenames, Encoding enc)
		{
			System.Diagnostics.Debug.Assert(astrFilenames.Length > 0);
			string strTitleName = "<various>";  // assume multiple files

			// if there's only one, then add it to the recently used files
			if (astrFilenames.Length == 1)
			{
				// add this filename to the list of recently used files
				string strFilename = astrFilenames[0];
				strTitleName = Path.GetFileName(strFilename);

				// add this filename to the list of recently used files
				if (Properties.Settings.Default.RecentFiles.Contains(strFilename))
				{
					int nIndex = Properties.Settings.Default.RecentFiles.IndexOf(strFilename);
					Properties.Settings.Default.RecentFiles.RemoveAt(nIndex);
					Properties.Settings.Default.RecentFilesCodePages.RemoveAt(nIndex);
				}
				else if (Properties.Settings.Default.RecentFiles.Count > nMaxRecentFiles)
				{
					Properties.Settings.Default.RecentFiles.RemoveAt(nMaxRecentFiles);
					Properties.Settings.Default.RecentFilesCodePages.RemoveAt(nMaxRecentFiles);
				}

				Properties.Settings.Default.RecentFiles.Insert(0, strFilename);
				Properties.Settings.Default.RecentFilesCodePages.Insert(0, enc.CodePage.ToString());
				Properties.Settings.Default.Save();
			}

			this.Text = String.Format("{0} -- {1}", cstrCaption, strTitleName);
		}

		private void PopulateGrid()
		{
			if (m_mapSfmData.Count > 0)
			{
				foreach (string strSfmField in m_mapSfmData.Keys)
				{
					if (!IsConverterDefined(strSfmField) && hideUnmappedFieldsToolStripMenuItem.Checked)
						continue;

					string strInput = m_mapSfmData[strSfmField];
					string strConverterName = (m_mapEncConverters.Count > 0) ? cstrDots :
						(selectProjectToolStripMenuItem.Checked) ? cstrCscClickMsg : cstrClickMsg;
					string strOutput = strInput;
					string strTooltip = null;
					if (IsConverterDefined(strSfmField))
					{
						DirectableEncConverter aEC = GetConverter(strSfmField);
						strConverterName = aEC.Name;
						strOutput = CallSafeConvert(aEC, strInput, false);
						strTooltip = aEC.ToString();
					}

					string[] row = { strSfmField, strInput, strConverterName, strOutput };
					int nIndex = this.dataGridView.Rows.Add(row);
					DataGridViewRow theRow = dataGridView.Rows[nIndex];
					theRow.Cells[cnEncConverterColumn].ToolTipText = strTooltip;

					if (mapName2FontExampleData.ContainsKey(strSfmField))
						theRow.Cells[cnExampleDataColumn].Style.Font = mapName2FontExampleData[strSfmField];
					if (mapName2FontExampleResult.ContainsKey(strSfmField))
						theRow.Cells[cnExampleOutputColumn].Style.Font = mapName2FontExampleResult[strSfmField];
					theRow.Height = RowMaxHeight;
				}

				// set it up so we 'see' the click
				m_dtStarted = DateTime.Now;
			}
		}

		protected string CallSafeConvert(DirectableEncConverter aEC, string strInput, bool bAllowAbort)
		{
			try
			{
				if (!String.IsNullOrEmpty(strInput))
				{
					// if the input side is legacy and the code page of the converter is not the same as the
					//  code page we opened the file with, then we'll be passing the wrong values...
					if (aEC.IsLhsLegacy)
					{
						IEncConverter aIEC = aEC.GetEncConverter;   // IsLhsLegacy will throw if GetEncConverter returns null
						if (aIEC.DirectionForward && (aIEC.CodePageInput != 0) && (aIEC.CodePageInput != m_encOpen.CodePage))
						{
							// we opened the SFM files with Encoding 0 == CP_ACP (or the default code page for this computer)
							//  but if the CodePageInput used by EncConverters is a different code page, then this will fail.
							//  If so, then convert it to a byte array and pass that
							byte[] abyInput = m_encOpen.GetBytes(strInput);
							strInput = ECNormalizeData.ByteArrToString(abyInput);
							aEC.GetEncConverter.EncodingIn = EncodingForm.LegacyBytes;
						}
					}

					string strOutput = aEC.Convert(strInput);
					aEC.GetEncConverter.EncodingIn = EncodingForm.Unspecified;  // in case the user switches directions

					// similarly, if the output is legacy, then if the code page used was not the same as the
					//  default code page, then we have to convert it so it'll produce the correct answer
					//  (this probably doesn't work for Legacy<>Legacy code pages)
					if (aEC.IsRhsLegacy)
					{
						IEncConverter aIEC = aEC.GetEncConverter;
						if (    (!aIEC.DirectionForward && (aIEC.CodePageInput != 0) && (Encoding.Default.CodePage != aIEC.CodePageInput))
							||  (aIEC.DirectionForward && (aIEC.CodePageOutput != 0) && (Encoding.Default.CodePage != aIEC.CodePageOutput)))
						{
							int nCP = (!aIEC.DirectionForward) ? aIEC.CodePageInput : aIEC.CodePageOutput;
							byte[] abyOutput = EncConverters.GetBytesFromEncoding(nCP, strOutput, true);
							strOutput = new string(Encoding.Default.GetChars(abyOutput));
						}
					}

					return strOutput;
				}
			}
			catch (Exception ex)
			{
				DialogResult res = MessageBox.Show(String.Format("Conversion failed! Reason: {0}", ex.Message),
					cstrCaption, (bAllowAbort) ? MessageBoxButtons.AbortRetryIgnore : MessageBoxButtons.OK);

				if (res == DialogResult.Abort)
					throw;
			}

			return strInput;
		}

		protected string CalcDataSampleToolTip(string strFilename, int nLineNum, string strData)
		{
			return String.Format("File: '{0}', Line: '{1}': {2}", Path.GetFileName(strFilename), nLineNum, strData);
		}

		private string FindNextDataSample(string strSfmField, string strCurrentData, out string strTooltip)
		{
			// go through all the files and see if you can find the next instance of the same SFM marker
			while(true)
			{
				for (int nIndexFile = m_mapSfmFileLastDataSmpl[strSfmField]; nIndexFile < m_lstFilesOpen.Count; nIndexFile++)
				{
					string strFilename = m_lstFilesOpen[nIndexFile];
					string[] aStrFileContents = m_mapFile2Contents[strFilename];
					for (int i = m_mapSfmIndexLastDataSmpl[strSfmField] + 1; i < aStrFileContents.Length; i++)
					{
						string strLine = aStrFileContents[i];
						if ((strLine.Length > 0) && (strLine[0] == '\\'))
						{
							int nIndex = strLine.IndexOf(' ');
							if (nIndex != -1)
							{
								string strSfm = strLine.Substring(0, nIndex);
								if (strSfm == strSfmField)
								{
									m_mapSfmFileLastDataSmpl[strSfmField] = nIndexFile;
									m_mapSfmIndexLastDataSmpl[strSfmField] = i;

									// prefix the tooltip text with the file and line number
									strTooltip = CalcDataSampleToolTip(strFilename, i, strCurrentData);
									return strLine.Substring(nIndex + 1);
								}
							}
						}
					}

					// try the next file... the last 'for' loop will automatically look at the
					// next line, so make it -1 so it starts at zero
					m_mapSfmIndexLastDataSmpl[strSfmField] = -1;
				}

				// start over
				m_mapSfmFileLastDataSmpl[strSfmField] = 0;
			};
		}

		// the GetDirectoryName returns a final slash, but only for files in the root folder
		//  so make sure we get exactly one.
		private string GetDirEnsureFinalSlash(string strFilename)
		{
			string strFolder = Path.GetDirectoryName(strFilename);
			if (strFolder[strFolder.Length - 1] != '\\')
				strFolder += '\\';
			return strFolder;
		}

		protected void MakeBackup(string strFileSpec)
		{
			int nIndex = strFileSpec.LastIndexOf('.');
			string strBackup = strFileSpec;
			if (nIndex != -1)
				strBackup = strFileSpec.Substring(0, nIndex) + ".bak";

			try
			{
				File.Copy(strFileSpec, strBackup, true);
			}
			catch { }
		}

		private void processAndSaveDocuments(Encoding enc)
		{
			TryProcessAndSaveDocumentsEx(enc, SaveOptions.QueryAddAffixes);

			// now that we've converted the file, our internal data no longer reflects the state of the
			//  original documents, so rescan them (seems the most obvious user-interface behavior)
			// But note, that if the user saves the file with the same name, then the original opening
			//  encoding will (likely) be different (so we check and if the same name is applied, then
			//  we reset m_encOpen to the target encoding).
			Reset();
			GetSfmList(openSFMFileDialog.FileNames, m_encOpen);
			PopulateGrid();
		}

		protected bool DoErrorChecking
		{
			get
			{
				return doErrorCheckingToolStripMenuItem.Checked;
			}
		}

		protected bool SingleStep
		{
			get
			{
				return (singlestepConversionToolStripMenuItem.Checked || toolStripButtonSingleStep.Checked);
			}
			set
			{
				singlestepConversionToolStripMenuItem.Checked = toolStripButtonSingleStep.Checked = value;
			}
		}

		protected bool ConvertDoc(string[] aStrFileContents, string strCurrentDocument)
		{
			BaseConverterForm dlg = null;
			if (SingleStep)
				dlg = new BaseConverterForm();

			FormButtons res = FormButtons.Replace;  // default behavior
			bool bModified = false;
			for (int i = 0; i < aStrFileContents.Length; i++)
			{
				progressBarSpellingCheck.PerformStep();
				string strLine = aStrFileContents[i];
				if ((strLine.Length > 0) && (strLine[0] == '\\'))
				{
					int nIndex = strLine.IndexOf(' ');
					if (nIndex == -1)
						continue;

					string strSfm = strLine.Substring(0, nIndex);
					if (IsConverterDefined(strSfm))
					{
						DirectableEncConverter aEC = GetConverter(strSfm);
						string strData = strLine.Substring(nIndex + 1);

						string strOutput = CallSafeConvert(aEC, strData, true);

						// if this string gets converted as a bunch of "?"s, it's probably an error. Show it to the
						//  user as a potential problem (unless we're already in single-step mode).
						bool bShowPotentialError = false;
						if (!SingleStep && DoErrorChecking)
						{
							// this is all sort of questionable, but...
							const double cfMinErrorDetectPercentage = 0.8;
							// ... e.g. no reason to think that the "?" character in the input string is a "?"...
							// e.g. if that was a legacy encoding, the character at the "?" code point could be anything...
							int nQMsInInput = strData.Length - strData.Replace("?", "").Length;
							string strWithoutErrors = strOutput.Replace("?", "");
							if (strWithoutErrors.Length < ((strOutput.Length - nQMsInInput) * cfMinErrorDetectPercentage))
							{
								bShowPotentialError = true;
								if (dlg == null)
									dlg = new BaseConverterForm();
							}
						}

					DoItAgain:
						// show user this one
						if (    (   (res != FormButtons.ReplaceAll)
									&& SingleStep
									&& (!dlg.SkipIdenticalValues || (strData != strOutput))
								)
							||  bShowPotentialError)
						{
							Font fontRhs = null;
							if (mapName2FontExampleResult.ContainsKey(strSfm))
								fontRhs = mapName2FontExampleResult[strSfm];
							Font fontLhs = null;
							if (mapName2FontExampleData.ContainsKey(strSfm))
								fontLhs = mapName2FontExampleData[strSfm];

							res = dlg.Show(strData, strOutput, aEC, fontLhs, fontRhs, strSfm, strCurrentDocument, bShowPotentialError);
							strOutput = dlg.ForwardString;  // just in case the user re-typed it
						}

						if ((res == FormButtons.Replace) || (res == FormButtons.ReplaceAll))
						{
							aStrFileContents[i] = strSfm + ' ' + strOutput;
							bModified = true;
						}
						else if (res == FormButtons.Cancel)
						{
							DialogResult dres = MessageBox.Show("If you have converted some of the document already, then cancelling now will leave your document in an unuseable state (unless you are doing 'spell fixing' or something which doesn't change the encoding). Click 'Yes' to confirm the cancellation or 'No' to continue with the conversion.", cstrCaption, MessageBoxButtons.YesNo);
							if (dres == DialogResult.Yes)
								throw new ApplicationException("User cancelled");
							goto DoItAgain;
						}
						else
						{
							System.Diagnostics.Debug.Assert(res == FormButtons.Next);
							res = FormButtons.Replace;  // reset for next time.
						}
					}
				}
			}
			return bModified;
		}

		protected enum SaveOptions
		{
			QueryAddAffixes,
			QuerySameName,
			NoQuerySameName
		}

		private void UpdateFilenameAffixes(string strFileSpec, ref string strExtn, ref string strFilenamePath, ref string strFileTitle)
		{
			if (strExtn == null)
				strExtn = Path.GetExtension(strFileSpec);

			if (strFilenamePath == null)
				strFilenamePath = GetDirEnsureFinalSlash(strFileSpec);

			strFileTitle = Path.GetFileNameWithoutExtension(strFileSpec);
		}

		private void TryProcessAndSaveDocumentsEx(Encoding enc, SaveOptions eSaveOption)
		{
			try
			{
				SetProgressMaximum();
				processAndSaveDocumentsEx(enc, eSaveOption);
			}
			catch { }
			finally
			{
				progressBarSpellingCheck.Value = 0;
			}
		}

		protected void SetProgressMaximum()
		{
			int nLineCount = 0;
			foreach (string strFileSpec in m_lstFilesOpen)
			{
				string[] aStrFileContents = m_mapFile2Contents[strFileSpec];
				nLineCount += aStrFileContents.Length;
			}
			progressBarSpellingCheck.Maximum = nLineCount;
		}

		private void processAndSaveDocumentsEx(Encoding enc, SaveOptions eSaveOption)
		{
			string strFilenamePath = null, strFileTitle = null, strFilenamePrefix = "", strFilenameSuffix = cstrOutputFileAddn, strExtn = null;  // for starters
			foreach (string strFileSpec in m_lstFilesOpen)
			{
				string[] aStrFileContents = m_mapFile2Contents[strFileSpec];
				if (!ConvertDoc(aStrFileContents, strFileSpec))
					continue;

				if ((eSaveOption == SaveOptions.QuerySameName) || (eSaveOption == SaveOptions.NoQuerySameName))
					saveFileDialog.FileName = strFileSpec;
				else if (eSaveOption == SaveOptions.QueryAddAffixes)
				{
					UpdateFilenameAffixes(strFileSpec, ref strExtn, ref strFilenamePath, ref strFileTitle);

#if !DontAllowSameNameSaves
					this.saveFileDialog.FileName = strFilenamePath + strFilenamePrefix + strFileTitle + strFilenameSuffix + strExtn;
				}

				DialogResult res = DialogResult.OK;
				if (eSaveOption != SaveOptions.NoQuerySameName)
					res = this.saveFileDialog.ShowDialog();

				string strFilename = strFileSpec.ToLower();
				string strSaveName = saveFileDialog.FileName.ToLower();
				if (res == DialogResult.Cancel)
					return;
				else if ((res == DialogResult.OK) && (strSaveName == strFilename))
				{
					// if in fact, the user is saving the file with the same name, then update the encoding that
					//  we think we used to process the files, so that when we reload the files after finishing
					//  the conversion, they will be read in correctly.
					MakeBackup(saveFileDialog.FileName);
					m_encOpen = enc;
				}
				else if (eSaveOption == SaveOptions.QuerySameName)
				{
					// if the user is expected to use the same name (e.g. SpellFixer), but *doesn't*, then go back
					//  to the affix mode
					UpdateFilenameAffixes(strFileSpec, ref strExtn, ref strFilenamePath, ref strFileTitle);
					eSaveOption = SaveOptions.QueryAddAffixes;
				}
#else
				string strSaveName = null;
				do
				{
					string strOutputFilenameOrig = strFilenamePath + strFilenamePrefix + strFileTitle + strFilenameSuffix + strExtOrig;

					this.saveFileDialog.FileName = strOutputFilenameOrig;

					DialogResult res = this.saveFileDialog.ShowDialog();
					strSaveName = saveFileDialog.FileName.ToLower();
					if (res == DialogResult.Cancel)
						return;
					else if ((res == DialogResult.OK) && (strSaveName == strFilename))
						MessageBox.Show("Sorry, you cannot save this file with the same name", cstrCaption);

				} while (strSaveName == strFilename);
#endif

				string strOutputFilenameNew = saveFileDialog.FileName;
				File.WriteAllLines(strOutputFilenameNew, aStrFileContents, enc);

				if (eSaveOption == SaveOptions.QueryAddAffixes)
				{
					// calculate (if possible) the suffix the user used so we can use that next time.
					strExtn = Path.GetExtension(strOutputFilenameNew);
					strFilenamePath = GetDirEnsureFinalSlash(strOutputFilenameNew);
					string strNewTitle = Path.GetFileNameWithoutExtension(strOutputFilenameNew);

					// see if the original name is some portion of the new name
					int nIndexOfOrigName = strNewTitle.IndexOf(strFileTitle, StringComparison.InvariantCultureIgnoreCase);

					// if we found the original name in the new name...
					if (nIndexOfOrigName != -1)
					{
						// first check for a prefix (which must be beyond the path)
						if (nIndexOfOrigName > 0)
							strFilenamePrefix = strNewTitle.Substring(0, nIndexOfOrigName);

						// also check for a suffix
						int nSuffixStart = (nIndexOfOrigName + strFileTitle.Length);
						if (strNewTitle.Length > nSuffixStart)
						{
							Debug.Assert(nSuffixStart >= 0);
							strFilenameSuffix = strNewTitle.Substring(nSuffixStart);
						}
						else
							strFilenameSuffix = null;
					}
				}
			}
		}

		protected void AdjustRowHeight(DataGridViewRow row, DataGridViewCell theCell, Font font)
		{
			theCell.Style.Font = font;
			int nRowHeight = font.Height;
			row.Height = Math.Max(row.Height, nRowHeight + 3);
		}

		protected void AddTargetFont(string strSfm, Font font)
		{
			if (mapName2FontExampleResult.ContainsKey(strSfm))
				mapName2FontExampleResult.Remove(strSfm);
			mapName2FontExampleResult.Add(strSfm, font);
		}

		protected void ResetTargetFont(string strSfm, DataGridViewCell theCell)
		{
			theCell.Style.Font = m_aFontDefaultResult;
			if (mapName2FontExampleResult.ContainsKey(strSfm))
				mapName2FontExampleResult.Remove(strSfm);
		}

		protected void AddDataFont(string strSfm, Font font)
		{
			if (mapName2FontExampleData.ContainsKey(strSfm))
				mapName2FontExampleData.Remove(strSfm);
			mapName2FontExampleData.Add(strSfm, font);
		}

		protected void ResetDataFont(string strSfm, DataGridViewCell theCell)
		{
			theCell.Style.Font = m_aFontDefaultData;
			if (mapName2FontExampleData.ContainsKey(strSfm))
				mapName2FontExampleData.Remove(strSfm);
		}

		private void dataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			// prevent the false click that occurs when the user chooses a menu item
			if ((DateTime.Now - m_dtStarted) < m_timeMinStartup)
				return;

			// if the user clicks on the header... that doesn't work
			if(     ((e.RowIndex < 0) || (e.RowIndex > dataGridView.Rows.Count))
				|| ((e.ColumnIndex < cnExampleDataColumn) || e.ColumnIndex > cnExampleOutputColumn))
				return;

			DataGridViewRow row = this.dataGridView.Rows[e.RowIndex];
			DataGridViewCell theCell = row.Cells[e.ColumnIndex];
			string strSfm = (string)row.Cells[cnSfmMarkerColumn].Value;
			switch (e.ColumnIndex)
			{
				case cnExampleOutputColumn:
					if (e.Button == MouseButtons.Right)
					{
						// if we didn't just set a font in the data column (in which case, we want
						//  the last font chosen to be the default), then get the font currently
						//  configured for the cell.
						if (!m_bLastFontSetWasDataColumn)
						{
							fontDialog.Font = theCell.Style.Font;
							m_bLastFontSetWasDataColumn = true;
						}

						if (fontDialog.ShowDialog() == DialogResult.OK)
						{
							AddTargetFont(strSfm, fontDialog.Font);
							AdjustRowHeight(row, theCell, fontDialog.Font);
						}
						else
						{
							ResetTargetFont(strSfm, theCell);
						}
					}
					/*  remove this or it'll be different behavior between the ExampleData and ExampleResults columns
					else if (e.Button == MouseButtons.Right)
					{
						if (m_aFontLast != null)
						{
							AddTargetFont(strSfm, m_aFontLast);
							AdjustRowHeight(row, m_aFontLast);
						}
						else
						{
							ResetTargetFont(strSfm, theCell);
						}
					}
					*/
					break;

				case cnExampleDataColumn: // clicked on example data column
					{
						if (e.Button == MouseButtons.Left)
						{
							string strTooltip;
							string strInput = FindNextDataSample(strSfm, (string)row.Cells[cnExampleDataColumn].Value, out strTooltip);
							row.Cells[cnExampleDataColumn].Value = strInput;
							string strOutput = strInput;
							if (IsConverterDefined(strSfm))
							{
								DirectableEncConverter aEC = GetConverter(strSfm);
								strOutput = CallSafeConvert(aEC, strInput, false);
							}
							row.Cells[cnExampleOutputColumn].Value = strOutput;

							// finally, adjust the tooltip as well to include the file and line numbers
							row.Cells[cnExampleDataColumn].ToolTipText = strTooltip;
						}
						else if (e.Button == MouseButtons.Right)
						{
							// if we just set a font in the data column, then get the font currently
							//  configured for the cell (because they don't likely want the *same* font as above)
							if (m_bLastFontSetWasDataColumn)
							{
								m_bLastFontSetWasDataColumn = false;
								fontDialog.Font = theCell.Style.Font;
							}

							// fontDialog.Font = theCell.Style.Font;
							if (fontDialog.ShowDialog() == DialogResult.OK)
							{
								AddDataFont(strSfm, fontDialog.Font);
								AdjustRowHeight(row, theCell, fontDialog.Font);
							}
							else
							{
								ResetDataFont(strSfm, theCell);
							}
						}
					}
					break;

				case cnEncConverterColumn: // clicked on the Converter column
					{
#if !TurnOffSpellFixer30
						if (selectProjectToolStripMenuItem.Checked)
						{
							System.Diagnostics.Debug.Assert(m_cscProject != null);
							if (cstrCscClickMsg == (string)theCell.Value)
							{
								IEncConverter aIEC = m_cscProject.SpellFixerEncConverter;
								if (aIEC != null)
								{
									DirectableEncConverter aEC = new DirectableEncConverter(aIEC);
									DefineConverter(strSfm, aEC);
									UpdateConverterCellValue(theCell, aEC);
								}
								else
								{
									DirectableEncConverter aEC = new DirectableEncConverter(m_cscProject.SpellFixerEncConverterName, true, NormalizeFlags.None);
									DefineConverter(strSfm, aEC);
									UpdateConverterCellValue(theCell, aEC);
									// theCell.Value = cstrCscFieldClickMsg;
								}
							}
							else
								theCell.Value = cstrCscClickMsg;

						}
						else
#endif
						{
							DirectableEncConverter aEC = null;

							// if the user right-clicked, then just repeat the last converter.
							//  (which now may be 'null' if cancelling an association)
							if (e.Button == MouseButtons.Right)
							{
								aEC = m_aECLast;
							}
							else
							{
								EncConverters aECs = GetEncConverters;
								if (aECs != null)
								{
									string strPreviewData = (string)row.Cells[cnExampleDataColumn].Value;
									IEncConverter aIEC = aECs.AutoSelectWithData(strPreviewData, null, ConvType.Unknown, "Choose Converter");
									if (aIEC != null)
										aEC = new DirectableEncConverter(aIEC);
								}
							}

							if (aEC != null)
							{
								DefineConverter(strSfm, aEC);
								UpdateConverterCellValue(row.Cells[cnEncConverterColumn], aEC);
								string strInput = (string)row.Cells[cnExampleDataColumn].Value;
								row.Cells[cnExampleOutputColumn].Value = CallSafeConvert(aEC, strInput, false);
								m_aECLast = aEC;
								CheckForFontHelps(strSfm, aEC, row);
							}
							else
							{
								// if the mapping was just removed (i.e. click + Cancel), then remove it
								//  from the list
								if (IsConverterDefined(strSfm))
									m_mapEncConverters.Remove(strSfm);

								UpdateConverterCellValue(row.Cells[cnEncConverterColumn], aEC);
								row.Cells[cnExampleOutputColumn].Value = row.Cells[cnExampleDataColumn].Value;
								m_aECLast = null;
								ResetDataFont(strSfm, row.Cells[cnExampleDataColumn]);
								ResetTargetFont(strSfm, row.Cells[cnExampleOutputColumn]);
							}

							if (m_mapEncConverters.Count > 0)
							{
								foreach (DataGridViewRow row2 in this.dataGridView.Rows)
								{
									DataGridViewCell cellConverter = row2.Cells[cnEncConverterColumn];
									if ((string)cellConverter.Value == cstrClickMsg)
									{
										cellConverter.Value = cstrDots;
										cellConverter.ToolTipText = null;
									}
								}
							}
						}
						break;
					}

				default:
					break;
			}
		}

		protected EncConverters GetEncConverters
		{
			get
			{
				try
				{
					return DirectableEncConverter.EncConverters;
				}
				catch (Exception ex)
				{
					MessageBox.Show(String.Format("Unable to access the repository because {0}", ex.Message), cstrCaption);
				}
				return null;
			}
		}

		public bool IsConverterDefined(string strSfmName)
		{
			return m_mapEncConverters.ContainsKey(strSfmName);
		}

		public void DefineConverter(string strSfmName, DirectableEncConverter aEC)
		{
			if (IsConverterDefined(strSfmName))
				m_mapEncConverters.Remove(strSfmName);
			m_mapEncConverters.Add(strSfmName, aEC);
		}

		public DirectableEncConverter GetConverter(string strSfmName)
		{
			return (DirectableEncConverter)m_mapEncConverters[strSfmName];
		}

		protected void GetSfmList(string[] aStrFilenames, Encoding enc)
		{
			m_encOpen = enc;    // in the spell fixer case, we use the same encoding as when opening.

			for (int nFileIndex = 0; nFileIndex < aStrFilenames.Length; nFileIndex++ )
			{
				string strFilename = aStrFilenames[nFileIndex];

				string[] aStrFileContents = null;
				try
				{
					aStrFileContents = File.ReadAllLines(strFilename, enc);
				}
				catch (Exception ex)
				{
					MessageBox.Show(String.Format("Unable to read '{1}'!{0}{0}Reason: {2}{0}{0}This file will be skipped.",
						Environment.NewLine, strFilename, ex.Message), cstrCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
					continue;
				}

				m_lstFilesOpen.Add(strFilename);
				CleanSfmFileContents(ref aStrFileContents);
				m_mapFile2Contents.Add(strFilename, aStrFileContents);

				for (int i = 0; i < aStrFileContents.Length; i++)
				{
					string strLine = aStrFileContents[i];
					if ((strLine.Length > 0) && (strLine[0] == '\\'))
					{
						int nIndex = strLine.IndexOf(' ');
						if (nIndex != -1)
						{
							string strSfm = strLine.Substring(0, nIndex);
							string strData = strLine.Substring(nIndex + 1);
							if (!m_mapSfmData.ContainsKey(strSfm) && (strData.Length > 0) )
							{
								m_mapSfmData.Add(strSfm, strData);

								// keep track of where this came from so we can handle a click on this
								//  column to jump to the next sample
								m_mapSfmFileLastDataSmpl[strSfm] = nFileIndex;
								m_mapSfmIndexLastDataSmpl[strSfm] = i;
							}
						}
					}
				}
			}
		}

		protected void CleanSfmFileContents(ref string[] aStrFileContents)
		{
			int i = 0, nLastSfmIndex = 0, nRemovedLines = 0;
			while (i < aStrFileContents.Length - 1)
			{
				if (aStrFileContents[i + 1].Length > 0)
				{
					char cFirstCharOfNextLine = aStrFileContents[i + 1][0];

					if ((cFirstCharOfNextLine != '\\')
					 && (cFirstCharOfNextLine != '\r'))
					{
						// this means this is a redundant line break so move it to the
						//  preceding line (but make sure there's only one space)
						string strLine = aStrFileContents[nLastSfmIndex];

						// if the last character is *not* a space, then put one in.
						if (strLine[strLine.Length - 1] != ' ')
							aStrFileContents[nLastSfmIndex] += ' ';

						// and add the next line.
						aStrFileContents[nLastSfmIndex] += aStrFileContents[i + 1];

						// null it out
						aStrFileContents[i + 1] = null;

						// keep track of new size
						nRemovedLines++;
					}
					else
						nLastSfmIndex = i + 1;
				}

				i++;
			}

			string[] aStrs = new string[aStrFileContents.Length - nRemovedLines];
			int j = 0;
			for (i = 0; i < aStrFileContents.Length; i++)
				if (aStrFileContents[i] != null)
					aStrs[j++] = aStrFileContents[i];

			aStrFileContents = aStrs;
		}

		private void legacyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.openSFMDocuments(Encoding.Default);
		}

		private void unicodeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.openSFMDocuments(Encoding.UTF8);
		}

		private void legacyToolStripMenuItemProcess_Click(object sender, EventArgs e)
		{
			this.processAndSaveDocuments(Encoding.Default);
		}

		private void toolStripMenuItemSaveAsUTF8_Click(object sender, EventArgs e)
		{
			this.processAndSaveDocuments(Encoding.UTF8);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ToolStripMenuItemFile_DropDownOpening(object sender, EventArgs e)
		{
			this.reloadToolStripMenuItem.Enabled = this.processAndSaveDocumentsToolStripMenuItem.Enabled =
				(m_lstFilesOpen.Count > 0);

			recentFilesToolStripMenuItem.DropDownItems.Clear();
			for (int i = 0; i < Properties.Settings.Default.RecentFiles.Count; i++)
			{
				string strRecentFile = Properties.Settings.Default.RecentFiles[i];
				int nCodePage = Convert.ToInt32(Properties.Settings.Default.RecentFilesCodePages[i]);
				ToolStripItem tsi = recentFilesToolStripMenuItem.DropDownItems.Add(strRecentFile, null, recentFilesToolStripMenuItem_Click);
				tsi.Tag = nCodePage;
			}
			recentFilesToolStripMenuItem.Enabled = (recentFilesToolStripMenuItem.DropDownItems.Count > 0);
		}

		private void converterMappingsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			bool bMappingsExist = (m_mapEncConverters.Count > 0);
			this.newToolStripMenuItem.Enabled = bMappingsExist;
			this.saveToolStripMenuItem.Enabled = bMappingsExist;
			bool bRowsExist = (dataGridView.Rows.Count > 0);
			this.setDefaultConverterToolStripMenuItem.Enabled = bRowsExist;
			this.setDefaultExampleDataFontToolStripMenuItem.Enabled = bRowsExist;

			// don't allow the unmapped rows to be hidden if there are none that are mapped.
			if (!bMappingsExist && hideUnmappedFieldsToolStripMenuItem.Checked)
			{
				this.hideUnmappedFieldsToolStripMenuItem.Checked = false;
				PopulateGrid();
			}

			recentToolStripMenuItem.DropDownItems.Clear();
			foreach (string strRecentFile in Properties.Settings.Default.ConverterMappingRecentFiles)
				recentToolStripMenuItem.DropDownItems.Add(strRecentFile, null, converterMapRecentFilesMenuItem_Click);
			recentToolStripMenuItem.Enabled = (recentToolStripMenuItem.DropDownItems.Count > 0);
		}

		protected void UpdateConverterCellValue(DataGridViewCell theCell, DirectableEncConverter aEC)
		{
			if (aEC == null)
			{
				theCell.Value = (selectProjectToolStripMenuItem.Checked) ? cstrCscClickMsg : cstrClickMsg;
				theCell.ToolTipText = null;
			}
			else
			{
				string strName = aEC.Name;
				if (strName.Length > cnMaxConverterName)
					strName = strName.Substring(0, cnMaxConverterName);
				theCell.Value = strName;
				theCell.ToolTipText = aEC.ToString();
			}
		}

		private void setDefaultConverterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Cursor = Cursors.WaitCursor;

			EncConverters aECs = GetEncConverters;
			if (aECs != null)
			{
				IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, "Choose Default Converter");
				if (aIEC != null)
				{
					DirectableEncConverter aEC = new DirectableEncConverter(aIEC.Name, aIEC.DirectionForward, aIEC.NormalizeOutput);
					foreach (DataGridViewRow aRow in dataGridView.Rows)
					{
						string strSfm = (string)aRow.Cells[cnSfmMarkerColumn].Value;
						if (!IsConverterDefined(strSfm))
						{
							DefineConverter(strSfm, aEC);    // add it
							UpdateConverterCellValue(aRow.Cells[cnEncConverterColumn], aEC);
							string strInput = (string)aRow.Cells[cnExampleDataColumn].Value;
							aRow.Cells[cnExampleOutputColumn].Value = CallSafeConvert(aEC, strInput, false);
						}
					}

					// clear the last one selected so that a right-click can be used to cancel the selection
					m_aECLast = null;
				}
			}

			Cursor = Cursors.Default;
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_mapEncConverters.Clear();
			mapName2FontExampleResult.Clear();
			mapName2FontExampleData.Clear();

			// clear out the EncConverters collection again (by faking a change to the repository file timestamp
			string strRepository = EncConverters.GetRepositoryFileName();
			if (File.Exists(strRepository))
			{
				FileInfo fi = new FileInfo(strRepository);
				fi.LastWriteTime = DateTime.Now;
			}

			if (m_lstFilesOpen.Count > 0)
			{
				hideUnmappedFieldsToolStripMenuItem.Checked = false;	// just in case we were hiding, we don't want to anymore
				dataGridView.Rows.Clear();
				PopulateGrid();
			}
		}

		private void loadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlgSettings = new OpenFileDialog();
			dlgSettings.DefaultExt = "scm";
			dlgSettings.RestoreDirectory = true;
			dlgSettings.InitialDirectory = Application.UserAppDataPath;
			dlgSettings.Filter = "SFM Converter mapping files (*.scm)|*.scm|All files|*.*";

			if (dlgSettings.ShowDialog() == DialogResult.OK)
			{
				LoadConverterMappingFile(dlgSettings.FileName);
			}
		}

		protected void LoadConverterMappingFile(string strFilename)
		{
			FileStream fs = new FileStream(strFilename, FileMode.Open);

			// Construct a SoapFormatter and use it
			// to serialize the data to the stream.
			try
			{
				SoapFormatter formatter = new SoapFormatter();
				formatter.Binder = new DirectableEncConverterDeserializationBinder();

				// serialize in the EncConverters
				m_mapEncConverters = (Hashtable)formatter.Deserialize(fs);

				// serialize in the result font mapping
				SerializeInFontMapping(fs, formatter, mapName2FontExampleResult);

				// serialize in the Data font mapping
				SerializeInFontMapping(fs, formatter, mapName2FontExampleData);

				// add this to the recently used list
				AddToConverterMappingRecentlyUsed(strFilename);
			}
			catch (XmlException)
			{
				// this happens if the user opens an old style file (that doesn't have the target font names)
				//  so just ignore it for now.
			}
			catch (SerializationException ex)
			{
				MessageBox.Show("Failed to open mapping file. Reason: " + ex.Message);
			}
			finally
			{
				fs.Close();
			}

			if (m_lstFilesOpen.Count > 0)
			{
				dataGridView.Rows.Clear();
				PopulateGrid();
			}
		}

		private void SerializeInFontMapping(FileStream fs, SoapFormatter formatter, Dictionary<string, Font> fontMapping)
		{
			Hashtable map = (Hashtable)formatter.Deserialize(fs);
			foreach (string strSfm in map.Keys)
			{
				if (fontMapping.ContainsKey(strSfm))
					fontMapping.Remove(strSfm);
				fontMapping.Add(strSfm, (Font)map[strSfm]);
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog dlgSettings = new SaveFileDialog();
			dlgSettings.DefaultExt = "scm";
			dlgSettings.FileName = "SFM Converter mapping1.scm";
			dlgSettings.InitialDirectory = Application.UserAppDataPath;
			dlgSettings.Filter = "SFM Converter mapping files (*.scm)|*.scm|All files|*.*";
			dlgSettings.RestoreDirectory = true;
			if (dlgSettings.ShowDialog() == DialogResult.OK)
			{
				// Construct a SoapFormatter and use it
				// to serialize the data to the stream.
				FileStream fs = new FileStream(dlgSettings.FileName, FileMode.Create);
				SoapFormatter formatter = new SoapFormatter();
				try
				{
					// write out the EncConverters used
					formatter.Serialize(fs, m_mapEncConverters);

					// write out the mapping of SFM fields to Result fonts
					SerializeOutFontMapping(fs, formatter, mapName2FontExampleResult);

					// write out the mapping of SFM fields to Data fonts
					SerializeOutFontMapping(fs, formatter, mapName2FontExampleData);

					AddToConverterMappingRecentlyUsed(dlgSettings.FileName);
				}
				catch (SerializationException ex)
				{
					MessageBox.Show("Failed to save! Reason: " + ex.Message);
				}
				finally
				{
					fs.Close();
				}
			}
		}

		// can't serialize generics, so put it in an intermediate Hashtable to serialize out
		private void SerializeOutFontMapping(FileStream fs, SoapFormatter formatter, Dictionary<string, Font> fontMapping)
		{
			Hashtable map = new Hashtable(fontMapping.Count);
			foreach (KeyValuePair<string, Font> kvp in fontMapping)
				map.Add(kvp.Key, kvp.Value);

			formatter.Serialize(fs, map);
		}

		private void hideUnmappedFieldsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (hideUnmappedFieldsToolStripMenuItem.Checked && (m_mapEncConverters.Count == 0))
			{
				MessageBox.Show("You probably don't want to hide mapped SFM fields when there are none mapped (or they all disappear).");
				hideUnmappedFieldsToolStripMenuItem.Checked = false;
			}
			else
			{
				dataGridView.Rows.Clear();
				PopulateGrid();
			}
		}

		void recentFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripDropDownItem aRecentFile = (ToolStripDropDownItem)sender;
			try
			{
				Encoding enc = Encoding.GetEncoding((int)aRecentFile.Tag);

				// put it in the open dialog's FileNames array because that's used to reload the file after
				//  conversion.
				openSFMFileDialog.FileName = aRecentFile.Text;
				OpenDocuments(openSFMFileDialog.FileNames, enc);
			}
			catch (Exception ex)
			{
				// probably means the file doesn't exist anymore, so remove it from the recent used list
				Properties.Settings.Default.RecentFiles.Remove(aRecentFile.Text);
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		void converterMapRecentFilesMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripDropDownItem aRecentFile = (ToolStripDropDownItem)sender;
			try
			{
				LoadConverterMappingFile(aRecentFile.Text);
			}
			catch (Exception ex)
			{
				// probably means the file doesn't exist anymore, so remove it from the recent used list
				Properties.Settings.Default.ConverterMappingRecentFiles.Remove(aRecentFile.Text);
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		public void AddToConverterMappingRecentlyUsed(string strFilename)
		{
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(strFilename));

			// add this filename to the list of recently used files
			if (Properties.Settings.Default.ConverterMappingRecentFiles.Contains(strFilename))
				Properties.Settings.Default.ConverterMappingRecentFiles.Remove(strFilename);
			else if (Properties.Settings.Default.ConverterMappingRecentFiles.Count > nMaxRecentFiles)
				Properties.Settings.Default.ConverterMappingRecentFiles.RemoveAt(nMaxRecentFiles);

			Properties.Settings.Default.ConverterMappingRecentFiles.Insert(0, strFilename);
			Properties.Settings.Default.Save();
		}

		public int RowMaxHeight = 28;    // start with this

		private void setDefaultExampleResultsFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DataGridViewColumn theColumn = dataGridView.Columns[cnExampleOutputColumn];
			fontDialog.Font = theColumn.DefaultCellStyle.Font;
			if (fontDialog.ShowDialog() == DialogResult.OK)
			{
				theColumn.DefaultCellStyle.Font = fontDialog.Font;
				RowMaxHeight = Math.Max(RowMaxHeight, fontDialog.Font.Height);
			}
		}

		private void setDefaultExampleDataFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DataGridViewColumn theColumn = dataGridView.Columns[cnExampleDataColumn];
			fontDialog.Font = theColumn.DefaultCellStyle.Font;
			if (fontDialog.ShowDialog() == DialogResult.OK)
			{
				theColumn.DefaultCellStyle.Font = fontDialog.Font;
				RowMaxHeight = Math.Max(RowMaxHeight, fontDialog.Font.Height);
			}
		}

		private void toolStripButtonSingleStep_CheckStateChanged(object sender, EventArgs e)
		{
			this.singlestepConversionToolStripMenuItem.Checked = ((ToolStripButton)sender).Checked;
		}

		private void singlestepConversionToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
		{
			this.toolStripButtonSingleStep.Checked = ((ToolStripMenuItem)sender).Checked;
		}

		private void toolStripButtonRefresh_Click(object sender, EventArgs e)
		{
			OpenDocuments(openSFMFileDialog.FileNames, m_encOpen);
		}

		private void dataGridView_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == ' ' || e.KeyChar == '\r')
			{
				if (dataGridView.SelectedCells.Count > 0)
				{
					int nRow = dataGridView.SelectedCells[0].RowIndex;
					dataGridView_CellMouseClick(sender, new DataGridViewCellMouseEventArgs(2, nRow, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));
				}
			}
		}

#if !TurnOffSpellFixer30
		protected bool TrySelectProject()
		{
			try
			{
				m_cscProject = CscProject.SelectProject();
			}
			catch (ApplicationException ex)
			{
				if (ex.Message == CscProject.cstrChooseProjectException)
					selectProjectToolStripMenuItem.Checked = false;
				else
					MessageBox.Show(ex.Message, cstrCaption);
				return false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
				return false;
			}
			return true;
		}

		protected CscProject m_cscProject = null;
		private void initializeCheckListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_cscProject == null)
				if (!TrySelectProject())
					return;

			CscAddToCheckList(m_cscProject);
			m_cscProject.QueryForSpellingCorrectionsBulk();
		}

		public char[] caSplitChars = new char[] { '\n', '\t', ' ' };

		protected void CscAddToCheckList(CscProject cscProject)
		{
			SetProgressMaximum();

			foreach (string strFileSpec in m_lstFilesOpen)
			{
				string[] aStrFileContents = m_mapFile2Contents[strFileSpec];
				for (int i = 0; i < aStrFileContents.Length; i++)
				{
					progressBarSpellingCheck.PerformStep();
					string strLine = aStrFileContents[i];
					if ((strLine.Length > 0) && (strLine[0] == '\\'))
					{
						int nIndex = strLine.IndexOf(' ');
						if (nIndex == -1)
							continue;

						string strSfm = strLine.Substring(0, nIndex);
						if (IsConverterDefined(strSfm))
						{
							string strData = strLine.Substring(nIndex + 1);
							string[] astrWords = strData.Split(caSplitChars, StringSplitOptions.RemoveEmptyEntries);
							int nNumWords = astrWords.Length;
							int nStartIndex = -1;
							int[] anStartIndices = new int[nNumWords];
							for (int j = 0; j < nNumWords; j++)
							{
								nStartIndex = strData.IndexOf(astrWords[j], ++nStartIndex);
								System.Diagnostics.Debug.Assert(nStartIndex != -1);
								anStartIndices[j] = nStartIndex;
							}

							const int cnNumContextWords = 3;
							for (int j = 0; j < nNumWords; j++)
							{
								if (j < cnNumContextWords)
									nStartIndex = 0;
								else
									nStartIndex = anStartIndices[j - cnNumContextWords];

								int nContextLength;
								if ((nNumWords - j) < cnNumContextWords)
									nContextLength = strData.Length - nStartIndex;
								else
								{
									int nArrIndex = j + cnNumContextWords - 1;
									nContextLength = anStartIndices[nArrIndex] + astrWords[nArrIndex].Length - nStartIndex;
								}

								string strContext = strData.Substring(nStartIndex, nContextLength);
								cscProject.AddWordToCheckList(astrWords[j], true, strContext);
							}
						}
					}
				}
			}
			progressBarSpellingCheck.Value = 0;
		}

		protected bool m_bBotherUser = true;

		private void selectProjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (selectProjectToolStripMenuItem.Checked)
			{
				if (m_cscProject != null)   // might want to select a different project
					m_cscProject = null;

				if (!TrySelectProject())
					return;

				foreach (DataGridViewRow aRow in dataGridView.Rows)
				{
					string strSFM = (string)aRow.Cells[cnSfmMarkerColumn].Value;
					DirectableEncConverter aEC = GetConverter(strSFM);
					UpdateConverterCellValue(aRow.Cells[cnEncConverterColumn], aEC);
				}

				if (m_bBotherUser)
					MessageBox.Show("Now click on the buttons in the 'Converter' column for the " + Environment.NewLine +
						"SFM fields that you want to check spelling in", cstrCaption);
				m_bBotherUser = false;

				selectProjectToolStripMenuItem.Text = "&Turn off Consistent Spell Check mode";
				selectProjectToolStripMenuItem.ToolTipText = String.Format("Click to unload the '{0}' Consistent Spell Fixer project or right-click to choose another", m_cscProject.Name);
			}
			else
			{
				dataGridView.Rows.Clear();
				PopulateGrid();
				selectProjectToolStripMenuItem.Text = "&Select Project";
				selectProjectToolStripMenuItem.ToolTipText = "Click to load a Consistent Spell Fixer project";
			}
		}

		private void selectProjectToolStripMenuItem_MouseUp(object sender, MouseEventArgs e)
		{
			if (selectProjectToolStripMenuItem.Checked && (e.Button == MouseButtons.Right))
				selectProjectToolStripMenuItem_Click(sender, e);
		}

		private void consistentSpellingCheckToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			selectProjectToolStripMenuItem.Enabled = (dataGridView.Rows.Count > 0);

			bool bEnableTheRest = (m_cscProject != null);
			initializeCheckListToolStripMenuItem.Enabled = bEnableTheRest;
			correctSpellingToolStripMenuItem.Enabled = bEnableTheRest;
			/*
			editSpellingFixesToolStripMenuItem.Enabled = bEnableTheRest;
			editDictionaryToolStripMenuItem.Enabled = bEnableTheRest;
			*/
		}

		private void correctSpellingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_cscProject == null)
				if (!TrySelectProject())
					return;

			SaveOptions eSaveOption = SaveOptions.QuerySameName;
			DialogResult res = MessageBox.Show("Do you want to save all modified files with a new name?", cstrCaption, MessageBoxButtons.YesNoCancel);
			if (res == DialogResult.Cancel)
				return;
			else if (res == DialogResult.No)
				eSaveOption = SaveOptions.NoQuerySameName;
			else
				System.Diagnostics.Debug.Assert(res == DialogResult.Yes);

			Cursor = Cursors.WaitCursor;
			TryProcessAndSaveDocumentsEx(m_encOpen, eSaveOption);
			Cursor = Cursors.Default;
		}
/*
		private void editSpellingFixesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_cscProject == null)
				if (!TrySelectProject())
					return;

			m_cscProject.EditSpellingFixes();
		}

		private void editDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_cscProject == null)
				if (!TrySelectProject())
					return;

			Cursor = Cursors.WaitCursor;
			m_cscProject.EditDictionary();
			Cursor = Cursors.Default;
		}
*/
#endif
	}
}