// Copyright (c) 2015-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.LexText.Controls
{
	partial class LexOptionsDlg
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

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexOptionsDlg));
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_tabPrivacy = new System.Windows.Forms.TabPage();
			this.llPrivacy = new System.Windows.Forms.LinkLabel();
			this.PrivacyText = new System.Windows.Forms.TextBox();
			this.m_okToPingCheckBox = new System.Windows.Forms.CheckBox();
			this.m_tabPlugins = new System.Windows.Forms.TabPage();
			this.m_labelPluginRights = new System.Windows.Forms.Label();
			this.m_labelPluginBlurb = new System.Windows.Forms.Label();
			this.m_lvPlugins = new System.Windows.Forms.ListView();
			this.m_chName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.m_chDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.m_tabInterface = new System.Windows.Forms.TabPage();
			this.m_autoOpenCheckBox = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_userInterfaceChooser = new SIL.FieldWorks.Common.Widgets.UserInterfaceChooser();
			this.label3 = new System.Windows.Forms.Label();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.m_tabUpdates = new System.Windows.Forms.TabPage();
			this.gbUpdateChannel = new System.Windows.Forms.GroupBox();
			this.m_textChannelDescription = new System.Windows.Forms.TextBox();
			this.m_cbUpdateChannel = new System.Windows.Forms.ComboBox();
			this.m_okToAutoupdate = new System.Windows.Forms.CheckBox();
			this.m_tabPrivacy.SuspendLayout();
			this.m_tabPlugins.SuspendLayout();
			this.m_tabInterface.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.m_tabUpdates.SuspendLayout();
			this.gbUpdateChannel.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btnOK
			// 
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			// 
			// m_btnCancel
			// 
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			// 
			// m_btnHelp
			// 
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// m_tabPrivacy
			// 
			this.m_tabPrivacy.Controls.Add(this.llPrivacy);
			this.m_tabPrivacy.Controls.Add(this.PrivacyText);
			this.m_tabPrivacy.Controls.Add(this.m_okToPingCheckBox);
			resources.ApplyResources(this.m_tabPrivacy, "m_tabPrivacy");
			this.m_tabPrivacy.Name = "m_tabPrivacy";
			this.m_tabPrivacy.UseVisualStyleBackColor = true;
			// 
			// llPrivacy
			// 
			resources.ApplyResources(this.llPrivacy, "llPrivacy");
			this.llPrivacy.Name = "llPrivacy";
			this.llPrivacy.TabStop = true;
			this.llPrivacy.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.PrivacyLinkClicked);
			// 
			// PrivacyText
			// 
			this.PrivacyText.BackColor = System.Drawing.SystemColors.Window;
			this.PrivacyText.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.PrivacyText, "PrivacyText");
			this.PrivacyText.Name = "PrivacyText";
			this.PrivacyText.ReadOnly = true;
			this.PrivacyText.TabStop = false;
			// 
			// m_okToPingCheckBox
			// 
			resources.ApplyResources(this.m_okToPingCheckBox, "m_okToPingCheckBox");
			this.m_okToPingCheckBox.Name = "m_okToPingCheckBox";
			this.m_okToPingCheckBox.UseVisualStyleBackColor = true;
			// 
			// m_tabPlugins
			// 
			this.m_tabPlugins.Controls.Add(this.m_labelPluginRights);
			this.m_tabPlugins.Controls.Add(this.m_labelPluginBlurb);
			this.m_tabPlugins.Controls.Add(this.m_lvPlugins);
			resources.ApplyResources(this.m_tabPlugins, "m_tabPlugins");
			this.m_tabPlugins.Name = "m_tabPlugins";
			this.m_tabPlugins.UseVisualStyleBackColor = true;
			// 
			// m_labelPluginRights
			// 
			resources.ApplyResources(this.m_labelPluginRights, "m_labelPluginRights");
			this.m_labelPluginRights.Name = "m_labelPluginRights";
			// 
			// m_labelPluginBlurb
			// 
			resources.ApplyResources(this.m_labelPluginBlurb, "m_labelPluginBlurb");
			this.m_labelPluginBlurb.Name = "m_labelPluginBlurb";
			// 
			// m_lvPlugins
			// 
			this.m_lvPlugins.CheckBoxes = true;
			this.m_lvPlugins.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.m_chName,
            this.m_chDescription});
			this.m_lvPlugins.FullRowSelect = true;
			this.m_lvPlugins.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvPlugins.HideSelection = false;
			resources.ApplyResources(this.m_lvPlugins, "m_lvPlugins");
			this.m_lvPlugins.MultiSelect = false;
			this.m_lvPlugins.Name = "m_lvPlugins";
			this.m_lvPlugins.UseCompatibleStateImageBehavior = false;
			this.m_lvPlugins.View = System.Windows.Forms.View.Details;
			// 
			// m_chName
			// 
			resources.ApplyResources(this.m_chName, "m_chName");
			// 
			// m_chDescription
			// 
			resources.ApplyResources(this.m_chDescription, "m_chDescription");
			// 
			// m_tabInterface
			// 
			resources.ApplyResources(this.m_tabInterface, "m_tabInterface");
			this.m_tabInterface.Controls.Add(this.m_autoOpenCheckBox);
			this.m_tabInterface.Controls.Add(this.label4);
			this.m_tabInterface.Controls.Add(this.groupBox1);
			this.m_tabInterface.Name = "m_tabInterface";
			this.m_tabInterface.UseVisualStyleBackColor = true;
			// 
			// m_autoOpenCheckBox
			// 
			resources.ApplyResources(this.m_autoOpenCheckBox, "m_autoOpenCheckBox");
			this.m_autoOpenCheckBox.Name = "m_autoOpenCheckBox";
			this.m_autoOpenCheckBox.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.m_userInterfaceChooser);
			this.groupBox1.Controls.Add(this.label3);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// m_userInterfaceChooser
			// 
			resources.ApplyResources(this.m_userInterfaceChooser, "m_userInterfaceChooser");
			this.m_userInterfaceChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_userInterfaceChooser.FormattingEnabled = true;
			this.m_userInterfaceChooser.Name = "m_userInterfaceChooser";
			this.m_userInterfaceChooser.Sorted = true;
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// tabControl1
			// 
			resources.ApplyResources(this.tabControl1, "tabControl1");
			this.tabControl1.Controls.Add(this.m_tabInterface);
			this.tabControl1.Controls.Add(this.m_tabPlugins);
			this.tabControl1.Controls.Add(this.m_tabPrivacy);
			this.tabControl1.Controls.Add(this.m_tabUpdates);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			// 
			// m_tabUpdates
			// 
			this.m_tabUpdates.Controls.Add(this.gbUpdateChannel);
			this.m_tabUpdates.Controls.Add(this.m_okToAutoupdate);
			resources.ApplyResources(this.m_tabUpdates, "m_tabUpdates");
			this.m_tabUpdates.Name = "m_tabUpdates";
			this.m_tabUpdates.UseVisualStyleBackColor = true;
			// 
			// gbUpdateChannel
			// 
			this.gbUpdateChannel.Controls.Add(this.m_textChannelDescription);
			this.gbUpdateChannel.Controls.Add(this.m_cbUpdateChannel);
			resources.ApplyResources(this.gbUpdateChannel, "gbUpdateChannel");
			this.gbUpdateChannel.Name = "gbUpdateChannel";
			this.gbUpdateChannel.TabStop = false;
			// 
			// m_textChannelDescription
			// 
			this.m_textChannelDescription.BackColor = System.Drawing.SystemColors.Window;
			this.m_textChannelDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_textChannelDescription, "m_textChannelDescription");
			this.m_textChannelDescription.Name = "m_textChannelDescription";
			this.m_textChannelDescription.ReadOnly = true;
			// 
			// m_cbUpdateChannel
			// 
			this.m_cbUpdateChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbUpdateChannel, "m_cbUpdateChannel");
			this.m_cbUpdateChannel.Name = "m_cbUpdateChannel";
			this.m_cbUpdateChannel.SelectedIndexChanged += new System.EventHandler(this.m_cbUpdateChannel_SelectedIndexChanged);
			this.m_cbUpdateChannel.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_cbUpdateChannel_KeyPress);
			// 
			// m_okToAutoupdate
			// 
			resources.ApplyResources(this.m_okToAutoupdate, "m_okToAutoupdate");
			this.m_okToAutoupdate.Name = "m_okToAutoupdate";
			this.m_okToAutoupdate.UseVisualStyleBackColor = true;
			this.m_okToAutoupdate.CheckedChanged += new System.EventHandler(this.m_okToAutoupdate_CheckedChanged);
			// 
			// LexOptionsDlg
			// 
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.tabControl1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LexOptionsDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.m_tabPrivacy.ResumeLayout(false);
			this.m_tabPrivacy.PerformLayout();
			this.m_tabPlugins.ResumeLayout(false);
			this.m_tabPlugins.PerformLayout();
			this.m_tabInterface.ResumeLayout(false);
			this.m_tabInterface.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.m_tabUpdates.ResumeLayout(false);
			this.m_tabUpdates.PerformLayout();
			this.gbUpdateChannel.ResumeLayout(false);
			this.gbUpdateChannel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.TabPage m_tabPrivacy;
		private System.Windows.Forms.TextBox PrivacyText;
		private System.Windows.Forms.CheckBox m_okToPingCheckBox;
		private System.Windows.Forms.TabPage m_tabPlugins;
		private System.Windows.Forms.Label m_labelPluginRights;
		private System.Windows.Forms.Label m_labelPluginBlurb;
		private System.Windows.Forms.ListView m_lvPlugins;
		private System.Windows.Forms.ColumnHeader m_chName;
		private System.Windows.Forms.ColumnHeader m_chDescription;
		private System.Windows.Forms.TabPage m_tabInterface;
		private System.Windows.Forms.CheckBox m_autoOpenCheckBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.GroupBox groupBox1;
		private Common.Widgets.UserInterfaceChooser m_userInterfaceChooser;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.LinkLabel llPrivacy;
		private System.Windows.Forms.TabPage m_tabUpdates;
		private System.Windows.Forms.CheckBox m_okToAutoupdate;
		private System.Windows.Forms.GroupBox gbUpdateChannel;
		private System.Windows.Forms.ComboBox m_cbUpdateChannel;
		private System.Windows.Forms.TextBox m_textChannelDescription;
	}
}