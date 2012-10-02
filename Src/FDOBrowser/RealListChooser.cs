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
// File: RealListChooser.cs
// Responsibility:
//
// <remarks>
// This implements a control during the dialog.
// The control is a listbox that lists the whatever is passsed into it.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace FDOBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The RealListChooser class is just an implementation of a ListBox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class RealListChooser : Form
	{
		/// <summary>
		/// This name of the selected class.
		/// </summary>
		public string m_chosenClass = "";

		/// ----------------------------------------------------------------------------------------		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RealListChooser"/> class.
		/// </summary>
		/// <param name="name">The name of the listBox.</param>
		/// <param name="list">The list of strings to be displayed inb the listBox.</param>
		/// ------------------------------------------------------------------------------------
		public RealListChooser(string name, List<string> list)
		{
			InitializeComponent();
			listBox.DataSource = list;
			listBox.Text = name;

			this.ShowDialog();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			m_chosenClass = listBox.SelectedItem.ToString();
			// Make an object of class newClassName and put it somewhere.
			this.Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			m_chosenClass = "Cancel";
			this.Close();
		}
	}
}
