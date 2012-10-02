using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ECInterfaces;
using SilEncConverters31;
using System.Xml;
using System.Xml.Xsl;
using System.Diagnostics;           // for Process

namespace DChartHelper
{
	public partial class DiscourseChartForm : Form
	{
		internal const string cstrCaption = "Discourse Chart Helper";
		private const string cstrHeader = "Ref,PreS,Subject,X1,X2,Verb,PostS";
		private const char cDelim = '"';
		protected DateTime m_dtStarted = DateTime.Now;
		TimeSpan m_timeMinStartup = new TimeSpan(0, 0, 3);
		protected const int nMaxRecentFiles = 15;

		protected Font m_fontVernacular = new Font("Arial Unicode MS", 9);
		protected Color m_colorVernacular = Color.Maroon;
		protected Font m_fontTransliteration = new Font("Doulos SIL", 9);
		protected Color m_colorTransliteration = Color.Green;
		protected Font m_fontGloss = new Font("Times New Roman", 9);
		protected Color m_colorGloss = Color.Blue;
		protected const int CnExtraHeight = 4;
		protected UndoDetails m_undoDetails = new UndoDetails();
		protected bool m_bModified = false;

		public DiscourseChartForm()
		{
			InitializeComponent();

			SetVernacularFontColor();

			int nIndex = dataGridViewChart.Rows.Add();  // always have one more than needed
			Debug.Assert(nIndex == 0);
			dataGridViewChart.Rows[nIndex].Tag = new GlossTranslations();
			SetHelpProviderStrings();

			LoadLastSettings();

			try
			{
				for (int i = 5; i >= 0; i--)
				{
					string strHeaderName = Properties.Settings.Default.ColumnNameDisplayIndices[i];
					dataGridViewChart.Columns[strHeaderName].DisplayIndex = i + 1;
				}
			}
			catch (Exception)
			{
				dataGridViewChart.Columns["PreS"].DisplayIndex = 1;
				dataGridViewChart.Columns["Subject"].DisplayIndex = 2;
				dataGridViewChart.Columns["X1"].DisplayIndex = 3;
				dataGridViewChart.Columns["X2"].DisplayIndex = 4;
				dataGridViewChart.Columns["Verb"].DisplayIndex = 5;
				dataGridViewChart.Columns["PostS"].DisplayIndex = 6;
			}
		}

		protected void LoadLastSettings()
		{
			Properties.Settings.Default.Reload();
			Point ptTopLeft = new Point();
			Size szForm = new Size();
			try
			{
				szForm.Width = Properties.Settings.Default.FormWidth;
				szForm.Height = Properties.Settings.Default.FormHeight;
				ptTopLeft.X = Properties.Settings.Default.PointX;
				ptTopLeft.Y = Properties.Settings.Default.PointY;
			}
			catch
			{
				Rectangle rectScreen = Screen.PrimaryScreen.WorkingArea;
				szForm.Width = rectScreen.Width;
				szForm.Height = rectScreen.Height / 2;
				ptTopLeft.X = ptTopLeft.Y = 0;
			}
			finally
			{
				this.Bounds = new Rectangle(ptTopLeft, szForm);
				Trace.WriteLine(String.Format("LoadLastSettings: {0}", Bounds.ToString()));
			}

			try
			{
				m_aECTransliterator = GetTransliterator;
				showTransToolStripMenuItem.Checked = Properties.Settings.Default.ShowTransliteration;
			}
			catch { }

			try
			{
				m_aECMeaningLookup = GetMeaningLookupConverter;
				showToolStripMenuItem.Checked = Properties.Settings.Default.ShowGloss;
			}
			catch { }
			SetColumnRowSpan();
		}

		public DiscourseChartForm(string strFilename)
			: this()
		{
			bool bIsChart = (Path.GetExtension(strFilename) == ".csv") || (Path.GetExtension(strFilename) == ".xml");
			if (bIsChart)
				this.OpenSavedGrid(strFilename);
			else
				this.OpenFile(strFilename);
			SetHelpProviderStrings();
			LoadLastSettings();
		}

		protected void SetHelpProviderStrings()
		{
			helpProvider.SetHelpString(dataGridViewChart, Properties.Resources.dataGridViewChartHelp);
			helpProvider.SetHelpString(dataGridViewGloss, Properties.Resources.dataGridViewGlossHelp);
			helpProvider.SetHelpString(richTextBoxText, Properties.Resources.richTextBoxTextHelp);
		}

		private void SetColumnRowSpan()
		{
			this.splitContainer.Panel2Collapsed = !(showToolStripMenuItem.Checked || showTransToolStripMenuItem.Checked);
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!CheckForModified())
				return;

			DialogResult res = this.openFileDialogRTF.ShowDialog();
			if (res == DialogResult.OK)
			{
				OpenFile(openFileDialogRTF.FileName);
			}
		}

		protected bool CheckForModified()
		{
			if (m_bModified)
			{
				DialogResult res = MessageBox.Show("Would you like to save the chart?", cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.Cancel)
					return false;
				else if (res == DialogResult.Yes)
					SaveHandler();
			}
			return true;
		}

		protected void OpenFile(string strFilename)
		{
			Reset();
			string[] astrsText = null;
			if (Path.GetExtension(strFilename) == ".rtf")
			{
				richTextBoxText.LoadFile(strFilename);
				astrsText = richTextBoxText.Lines;
			}
			else
				astrsText = File.ReadAllLines(strFilename);

			this.richTextBoxText.InitLines(astrsText);
			int nIndex = dataGridViewChart.Rows.Add();  // always have one more than needed
			Debug.Assert(nIndex == 0);
			dataGridViewChart.Rows[nIndex].Tag = new GlossTranslations();

			// add this filename to the list of recently used files
			if (Properties.Settings.Default.RecentFiles.Contains(strFilename))
				Properties.Settings.Default.RecentFiles.Remove(strFilename);
			else if (Properties.Settings.Default.RecentFiles.Count > nMaxRecentFiles)
				Properties.Settings.Default.RecentFiles.RemoveAt(nMaxRecentFiles);

			Properties.Settings.Default.RecentFiles.Insert(0, strFilename);
			Properties.Settings.Default.Save();
		}

		public void AddFilenameToTitle(string strFilename)
		{
			string strTitleName = Path.GetFileNameWithoutExtension(strFilename);
			this.Text = String.Format("{0} -- {1}", cstrCaption, strTitleName);
		}

		protected void Reset()
		{
			this.dataGridViewChart.Rows.Clear();
			m_nCurrRow = 0;
			m_dtStarted = DateTime.Now;
			this.lockChartToolStripMenuItem.Checked = false;
			this.adjustAllColumnWidthsToFitToolStripMenuItem.Checked = false;
			this.m_undoDetails.Clear();

			if (showTransToolStripMenuItem.Checked)
			{
				tableLayoutPanel.SetRowSpan(this.dataGridViewChart, 1);
			}
			else
			{
				tableLayoutPanel.SetRowSpan(this.dataGridViewChart, 2);
			}

			saveFileDialog.FileName = null;
		}

		private void dataGridViewGloss_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			// this triggers a rescan of the gloss/transliteration
			// (basically, forget that we'd done this one already and do it from scratch)
			DataGridViewRow theRow = dataGridViewGloss.Rows[e.RowIndex];
			Debug.Assert(theRow.Tag != null);
			DataGridViewRow theOrigChartRow = (DataGridViewRow)theRow.Tag;
			Debug.Assert(theOrigChartRow.Tag != null);

