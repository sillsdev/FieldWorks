using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.IText
{
	public partial class AddWordsToLexiconDlg : Form
	{

		private const string s_helpTopic = "kshtpAddWordsToLexicon";

		public AddWordsToLexiconDlg()
		{
			InitializeComponent();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// indicates the status of the dialog checkbox which indicates
		/// whether the user wants to add words to the lexicon.
		/// </summary>
		internal bool UserWantsToAddWordsToLexicon
		{
			get { return checkBox1.Checked; }
			set { checkBox1.Checked = value; }
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}
	}
}