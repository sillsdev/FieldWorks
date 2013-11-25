// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: GotoReferenceDialog.cs
// Responsibility: TE Team

using System;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrControls;
using SILUBS.SharedScrUtils;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// GotoReferenceDialog class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class GotoReferenceDialog : Form, IFWDisposable
	{
		private ScrPassageControl scrPassageControl = null;
		//		private System.Windows.Forms.Button btn_ok;
		private IScripture m_scripture;
		private Button btn_help;
		private Button btn_cancel;
		private Button btn_OK;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private IHelpTopicProvider m_helpProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor - needed for Designer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public GotoReferenceDialog() : this(ScrReference.Empty, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="GotoReferenceDialog"/> class.
		/// </summary>
		/// <param name="reference">The initial reference to populate the control.</param>
		/// <param name="scr">The Scripture object.</param>
		/// <param name="helpProvider">The help provider.</param>
		/// ------------------------------------------------------------------------------------
		public GotoReferenceDialog(ScrReference reference, IScripture scr, IHelpTopicProvider helpProvider)
		{
			Logger.WriteEvent("Opening 'Goto Reference' dialog");

			m_scripture = scr;
			m_helpProvider = helpProvider;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			scrPassageControl = new DbScrPassageControl(reference, m_scripture);
			scrPassageControl.Location = new System.Drawing.Point(16, 16);
			scrPassageControl.Name = "scrPassageControl";
			scrPassageControl.Size = new System.Drawing.Size(Width - 36, 24);
			Controls.Add(scrPassageControl);

			scrPassageControl.TabIndex = 0;
			btn_OK.TabIndex = 1;
			btn_cancel.TabIndex = 2;
			btn_help.TabIndex = 3;
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
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GotoReferenceDialog));
			this.btn_cancel = new System.Windows.Forms.Button();
			this.btn_OK = new System.Windows.Forms.Button();
			this.btn_help = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// btn_cancel
			//
			this.btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btn_cancel, "btn_cancel");
			this.btn_cancel.Name = "btn_cancel";
			//
			// btn_OK
			//
			resources.ApplyResources(this.btn_OK, "btn_OK");
			this.btn_OK.Name = "btn_OK";
			this.btn_OK.Click += new System.EventHandler(this.btn_ok_Click);
			//
			// btn_help
			//
			resources.ApplyResources(this.btn_help, "btn_help");
			this.btn_help.Name = "btn_help";
			this.btn_help.Click += new System.EventHandler(this.btn_help_Click);
			//
			// GotoReferenceDialog
			//
			this.AcceptButton = this.btn_OK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btn_cancel;
			this.Controls.Add(this.btn_OK);
			this.Controls.Add(this.btn_help);
			this.Controls.Add(this.btn_cancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GotoReferenceDialog";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The ScReference for where we want to go today.
		/// </summary>
		/// <value>The sc reference.</value>
		/// ------------------------------------------------------------------------------------
		public ScrReference ScReference
		{
			get
			{
				CheckDisposed();

				return scrPassageControl.ScReference;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the etched, horizontal line separating the wizard buttons
			// from the rest of the form.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle, btn_help.Bounds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent("Closing 'Goto Reference' dialog with result " +
				DialogResult.ToString());
			base.OnClosing (e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the OK button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btn_ok_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;

			// If the text has been edited in the scripture passage control, then the reference
			// may not have been parsed yet, so make sure it gets parsed.
			scrPassageControl.ResolveReference();

			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btn_help_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpProvider, "khtpGoToReference");
		}
		#endregion
	}
}
