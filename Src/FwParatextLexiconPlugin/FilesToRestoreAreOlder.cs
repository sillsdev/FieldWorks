// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	///
	/// </summary>
	public partial class FilesToRestoreAreOlder : Form
	{
		/// <summary>
		///
		/// </summary>
		public FilesToRestoreAreOlder()
		{
			InitializeComponent();
		}

		/// <summary>
		///
		/// </summary>
		public bool fKeepFilesThatAreNewer
		{
			get { return radio_Keep.Checked; }
		}

		/// <summary>
		///
		/// </summary>
		public bool fOverWriteThatAreNewer
		{
			get { return radio_Overwrite.Checked; }
		}

		private void button_OK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void button_Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void button_Help_Click(object sender, EventArgs e)
		{
		}
	}
}
