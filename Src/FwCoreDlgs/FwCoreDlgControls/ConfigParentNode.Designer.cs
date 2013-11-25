// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ConfigMainNode.cs
// Responsibility: mcconnel

using System.Diagnostics.CodeAnalysis;

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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
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
