// #define DefineToTurnOffBackgroundWorker

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters31;
using Word = Microsoft.Office.Interop.Word;
using System.Collections;
using System.IO;                                    // FileStream
using System.Runtime.Serialization;                 // for SerializationException
using System.Runtime.Serialization.Formatters.Soap; // for soap formatter

namespace SILConvertersOffice
{
	internal partial class FontConvertersPicker : Form
	{
		private OfficeDocument m_doc = null;
		protected DirectableEncConverter m_aECForAll = null;
		protected DirectableEncConverter m_aECLast = null;
		protected Hashtable m_mapEncConverters = new Hashtable();
		protected const int nMaxRecentFiles = 15;

		const int nColumnFontNames = 0;
		const int nColumnConverterNames = 1;
		const int nColumnTargetFontNames = 2;

		// const string cstrDots = "...";
		protected const string cstrClickMsg = "Select a converter";
		protected const string cstrFontClickMsg = "Select a font to apply";
		protected const string cstrApplyECFormat = "Apply '{0}'?";
		protected string m_strApplyEC = null;

		/// <summary>
		/// FontConvertersPicker: to choose the font you want to process in the Word document
		///
		/// This version of the constructor is for when you want the EncConverter picked (by
		/// the user when the font is selected).
		/// </summary>
		/// <param name="doc"></param>
		public FontConvertersPicker(OfficeDocument doc)
		{
			CommonConstruct(doc);
		}

		protected void InsureSettings()
		{
			// make sure we have the collection around so we don't have to check it later
			if (Properties.Settings.Default.ConverterMappingRecentFiles == null)
			{
				Properties.Settings.Default.ConverterMappingRecentFiles = new System.Collections.Specialized.StringCollection();
				Properties.Settings.Default.Save();
			}
		}

		/// <summary>
		/// FontConvertersPicker: to choose the font you want to process in the Word document
		///
		/// This version of the constructor is for when you want the same (given) EncConverter
		/// used for all fonts.
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="aEC"></param>
		public FontConvertersPicker(OfficeDocument doc, IEncConverter aEC)
		{
			System.Diagnostics.Debug.Assert(aEC != null);
			m_aECForAll = new DirectableEncConverter(aEC);
			m_strApplyEC = String.Format(cstrApplyECFormat, aEC.Name);
			CommonConstruct(doc);

			// no mapping in this mode
			converterMappingToolStripMenuItem.Visible = false;
		}

		/// <summary>
		/// FontConvertersPicker: to choose the converter and target font for a single
		/// font.
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="strFontName"></param>
		public FontConvertersPicker(string strFontName)
		{
			InitializeComponent();
			InsureSettings();
			// no mapping in this mode
			this.checkBoxFontsInUse.Visible = false;

			AddRow(strFontName);
		}

		public bool UsingSameEncConverter
		{
			get { return (m_aECForAll != null); }
		}

		protected void CommonConstruct(OfficeDocument doc)
		{
			InitializeComponent();
			InsureSettings();
			m_doc = doc;

			PopulateGrid();
		}

		protected void PopulateGrid()
		{
			ResetBackgroundWorker();    // just in case

			dataGridViewFontsConverters.Rows.Clear();

			// check the check box state which says whether we do "all fonts" or just those in the document
			if (checkBoxFontsInUse.Checked)
			{
				progressBarFontsInUse.Visible = true;
				progressBarFontsInUse.Value = 0;

				// Office 2007 (what I'm calling build 4518) doesn't allow background workers to run while a
				//  modal dialog is visible. So do it inline.
				if (    ((m_doc.GetType() != typeof(PubDocument)) && (m_doc.GetType() != typeof(PubStoryDocument)))
					||  ((PubDocument)m_doc).Document.Application.Version.Substring(0,2) == "11")
					backgroundWorker.RunWorkerAsync(m_doc);
				else
				{
					DoWork(backgroundWorker, m_doc);
					progressBarFontsInUse.Visible = false;
				}
			}
			else
				foreach (FontFamily aFontFamily in new InstalledFontCollection().Families)
					AddRow(aFontFamily.Name);
		}

