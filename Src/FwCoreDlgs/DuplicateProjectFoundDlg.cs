// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DuplicateProjectFoundDlg.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog that is shown if user tries to create a new project but a project with that name
	/// already exists
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DuplicateProjectFoundDlg : Form, IFWDisposable
	{
		#region Member variables
		#endregion

		#region Construction, Initialization and Deconstruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DuplicateProjectFoundDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DuplicateProjectFoundDlg()
		{
			AccessibleName = GetType().Name;
			Logger.WriteEvent("Opening duplicate project found dialog");
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if(disposing)
			{
			}
			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button btnOpen;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DuplicateProjectFoundDlg));
			System.Windows.Forms.Button btnCancel;
			System.Windows.Forms.Label lbl_selectText;
			System.Windows.Forms.Label lbl_openExisting;
			System.Windows.Forms.Label lbl_ChangeName;
			btnOpen = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			lbl_selectText = new System.Windows.Forms.Label();
			lbl_openExisting = new System.Windows.Forms.Label();
			lbl_ChangeName = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// btnOpen
			//
			resources.ApplyResources(btnOpen, "btnOpen");
			btnOpen.Name = "btnOpen";
			btnOpen.Click += new System.EventHandler(this.OnOpenClick);
			//
			// btnCancel
			//
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.Name = "btnCancel";
			//
			// lbl_selectText
			//
			resources.ApplyResources(lbl_selectText, "lbl_selectText");
			lbl_selectText.Name = "lbl_selectText";
			//
			// lbl_openExisting
			//
			resources.ApplyResources(lbl_openExisting, "lbl_openExisting");
			lbl_openExisting.Name = "lbl_openExisting";
			//
			// lbl_ChangeName
			//
			resources.ApplyResources(lbl_ChangeName, "lbl_ChangeName");
			lbl_ChangeName.Name = "lbl_ChangeName";
			//
			// DuplicateProjectFoundDlg
			//
			this.AcceptButton = btnCancel;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(lbl_ChangeName);
			this.Controls.Add(lbl_openExisting);
			this.Controls.Add(lbl_selectText);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOpen);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DuplicateProjectFoundDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Button click handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the event when the user clicks on the Open button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnOpenClick(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}
		#endregion

		#region Overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Log the closing of the dialog.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent(string.Format("Closing 'duplicate project found' dialog with result {0}",
				DialogResult));
			base.OnClosing (e);
		}

		#endregion
	}
}
