// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace FDOBrowser
{
	partial class FDOBrowserForm
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (disposing && !IsDisposed)
			{
				if (components != null)
					components.Dispose();
				if (File.Exists(FileName + ".lock"))
				{
					m_cache.ServiceLocator.GetInstance<IActionHandler>().Commit();
					m_cache.Dispose();
				}
			}
			m_cache = null; // Don't try to use it again
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// FDOBrowserForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(872, 567);
			this.Location = new System.Drawing.Point(0, 0);
			this.Name = "FDOBrowserForm";
			this.Text = "FDO Browser";
			this.Controls.SetChildIndex(this.m_dockPanel, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

	}
}