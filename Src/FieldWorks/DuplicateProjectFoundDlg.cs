// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using System.Windows.Forms;
using SIL.Reporting;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Dialog that is shown if user tries to create a new project but a project with that name
	/// already exists
	/// </summary>
	internal sealed class DuplicateProjectFoundDlg : Form
	{
		#region Construction, Initialization and Deconstruction

		/// <summary />
		public DuplicateProjectFoundDlg()
		{
			AccessibleName = GetType().Name;
			Logger.WriteEvent("Opening duplicate project found dialog");
			InitializeComponent();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
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

		/// <summary>
		/// Handle the event when the user clicks on the Open button.
		/// </summary>
		private void OnOpenClick(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}
		#endregion

		#region Overriden methods
		/// <inheritdoc />
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent($"Closing 'duplicate project found' dialog with result {DialogResult}");
			base.OnClosing(e);
		}

		#endregion
	}
}