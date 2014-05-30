// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	partial class PublishToWebonaryDlg
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PublishToWebonaryDlg));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.explanationLabel = new System.Windows.Forms.LinkLabel();
			this.publishButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.outputLogTextbox = new System.Windows.Forms.TextBox();
			this.helpButton = new System.Windows.Forms.Button();
			this.publicationLabel = new System.Windows.Forms.Label();
			this.configurationLabel = new System.Windows.Forms.Label();
			this.publicationBox = new System.Windows.Forms.ComboBox();
			this.configurationBox = new System.Windows.Forms.ComboBox();
			this.webonarySettingsGroupbox = new System.Windows.Forms.GroupBox();
			this.settingsForWebonaryTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.showPasswordCheckBox = new System.Windows.Forms.CheckBox();
			this.webonaryPasswordTextbox = new System.Windows.Forms.TextBox();
			this.webonaryUsernameTextbox = new System.Windows.Forms.TextBox();
			this.webonarySiteNameTextbox = new System.Windows.Forms.TextBox();
			this.passwordLabel = new System.Windows.Forms.Label();
			this.usernameLabel = new System.Windows.Forms.Label();
			this.siteNameLabel = new System.Windows.Forms.Label();
			this.webonaryDomainLabel = new System.Windows.Forms.Label();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.tableLayoutPanel.SuspendLayout();
			this.webonarySettingsGroupbox.SuspendLayout();
			this.settingsForWebonaryTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
			this.tableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.publishButton, 0, 4);
			this.tableLayoutPanel.Controls.Add(this.closeButton, 1, 4);
			this.tableLayoutPanel.Controls.Add(this.outputLogTextbox, 0, 5);
			this.tableLayoutPanel.Controls.Add(this.helpButton, 2, 4);
			this.tableLayoutPanel.Controls.Add(this.publicationLabel, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.configurationLabel, 0, 3);
			this.tableLayoutPanel.Controls.Add(this.publicationBox, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.configurationBox, 1, 3);
			this.tableLayoutPanel.Controls.Add(this.webonarySettingsGroupbox, 0, 1);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			// 
			// explanationLabel
			// 
			resources.ApplyResources(this.explanationLabel, "explanationLabel");
			this.tableLayoutPanel.SetColumnSpan(this.explanationLabel, 3);
			this.explanationLabel.Name = "explanationLabel";
			this.explanationLabel.TabStop = true;
			this.explanationLabel.UseCompatibleTextRendering = true;
			// 
			// publishButton
			// 
			resources.ApplyResources(this.publishButton, "publishButton");
			this.publishButton.Name = "publishButton";
			this.publishButton.UseVisualStyleBackColor = true;
			// 
			// closeButton
			// 
			resources.ApplyResources(this.closeButton, "closeButton");
			this.closeButton.Name = "closeButton";
			this.closeButton.UseVisualStyleBackColor = true;
			// 
			// outputLogTextbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.outputLogTextbox, 3);
			resources.ApplyResources(this.outputLogTextbox, "outputLogTextbox");
			this.outputLogTextbox.Name = "outputLogTextbox";
			this.outputLogTextbox.ReadOnly = true;
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = true;
			// 
			// publicationLabel
			// 
			resources.ApplyResources(this.publicationLabel, "publicationLabel");
			this.publicationLabel.Name = "publicationLabel";
			// 
			// configurationLabel
			// 
			resources.ApplyResources(this.configurationLabel, "configurationLabel");
			this.configurationLabel.Name = "configurationLabel";
			// 
			// publicationBox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.publicationBox, 2);
			resources.ApplyResources(this.publicationBox, "publicationBox");
			this.publicationBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.publicationBox.FormattingEnabled = true;
			this.publicationBox.Items.AddRange(new object[] {
            resources.GetString("publicationBox.Items")});
			this.publicationBox.Name = "publicationBox";
			// 
			// configurationBox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.configurationBox, 2);
			resources.ApplyResources(this.configurationBox, "configurationBox");
			this.configurationBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.configurationBox.FormattingEnabled = true;
			this.configurationBox.Items.AddRange(new object[] {
            resources.GetString("configurationBox.Items")});
			this.configurationBox.Name = "configurationBox";
			// 
			// webonarySettingsGroupbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.webonarySettingsGroupbox, 3);
			this.webonarySettingsGroupbox.Controls.Add(this.settingsForWebonaryTableLayoutPanel);
			resources.ApplyResources(this.webonarySettingsGroupbox, "webonarySettingsGroupbox");
			this.webonarySettingsGroupbox.Name = "webonarySettingsGroupbox";
			this.webonarySettingsGroupbox.TabStop = false;
			// 
			// settingsForWebonaryTableLayoutPanel
			// 
			resources.ApplyResources(this.settingsForWebonaryTableLayoutPanel, "settingsForWebonaryTableLayoutPanel");
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.showPasswordCheckBox, 3, 2);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonaryPasswordTextbox, 1, 2);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonaryUsernameTextbox, 1, 1);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonarySiteNameTextbox, 1, 0);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.passwordLabel, 0, 2);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.usernameLabel, 0, 1);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.siteNameLabel, 0, 0);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonaryDomainLabel, 3, 0);
			this.settingsForWebonaryTableLayoutPanel.Name = "settingsForWebonaryTableLayoutPanel";
			// 
			// showPasswordCheckBox
			// 
			resources.ApplyResources(this.showPasswordCheckBox, "showPasswordCheckBox");
			this.settingsForWebonaryTableLayoutPanel.SetColumnSpan(this.showPasswordCheckBox, 2);
			this.showPasswordCheckBox.Name = "showPasswordCheckBox";
			this.showPasswordCheckBox.UseVisualStyleBackColor = true;
			this.showPasswordCheckBox.CheckedChanged += new System.EventHandler(this.showPasswordCheckBox_CheckedChanged);
			// 
			// webonaryPasswordTextbox
			// 
			this.settingsForWebonaryTableLayoutPanel.SetColumnSpan(this.webonaryPasswordTextbox, 2);
			resources.ApplyResources(this.webonaryPasswordTextbox, "webonaryPasswordTextbox");
			this.webonaryPasswordTextbox.Name = "webonaryPasswordTextbox";
			// 
			// webonaryUsernameTextbox
			// 
			this.settingsForWebonaryTableLayoutPanel.SetColumnSpan(this.webonaryUsernameTextbox, 4);
			resources.ApplyResources(this.webonaryUsernameTextbox, "webonaryUsernameTextbox");
			this.webonaryUsernameTextbox.Name = "webonaryUsernameTextbox";
			// 
			// webonarySiteNameTextbox
			// 
			this.settingsForWebonaryTableLayoutPanel.SetColumnSpan(this.webonarySiteNameTextbox, 2);
			resources.ApplyResources(this.webonarySiteNameTextbox, "webonarySiteNameTextbox");
			this.webonarySiteNameTextbox.Name = "webonarySiteNameTextbox";
			// 
			// passwordLabel
			// 
			resources.ApplyResources(this.passwordLabel, "passwordLabel");
			this.passwordLabel.Name = "passwordLabel";
			// 
			// usernameLabel
			// 
			resources.ApplyResources(this.usernameLabel, "usernameLabel");
			this.usernameLabel.Name = "usernameLabel";
			// 
			// siteNameLabel
			// 
			resources.ApplyResources(this.siteNameLabel, "siteNameLabel");
			this.siteNameLabel.Name = "siteNameLabel";
			this.toolTip.SetToolTip(this.siteNameLabel, resources.GetString("siteNameLabel.ToolTip"));
			// 
			// webonaryDomainLabel
			// 
			resources.ApplyResources(this.webonaryDomainLabel, "webonaryDomainLabel");
			this.settingsForWebonaryTableLayoutPanel.SetColumnSpan(this.webonaryDomainLabel, 2);
			this.webonaryDomainLabel.Name = "webonaryDomainLabel";
			// 
			// PublishToWebonaryDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel);
			this.Name = "PublishToWebonaryDlg";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.webonarySettingsGroupbox.ResumeLayout(false);
			this.settingsForWebonaryTableLayoutPanel.ResumeLayout(false);
			this.settingsForWebonaryTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.LinkLabel explanationLabel;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.TextBox outputLogTextbox;
		private System.Windows.Forms.Button publishButton;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Label publicationLabel;
		private System.Windows.Forms.Label configurationLabel;
		private System.Windows.Forms.ComboBox publicationBox;
		private System.Windows.Forms.ComboBox configurationBox;
		private System.Windows.Forms.GroupBox webonarySettingsGroupbox;
		private System.Windows.Forms.TableLayoutPanel settingsForWebonaryTableLayoutPanel;
		private System.Windows.Forms.CheckBox showPasswordCheckBox;
		private System.Windows.Forms.TextBox webonaryPasswordTextbox;
		private System.Windows.Forms.TextBox webonaryUsernameTextbox;
		private System.Windows.Forms.TextBox webonarySiteNameTextbox;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.Label usernameLabel;
		private System.Windows.Forms.Label siteNameLabel;
		private System.Windows.Forms.Label webonaryDomainLabel;
		private System.Windows.Forms.ToolTip toolTip;
	}
}