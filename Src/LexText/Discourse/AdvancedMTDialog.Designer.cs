// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using XCore;

namespace SIL.FieldWorks.Discourse
{
	partial class AdvancedMTDialog
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private IHelpTopicProvider m_helpTopicProvider;
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_AMTDLogic != null)
					m_AMTDLogic.Dispose();
			}
			components = null;
			m_AMTDLogic = null;

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdvancedMTDialog));
			this.helpProvider = new HelpProvider();
			this.m_rowLabel = new System.Windows.Forms.Label();
			this.m_columnLabel = new System.Windows.Forms.Label();
			this.m_rowsCombo = new System.Windows.Forms.ComboBox();
			this.m_columnsCombo = new System.Windows.Forms.ComboBox();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_OkButton = new System.Windows.Forms.Button();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_mainText = new System.Windows.Forms.TextBox();
			this.m_partialText = new System.Windows.Forms.TextBox();
			this.m_bottomStuff = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			//
			// m_rowLabel
			//
			resources.ApplyResources(this.m_rowLabel, "m_rowLabel");
			this.m_rowLabel.Name = "m_rowLabel";
			//
			// m_columnLabel
			//
			resources.ApplyResources(this.m_columnLabel, "m_columnLabel");
			this.m_columnLabel.Name = "m_columnLabel";
			//
			// m_rowsCombo
			//
			this.m_rowsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_rowsCombo.FormattingEnabled = true;
			resources.ApplyResources(this.m_rowsCombo, "m_rowsCombo");
			this.m_rowsCombo.Name = "m_rowsCombo";
			this.m_rowsCombo.SelectedIndexChanged += new System.EventHandler(this.m_rowsCombo_SelectedIndexChanged);
			//
			// m_columnsCombo
			//
			this.m_columnsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_columnsCombo.FormattingEnabled = true;
			resources.ApplyResources(this.m_columnsCombo, "m_columnsCombo");
			this.m_columnsCombo.Name = "m_columnsCombo";
			//
			// m_cancelButton
			//
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.Name = "m_cancelButton";
			this.m_cancelButton.UseVisualStyleBackColor = true;
			this.m_cancelButton.Click += new System.EventHandler(this.m_cancelButton_Click);
			//
			// m_OkButton
			//
			resources.ApplyResources(this.m_OkButton, "m_OkButton");
			this.m_OkButton.Name = "m_OkButton";
			this.m_OkButton.UseVisualStyleBackColor = true;
			this.m_OkButton.Click += new System.EventHandler(this.m_OkButton_Click);
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.UseVisualStyleBackColor = true;
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// m_mainText
			//
			this.m_mainText.BackColor = System.Drawing.SystemColors.Control;
			this.m_mainText.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_mainText.Cursor = System.Windows.Forms.Cursors.Arrow;
			resources.ApplyResources(this.m_mainText, "m_mainText");
			this.m_mainText.Name = "m_mainText";
			this.m_mainText.ReadOnly = true;
			this.m_mainText.ShortcutsEnabled = false;
			this.m_mainText.TabStop = false;
			//
			// m_partialText
			//
			this.m_partialText.BackColor = System.Drawing.SystemColors.Control;
			this.m_partialText.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_partialText.Cursor = System.Windows.Forms.Cursors.Arrow;
			resources.ApplyResources(this.m_partialText, "m_partialText");
			this.m_partialText.Name = "m_partialText";
			this.m_partialText.ReadOnly = true;
			this.m_partialText.ShortcutsEnabled = false;
			this.m_partialText.TabStop = false;
			//
			// m_bottomStuff
			//
			this.m_bottomStuff.BackColor = System.Drawing.SystemColors.ControlLightLight;
			resources.ApplyResources(this.m_bottomStuff, "m_bottomStuff");
			this.m_bottomStuff.Name = "m_bottomStuff";
			//
			// AdvancedMTDialog
			//
			this.AcceptButton = this.m_OkButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancelButton;
			this.Controls.Add(this.m_bottomStuff);
			this.Controls.Add(this.m_mainText);
			this.Controls.Add(this.m_partialText);
			this.Controls.Add(this.m_columnsCombo);
			this.Controls.Add(this.m_columnLabel);
			this.Controls.Add(this.m_rowLabel);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_OkButton);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_rowsCombo);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AdvancedMTDialog";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private const string s_helpTopic = "khtpAdvancedPreposedDialog";
		private System.Windows.Forms.Label m_rowLabel;
		private System.Windows.Forms.Label m_columnLabel;
		private System.Windows.Forms.ComboBox m_rowsCombo;
		private System.Windows.Forms.ComboBox m_columnsCombo;
		private System.Windows.Forms.Button m_cancelButton;
		private System.Windows.Forms.Button m_OkButton;
		private System.Windows.Forms.Button m_helpButton;
		private System.Windows.Forms.TextBox m_mainText;
		private System.Windows.Forms.TextBox m_partialText;
		private System.Windows.Forms.Panel m_bottomStuff;
	}
}
