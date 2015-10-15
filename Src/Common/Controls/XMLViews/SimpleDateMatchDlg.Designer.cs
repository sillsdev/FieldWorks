// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	partial class SimpleDateMatchDlg
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimpleDateMatchDlg));
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_startPicker = new System.Windows.Forms.DateTimePicker();
			this.m_endPicker = new System.Windows.Forms.DateTimePicker();
			this.m_typeCombo = new System.Windows.Forms.ComboBox();
			this.m_andLabel = new System.Windows.Forms.Label();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.m_chkUnspecific = new System.Windows.Forms.CheckBox();
			this.m_chkStartBC = new System.Windows.Forms.CheckBox();
			this.m_chkEndBC = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			//
			// m_okButton
			//
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.UseVisualStyleBackColor = true;
			//
			// m_cancelButton
			//
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_cancelButton.Name = "m_cancelButton";
			this.m_cancelButton.UseVisualStyleBackColor = true;
			//
			// m_startPicker
			//
			resources.ApplyResources(this.m_startPicker, "m_startPicker");
			this.m_startPicker.Name = "m_startPicker";
			this.m_startPicker.ValueChanged += new System.EventHandler(this.m_startPicker_ValueChanged);
			//
			// m_endPicker
			//
			resources.ApplyResources(this.m_endPicker, "m_endPicker");
			this.m_endPicker.Name = "m_endPicker";
			this.m_endPicker.ValueChanged += new System.EventHandler(this.m_endPicker_ValueChanged);
			//
			// m_typeCombo
			//
			this.m_typeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_typeCombo.FormattingEnabled = true;
			this.m_typeCombo.Items.AddRange(new object[] {
			resources.GetString("m_typeCombo.Items"),
			resources.GetString("m_typeCombo.Items1"),
			resources.GetString("m_typeCombo.Items2"),
			resources.GetString("m_typeCombo.Items3"),
			resources.GetString("m_typeCombo.Items4")});
			resources.ApplyResources(this.m_typeCombo, "m_typeCombo");
			this.m_typeCombo.Name = "m_typeCombo";
			this.m_typeCombo.SelectedIndexChanged += new System.EventHandler(this.m_typeCombo_SelectedIndexChanged);
			//
			// m_andLabel
			//
			resources.ApplyResources(this.m_andLabel, "m_andLabel");
			this.m_andLabel.Name = "m_andLabel";
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.UseVisualStyleBackColor = true;
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// m_chkUnspecific
			//
			resources.ApplyResources(this.m_chkUnspecific, "m_chkUnspecific");
			this.m_chkUnspecific.Name = "m_chkUnspecific";
			this.m_chkUnspecific.UseVisualStyleBackColor = true;
			//
			// m_chkStartBC
			//
			resources.ApplyResources(this.m_chkStartBC, "m_chkStartBC");
			this.m_chkStartBC.Name = "m_chkStartBC";
			this.m_chkStartBC.UseVisualStyleBackColor = true;
			//
			// m_chkEndBC
			//
			resources.ApplyResources(this.m_chkEndBC, "m_chkEndBC");
			this.m_chkEndBC.Name = "m_chkEndBC";
			this.helpProvider1.SetShowHelp(this.m_chkEndBC, ((bool)(resources.GetObject("m_chkEndBC.ShowHelp"))));
			this.m_chkEndBC.UseVisualStyleBackColor = true;
			//
			// SimpleDateMatchDlg
			//
			this.AcceptButton = this.m_okButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancelButton;
			this.Controls.Add(this.m_chkEndBC);
			this.Controls.Add(this.m_chkStartBC);
			this.Controls.Add(this.m_chkUnspecific);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_andLabel);
			this.Controls.Add(this.m_typeCombo);
			this.Controls.Add(this.m_endPicker);
			this.Controls.Add(this.m_startPicker);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_okButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SimpleDateMatchDlg";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_okButton;
		private System.Windows.Forms.Button m_cancelButton;
		private System.Windows.Forms.DateTimePicker m_startPicker;
		private System.Windows.Forms.DateTimePicker m_endPicker;
		private System.Windows.Forms.ComboBox m_typeCombo;
		private System.Windows.Forms.Label m_andLabel;
		private System.Windows.Forms.Button m_helpButton;
		private System.Windows.Forms.HelpProvider helpProvider1;
		private System.Windows.Forms.CheckBox m_chkUnspecific;
		private System.Windows.Forms.CheckBox m_chkStartBC;
		private System.Windows.Forms.CheckBox m_chkEndBC;
	}
}