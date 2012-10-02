using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace SILConvertersWordML
{
	public partial class FilesToOpenListDlg : Form
	{
		protected const int cnFileSelectedColumn = 0;
		protected const int cnFilenameColumn = 1;

		protected string[] m_astrFileNamesToOpen = null;
		protected string m_strFilenameExistPrefix = null;
		protected string m_strFilenameNewPrefix = null;
		protected Dictionary<string, DocXmlDocument> m_mapDocName2XmlDocument = new Dictionary<string, DocXmlDocument>();

		public FilesToOpenListDlg(ref Dictionary<string, DocXmlDocument> mapDocName2XmlDocument, List<FileInfo> afiFilesToList, string strFilenameExistPrefix, string strFilenameNewPrefix)
		{
			InitializeComponent();

			m_strFilenameExistPrefix = strFilenameExistPrefix;
			m_strFilenameNewPrefix = strFilenameNewPrefix;
			m_mapDocName2XmlDocument = mapDocName2XmlDocument;

			foreach (FileInfo fiFileToList in afiFilesToList)
			{
				object[] obs = new object[2] { true, m_strFilenameNewPrefix + fiFileToList.FullName.Substring(m_strFilenameExistPrefix.Length) };
				dataGridViewFilesList.Rows.Add(obs);
			}
		}

		public string[] FilesToOpen
		{
			get { return m_astrFileNamesToOpen; }
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

		private void FilesToOpenListDlg_FormClosing(object sender, FormClosingEventArgs e)
		{
			int nCount = 0;
			foreach (DataGridViewRow aRow in dataGridViewFilesList.Rows)
				if ((bool)aRow.Cells[cnFileSelectedColumn].Value)
					nCount++;

			if (nCount > 0)
			{
				m_astrFileNamesToOpen = new string[nCount];
				nCount = 0;
				foreach (DataGridViewRow aRow in dataGridViewFilesList.Rows)
				{
					string strFilename = m_strFilenameExistPrefix + aRow.Cells[cnFilenameColumn].Value.ToString().Substring(m_strFilenameNewPrefix.Length);
					if ((bool)aRow.Cells[cnFileSelectedColumn].Value)
						m_astrFileNamesToOpen[nCount++] = strFilename;
					else
						m_mapDocName2XmlDocument.Remove(strFilename);
				}
			}
			else
				DialogResult = DialogResult.Cancel;
		}
	}
}