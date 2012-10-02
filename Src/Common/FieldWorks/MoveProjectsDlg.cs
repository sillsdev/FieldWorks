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
// File: MoveProjectsDlg.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class MoveProjectsDlg : Form
	{
		IHelpTopicProvider m_helpTopicProvider;
		private HelpProvider m_helpProvider;
		private string m_helpTopic = "khtpMoveProjectDlgNewLocation";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MoveProjectsDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MoveProjectsDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			m_helpTopicProvider = helpTopicProvider;
			InitHelp();
		}

		private void m_btnYes_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Yes;
			Close();
		}

		private void m_btnNo_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.No;
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		private void InitHelp()
		{
			// Only enable the Help button if we have a help topic for the fieldName
			if (m_helpTopicProvider != null)
			{
				string keyword;
				m_btnHelp.Enabled = HelpTopicIsValid(m_helpTopic, out keyword);
				if (m_btnHelp.Enabled)
				{
					if (m_helpProvider == null)
						m_helpProvider = new HelpProvider();
					m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
					m_helpProvider.SetHelpKeyword(this, keyword);
					m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
				}
			}
		}

		private bool HelpTopicIsValid(string helpTopic, out string keyword)
		{
			keyword = m_helpTopicProvider.GetHelpString(helpTopic);
			return keyword != null;
		}
	}
}