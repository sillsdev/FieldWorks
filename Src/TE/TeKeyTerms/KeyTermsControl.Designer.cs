// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
			this.lblDescription = new System.Windows.Forms.Label();
			this.lblSeeAlso = new System.Windows.Forms.Label();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.pnlOuter.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
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
			this.splitContainer.Panel2.Controls.Add(this.flowLayoutPanel1);
			resources.ApplyResources(this.splitContainer, "splitContainer");
			//
			// lblDescription
			//
			this.lblDescription.AutoEllipsis = true;
			resources.ApplyResources(this.lblDescription, "lblDescription");
			this.lblDescription.Name = "lblDescription";
			//
			// lblSeeAlso
			//
			this.lblSeeAlso.AutoEllipsis = true;
			resources.ApplyResources(this.lblSeeAlso, "lblSeeAlso");
			this.lblSeeAlso.Name = "lblSeeAlso";
			//
			// flowLayoutPanel1
			//
			this.flowLayoutPanel1.Controls.Add(this.lblSeeAlso);
			this.flowLayoutPanel1.Controls.Add(this.lblDescription);
			resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			//
			// KeyTermsControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "KeyTermsControl";
			this.pnlOuter.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.splitContainer.ResumeLayout(false);
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.Label lblSeeAlso;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
	}
}
