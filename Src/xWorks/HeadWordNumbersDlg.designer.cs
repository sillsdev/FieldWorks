// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
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
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_radioNone = new System.Windows.Forms.RadioButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this._stylesButton = new System.Windows.Forms.Button();
			this._stylesCombo = new System.Windows.Forms.ComboBox();
			this.styleLabel = new System.Windows.Forms.TextBox();
			this.buttonLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.mainLayoutTable.SuspendLayout();
			this.descriptionPanel.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.buttonLayoutPanel.SuspendLayout();
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
			this.mainLayoutTable.Controls.Add(this.groupBox2, 0, 3);
			this.mainLayoutTable.Controls.Add(this.groupBox1, 0, 1);
			this.mainLayoutTable.Controls.Add(this.panel1, 0, 2);
			this.mainLayoutTable.Controls.Add(this.buttonLayoutPanel, 0, 4);
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
			this.dialogDescription.Name = "dialogDescription";
			// 
			// m_configurationDescription
			// 
			this.m_configurationDescription.BackColor = System.Drawing.SystemColors.Control;
			this.m_configurationDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_configurationDescription, "m_configurationDescription");
			this.m_configurationDescription.Name = "m_configurationDescription";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.m_chkShowHomographNumInDict);
			this.groupBox2.Controls.Add(this.m_chkShowSenseNumber);
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
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
			// panel1
			// 
			this.panel1.Controls.Add(this._stylesButton);
			this.panel1.Controls.Add(this._stylesCombo);
			this.panel1.Controls.Add(this.styleLabel);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			// 
			// _stylesButton
			// 
			resources.ApplyResources(this._stylesButton, "_stylesButton");
			this._stylesButton.Name = "_stylesButton";
			this._stylesButton.UseVisualStyleBackColor = true;
			// 
			// _stylesCombo
			// 
			this._stylesCombo.FormattingEnabled = true;
			resources.ApplyResources(this._stylesCombo, "_stylesCombo");
			this._stylesCombo.Name = "_stylesCombo";
			// 
			// styleLabel
			// 
			this.styleLabel.BackColor = System.Drawing.SystemColors.Control;
			this.styleLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.styleLabel, "styleLabel");
			this.styleLabel.Name = "styleLabel";
			// 
			// buttonLayoutPanel
			// 
			resources.ApplyResources(this.buttonLayoutPanel, "buttonLayoutPanel");
			this.buttonLayoutPanel.Controls.Add(this.m_btnHelp);
			this.buttonLayoutPanel.Controls.Add(this.m_btnCancel);
			this.buttonLayoutPanel.Controls.Add(this.m_btnOk);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
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
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.buttonLayoutPanel.ResumeLayout(false);
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
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.FlowLayoutPanel buttonLayoutPanel;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button _stylesButton;
		private System.Windows.Forms.ComboBox _stylesCombo;
		private System.Windows.Forms.TextBox styleLabel;
		private System.Windows.Forms.FlowLayoutPanel descriptionPanel;
		private System.Windows.Forms.RichTextBox dialogDescription;
		private System.Windows.Forms.TextBox m_configurationDescription;
		private System.Windows.Forms.RadioButton m_radioNone;
	}
}
