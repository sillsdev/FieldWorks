// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwNewLangProjWritingSystemsControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwNewLangProjWritingSystemsControl));
			this.m_lblWsTypeHeader = new System.Windows.Forms.Label();
			this.m_defaultWsLabel = new System.Windows.Forms.TextBox();
			this.m_btnNewVernWrtSys = new System.Windows.Forms.Button();
			this.m_lblExplainWsTypeUsage = new System.Windows.Forms.Label();
			this.m_defaultAnalysisAndVernSame = new System.Windows.Forms.TextBox();
			this.m_defaultAnalysisAndVernSameIcon = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.m_defaultAnalysisAndVernSameIcon)).BeginInit();
			this.SuspendLayout();
			// 
			// m_lblWsTypeHeader
			// 
			resources.ApplyResources(this.m_lblWsTypeHeader, "m_lblWsTypeHeader");
			this.m_lblWsTypeHeader.Name = "m_lblWsTypeHeader";
			// 
			// m_defaultWsLabel
			// 
			resources.ApplyResources(this.m_defaultWsLabel, "m_defaultWsLabel");
			this.m_defaultWsLabel.Name = "m_defaultWsLabel";
			this.m_defaultWsLabel.ReadOnly = true;
			this.m_defaultWsLabel.TabStop = false;
			// 
			// m_btnNewVernWrtSys
			// 
			resources.ApplyResources(this.m_btnNewVernWrtSys, "m_btnNewVernWrtSys");
			this.m_btnNewVernWrtSys.Name = "m_btnNewVernWrtSys";
			this.m_btnNewVernWrtSys.Click += new System.EventHandler(this.ChooseLanguageClick);
			// 
			// m_lblExplainWsTypeUsage
			// 
			resources.ApplyResources(this.m_lblExplainWsTypeUsage, "m_lblExplainWsTypeUsage");
			this.m_lblExplainWsTypeUsage.Name = "m_lblExplainWsTypeUsage";
			// 
			// m_defaultAnalysisAndVernSame
			// 
			this.m_defaultAnalysisAndVernSame.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_defaultAnalysisAndVernSame, "m_defaultAnalysisAndVernSame");
			this.m_defaultAnalysisAndVernSame.Name = "m_defaultAnalysisAndVernSame";
			this.m_defaultAnalysisAndVernSame.ReadOnly = true;
			// 
			// m_defaultAnalysisAndVernSameIcon
			// 
			resources.ApplyResources(this.m_defaultAnalysisAndVernSameIcon, "m_defaultAnalysisAndVernSameIcon");
			this.m_defaultAnalysisAndVernSameIcon.Name = "m_defaultAnalysisAndVernSameIcon";
			this.m_defaultAnalysisAndVernSameIcon.TabStop = false;
			// 
			// FwNewLangProjWritingSystemsControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_defaultAnalysisAndVernSameIcon);
			this.Controls.Add(this.m_defaultAnalysisAndVernSame);
			this.Controls.Add(this.m_defaultWsLabel);
			this.Controls.Add(this.m_btnNewVernWrtSys);
			this.Controls.Add(this.m_lblExplainWsTypeUsage);
			this.Controls.Add(this.m_lblWsTypeHeader);
			this.Name = "FwNewLangProjWritingSystemsControl";
			((System.ComponentModel.ISupportInitialize)(this.m_defaultAnalysisAndVernSameIcon)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblWsTypeHeader;
		private System.Windows.Forms.TextBox m_defaultWsLabel;
		private System.Windows.Forms.Button m_btnNewVernWrtSys;
		private System.Windows.Forms.Label m_lblExplainWsTypeUsage;
		private System.Windows.Forms.TextBox m_defaultAnalysisAndVernSame;
		private System.Windows.Forms.PictureBox m_defaultAnalysisAndVernSameIcon;
	}
}