		protected bool IsTargetFontDefined(string strFontNameOutput)
		{
			return (strFontNameOutput == cstrFontClickMsg);
		}

		protected void InitConverterDetails(string strFontName, out string strConverterName, out string strTooltip, out string strFontNameOutput)
		{
			strConverterName = (UsingSameEncConverter) ? m_strApplyEC : cstrClickMsg;
			strTooltip = strConverterName;
			strFontNameOutput = (UsingSameEncConverter) ? strFontName : cstrFontClickMsg;

			// if there is no converted selected, then see if the repository has a suggestion
			if (!m_mapEncConverters.ContainsKey(strFontName) && (strConverterName == cstrClickMsg))
			{
				EncConverters aECs = OfficeApp.GetEncConverters;
				if (aECs != null)
				{
					string strMappingName = aECs.GetMappingNameFromFont(strFontName);
					if (!String.IsNullOrEmpty(strMappingName))
					{
						strConverterName = strMappingName;
						IEncConverter aIEC = aECs[strConverterName];

						if (aIEC != null)
						{
							DirectableEncConverter aEC = new DirectableEncConverter(aIEC);
							m_mapEncConverters.Add(strFontName, aEC);
						}
					}
				}
			}

			if (m_mapEncConverters.ContainsKey(strFontName))
			{
				DirectableEncConverter aEC = (DirectableEncConverter)m_mapEncConverters[strFontName];
				strConverterName = aEC.Name;
				strTooltip = aEC.ToString();
				if (aEC.TargetFont != null)
				{
					strFontNameOutput = aEC.TargetFont.Name;
				}
				else // otherwise, the repository might be able to tell us what font to use
				{
					EncConverters aECs = OfficeApp.GetEncConverters;
					if (aECs != null)
					{
						string[] astrFontnames = aECs.GetFontMapping(aEC.Name, strFontName);
						if (astrFontnames.Length > 0)
						{
							strFontNameOutput = astrFontnames[0];
							aEC.TargetFont = new Font(strFontNameOutput, 14);
						}
					}
				}
			}
		}

		protected void AddRow(string strFontName)
		{
			string strConverterName, strTooltip, strFontNameOutput;
			InitConverterDetails(strFontName, out strConverterName, out strTooltip, out strFontNameOutput);

			string[] row = { strFontName, strConverterName, strFontNameOutput };
			int nIndex = dataGridViewFontsConverters.Rows.Add(row);
			dataGridViewFontsConverters.Rows[nIndex].Cells[nColumnConverterNames].ToolTipText = strTooltip;
			dataGridViewFontsConverters.Rows[nIndex].Cells[nColumnTargetFontNames].Value = strFontNameOutput;
		}

		public FontConverters SelectedFontConverters
		{
			get
			{
				FontConverters aFCs = new FontConverters();
				foreach (string strFontName in m_mapEncConverters.Keys)
				{
					DirectableEncConverter aIEC = (DirectableEncConverter)m_mapEncConverters[strFontName];
					FontConverter aFC = new FontConverter(strFontName, aIEC);
					aFC.RhsFont = aIEC.TargetFont;
					aFCs.Add(strFontName, aFC);
				}
				return aFCs;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (m_mapEncConverters.Count > 0)
			{
				DialogResult = DialogResult.OK;
				this.Close();
			}
			else
				MessageBox.Show("You must choose at least one converter (or click Cancel)", Connect.cstrCaption);
		}

		private void checkBoxFontsInUse_CheckedChanged(object sender, EventArgs e)
		{
			PopulateGrid();
		}

		void FontConvertersPicker_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
		{
			ResetBackgroundWorker();
		}

		private void ResetBackgroundWorker()
		{
			if (this.backgroundWorker.IsBusy)
				this.backgroundWorker.CancelAsync();
		}

		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			DoWork(sender as BackgroundWorker, e.Argument as OfficeDocument);
		}

