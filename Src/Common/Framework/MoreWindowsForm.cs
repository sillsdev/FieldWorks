// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MoreWindowsForm.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Summary description for MoreWindowsForm.
	/// </summary>
	public class MoreWindowsForm : Form, IFWDisposable
	{
		/// <summary></summary>
		protected System.Windows.Forms.Button btnSwitchTo;
		/// <summary></summary>
		protected System.Windows.Forms.Button btnCancel;
		/// <summary></summary>
		protected System.Windows.Forms.ListBox lstWindows;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the items in the ListBox
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ListBox List
		{
			get
			{
				CheckDisposed();
				return this.lstWindows;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the switch to button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button SwitchTo
		{
			get
			{
				CheckDisposed();
				return this.btnSwitchTo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the cancel button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button Cancel
		{
			get
			{
				CheckDisposed();
				return this.btnCancel;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the selected main window. (This value is the index of the window
		/// in the applications list of windows, which is not necessarily the index of windows
		/// in the "More Windows" list box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectedWindow
		{
			get
			{
				CheckDisposed();

				if (this.DialogResult != DialogResult.OK)
					return -1;
				else
					return lstWindows.Items.Count - lstWindows.SelectedIndex - 1;
			}
		}

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MoreWindowsForm"/> class.
		/// </summary>
		/// <param name="windows">The array of open main windows that comes from the FwApp
		/// object. Windows are added to the list in the reverse order in which they were
		/// opened.</param>
		/// ------------------------------------------------------------------------------------
		public MoreWindowsForm(List<IFwMainWnd> windows)
		{
			InitializeComponent();

			for (int i = windows.Count - 1; i >= 0; i--)
				lstWindows.Items.Add(((FwMainWnd)windows[i]).InformationBarText);

			if (lstWindows.SelectedIndex == -1 && lstWindows.Items.Count > 0)
				lstWindows.SelectedIndex = 0;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MoreWindowsForm));
			this.lstWindows = new System.Windows.Forms.ListBox();
			this.btnSwitchTo = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lstWindows
			//
			resources.ApplyResources(this.lstWindows, "lstWindows");
			this.lstWindows.Name = "lstWindows";
			this.lstWindows.DoubleClick += new System.EventHandler(this.SelectionMade);
			//
			// btnSwitchTo
			//
			resources.ApplyResources(this.btnSwitchTo, "btnSwitchTo");
			this.btnSwitchTo.Name = "btnSwitchTo";
			this.btnSwitchTo.Click += new System.EventHandler(this.SelectionMade);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// MoreWindowsForm
			//
			this.AcceptButton = this.btnSwitchTo;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnSwitchTo);
			this.Controls.Add(this.lstWindows);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MoreWindowsForm";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void SelectionMade(object sender, System.EventArgs e)
		{
			if (lstWindows.SelectedIndex > -1)
			{
				this.DialogResult = DialogResult.OK;
				Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
