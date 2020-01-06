// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using System.Windows.Forms;
using SIL.Windows.Forms.Widgets;

namespace SIL.FieldWorks.FwCoreDlgs
{
	public partial class AdvancedScriptRegionVariantView
	{


		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdvancedScriptRegionVariantView));
			this._scriptChooser = new System.Windows.Forms.ComboBox();
			this._scriptCodeTextBox = new System.Windows.Forms.TextBox();
			this._scriptNameTextbox = new System.Windows.Forms.TextBox();
			this._regionNameTextBox = new System.Windows.Forms.TextBox();
			this._regionCodeTextbox = new System.Windows.Forms.TextBox();
			this._regionChooser = new System.Windows.Forms.ComboBox();
			this._variantsTextBox = new System.Windows.Forms.TextBox();
			this._ietftagTextBox = new System.Windows.Forms.TextBox();
			this._standardVariantCombo = new System.Windows.Forms.ComboBox();
			this._abbreviation = new System.Windows.Forms.TextBox();
			this._specialTypeComboBox = new System.Windows.Forms.ComboBox();
			this.betterLabel5 = new SIL.Windows.Forms.Widgets.BetterLabel();
			this.betterLabel4 = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._scriptLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._regionLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._standardVariantLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._bcp47Label = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._customScriptNameLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._customRegionNameLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._otherVariantsLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._scriptCodeLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this._regionCodeLabel = new SIL.Windows.Forms.Widgets.BetterLabel();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// _scriptChooser
			// 
			this._scriptChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._scriptChooser.DropDownWidth = 200;
			this._scriptChooser.FormattingEnabled = true;
			resources.ApplyResources(this._scriptChooser, "_scriptChooser");
			this._scriptChooser.Name = "_scriptChooser";
			// 
			// _scriptCodeTextBox
			// 
			resources.ApplyResources(this._scriptCodeTextBox, "_scriptCodeTextBox");
			this._scriptCodeTextBox.Name = "_scriptCodeTextBox";
			// 
			// _scriptNameTextbox
			// 
			resources.ApplyResources(this._scriptNameTextbox, "_scriptNameTextbox");
			this._scriptNameTextbox.Name = "_scriptNameTextbox";
			// 
			// _regionNameTextBox
			// 
			resources.ApplyResources(this._regionNameTextBox, "_regionNameTextBox");
			this._regionNameTextBox.Name = "_regionNameTextBox";
			// 
			// _regionCodeTextbox
			// 
			resources.ApplyResources(this._regionCodeTextbox, "_regionCodeTextbox");
			this._regionCodeTextbox.Name = "_regionCodeTextbox";
			// 
			// _regionChooser
			// 
			this._regionChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._regionChooser.DropDownWidth = 185;
			this._regionChooser.FormattingEnabled = true;
			resources.ApplyResources(this._regionChooser, "_regionChooser");
			this._regionChooser.Name = "_regionChooser";
			// 
			// _variantsTextBox
			// 
			resources.ApplyResources(this._variantsTextBox, "_variantsTextBox");
			this._variantsTextBox.Name = "_variantsTextBox";
			// 
			// _ietftagTextBox
			// 
			resources.ApplyResources(this._ietftagTextBox, "_ietftagTextBox");
			this._ietftagTextBox.Name = "_ietftagTextBox";
			// 
			// _standardVariantCombo
			// 
			this._standardVariantCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._standardVariantCombo.DropDownWidth = 180;
			this._standardVariantCombo.FormattingEnabled = true;
			resources.ApplyResources(this._standardVariantCombo, "_standardVariantCombo");
			this._standardVariantCombo.Name = "_standardVariantCombo";
			// 
			// _abbreviation
			// 
			resources.ApplyResources(this._abbreviation, "_abbreviation");
			this._abbreviation.Name = "_abbreviation";
			// 
			// _specialTypeComboBox
			// 
			this._specialTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._specialTypeComboBox.FormattingEnabled = true;
			this._specialTypeComboBox.Items.AddRange(new object[] {
            resources.GetString("_specialTypeComboBox.Items")});
			resources.ApplyResources(this._specialTypeComboBox, "_specialTypeComboBox");
			this._specialTypeComboBox.Name = "_specialTypeComboBox";
			// 
			// betterLabel5
			// 
			resources.ApplyResources(this.betterLabel5, "betterLabel5");
			this.betterLabel5.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.betterLabel5.ForeColor = System.Drawing.SystemColors.ControlText;
			this.betterLabel5.IsTextSelectable = false;
			this.betterLabel5.Name = "betterLabel5";
			this.betterLabel5.ReadOnly = true;
			this.betterLabel5.TabStop = false;
			// 
			// betterLabel4
			// 
			resources.ApplyResources(this.betterLabel4, "betterLabel4");
			this.betterLabel4.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.betterLabel4.ForeColor = System.Drawing.SystemColors.ControlText;
			this.betterLabel4.IsTextSelectable = false;
			this.betterLabel4.Name = "betterLabel4";
			this.betterLabel4.ReadOnly = true;
			this.betterLabel4.TabStop = false;
			// 
			// _scriptLabel
			// 
			resources.ApplyResources(this._scriptLabel, "_scriptLabel");
			this._scriptLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._scriptLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._scriptLabel.IsTextSelectable = false;
			this._scriptLabel.Name = "_scriptLabel";
			this._scriptLabel.ReadOnly = true;
			this._scriptLabel.TabStop = false;
			// 
			// _regionLabel
			// 
			resources.ApplyResources(this._regionLabel, "_regionLabel");
			this._regionLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._regionLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._regionLabel.IsTextSelectable = false;
			this._regionLabel.Name = "_regionLabel";
			this._regionLabel.ReadOnly = true;
			this._regionLabel.TabStop = false;
			// 
			// _standardVariantLabel
			// 
			resources.ApplyResources(this._standardVariantLabel, "_standardVariantLabel");
			this._standardVariantLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._standardVariantLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._standardVariantLabel.IsTextSelectable = false;
			this._standardVariantLabel.Name = "_standardVariantLabel";
			this._standardVariantLabel.ReadOnly = true;
			this._standardVariantLabel.TabStop = false;
			// 
			// _bcp47Label
			// 
			resources.ApplyResources(this._bcp47Label, "_bcp47Label");
			this._bcp47Label.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._bcp47Label.ForeColor = System.Drawing.SystemColors.ControlText;
			this._bcp47Label.IsTextSelectable = false;
			this._bcp47Label.Name = "_bcp47Label";
			this._bcp47Label.ReadOnly = true;
			this._bcp47Label.TabStop = false;
			// 
			// _customScriptNameLabel
			// 
			resources.ApplyResources(this._customScriptNameLabel, "_customScriptNameLabel");
			this._customScriptNameLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._customScriptNameLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._customScriptNameLabel.IsTextSelectable = false;
			this._customScriptNameLabel.Name = "_customScriptNameLabel";
			this._customScriptNameLabel.ReadOnly = true;
			this._customScriptNameLabel.TabStop = false;
			// 
			// _customRegionNameLabel
			// 
			resources.ApplyResources(this._customRegionNameLabel, "_customRegionNameLabel");
			this._customRegionNameLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._customRegionNameLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._customRegionNameLabel.IsTextSelectable = false;
			this._customRegionNameLabel.Name = "_customRegionNameLabel";
			this._customRegionNameLabel.ReadOnly = true;
			this._customRegionNameLabel.TabStop = false;
			// 
			// _otherVariantsLabel
			// 
			resources.ApplyResources(this._otherVariantsLabel, "_otherVariantsLabel");
			this._otherVariantsLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._otherVariantsLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._otherVariantsLabel.IsTextSelectable = false;
			this._otherVariantsLabel.Name = "_otherVariantsLabel";
			this._otherVariantsLabel.ReadOnly = true;
			this._otherVariantsLabel.TabStop = false;
			// 
			// _scriptCodeLabel
			// 
			resources.ApplyResources(this._scriptCodeLabel, "_scriptCodeLabel");
			this._scriptCodeLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._scriptCodeLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._scriptCodeLabel.IsTextSelectable = false;
			this._scriptCodeLabel.Name = "_scriptCodeLabel";
			this._scriptCodeLabel.ReadOnly = true;
			this._scriptCodeLabel.TabStop = false;
			// 
			// _regionCodeLabel
			// 
			resources.ApplyResources(this._regionCodeLabel, "_regionCodeLabel");
			this._regionCodeLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._regionCodeLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this._regionCodeLabel.IsTextSelectable = false;
			this._regionCodeLabel.Name = "_regionCodeLabel";
			this._regionCodeLabel.ReadOnly = true;
			this._regionCodeLabel.TabStop = false;
			// 
			// tableLayoutPanel1
			// 
			resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
			this.tableLayoutPanel1.Controls.Add(this._scriptLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this._regionCodeLabel, 4, 1);
			this.tableLayoutPanel1.Controls.Add(this._regionLabel, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this._scriptCodeLabel, 4, 0);
			this.tableLayoutPanel1.Controls.Add(this._scriptChooser, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this._regionChooser, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this._customRegionNameLabel, 2, 1);
			this.tableLayoutPanel1.Controls.Add(this._customScriptNameLabel, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this._scriptNameTextbox, 3, 0);
			this.tableLayoutPanel1.Controls.Add(this._regionNameTextBox, 3, 1);
			this.tableLayoutPanel1.Controls.Add(this._scriptCodeTextBox, 5, 0);
			this.tableLayoutPanel1.Controls.Add(this._regionCodeTextbox, 5, 1);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			// 
			// tableLayoutPanel2
			// 
			resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
			this.tableLayoutPanel2.Controls.Add(this._standardVariantLabel, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this._bcp47Label, 0, 1);
			this.tableLayoutPanel2.Controls.Add(this._otherVariantsLabel, 2, 0);
			this.tableLayoutPanel2.Controls.Add(this._standardVariantCombo, 1, 0);
			this.tableLayoutPanel2.Controls.Add(this._ietftagTextBox, 1, 1);
			this.tableLayoutPanel2.Controls.Add(this._variantsTextBox, 3, 0);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			// 
			// AdvancedScriptRegionVariantView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel2);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this._abbreviation);
			this.Controls.Add(this._specialTypeComboBox);
			this.Controls.Add(this.betterLabel5);
			this.Controls.Add(this.betterLabel4);
			resources.ApplyResources(this, "$this");
			this.Name = "AdvancedScriptRegionVariantView";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private bool _updatingFromModel;
		private IContainer components;
		private TextBox nonStandardLanguageName;
		private ComboBox _scriptChooser;
		private TextBox _scriptCodeTextBox;
		private TextBox _scriptNameTextbox;
		private TextBox _regionNameTextBox;
		private TextBox _regionCodeTextbox;
		private ComboBox _regionChooser;
		private TextBox _variantsTextBox;
		private TextBox _ietftagTextBox;
		private ComboBox _standardVariantCombo;
		private TextBox _abbreviation;
		private ComboBox _specialTypeComboBox;
		private BetterLabel betterLabel5;
		private BetterLabel betterLabel4;
		private BetterLabel _scriptLabel;
		private BetterLabel _regionLabel;
		private BetterLabel _standardVariantLabel;
		private BetterLabel _bcp47Label;
		private BetterLabel _customScriptNameLabel;
		private BetterLabel _customRegionNameLabel;
		private BetterLabel _otherVariantsLabel;
		private BetterLabel _scriptCodeLabel;
		private BetterLabel _regionCodeLabel;
		private BetterLabel betterLabel2;
		private TableLayoutPanel tableLayoutPanel1;
		private TableLayoutPanel tableLayoutPanel2;
	}
}
