// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// This dialog provides find capability without any attempt to be fancy. All the work must be done by the caller in events fired by the dialog.
	/// </summary>
	public partial class BasicFindDialog : Form, IBasicFindView
	{
		/// <summary/>
		public delegate void FindNextDelegate(object sender, IBasicFindView view);

		/// <summary/>
		public event FindNextDelegate FindNext;

		/// <summary>
		/// Basic constructor (for the designer)
		/// </summary>
		public BasicFindDialog()
		{
			InitializeComponent();
		}

		private void _findNext_Click(object sender, EventArgs e)
		{
			if(FindNext != null)
				FindNext(this, this);
		}

		/// <summary>
		/// Any search status updates that we want to display back to the user
		/// </summary>
		public string StatusText
		{
			get { return _notificationLabel.Text; }
			set { _notificationLabel.Text = value; }
		}

		/// <summary>
		/// The text string the user asked us to search for
		/// </summary>
		public string SearchText { get { return _searchTextbox.Text; } }

		private void _searchTextbox_TextChanged(object sender, EventArgs e)
		{
			_findNext.Enabled = !string.IsNullOrEmpty(_searchTextbox.Text);
		}

		/// <summary>
		/// Let the Enter and F3 keys perform a search if the search button is active
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _searchTextbox_KeyDown(object sender, KeyEventArgs e)
		{
			if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3) && _findNext.Enabled)
			{
				_findNext_Click(this, EventArgs.Empty);
				e.SuppressKeyPress = true;
			}
		}
	}

	/// <summary/>
	public interface IBasicFindView
	{
		/// <summary>
		/// Text to display to the user in the dialog
		/// </summary>
		string StatusText { get; set; }

		/// <summary>
		/// The search box contents
		/// </summary>
		string SearchText { get; }
	}
}
