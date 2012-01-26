// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AddNewUserDlg.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Add New User Dialog is used by User Properties dialog when the Add button is clicked.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AddNewUserDlg : Form, IFWDisposable
	{
		#region Member variables

		private System.Windows.Forms.RadioButton radioBeginner;
		private System.Windows.Forms.RadioButton radioIntermediate;
		private System.Windows.Forms.RadioButton radioAdvanced;
		private System.Windows.Forms.CheckBox cbHasMaintenance;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>The user level that was chosen (1, 3, or 5) for this account.</summary>
		public int m_UserLevel;
		/// <summary>This account has maintenance access of all accounts.</summary>
		public bool m_HasMaintenance;
		/// <summary></summary>
		protected Button btnAdd;

		private IHelpTopicProvider m_helpTopicProvider;

		#endregion

		#region AddNewUserDlg Construction and disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AddNewUserDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddNewUserDlg()
		{
			AccessibleName = GetType().Name;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the dialog properties object for dialogs that are created.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void SetDialogProperties(IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_helpTopicProvider = helpTopicProvider;
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

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddNewUserDlg));
			System.Windows.Forms.Button btnHelp;
			System.Windows.Forms.Label lblPrompt;
			this.btnAdd = new System.Windows.Forms.Button();
			this.radioBeginner = new System.Windows.Forms.RadioButton();
			this.radioIntermediate = new System.Windows.Forms.RadioButton();
			this.radioAdvanced = new System.Windows.Forms.RadioButton();
			this.cbHasMaintenance = new System.Windows.Forms.CheckBox();
			btnCancel = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			lblPrompt = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// lblPrompt
			//
			resources.ApplyResources(lblPrompt, "lblPrompt");
			lblPrompt.Name = "lblPrompt";
			//
			// btnAdd
			//
			resources.ApplyResources(this.btnAdd, "btnAdd");
			this.btnAdd.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// radioBeginner
			//
			resources.ApplyResources(this.radioBeginner, "radioBeginner");
			this.radioBeginner.Name = "radioBeginner";
			//
			// radioIntermediate
			//
			resources.ApplyResources(this.radioIntermediate, "radioIntermediate");
			this.radioIntermediate.Checked = true;
			this.radioIntermediate.Name = "radioIntermediate";
			this.radioIntermediate.TabStop = true;
			//
			// radioAdvanced
			//
			resources.ApplyResources(this.radioAdvanced, "radioAdvanced");
			this.radioAdvanced.Name = "radioAdvanced";
			//
			// cbHasMaintenance
			//
			resources.ApplyResources(this.cbHasMaintenance, "cbHasMaintenance");
			this.cbHasMaintenance.Name = "cbHasMaintenance";
			//
			// AddNewUserDlg
			//
			this.AcceptButton = this.btnAdd;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(this.cbHasMaintenance);
			this.Controls.Add(lblPrompt);
			this.Controls.Add(this.radioAdvanced);
			this.Controls.Add(this.radioIntermediate);
			this.Controls.Add(this.radioBeginner);
			this.Controls.Add(btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnAdd);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddNewUserDlg";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);

		}
		#endregion

		#region Event Handlers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint an etched line to separate main controls from Add, Cancel, and Help buttons.
		/// </summary>
		/// <param name="e">Paint Event arguments</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the etched, horizontal line separating the Add/Cancel/Help buttons
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle,
				btnAdd.Top - (this.ClientRectangle.Bottom - btnAdd.Bottom));
		}

		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			if (radioAdvanced.Checked == true)
				m_UserLevel = 5;
			else if (radioIntermediate.Checked == true)
				m_UserLevel = 3;
			else
				m_UserLevel = 1;

			if (cbHasMaintenance.Checked == true)
				m_HasMaintenance = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpUserProperties_NewUser");
		}

		#endregion
	}
}
