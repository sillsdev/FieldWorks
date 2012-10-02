// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportCharMappingDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class ImportCharMappingDlg
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportCharMappingDlg));
			this.m_groupStdFmt = new System.Windows.Forms.GroupBox();
			this.m_rbEndOfField = new System.Windows.Forms.RadioButton();
			this.m_rbEndOfWord = new System.Windows.Forms.RadioButton();
			this.m_tbEndMkr = new System.Windows.Forms.TextBox();
			this.m_tbBeginMkr = new System.Windows.Forms.TextBox();
			this.m_lblEndMarker = new System.Windows.Forms.Label();
			this.m_lblBeginMkr = new System.Windows.Forms.Label();
			this.m_groupDest = new System.Windows.Forms.GroupBox();
			this.m_chkIgnore = new System.Windows.Forms.CheckBox();
			this.m_btnStyles = new System.Windows.Forms.Button();
			this.m_cbStyle = new System.Windows.Forms.ComboBox();
			this.m_cbWritingSystem = new System.Windows.Forms.ComboBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_lblWritingSystem = new System.Windows.Forms.Label();
			this.m_lblStyle = new System.Windows.Forms.Label();
			this.m_btnAddWS = new SIL.FieldWorks.LexText.Controls.AddWritingSystemButton(this.components);
			this.m_groupStdFmt.SuspendLayout();
			this.m_groupDest.SuspendLayout();
			this.SuspendLayout();
			//
			// m_groupStdFmt
			//
			this.m_groupStdFmt.Controls.Add(this.m_rbEndOfField);
			this.m_groupStdFmt.Controls.Add(this.m_rbEndOfWord);
			this.m_groupStdFmt.Controls.Add(this.m_tbEndMkr);
			this.m_groupStdFmt.Controls.Add(this.m_tbBeginMkr);
			this.m_groupStdFmt.Controls.Add(this.m_lblEndMarker);
			this.m_groupStdFmt.Controls.Add(this.m_lblBeginMkr);
			resources.ApplyResources(this.m_groupStdFmt, "m_groupStdFmt");
			this.m_groupStdFmt.Name = "m_groupStdFmt";
			this.m_groupStdFmt.TabStop = false;
			//
			// m_rbEndOfField
			//
			resources.ApplyResources(this.m_rbEndOfField, "m_rbEndOfField");
			this.m_rbEndOfField.Checked = true;
			this.m_rbEndOfField.Name = "m_rbEndOfField";
			this.m_rbEndOfField.TabStop = true;
			this.m_rbEndOfField.UseVisualStyleBackColor = true;
			this.m_rbEndOfField.CheckedChanged += new System.EventHandler(this.m_rbEndOfField_CheckedChanged);
			//
			// m_rbEndOfWord
			//
			resources.ApplyResources(this.m_rbEndOfWord, "m_rbEndOfWord");
			this.m_rbEndOfWord.Name = "m_rbEndOfWord";
			this.m_rbEndOfWord.UseVisualStyleBackColor = true;
			this.m_rbEndOfWord.CheckedChanged += new System.EventHandler(this.m_rbEndOfWord_CheckedChanged);
			//
			// m_tbEndMkr
			//
			resources.ApplyResources(this.m_tbEndMkr, "m_tbEndMkr");
			this.m_tbEndMkr.Name = "m_tbEndMkr";
			//
			// m_tbBeginMkr
			//
			resources.ApplyResources(this.m_tbBeginMkr, "m_tbBeginMkr");
			this.m_tbBeginMkr.Name = "m_tbBeginMkr";
			//
			// m_lblEndMarker
			//
			resources.ApplyResources(this.m_lblEndMarker, "m_lblEndMarker");
			this.m_lblEndMarker.Name = "m_lblEndMarker";
			//
			// m_lblBeginMkr
			//
			resources.ApplyResources(this.m_lblBeginMkr, "m_lblBeginMkr");
			this.m_lblBeginMkr.Name = "m_lblBeginMkr";
			//
			// m_groupDest
			//
			this.m_groupDest.Controls.Add(this.m_lblStyle);
			this.m_groupDest.Controls.Add(this.m_lblWritingSystem);
			this.m_groupDest.Controls.Add(this.m_chkIgnore);
			this.m_groupDest.Controls.Add(this.m_btnStyles);
			this.m_groupDest.Controls.Add(this.m_cbStyle);
			this.m_groupDest.Controls.Add(this.m_btnAddWS);
			this.m_groupDest.Controls.Add(this.m_cbWritingSystem);
			resources.ApplyResources(this.m_groupDest, "m_groupDest");
			this.m_groupDest.Name = "m_groupDest";
			this.m_groupDest.TabStop = false;
			//
			// m_chkIgnore
			//
			resources.ApplyResources(this.m_chkIgnore, "m_chkIgnore");
			this.m_chkIgnore.Name = "m_chkIgnore";
			this.m_chkIgnore.UseVisualStyleBackColor = true;
			this.m_chkIgnore.CheckedChanged += new System.EventHandler(this.m_chkIgnore_CheckedChanged);
			//
			// m_btnStyles
			//
			resources.ApplyResources(this.m_btnStyles, "m_btnStyles");
			this.m_btnStyles.Name = "m_btnStyles";
			this.m_btnStyles.UseVisualStyleBackColor = true;
			this.m_btnStyles.Click += new System.EventHandler(this.m_btnStyles_Click);
			//
			// m_cbStyle
			//
			resources.ApplyResources(this.m_cbStyle, "m_cbStyle");
			this.m_cbStyle.FormattingEnabled = true;
			this.m_cbStyle.Name = "m_cbStyle";
			//
			// m_cbWritingSystem
			//
			resources.ApplyResources(this.m_cbWritingSystem, "m_cbWritingSystem");
			this.m_cbWritingSystem.FormattingEnabled = true;
			this.m_cbWritingSystem.Name = "m_cbWritingSystem";
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_lblWritingSystem
			//
			resources.ApplyResources(this.m_lblWritingSystem, "m_lblWritingSystem");
			this.m_lblWritingSystem.Name = "m_lblWritingSystem";
			//
			// m_lblStyle
			//
			resources.ApplyResources(this.m_lblStyle, "m_lblStyle");
			this.m_lblStyle.Name = "m_lblStyle";
			//
			// m_btnAddWS
			//
			resources.ApplyResources(this.m_btnAddWS, "m_btnAddWS");
			this.m_btnAddWS.Name = "m_btnAddWS";
			this.m_btnAddWS.UseVisualStyleBackColor = true;
			this.m_btnAddWS.WritingSystemAdded += new System.EventHandler(this.m_btnAddWS_WritingSystemAdded);
			//
			// ImportCharMappingDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_groupDest);
			this.Controls.Add(this.m_groupStdFmt);
			this.Name = "ImportCharMappingDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.m_groupStdFmt.ResumeLayout(false);
			this.m_groupStdFmt.PerformLayout();
			this.m_groupDest.ResumeLayout(false);
			this.m_groupDest.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox m_groupStdFmt;
		private System.Windows.Forms.Label m_lblBeginMkr;
		private System.Windows.Forms.TextBox m_tbEndMkr;
		private System.Windows.Forms.TextBox m_tbBeginMkr;
		private System.Windows.Forms.Label m_lblEndMarker;
		private System.Windows.Forms.GroupBox m_groupDest;
		private System.Windows.Forms.ComboBox m_cbWritingSystem;
		private AddWritingSystemButton m_btnAddWS;
		private System.Windows.Forms.ComboBox m_cbStyle;
		private System.Windows.Forms.Button m_btnStyles;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.RadioButton m_rbEndOfWord;
		private System.Windows.Forms.RadioButton m_rbEndOfField;
		private System.Windows.Forms.CheckBox m_chkIgnore;
		private System.Windows.Forms.Label m_lblWritingSystem;
		private System.Windows.Forms.Label m_lblStyle;
	}
}