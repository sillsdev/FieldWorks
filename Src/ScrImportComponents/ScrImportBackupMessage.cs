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
// File: ScrImportBackupMessage.cs
// Responsibility: TeTeam
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
using System.Resources;
using System.Diagnostics;

using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.ScrImportComponents
{
	/// <summary>
	/// Dialog to encourage user to back up before importing.
	/// </summary>
	public class ScrImportBackupMessage : Form, IFWDisposable
	{
		#region Data Members

		/// <summary></summary>
		protected System.Windows.Forms.CheckBox chkDontBugMe;
		private System.Windows.Forms.ImageList icons;
		/// <summary></summary>
		protected Button btnBackup;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button btnImport;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button btnCancel;
		private System.ComponentModel.IContainer components;
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrImportBackupMessage"/> class. No
		/// help file.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrImportBackupMessage()
		{
			InitializeComponent();
			btnBackup.Focus();
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Label lblMsg;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScrImportBackupMessage));
			System.Windows.Forms.PictureBox icon;
			this.btnImport = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.icons = new System.Windows.Forms.ImageList(this.components);
			this.btnBackup = new System.Windows.Forms.Button();
			this.chkDontBugMe = new System.Windows.Forms.CheckBox();
			lblMsg = new System.Windows.Forms.Label();
			icon = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(icon)).BeginInit();
			this.SuspendLayout();
			//
			// lblMsg
			//
			resources.ApplyResources(lblMsg, "lblMsg");
			lblMsg.Name = "lblMsg";
			//
			// btnImport
			//
			this.btnImport.DialogResult = System.Windows.Forms.DialogResult.No;
			resources.ApplyResources(this.btnImport, "btnImport");
			this.btnImport.Name = "btnImport";
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			//
			// icon
			//
			resources.ApplyResources(icon, "icon");
			icon.Name = "icon";
			icon.TabStop = false;
			//
			// icons
			//
			this.icons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("icons.ImageStream")));
			this.icons.TransparentColor = System.Drawing.Color.Transparent;
			this.icons.Images.SetKeyName(0, "");
			//
			// btnBackup
			//
			this.btnBackup.DialogResult = System.Windows.Forms.DialogResult.Yes;
			resources.ApplyResources(this.btnBackup, "btnBackup");
			this.btnBackup.Name = "btnBackup";
			//
			// chkDontBugMe
			//
			resources.ApplyResources(this.chkDontBugMe, "chkDontBugMe");
			this.chkDontBugMe.Name = "chkDontBugMe";
			//
			// ScrImportBackupMessage
			//
			this.AcceptButton = this.btnBackup;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.chkDontBugMe);
			this.Controls.Add(this.btnBackup);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnImport);
			this.Controls.Add(lblMsg);
			this.Controls.Add(icon);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ScrImportBackupMessage";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(icon)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Painting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint an etched line to separate main controls from OK, Cancel, and Help buttons.
		/// </summary>
		/// <param name="e">Paint Event arguments</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the etched, horizontal line separating the buttons from the stuff above
			// them.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle,
				btnBackup.Bounds);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets if the don't bug me box is checked
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DontBugMeChecked
		{
			get
			{
				CheckDisposed();
				return chkDontBugMe.Checked;
			}
		}
		#endregion
	}
}
