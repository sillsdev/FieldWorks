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
// File: AddWsFromPastedTextDlg.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// When a paste operation might add new writing systems to this dialog as a side-effect,
	/// show this dialog to the user so that they can choose to add the writing systems or not.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class AddWsFromPastedTextDlg : Form
	{
		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AddWsFromPastedTextDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddWsFromPastedTextDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AddWsFromPastedTextDlg"/> class.
		/// </summary>
		/// <param name="projName">Name of the current project</param>
		/// <param name="destWsName">Name of the destination writing system</param>
		/// <param name="wsToAdd">The ws to add.</param>
		/// ------------------------------------------------------------------------------------
		public AddWsFromPastedTextDlg(string projName, string destWsName, List<string> wsToAdd) : this()
		{
			// Customize radio button options with project and destination writing system names.
			rdoAddWs.Text = String.Format(rdoAddWs.Text, projName);
			rdoUseDest.Text = String.Format(rdoUseDest.Text, destWsName);

			// Display new writing systems on one line.
			int origHeightOfWsLabel = lblWritingSystems.Height;
			Debug.Assert(wsToAdd.Count > 0);
			lblWritingSystems.Text = wsToAdd[0];
			for (int i = 1; i < wsToAdd.Count; i++ )
				lblWritingSystems.Text += Environment.NewLine + wsToAdd[i];
			Height += (lblWritingSystems.Height - origHeightOfWsLabel);
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the response from the user to handle the writing systems in the paste operation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper.PasteStatus PasteStatus
		{
			get
			{
				if (DialogResult == DialogResult.Cancel)
					return EditingHelper.PasteStatus.CancelPaste;

				return (rdoAddWs.Checked) ? EditingHelper.PasteStatus.PreserveWs :
					EditingHelper.PasteStatus.UseDestWs;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnOk control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, EventArgs e)
		{
			if (rdoNeverAdd.Checked)
				Options.ShowPasteWsChoice = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpWsFromPastedTextDlg");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the rdoUseDest or rdoAddWs control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void rdoUseDestOrAddWs_CheckedChanged(object sender, EventArgs e)
		{
			rdoAlwaysAsk.Enabled = rdoUseDest.Checked;
			rdoNeverAdd.Enabled = rdoUseDest.Checked;
			rdoAlwaysAsk.Checked = true;
		}
		#endregion
	}
}
