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
// File: ProgressDialogWorkingOn.cs
// Responsibility: AndyBlack
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

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for ProgressDialogWorkingOn.
	/// </summary>
	public class ProgressDialogWorkingOn : Form, IFWDisposable
	{
		private System.Windows.Forms.Label m_LabelCreationProgress;
		private System.Windows.Forms.Label m_LabelWorkingOnPrompt;
		private System.Windows.Forms.Label m_LabelWorkingOn;
		private System.Windows.Forms.ProgressBar m_ProgressBar;
		private System.Windows.Forms.Button m_CancelBtn;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Initializes a new instance of the ProgressDialog class.
		/// </summary>
		public ProgressDialogWorkingOn()
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
		protected override void Dispose(bool disposing)
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressDialogWorkingOn));
			this.m_LabelCreationProgress = new System.Windows.Forms.Label();
			this.m_LabelWorkingOnPrompt = new System.Windows.Forms.Label();
			this.m_LabelWorkingOn = new System.Windows.Forms.Label();
			this.m_ProgressBar = new System.Windows.Forms.ProgressBar();
			this.m_CancelBtn = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_LabelCreationProgress
			//
			resources.ApplyResources(this.m_LabelCreationProgress, "m_LabelCreationProgress");
			this.m_LabelCreationProgress.Name = "m_LabelCreationProgress";
			//
			// m_LabelWorkingOnPrompt
			//
			resources.ApplyResources(this.m_LabelWorkingOnPrompt, "m_LabelWorkingOnPrompt");
			this.m_LabelWorkingOnPrompt.Name = "m_LabelWorkingOnPrompt";
			//
			// m_LabelWorkingOn
			//
			resources.ApplyResources(this.m_LabelWorkingOn, "m_LabelWorkingOn");
			this.m_LabelWorkingOn.Name = "m_LabelWorkingOn";
			//
			// m_ProgressBar
			//
			resources.ApplyResources(this.m_ProgressBar, "m_ProgressBar");
			this.m_ProgressBar.Maximum = 18;
			this.m_ProgressBar.Name = "m_ProgressBar";
			this.m_ProgressBar.Step = 1;
			//
			// m_CancelBtn
			//
			this.m_CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_CancelBtn, "m_CancelBtn");
			this.m_CancelBtn.Name = "m_CancelBtn";
			//
			// ProgressDialogWorkingOn
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_CancelBtn;
			this.Controls.Add(this.m_CancelBtn);
			this.Controls.Add(this.m_ProgressBar);
			this.Controls.Add(this.m_LabelWorkingOn);
			this.Controls.Add(this.m_LabelWorkingOnPrompt);
			this.Controls.Add(this.m_LabelCreationProgress);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProgressDialogWorkingOn";
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Perform a progress step
		/// </summary>
		public void PerformStep()
		{
			CheckDisposed();

			m_ProgressBar.PerformStep();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the content of the "Working on" field
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string WorkingOnText
		{
			get
			{
				CheckDisposed();

				return m_LabelWorkingOn.Text;
			}
			set
			{
				CheckDisposed();

				m_LabelWorkingOn.Text = value;
			}
		}

		/// <summary>
		/// Gets/sets the label next to the progress bar (default: Creation Progress)
		/// </summary>
		public string ProgressLabel
		{
			get
			{
				CheckDisposed();

				return m_LabelCreationProgress.Text;
			}
			set
			{
				CheckDisposed();

				m_LabelCreationProgress.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the minimum number of steps or increments corresponding to a progress
		/// bar that's 100% filled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Minimum
		{
			get
			{
				CheckDisposed();

				return m_ProgressBar.Minimum;
			}
			set
			{
				CheckDisposed();

				m_ProgressBar.Minimum = value;
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

				return m_ProgressBar.Maximum;
			}
			set
			{
				CheckDisposed();

				m_ProgressBar.Maximum = value;
			}
		}

		/// <summary>
		/// Provides access to the progress bar for use in a ProgressState.
		/// Note that as a side effect the minimum and maximum get set.
		/// </summary>
		public IProgressDisplayer ProgressDisplayer
		{
			get { return new ProgressBarWrapper(m_ProgressBar); }
		}
	}
}
