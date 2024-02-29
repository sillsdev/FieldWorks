// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
			this._advancedIdentifiersControl = new SIL.FieldWorks.FwCoreDlgs.AdvancedScriptRegionVariantView();
			this._rightToLeftCheckbox = new System.Windows.Forms.CheckBox();
			this.m_FullCode = new System.Windows.Forms.Label();
			this.lblFullCode = new System.Windows.Forms.Label();
			this.lblSpellingDictionary = new System.Windows.Forms.Label();
			this._spellingCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this._fontTab = new System.Windows.Forms.TabPage();
			this._defaultFontControl = new SIL.FieldWorks.FwCoreDlgControls.DefaultFontsControl();
			this._keyboardTab = new System.Windows.Forms.TabPage();
			this._keyboardControl = new SIL.Windows.Forms.WritingSystems.WSKeyboardControl();
			this._sortTab = new System.Windows.Forms.TabPage();
			this._sortControl = new SIL.Windows.Forms.WritingSystems.WSSortControl();
			this._charactersTab = new System.Windows.Forms.TabPage();
			this.m_lblValidCharacters = new System.Windows.Forms.Label();
			this.btnValidChars = new System.Windows.Forms.Button();
			this._numbersTab = new System.Windows.Forms.TabPage();
			this.customDigits = new SIL.FieldWorks.Common.Widgets.CustomDigitEntryControl();
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
			resources.ApplyResources(this._mainLayoutPanel, "_mainLayoutPanel");
			this._mainLayoutPanel.Controls.Add(this.splitContainer2, 0, 0);
			this._mainLayoutPanel.Controls.Add(this.flowLayoutPanel1, 0, 1);
			this._mainLayoutPanel.Name = "_mainLayoutPanel";
			// 
			// splitContainer2
			// 
			resources.ApplyResources(this.splitContainer2, "splitContainer2");
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this._wsListPanel);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.tableLayoutPanel2);
			// 
			// _wsListPanel
			// 
			resources.ApplyResources(this._wsListPanel, "_wsListPanel");
			this._wsListPanel.Controls.Add(this._writingSystemListPanel, 0, 0);
			this._wsListPanel.Controls.Add(this._help, 1, 4);
			this._wsListPanel.Controls.Add(this.moveDown, 1, 3);
			this._wsListPanel.Controls.Add(this.moveUp, 1, 2);
			this._wsListPanel.Controls.Add(this._addWsButton, 1, 1);
			this._wsListPanel.Name = "_wsListPanel";
			this._wsListPanel.CellPaint += new System.Windows.Forms.TableLayoutCellPaintEventHandler(this.WsListPanelCellPaint);
			// 
			// _writingSystemListPanel
			// 
			resources.ApplyResources(this._writingSystemListPanel, "_writingSystemListPanel");
			this._writingSystemListPanel.Name = "_writingSystemListPanel";
			// 
			// _writingSystemListPanel.Panel1
			// 
			this._writingSystemListPanel.Panel1.Controls.Add(this._writingSystemsLabel);
			// 
			// _writingSystemListPanel.Panel2
			// 
			this._writingSystemListPanel.Panel2.Controls.Add(this._writingSystemList);
			this._wsListPanel.SetRowSpan(this._writingSystemListPanel, 6);
			// 
			// _writingSystemsLabel
			// 
			resources.ApplyResources(this._writingSystemsLabel, "_writingSystemsLabel");
			this._writingSystemsLabel.Name = "_writingSystemsLabel";
			// 
			// _writingSystemList
			// 
			resources.ApplyResources(this._writingSystemList, "_writingSystemList");
			this._writingSystemList.FormattingEnabled = true;
			this._writingSystemList.Name = "_writingSystemList";
			this._writingSystemList.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.WritingSystemListItemCheck);
			this._writingSystemList.SelectedIndexChanged += new System.EventHandler(this.WritingSystemListSelectedIndexChanged);
			this._writingSystemList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.WritingSystemListMouseDown);
			// 
			// _help
			// 
			resources.ApplyResources(this._help, "_help");
			this._help.Name = "_help";
			this._help.UseVisualStyleBackColor = true;
			this._help.Click += new System.EventHandler(this.WritingListHelpClick);
			// 
			// moveDown
			// 
			resources.ApplyResources(this.moveDown, "moveDown");
			this.moveDown.Name = "moveDown";
			this.moveDown.UseVisualStyleBackColor = true;
			this.moveDown.Click += new System.EventHandler(this.MoveDownClick);
			// 
			// moveUp
			// 
			resources.ApplyResources(this.moveUp, "moveUp");
			this.moveUp.Name = "moveUp";
			this.moveUp.UseVisualStyleBackColor = true;
			this.moveUp.Click += new System.EventHandler(this.MoveUpClick);
			// 
			// _addWsButton
			// 
			resources.ApplyResources(this._addWsButton, "_addWsButton");
			this._addWsButton.Name = "_addWsButton";
			this._addWsButton.UseVisualStyleBackColor = true;
			this._addWsButton.Click += new System.EventHandler(this.AddWsButtonClick);
			// 
			// tableLayoutPanel2
			// 
			resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
			this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel1, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this._tabControl, 0, 1);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			// 
			// tableLayoutPanel1
			// 
			resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
			this.tableLayoutPanel1.Controls.Add(this._shareWithSldrCheckbox, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this._languageNameTextbox, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this._nameLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this._languageCodeLayout, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this._ethnologueLink, 1, 1);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			// 
			// _shareWithSldrCheckbox
			// 
			resources.ApplyResources(this._shareWithSldrCheckbox, "_shareWithSldrCheckbox");
			this._shareWithSldrCheckbox.Name = "_shareWithSldrCheckbox";
			this._shareWithSldrCheckbox.UseVisualStyleBackColor = true;
			// 
			// _languageNameTextbox
			// 
			resources.ApplyResources(this._languageNameTextbox, "_languageNameTextbox");
			this._languageNameTextbox.Name = "_languageNameTextbox";
			// 
			// _nameLabel
			// 
			resources.ApplyResources(this._nameLabel, "_nameLabel");
			this._nameLabel.Name = "_nameLabel";
			// 
			// _languageCodeLayout
			// 
			this._languageCodeLayout.Controls.Add(this._languageLabel);
			this._languageCodeLayout.Controls.Add(this._languageCode);
			this._languageCodeLayout.Controls.Add(this._changeLanguage);
			resources.ApplyResources(this._languageCodeLayout, "_languageCodeLayout");
			this._languageCodeLayout.Name = "_languageCodeLayout";
			// 
			// _languageLabel
			// 
			resources.ApplyResources(this._languageLabel, "_languageLabel");
			this._languageLabel.Name = "_languageLabel";
			// 
			// _languageCode
			// 
			resources.ApplyResources(this._languageCode, "_languageCode");
			this._languageCode.Name = "_languageCode";
			// 
			// _changeLanguage
			// 
			resources.ApplyResources(this._changeLanguage, "_changeLanguage");
			this._changeLanguage.Name = "_changeLanguage";
			this._changeLanguage.TabStop = true;
			this._changeLanguage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ChangeCodeLinkClick);
			// 
			// _ethnologueLink
			// 
			resources.ApplyResources(this._ethnologueLink, "_ethnologueLink");
			this._ethnologueLink.Name = "_ethnologueLink";
			this._ethnologueLink.TabStop = true;
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
			resources.ApplyResources(this._tabControl, "_tabControl");
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			// 
			// _generalTab
			// 
			resources.ApplyResources(this._generalTab, "_generalTab");
			this._generalTab.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._generalTab.Controls.Add(this._enableAdvanced);
			this._generalTab.Controls.Add(this._identifiersControl);
			this._generalTab.Controls.Add(this._rightToLeftCheckbox);
			this._generalTab.Controls.Add(this.m_FullCode);
			this._generalTab.Controls.Add(this.lblFullCode);
			this._generalTab.Controls.Add(this.lblSpellingDictionary);
			this._generalTab.Controls.Add(this._spellingCombo);
			this._generalTab.Name = "_generalTab";
			// 
			// _enableAdvanced
			// 
			resources.ApplyResources(this._enableAdvanced, "_enableAdvanced");
			this._enableAdvanced.Name = "_enableAdvanced";
			this._enableAdvanced.UseVisualStyleBackColor = true;
			// 
			// _identifiersControl
			// 
			resources.ApplyResources(this._identifiersControl, "_identifiersControl");
			this._identifiersControl.Name = "_identifiersControl";
			//
			// _advancedIdentifiersControl
			//
			resources.ApplyResources(this._advancedIdentifiersControl, "_advancedIdentifiersControl");
			this._advancedIdentifiersControl.Name = "_advancedIdentifiersControl";
			// 
			// _rightToLeftCheckbox
			// 
			resources.ApplyResources(this._rightToLeftCheckbox, "_rightToLeftCheckbox");
			this._rightToLeftCheckbox.Name = "_rightToLeftCheckbox";
			this._rightToLeftCheckbox.UseVisualStyleBackColor = true;
			this._rightToLeftCheckbox.CheckedChanged += new System.EventHandler(this.RightToLeftCheckChanged);
			// 
			// m_FullCode
			// 
			resources.ApplyResources(this.m_FullCode, "m_FullCode");
			this.m_FullCode.Name = "m_FullCode";
			// 
			// lblFullCode
			// 
			resources.ApplyResources(this.lblFullCode, "lblFullCode");
			this.lblFullCode.Name = "lblFullCode";
			// 
			// lblSpellingDictionary
			// 
			resources.ApplyResources(this.lblSpellingDictionary, "lblSpellingDictionary");
			this.lblSpellingDictionary.Name = "lblSpellingDictionary";
			// 
			// _spellingCombo
			// 
			this._spellingCombo.AllowSpaceInEditBox = false;
			this._spellingCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._spellingCombo.FormattingEnabled = true;
			resources.ApplyResources(this._spellingCombo, "_spellingCombo");
			this._spellingCombo.Name = "_spellingCombo";
			// 
			// _fontTab
			// 
			resources.ApplyResources(this._fontTab, "_fontTab");
			this._fontTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._fontTab.Controls.Add(this._defaultFontControl);
			this._fontTab.Name = "_fontTab";
			this._fontTab.UseVisualStyleBackColor = true;
			// 
			// _defaultFontControl
			// 
			this._defaultFontControl.DefaultNormalFont = "";
			resources.ApplyResources(this._defaultFontControl, "_defaultFontControl");
			this._defaultFontControl.Name = "_defaultFontControl";
			this._defaultFontControl.WritingSystem = null;
			// 
			// _keyboardTab
			// 
			resources.ApplyResources(this._keyboardTab, "_keyboardTab");
			this._keyboardTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._keyboardTab.Controls.Add(this._keyboardControl);
			this._keyboardTab.Name = "_keyboardTab";
			this._keyboardTab.UseVisualStyleBackColor = true;
			// 
			// _keyboardControl
			// 
			resources.ApplyResources(this._keyboardControl, "_keyboardControl");
			this._keyboardControl.Name = "_keyboardControl";
			// 
			// _sortTab
			// 
			resources.ApplyResources(this._sortTab, "_sortTab");
			this._sortTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._sortTab.Controls.Add(this._sortControl);
			this._sortTab.Name = "_sortTab";
			this._sortTab.UseVisualStyleBackColor = true;
			// 
			// _sortControl
			// 
			resources.ApplyResources(this._sortControl, "_sortControl");
			this._sortControl.Name = "_sortControl";
			// 
			// _charactersTab
			// 
			resources.ApplyResources(this._charactersTab, "_charactersTab");
			this._charactersTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._charactersTab.Controls.Add(this.m_lblValidCharacters);
			this._charactersTab.Controls.Add(this.btnValidChars);
			this._charactersTab.Name = "_charactersTab";
			this._charactersTab.UseVisualStyleBackColor = true;
			// 
			// m_lblValidCharacters
			// 
			resources.ApplyResources(this.m_lblValidCharacters, "m_lblValidCharacters");
			this.m_lblValidCharacters.Name = "m_lblValidCharacters";
			// 
			// btnValidChars
			// 
			resources.ApplyResources(this.btnValidChars, "btnValidChars");
			this.btnValidChars.Name = "btnValidChars";
			this.btnValidChars.Click += new System.EventHandler(this.OnValidCharsButtonClick);
			// 
			// _numbersTab
			// 
			resources.ApplyResources(this._numbersTab, "_numbersTab");
			this._numbersTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._numbersTab.Controls.Add(this.customDigits);
			this._numbersTab.Controls.Add(this.numberSettingsCombo);
			this._numbersTab.Name = "_numbersTab";
			this._numbersTab.UseVisualStyleBackColor = true;
			// 
			// customDigits
			// 
			resources.ApplyResources(this.customDigits, "customDigits");
			this.customDigits.Name = "customDigits";
			// 
			// numberSettingsCombo
			// 
			this.numberSettingsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.numberSettingsCombo.FormattingEnabled = true;
			resources.ApplyResources(this.numberSettingsCombo, "numberSettingsCombo");
			this.numberSettingsCombo.Name = "numberSettingsCombo";
			// 
			// _convertersTab
			// 
			resources.ApplyResources(this._convertersTab, "_convertersTab");
			this._convertersTab.AccessibleRole = System.Windows.Forms.AccessibleRole.PageTab;
			this._convertersTab.Controls.Add(this.btnEncodingConverter);
			this._convertersTab.Controls.Add(this.m_lblEncodingConverter);
			this._convertersTab.Controls.Add(this._encodingConverterCombo);
			this._convertersTab.Name = "_convertersTab";
			this._convertersTab.UseVisualStyleBackColor = true;
			// 
			// btnEncodingConverter
			// 
			resources.ApplyResources(this.btnEncodingConverter, "btnEncodingConverter");
			this.btnEncodingConverter.Name = "btnEncodingConverter";
			this.btnEncodingConverter.Click += new System.EventHandler(this.EncodingConverterButtonClick);
			// 
			// m_lblEncodingConverter
			// 
			resources.ApplyResources(this.m_lblEncodingConverter, "m_lblEncodingConverter");
			this.m_lblEncodingConverter.Name = "m_lblEncodingConverter";
			// 
			// _encodingConverterCombo
			// 
			this._encodingConverterCombo.AllowSpaceInEditBox = false;
			this._encodingConverterCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this._encodingConverterCombo, "_encodingConverterCombo");
			this._encodingConverterCombo.Name = "_encodingConverterCombo";
			this._encodingConverterCombo.Sorted = true;
			this._encodingConverterCombo.SelectedIndexChanged += new System.EventHandler(this.EncodingConverterComboSelectedIndexChanged);
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this._helpBtn);
			this.flowLayoutPanel1.Controls.Add(this._cancelBtn);
			this.flowLayoutPanel1.Controls.Add(this._okBtn);
			resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			// 
			// _helpBtn
			// 
			resources.ApplyResources(this._helpBtn, "_helpBtn");
			this._helpBtn.Name = "_helpBtn";
			this._helpBtn.UseVisualStyleBackColor = true;
			this._helpBtn.Click += new System.EventHandler(this.FormHelpClick);
			// 
			// _cancelBtn
			// 
			this._cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this._cancelBtn, "_cancelBtn");
			this._cancelBtn.Name = "_cancelBtn";
			this._cancelBtn.UseVisualStyleBackColor = true;
			this._cancelBtn.Click += new System.EventHandler(this.CancelButtonClick);
			// 
			// _okBtn
			// 
			resources.ApplyResources(this._okBtn, "_okBtn");
			this._okBtn.Name = "_okBtn";
			this._okBtn.UseVisualStyleBackColor = true;
			this._okBtn.Click += new System.EventHandler(this.OkButtonClick);
			// 
			// _addMenuStrip
			// 
			this._addMenuStrip.Name = "_addMenuStrip";
			resources.ApplyResources(this._addMenuStrip, "_addMenuStrip");
			// 
			// FwWritingSystemSetupDlg
			// 
			this.AcceptButton = this._okBtn;
			resources.ApplyResources(this, "$this");
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._cancelBtn;
			this.ControlBox = false;
			this.Controls.Add(this._mainLayoutPanel);
			this.MinimizeBox = false;
			this.Name = "FwWritingSystemSetupDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
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
		private Common.Widgets.CustomDigitEntryControl customDigits;
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
		private SIL.Windows.Forms.WritingSystems.WSIdentifiers.WSIdentifierView _identifiersControl;
		private SIL.FieldWorks.FwCoreDlgs.AdvancedScriptRegionVariantView _advancedIdentifiersControl;
		private System.Windows.Forms.CheckBox _rightToLeftCheckbox;
		private System.Windows.Forms.Label m_FullCode;
		private System.Windows.Forms.Label lblFullCode;
		private System.Windows.Forms.Label lblSpellingDictionary;
		private Common.Controls.FwOverrideComboBox _spellingCombo;
		private System.Windows.Forms.TabPage _fontTab;
		private FwCoreDlgControls.DefaultFontsControl _defaultFontControl;
		private System.Windows.Forms.CheckBox _shareWithSldrCheckbox;
		private System.Windows.Forms.SplitContainer _writingSystemListPanel;
		private System.Windows.Forms.Label _writingSystemsLabel;
		private System.Windows.Forms.CheckedListBox _writingSystemList;
		private System.Windows.Forms.CheckBox _enableAdvanced;
	}
}