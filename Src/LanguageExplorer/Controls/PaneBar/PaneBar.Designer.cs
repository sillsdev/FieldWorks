// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.PaneBar
{
	partial class PaneBar
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();
			}

			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.m_panelMain = new LanguageExplorer.Controls.PaneBar.PanelExtension();
			this.m_panelMain.SuspendLayout();
			this.SuspendLayout();
			//
			// m_panelMain
			//
			this.m_panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panelMain.DockPadding.Bottom = 0;
			this.m_panelMain.DockPadding.Left = 2;
			this.m_panelMain.DockPadding.Right = 2;
			this.m_panelMain.DockPadding.Top = 0;
			this.m_panelMain.Font = new System.Drawing.Font("Tahoma", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.m_panelMain.Location = new System.Drawing.Point(0, 0);
			this.m_panelMain.Name = "m_panelMain";
			this.m_panelMain.Size = new System.Drawing.Size(704, 24);
			this.m_panelMain.TabIndex = 0;
			//
			// PaneBar
			//
			this.Controls.Add(this.m_panelMain);
			this.Name = "PaneBar";
			this.Size = new System.Drawing.Size(696, 24);
			this.m_panelMain.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		#endregion

		private LanguageExplorer.Controls.PaneBar.PanelExtension m_panelMain;
	}
}
