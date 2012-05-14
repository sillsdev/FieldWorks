// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ConfigMainNode.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class ConfigParentNode
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigParentNode));
			this.m_lnkConfigureNow = new System.Windows.Forms.LinkLabel();
			this.m_lblMoreDetail = new System.Windows.Forms.Label();
			this.m_tbMoreDetail = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// m_lnkConfigureNow
			//
			resources.ApplyResources(this.m_lnkConfigureNow, "m_lnkConfigureNow");
			this.m_lnkConfigureNow.Name = "m_lnkConfigureNow";
			this.m_lnkConfigureNow.TabStop = true;
			this.m_lnkConfigureNow.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lnkConfigureNow_LinkClicked);
			//
			// m_lblMoreDetail
			//
			resources.ApplyResources(this.m_lblMoreDetail, "m_lblMoreDetail");
			this.m_lblMoreDetail.Name = "m_lblMoreDetail";
			//
			// m_tbMoreDetail
			//
			this.m_tbMoreDetail.BackColor = System.Drawing.SystemColors.Control;
			this.m_tbMoreDetail.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbMoreDetail.CausesValidation = false;
			resources.ApplyResources(this.m_tbMoreDetail, "m_tbMoreDetail");
			this.m_tbMoreDetail.Name = "m_tbMoreDetail";
			this.m_tbMoreDetail.ReadOnly = true;
			this.m_tbMoreDetail.TabStop = false;
			//
			// ConfigParentNode
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tbMoreDetail);
			this.Controls.Add(this.m_lnkConfigureNow);
			this.Controls.Add(this.m_lblMoreDetail);
			this.Name = "ConfigParentNode";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.LinkLabel m_lnkConfigureNow;
		private System.Windows.Forms.Label m_lblMoreDetail;
		private System.Windows.Forms.TextBox m_tbMoreDetail;
	}
}
