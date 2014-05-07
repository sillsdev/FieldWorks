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
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			this.tableLayoutPanel.ColumnCount = 3;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
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
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 8;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(369, 286);
			this.tableLayoutPanel.TabIndex = 0;
			// 
			// explanationLabel
			// 
			this.explanationLabel.AutoSize = true;
			this.tableLayoutPanel.SetColumnSpan(this.explanationLabel, 3);
			this.explanationLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.explanationLabel.LinkArea = new System.Windows.Forms.LinkArea(45, 24);
			this.explanationLabel.Location = new System.Drawing.Point(3, 0);
			this.explanationLabel.Name = "explanationLabel";
			this.explanationLabel.Size = new System.Drawing.Size(363, 42);
			this.explanationLabel.TabIndex = 9;
			this.explanationLabel.TabStop = true;
			this.explanationLabel.Text = resources.GetString("explanationLabel.Text");
			this.explanationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.explanationLabel.UseCompatibleTextRendering = true;
			// 
			// siteNameLabel
			// 
			this.siteNameLabel.AutoSize = true;
			this.siteNameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.siteNameLabel.Location = new System.Drawing.Point(3, 42);
			this.siteNameLabel.Name = "siteNameLabel";
			this.siteNameLabel.Size = new System.Drawing.Size(115, 26);
			this.siteNameLabel.TabIndex = 1;
			this.siteNameLabel.Text = "Webonary site name:";
			this.siteNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// usernameLabel
			// 
			this.usernameLabel.AutoSize = true;
			this.usernameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.usernameLabel.Location = new System.Drawing.Point(3, 68);
			this.usernameLabel.Name = "usernameLabel";
			this.usernameLabel.Size = new System.Drawing.Size(115, 26);
			this.usernameLabel.TabIndex = 2;
			this.usernameLabel.Text = "Webonary username:";
			this.usernameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// passwordLabel
			// 
			this.passwordLabel.AutoSize = true;
			this.passwordLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.passwordLabel.Location = new System.Drawing.Point(3, 94);
			this.passwordLabel.Name = "passwordLabel";
			this.passwordLabel.Size = new System.Drawing.Size(115, 26);
			this.passwordLabel.TabIndex = 3;
			this.passwordLabel.Text = "Webonary password:";
			this.passwordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// webonarySiteNameTextbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.webonarySiteNameTextbox, 2);
			this.webonarySiteNameTextbox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.webonarySiteNameTextbox.Location = new System.Drawing.Point(124, 45);
			this.webonarySiteNameTextbox.Name = "webonarySiteNameTextbox";
			this.webonarySiteNameTextbox.Size = new System.Drawing.Size(242, 20);
			this.webonarySiteNameTextbox.TabIndex = 0;
			// 
			// webonaryUsernameTextbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.webonaryUsernameTextbox, 2);
			this.webonaryUsernameTextbox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.webonaryUsernameTextbox.Location = new System.Drawing.Point(124, 71);
			this.webonaryUsernameTextbox.Name = "webonaryUsernameTextbox";
			this.webonaryUsernameTextbox.Size = new System.Drawing.Size(242, 20);
			this.webonaryUsernameTextbox.TabIndex = 1;
			// 
			// webonaryPasswordTextbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.webonaryPasswordTextbox, 2);
			this.webonaryPasswordTextbox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.webonaryPasswordTextbox.Location = new System.Drawing.Point(124, 97);
			this.webonaryPasswordTextbox.Name = "webonaryPasswordTextbox";
			this.webonaryPasswordTextbox.PasswordChar = '*';
			this.webonaryPasswordTextbox.Size = new System.Drawing.Size(242, 20);
			this.webonaryPasswordTextbox.TabIndex = 2;
			// 
			// publishButton
			// 
			this.publishButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.publishButton.Location = new System.Drawing.Point(3, 177);
			this.publishButton.Name = "publishButton";
			this.publishButton.Size = new System.Drawing.Size(115, 23);
			this.publishButton.TabIndex = 5;
			this.publishButton.Text = "Publish";
			this.publishButton.UseVisualStyleBackColor = true;
			// 
			// closeButton
			// 
			this.closeButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.closeButton.Location = new System.Drawing.Point(124, 177);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(115, 23);
			this.closeButton.TabIndex = 6;
			this.closeButton.Text = "Close";
			this.closeButton.UseVisualStyleBackColor = true;
			// 
			// outputLogTextbox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.outputLogTextbox, 3);
			this.outputLogTextbox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.outputLogTextbox.Location = new System.Drawing.Point(3, 206);
			this.outputLogTextbox.Multiline = true;
			this.outputLogTextbox.Name = "outputLogTextbox";
			this.outputLogTextbox.ReadOnly = true;
			this.outputLogTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.outputLogTextbox.Size = new System.Drawing.Size(363, 77);
			this.outputLogTextbox.TabIndex = 8;
			this.outputLogTextbox.Text = "(Output log)";
			// 
			// helpButton
			// 
			this.helpButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpButton.Location = new System.Drawing.Point(245, 177);
			this.helpButton.Name = "helpButton";
			this.helpButton.Size = new System.Drawing.Size(121, 23);
			this.helpButton.TabIndex = 7;
			this.helpButton.Text = "Help";
			this.helpButton.UseVisualStyleBackColor = true;
			// 
			// publicationLabel
			// 
			this.publicationLabel.AutoSize = true;
			this.publicationLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.publicationLabel.Location = new System.Drawing.Point(3, 120);
			this.publicationLabel.Name = "publicationLabel";
			this.publicationLabel.Size = new System.Drawing.Size(115, 27);
			this.publicationLabel.TabIndex = 17;
			this.publicationLabel.Text = "Publication:";
			this.publicationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// configurationLabel
			// 
			this.configurationLabel.AutoSize = true;
			this.configurationLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.configurationLabel.Location = new System.Drawing.Point(3, 147);
			this.configurationLabel.Name = "configurationLabel";
			this.configurationLabel.Size = new System.Drawing.Size(115, 27);
			this.configurationLabel.TabIndex = 18;
			this.configurationLabel.Text = "Configuration:";
			this.configurationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// publicationBox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.publicationBox, 2);
			this.publicationBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.publicationBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.publicationBox.FormattingEnabled = true;
			this.publicationBox.Items.AddRange(new object[] {
            "(Main Dictionary Entries)"});
			this.publicationBox.Location = new System.Drawing.Point(124, 123);
			this.publicationBox.Name = "publicationBox";
			this.publicationBox.Size = new System.Drawing.Size(242, 21);
			this.publicationBox.TabIndex = 3;
			// 
			// configurationBox
			// 
			this.tableLayoutPanel.SetColumnSpan(this.configurationBox, 2);
			this.configurationBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.configurationBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.configurationBox.FormattingEnabled = true;
			this.configurationBox.Items.AddRange(new object[] {
            "Stem-based"});
			this.configurationBox.Location = new System.Drawing.Point(124, 150);
			this.configurationBox.Name = "configurationBox";
			this.configurationBox.Size = new System.Drawing.Size(242, 21);
			this.configurationBox.TabIndex = 4;
			// 
			// PublishToWebonaryDlg
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(369, 286);
			this.Controls.Add(this.tableLayoutPanel);
			this.Name = "PublishToWebonaryDlg";
			this.Text = "Publish to Webonary";
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
	}
}