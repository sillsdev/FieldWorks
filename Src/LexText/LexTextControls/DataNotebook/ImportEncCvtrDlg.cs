// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportEncCvtrDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SilEncConverters31;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for choosing an encoding converter for a given writing system.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ImportEncCvtrDlg : Form
	{
		private string m_sBlankEC = Sfm2Xml.STATICS.AlreadyInUnicode;
		private string m_sDescriptionFmt;
		private string m_sEncConverter;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ImportEncCvtrDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportEncCvtrDlg()
		{
			InitializeComponent();

			m_sDescriptionFmt = m_lblDescription.Text;
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
		}

		public void Initialize(string wsName, string encConverter, IHelpTopicProvider helpTopicProvider,
			IApp app)
		{
			m_lblDescription.Text = String.Format(m_sDescriptionFmt, wsName);
			m_sEncConverter = encConverter;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			LoadEncodingConverters();
			int index = 0;
			if (m_sEncConverter != null && m_sEncConverter != "")
			{
				index = m_cbEC.FindStringExact(m_sEncConverter);
				if (index < 0)
					index = 0;
			}
			m_cbEC.SelectedIndex = index;
		}

		private void m_btnAddEC_Click(object sender, EventArgs e)
		{
			try
			{
				string prevEC = m_cbEC.Text;
				using (AddCnvtrDlg dlg = new AddCnvtrDlg(m_helpTopicProvider, m_app, null,
					m_cbEC.Text, null, false))
				{
					dlg.ShowDialog(this);

					// Reload the converter list in the combo to reflect the changes.
					LoadEncodingConverters();

					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !String.IsNullOrEmpty(dlg.SelectedConverter))
						m_cbEC.SelectedItem = dlg.SelectedConverter;
					else if (m_cbEC.Items.Count > 0)
						m_cbEC.SelectedItem = prevEC; // preserve selection if possible
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, String.Format(LexTextControls.ksErrorAcessingEncodingConverters, ex.Message));
				return;
			}
		}

		private void LoadEncodingConverters()
		{
			/// Added to make the list of encoding converters match the list that is given when
			/// the add new converter option is selected. (LT-2955)
			EncConverters encConv = new EncConverters();
			System.Collections.IDictionaryEnumerator de = encConv.GetEnumerator();
			m_cbEC.BeginUpdate();
			m_cbEC.Items.Clear();
			m_cbEC.Sorted = true;
			while (de.MoveNext())
			{
				string name = de.Key as string;
				if (name != null)
					m_cbEC.Items.Add(name);
			}
			m_cbEC.Sorted = false;
			m_cbEC.Items.Insert(0, m_sBlankEC);
			m_cbEC.EndUpdate();
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			SIL.FieldWorks.Common.FwUtils.ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportWizStep3");
		}

		public string EncodingConverter
		{
			get { return m_cbEC.SelectedItem.ToString(); }
		}
	}
}