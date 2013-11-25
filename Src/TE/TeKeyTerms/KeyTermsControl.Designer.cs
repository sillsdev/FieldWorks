// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: KeyTermsControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.TE
{
	partial class KeyTermsControl
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
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeyTermsControl));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.lblDescription = new System.Windows.Forms.Label();
			this.lblSeeAlso = new System.Windows.Forms.Label();
			this.pnlOuter.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// pnlOuter
			//
			resources.ApplyResources(this.pnlOuter, "pnlOuter");
			//
			// splitContainer
			//
			//
			// splitContainer.Panel2
			//
			this.splitContainer.Panel2.Controls.Add(this.tableLayoutPanel1);
			resources.ApplyResources(this.splitContainer, "splitContainer");
			//
			// tableLayoutPanel1
			//
			resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
			this.tableLayoutPanel1.Controls.Add(this.lblDescription, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.lblSeeAlso, 0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			//
			// lblDescription
			//
			resources.ApplyResources(this.lblDescription, "lblDescription");
			this.lblDescription.AutoEllipsis = true;
			this.lblDescription.Name = "lblDescription";
			//
			// lblSeeAlso
			//
			resources.ApplyResources(this.lblSeeAlso, "lblSeeAlso");
			this.lblSeeAlso.Name = "lblSeeAlso";
			//
			// KeyTermsControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "KeyTermsControl";
			this.pnlOuter.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.splitContainer.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.Label lblSeeAlso;
	}
}
