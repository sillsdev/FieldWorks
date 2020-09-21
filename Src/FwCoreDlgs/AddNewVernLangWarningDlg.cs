// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Warning to scare users away from adding multiple vernacular languages.
	/// Usually you want multiple writing systems of the same vernacular language
	/// </summary>
	public partial class AddNewVernLangWarningDlg : Form
	{
		private IHelpTopicProvider _helpTopicProvider;

		/// <summary/>
		public AddNewVernLangWarningDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			_helpTopicProvider = helpTopicProvider;
			warningIconBox.BackgroundImageLayout = ImageLayout.Center;
			warningIconBox.BackgroundImage = System.Drawing.SystemIcons.Warning.ToBitmap();
		}

		private void _yesButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Yes;
			Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{

			DialogResult = DialogResult.No;
			Close();
		}

		private void _helpBtn_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(_helpTopicProvider, "khtpAddAnotherVernacular");
		}
	}
}
