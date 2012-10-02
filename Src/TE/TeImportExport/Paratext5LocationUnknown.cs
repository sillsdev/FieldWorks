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
// File: Paratext5LocationUnknown.cs
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

using SIL.Utils;
using SIL.FieldWorks.Common.Drawing;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This dialog is displayed during the FW scripture import process the location
	/// of Paratext 5 cannot be located on a user's computer.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class Paratext5LocationUnknown : Form, IFWDisposable
	{
		/// <summary>
		/// This is similar to a DialogResult, however, this is a taylored result for a
		/// Paratext5LocationUnknown dialog box.
		/// </summary>
		public enum Results
		{
			/// <summary>Browse button chosen</summary>
			Browse,
			/// <summary>OK button chosen</summary>
			OK,
			/// <summary>Help button chosen</summary>
			Help
		}

		private Results m_Result;
		private Button btnOK;
		private PictureBox picDlgIcon;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		///
		/// </summary>
		public Paratext5LocationUnknown()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Paratext5LocationUnknown));
			System.Windows.Forms.Button btnBrowse;
			System.Windows.Forms.Button btnHelp;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label2;
			this.picDlgIcon = new System.Windows.Forms.PictureBox();
			this.btnOK = new System.Windows.Forms.Button();
			btnBrowse = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.picDlgIcon)).BeginInit();
			this.SuspendLayout();
			//
			// picDlgIcon
			//
			resources.ApplyResources(this.picDlgIcon, "picDlgIcon");
			this.picDlgIcon.Name = "picDlgIcon";
			this.picDlgIcon.TabStop = false;
			//
			// btnBrowse
			//
			resources.ApplyResources(btnBrowse, "btnBrowse");
			btnBrowse.Name = "btnBrowse";
			btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// Paratext5LocationUnknown
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(label2);
			this.Controls.Add(label1);
			this.Controls.Add(btnHelp);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(btnBrowse);
			this.Controls.Add(this.picDlgIcon);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Paratext5LocationUnknown";
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.picDlgIcon)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the result of the user's interation with the dialog (i.e. what button
		/// they clicked.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Results Result
		{
			get
			{
				CheckDisposed();
				return m_Result;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Misc. initializations (i.e. make the icon's picture box just large enough to
		/// accomodate the icon. Then load the icon into the picture box from the system
		/// icons.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			picDlgIcon.Size = SystemIcons.Information.Size;
			picDlgIcon.Image = SystemIcons.Information.ToBitmap();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This will make sure an etched line is drawn above the OK and Help buttons.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the etched, horizontal line separating the OK and Help buttons
			// from the rest of the form.
			LineDrawing.Draw(e.Graphics, 10, btnOK.Top - 10, this.ClientSize.Width - 20,
				LineTypes.Etched);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Saves that the browse button was clicked and hide the dialog so control is
		/// returned to the dialog's parent object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void btnBrowse_Click(object sender, System.EventArgs e)
		{
			m_Result = Results.Browse;
			this.Visible = false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Saves that the Help button was clicked and hide the dialog so control is
		/// returned to the dialog's parent object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			m_Result = Results.Help;
			this.Visible = false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Saves that the OK button was clicked and hide the dialog so control is
		/// returned to the dialog's parent object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void btnOK_Click(object sender, System.EventArgs e)
		{
			m_Result = Results.OK;
			this.Visible = false;
		}
	}
}
