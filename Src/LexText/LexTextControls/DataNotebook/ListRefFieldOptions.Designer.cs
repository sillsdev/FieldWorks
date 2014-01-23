// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ListRefFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class ListRefFieldOptions
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

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ListRefFieldOptions));
			this.m_chkDelimMultiEnable = new System.Windows.Forms.CheckBox();
			this.m_chkDelimSubEnable = new System.Windows.Forms.CheckBox();
			this.m_chkBetweenEnable = new System.Windows.Forms.CheckBox();
			this.m_chkOnlyBeforeEnable = new System.Windows.Forms.CheckBox();
			this.m_chkDiscardNewStuff = new System.Windows.Forms.CheckBox();
			this.m_tbBetweenAfter = new System.Windows.Forms.TextBox();
			this.m_tbOnlyBefore = new System.Windows.Forms.TextBox();
			this.m_tbDelimMulti = new System.Windows.Forms.TextBox();
			this.m_tbDelimSub = new System.Windows.Forms.TextBox();
			this.m_tbBetweenBefore = new System.Windows.Forms.TextBox();
			this.m_lblAnd = new System.Windows.Forms.Label();
			this.m_lvSubstitutions = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.m_lblTextChanges = new System.Windows.Forms.Label();
			this.m_btnAddSubst = new System.Windows.Forms.Button();
			this.m_btnModifySubst = new System.Windows.Forms.Button();
			this.m_btnDeleteSubst = new System.Windows.Forms.Button();
			this.m_tbDefaultValue = new System.Windows.Forms.TextBox();
			this.m_lblDefault = new System.Windows.Forms.Label();
			this.m_rbMatchAbbr = new System.Windows.Forms.RadioButton();
			this.m_rbMatchName = new System.Windows.Forms.RadioButton();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_cbWritingSystem = new System.Windows.Forms.ComboBox();
			this.m_btnAddWritingSystem = new SIL.FieldWorks.LexText.Controls.AddWritingSystemButton(this.components);
			this.m_lblWritingSystem = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_chkDelimMultiEnable
			//
			resources.ApplyResources(this.m_chkDelimMultiEnable, "m_chkDelimMultiEnable");
			this.m_chkDelimMultiEnable.Name = "m_chkDelimMultiEnable";
			this.m_toolTip.SetToolTip(this.m_chkDelimMultiEnable, resources.GetString("m_chkDelimMultiEnable.ToolTip"));
			this.m_chkDelimMultiEnable.UseVisualStyleBackColor = true;
			this.m_chkDelimMultiEnable.CheckedChanged += new System.EventHandler(this.m_chkDelimMultiEnable_CheckedChanged);
			//
			// m_chkDelimSubEnable
			//
			resources.ApplyResources(this.m_chkDelimSubEnable, "m_chkDelimSubEnable");
			this.m_chkDelimSubEnable.Name = "m_chkDelimSubEnable";
			this.m_toolTip.SetToolTip(this.m_chkDelimSubEnable, resources.GetString("m_chkDelimSubEnable.ToolTip"));
			this.m_chkDelimSubEnable.UseVisualStyleBackColor = true;
			this.m_chkDelimSubEnable.CheckedChanged += new System.EventHandler(this.m_chkDelimSubEnable_CheckedChanged);
			//
			// m_chkBetweenEnable
			//
			resources.ApplyResources(this.m_chkBetweenEnable, "m_chkBetweenEnable");
			this.m_chkBetweenEnable.Name = "m_chkBetweenEnable";
			this.m_toolTip.SetToolTip(this.m_chkBetweenEnable, resources.GetString("m_chkBetweenEnable.ToolTip"));
			this.m_chkBetweenEnable.UseVisualStyleBackColor = true;
			this.m_chkBetweenEnable.CheckedChanged += new System.EventHandler(this.m_chkBetweenEnable_CheckedChanged);
			//
			// m_chkOnlyBeforeEnable
			//
			resources.ApplyResources(this.m_chkOnlyBeforeEnable, "m_chkOnlyBeforeEnable");
			this.m_chkOnlyBeforeEnable.Name = "m_chkOnlyBeforeEnable";
			this.m_toolTip.SetToolTip(this.m_chkOnlyBeforeEnable, resources.GetString("m_chkOnlyBeforeEnable.ToolTip"));
			this.m_chkOnlyBeforeEnable.UseVisualStyleBackColor = true;
			this.m_chkOnlyBeforeEnable.CheckedChanged += new System.EventHandler(this.m_chkOnlyBeforeEnable_CheckedChanged);
			//
			// m_chkDiscardNewStuff
			//
			resources.ApplyResources(this.m_chkDiscardNewStuff, "m_chkDiscardNewStuff");
			this.m_chkDiscardNewStuff.Name = "m_chkDiscardNewStuff";
			this.m_toolTip.SetToolTip(this.m_chkDiscardNewStuff, resources.GetString("m_chkDiscardNewStuff.ToolTip"));
			this.m_chkDiscardNewStuff.UseVisualStyleBackColor = true;
			//
			// m_tbBetweenAfter
			//
			resources.ApplyResources(this.m_tbBetweenAfter, "m_tbBetweenAfter");
			this.m_tbBetweenAfter.Name = "m_tbBetweenAfter";
			this.m_toolTip.SetToolTip(this.m_tbBetweenAfter, resources.GetString("m_tbBetweenAfter.ToolTip"));
			//
			// m_tbOnlyBefore
			//
			resources.ApplyResources(this.m_tbOnlyBefore, "m_tbOnlyBefore");
			this.m_tbOnlyBefore.Name = "m_tbOnlyBefore";
			this.m_toolTip.SetToolTip(this.m_tbOnlyBefore, resources.GetString("m_tbOnlyBefore.ToolTip"));
			//
			// m_tbDelimMulti
			//
			resources.ApplyResources(this.m_tbDelimMulti, "m_tbDelimMulti");
			this.m_tbDelimMulti.Name = "m_tbDelimMulti";
			this.m_toolTip.SetToolTip(this.m_tbDelimMulti, resources.GetString("m_tbDelimMulti.ToolTip"));
			//
			// m_tbDelimSub
			//
			resources.ApplyResources(this.m_tbDelimSub, "m_tbDelimSub");
			this.m_tbDelimSub.Name = "m_tbDelimSub";
			this.m_toolTip.SetToolTip(this.m_tbDelimSub, resources.GetString("m_tbDelimSub.ToolTip"));
			//
			// m_tbBetweenBefore
			//
			resources.ApplyResources(this.m_tbBetweenBefore, "m_tbBetweenBefore");
			this.m_tbBetweenBefore.Name = "m_tbBetweenBefore";
			this.m_toolTip.SetToolTip(this.m_tbBetweenBefore, resources.GetString("m_tbBetweenBefore.ToolTip"));
			//
			// m_lblAnd
			//
			resources.ApplyResources(this.m_lblAnd, "m_lblAnd");
			this.m_lblAnd.Name = "m_lblAnd";
			//
			// m_lvSubstitutions
			//
			resources.ApplyResources(this.m_lvSubstitutions, "m_lvSubstitutions");
			this.m_lvSubstitutions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2});
			this.m_lvSubstitutions.FullRowSelect = true;
			this.m_lvSubstitutions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvSubstitutions.MultiSelect = false;
			this.m_lvSubstitutions.Name = "m_lvSubstitutions";
			this.m_toolTip.SetToolTip(this.m_lvSubstitutions, resources.GetString("m_lvSubstitutions.ToolTip"));
			this.m_lvSubstitutions.UseCompatibleStateImageBehavior = false;
			this.m_lvSubstitutions.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// m_lblTextChanges
			//
			resources.ApplyResources(this.m_lblTextChanges, "m_lblTextChanges");
			this.m_lblTextChanges.Name = "m_lblTextChanges";
			//
			// m_btnAddSubst
			//
			resources.ApplyResources(this.m_btnAddSubst, "m_btnAddSubst");
			this.m_btnAddSubst.Name = "m_btnAddSubst";
			this.m_toolTip.SetToolTip(this.m_btnAddSubst, resources.GetString("m_btnAddSubst.ToolTip"));
			this.m_btnAddSubst.UseVisualStyleBackColor = true;
			this.m_btnAddSubst.Click += new System.EventHandler(this.m_btnAddSubst_Click);
			//
			// m_btnModifySubst
			//
			resources.ApplyResources(this.m_btnModifySubst, "m_btnModifySubst");
			this.m_btnModifySubst.Name = "m_btnModifySubst";
			this.m_toolTip.SetToolTip(this.m_btnModifySubst, resources.GetString("m_btnModifySubst.ToolTip"));
			this.m_btnModifySubst.UseVisualStyleBackColor = true;
			this.m_btnModifySubst.Click += new System.EventHandler(this.m_btnModifySubst_Click);
			//
			// m_btnDeleteSubst
			//
			resources.ApplyResources(this.m_btnDeleteSubst, "m_btnDeleteSubst");
			this.m_btnDeleteSubst.Name = "m_btnDeleteSubst";
			this.m_toolTip.SetToolTip(this.m_btnDeleteSubst, resources.GetString("m_btnDeleteSubst.ToolTip"));
			this.m_btnDeleteSubst.UseVisualStyleBackColor = true;
			this.m_btnDeleteSubst.Click += new System.EventHandler(this.m_btnDeleteSubst_Click);
			//
			// m_tbDefaultValue
			//
			resources.ApplyResources(this.m_tbDefaultValue, "m_tbDefaultValue");
			this.m_tbDefaultValue.Name = "m_tbDefaultValue";
			this.m_toolTip.SetToolTip(this.m_tbDefaultValue, resources.GetString("m_tbDefaultValue.ToolTip"));
			//
			// m_lblDefault
			//
			resources.ApplyResources(this.m_lblDefault, "m_lblDefault");
			this.m_lblDefault.Name = "m_lblDefault";
			//
			// m_rbMatchAbbr
			//
			resources.ApplyResources(this.m_rbMatchAbbr, "m_rbMatchAbbr");
			this.m_rbMatchAbbr.Name = "m_rbMatchAbbr";
			this.m_rbMatchAbbr.TabStop = true;
			this.m_toolTip.SetToolTip(this.m_rbMatchAbbr, resources.GetString("m_rbMatchAbbr.ToolTip"));
			this.m_rbMatchAbbr.UseVisualStyleBackColor = true;
			this.m_rbMatchAbbr.CheckedChanged += new System.EventHandler(this.m_rbMatchAbbr_CheckedChanged);
			//
			// m_rbMatchName
			//
			resources.ApplyResources(this.m_rbMatchName, "m_rbMatchName");
			this.m_rbMatchName.Name = "m_rbMatchName";
			this.m_rbMatchName.TabStop = true;
			this.m_toolTip.SetToolTip(this.m_rbMatchName, resources.GetString("m_rbMatchName.ToolTip"));
			this.m_rbMatchName.UseVisualStyleBackColor = true;
			this.m_rbMatchName.CheckedChanged += new System.EventHandler(this.m_rbMatchName_CheckedChanged);
			//
			// m_toolTip
			//
			this.m_toolTip.IsBalloon = true;
			//
			// m_cbWritingSystem
			//
			this.m_cbWritingSystem.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbWritingSystem, "m_cbWritingSystem");
			this.m_cbWritingSystem.Name = "m_cbWritingSystem";
			this.m_toolTip.SetToolTip(this.m_cbWritingSystem, resources.GetString("m_cbWritingSystem.ToolTip"));
			//
			// m_btnAddWritingSystem
			//
			resources.ApplyResources(this.m_btnAddWritingSystem, "m_btnAddWritingSystem");
			this.m_btnAddWritingSystem.Name = "m_btnAddWritingSystem";
			this.m_btnAddWritingSystem.UseVisualStyleBackColor = true;
			this.m_btnAddWritingSystem.WritingSystemAdded += new System.EventHandler(this.m_btnAddWritingSystem_WritingSystemAdded);
			//
			// m_lblWritingSystem
			//
			resources.ApplyResources(this.m_lblWritingSystem, "m_lblWritingSystem");
			this.m_lblWritingSystem.Name = "m_lblWritingSystem";
			//
			// ListRefFieldOptions
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_lblWritingSystem);
			this.Controls.Add(this.m_btnAddWritingSystem);
			this.Controls.Add(this.m_cbWritingSystem);
			this.Controls.Add(this.m_rbMatchName);
			this.Controls.Add(this.m_rbMatchAbbr);
			this.Controls.Add(this.m_lblDefault);
			this.Controls.Add(this.m_tbDefaultValue);
			this.Controls.Add(this.m_btnDeleteSubst);
			this.Controls.Add(this.m_btnModifySubst);
			this.Controls.Add(this.m_btnAddSubst);
			this.Controls.Add(this.m_lblTextChanges);
			this.Controls.Add(this.m_lvSubstitutions);
			this.Controls.Add(this.m_lblAnd);
			this.Controls.Add(this.m_tbBetweenBefore);
			this.Controls.Add(this.m_tbDelimSub);
			this.Controls.Add(this.m_tbDelimMulti);
			this.Controls.Add(this.m_tbOnlyBefore);
			this.Controls.Add(this.m_tbBetweenAfter);
			this.Controls.Add(this.m_chkDiscardNewStuff);
			this.Controls.Add(this.m_chkOnlyBeforeEnable);
			this.Controls.Add(this.m_chkBetweenEnable);
			this.Controls.Add(this.m_chkDelimSubEnable);
			this.Controls.Add(this.m_chkDelimMultiEnable);
			this.Name = "ListRefFieldOptions";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox m_chkDelimMultiEnable;
		private System.Windows.Forms.CheckBox m_chkDelimSubEnable;
		private System.Windows.Forms.CheckBox m_chkBetweenEnable;
		private System.Windows.Forms.CheckBox m_chkOnlyBeforeEnable;
		private System.Windows.Forms.CheckBox m_chkDiscardNewStuff;
		private System.Windows.Forms.TextBox m_tbBetweenAfter;
		private System.Windows.Forms.TextBox m_tbOnlyBefore;
		private System.Windows.Forms.TextBox m_tbDelimMulti;
		private System.Windows.Forms.TextBox m_tbDelimSub;
		private System.Windows.Forms.TextBox m_tbBetweenBefore;
		private System.Windows.Forms.Label m_lblAnd;
		private System.Windows.Forms.ListView m_lvSubstitutions;
		private System.Windows.Forms.Label m_lblTextChanges;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button m_btnAddSubst;
		private System.Windows.Forms.Button m_btnModifySubst;
		private System.Windows.Forms.Button m_btnDeleteSubst;
		private System.Windows.Forms.TextBox m_tbDefaultValue;
		private System.Windows.Forms.Label m_lblDefault;
		private System.Windows.Forms.RadioButton m_rbMatchAbbr;
		private System.Windows.Forms.RadioButton m_rbMatchName;
		private System.Windows.Forms.ToolTip m_toolTip;
		private AddWritingSystemButton m_btnAddWritingSystem;
		private System.Windows.Forms.ComboBox m_cbWritingSystem;
		private System.Windows.Forms.Label m_lblWritingSystem;
	}
}
