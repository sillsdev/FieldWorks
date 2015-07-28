// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal partial class ProjectExistsForm : Form
	{
		private ProjectExistsForm()
		{
			InitializeComponent();

			Icon = System.Drawing.SystemIcons.Warning;
		}

		public ProjectExistsForm(string projectName) : this()
		{
			label1.Text = string.Format(Strings.ksProjectExistsText1, projectName) + Environment.NewLine + Strings.ksProjectExistsText2;
			DialogResult = DialogResult.Cancel;
		}

		private void btnOverwrite_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnRename_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
