// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Windows.Forms.WritingSystems.WSIdentifiers;

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwWritingSystemSetupDlg
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwWritingSystemSetupDlg));
			this._mainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this._wsListPanel = new System.Windows.Forms.TableLayoutPanel();
			this._writingSystemListPanel = new System.Windows.Forms.SplitContainer();
			this._writingSystemsLabel = new System.Windows.Forms.Label();
			this._writingSystemList = new System.Windows.Forms.CheckedListBox();
			this._help = new System.Windows.Forms.Button();
			this.moveDown = new System.Windows.Forms.Button();
			this.moveUp = new System.Windows.Forms.Button();
			this._addWsButton = new System.Windows.Forms.Button();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this._shareWithSldrCheckbox = new System.Windows.Forms.CheckBox();
			this._languageNameTextbox = new System.Windows.Forms.TextBox();
			this._nameLabel = new System.Windows.Forms.Label();
			this._languageCodeLayout = new System.Windows.Forms.FlowLayoutPanel();
			this._languageLabel = new System.Windows.Forms.Label();
			this._languageCode = new System.Windows.Forms.Label();
			this._changeLanguage = new System.Windows.Forms.LinkLabel();
			this._ethnologueLink = new System.Windows.Forms.LinkLabel();
			this._tabControl = new System.Windows.Forms.TabControl();
			this._generalTab = new System.Windows.Forms.TabPage();
			this._enableAdvanced = new System.Windows.Forms.CheckBox();
			this._identifiersControl = new SIL.Windows.Forms.WritingSystems.WSIdentifiers.WSIdentifierView();
			this._advancedIdentifiersControl = new AdvancedScriptRegionVariantView();
			this._rightToLeftCheckbox = new System.Windows.Forms.CheckBox();
			this.m_FullCode = new System.Windows.Forms.Label();
			this.lblFullCode = new System.Windows.Forms.Label();
			this.lblSpellingDictionary = new System.Windows.Forms.Label();
			this._spellingCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this._fontTab = new System.Windows.Forms.TabPage();
			this._defaultFontControl = new SIL.FieldWorks.FwCoreDlgs.Controls.DefaultFontsControl();
			this._keyboardTab = new System.Windows.Forms.TabPage();
			this._keyboardControl = new SIL.Windows.Forms.WritingSystems.WSKeyboardControl();
			this._sortTab = new System.Windows.Forms.TabPage();
			this._sortControl = new SIL.Windows.Forms.WritingSystems.WSSortControl();
			this._charactersTab = new System.Windows.Forms.TabPage();
			this.m_lblValidCharacters = new System.Windows.Forms.Label();
			this.btnValidChars = new System.Windows.Forms.Button();
			this._numbersTab = new System.Windows.Forms.TabPage();
			this.customDigits = new SIL.FieldWorks.FwCoreDlgs.Controls.CustomDigitEntryControl();
			this.numberSettingsCombo = new System.Windows.Forms.ComboBox();
			this._convertersTab = new System.Windows.Forms.TabPage();
			this.btnEncodingConverter = new System.Windows.Forms.Button();
			this.m_lblEncodingConverter = new System.Windows.Forms.Label();
			this._encodingConverterCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this._helpBtn = new System.Windows.Forms.Button();
			this._cancelBtn = new System.Windows.Forms.Button();
			this._okBtn = new System.Windows.Forms.Button();
			this._addMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			this._mainLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this._wsListPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._writingSystemListPanel)).BeginInit();
			this._writingSystemListPanel.Panel1.SuspendLayout();
			this._writingSystemListPanel.Panel2.SuspendLayout();
			this._writingSystemListPanel.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this._languageCodeLayout.SuspendLayout();
			this._tabControl.SuspendLayout();
			this._generalTab.SuspendLayout();
			this._fontTab.SuspendLayout();
			this._keyboardTab.SuspendLayout();
			this._sortTab.SuspendLayout();
			this._charactersTab.SuspendLayout();
			this._numbersTab.SuspendLayout();
			this._convertersTab.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// _mainLayoutPanel
			// 
			this._mainLayoutPanel.ColumnCount = 1;
			this._mainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this._mainLayoutPanel.Controls.Add(this.splitContainer2, 0, 0);
			this._mainLayoutPanel.Controls.Add(this.flowLayoutPanel1, 0, 1);
			this._mainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._mainLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this._mainLayoutPanel.Name = "_mainLayoutPanel";
			this._mainLayoutPanel.RowCount = 2;
			this._mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this._mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
			this._mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this._mainLayoutPanel.Size = new System.Drawing.Size(891, 609);
			this._mainLayoutPanel.TabIndex = 1;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(3, 3);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this._wsListPanel);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.tableLayoutPanel2);
			this.splitContainer2.Size = new System.Drawing.Size(885, 569);
			this.splitContainer2.SplitterDistance = 228;
			this.splitContainer2.TabIndex = 1;
			// 
			// _wsListPanel
			// 
			this._wsListPanel.ColumnCount = 2;
			this._wsListPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this._wsListPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this._wsListPanel.Controls.Add(this._writingSystemListPanel, 0, 0);
			this._wsListPanel.Controls.Add(this._help, 1, 4);
			this._wsListPanel.Controls.Add(this.moveDown, 1, 3);
			this._wsListPanel.Controls.Add(this.moveUp, 1, 2);
			this._wsListPanel.Controls.Add(this._addWsButton, 1, 1);
			this._wsListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._wsListPanel.Location = new System.Drawing.Point(0, 0);
			this._wsListPanel.Name = "_wsListPanel";
			this._wsListPanel.RowCount = 6;
			this._wsListPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this._wsListPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this._wsListPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this._wsListPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this._wsListPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this._wsListPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this._wsListPanel.Size = new System.Drawing.Size(228, 569);
			this._wsListPanel.TabIndex = 2;
			this._wsListPanel.CellPaint += new System.Windows.Forms.TableLayoutCellPaintEventHandler(this.WsListPanelCellPaint);
			// 
			// _writingSystemListPanel
			// 
			this._writingSystemListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._writingSystemListPanel.Location = new System.Drawing.Point(3, 3);
			this._writingSystemListPanel.Name = "_writingSystemListPanel";
			this._writingSystemListPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _writingSystemListPanel.Panel1
			// 
			this._writingSystemListPanel.Panel1.Controls.Add(this._writingSystemsLabel);
			// 
			// _writingSystemListPanel.Panel2
			// 
			this._writingSystemListPanel.Panel2.Controls.Add(this._writingSystemList);
			this._wsListPanel.SetRowSpan(this._writingSystemListPanel, 6);
			this._writingSystemListPanel.Size = new System.Drawing.Size(184, 563);
			this._writingSystemListPanel.SplitterDistance = 25;
			this._writingSystemListPanel.TabIndex = 10;
			// 
			// _writingSystemsLabel
			// 
			this._writingSystemsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._writingSystemsLabel.AutoSize = true;
			this._writingSystemsLabel.Location = new System.Drawing.Point(0, 9);
			this._writingSystemsLabel.Name = "_writingSystemsLabel";
			this._writingSystemsLabel.Size = new System.Drawing.Size(85, 13);
			this._writingSystemsLabel.TabIndex = 0;
			this._writingSystemsLabel.Text = "Writing Systems:";
			// 
			// _writingSystemList
			// 
			this._writingSystemList.Dock = System.Windows.Forms.DockStyle.Fill;
			this._writingSystemList.FormattingEnabled = true;
			this._writingSystemList.Location = new System.Drawing.Point(0, 0);
			this._writingSystemList.Name = "_writingSystemList";
			this._writingSystemList.Size = new System.Drawing.Size(184, 534);
			this._writingSystemList.TabIndex = 8;
			this._writingSystemList.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.WritingSystemListItemCheck);
			this._writingSystemList.SelectedIndexChanged += new System.EventHandler(this.WritingSystemListSelectedIndexChanged);
			this._writingSystemList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.WritingSystemListMouseDown);
			// 
			// _help
			// 
			this._help.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this._help.AutoSize = true;
			this._help.Image = ((System.Drawing.Image)(resources.GetObject("_help.Image")));
			this._help.Location = new System.Drawing.Point(193, 147);
			this._help.Name = "_help";
			this._help.Size = new System.Drawing.Size(32, 32);
			this._help.TabIndex = 9;
			this._help.UseVisualStyleBackColor = true;
			this._help.Click += new System.EventHandler(this.WritingListHelpClick);
			// 
			// moveDown
			// 
			this.moveDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
			this.moveDown.Image = ((System.Drawing.Image)(resources.GetObject("moveDown.Image")));
			this.moveDown.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.moveDown.Location = new System.Drawing.Point(193, 109);
			this.moveDown.Name = "moveDown";
			this.moveDown.Size = new System.Drawing.Size(32, 32);
			this.moveDown.TabIndex = 2;
			this.moveDown.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.moveDown.UseVisualStyleBackColor = true;
			this.moveDown.Click += new System.EventHandler(this.MoveDownClick);
			// 
			// moveUp
			// 
			this.moveUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
			this.moveUp.Image = ((System.Drawing.Image)(resources.GetObject("moveUp.Image")));
			this.moveUp.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.moveUp.Location = new System.Drawing.Point(193, 71);
			this.moveUp.Name = "moveUp";
			this.moveUp.Size = new System.Drawing.Size(32, 32);
			this.moveUp.TabIndex = 1;
			this.moveUp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.moveUp.UseVisualStyleBackColor = true;
			this.moveUp.Click += new System.EventHandler(this.MoveUpClick);
			// 
			// _addWsButton
			// 
			this._addWsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this._addWsButton.AutoSize = true;
			this._addWsButton.Image = ((System.Drawing.Image)(resources.GetObject("_addWsButton.Image")));
			this._addWsButton.Location = new System.Drawing.Point(193, 33);
			this._addWsButton.Name = "_addWsButton";
			this._addWsButton.Size = new System.Drawing.Size(32, 32);
			this._addWsButton.TabIndex = 8;
			this._addWsButton.UseVisualStyleBackColor = true;
			this._addWsButton.Click += new System.EventHandler(this.AddWsButtonClick);
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.ColumnCount = 1;
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel1, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this._tabControl, 0, 1);
			this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 2;
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.60232F));
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 83.39768F));
			this.tableLayoutPanel2.Size = new System.Drawing.Size(653, 569);
			this.tableLayoutPanel2.TabIndex = 0;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 250F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Controls.Add(this._shareWithSldrCheckbox, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this._languageNameTextbox, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this._nameLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this._languageCodeLayout, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this._ethnologueLink, 1, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(647, 88);
			this.tableLayoutPanel1.TabIndex = 3;
			// 
			// _shareWithSldrCheckbox
			// 
			this._shareWithSldrCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._shareWithSldrCheckbox.AutoSize = true;
			this._shareWithSldrCheckbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this._shareWithSldrCheckbox.Location = new System.Drawing.Point(250, 8);
			this._shareWithSldrCheckbox.Margin = new System.Windows.Forms.Padding(0);
			this._shareWithSldrCheckbox.Name = "_shareWithSldrCheckbox";
			this._shareWithSldrCheckbox.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._shareWithSldrCheckbox.Size = new System.Drawing.Size(200, 17);
			this._shareWithSldrCheckbox.TabIndex = 9;
			this._shareWithSldrCheckbox.Text = "Share writing system data with SLDR";
			this._shareWithSldrCheckbox.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			this._shareWithSldrCheckbox.UseVisualStyleBackColor = true;
			// 
			// _languageNameTextbox
			// 
			this._languageNameTextbox.Location = new System.Drawing.Point(3, 28);
			this._languageNameTextbox.Name = "_languageNameTextbox";
			this._languageNameTextbox.Size = new System.Drawing.Size(162, 20);
			this._languageNameTextbox.TabIndex = 6;
			// 
			// _nameLabel
			// 
			this._nameLabel.AutoSize = true;
			this._nameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._nameLabel.Location = new System.Drawing.Point(3, 0);
			this._nameLabel.Name = "_nameLabel";
			this._nameLabel.Size = new System.Drawing.Size(244, 25);
			this._nameLabel.TabIndex = 5;
			this._nameLabel.Text = "Language Name:";
			this._nameLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// _languageCodeLayout
			// 
			this._languageCodeLayout.Controls.Add(this._languageLabel);
			this._languageCodeLayout.Controls.Add(this._languageCode);
			this._languageCodeLayout.Controls.Add(this._changeLanguage);
			this._languageCodeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this._languageCodeLayout.Location = new System.Drawing.Point(0, 53);
			this._languageCodeLayout.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this._languageCodeLayout.Name = "_languageCodeLayout";
			this._languageCodeLayout.Size = new System.Drawing.Size(250, 30);
			this._languageCodeLayout.TabIndex = 8;
			// 
			// _languageLabel
			// 
			this._languageLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this._languageLabel.AutoSize = true;
			this._languageLabel.Location = new System.Drawing.Point(0, 0);
			this._languageLabel.Margin = new System.Windows.Forms.Padding(0);
			this._languageLabel.Name = "_languageLabel";
			this._languageLabel.Size = new System.Drawing.Size(86, 13);
			this._languageLabel.TabIndex = 0;
			this._languageLabel.Text = "Language Code:";
			this._languageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _languageCode
			// 
			this._languageCode.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this._languageCode.AutoSize = true;
			this._languageCode.Location = new System.Drawing.Point(86, 0);
			this._languageCode.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this._languageCode.Name = "_languageCode";
			this._languageCode.Size = new System.Drawing.Size(25, 13);
			this._languageCode.TabIndex = 1;
			this._languageCode.Text = "eng";
			this._languageCode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _changeLanguage
			// 
			this._changeLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._changeLanguage.Location = new System.Drawing.Point(114, 0);
			this._changeLanguage.Margin = new System.Windows.Forms.Padding(0);
			this._changeLanguage.Name = "_changeLanguage";
			this._changeLanguage.Size = new System.Drawing.Size(53, 13);
			this._changeLanguage.TabIndex = 7;
			this._changeLanguage.TabStop = true;
			this._changeLanguage.Text = "Change...";
			this._changeLanguage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ChangeCodeLinkClick);
			// 
			// _ethnologueLink
			// 
			this._ethnologueLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._ethnologueLink.AutoSize = true;
			this._ethnologueLink.Location = new System.Drawing.Point(250, 37);
			this._ethnologueLink.Margin = new System.Windows.Forms.Padding(0);
			this._ethnologueLink.Name = "_ethnologueLink";
			this._ethnologueLink.Size = new System.Drawing.Size(119, 13);
			this._ethnologueLink.TabIndex = 4;
			this._ethnologueLink.TabStop = true;
			this._ethnologueLink.Text = "&Ethnologue entry for {0}";
			this._ethnologueLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.EthnologueLinkClicked);
			// 
			// _tabControl
			// 
			this._tabControl.Controls.Add(this._generalTab);
			this._tabControl.Controls.Add(this._fontTab);
			this._tabControl.Controls.Add(this._keyboardTab);
			this._tabControl.Controls.Add(this._sortTab);
			this._tabControl.Controls.Add(this._charactersTab);
			this._tabControl.Controls.Add(this._numbersTab);
			this._tabControl.Controls.Add(this._convertersTab);
			this._tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._tabControl.Location = new System.Drawing.Point(3, 97);
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			this._tabControl.Size = new System.Drawing.Size(647, 469);
			this._tabControl.TabIndex = 2;
			// 
			// _generalTab
			// 
			this._generalTab.AccessibleDescription = "Set codes and right to left";
			this._generalTab.AccessibleName = "General";
			this._generalTab.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._generalTab.Controls.Add(this._enableAdvanced);
			this._generalTab.Controls.Add(this._identifiersControl);
			this._generalTab.Controls.Add(this._rightToLeftCheckbox);
			this._generalTab.Controls.Add(this.m_FullCode);
			this._generalTab.Controls.Add(this.lblFullCode);
			this._generalTab.Controls.Add(this.lblSpellingDictionary);
			this._generalTab.Controls.Add(this._spellingCombo);
			this._generalTab.Location = new System.Drawing.Point(4, 22);
			this._generalTab.Name = "_generalTab";
			this._generalTab.Padding = new System.Windows.Forms.Padding(3);
			this._generalTab.Size = new System.Drawing.Size(639, 443);
			this._generalTab.TabIndex = 0;
			this._generalTab.Text = "General";
			// 
			// _enableAdvanced
			// 
			this._enableAdvanced.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._enableAdvanced.AutoSize = true;
			this._enableAdvanced.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this._enableAdvanced.Location = new System.Drawing.Point(17, 410);
			this._enableAdvanced.Name = "_enableAdvanced";
			this._enableAdvanced.Size = new System.Drawing.Size(75, 17);
			this._enableAdvanced.TabIndex = 21;
			this._enableAdvanced.Text = "Advanced";
			this._enableAdvanced.UseVisualStyleBackColor = true;
			// 
			// _identifiersControl
			// 
			this._identifiersControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this._identifiersControl.Location = new System.Drawing.Point(1, 111);
			this._identifiersControl.Name = "_identifiersControl";
			this._identifiersControl.Size = new System.Drawing.Size(632, 276);
			this._identifiersControl.TabIndex = 20;
			// 
			// _advancedIdentifiersControl
			// 
			this._advancedIdentifiersControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this._advancedIdentifiersControl.Location = new System.Drawing.Point(1, 121);
			this._advancedIdentifiersControl.Name = "_advancedIdentifiersControl";
			this._advancedIdentifiersControl.Size = new System.Drawing.Size(632, 276);
			this._advancedIdentifiersControl.TabIndex = 20;
			// 
			// _rightToLeftCheckbox
			// 
			this._rightToLeftCheckbox.AutoSize = true;
			this._rightToLeftCheckbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this._rightToLeftCheckbox.Location = new System.Drawing.Point(13, 93);
			this._rightToLeftCheckbox.Name = "_rightToLeftCheckbox";
			this._rightToLeftCheckbox.Size = new System.Drawing.Size(80, 17);
			this._rightToLeftCheckbox.TabIndex = 19;
			this._rightToLeftCheckbox.Text = "Right-to-left";
			this._rightToLeftCheckbox.UseVisualStyleBackColor = true;
			this._rightToLeftCheckbox.CheckedChanged += new System.EventHandler(this.RightToLeftCheckChanged);
			// 
			// m_FullCode
			// 
			this.m_FullCode.AutoSize = true;
			this.m_FullCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_FullCode.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_FullCode.Location = new System.Drawing.Point(55, 7);
			this.m_FullCode.Name = "m_FullCode";
			this.m_FullCode.Size = new System.Drawing.Size(119, 17);
			this.m_FullCode.TabIndex = 17;
			this.m_FullCode.Text = "CurrentFullLocale";
			// 
			// lblFullCode
			// 
			this.lblFullCode.AutoSize = true;
			this.lblFullCode.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.lblFullCode.Location = new System.Drawing.Point(14, 9);
			this.lblFullCode.Name = "lblFullCode";
			this.lblFullCode.Size = new System.Drawing.Size(41, 13);
			this.lblFullCode.TabIndex = 16;
			this.lblFullCode.Text = "Code:  ";
			// 
			// lblSpellingDictionary
			// 
			this.lblSpellingDictionary.AutoSize = true;
			this.lblSpellingDictionary.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.lblSpellingDictionary.Location = new System.Drawing.Point(14, 37);
			this.lblSpellingDictionary.Name = "lblSpellingDictionary";
			this.lblSpellingDictionary.Size = new System.Drawing.Size(95, 13);
			this.lblSpellingDictionary.TabIndex = 15;
			this.lblSpellingDictionary.Text = "Spelling dictionary:";
			// 
			// _spellingCombo
			// 
			this._spellingCombo.AllowSpaceInEditBox = false;
			this._spellingCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._spellingCombo.FormattingEnabled = true;
			this._spellingCombo.Location = new System.Drawing.Point(17, 56);
			this._spellingCombo.Name = "_spellingCombo";
			this._spellingCombo.Size = new System.Drawing.Size(183, 21);
			this._spellingCombo.TabIndex = 18;
			// 
			// _fontTab
			// 
			this._fontTab.AccessibleDescription = "Change default font and set font options";
			this._fontTab.AccessibleName = "Font";
			this._fontTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._fontTab.Controls.Add(this._defaultFontControl);
			this._fontTab.Location = new System.Drawing.Point(4, 22);
			this._fontTab.Name = "_fontTab";
			this._fontTab.Padding = new System.Windows.Forms.Padding(3);
			this._fontTab.Size = new System.Drawing.Size(639, 443);
			this._fontTab.TabIndex = 1;
			this._fontTab.Text = "Font";
			this._fontTab.UseVisualStyleBackColor = true;
			// 
			// _defaultFontControl
			// 
			this._defaultFontControl.DefaultNormalFont = "";
			this._defaultFontControl.Location = new System.Drawing.Point(3, 3);
			this._defaultFontControl.Name = "_defaultFontControl";
			this._defaultFontControl.Size = new System.Drawing.Size(291, 140);
			this._defaultFontControl.TabIndex = 0;
			this._defaultFontControl.WritingSystem = null;
			// 
			// _keyboardTab
			// 
			this._keyboardTab.AccessibleDescription = "Set the keyboard to use for this writing system";
			this._keyboardTab.AccessibleName = "Keyboard";
			this._keyboardTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._keyboardTab.Controls.Add(this._keyboardControl);
			this._keyboardTab.Location = new System.Drawing.Point(4, 22);
			this._keyboardTab.Name = "_keyboardTab";
			this._keyboardTab.Padding = new System.Windows.Forms.Padding(3);
			this._keyboardTab.Size = new System.Drawing.Size(639, 443);
			this._keyboardTab.TabIndex = 2;
			this._keyboardTab.Text = "Keyboard";
			this._keyboardTab.UseVisualStyleBackColor = true;
			// 
			// _keyboardControl
			// 
			this._keyboardControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._keyboardControl.Location = new System.Drawing.Point(3, 3);
			this._keyboardControl.Name = "_keyboardControl";
			this._keyboardControl.Size = new System.Drawing.Size(633, 437);
			this._keyboardControl.TabIndex = 1;
			// 
			// _sortTab
			// 
			this._sortTab.AccessibleDescription = "Set sorting rules for this writing system";
			this._sortTab.AccessibleName = "Sorting";
			this._sortTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._sortTab.Controls.Add(this._sortControl);
			this._sortTab.Location = new System.Drawing.Point(4, 22);
			this._sortTab.Name = "_sortTab";
			this._sortTab.Padding = new System.Windows.Forms.Padding(3);
			this._sortTab.Size = new System.Drawing.Size(639, 443);
			this._sortTab.TabIndex = 3;
			this._sortTab.Text = "Sorting";
			this._sortTab.UseVisualStyleBackColor = true;
			// 
			// _sortControl
			// 
			this._sortControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._sortControl.Location = new System.Drawing.Point(3, 3);
			this._sortControl.Name = "_sortControl";
			this._sortControl.Size = new System.Drawing.Size(633, 437);
			this._sortControl.TabIndex = 1;
			// 
			// _charactersTab
			// 
			this._charactersTab.AccessibleDescription = "Set valid character types for this writing system";
			this._charactersTab.AccessibleName = "Characters";
			this._charactersTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._charactersTab.Controls.Add(this.m_lblValidCharacters);
			this._charactersTab.Controls.Add(this.btnValidChars);
			this._charactersTab.Location = new System.Drawing.Point(4, 22);
			this._charactersTab.Name = "_charactersTab";
			this._charactersTab.Padding = new System.Windows.Forms.Padding(3);
			this._charactersTab.Size = new System.Drawing.Size(639, 443);
			this._charactersTab.TabIndex = 4;
			this._charactersTab.Text = "Characters";
			this._charactersTab.UseVisualStyleBackColor = true;
			// 
			// m_lblValidCharacters
			// 
			this.m_lblValidCharacters.AutoSize = true;
			this.m_lblValidCharacters.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_lblValidCharacters.Location = new System.Drawing.Point(14, 11);
			this.m_lblValidCharacters.Name = "m_lblValidCharacters";
			this.m_lblValidCharacters.Size = new System.Drawing.Size(115, 13);
			this.m_lblValidCharacters.TabIndex = 12;
			this.m_lblValidCharacters.Text = "Valid characters for {0}";
			// 
			// btnValidChars
			// 
			this.btnValidChars.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.btnValidChars.Location = new System.Drawing.Point(17, 30);
			this.btnValidChars.Name = "btnValidChars";
			this.btnValidChars.Size = new System.Drawing.Size(116, 22);
			this.btnValidChars.TabIndex = 13;
			this.btnValidChars.Text = "&Valid Characters...";
			this.btnValidChars.Click += new System.EventHandler(this.OnValidCharsButtonClick);
			// 
			// _numbersTab
			// 
			this._numbersTab.AccessibleDescription = "Set numbering system to use for this writing system";
			this._numbersTab.AccessibleName = "Numbers";
			this._numbersTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._numbersTab.Controls.Add(this.customDigits);
			this._numbersTab.Controls.Add(this.numberSettingsCombo);
			this._numbersTab.Location = new System.Drawing.Point(4, 22);
			this._numbersTab.Name = "_numbersTab";
			this._numbersTab.Padding = new System.Windows.Forms.Padding(3);
			this._numbersTab.Size = new System.Drawing.Size(639, 443);
			this._numbersTab.TabIndex = 5;
			this._numbersTab.Text = "Numbers";
			this._numbersTab.UseVisualStyleBackColor = true;
			// 
			// customDigits
			// 
			this.customDigits.AutoScroll = true;
			this.customDigits.Location = new System.Drawing.Point(11, 38);
			this.customDigits.Name = "customDigits";
			this.customDigits.Size = new System.Drawing.Size(432, 65);
			this.customDigits.TabIndex = 4;
			this.customDigits.WrapContents = false;
			// 
			// numberSettingsCombo
			// 
			this.numberSettingsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.numberSettingsCombo.FormattingEnabled = true;
			this.numberSettingsCombo.Location = new System.Drawing.Point(11, 11);
			this.numberSettingsCombo.Name = "numberSettingsCombo";
			this.numberSettingsCombo.Size = new System.Drawing.Size(240, 21);
			this.numberSettingsCombo.TabIndex = 3;
			// 
			// _convertersTab
			// 
			this._convertersTab.AccessibleDescription = "Set encoding converter to use when importing data in this writing system";
			this._convertersTab.AccessibleName = "Converters";
			this._convertersTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._convertersTab.Controls.Add(this.btnEncodingConverter);
			this._convertersTab.Controls.Add(this.m_lblEncodingConverter);
			this._convertersTab.Controls.Add(this._encodingConverterCombo);
			this._convertersTab.Location = new System.Drawing.Point(4, 22);
			this._convertersTab.Name = "_convertersTab";
			this._convertersTab.Padding = new System.Windows.Forms.Padding(3);
			this._convertersTab.Size = new System.Drawing.Size(639, 443);
			this._convertersTab.TabIndex = 6;
			this._convertersTab.Text = "Converters";
			this._convertersTab.UseVisualStyleBackColor = true;
			// 
			// btnEncodingConverter
			// 
			this.btnEncodingConverter.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.btnEncodingConverter.Location = new System.Drawing.Point(193, 33);
			this.btnEncodingConverter.Name = "btnEncodingConverter";
			this.btnEncodingConverter.Size = new System.Drawing.Size(75, 23);
			this.btnEncodingConverter.TabIndex = 13;
			this.btnEncodingConverter.Text = "&More...";
			this.btnEncodingConverter.Click += new System.EventHandler(this.EncodingConverterButtonClick);
			// 
			// m_lblEncodingConverter
			// 
			this.m_lblEncodingConverter.AutoSize = true;
			this.m_lblEncodingConverter.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_lblEncodingConverter.Location = new System.Drawing.Point(12, 10);
			this.m_lblEncodingConverter.Name = "m_lblEncodingConverter";
			this.m_lblEncodingConverter.Size = new System.Drawing.Size(149, 13);
			this.m_lblEncodingConverter.TabIndex = 11;
			this.m_lblEncodingConverter.Text = "Set up input converters for {0}";
			// 
			// _encodingConverterCombo
			// 
			this._encodingConverterCombo.AllowSpaceInEditBox = false;
			this._encodingConverterCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._encodingConverterCombo.Location = new System.Drawing.Point(17, 34);
			this._encodingConverterCombo.Name = "_encodingConverterCombo";
			this._encodingConverterCombo.Size = new System.Drawing.Size(168, 21);
			this._encodingConverterCombo.Sorted = true;
			this._encodingConverterCombo.TabIndex = 12;
			this._encodingConverterCombo.SelectedIndexChanged += new System.EventHandler(this.EncodingConverterComboSelectedIndexChanged);
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this._helpBtn);
			this.flowLayoutPanel1.Controls.Add(this._cancelBtn);
			this.flowLayoutPanel1.Controls.Add(this._okBtn);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 578);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(885, 28);
			this.flowLayoutPanel1.TabIndex = 2;
			// 
			// _helpBtn
			// 
			this._helpBtn.Location = new System.Drawing.Point(807, 3);
			this._helpBtn.Name = "_helpBtn";
			this._helpBtn.Size = new System.Drawing.Size(75, 23);
			this._helpBtn.TabIndex = 0;
			this._helpBtn.Text = "Help";
			this._helpBtn.UseVisualStyleBackColor = true;
			this._helpBtn.Click += new System.EventHandler(this.FormHelpClick);
			// 
			// _cancelBtn
			// 
			this._cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._cancelBtn.Location = new System.Drawing.Point(726, 3);
			this._cancelBtn.Name = "_cancelBtn";
			this._cancelBtn.Size = new System.Drawing.Size(75, 23);
			this._cancelBtn.TabIndex = 1;
			this._cancelBtn.Text = "Cancel";
			this._cancelBtn.UseVisualStyleBackColor = true;
			// 
			// _okBtn
			// 
			this._okBtn.Location = new System.Drawing.Point(645, 3);
			this._okBtn.Name = "_okBtn";
			this._okBtn.Size = new System.Drawing.Size(75, 23);
			this._okBtn.TabIndex = 2;
			this._okBtn.Text = "OK";
			this._okBtn.UseVisualStyleBackColor = true;
			this._okBtn.Click += new System.EventHandler(this.OkButtonClick);
			// 
			// _addMenuStrip
			// 
			this._addMenuStrip.Name = "_addMenuStrip";
			this._addMenuStrip.Size = new System.Drawing.Size(61, 4);
			// 
			// FwWritingSystemSetupDlg
			// 
			this.AcceptButton = this._okBtn;
			this.AccessibleDescription = "Used to change settings related to languages and their use in FieldWorks";
			this.AccessibleName = "Writing System Properties";
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._cancelBtn;
			this.ClientSize = new System.Drawing.Size(891, 609);
			this.ControlBox = false;
			this.Controls.Add(this._mainLayoutPanel);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(800, 620);
			this.Name = "FwWritingSystemSetupDlg";
			this.ShowIcon = false;
			this.Text = "Writing System Properties";
			this._mainLayoutPanel.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this._wsListPanel.ResumeLayout(false);
			this._wsListPanel.PerformLayout();
			this._writingSystemListPanel.Panel1.ResumeLayout(false);
			this._writingSystemListPanel.Panel1.PerformLayout();
			this._writingSystemListPanel.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._writingSystemListPanel)).EndInit();
			this._writingSystemListPanel.ResumeLayout(false);
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this._languageCodeLayout.ResumeLayout(false);
			this._languageCodeLayout.PerformLayout();
			this._tabControl.ResumeLayout(false);
			this._generalTab.ResumeLayout(false);
			this._generalTab.PerformLayout();
			this._fontTab.ResumeLayout(false);
			this._keyboardTab.ResumeLayout(false);
			this._sortTab.ResumeLayout(false);
			this._charactersTab.ResumeLayout(false);
			this._charactersTab.PerformLayout();
			this._numbersTab.ResumeLayout(false);
			this._convertersTab.ResumeLayout(false);
			this._convertersTab.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel _mainLayoutPanel;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.TableLayoutPanel _wsListPanel;
		private System.Windows.Forms.Button moveDown;
		private System.Windows.Forms.Button moveUp;
		private System.Windows.Forms.Button _addWsButton;
		private System.Windows.Forms.Button _help;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Button _helpBtn;
		private System.Windows.Forms.Button _cancelBtn;
		private System.Windows.Forms.Button _okBtn;
		private System.Windows.Forms.TabPage _keyboardTab;
		private System.Windows.Forms.TabPage _sortTab;
		private System.Windows.Forms.TabPage _charactersTab;
		private System.Windows.Forms.TabPage _numbersTab;
		private System.Windows.Forms.TabPage _convertersTab;
		private System.Windows.Forms.Label m_lblValidCharacters;
		private System.Windows.Forms.Button btnValidChars;
		private System.Windows.Forms.Button btnEncodingConverter;
		private System.Windows.Forms.Label m_lblEncodingConverter;
		private Common.Controls.FwOverrideComboBox _encodingConverterCombo;
		private Windows.Forms.WritingSystems.WSKeyboardControl _keyboardControl;
		private Windows.Forms.WritingSystems.WSSortControl _sortControl;
		private SIL.FieldWorks.FwCoreDlgs.Controls.CustomDigitEntryControl customDigits;
		private System.Windows.Forms.ComboBox numberSettingsCombo;
		private System.Windows.Forms.ContextMenuStrip _addMenuStrip;
		private System.Windows.Forms.ToolTip _toolTip;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.LinkLabel _ethnologueLink;
		private System.Windows.Forms.TextBox _languageNameTextbox;
		private System.Windows.Forms.Label _nameLabel;
		private System.Windows.Forms.FlowLayoutPanel _languageCodeLayout;
		private System.Windows.Forms.Label _languageLabel;
		private System.Windows.Forms.Label _languageCode;
		private System.Windows.Forms.LinkLabel _changeLanguage;
		private System.Windows.Forms.TabControl _tabControl;
		private System.Windows.Forms.TabPage _generalTab;
		private WSIdentifierView _identifiersControl;
		private AdvancedScriptRegionVariantView _advancedIdentifiersControl;
		private System.Windows.Forms.CheckBox _rightToLeftCheckbox;
		private System.Windows.Forms.Label m_FullCode;
		private System.Windows.Forms.Label lblFullCode;
		private System.Windows.Forms.Label lblSpellingDictionary;
		private Common.Controls.FwOverrideComboBox _spellingCombo;
		private System.Windows.Forms.TabPage _fontTab;
		private SIL.FieldWorks.FwCoreDlgs.Controls.DefaultFontsControl _defaultFontControl;
		private System.Windows.Forms.CheckBox _shareWithSldrCheckbox;
		private System.Windows.Forms.SplitContainer _writingSystemListPanel;
		private System.Windows.Forms.Label _writingSystemsLabel;
		private System.Windows.Forms.CheckedListBox _writingSystemList;
		private System.Windows.Forms.CheckBox _enableAdvanced;
	}
}