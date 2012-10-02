// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwGeneralTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class FwGeneralTab
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
			System.Windows.Forms.GroupBox groupBox1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwGeneralTab));
			System.Windows.Forms.Label label8;
			System.Windows.Forms.Label label7;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label3;
			this.m_lblStyleDescription = new System.Windows.Forms.Label();
			this.m_txtStyleUsage = new System.Windows.Forms.TextBox();
			this.m_txtShortcut = new System.Windows.Forms.TextBox();
			this.m_cboFollowingStyle = new System.Windows.Forms.ComboBox();
			this.m_cboBasedOn = new System.Windows.Forms.ComboBox();
			this.m_lblStyleType = new System.Windows.Forms.Label();
			this.m_txtStyleName = new System.Windows.Forms.TextBox();
			groupBox1 = new System.Windows.Forms.GroupBox();
			label8 = new System.Windows.Forms.Label();
			label7 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// groupBox1
			//
			groupBox1.Controls.Add(this.m_lblStyleDescription);
			resources.ApplyResources(groupBox1, "groupBox1");
			groupBox1.Name = "groupBox1";
			groupBox1.TabStop = false;
			//
			// m_lblStyleDescription
			//
			resources.ApplyResources(this.m_lblStyleDescription, "m_lblStyleDescription");
			this.m_lblStyleDescription.Name = "m_lblStyleDescription";
			//
			// label8
			//
			resources.ApplyResources(label8, "label8");
			label8.Name = "label8";
			//
			// label7
			//
			resources.ApplyResources(label7, "label7");
			label7.Name = "label7";
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// m_txtStyleUsage
			//
			resources.ApplyResources(this.m_txtStyleUsage, "m_txtStyleUsage");
			this.m_txtStyleUsage.Name = "m_txtStyleUsage";
			//
			// m_txtShortcut
			//
			resources.ApplyResources(this.m_txtShortcut, "m_txtShortcut");
			this.m_txtShortcut.Name = "m_txtShortcut";
			//
			// m_cboFollowingStyle
			//
			this.m_cboFollowingStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboFollowingStyle.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboFollowingStyle, "m_cboFollowingStyle");
			this.m_cboFollowingStyle.Name = "m_cboFollowingStyle";
			//
			// m_cboBasedOn
			//
			this.m_cboBasedOn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboBasedOn.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboBasedOn, "m_cboBasedOn");
			this.m_cboBasedOn.Name = "m_cboBasedOn";
			//
			// m_lblStyleType
			//
			resources.ApplyResources(this.m_lblStyleType, "m_lblStyleType");
			this.m_lblStyleType.Name = "m_lblStyleType";
			//
			// m_txtStyleName
			//
			resources.ApplyResources(this.m_txtStyleName, "m_txtStyleName");
			this.m_txtStyleName.Name = "m_txtStyleName";
			this.m_txtStyleName.Validated += new System.EventHandler(this.m_txtStyleName_Validated);
			this.m_txtStyleName.Validating += new System.ComponentModel.CancelEventHandler(this.m_txtStyleName_Validating);
			//
			// FwGeneralTab
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(groupBox1);
			this.Controls.Add(this.m_txtStyleUsage);
			this.Controls.Add(label8);
			this.Controls.Add(this.m_txtShortcut);
			this.Controls.Add(label7);
			this.Controls.Add(label6);
			this.Controls.Add(this.m_cboFollowingStyle);
			this.Controls.Add(label5);
			this.Controls.Add(this.m_cboBasedOn);
			this.Controls.Add(this.m_lblStyleType);
			this.Controls.Add(label4);
			this.Controls.Add(this.m_txtStyleName);
			this.Controls.Add(label3);
			this.Name = "FwGeneralTab";
			groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblStyleDescription;
		private System.Windows.Forms.TextBox m_txtStyleUsage;
		private System.Windows.Forms.TextBox m_txtShortcut;
		private System.Windows.Forms.ComboBox m_cboFollowingStyle;
		private System.Windows.Forms.ComboBox m_cboBasedOn;
		private System.Windows.Forms.Label m_lblStyleType;
		private System.Windows.Forms.TextBox m_txtStyleName;
	}
}
