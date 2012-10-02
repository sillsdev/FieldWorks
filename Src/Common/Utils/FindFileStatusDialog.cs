// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FindFileStatusDialog.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.Utils
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// FindFileStatusDialog provides a dialog box for the FindFile method in DriveUtil.
	/// It provides the caller of FindFile the means to display the current path being
	/// searched during a file (or file pattern) search operation. It also provides a
	/// means via the 'Cancel' button, for the end user to abort the search operation.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class FindFileStatusDialog : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label lblSearching;
		private System.Windows.Forms.Label lblCurrentFolder;
		private System.Windows.Forms.Button btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FindFileStatusDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindFileStatusDialog));
			this.lblSearching = new System.Windows.Forms.Label();
			this.lblCurrentFolder = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lblSearching
			//
			resources.ApplyResources(this.lblSearching, "lblSearching");
			this.lblSearching.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblSearching.Name = "lblSearching";
			//
			// lblCurrentFolder
			//
			this.lblCurrentFolder.FlatStyle = System.Windows.Forms.FlatStyle.System;
			resources.ApplyResources(this.lblCurrentFolder, "lblCurrentFolder");
			this.lblCurrentFolder.Name = "lblCurrentFolder";
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// FindFileStatusDialog
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.lblSearching);
			this.Controls.Add(this.lblCurrentFolder);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FindFileStatusDialog";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Cancel button's click event.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the file name and it's path that's displayed in the dialog.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public string FullFileName
		{
			get {return lblCurrentFolder.Text;}
			set
			{
				if (lblCurrentFolder.Text != value)
					lblCurrentFolder.Text = value;
			}
		}
	}
}
