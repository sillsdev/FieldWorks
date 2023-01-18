// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.SfmToXml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SilEncConverters40;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary>
	/// Dialog for choosing an encoding converter for a given writing system.
	/// </summary>
	public partial class ImportEncCvtrDlg : Form
	{
		private string m_sBlankEC = SfmToXmlServices.AlreadyInUnicode;
		private string m_sDescriptionFmt;
		private string m_sEncConverter;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;

		/// <summary />
		public ImportEncCvtrDlg()
		{
			InitializeComponent();

			m_sDescriptionFmt = m_lblDescription.Text;
		}

		public void Initialize(string wsName, string encConverter, IHelpTopicProvider helpTopicProvider, IApp app)
		{
			m_lblDescription.Text = string.Format(m_sDescriptionFmt, wsName);
			m_sEncConverter = encConverter;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			LoadEncodingConverters();
			var index = 0;
			if (!string.IsNullOrEmpty(m_sEncConverter))
			{
				index = m_cbEC.FindStringExact(m_sEncConverter);
				if (index < 0)
				{
					index = 0;
				}
			}
			m_cbEC.SelectedIndex = index;
		}

		private void m_btnAddEC_Click(object sender, EventArgs e)
		{
			try
			{
				var prevEC = m_cbEC.Text;
				using (var dlg = new AddCnvtrDlg(m_helpTopicProvider, m_app, null, m_cbEC.Text, null, false))
				{
					dlg.ShowDialog(this);

					// Reload the converter list in the combo to reflect the changes.
					LoadEncodingConverters();

					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedConverter))
					{
						m_cbEC.SelectedItem = dlg.SelectedConverter;
					}
					else if (m_cbEC.Items.Count > 0)
					{
						m_cbEC.SelectedItem = prevEC; // preserve selection if possible
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, string.Format(LanguageExplorerControls.ksErrorAccessingEncodingConverters, ex.Message));
			}
		}

		private void LoadEncodingConverters()
		{
			// Added to make the list of encoding converters match the list that is given when
			// the add new converter option is selected. (LT-2955)
			var encConv = new EncConverters();
			var de = encConv.GetEnumerator();
			m_cbEC.BeginUpdate();
			m_cbEC.Items.Clear();
			m_cbEC.Sorted = true;
			while (de.MoveNext())
			{
				if (de.Key is string name)
				{
					m_cbEC.Items.Add(name);
				}
			}
			m_cbEC.Sorted = false;
			m_cbEC.Items.Insert(0, m_sBlankEC);
			m_cbEC.EndUpdate();
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportWizStep3");
		}

		public string EncodingConverter => m_cbEC.SelectedItem.ToString();
	}
}