// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.IText
{
	partial class ChooseTextWritingSystemDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChooseTextWritingSystemDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.m_cbWritingSystems = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_cbWritingSystems
			//
			this.m_cbWritingSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbWritingSystems.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbWritingSystems, "m_cbWritingSystems");
			this.m_cbWritingSystems.Name = "m_cbWritingSystems";
			this.m_cbWritingSystems.SelectedIndexChanged += new System.EventHandler(this.m_cbWritingSystems_SelectedIndexChanged);
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.MaximumSize = new System.Drawing.Size(320, 78);
			this.label2.Name = "label2";
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.AccessibleName = "ChooseTextWritingSystemDlg";
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// ChooseTextWritingSystemDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_cbWritingSystems);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChooseTextWritingSystemDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox m_cbWritingSystems;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnHelp;
	}
}
