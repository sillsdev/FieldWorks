// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DictionaryConfigMgrDlg.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigMgrDlg
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
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DictionaryConfigMgrDlg));
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_listView = new System.Windows.Forms.ListView();
			this.m_btnDelete = new System.Windows.Forms.Button();
			this.m_btnCopy = new System.Windows.Forms.Button();
			this.m_dlgText = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// groupBox1
			//
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Controls.Add(this.m_listView);
			this.groupBox1.Controls.Add(this.m_btnDelete);
			this.groupBox1.Controls.Add(this.m_btnCopy);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// m_listView
			//
			resources.ApplyResources(this.m_listView, "m_listView");
			this.m_listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.m_listView.LabelEdit = true;
			this.m_listView.MinimumSize = new System.Drawing.Size(425, 122);
			this.m_listView.MultiSelect = false;
			this.m_listView.Name = "m_listView";
			this.m_listView.ShowGroups = false;
			this.m_listView.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.m_listView.UseCompatibleStateImageBehavior = false;
			this.m_listView.View = System.Windows.Forms.View.List;
			this.m_listView.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.m_listView_AfterLabelEdit);
			this.m_listView.SelectedIndexChanged += new System.EventHandler(this.m_listView_SelectedIndexChanged);
			this.m_listView.BeforeLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.m_listView_BeforeLabelEdit);
			//
			// m_btnDelete
			//
			resources.ApplyResources(this.m_btnDelete, "m_btnDelete");
			this.m_btnDelete.Name = "m_btnDelete";
			this.m_btnDelete.UseVisualStyleBackColor = true;
			this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
			//
			// m_btnCopy
			//
			resources.ApplyResources(this.m_btnCopy, "m_btnCopy");
			this.m_btnCopy.Name = "m_btnCopy";
			this.m_btnCopy.UseVisualStyleBackColor = true;
			this.m_btnCopy.Click += new System.EventHandler(this.m_btnCopy_Click);
			//
			// m_dlgText
			//
			resources.ApplyResources(this.m_dlgText, "m_dlgText");
			this.m_dlgText.Name = "m_dlgText";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// DictionaryConfigMgrDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_dlgText);
			this.Controls.Add(this.groupBox1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DictionaryConfigMgrDlg";
			this.ShowIcon = false;
			this.TopMost = true;
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button m_btnDelete;
		private System.Windows.Forms.Button m_btnCopy;
		private System.Windows.Forms.Label m_dlgText;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.ListView m_listView;
	}
}