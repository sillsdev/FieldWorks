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
