// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SequenceOptionsDlg.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

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
using SIL.Utils;
using SIL.FieldWorks.Common.Framework;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class SequenceOptionsDlg : Form, IFWDisposable
	{
		private IHelpTopicProvider m_helpTopicProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SequenceOptionsDlg"/> class.
		/// </summary>
		/// <param name="restartSequence">if set to <c>true</c> [restart sequence].</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public SequenceOptionsDlg(bool restartSequence, IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			m_helpTopicProvider = helpTopicProvider;
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
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpScrProp-FootnoteSequenceOptions");
		}
	}
}