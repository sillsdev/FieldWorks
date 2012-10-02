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
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Implementation of the HelloView sample in .NET
/// </remarks>
/// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace SIL.FieldWorks.Samples.HelloView
{
	/// <summary>
	/// Summary description for HelloView.
	/// </summary>
	public class HelloView : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		protected SIL.FieldWorks.Samples.HelloView.HelloViewView myView;

		public HelloView()
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
		/// </summary>
		private void InitializeComponent()
		{
			this.myView = new SIL.FieldWorks.Samples.HelloView.HelloViewView();
			this.SuspendLayout();
			//
			// myView
			//
			this.myView.BackColor = System.Drawing.SystemColors.Window;
			this.myView.ForeColor = System.Drawing.SystemColors.WindowText;
			this.myView.Location = new System.Drawing.Point(16, 32);
			this.myView.Name = "myView";
			this.myView.Size = new System.Drawing.Size(232, 192);
			this.myView.TabIndex = 0;
			//
			// HelloView
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.myView});
			this.Name = "HelloView";
			this.Text = "HelloView.NET";
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new HelloView());
		}
	}
}