			GlossTranslations gts = (GlossTranslations)theOrigChartRow.Tag;
			if (showTransToolStripMenuItem.Checked && showToolStripMenuItem.Checked)
			{
				// if we're showing both...
				if ((e.RowIndex % 2) == 0)
					gts.TransInfo = null;   // even are transliterations
				else
					gts.GlossInfo = null;   // odd are glosses
			}
			else if (showTransToolStripMenuItem.Checked)
				gts.TransInfo = null;       // only showing transliterations
			else if (showToolStripMenuItem.Checked)
				gts.GlossInfo = null;       // only showing glosses
			else
			{
				Debug.Assert(false);
				return; // ignore
			}

			ShowTransAndGlossInfo();
		}

		private void dataGridViewGloss_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
		{
			// prevent the false click that occurs when the user chooses a menu item
			if ((DateTime.Now - m_dtStarted) < m_timeMinStartup)
				return;

			if ((e.RowIndex < 0) || (e.RowIndex >= dataGridViewGloss.Rows.Count)
				|| (e.ColumnIndex < 1) || (e.ColumnIndex >= dataGridViewGloss.Columns.Count))
				return;

			DataGridViewRow theRow = dataGridViewGloss.Rows[e.RowIndex];
			DataGridViewCell theCell = theRow.Cells[e.ColumnIndex];

			if (String.IsNullOrEmpty((string)theCell.Value))
				return;

			// if we're showing only transliterations...
			Debug.Assert(theRow.Tag != null);
			DataGridViewRow theOrigChartRow = (DataGridViewRow)theRow.Tag;
			Debug.Assert(theOrigChartRow.Tag != null);
			GlossTranslations gts = (GlossTranslations)theOrigChartRow.Tag;

			DirectableEncConverter theEC;
			GlossTranslationInfo gi;
			if (showTransToolStripMenuItem.Checked && !showToolStripMenuItem.Checked)
			{
				gi = gts.TransInfo;
				theEC = GetTransliterator;
			}
			else if (!showTransToolStripMenuItem.Checked && showToolStripMenuItem.Checked)
			{
				// or if we showing only glosses...
				gi = gts.GlossInfo;
				theEC = GetMeaningLookupConverter;
			}
			else if (showTransToolStripMenuItem.Checked && showToolStripMenuItem.Checked)
			{
				// or we're showing both...
				if ((e.RowIndex % 2) == 0)
				{
					gi = gts.TransInfo;
					theEC = GetTransliterator;
				}
				else
				{
					gi = gts.GlossInfo;
					theEC = GetMeaningLookupConverter;
				}
			}
			else
			{
				Debug.Assert(false);
				return; // ignore
			}

			if (gi == null)
			{
				Debug.Assert(false);
				gi = new GlossTranslationInfo();
			}

			PickAmbiguity dlg = new PickAmbiguity(theCell.Value,
				(e.Button == MouseButtons.Left) ? null : theEC,
				m_fontVernacular, m_fontGloss);
			dlg.Location = e.Location;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				theCell.Value = dlg.DisambiguatedPhrase;
				switch (e.ColumnIndex)
				{
					case 1:
						gi.PreS = (string)theCell.Value;
						break;
					case 2:
						gi.Subject = (string)theCell.Value;
						break;
					case 3:
						gi.X1 = (string)theCell.Value;
						break;
					case 4:
						gi.X2 = (string)theCell.Value;
						break;
					case 5:
						gi.Verb = (string)theCell.Value;
						break;
					case 6:
						gi.PostS = (string)theCell.Value;
						break;
				}
				/*
				object[] aoGlossValues = (object[]);
				aoGlossValues[e.ColumnIndex] = theCell.Value;
				 */
			}
		}

		protected int m_nCurrRow = 0;
		private void dataGridViewChart_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
		{
			// prevent the false click that occurs when the user chooses a menu item
			if ((DateTime.Now - m_dtStarted) < m_timeMinStartup)
				return;

			if ((e.RowIndex < 0) || (e.RowIndex >= dataGridViewChart.Rows.Count)
				|| (e.ColumnIndex < 0) || (e.ColumnIndex >= dataGridViewChart.Columns.Count))
				return;

			if (this.lockChartToolStripMenuItem.Checked)
				return;

			m_bModified = true;
			if (e.ColumnIndex == 0)
			{
				// for the reference row, this just means switch to the next reference
				DataGridViewCell theRefCell = dataGridViewChart.Rows[e.RowIndex].Cells[0];
				string strExistingReference = (string)theRefCell.Value;
				if ((strExistingReference == null) || (e.Button == MouseButtons.Right))
				{
					if ((e.Button == MouseButtons.Right) && (e.RowIndex > 0))
						strExistingReference = (string)dataGridViewChart.Rows[e.RowIndex - 1].Cells[0].Value;
					if ((strExistingReference == " ") && (e.RowIndex > 1))
						strExistingReference = (string)dataGridViewChart.Rows[e.RowIndex - 2].Cells[0].Value;

					theRefCell.Value = NextRef(strExistingReference);
				}
				else if( strExistingReference != " ")
					theRefCell.Value = CycleReference(strExistingReference);
			}
			else
			{
				string strWord = null;
				if (e.Button == MouseButtons.Left)
				{
					if (e.RowIndex > m_nCurrRow)
					{
						// add a new extra one.
						m_nCurrRow = e.RowIndex;
						int nIndex = this.dataGridViewChart.Rows.Add();
						DataGridViewRow theRow = dataGridViewChart.Rows[nIndex];
						theRow.Height = m_fontVernacular.Height + CnExtraHeight;
						theRow.Tag = new GlossTranslations();
						foreach (DataGridViewCell aCell in theRow.Cells)
						{
							aCell.Value = null;
							aCell.Selected = false;
						}
					}

					// get the word from the text box
					strWord = richTextBoxText.GetCurrentWord();
					DataGridViewCell aCurrCell = dataGridViewChart.Rows[e.RowIndex].Cells[e.ColumnIndex];
					m_undoDetails.GoForward(aCurrCell, strWord, m_fontVernacular, m_fontGloss);

					// help out with the reference number
					DataGridViewCell theRefCell = dataGridViewChart.Rows[e.RowIndex].Cells[0];
					string strValue = (string)theRefCell.Value;
					if ((strValue == null) || (strValue.Length == 0))
					{
						if (e.RowIndex > 0)
							strValue = (string)dataGridViewChart.Rows[e.RowIndex - 1].Cells[0].Value;

						if ((strValue == " ") && (e.RowIndex > 1))
							strValue = (string)dataGridViewChart.Rows[e.RowIndex - 2].Cells[0].Value;

						theRefCell.Value = NextRef(strValue);
					}
				}
				else
				{
					// dataGridViewChart.EndEdit();    // just in case
					m_undoDetails.Undo(out strWord);
					if (strWord != null)
						richTextBoxText.SetCurrentWord(strWord);
					else
					{
						DataGridViewCell theCell = dataGridViewChart.Rows[e.RowIndex].Cells[e.ColumnIndex];
						if (theCell.Value != null)
						{
							PickAmbiguity dlg = new PickAmbiguity(theCell.Value,
								(e.Button == MouseButtons.Left) ? null : GetMeaningLookupConverter,
								m_fontVernacular, m_fontGloss);
							if (dlg.ShowDialog() == DialogResult.OK)
							{

							}
						}
					}
				}
			}
		}

		protected static char[] achNumbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		private string CycleReference(string strExistingReference)
		{
			string strNextReference = null;
			try
			{
				// if this works, then we're in the number only for (e.g. 13)...
				int nValue = Convert.ToInt32(strExistingReference);

				// so cycle it to the with letter form
				strNextReference = strExistingReference + 'a';
			}
			catch (Exception)
			{
				// when this fails, it means there was a letter in the existing ref
				// so cycling means: bump up the number to the next whole #
				int nIndex = strExistingReference.LastIndexOfAny(achNumbers);
				strNextReference = Convert.ToString(Convert.ToInt32(strExistingReference.Substring(0, nIndex + 1)) + 1) + 'a';
			}
			return strNextReference;
		}

		private string NextRef(string strLastReference)
		{
			string strNextReference = null;
			if ((strLastReference != null) && (strLastReference != " ") && (strLastReference.Length > 0))
			{
				int nIndex = strLastReference.LastIndexOfAny(achNumbers);
				char chNew = 'a';
				if ((nIndex != -1) && (strLastReference.Length > nIndex + 1))
					chNew = (char)(((int)strLastReference[nIndex + 1]) + 1);
				else
					strLastReference = Convert.ToString(Convert.ToInt32(strLastReference) + 1);
				strNextReference = strLastReference.Substring(0, nIndex + 1) + chNew;
			}
			else
			{
				strNextReference = "1a";
			}
			return strNextReference;
		}

		private void dataGridViewChart_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			Trace.WriteLine(string.Format("code: {0}, data: {1}, value: {2}",
				e.KeyCode, e.KeyData, e.KeyValue));

			if ((e.KeyCode == Keys.F2) && lockChartToolStripMenuItem.Checked)
				return;

			else if (e.KeyCode == Keys.Delete)
			{
				dataGridViewChart.CurrentCell.Value = null;
				dataGridViewChart.CurrentCell.ToolTipText = null;
			}
			else if (e.Control && (e.KeyCode == Keys.C))
			{
				DoCopy();
			}
			else if (!e.Control && !e.Alt)
			{
				if ((e.KeyCode != Keys.Up)
					&& (e.KeyCode != Keys.Right)
					&& (e.KeyCode != Keys.Right)
					&& (e.KeyCode != Keys.Right))
					dataGridViewChart.BeginEdit(true);
			}
		}

		protected string FormatColorAsHtmlColor(Color color)
		{
			return String.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
		}

		protected void DoCopy()
		{
			// write the row data as XML to a memory stream
			MemoryStream streamData = new MemoryStream();
			using (DiscourseChartDataClass ds = GetDataSet(true))
			{
				ds.WriteXml(streamData);
				streamData.Seek(0, SeekOrigin.Begin);   // rewind so it's at the start
			}

			// get the names of the columns in DisplayIndex order
			string[] astrColumnNames = new string[6];
			for (int i = 1; i < 7; i++)
			{
				DataGridViewColumn aColumn = dataGridViewChart.Columns[i];
				int nDisplayIndex = aColumn.DisplayIndex;
				astrColumnNames[nDisplayIndex - 1] = aColumn.HeaderText;
			}

			// get the XSLT format string (i.e. without the font names/sizes or column names) and then populate
			//  the appropriate information. (this doesn't support repositioning the "Ref" column)
			string strXsltString = String.Format(Properties.Resources.DiscourseChart2HtmlFormatString,
				"Ref",
				astrColumnNames[0],
				astrColumnNames[1],
				astrColumnNames[2],
				astrColumnNames[3],
				astrColumnNames[4],
				astrColumnNames[5],
				m_fontVernacular.Name,
				FormatColorAsHtmlColor(m_colorVernacular),
				m_fontTransliteration.Name,
				FormatColorAsHtmlColor(m_colorTransliteration),
				m_fontGloss.Name,
				FormatColorAsHtmlColor(m_colorGloss));

			// write the formatted XSLT to another memory stream.
			MemoryStream streamXSLT = new MemoryStream(Encoding.UTF8.GetBytes(strXsltString));

			// transform the row data to HTML using the XSLT.
			string strCBData = TransformedRowXmlDataToHtml(streamXSLT, streamData);

			// finally copy it to clipboard.
			DataObject obj = new DataObject();
			CopyHtmlToClipBoard(strCBData, ref obj);
			Clipboard.SetDataObject(obj, true);
		}

		protected string TransformedRowXmlDataToHtml(Stream streamXSLT, Stream streamData)
		{
			XslCompiledTransform myProcessor = new XslCompiledTransform();
			XmlReader xslReader = XmlReader.Create(streamXSLT);
			myProcessor.Load(xslReader);

			XmlReader reader = XmlReader.Create(streamData);
			StringBuilder strBuilder = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			XmlWriter writer = XmlWriter.Create(strBuilder, settings);
			myProcessor.Transform(reader, null, writer);
			return strBuilder.ToString();
		}

		public static void CopyHtmlToClipBoard(string html, ref DataObject obj)
		{
			Encoding enc = Encoding.UTF8;

			string begin = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}"
				+ "\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";

			string html_begin = "<html>\r\n<head>\r\n"
				+ "<meta http-equiv=\"Content-Type\""
				+ " content=\"text/html; charset=" + enc.WebName + "\">\r\n"
				+ "<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n"
				+ "<!--StartFragment-->";

			string html_end = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";

			string begin_sample = String.Format(begin, 0, 0, 0, 0);

			int count_begin = enc.GetByteCount(begin_sample);
			int count_html_begin = enc.GetByteCount(html_begin);
			int count_html = enc.GetByteCount(html);
			int count_html_end = enc.GetByteCount(html_end);

		   string html_total = String.Format(
			  begin
			  , count_begin
			  , count_begin + count_html_begin + count_html + count_html_end
			  , count_begin + count_html_begin
			  , count_begin + count_html_begin + count_html
			  ) + html_begin + html + html_end;

		   obj.SetData(DataFormats.Html, new MemoryStream(enc.GetBytes(html_total)));
		}

		private void buttonPrevParagraph_Click(object sender, EventArgs e)
		{
			richTextBoxText.MovePrevLine();
		}

		private void buttonNextParagraph_Click(object sender, EventArgs e)
		{
			richTextBoxText.MoveNextLine();
		}

		private string Trim(string str)
		{
			return (!String.IsNullOrEmpty(str)) ? str.Trim() : null;
		}

		private string Transliterate(string str)
		{
			string strTranslit = null;
			try
			{
				if (!String.IsNullOrEmpty(str) && (GetTransliterator != null) && (GetTransliterator.GetEncConverter != null))
					strTranslit = GetTransliterator.Convert(str);
				if (!String.IsNullOrEmpty(strTranslit))
					strTranslit = strTranslit.Trim();
			}
			catch { }
			return strTranslit;
		}

		private string Gloss(string str)
		{
			string strGloss = null;
			try
			{
				if (!String.IsNullOrEmpty(str) && (GetMeaningLookupConverter != null) && (GetMeaningLookupConverter.GetEncConverter != null))
					strGloss = GetMeaningLookupConverter.Convert(str);
				if (!String.IsNullOrEmpty(strGloss))
					strGloss = strGloss.Trim();
			}
			catch { }
			return strGloss;
		}

		private DiscourseChartDataClass GetDataSet(bool bSelectedOnly)
		{
			DiscourseChartDataClass file = new DiscourseChartDataClass();
			file.DiscourseChartData.AddDiscourseChartDataRow(
				dataGridViewChart.Columns[0].DisplayIndex,
				dataGridViewChart.Columns[1].DisplayIndex,
				dataGridViewChart.Columns[2].DisplayIndex,
				dataGridViewChart.Columns[3].DisplayIndex,
				dataGridViewChart.Columns[4].DisplayIndex,
				dataGridViewChart.Columns[5].DisplayIndex,
				dataGridViewChart.Columns[6].DisplayIndex);

			DiscourseChartDataClass.DiscourseChartDataRow aDCDRow = file.DiscourseChartData[0];
			DiscourseChartDataClass.FontsRow fonts = file.Fonts.AddFontsRow(aDCDRow);
			file.VernacularFont.AddVernacularFontRow(m_fontVernacular.Name, m_fontVernacular.Size, m_colorVernacular.Name, fonts);
			file.TransliterationFont.AddTransliterationFontRow(m_fontTransliteration.Name, m_fontTransliteration.Size, m_colorTransliteration.Name, fonts);
			file.GlossFont.AddGlossFontRow(m_fontGloss.Name, m_fontGloss.Size, m_colorGloss.Name, fonts);

			foreach (DataGridViewRow aRow in dataGridViewChart.Rows)
			{
				string strRef = (string)aRow.Cells[0].Value;
				if (String.IsNullOrEmpty(strRef) || (bSelectedOnly && !aRow.Selected))
					continue;
				Debug.Assert(aRow.Tag != null);
				GlossTranslations gts = (GlossTranslations)aRow.Tag;
				string strFreeTr = gts.FreeTranslation;
				DiscourseChartDataClass.DiscourseClauseRow aClause = file.DiscourseClause.AddDiscourseClauseRow(strRef, strFreeTr, aDCDRow);

				string strPreS = Trim((string)aRow.Cells[1].Value);
				string strSubj = Trim((string)aRow.Cells[2].Value);
				string strX1 = Trim((string)aRow.Cells[3].Value);
				string strX2 = Trim((string)aRow.Cells[4].Value);
				string strVerb = Trim((string)aRow.Cells[5].Value);
				string strPostV = Trim((string)aRow.Cells[6].Value);

				file.Vernacular.AddVernacularRow(strPreS, strSubj, strX1, strX2,
						strVerb, strPostV, aClause);

				if ((!bSelectedOnly || showTransToolStripMenuItem.Checked) && (GetTransliterator != null))
				{
					GlossTranslationInfo ti = gts.TransInfo;
					if (ti != null)
					{
						strPreS = ti.PreS;
						strSubj = ti.Subject;
						strX1 = ti.X1;
						strX2 = ti.X2;
						strVerb = ti.Verb;
						strPostV = ti.PostS;
					}
					else if (!String.IsNullOrEmpty(GetTransliterator.Name))
					{
						strPreS = Transliterate(strPreS);
						strSubj = Transliterate(strSubj);
						strX1 = Transliterate(strX1);
						strX2 = Transliterate(strX2);
						strVerb = Transliterate(strVerb);
						strPostV = Transliterate(strPostV);
					}

					file.Transliteration.AddTransliterationRow(strPreS, strSubj, strX1, strX2,
						strVerb, strPostV, aClause);
				}

				if ((!bSelectedOnly || showToolStripMenuItem.Checked) && (GetMeaningLookupConverter != null))
				{
					GlossTranslationInfo gi = gts.GlossInfo;
					if (gi != null)
					{
						strPreS = gi.PreS;
						strSubj = gi.Subject;
						strX1 = gi.X1;
						strX2 = gi.X2;
						strVerb = gi.Verb;
						strPostV = gi.PostS;
					}
					else if (!String.IsNullOrEmpty(GetMeaningLookupConverter.Name))
					{
						strPreS = Gloss(strPreS);
						strSubj = Gloss(strSubj);
						strX1 = Gloss(strX1);
						strX2 = Gloss(strX2);
						strVerb = Gloss(strVerb);
						strPostV = Gloss(strPostV);
					}

					file.Gloss.AddGlossRow(strPreS, strSubj, strX1, strX2,
						strVerb, strPostV, aClause);
				}
			}
			return file;
		}

		private void SaveFile(string strFilename, bool bXML)
		{
			if (bXML)
			{
				GetDataSet(false).WriteXml(strFilename);
			}
			else
			{
				StreamWriter sw = new StreamWriter(strFilename, false, Encoding.UTF8);
				sw.WriteLine(cstrHeader);

				foreach (DataGridViewRow aRow in dataGridViewChart.Rows)
				{
					string strLine = null;
					bool bSomethingThere = false;
					foreach (DataGridViewCell aCell in aRow.Cells)
					{
						string strEntry = (string)aCell.Value;
						if ((strEntry != null) && (strEntry.Length > 0))
						{
							strEntry = strEntry.Trim();

							int nQuoteIndex = -1;
							while ((nQuoteIndex = strEntry.IndexOf('"', ++nQuoteIndex)) != -1)
								strEntry = strEntry.Insert(++nQuoteIndex, "\"");

							strLine += string.Format("{0}{1}{0},", cDelim, strEntry);
							bSomethingThere = true;
						}
						else
							strLine += ",";
					}
					if (bSomethingThere)
					{
						strLine = strLine.Remove(strLine.Length - 1);
						sw.WriteLine(strLine);
					}
				}

				sw.Flush();
				sw.Close();
			}

			// add this filename to the list of recently used files
			if (Properties.Settings.Default.RecentCharts.Contains(strFilename))
				Properties.Settings.Default.RecentCharts.Remove(strFilename);
			else if (Properties.Settings.Default.RecentCharts.Count > nMaxRecentFiles)
				Properties.Settings.Default.RecentCharts.RemoveAt(nMaxRecentFiles);

			Properties.Settings.Default.RecentCharts.Insert(0, strFilename);
			Properties.Settings.Default.Save();

			m_bModified = false;

			AddFilenameToTitle(strFilename);
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveHandler();
		}

		protected bool SaveHandler()
		{
			if ((saveFileDialog.FileName.Length > 0) || (saveFileDialog.ShowDialog() == DialogResult.OK))
			{
				bool bXML = ((saveFileDialog.FileName.Substring(saveFileDialog.FileName.Length - 3) == "xml")
					|| (saveFileDialog.FilterIndex == 1));
				SaveFile(saveFileDialog.FileName, bXML);
				return true;
			}
			return false;
		}

		private void saveChartAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			saveFileDialog.FileName = null;
			SaveHandler();
		}

		private void openSavedGridToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!CheckForModified())
				return;

			if (openFileDialogCsv.ShowDialog() == DialogResult.OK)
			{
				OpenSavedGrid(openFileDialogCsv.FileName);
			}
		}

		private void OpenSavedGrid(string strFilename)
		{
			Reset();
			bool bXML = (Path.GetExtension(strFilename).ToLower() == ".xml");
			if (bXML)
			{
				DiscourseChartDataClass file = new DiscourseChartDataClass();
				file.ReadXml(strFilename);

				// get the font colors
				if (file.VernacularFont.Count > 0)
				{
					DiscourseChartDataClass.VernacularFontRow aVFRow = file.VernacularFont[0];
					m_fontVernacular = new Font(aVFRow.FontName, aVFRow.FontSize);
					m_colorVernacular = Color.FromName(aVFRow.FontColor);
					SetVernacularFontColor();
				}

				if (file.GlossFont.Count > 0)
				{
					DiscourseChartDataClass.GlossFontRow aGFRow = file.GlossFont[0];
					m_fontGloss = new Font(aGFRow.FontName, aGFRow.FontSize);
					m_colorGloss = Color.FromName(aGFRow.FontColor);
				}

				if (file.TransliterationFont.Count > 0)
				{
					DiscourseChartDataClass.TransliterationFontRow aTFRow = file.TransliterationFont[0];
					m_fontTransliteration = new Font(aTFRow.FontName, aTFRow.FontSize);
					m_colorTransliteration = Color.FromName(aTFRow.FontColor);
				}

				foreach (DiscourseChartDataClass.DiscourseClauseRow aClause in file.DiscourseClause)
				{
					Debug.Assert(aClause.GetVernacularRows().Length > 0);
					DiscourseChartDataClass.VernacularRow aVernRow = aClause.GetVernacularRows()[0];
					int nIndex = dataGridViewChart.Rows.Add();
					DataGridViewRow aRow = dataGridViewChart.Rows[nIndex];
					aRow.Height = m_fontVernacular.Height + CnExtraHeight;
					aRow.HeaderCell.ToolTipText = "Right-click to add/edit a free translation";

					aRow.Cells[0].Value = aClause.Ref;
					aRow.Cells[1].Value = (aVernRow.IsPreSNull()) ? null : aVernRow.PreS;
					aRow.Cells[2].Value = (aVernRow.IsSubjectNull()) ? null : aVernRow.Subject;
					aRow.Cells[3].Value = (aVernRow.IsX1Null()) ? null : aVernRow.X1;
					aRow.Cells[4].Value = (aVernRow.IsX2Null()) ? null : aVernRow.X2;
					aRow.Cells[5].Value = (aVernRow.IsVerbNull()) ? null : aVernRow.Verb;
					aRow.Cells[6].Value = (aVernRow.IsPostSNull()) ? null : aVernRow.PostS;

					GlossTranslations gts = new GlossTranslations
												{
													Reference = aClause.Ref,
													FreeTranslation = (!aClause.IsFreeTranslationNull()) ? aClause.FreeTranslation : null
												};

					aRow.Tag = gts;

					DiscourseChartDataClass.GlossRow[] aGRs = aClause.GetGlossRows();
					if (aGRs.Length > 0)
					{
						DiscourseChartDataClass.GlossRow aGlossRow = aGRs[0];
						gts.GlossInfo = new GlossTranslationInfo
											{
												PreS = (aGlossRow.IsPreSNull()) ? null : aGlossRow.PreS,
												Subject = (aGlossRow.IsSubjectNull()) ? null : aGlossRow.Subject,
												X1 = (aGlossRow.IsX1Null()) ? null : aGlossRow.X1,
												X2 = (aGlossRow.IsX2Null()) ? null : aGlossRow.X2,
												Verb = (aGlossRow.IsVerbNull()) ? null : aGlossRow.Verb,
												PostS = (aGlossRow.IsPostSNull()) ? null : aGlossRow.PostS
											};
					}

					DiscourseChartDataClass.TransliterationRow[] theTRs = aClause.GetTransliterationRows();
					if (theTRs.Length > 0)
					{
						DiscourseChartDataClass.TransliterationRow aTransliterationRow = theTRs[0];
						gts.TransInfo = new GlossTranslationInfo
											{
												PreS = (aTransliterationRow.IsPreSNull()) ? null : aTransliterationRow.PreS,
												Subject = (aTransliterationRow.IsSubjectNull()) ? null : aTransliterationRow.Subject,
												X1 = (aTransliterationRow.IsX1Null()) ? null : aTransliterationRow.X1,
												X2 = (aTransliterationRow.IsX2Null()) ? null : aTransliterationRow.X2,
												Verb = (aTransliterationRow.IsVerbNull()) ? null : aTransliterationRow.Verb,
												PostS = (aTransliterationRow.IsPostSNull()) ? null : aTransliterationRow.PostS
											};
					}
				}

				// adjust the column indices
				if (file.DiscourseChartData.Count > 0)
				{
					DiscourseChartDataClass.DiscourseChartDataRow aDCDRow = file.DiscourseChartData[0];
					dataGridViewChart.Columns[6].DisplayIndex = aDCDRow.DisplayIndexPostS;
					dataGridViewChart.Columns[5].DisplayIndex = aDCDRow.DisplayIndexVerb;
					dataGridViewChart.Columns[4].DisplayIndex = aDCDRow.DisplayIndexX2;
					dataGridViewChart.Columns[3].DisplayIndex = aDCDRow.DisplayIndexX1;
					dataGridViewChart.Columns[2].DisplayIndex = aDCDRow.DisplayIndexSubject;
					dataGridViewChart.Columns[1].DisplayIndex = aDCDRow.DisplayIndexPreS;
					dataGridViewChart.Columns[0].DisplayIndex = aDCDRow.DisplayIndexRef;
				}

				saveFileDialog.FileName = strFilename;  // so we know what file to save later
			}
			else
			{
				Debug.Assert(false);
				string[] astrLines = File.ReadAllLines(strFilename, Encoding.UTF8);
				if (astrLines[0] != cstrHeader)
				{
					MessageBox.Show("This doesn't look like a file that I've written!", cstrCaption);
				}
				else
				{
					for (int i = 1; i < astrLines.Length; i++)
					{
						string strLine = astrLines[i];
						string[] astrWords = strLine.Split(new char[] { ',' });

						int nIndex = dataGridViewChart.Rows.Add();
						DataGridViewRow aRow = dataGridViewChart.Rows[nIndex];
						aRow.Height = m_fontVernacular.Height + CnExtraHeight;
						for (int j = 0, k = 0; j < astrWords.Length; j++, k++ )
						{
							string strWord = astrWords[j];
							if ((strWord != null) && (strWord.Length > 1))
							{
								while(strWord[strWord.Length - 1] != cDelim)
									// this means that this word had a comma in it
									//  (which mistakenly was tokenized away), so add the
									//  next word as well. (probably should count "s to make
									//  sure they're even
									strWord += ',' + astrWords[++j];

								// then strip off the delimiters
								Debug.Assert(strWord.Length > 2);
								strWord = strWord.Substring(1, strWord.Length - 2);

								// remove the extra double-quote(s) added
								int nQuoteIndex = 0;
								while ((nQuoteIndex = strWord.IndexOf('"', nQuoteIndex)) != -1)
									strWord = strWord.Remove(++nQuoteIndex, 1);
							}

							aRow.Cells[k].Value = strWord;
						}
					}
				}
			}

			AddFilenameToTitle(strFilename);
			m_bModified = false;
		}

		private void nextWordToolStripMenuItem_Click(object sender, EventArgs e)
		{
			richTextBoxText.NextWord();
		}

		private void skipToNextLineToolStripMenuItem_Click(object sender, EventArgs e)
		{
			richTextBoxText.MoveNextLine();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void DiscourseChartForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!CheckForModified())
				e.Cancel = true;
		}

		private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			// paste from clipboard
			IDataObject iData = Clipboard.GetDataObject();

			// Determines whether the data is in a format you can use.
			if (iData.GetDataPresent(DataFormats.UnicodeText))
			{
				this.richTextBoxText.PutLineParse((string)iData.GetData(DataFormats.UnicodeText));
			}
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Reset();
			m_bModified = false;
			int nIndex = dataGridViewChart.Rows.Add();
			Debug.Assert(nIndex == 0);
			dataGridViewChart.Rows[nIndex].Tag = new GlossTranslations();
		}

		private void resizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewColumn aColumn in dataGridViewChart.Columns)
			{
				aColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				aColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
			}
			foreach (DataGridViewColumn aColumn in dataGridViewGloss.Columns)
			{
				aColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				aColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
			}
			adjustAllColumnWidthsToFitToolStripMenuItem.Checked = false;
		}

		private void dataGridViewChart_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
		{
			if (    showTransToolStripMenuItem.Checked
				||  showToolStripMenuItem.Checked)
			{
				ShowTransAndGlossInfo();
			}
		}

		private void dataGridViewChart_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
		{
			dataGridViewGloss.Columns[e.Column.Index].DisplayIndex = e.Column.DisplayIndex;

			string[] astrColumnNames = new string[6];
			foreach (DataGridViewColumn aGridCol in dataGridViewChart.Columns)
				if (aGridCol.DisplayIndex > 0)
					astrColumnNames[aGridCol.DisplayIndex - 1] = aGridCol.HeaderText;

			// save column display index so we can re-init next time
			if (Properties.Settings.Default.ColumnNameDisplayIndices == null)
				Properties.Settings.Default.ColumnNameDisplayIndices = new System.Collections.Specialized.StringCollection();

			Properties.Settings.Default.ColumnNameDisplayIndices.Clear();
			Properties.Settings.Default.ColumnNameDisplayIndices.AddRange(astrColumnNames);
			Properties.Settings.Default.Save();

			foreach (string str in Properties.Settings.Default.ColumnNameDisplayIndices)
				Trace.WriteLine(str);
		}

		private void ShowTransAndGlossInfo()
		{
			dataGridViewGloss.Rows.Clear(); // first clear it
			foreach (DataGridViewRow aRow in dataGridViewChart.Rows)
				if (aRow.Selected)
				{
					if (aRow.Tag == null)
						continue;   // this might happen if the user selects the last (empty) row in the above chart

					GlossTranslations gts = (GlossTranslations)aRow.Tag;

					// first add the transliteration row if checked
					if (showTransToolStripMenuItem.Checked)
					{
						object[] aoTransValues = GetTransliterationData(gts, aRow);
						int nNewRow = dataGridViewGloss.Rows.Add(aoTransValues);
						DataGridViewRow rowTrans = dataGridViewGloss.Rows[nNewRow];
						rowTrans.Height = m_fontTransliteration.Height + CnExtraHeight;
						rowTrans.DefaultCellStyle.Font = m_fontTransliteration;
						rowTrans.DefaultCellStyle.ForeColor = m_colorTransliteration;
						rowTrans.Tag = aRow;
						rowTrans.HeaderCell.ToolTipText = "Click here to re-query the tranliterations";
					}

					// next see if we're supposed to add the gloss lookup
					// (which, if we've already done it once, will be in the row's Tag
					//  member -- we might have even disambiguated it, so we want to
					//  keep that around)
					if (showToolStripMenuItem.Checked)
					{
						object[] aoGlossValues = GetGlossData(gts, aRow);
						int nIndex = dataGridViewGloss.Rows.Add(aoGlossValues);
						DataGridViewRow rowGloss = dataGridViewGloss.Rows[nIndex];
						rowGloss.Height = m_fontGloss.Height + CnExtraHeight;
						rowGloss.DefaultCellStyle.Font = m_fontGloss;
						rowGloss.DefaultCellStyle.ForeColor = m_colorGloss;
						rowGloss.Tag = aRow;    // save which row this came from so we can find it during disambiguation
						rowGloss.HeaderCell.ToolTipText = "Click here to re-query the glosses";
					}
				}
		}

		private object[] GetTransliterationData(GlossTranslations gts, DataGridViewRow theVernRow)
		{
			if (gts.TransInfo == null)
			{
				// otherwise, grab the value out of the main (upper) chart, and transliterate it.
				gts.Reference = (string)theVernRow.Cells[0].Value;
				gts.TransInfo = new GlossTranslationInfo
				{
					PreS = TransliteratorCell(theVernRow, 1),
					Subject = TransliteratorCell(theVernRow, 2),
					X1 = TransliteratorCell(theVernRow, 3),
					X2 = TransliteratorCell(theVernRow, 4),
					Verb = TransliteratorCell(theVernRow, 5),
					PostS = TransliteratorCell(theVernRow, 6)
				};
			}

			object[] aoTransValues = new object[]
			{
				gts.Reference,
				gts.TransInfo.PreS,
				gts.TransInfo.Subject,
				gts.TransInfo.X1,
				gts.TransInfo.X2,
				gts.TransInfo.Verb,
				gts.TransInfo.PostS
			};
			return aoTransValues;
		}

		private object[] GetGlossData(GlossTranslations gts, DataGridViewRow theVernRow)
		{
			if (gts.GlossInfo == null)
			{
				// otherwise, grab the value out of the main (upper) chart, and transliterate it.
				gts.Reference = (string)theVernRow.Cells[0].Value;
				gts.GlossInfo = new GlossTranslationInfo
				{
					PreS = GlossCell(theVernRow, 1),
					Subject = GlossCell(theVernRow, 2),
					X1 = GlossCell(theVernRow, 3),
					X2 = GlossCell(theVernRow, 4),
					Verb = GlossCell(theVernRow, 5),
					PostS = GlossCell(theVernRow, 6)
				};
			}

			object[] aoGlossValues = new object[]
			{
				gts.Reference,
				gts.GlossInfo.PreS,
				gts.GlossInfo.Subject,
				gts.GlossInfo.X1,
				gts.GlossInfo.X2,
				gts.GlossInfo.Verb,
				gts.GlossInfo.PostS
			};
			return aoGlossValues;
		}

		protected string TransliteratorCell(DataGridViewRow theVernRow, int nColumn)
		{
			string strValue = (string)theVernRow.Cells[nColumn].Value;
			if (!String.IsNullOrEmpty(strValue))
				return Transliterate(strValue);
			return null;
		}

		protected string GlossCell(DataGridViewRow theVernRow, int nColumn)
		{
			string strValue = (string)theVernRow.Cells[nColumn].Value;
			if (!String.IsNullOrEmpty(strValue))
				return Gloss(strValue);
			return null;
		}

		private void dataGridViewChart_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			// right-click on the row header means to add a free translation
			if ((e.Button == MouseButtons.Right) && (e.RowIndex >= 0))
			{
				DataGridViewRow theRow = dataGridViewChart.Rows[e.RowIndex];
				if (theRow.Tag == null)
					return; // this could happen if the user clicks on the 'add new row' row header

				GlossTranslations gts = (GlossTranslations)theRow.Tag;
				QueryFreeTr dlg = new QueryFreeTr(gts.FreeTranslation);
				if (dlg.ShowDialog() == DialogResult.OK)
					gts.FreeTranslation = (!String.IsNullOrEmpty(dlg.FreeTranslation)) ? dlg.FreeTranslation : null;
			}
		}

		DataGridViewRow m_rowLastFound = null;
		DataGridViewCell m_cellLastFound = null;
		string m_strFindWhat = null;

		private void findToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FindWhatDialog dlg = new FindWhatDialog();
			dlg.FindWhat = m_strFindWhat;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				foreach (DataGridViewRow aRow in dataGridViewChart.Rows)
					foreach (DataGridViewCell aCell in aRow.Cells)
					{
						string strValue = (string)aCell.Value;
						if (String.IsNullOrEmpty(strValue))
							aCell.Selected = false;
						else
							aCell.Selected = (strValue.IndexOf((m_strFindWhat = dlg.FindWhat)) != -1);
					}
			}
		}

		private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool bFoundLastFoundRow = false;
			bool bFoundLastFoundCell = false;
			foreach (DataGridViewRow aRow in dataGridViewChart.Rows)
			{
				if (!bFoundLastFoundRow && (aRow != m_rowLastFound))
					continue;
				else
					bFoundLastFoundRow = true;

				foreach (DataGridViewCell aCell in aRow.Cells)
				{
					if (!bFoundLastFoundCell)
					{
						if (aCell == m_cellLastFound)
							bFoundLastFoundCell = true;
						continue;
					}

					string strValue = (string)aCell.Value;
					if (!String.IsNullOrEmpty(strValue))
					{
						if (strValue.IndexOf(m_strFindWhat) != -1)
						{
							aCell.Selected = true;
							m_rowLastFound = aRow;
							m_cellLastFound = aCell;
							return;
						}
					}
				}
			}
		}

		protected static EncConverters GetEncConverters
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

		protected static DirectableEncConverter m_aECTransliterator = null;
		protected static DirectableEncConverter GetTransliterator
		{
			get
			{
				if (m_aECTransliterator == null)
				{
					DChartHelper.Properties.Settings.Default.Reload();
					string strECName = DChartHelper.Properties.Settings.Default.TransliterationECName;
					if (String.IsNullOrEmpty(strECName))
					{
						strECName = "Any to Latin";
						EncConverters aECs = GetEncConverters;
						if (aECs != null)
						{
							aECs.Add(strECName,
								"Any-Latin", ConvType.Unicode_to_from_Unicode,
								null, null, ProcessTypeFlags.ICUTransliteration);

							m_aECTransliterator = new DirectableEncConverter(strECName, true, NormalizeFlags.None);
						}
					}
					else
					{
						bool bDirection = DChartHelper.Properties.Settings.Default.TransliterationECDirection;
						NormalizeFlags formNorm = (NormalizeFlags)DChartHelper.Properties.Settings.Default.TransliterationECNormalize;
						m_aECTransliterator = new DirectableEncConverter(strECName, bDirection, formNorm);
					}

					if (m_aECTransliterator.GetEncConverter != null)
					{
						DChartHelper.Properties.Settings.Default.TransliterationECName = m_aECTransliterator.GetEncConverter.Name;
						DChartHelper.Properties.Settings.Default.TransliterationECDirection = m_aECTransliterator.GetEncConverter.DirectionForward;
						DChartHelper.Properties.Settings.Default.TransliterationECNormalize = (int)m_aECTransliterator.GetEncConverter.NormalizeOutput;
						DChartHelper.Properties.Settings.Default.Save();
					}
				}
				return m_aECTransliterator;
			}
		}

		protected static DirectableEncConverter m_aECMeaningLookup = null;
		protected static DirectableEncConverter GetMeaningLookupConverter
		{
			get
			{
				if (m_aECMeaningLookup == null)
				{
					DChartHelper.Properties.Settings.Default.Reload();
					string strECName = DChartHelper.Properties.Settings.Default.GlossingECName;
					if (!String.IsNullOrEmpty(strECName))
					{
						bool bDirection = DChartHelper.Properties.Settings.Default.GlossingECDirection;
						NormalizeFlags formNorm = (NormalizeFlags)DChartHelper.Properties.Settings.Default.GlossingECNormalize;
						m_aECMeaningLookup = new DirectableEncConverter(strECName, bDirection, formNorm);

						// see if it still exists
						if (m_aECMeaningLookup.GetEncConverter == null)
						{
							m_aECMeaningLookup = null;
							DChartHelper.Properties.Settings.Default.GlossingECName = null;
							DChartHelper.Properties.Settings.Default.Save();
						}
					}
				}
				return m_aECMeaningLookup;
			}
		}

		private void chooseSILConverterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				m_aECMeaningLookup = null;
				EncConverters aECs = GetEncConverters;
				if (aECs != null)
				{
					IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, "Select Converter for Glossing");
					m_aECMeaningLookup = new DirectableEncConverter(aIEC);
				}
				if ((m_aECMeaningLookup != null) && (m_aECMeaningLookup.GetEncConverter != null))
				{
					DChartHelper.Properties.Settings.Default.GlossingECName = m_aECMeaningLookup.GetEncConverter.Name;
					DChartHelper.Properties.Settings.Default.GlossingECDirection = m_aECMeaningLookup.GetEncConverter.DirectionForward;
					DChartHelper.Properties.Settings.Default.GlossingECNormalize = (int)m_aECMeaningLookup.GetEncConverter.NormalizeOutput;
				}
				else
					throw new Exception();
			}
			catch
			{
				DChartHelper.Properties.Settings.Default.GlossingECName = null;
				m_aECMeaningLookup = null;
			}
			finally
			{
				DChartHelper.Properties.Settings.Default.Save();
			}

			if (m_aECMeaningLookup != null)
			{
				if (this.showToolStripMenuItem.Checked)
					ShowTransAndGlossInfo();
				else
					this.showToolStripMenuItem.Checked = true;
			}
			else
				showToolStripMenuItem.Checked = false;
		}

		private void chooseTransSILConverterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				m_aECTransliterator = null;
				EncConverters aECs = GetEncConverters;
				if (aECs != null)
				{
					IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, "Select Transliterator");
					m_aECTransliterator = new DirectableEncConverter(aIEC);
				}

				if ((m_aECTransliterator != null) && (m_aECTransliterator.GetEncConverter != null))
				{
					DChartHelper.Properties.Settings.Default.TransliterationECName = m_aECTransliterator.GetEncConverter.Name;
					DChartHelper.Properties.Settings.Default.TransliterationECDirection = m_aECTransliterator.GetEncConverter.DirectionForward;
					DChartHelper.Properties.Settings.Default.TransliterationECNormalize = (int)m_aECTransliterator.GetEncConverter.NormalizeOutput;
				}
				else
					throw new Exception();
			}
			catch
			{
				DChartHelper.Properties.Settings.Default.TransliterationECName = null;
				m_aECTransliterator = null;
			}
			finally
			{
				DChartHelper.Properties.Settings.Default.Save();
			}

			if (m_aECTransliterator != null)
			{
				if (showTransToolStripMenuItem.Checked)
					ShowTransAndGlossInfo();
				else
					showTransToolStripMenuItem.Checked = true;
			}
			else
				showTransToolStripMenuItem.Checked = false;
		}

		private void showToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
		{
			if (m_aECMeaningLookup == null)
				chooseSILConverterToolStripMenuItem_Click(sender, e);

			if (m_aECMeaningLookup != null)
			{
				SetColumnRowSpan();
				ShowTransAndGlossInfo();
				Properties.Settings.Default.ShowGloss = this.showToolStripMenuItem.Checked;
				Properties.Settings.Default.Save();
			}
			else
				showToolStripMenuItem.Checked = false;
		}

		private void showTransToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
		{
			if (m_aECTransliterator == null)
				chooseTransSILConverterToolStripMenuItem_Click(sender, e);

			if (m_aECTransliterator != null)
			{
				SetColumnRowSpan();
				ShowTransAndGlossInfo();
				Properties.Settings.Default.ShowTransliteration = this.showTransToolStripMenuItem.Checked;
				Properties.Settings.Default.Save();
			}
			else
				showTransToolStripMenuItem.Checked = false;
		}

		private void reglossToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_bModified = true;
			foreach (DataGridViewRow aRow in this.dataGridViewChart.Rows)
			{
				if (aRow.Tag == null)
					continue;   // this could happen for the last (empty) 'add new row' row header

				GlossTranslations gts = (GlossTranslations)aRow.Tag;
				gts.GlossInfo = null;
				gts.TransInfo = null;
			}
			ShowTransAndGlossInfo();
		}

		protected void SetVernacularFontColor()
		{
			dataGridViewChart.RowsDefaultCellStyle.Font = m_fontVernacular;
			dataGridViewChart.RowsDefaultCellStyle.ForeColor = m_colorVernacular;
			int nHeight = m_fontVernacular.Height + CnExtraHeight;
			foreach (DataGridViewRow aRow in dataGridViewChart.Rows)
				aRow.Height = nHeight;
		}

		private void vernacularToolStripMenuItem_Click(object sender, EventArgs e)
		{
			fontDialog.Font = m_fontVernacular;
			fontDialog.Color = m_colorVernacular;
			if (fontDialog.ShowDialog() == DialogResult.OK)
			{
				m_fontVernacular = fontDialog.Font;
				m_colorVernacular = fontDialog.Color;

				SetVernacularFontColor();
			}
		}

		private void transliterationToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			fontDialog.Font = m_fontTransliteration;
			fontDialog.Color = m_colorTransliteration;
			if (fontDialog.ShowDialog() == DialogResult.OK)
			{
				m_fontTransliteration = fontDialog.Font;
				m_colorTransliteration = fontDialog.Color;
				ShowTransAndGlossInfo();
			}
		}

		private void glossingToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			fontDialog.Font = m_fontGloss;
			fontDialog.Color = m_colorGloss;
			if (fontDialog.ShowDialog() == DialogResult.OK)
			{
				m_fontGloss = fontDialog.Font;
				m_colorGloss = fontDialog.Color;
				ShowTransAndGlossInfo();
			}
		}
