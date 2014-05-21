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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PublishToWebonaryDlg));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.explanationLabel = new System.Windows.Forms.LinkLabel();
			this.siteNameLabel = new System.Windows.Forms.Label();
			this.usernameLabel = new System.Windows.Forms.Label();
			this.passwordLabel = new System.Windows.Forms.Label();
			this.webonarySiteNameTextbox = new System.Windows.Forms.TextBox();
			this.webonaryUsernameTextbox = new System.Windows.Forms.TextBox();
			this.webonaryPasswordTextbox = new System.Windows.Forms.TextBox();
			this.publishButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.outputLogTextbox = new System.Windows.Forms.TextBox();
			this.helpButton = new System.Windows.Forms.Button();
			this.publicationLabel = new System.Windows.Forms.Label();
			this.configurationLabel = new System.Windows.Forms.Label();
			this.publicationBox = new System.Windows.Forms.ComboBox();
			this.configurationBox = new System.Windows.Forms.ComboBox();
			this.showPasswordCheckBox = new System.Windows.Forms.CheckBox();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
			this.tableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.siteNameLabel, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.usernameLabel, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.passwordLabel, 0, 3);
			this.tableLayoutPanel.Controls.Add(this.webonarySiteNameTextbox, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.webonaryUsernameTextbox, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.webonaryPasswordTextbox, 1, 3);
			this.tableLayoutPanel.Controls.Add(this.publishButton, 0, 6);
			this.tableLayoutPanel.Controls.Add(this.closeButton, 1, 6);
			this.tableLayoutPanel.Controls.Add(this.outputLogTextbox, 0, 7);
			this.tableLayoutPanel.Controls.Add(this.helpButton, 2, 6);
			this.tableLayoutPanel.Controls.Add(this.publicationLabel, 0, 4);
			this.tableLayoutPanel.Controls.Add(this.configurationLabel, 0, 5);
			this.tableLayoutPanel.Controls.Add(this.publicationBox, 1, 4);
			this.tableLayoutPanel.Controls.Add(this.configurationBox, 1, 5);
			this.tableLayoutPanel.Controls.Add(this.showPasswordCheckBox, 2, 3);
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
			// siteNameLabel
			// 
			resources.ApplyResources(this.siteNameLabel, "siteNameLabel");
			this.siteNameLabel.Name = "siteNameLabel";
			// 
			// usernameLabel
			// 
			resources.ApplyResources(this.usernameLabel, "usernameLabel");
			this.usernameLabel.Name = "usernameLabel";
			// 
			// passwordLabel
			// 
			resources.ApplyResources(this.passwordLabel, "passwordLabel");
			this.passwordLabel.Name = "passwordLabel";
			// 
			// webonarySiteNameTextbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.webonarySiteNameTextbox, 2);
			resources.ApplyResources(this.webonarySiteNameTextbox, "webonarySiteNameTextbox");
			this.webonarySiteNameTextbox.Name = "webonarySiteNameTextbox";
			// 
			// webonaryUsernameTextbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.webonaryUsernameTextbox, 2);
			resources.ApplyResources(this.webonaryUsernameTextbox, "webonaryUsernameTextbox");
			this.webonaryUsernameTextbox.Name = "webonaryUsernameTextbox";
			// 
			// webonaryPasswordTextbox
			// 
			resources.ApplyResources(this.webonaryPasswordTextbox, "webonaryPasswordTextbox");
			this.webonaryPasswordTextbox.Name = "webonaryPasswordTextbox";
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
			// showPasswordCheckBox
			// 
			resources.ApplyResources(this.showPasswordCheckBox, "showPasswordCheckBox");
			this.showPasswordCheckBox.Name = "showPasswordCheckBox";
			this.showPasswordCheckBox.UseVisualStyleBackColor = true;
			this.showPasswordCheckBox.CheckedChanged += new System.EventHandler(this.showPasswordCheckBox_CheckedChanged);
			// 
			// PublishToWebonaryDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel);
			this.Name = "PublishToWebonaryDlg";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.LinkLabel explanationLabel;
		private System.Windows.Forms.Label siteNameLabel;
		private System.Windows.Forms.Label usernameLabel;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.TextBox webonarySiteNameTextbox;
		private System.Windows.Forms.TextBox webonaryUsernameTextbox;
		private System.Windows.Forms.TextBox webonaryPasswordTextbox;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.TextBox outputLogTextbox;
		private System.Windows.Forms.Button publishButton;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Label publicationLabel;
		private System.Windows.Forms.Label configurationLabel;
		private System.Windows.Forms.ComboBox publicationBox;
		private System.Windows.Forms.ComboBox configurationBox;
		private System.Windows.Forms.CheckBox showPasswordCheckBox;
	}
}