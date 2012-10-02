/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: HelloView.cs
/// Responsibility: John Thomson
/// Last reviewed:
///
/// <remarks>
/// Implementation of the InterlinearSample sample in .NET.
/// </remarks>
/// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace SIL.FieldWorks.Samples.InterlinearSample
{
	/// <summary>
	/// Top level form for the interlinear sample application
	/// </summary>
	public class InterlinForm : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		protected SIL.FieldWorks.Samples.InterlinearSample.InterlinearView myView;

		public InterlinForm()
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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
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
		/// (But I  (JohnT) did, following the model of HelloView...maybe we'd better not make any more
		/// changes in designer mode??)
		/// </summary>
		private void InitializeComponent()
		{
			//
			// Set up the interlinear view
			this.myView = new SIL.FieldWorks.Samples.InterlinearSample.InterlinearView();
			this.SuspendLayout();
			//
			// myView
			//
			this.myView.BackColor = System.Drawing.SystemColors.Window;
			this.myView.ForeColor = System.Drawing.SystemColors.WindowText;
			this.myView.Location = new System.Drawing.Point(16, 32); // Review JohnT: compute from container info?
			this.myView.Name = "InterlinearView";
			this.myView.Size = new System.Drawing.Size(232, 192); // Review JohnT: compute from container info?
			this.myView.TabIndex = 0;
			//
			// Main window (after contained view, following model of HellowView; maybe so
			// we get an appropriate OnSize for the contained view when sizing the main one?
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 273);
			// JohnT: Don't really understand this...copied from HelloView.
			this.Controls.AddRange(new System.Windows.Forms.Control[] {this.myView});
			this.Name = "InterlinSample";
			this.Text = "InterlinSample";

			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new InterlinForm());
		}
	}
}