/*
			// paste from clipboard
			IDataObject iData = Clipboard.GetDataObject();

			// Determines whether the data is in a format you can use.
			if (iData.GetDataPresent(DataFormats.Html))
			{
				string strData = (string)iData.GetData(DataFormats.Html);
				File.WriteAllText(@"C:\SrcSubversion\Src\DChartHelper\HTMLPaste.htm", strData);
			}
*/
		private void dataGridViewChart_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			if ((e.RowIndex >= 0) && (e.RowIndex < dataGridViewChart.Rows.Count))
				dataGridViewChart.Rows[e.RowIndex].HeaderCell.ToolTipText = "Right-click to add/edit a free translation";
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DoCopy();
		}

		private void openReadmeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string strAppPath = Application.ExecutablePath;
			string strHelpFilename = strAppPath.Substring(0, strAppPath.LastIndexOf('\\')) + @"\Discourse Chart Helper Readme.pdf";
			LaunchProgram(strHelpFilename, "");
		}

		internal static void LaunchProgram(string strProgram, string strArguments)
		{
			try
			{
				Process myProcess = new Process();

				myProcess.StartInfo.FileName = strProgram;
				myProcess.StartInfo.Arguments = strArguments;
				myProcess.Start();
			}
			catch (Exception ex)
			{
				if (ex.Message == "The system cannot find the file specified")
					MessageBox.Show("The help file isn't there! You should reinstall the product. (some Context Sensitive Help is also available by selecting a control in the window and pressing F1)", cstrCaption);
				else
					MessageBox.Show("Can't launch a viewer for the PDF Readme file! (see http://www.adobe.com/go/gntray_dl_get_reader; some Context Sensitive Help is also available by selecting a control in the window and pressing F1)", cstrCaption);
			}
		}

		private void adjustAllColumnWidthsToFitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DataGridViewAutoSizeColumnMode theSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			if (this.adjustAllColumnWidthsToFitToolStripMenuItem.Checked)
				theSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

			foreach (DataGridViewColumn aColumn in dataGridViewChart.Columns)
				if (!aColumn.Frozen)
					aColumn.AutoSizeMode = theSizeMode;
			foreach (DataGridViewColumn aColumn in dataGridViewGloss.Columns)
				if (!aColumn.Frozen)
					aColumn.AutoSizeMode = theSizeMode;
		}

		private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			recentFileToolStripMenuItem.DropDownItems.Clear();
			recentFileToolStripMenuItem.Enabled = (Properties.Settings.Default.RecentFiles.Count > 0);
			foreach (string strRecentFile in Properties.Settings.Default.RecentFiles)
				recentFileToolStripMenuItem.DropDownItems.Add(strRecentFile, null, recentFilesToolStripMenuItem_Click);

			recentChartsToolStripMenuItem.DropDownItems.Clear();
			recentChartsToolStripMenuItem.Enabled = (Properties.Settings.Default.RecentCharts.Count > 0);
			foreach (string strRecentChart in Properties.Settings.Default.RecentCharts)
				recentChartsToolStripMenuItem.DropDownItems.Add(strRecentChart, null, recentChartsToolStripMenuItem_Click);
		}

		void recentFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripDropDownItem aRecentFile = (ToolStripDropDownItem)sender;
			try
			{
				if (!CheckForModified())
					return;

				OpenFile(aRecentFile.Text);
			}
			catch (Exception ex)
			{
				// probably means the file doesn't exist anymore, so remove it from the recent used list
				Properties.Settings.Default.RecentFiles.Remove(aRecentFile.Text);
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		void recentChartsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripDropDownItem aRecentFile = (ToolStripDropDownItem)sender;
			try
			{
				if (!CheckForModified())
					return;

				OpenSavedGrid(aRecentFile.Text);
			}
			catch (Exception ex)
			{
				// probably means the file doesn't exist anymore, so remove it from the recent used list
				Properties.Settings.Default.RecentCharts.Remove(aRecentFile.Text);
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		private void editCellToolStripMenuItem_Click(object sender, EventArgs e)
		{
			dataGridViewChart.BeginEdit(true);
		}

		private void DiscourseChartForm_ResizeEnd(object sender, EventArgs e)
		{
			Properties.Settings.Default.FormWidth = Bounds.Width;
			Properties.Settings.Default.FormHeight = Bounds.Height;
			Properties.Settings.Default.PointX = Bounds.Location.X;
			Properties.Settings.Default.PointY = Bounds.Location.Y;
			Properties.Settings.Default.Save();
		}
	}

	public class GlossTranslations
	{
		public string Reference { get; set; }
		public string FreeTranslation { get; set; }
		public GlossTranslationInfo GlossInfo { get; set; }
		public GlossTranslationInfo TransInfo { get; set; }
	}

	public class GlossTranslationInfo
	{
		private string m_strPreS = null;
		private string m_strSubject = null;
		private string m_strX1 = null;
		private string m_strX2 = null;
		private string m_strVerb = null;
		private string m_strPostS = null;

		public string PreS
		{
			get { return m_strPreS; }
			set { m_strPreS = value; }
		}

		public string Subject
		{
			get { return m_strSubject; }
			set { m_strSubject = value; }
		}

		public string X1
		{
			get { return m_strX1; }
			set { m_strX1 = value; }
		}

		public string X2
		{
			get { return m_strX2; }
			set { m_strX2 = value; }
		}

		public string Verb
		{
			get { return m_strVerb; }
			set { m_strVerb = value; }
		}

		public string PostS
		{
			get { return m_strPostS; }
			set { m_strPostS = value; }
		}
	}
}