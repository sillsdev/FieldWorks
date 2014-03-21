using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal partial class ProjectExistsForm : Form
	{
		private const string labelText1 = "The {0} project already exists.";
		private const string labelText2 = "You may overwrite the existing project or select a different name.";

		private ProjectExistsForm()
		{
			InitializeComponent();

			Icon = System.Drawing.SystemIcons.Warning;
		}

		public ProjectExistsForm(string projectName) : this()
		{
			label1.Text = string.Format(labelText1, projectName) + Environment.NewLine + labelText2;
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
