// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProgressDialog.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This implements the real progress dialog that is visible to the user. This dialog is
	/// displayed as a modal dialog in the main thread so that it can respond to the user
	/// clicking cancel while the work we want to accomplish is done in a background thread.
	/// </summary>
	/// <remarks>This dialog shouldn't be used directly, only through ProgressDialogWithTask.
	/// If we want to update the progress dialog to update properly and to be responsive to
	/// the user, we have to continually process its messages from the message loop. Previously
	/// this was done by calling Application.DoEvents(), but this can easily cause reentrancy
	/// problems. The proper way to do it is to run the progress dialog on the main thread as
	/// a modal dialog, and do the work which we want to accomplish while showing the dialog in
	/// a background thread. ProgressDialogWithTask is a wrapper to accomplish that, and it
	/// also deals with the multi-threading issues when we want to update the progress dialog
	/// from the background thread.</remarks>
	/// ----------------------------------------------------------------------------------------
	internal partial class ProgressDialogImpl : Form, IFWDisposable
	{
		#region Member variables
		/// <summary>
		/// Event handler for listening to whether or the cancel button is pressed.
		/// </summary>
		public event CancelHandler Cancel;
		/// <summary>
		/// If true, this allows the progress indicator to restart at 0 if it overflows.
		/// </summary>
		private bool m_fRestartable;

		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressDialogImpl"/> class.
		/// </summary>
		/// <param name="owner">The owner (which we also expect to be used as a parameter when
		/// Show() is called).</param>
		/// ------------------------------------------------------------------------------------
		public ProgressDialogImpl(Form owner)
		{
			InitializeComponent();

			StatusMessage = string.Empty;
			m_fRestartable = false;
			FormClosing += new FormClosingEventHandler(CancelFormClosing);
			if (owner == null)
				StartPosition = FormStartPosition.CenterScreen;
			else
			{
				//StartPosition = FormStartPosition.CenterParent;
				// Sadly, just doing CenterParent won't work in this case :-(
				Left = owner.Left + (owner.Width - Width) / 2;
				Top = owner.Top + (owner.Height - Height) / 2;
				Screen primaryScreen = Screen.FromControl(owner);
				Left = Math.Max(Left, primaryScreen.WorkingArea.Left);
				Top = Math.Max(Top, primaryScreen.WorkingArea.Top);
				if (Right > primaryScreen.WorkingArea.Right)
					Left -= (Right - primaryScreen.WorkingArea.Right);
				if (Bottom > primaryScreen.WorkingArea.Bottom)
					Top -= (Bottom - primaryScreen.WorkingArea.Bottom);
			}
		}
		#endregion

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a message indicating progress status.
		/// </summary>
		/// <returns>The status message</returns>
		/// ------------------------------------------------------------------------------------
		public string StatusMessage
		{
			get
			{
				CheckDisposed();
				return lblStatusMessage.Text;
			}
			set
			{
				CheckDisposed();
				lblStatusMessage.Text = value;
				Refresh();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum number of steps or increments corresponding to a progress
		/// bar that's 100% filled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Maximum
		{
			get
			{
				CheckDisposed();
				return progressBar.Maximum;
			}
			set
			{
				CheckDisposed();
				progressBar.Maximum = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the minimum number of steps or increments corresponding to a progress
		/// bar that's 100% empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Minimum
		{
			get
			{
				CheckDisposed();
				return progressBar.Minimum;
			}
			set
			{
				CheckDisposed();
				progressBar.Minimum = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the number of steps (or increments) having been
		/// completed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Value
		{
			get
			{
				CheckDisposed();
				return progressBar.Value;
			}
			set
			{
				CheckDisposed();
				if (m_fRestartable)
					progressBar.Value = (value > progressBar.Maximum) ? 0 : value;
				else
					progressBar.Value = (value > progressBar.Maximum) ? progressBar.Maximum : value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the progress indicator can restart
		/// at zero if it goes beyond the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Restartable
		{
			get
			{
				CheckDisposed();

				return m_fRestartable;
			}
			set
			{
				CheckDisposed();

				m_fRestartable = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the cancel button is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CancelButtonVisible
		{
			get
			{
				CheckDisposed();
				return btnCancel.Visible;
			}
			set
			{
				CheckDisposed();
				btnCancel.Visible = value;

				if (Visible && value)
				{
					// If the cancel button is showing then make sure that the user thinks he can
					// press it by changing the cursor from an hourglass to a busy cursor.
					Cursor = Cursors.AppStarting;
				}

				// TODO: this needs to be removed when we improve the performance of the COM calls
				// from different threads.
				Application.DoEvents();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text on the cancel button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CancelButtonText
		{
			get
			{
				CheckDisposed();
				return btnCancel.Text;
			}
			set
			{
				CheckDisposed();
				btnCancel.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style of the ProgressBar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProgressBarStyle ProgressBarStyle
		{
			get { return progressBar.Style; }
			set { progressBar.Style = value; }
		}
		#endregion

		#region Misc. Methods
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
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Increment the progress counter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Increment()
		{
			Step(1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If no one has subscribed to the cancel event, then don't bother showing the cancel
		/// button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (Visible)
			{
				// If the cancel button is showing then make sure that the user thinks he can
				// press it by changing the cursor from an hourglass to a busy cursor.
				Cursor = Cursors.AppStarting;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls subscribers to the cancel event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnCancel()
		{
			if (Cancel != null)
				Cancel(this);
		}

		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs when the cancel button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected internal void btnCancel_Click(object sender, EventArgs e)
		{
			OnCancel();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs when the ALT-F4 key is pressed to close the form.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void CancelFormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			base.OnClosing(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Closing"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"></see> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			Enabled = false;
		}
		#endregion

		#region Implementation of IAdvInd3

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="cSteps"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void Step(int cSteps)
		{
			CheckDisposed();

			Value += (cSteps > 0) ? cSteps : progressBar.Step;

			// TODO: this needs to be removed when we improve the performance of the COM calls
			// from different threads.
			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string Title
		{
			get
			{
				CheckDisposed();
				return Text;
			}
			set
			{
				CheckDisposed();
				Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the position.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int Position
		{
			get
			{
				CheckDisposed();
				return Value;
			}
			set
			{
				CheckDisposed();

				if (value < progressBar.Minimum)
					Value = progressBar.Minimum;
				else if (value > progressBar.Maximum)
					Value = progressBar.Maximum;
				else
					Value = value;

				// TODO: this needs to be removed when we improve the performance of the COM calls
				// from different threads.
				Application.DoEvents();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the size of the step.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int StepSize
		{
			get
			{
				CheckDisposed();
				return progressBar.Step;
			}
			set
			{
				CheckDisposed();
				progressBar.Step = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="nMin"></param>
		/// <param name="nMax"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetRange(int nMin, int nMax)
		{
			CheckDisposed();

			Debug.Assert(nMin < nMax);
			progressBar.Minimum = nMin;
			progressBar.Maximum = nMax;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the range.
		/// </summary>
		/// <param name="nMin">The minimum.</param>
		/// <param name="nMax">The maximum.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void GetRange(out int nMin, out int nMax)
		{
			CheckDisposed();

			nMin = progressBar.Minimum;
			nMax = progressBar.Maximum;
		}
		#endregion
	}
}
