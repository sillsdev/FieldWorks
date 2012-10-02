using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters31;
using Microsoft.Win32;                  // for RegistryKey

namespace SILConvertersWordML
{
	public partial class AutoSearchOptions : Form
	{
		const int cnFontColumn = 0;
		const string cstrDefaultFilter = "*.doc;*.rtf";
		const string cstrFilterAdd2007 = "*.docx";
		const string cstrDocxExtension = ".docx";

		protected string m_strSearchPath = null;
		protected string m_strStorePath = null;
		protected string m_strSearchFilter = null;
		protected bool m_bConvertBackupFiles = false;
		protected bool m_bNeed2007 = false;

		protected List<string> m_astrFontsToSearchFor = new List<string>();
		protected List<string> m_astrSearchFilters = new List<string>();

		public AutoSearchOptions()
		{
			InitializeComponent();

			if (!String.IsNullOrEmpty(Properties.Settings.Default.LastSearchFolderUsed))
				textBoxSearchStart.Text = Properties.Settings.Default.LastSearchFolderUsed;
			else
				textBoxSearchStart.Text = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

			textBoxStoreResults.Text =
#if DEBUG
				@"C:\Trashbin";
#else
				@"C:\Backup";
#endif
			textBoxSearchFilter.Text = cstrDefaultFilter;
			RegistryKey keyDocx = Registry.ClassesRoot.OpenSubKey(cstrDocxExtension, false);
			if (keyDocx != null)
				textBoxSearchFilter.Text += ';' + cstrFilterAdd2007;
			else
				m_bNeed2007 = true; // this indicates that 2007 isn't installed, so if the user adds the .docx, we tell her it's needed.

			InstalledFontCollection installedFontCollection = new InstalledFontCollection();

			// Get the array of FontFamily objects.
			ColumnFont.Items.Add(""); // make the first one null, so users can cancel one (I can't figure out how to actually delete the row)
			foreach (FontFamily ff in installedFontCollection.Families)
				ColumnFont.Items.Add(ff.Name);
		}

		public string SearchPath
		{
			get { return m_strSearchPath; }
			set { m_strSearchPath = value; }
		}

		public string StorePath
		{
			get { return m_strStorePath; }
			set { m_strStorePath = value; }
		}

		public bool ConvertBackupFiles
		{
			get { return m_bConvertBackupFiles; }
			set { m_bConvertBackupFiles = value; }
		}

		public void SetSearchFilters(string strSearchFilterString)
		{
			m_astrSearchFilters.Clear();
			string[] astrFilters = strSearchFilterString.Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string strFilter in astrFilters)
				m_astrSearchFilters.Add(strFilter);
		}

		public List<string> SearchFilters
		{
			get { return m_astrSearchFilters; }
		}

		public List<string> FontsToSearchFor
		{
			get { return m_astrFontsToSearchFor; }
		}

		private void buttonBrowseStartSearch_Click(object sender, EventArgs e)
		{
			folderBrowserDialogAutoFind.SelectedPath = textBoxSearchStart.Text;
			if (folderBrowserDialogAutoFind.ShowDialog() == DialogResult.OK)
				textBoxSearchStart.Text = folderBrowserDialogAutoFind.SelectedPath;
		}

		private void buttonBrowseStoreResults_Click(object sender, EventArgs e)
		{
			folderBrowserDialogAutoFind.SelectedPath = textBoxStoreResults.Text;
			if (folderBrowserDialogAutoFind.ShowDialog() == DialogResult.OK)
				textBoxStoreResults.Text = folderBrowserDialogAutoFind.SelectedPath;
		}

		private void AutoSearchOptions_FormClosing(object sender, FormClosingEventArgs e)
		{
			// only do our checking if the user means to begin the search
			if (DialogResult != DialogResult.OK)
				return;

			string strFilters = textBoxSearchFilter.Text.ToLower();
			if (m_bNeed2007 && (strFilters.IndexOf(cstrDocxExtension) != -1))
			{
				MessageBox.Show("It doesn't appear that either Office/Word 2007 or the Compatibility Pack for 2007 file formats is installed. Without one of these, this program will not be able to process *.docx files. You can search www.microsoft.com/downloads for \"FileFormatConverters\" and install those to be able to work with .docx files within only Office/Word 2003 installed.", FontsStylesForm.cstrCaption);
			}

			SetSearchFilters(strFilters);

			SearchPath = textBoxSearchStart.Text;
			Properties.Settings.Default.LastSearchFolderUsed = SearchPath;
			Properties.Settings.Default.Save();

			StorePath = textBoxStoreResults.Text;
			ConvertBackupFiles = checkBoxConvertBackedupFiles.Checked;

			if (SearchPath.ToLower() == StorePath.ToLower())
			{
				MessageBox.Show("You can't have the Backup path be the same as the search path", FontsStylesForm.cstrCaption);
				e.Cancel = true;
				return;
			}

			if (StorePath.ToLower().IndexOf(SearchPath.ToLower()) != -1)
			{
				MessageBox.Show("You can't have the Backup path be somewhere within the search path", FontsStylesForm.cstrCaption);
				e.Cancel = true;
				return;
			}

			if (SearchFilters.Count <= 0)
				MessageBox.Show(String.Format("You can't have a null search filter. the default one (i.e. {0}) will be used instead", cstrDefaultFilter), FontsStylesForm.cstrCaption);

			// get the fonts to search for
			foreach (DataGridViewRow aRow in dataGridViewFonts.Rows)
			{
				string strFontName = (string)aRow.Cells[cnFontColumn].Value;
				if (!String.IsNullOrEmpty(strFontName))
					m_astrFontsToSearchFor.Add(strFontName);
			}

			if (m_astrFontsToSearchFor.Count <= 0)
			{
				MessageBox.Show("You first have to select some fonts to search for", FontsStylesForm.cstrCaption);
				e.Cancel = true;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}
	}
}