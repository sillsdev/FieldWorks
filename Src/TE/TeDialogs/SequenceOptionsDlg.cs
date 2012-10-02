// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SequenceOptionsDlg.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class SequenceOptionsDlg : Form, IFWDisposable
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SequenceOptionsDlg"/> class.
		/// </summary>
		/// <param name="restartSequence"></param>
		/// ------------------------------------------------------------------------------------
		public SequenceOptionsDlg(bool restartSequence)
		{
			InitializeComponent();
			opnRestart.Checked = restartSequence;
			opnContinuous.Checked = !restartSequence;
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
		/// Gets a value indicating whether [restart footnote sequence].
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [restart footnote sequence]; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool RestartFootnoteSequence
		{
			get
			{
				CheckDisposed();
				return opnRestart.Checked;
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the paint event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PaintEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle, btnOK.Bounds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnHelp control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpScrProp-FootnoteSequenceOptions");
		}
	}
}