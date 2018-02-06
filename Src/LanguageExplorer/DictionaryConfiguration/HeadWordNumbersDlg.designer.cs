// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.DictionaryConfiguration
{
	partial class HeadwordNumbersDlg
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (disposing)
			{
				components?.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HeadwordNumbersDlg));
			this.label5 = new System.Windows.Forms.Label();
			this.m_chkShowHomographNumInDict = new System.Windows.Forms.CheckBox();
			this.m_chkShowSenseNumber = new System.Windows.Forms.CheckBox();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_radioBefore = new System.Windows.Forms.RadioButton();
			this.m_radioAfter = new System.Windows.Forms.RadioButton();
			this.mainLayoutTable = new System.Windows.Forms.TableLayoutPanel();
			this.descriptionPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.dialogDescription = new System.Windows.Forms.RichTextBox();
			this.m_configurationDescription = new System.Windows.Forms.TextBox();
			this.referenceNumberGroup = new System.Windows.Forms.GroupBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_radioNone = new System.Windows.Forms.RadioButton();
			this.buttonLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.customNumbersPanel = new System.Windows.Forms.Panel();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_digitNine = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitEight = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitSeven = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitSix = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitFive = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitFour = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitThree = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitTwo = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitOne = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_digitZero = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.label11 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.m_customNumbersLabel = new System.Windows.Forms.TextBox();
			this.m_writingSystemCombo = new System.Windows.Forms.ComboBox();
			this.m_writingSystemLabel = new System.Windows.Forms.Label();
			this.homographStylePanel = new System.Windows.Forms.Panel();
			this._homographStyleButton = new System.Windows.Forms.Button();
			this.styleLabel = new System.Windows.Forms.TextBox();
			this._homographStyleCombo = new System.Windows.Forms.ComboBox();
			this.senseNumberStylePanel = new System.Windows.Forms.Panel();
			this._senseNumberStyleBtn = new System.Windows.Forms.Button();
			this._senseStyleLabel = new System.Windows.Forms.TextBox();
			this._senseStyleCombo = new System.Windows.Forms.ComboBox();
			this.mainLayoutTable.SuspendLayout();
			this.descriptionPanel.SuspendLayout();
			this.referenceNumberGroup.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.buttonLayoutPanel.SuspendLayout();
			this.customNumbersPanel.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_digitNine)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitEight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitSeven)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitSix)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitFive)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitFour)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitThree)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitTwo)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitOne)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitZero)).BeginInit();
			this.homographStylePanel.SuspendLayout();
			this.senseNumberStylePanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// label5
			// 
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			// 
			// m_chkShowHomographNumInDict
			// 
			resources.ApplyResources(this.m_chkShowHomographNumInDict, "m_chkShowHomographNumInDict");
			this.m_chkShowHomographNumInDict.Name = "m_chkShowHomographNumInDict";
			this.m_chkShowHomographNumInDict.UseVisualStyleBackColor = true;
			this.m_chkShowHomographNumInDict.CheckedChanged += new System.EventHandler(this.m_chkShowHomographNumInDict_CheckedChanged);
			// 
			// m_chkShowSenseNumber
			// 
			resources.ApplyResources(this.m_chkShowSenseNumber, "m_chkShowSenseNumber");
			this.m_chkShowSenseNumber.Name = "m_chkShowSenseNumber";
			this.m_chkShowSenseNumber.UseVisualStyleBackColor = true;
			// 
			// m_btnOk
			// 
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.UseVisualStyleBackColor = true;
			// 
			// m_btnCancel
			// 
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			// 
			// m_btnHelp
			// 
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// m_radioBefore
			// 
			resources.ApplyResources(this.m_radioBefore, "m_radioBefore");
			this.m_radioBefore.Name = "m_radioBefore";
			this.m_radioBefore.UseVisualStyleBackColor = true;
			// 
			// m_radioAfter
			// 
			resources.ApplyResources(this.m_radioAfter, "m_radioAfter");
			this.m_radioAfter.Checked = true;
			this.m_radioAfter.Name = "m_radioAfter";
			this.m_radioAfter.TabStop = true;
			this.m_radioAfter.UseVisualStyleBackColor = true;
			// 
			// mainLayoutTable
			// 
			resources.ApplyResources(this.mainLayoutTable, "mainLayoutTable");
			this.mainLayoutTable.Controls.Add(this.descriptionPanel, 0, 0);
			this.mainLayoutTable.Controls.Add(this.referenceNumberGroup, 0, 4);
			this.mainLayoutTable.Controls.Add(this.groupBox1, 0, 1);
			this.mainLayoutTable.Controls.Add(this.buttonLayoutPanel, 0, 6);
			this.mainLayoutTable.Controls.Add(this.customNumbersPanel, 0, 5);
			this.mainLayoutTable.Controls.Add(this.homographStylePanel, 0, 2);
			this.mainLayoutTable.Controls.Add(this.senseNumberStylePanel, 0, 3);
			this.mainLayoutTable.Name = "mainLayoutTable";
			// 
			// descriptionPanel
			// 
			resources.ApplyResources(this.descriptionPanel, "descriptionPanel");
			this.descriptionPanel.Controls.Add(this.dialogDescription);
			this.descriptionPanel.Controls.Add(this.m_configurationDescription);
			this.descriptionPanel.Name = "descriptionPanel";
			// 
			// dialogDescription
			// 
			resources.ApplyResources(this.dialogDescription, "dialogDescription");
			this.dialogDescription.BackColor = System.Drawing.SystemColors.Control;
			this.dialogDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.dialogDescription.Cursor = System.Windows.Forms.Cursors.Default;
			this.dialogDescription.Name = "dialogDescription";
			this.dialogDescription.ReadOnly = true;
			// 
			// m_configurationDescription
			// 
			this.m_configurationDescription.BackColor = System.Drawing.SystemColors.Control;
			this.m_configurationDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_configurationDescription.Cursor = System.Windows.Forms.Cursors.Default;
			resources.ApplyResources(this.m_configurationDescription, "m_configurationDescription");
			this.m_configurationDescription.Name = "m_configurationDescription";
			this.m_configurationDescription.ReadOnly = true;
			// 
			// referenceNumberGroup
			// 
			this.referenceNumberGroup.Controls.Add(this.m_chkShowHomographNumInDict);
			this.referenceNumberGroup.Controls.Add(this.m_chkShowSenseNumber);
			resources.ApplyResources(this.referenceNumberGroup, "referenceNumberGroup");
			this.referenceNumberGroup.Name = "referenceNumberGroup";
			this.referenceNumberGroup.TabStop = false;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.m_radioNone);
			this.groupBox1.Controls.Add(this.m_radioBefore);
			this.groupBox1.Controls.Add(this.m_radioAfter);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// m_radioNone
			// 
			resources.ApplyResources(this.m_radioNone, "m_radioNone");
			this.m_radioNone.Checked = true;
			this.m_radioNone.Name = "m_radioNone";
			this.m_radioNone.TabStop = true;
			this.m_radioNone.UseVisualStyleBackColor = true;
			// 
			// buttonLayoutPanel
			// 
			resources.ApplyResources(this.buttonLayoutPanel, "buttonLayoutPanel");
			this.buttonLayoutPanel.Controls.Add(this.m_btnHelp);
			this.buttonLayoutPanel.Controls.Add(this.m_btnCancel);
			this.buttonLayoutPanel.Controls.Add(this.m_btnOk);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			// 
			// customNumbersPanel
			// 
			this.customNumbersPanel.Controls.Add(this.tableLayoutPanel1);
			this.customNumbersPanel.Controls.Add(this.m_customNumbersLabel);
			this.customNumbersPanel.Controls.Add(this.m_writingSystemCombo);
			this.customNumbersPanel.Controls.Add(this.m_writingSystemLabel);
			resources.ApplyResources(this.customNumbersPanel, "customNumbersPanel");
			this.customNumbersPanel.Name = "customNumbersPanel";
			// 
			// tableLayoutPanel1
			// 
			resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
			this.tableLayoutPanel1.Controls.Add(this.m_digitNine, 9, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitEight, 8, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitSeven, 7, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitSix, 6, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitFive, 5, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitFour, 4, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitThree, 3, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitTwo, 2, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitOne, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_digitZero, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label11, 9, 0);
			this.tableLayoutPanel1.Controls.Add(this.label10, 8, 0);
			this.tableLayoutPanel1.Controls.Add(this.label9, 7, 0);
			this.tableLayoutPanel1.Controls.Add(this.label8, 6, 0);
			this.tableLayoutPanel1.Controls.Add(this.label7, 5, 0);
			this.tableLayoutPanel1.Controls.Add(this.label6, 4, 0);
			this.tableLayoutPanel1.Controls.Add(this.label4, 3, 0);
			this.tableLayoutPanel1.Controls.Add(this.label3, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.label2, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			// 
			// m_digitNine
			// 
			this.m_digitNine.AcceptsReturn = false;
			this.m_digitNine.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitNine, "m_digitNine");
			this.m_digitNine.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitNine.controlID = null;
			this.m_digitNine.HasBorder = true;
			this.m_digitNine.Name = "m_digitNine";
			this.m_digitNine.SuppressEnter = false;
			this.m_digitNine.WordWrap = false;
			// 
			// m_digitEight
			// 
			this.m_digitEight.AcceptsReturn = false;
			this.m_digitEight.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitEight, "m_digitEight");
			this.m_digitEight.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitEight.controlID = null;
			this.m_digitEight.HasBorder = true;
			this.m_digitEight.Name = "m_digitEight";
			this.m_digitEight.SuppressEnter = false;
			this.m_digitEight.WordWrap = false;
			// 
			// m_digitSeven
			// 
			this.m_digitSeven.AcceptsReturn = false;
			this.m_digitSeven.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitSeven, "m_digitSeven");
			this.m_digitSeven.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitSeven.controlID = null;
			this.m_digitSeven.HasBorder = true;
			this.m_digitSeven.Name = "m_digitSeven";
			this.m_digitSeven.SuppressEnter = false;
			this.m_digitSeven.WordWrap = false;
			// 
			// m_digitSix
			// 
			this.m_digitSix.AcceptsReturn = false;
			this.m_digitSix.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitSix, "m_digitSix");
			this.m_digitSix.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitSix.controlID = null;
			this.m_digitSix.HasBorder = true;
			this.m_digitSix.Name = "m_digitSix";
			this.m_digitSix.SuppressEnter = false;
			this.m_digitSix.WordWrap = false;
			// 
			// m_digitFive
			// 
			this.m_digitFive.AcceptsReturn = false;
			this.m_digitFive.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitFive, "m_digitFive");
			this.m_digitFive.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitFive.controlID = null;
			this.m_digitFive.HasBorder = true;
			this.m_digitFive.Name = "m_digitFive";
			this.m_digitFive.SuppressEnter = false;
			this.m_digitFive.WordWrap = false;
			// 
			// m_digitFour
			// 
			this.m_digitFour.AcceptsReturn = false;
			this.m_digitFour.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitFour, "m_digitFour");
			this.m_digitFour.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitFour.controlID = null;
			this.m_digitFour.HasBorder = true;
			this.m_digitFour.Name = "m_digitFour";
			this.m_digitFour.SuppressEnter = false;
			this.m_digitFour.WordWrap = false;
			// 
			// m_digitThree
			// 
			this.m_digitThree.AcceptsReturn = false;
			this.m_digitThree.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitThree, "m_digitThree");
			this.m_digitThree.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitThree.controlID = null;
			this.m_digitThree.HasBorder = true;
			this.m_digitThree.Name = "m_digitThree";
			this.m_digitThree.SuppressEnter = false;
			this.m_digitThree.WordWrap = false;
			// 
			// m_digitTwo
			// 
			this.m_digitTwo.AcceptsReturn = false;
			this.m_digitTwo.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitTwo, "m_digitTwo");
			this.m_digitTwo.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitTwo.controlID = null;
			this.m_digitTwo.HasBorder = true;
			this.m_digitTwo.Name = "m_digitTwo";
			this.m_digitTwo.SuppressEnter = false;
			this.m_digitTwo.WordWrap = false;
			// 
			// m_digitOne
			// 
			this.m_digitOne.AcceptsReturn = false;
			this.m_digitOne.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitOne, "m_digitOne");
			this.m_digitOne.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitOne.controlID = null;
			this.m_digitOne.HasBorder = true;
			this.m_digitOne.Name = "m_digitOne";
			this.m_digitOne.SuppressEnter = false;
			this.m_digitOne.WordWrap = false;
			// 
			// m_digitZero
			// 
			this.m_digitZero.AcceptsReturn = false;
			this.m_digitZero.AdjustStringHeight = true;
			resources.ApplyResources(this.m_digitZero, "m_digitZero");
			this.m_digitZero.BackColor = System.Drawing.SystemColors.Window;
			this.m_digitZero.controlID = null;
			this.m_digitZero.HasBorder = true;
			this.m_digitZero.Name = "m_digitZero";
			this.m_digitZero.SuppressEnter = false;
			this.m_digitZero.WordWrap = false;
			// 
			// label11
			// 
			resources.ApplyResources(this.label11, "label11");
			this.label11.Name = "label11";
			// 
			// label10
			// 
			resources.ApplyResources(this.label10, "label10");
			this.label10.Name = "label10";
			// 
			// label9
			// 
			resources.ApplyResources(this.label9, "label9");
			this.label9.Name = "label9";
			// 
			// label8
			// 
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			// 
			// label7
			// 
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			// 
			// label6
			// 
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// m_customNumbersLabel
			// 
			this.m_customNumbersLabel.BackColor = System.Drawing.SystemColors.Control;
			this.m_customNumbersLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_customNumbersLabel.Cursor = System.Windows.Forms.Cursors.Default;
			resources.ApplyResources(this.m_customNumbersLabel, "m_customNumbersLabel");
			this.m_customNumbersLabel.Name = "m_customNumbersLabel";
			this.m_customNumbersLabel.ReadOnly = true;
			// 
			// m_writingSystemCombo
			// 
			this.m_writingSystemCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_writingSystemCombo.FormattingEnabled = true;
			resources.ApplyResources(this.m_writingSystemCombo, "m_writingSystemCombo");
			this.m_writingSystemCombo.Name = "m_writingSystemCombo";
			this.m_writingSystemCombo.SelectedIndexChanged += new System.EventHandler(this.m_writingSystemCombo_SelectedIndexChanged);
			// 
			// m_writingSystemLabel
			// 
			resources.ApplyResources(this.m_writingSystemLabel, "m_writingSystemLabel");
			this.m_writingSystemLabel.Name = "m_writingSystemLabel";
			// 
			// homographStylePanel
			// 
			this.homographStylePanel.Controls.Add(this._homographStyleButton);
			this.homographStylePanel.Controls.Add(this.styleLabel);
			this.homographStylePanel.Controls.Add(this._homographStyleCombo);
			resources.ApplyResources(this.homographStylePanel, "homographStylePanel");
			this.homographStylePanel.Name = "homographStylePanel";
			// 
			// _homographStyleButton
			// 
			resources.ApplyResources(this._homographStyleButton, "_homographStyleButton");
			this._homographStyleButton.Name = "_homographStyleButton";
			this._homographStyleButton.UseVisualStyleBackColor = true;
			// 
			// styleLabel
			// 
			this.styleLabel.BackColor = System.Drawing.SystemColors.Control;
			this.styleLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.styleLabel.Cursor = System.Windows.Forms.Cursors.Default;
			resources.ApplyResources(this.styleLabel, "styleLabel");
			this.styleLabel.Name = "styleLabel";
			this.styleLabel.ReadOnly = true;
			// 
			// _homographStyleCombo
			// 
			resources.ApplyResources(this._homographStyleCombo, "_homographStyleCombo");
			this._homographStyleCombo.Name = "_homographStyleCombo";
			// 
			// senseNumberStylePanel
			// 
			this.senseNumberStylePanel.Controls.Add(this._senseNumberStyleBtn);
			this.senseNumberStylePanel.Controls.Add(this._senseStyleLabel);
			this.senseNumberStylePanel.Controls.Add(this._senseStyleCombo);
			resources.ApplyResources(this.senseNumberStylePanel, "senseNumberStylePanel");
			this.senseNumberStylePanel.Name = "senseNumberStylePanel";
			// 
			// _senseNumberStyleBtn
			// 
			resources.ApplyResources(this._senseNumberStyleBtn, "_senseNumberStyleBtn");
			this._senseNumberStyleBtn.Name = "_senseNumberStyleBtn";
			this._senseNumberStyleBtn.UseVisualStyleBackColor = true;
			// 
			// _senseStyleLabel
			// 
			this._senseStyleLabel.BackColor = System.Drawing.SystemColors.Control;
			this._senseStyleLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._senseStyleLabel.Cursor = System.Windows.Forms.Cursors.Default;
			resources.ApplyResources(this._senseStyleLabel, "_senseStyleLabel");
			this._senseStyleLabel.Name = "_senseStyleLabel";
			this._senseStyleLabel.ReadOnly = true;
			// 
			// _senseStyleCombo
			// 
			resources.ApplyResources(this._senseStyleCombo, "_senseStyleCombo");
			this._senseStyleCombo.Name = "_senseStyleCombo";
			// 
			// HeadwordNumbersDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.mainLayoutTable);
			this.Controls.Add(this.label5);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "HeadwordNumbersDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.mainLayoutTable.ResumeLayout(false);
			this.descriptionPanel.ResumeLayout(false);
			this.descriptionPanel.PerformLayout();
			this.referenceNumberGroup.ResumeLayout(false);
			this.referenceNumberGroup.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.buttonLayoutPanel.ResumeLayout(false);
			this.customNumbersPanel.ResumeLayout(false);
			this.customNumbersPanel.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_digitNine)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitEight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitSeven)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitSix)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitFive)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitFour)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitThree)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitTwo)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitOne)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_digitZero)).EndInit();
			this.homographStylePanel.ResumeLayout(false);
			this.homographStylePanel.PerformLayout();
			this.senseNumberStylePanel.ResumeLayout(false);
			this.senseNumberStylePanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox m_chkShowHomographNumInDict;
		private System.Windows.Forms.CheckBox m_chkShowSenseNumber;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.RadioButton m_radioBefore;
		private System.Windows.Forms.RadioButton m_radioAfter;
		private System.Windows.Forms.TableLayoutPanel mainLayoutTable;
		private System.Windows.Forms.GroupBox referenceNumberGroup;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.FlowLayoutPanel buttonLayoutPanel;
		private System.Windows.Forms.Panel homographStylePanel;
		private System.Windows.Forms.Panel senseNumberStylePanel;
		private System.Windows.Forms.Button _homographStyleButton;
		private System.Windows.Forms.ComboBox _homographStyleCombo;
		private System.Windows.Forms.TextBox styleLabel;
		private System.Windows.Forms.Button _senseNumberStyleBtn;
		private System.Windows.Forms.TextBox _senseStyleLabel;
		private System.Windows.Forms.ComboBox _senseStyleCombo;
		private System.Windows.Forms.FlowLayoutPanel descriptionPanel;
		private System.Windows.Forms.RichTextBox dialogDescription;
		private System.Windows.Forms.TextBox m_configurationDescription;
		private System.Windows.Forms.RadioButton m_radioNone;
		private System.Windows.Forms.Panel customNumbersPanel;
		private System.Windows.Forms.TextBox m_customNumbersLabel;
		private System.Windows.Forms.ComboBox m_writingSystemCombo;
		private System.Windows.Forms.Label m_writingSystemLabel;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitNine;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitEight;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitSeven;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitSix;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitFive;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitFour;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitThree;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitTwo;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitOne;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_digitZero;
	}
}
