// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks
{
	partial class UploadToWebonaryDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UploadToWebonaryDlg));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.explanationLabel = new System.Windows.Forms.LinkLabel();
			this.webonarySettingsGroupbox = new System.Windows.Forms.GroupBox();
			this.settingsForWebonaryTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.webonaryPasswordTextbox = new SIL.FieldWorks.Common.Widgets.PasswordBox();
			this.webonaryUsernameTextbox = new System.Windows.Forms.TextBox();
			this.webonarySiteNameTextbox = new System.Windows.Forms.TextBox();
			this.passwordLabel = new System.Windows.Forms.Label();
			this.usernameLabel = new System.Windows.Forms.Label();
			this.siteNameLabel = new System.Windows.Forms.Label();
			this.webonarySiteURLLabel = new System.Windows.Forms.Label();
			this.rememberPasswordCheckbox = new System.Windows.Forms.CheckBox();
			this.publicationGroupBox = new System.Windows.Forms.GroupBox();
			this.publicationSelectionTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.configurationBox = new System.Windows.Forms.ComboBox();
			this.publicationBox = new System.Windows.Forms.ComboBox();
			this.configurationLabel = new System.Windows.Forms.Label();
			this.publicationLabel = new System.Windows.Forms.Label();
			this.reversalsLabel = new System.Windows.Forms.Label();
			this.howManyPubsAlertLabel = new System.Windows.Forms.Label();
			this.reversalsCheckedListBox = new System.Windows.Forms.CheckedListBox();
			this.publishButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.reportButton = new System.Windows.Forms.Button();
			this.helpButton = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_progress = new System.Windows.Forms.ProgressBar();
			this.tableLayoutPanel.SuspendLayout();
			this.webonarySettingsGroupbox.SuspendLayout();
			this.settingsForWebonaryTableLayoutPanel.SuspendLayout();
			this.publicationGroupBox.SuspendLayout();
			this.publicationSelectionTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
			this.tableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.webonarySettingsGroupbox, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.publicationGroupBox, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.publishButton, 0, 4);
			this.tableLayoutPanel.Controls.Add(this.closeButton, 1, 4);
			this.tableLayoutPanel.Controls.Add(this.reportButton, 2, 4);
			this.tableLayoutPanel.Controls.Add(this.helpButton, 3, 4);
			this.tableLayoutPanel.Controls.Add(this.m_progress, 0, 3);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			// 
			// explanationLabel
			// 
			resources.ApplyResources(this.explanationLabel, "explanationLabel");
			this.tableLayoutPanel.SetColumnSpan(this.explanationLabel, 4);
			this.explanationLabel.Name = "explanationLabel";
			// 
			// webonarySettingsGroupbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.webonarySettingsGroupbox, 4);
			this.webonarySettingsGroupbox.Controls.Add(this.settingsForWebonaryTableLayoutPanel);
			resources.ApplyResources(this.webonarySettingsGroupbox, "webonarySettingsGroupbox");
			this.webonarySettingsGroupbox.Name = "webonarySettingsGroupbox";
			this.webonarySettingsGroupbox.TabStop = false;
			// 
			// settingsForWebonaryTableLayoutPanel
			// 
			resources.ApplyResources(this.settingsForWebonaryTableLayoutPanel, "settingsForWebonaryTableLayoutPanel");
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonaryPasswordTextbox, 1, 3);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonaryUsernameTextbox, 1, 2);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonarySiteNameTextbox, 1, 0);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.passwordLabel, 0, 3);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.usernameLabel, 0, 2);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.siteNameLabel, 0, 0);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.webonarySiteURLLabel, 1, 1);
			this.settingsForWebonaryTableLayoutPanel.Controls.Add(this.rememberPasswordCheckbox, 2, 3);
			this.settingsForWebonaryTableLayoutPanel.Name = "settingsForWebonaryTableLayoutPanel";
			// 
			// webonaryPasswordTextbox
			// 
			resources.ApplyResources(this.webonaryPasswordTextbox, "webonaryPasswordTextbox");
			this.webonaryPasswordTextbox.Name = "webonaryPasswordTextbox";
			this.toolTip.SetToolTip(this.webonaryPasswordTextbox, resources.GetString("webonaryPasswordTextbox.ToolTip"));
			// 
			// webonaryUsernameTextbox
			// 
			resources.ApplyResources(this.webonaryUsernameTextbox, "webonaryUsernameTextbox");
			this.webonaryUsernameTextbox.Name = "webonaryUsernameTextbox";
			this.toolTip.SetToolTip(this.webonaryUsernameTextbox, resources.GetString("webonaryUsernameTextbox.ToolTip"));
			// 
			// webonarySiteNameTextbox
			// 
			resources.ApplyResources(this.webonarySiteNameTextbox, "webonarySiteNameTextbox");
			this.webonarySiteNameTextbox.Name = "webonarySiteNameTextbox";
			this.toolTip.SetToolTip(this.webonarySiteNameTextbox, resources.GetString("webonarySiteNameTextbox.ToolTip"));
			this.webonarySiteNameTextbox.TextChanged += new System.EventHandler(this.siteNameBox_TextChanged);
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
			// webonarySiteURLLabel
			// 
			resources.ApplyResources(this.webonarySiteURLLabel, "webonarySiteURLLabel");
			this.settingsForWebonaryTableLayoutPanel.SetColumnSpan(this.webonarySiteURLLabel, 2);
			this.webonarySiteURLLabel.Name = "webonarySiteURLLabel";
			// 
			// rememberPasswordCheckbox
			// 
			resources.ApplyResources(this.rememberPasswordCheckbox, "rememberPasswordCheckbox");
			this.rememberPasswordCheckbox.Name = "rememberPasswordCheckbox";
			this.rememberPasswordCheckbox.UseVisualStyleBackColor = true;
			// 
			// publicationGroupBox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.publicationGroupBox, 4);
			this.publicationGroupBox.Controls.Add(this.publicationSelectionTableLayoutPanel);
			resources.ApplyResources(this.publicationGroupBox, "publicationGroupBox");
			this.publicationGroupBox.Name = "publicationGroupBox";
			this.publicationGroupBox.TabStop = false;
			// 
			// publicationSelectionTableLayoutPanel
			// 
			resources.ApplyResources(this.publicationSelectionTableLayoutPanel, "publicationSelectionTableLayoutPanel");
			this.publicationSelectionTableLayoutPanel.Controls.Add(this.configurationBox, 1, 1);
			this.publicationSelectionTableLayoutPanel.Controls.Add(this.publicationBox, 1, 0);
			this.publicationSelectionTableLayoutPanel.Controls.Add(this.configurationLabel, 0, 1);
			this.publicationSelectionTableLayoutPanel.Controls.Add(this.publicationLabel, 0, 0);
			this.publicationSelectionTableLayoutPanel.Controls.Add(this.reversalsLabel, 0, 2);
			this.publicationSelectionTableLayoutPanel.Controls.Add(this.howManyPubsAlertLabel, 0, 3);
			this.publicationSelectionTableLayoutPanel.Controls.Add(this.reversalsCheckedListBox, 1, 2);
			this.publicationSelectionTableLayoutPanel.Name = "publicationSelectionTableLayoutPanel";
			// 
			// configurationBox
			// 
			this.publicationSelectionTableLayoutPanel.SetColumnSpan(this.configurationBox, 2);
			resources.ApplyResources(this.configurationBox, "configurationBox");
			this.configurationBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.configurationBox.FormattingEnabled = true;
			this.configurationBox.Name = "configurationBox";
			this.configurationBox.SelectedIndexChanged += new System.EventHandler(this.configurationBox_SelectedIndexChanged);
			// 
			// publicationBox
			// 
			this.publicationSelectionTableLayoutPanel.SetColumnSpan(this.publicationBox, 2);
			resources.ApplyResources(this.publicationBox, "publicationBox");
			this.publicationBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.publicationBox.FormattingEnabled = true;
			this.publicationBox.Name = "publicationBox";
			this.publicationBox.SelectedIndexChanged += new System.EventHandler(this.publicationBox_SelectedIndexChanged);
			// 
			// configurationLabel
			// 
			resources.ApplyResources(this.configurationLabel, "configurationLabel");
			this.configurationLabel.Name = "configurationLabel";
			// 
			// publicationLabel
			// 
			resources.ApplyResources(this.publicationLabel, "publicationLabel");
			this.publicationLabel.Name = "publicationLabel";
			// 
			// reversalsLabel
			// 
			resources.ApplyResources(this.reversalsLabel, "reversalsLabel");
			this.reversalsLabel.Name = "reversalsLabel";
			// 
			// howManyPubsAlertLabel
			// 
			resources.ApplyResources(this.howManyPubsAlertLabel, "howManyPubsAlertLabel");
			this.publicationSelectionTableLayoutPanel.SetColumnSpan(this.howManyPubsAlertLabel, 3);
			this.howManyPubsAlertLabel.Name = "howManyPubsAlertLabel";
			// 
			// reversalsCheckedListBox
			// 
			this.reversalsCheckedListBox.CheckOnClick = true;
			this.publicationSelectionTableLayoutPanel.SetColumnSpan(this.reversalsCheckedListBox, 2);
			resources.ApplyResources(this.reversalsCheckedListBox, "reversalsCheckedListBox");
			this.reversalsCheckedListBox.FormattingEnabled = true;
			this.reversalsCheckedListBox.Name = "reversalsCheckedListBox";
			this.reversalsCheckedListBox.SelectedIndexChanged += new System.EventHandler(this.reversalsCheckedListBox_SelectedIndexChanged);
			// 
			// publishButton
			// 
			resources.ApplyResources(this.publishButton, "publishButton");
			this.publishButton.Name = "publishButton";
			this.publishButton.UseVisualStyleBackColor = true;
			this.publishButton.Click += new System.EventHandler(this.publishButton_Click);
			// 
			// closeButton
			// 
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.closeButton, "closeButton");
			this.closeButton.Name = "closeButton";
			this.closeButton.UseVisualStyleBackColor = true;
			this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
			// 
			// reportButton
			// 
			resources.ApplyResources(this.reportButton, "reportButton");
			this.reportButton.Name = "reportButton";
			this.reportButton.UseVisualStyleBackColor = true;
			this.reportButton.Click += new System.EventHandler(this.reportButton_Click);
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = true;
			this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
			// 
			// m_progress
			// 
			this.tableLayoutPanel.SetColumnSpan(this.m_progress, 4);
			resources.ApplyResources(this.m_progress, "m_progress");
			this.m_progress.ForeColor = System.Drawing.Color.Lime;
			this.m_progress.MarqueeAnimationSpeed = 25;
			this.m_progress.Name = "m_progress";
			this.m_progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			// 
			// UploadToWebonaryDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.closeButton;
			this.Controls.Add(this.tableLayoutPanel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "UploadToWebonaryDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.webonarySettingsGroupbox.ResumeLayout(false);
			this.settingsForWebonaryTableLayoutPanel.ResumeLayout(false);
			this.settingsForWebonaryTableLayoutPanel.PerformLayout();
			this.publicationGroupBox.ResumeLayout(false);
			this.publicationSelectionTableLayoutPanel.ResumeLayout(false);
			this.publicationSelectionTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.LinkLabel explanationLabel;
		private System.Windows.Forms.GroupBox webonarySettingsGroupbox;
		private System.Windows.Forms.TableLayoutPanel settingsForWebonaryTableLayoutPanel;
		private System.Windows.Forms.TextBox webonaryUsernameTextbox;
		private System.Windows.Forms.TextBox webonarySiteNameTextbox;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.Label usernameLabel;
		private System.Windows.Forms.Label siteNameLabel;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.GroupBox publicationGroupBox;
		private System.Windows.Forms.TableLayoutPanel publicationSelectionTableLayoutPanel;
		private System.Windows.Forms.ComboBox configurationBox;
		private System.Windows.Forms.ComboBox publicationBox;
		private System.Windows.Forms.Label configurationLabel;
		private System.Windows.Forms.Label publicationLabel;
		private System.Windows.Forms.Label reversalsLabel;
		private System.Windows.Forms.Label howManyPubsAlertLabel;
		private System.Windows.Forms.CheckedListBox reversalsCheckedListBox;
		private System.Windows.Forms.Button publishButton;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Button reportButton;
		private Button helpButton;
		private System.Windows.Forms.Label webonarySiteURLLabel;
		private PasswordBox webonaryPasswordTextbox;
		private System.Windows.Forms.CheckBox rememberPasswordCheckbox;
	  private ProgressBar m_progress;
   }
}
