// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LCMBrowser
{
	/// <summary>
	/// The RealListChooser class is just an implementation of a ListBox.
	/// </summary>
	public partial class RealListChooser : Form
	{
		/// <summary>
		/// This name of the selected class.
		/// </summary>
		public string m_chosenClass = string.Empty;

		/// <summary />
		/// <param name="name">The name of the listBox.</param>
		/// <param name="list">The list of strings to be displayed inb the listBox.</param>
		public RealListChooser(string name, List<string> list)
		{
			InitializeComponent();
			listBox.DataSource = list;
			listBox.Text = name;
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			m_chosenClass = listBox.SelectedItem.ToString();
			// Make an object of class newClassName and put it somewhere.
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			m_chosenClass = "Cancel";
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}