		protected void DoWork(BackgroundWorker worker, OfficeDocument doc)
		{
			int nWordCount = doc.WordCount;
			/*
			if (doc.GetType() == typeof(PubDocument))   // for publisher, we step when we've found a font
			{
				progressBarFontsInUse.Maximum = nWordCount;
				progressBarFontsInUse.Step = 1;
				nWordCount = 0;
			}
			*/
			FontNamesInUseProcessor aDocumentProcess = new FontNamesInUseProcessor(nWordCount, worker);

			try
			{
				doc.ProcessWordByWord(aDocumentProcess);
			}
			catch (Exception ex)
			{
				OfficeApp.DisplayException(ex);
			}
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			// for us the "progress" is a new font found (if not null)
			if (e.UserState != null)
			{
				string[] aStrs = (string[])e.UserState;
				System.Diagnostics.Debug.Assert(aStrs.Length == 2);
				string strFontName = aStrs[0];
				string strWord = aStrs[1];  // in case we want to
				AddRow(strFontName);
			}
			else
				progressBarFontsInUse.PerformStep();
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
				MessageBox.Show(e.Error.Message, Connect.cstrCaption);
			progressBarFontsInUse.Visible = false;
		}

		private void setDefaultConverterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EncConverters aECs = OfficeApp.GetEncConverters;
			if (aECs != null)
			{
				IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, "Select Default Converter");
				if (aIEC != null)
				{
					DirectableEncConverter aEC = new DirectableEncConverter(aIEC.Name, aIEC.DirectionForward, aIEC.NormalizeOutput);
					foreach (DataGridViewRow aRow in dataGridViewFontsConverters.Rows)
					{
						string strFontName = (string)aRow.Cells[nColumnFontNames].Value;
						if (!m_mapEncConverters.ContainsKey(strFontName))
						{
							m_mapEncConverters.Add(strFontName, aEC);    // add it
							DataGridViewCell cellConverter = aRow.Cells[nColumnConverterNames];
							cellConverter.Value = aEC.Name;
							cellConverter.ToolTipText = aEC.ToString();
						}
					}

					// clear the last one selected so that a right-click can be used to cancel the selection
					m_aECLast = null;
				}
			}
		}

		private void UpdateConverterNames()
		{
			foreach (DataGridViewRow aRow in dataGridViewFontsConverters.Rows)
			{
				string strFontname = (string)aRow.Cells[nColumnFontNames].Value;
				string strConverterName, strTooltip, strFontNameOutput;
				InitConverterDetails(strFontname, out strConverterName, out strTooltip, out strFontNameOutput);
				DataGridViewCell cell = aRow.Cells[nColumnConverterNames];
				cell.Value = strConverterName;
				cell.ToolTipText = strTooltip;
				aRow.Cells[nColumnTargetFontNames].Value = strFontNameOutput;
			}
		}

		private void newConverterMappingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_mapEncConverters.Clear();
			UpdateConverterNames();
		}

		private void openConverterMappingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlgSettings = new OpenFileDialog();
			dlgSettings.DefaultExt = "fcm";
			dlgSettings.InitialDirectory = Connect.GetAppDataDir;
			dlgSettings.Filter = "Font Converter mapping files (*.fcm)|*.fcm|All files|*.*";

			if (dlgSettings.ShowDialog() == DialogResult.OK)
			{
				LoadMappingFile(dlgSettings.FileName);
			}
		}

		protected void LoadMappingFile(string strFilename)
		{
			m_mapEncConverters = null;
			FileStream fs = new FileStream(strFilename, FileMode.Open);

			// Construct a SoapFormatter and use it
			// to serialize the data to the stream.
			try
			{
				SoapFormatter formatter = new SoapFormatter();
				formatter.Binder = new DirectableEncConverterDeserializationBinder();
				formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
				m_mapEncConverters = (Hashtable)formatter.Deserialize(fs);
				AddToConverterMappingRecentlyUsed(strFilename);
			}
			catch (SerializationException ex)
			{
				MessageBox.Show("Failed to open mapping file. Reason: " + ex.Message);
			}
			finally
			{
				fs.Close();
			}

			if ((m_mapEncConverters != null) && (m_mapEncConverters.Count > 0))
				UpdateConverterNames();
		}

		private void saveConverterMappingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog dlgSettings = new SaveFileDialog();
			dlgSettings.DefaultExt = "fcm";
			dlgSettings.FileName = "Font Converter mapping1.fcm";
			if (!Directory.Exists(Connect.GetAppDataDir))
				Directory.CreateDirectory(Connect.GetAppDataDir);
			dlgSettings.InitialDirectory = Connect.GetAppDataDir;
			dlgSettings.Filter = "Font Converter mapping files (*.fcm)|*.fcm|All files|*.*";
			if (dlgSettings.ShowDialog() == DialogResult.OK)
			{
				// Construct a SoapFormatter and use it
				// to serialize the data to the stream.
				FileStream fs = new FileStream(dlgSettings.FileName, FileMode.Create);
				SoapFormatter formatter = new SoapFormatter();
				try
				{
					formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
					formatter.Serialize(fs, m_mapEncConverters);
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

		private void dataGridViewFontsConverters_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			int nColumnIndex = e.ColumnIndex;
			// if the user clicks on the header... that doesn't work
			if (((e.RowIndex < 0) || (e.RowIndex > dataGridViewFontsConverters.Rows.Count))
				|| ((nColumnIndex < nColumnConverterNames) || (nColumnIndex > nColumnTargetFontNames)))
				return;

			DataGridViewRow row = this.dataGridViewFontsConverters.Rows[e.RowIndex];
			string strFontname = (string)row.Cells[nColumnFontNames].Value;
			DirectableEncConverter aEC = null;

			// don't allow the font to be configured before the converter
			if (!m_mapEncConverters.ContainsKey(strFontname))
				nColumnIndex = nColumnConverterNames;

			switch (nColumnIndex)
			{
				case nColumnConverterNames:
					// if the user right-clicked, then just repeat the last converter.
					//  (which now may be 'null' if cancelling an association)
					if (UsingSameEncConverter)
					{
						if (e.Button == MouseButtons.Right)
						{
							if (m_mapEncConverters.ContainsKey(strFontname))
								m_mapEncConverters.Remove(strFontname);
						}
						else
							aEC = m_aECForAll;
					}
					else
					{
						if (e.Button == MouseButtons.Right)
						{
							aEC = m_aECLast;
							if ((m_aECLast == null) && m_mapEncConverters.ContainsKey(strFontname))
								m_mapEncConverters.Remove(strFontname);
						}
						else
						{
							EncConverters aECs = OfficeApp.GetEncConverters;
							if (aECs != null)
							{
								IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, "Select Converter");

								if (aIEC != null)
									aEC = new DirectableEncConverter(aIEC);
							}
						}
					}

					if (aEC != null)
					{
						if (m_mapEncConverters.ContainsKey(strFontname))
							m_mapEncConverters.Remove(strFontname);
						m_mapEncConverters.Add(strFontname, aEC);
					}

					string strConverterName, strTooltip, strFontNameOutput;
					InitConverterDetails(strFontname, out strConverterName, out strTooltip, out strFontNameOutput);

					DataGridViewCell cellConverter = row.Cells[nColumnConverterNames];
					cellConverter.Value = strConverterName;
					cellConverter.ToolTipText = strTooltip;
					row.Cells[nColumnTargetFontNames].Value = strFontNameOutput;
					m_aECLast = aEC;
					/*
					if (m_mapEncConverters.Count > 0)
					{
						foreach (DataGridViewRow row2 in this.dataGridViewFontsConverters.Rows)
						{
							cellConverter = row2.Cells[nColumnConverterNames];
							if ((string)cellConverter.Value == cstrClickMsg)
							{
								cellConverter.Value = cstrDots;
								cellConverter.ToolTipText = null;
							}
						}
					}
					*/
					break;

				case nColumnTargetFontNames:
					System.Diagnostics.Debug.Assert(m_mapEncConverters[strFontname] != null);
					aEC = (DirectableEncConverter)m_mapEncConverters[strFontname];
					if (fontTargetDialog.ShowDialog() == DialogResult.OK)
					{
						aEC.TargetFont = fontTargetDialog.Font;
						row.Cells[nColumnTargetFontNames].Value = fontTargetDialog.Font.Name;
					}
					break;
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

		private void recentFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripDropDownItem aRecentFile = (ToolStripDropDownItem)sender;
			try
			{
				// put it in the open dialog's FileNames array because that's used to reload the file after
				//  conversion.
				LoadMappingFile(aRecentFile.Text);
			}
			catch (Exception ex)
			{
				// probably means the file doesn't exist anymore, so remove it from the recent used list
				Properties.Settings.Default.ConverterMappingRecentFiles.Remove(aRecentFile.Text);
				MessageBox.Show(ex.Message, Connect.cstrCaption);
			}
		}

		private void converterMappingToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			bool bMappingsExist = (m_mapEncConverters.Count > 0);
			this.newConverterMappingToolStripMenuItem.Enabled = bMappingsExist;
			this.saveConverterMappingToolStripMenuItem.Enabled = bMappingsExist;
			bool bRowsExist = (dataGridViewFontsConverters.Rows.Count > 0);
			this.setDefaultConverterToolStripMenuItem.Enabled = bRowsExist;

			recentToolStripMenuItem.DropDownItems.Clear();
			foreach (string strRecentFile in Properties.Settings.Default.ConverterMappingRecentFiles)
				recentToolStripMenuItem.DropDownItems.Add(strRecentFile, null, recentFilesToolStripMenuItem_Click);
			recentToolStripMenuItem.Enabled = (recentToolStripMenuItem.DropDownItems.Count > 0);
		}
	}

	/// <summary>
	/// A document (word-by-word) processor for determining Font names in use
	/// </summary>
	internal class FontNamesInUseProcessor : OfficeDocumentProcessor
	{
		BackgroundWorker m_worker = null; // who we send updates to
		List<string> m_astrListFontNames = new List<string>();
		int m_nCount = 0;
		int m_nModulo = 10;
		const int cnNumSteps = 50;

		public FontNamesInUseProcessor(int nDocumentWordCount, BackgroundWorker worker)
		{
			Process = myWordProcessor;

			// get a rough estimate of the number of words in the document
			m_nModulo = (nDocumentWordCount == 0) ? 0 : Math.Max(nDocumentWordCount / cnNumSteps, 2);
			m_worker = worker;
		}

		protected bool myWordProcessor(OfficeRange aWordRange, ref int nCharIndex)
		{
			if (m_worker.CancellationPending)
				return false;

			string strFontName = aWordRange.FontName;
			System.Diagnostics.Debug.Assert(strFontName != null);
			if (!String.IsNullOrEmpty(strFontName) && !m_astrListFontNames.Contains(strFontName))
			{
				m_astrListFontNames.Add(strFontName);
				string[] aStrs = new string[] { strFontName, aWordRange.Text };
				m_worker.ReportProgress(0, aStrs);
			}

			// show some user feedback
			if ((m_nModulo != 0) && ((m_nCount++ % m_nModulo) == 0))
				m_worker.ReportProgress(0);

			return true;
		}

		public List<string> FontNames
		{
			get { return m_astrListFontNames; }
		}
	}
}