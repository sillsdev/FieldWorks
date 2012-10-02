using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace SIL.FieldWorks.Samples.ViewSample
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class ViewSampleForm : System.Windows.Forms.Form
	{
		ViewSampleView m_myView;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ViewSampleForm()
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
			this.m_myView = new ViewSampleView();
			this.SuspendLayout();
			//
			// m_myView
			//
			this.m_myView.BackColor = System.Drawing.SystemColors.ControlLight;
			this.m_myView.ForeColor = System.Drawing.SystemColors.WindowText;
			this.m_myView.Location = new System.Drawing.Point(16, 32);
			this.m_myView.Name = "m_myView";
			this.m_myView.Dock = DockStyle.Fill;
			this.m_myView.TabIndex = 0;
			//
			// This
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Name = "Form1";
			this.Text = "ViewSample";
			this.Controls.Add(m_myView);
			this.ResumeLayout();

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new ViewSampleForm());
		}
	}
}
