// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwFontTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class FwFontTab
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
			System.Windows.Forms.GroupBox groupBox2;
			System.Windows.Forms.Label label12;
			System.Windows.Forms.Label label11;
			System.Windows.Forms.Label label10;
			System.Windows.Forms.Label label9;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwFontTab));
			this.m_lstWritingSystems = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.m_FontAttributes = new SIL.FieldWorks.FwCoreDlgControls.FwFontAttributes();
			this.m_cboFontSize = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			this.m_cboFontNames = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			groupBox2 = new System.Windows.Forms.GroupBox();
			label12 = new System.Windows.Forms.Label();
			label11 = new System.Windows.Forms.Label();
			label10 = new System.Windows.Forms.Label();
			label9 = new System.Windows.Forms.Label();
			groupBox2.SuspendLayout();
			this.SuspendLayout();
			//
			// groupBox2
			//
			groupBox2.Controls.Add(this.m_FontAttributes);
			groupBox2.Controls.Add(this.m_cboFontSize);
			groupBox2.Controls.Add(label12);
			groupBox2.Controls.Add(this.m_cboFontNames);
			groupBox2.Controls.Add(label11);
			resources.ApplyResources(groupBox2, "groupBox2");
			groupBox2.Name = "groupBox2";
			groupBox2.TabStop = false;
			//
			// label12
			//
			resources.ApplyResources(label12, "label12");
			label12.Name = "label12";
			//
			// label11
			//
			resources.ApplyResources(label11, "label11");
			label11.Name = "label11";
			//
			// label10
			//
			resources.ApplyResources(label10, "label10");
			label10.Name = "label10";
			//
			// label9
			//
			resources.ApplyResources(label9, "label9");
			label9.Name = "label9";
			//
			// m_lstWritingSystems
			//
			this.m_lstWritingSystems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2});
			this.m_lstWritingSystems.HideSelection = false;
			resources.ApplyResources(this.m_lstWritingSystems, "m_lstWritingSystems");
			this.m_lstWritingSystems.MultiSelect = false;
			this.m_lstWritingSystems.Name = "m_lstWritingSystems";
			this.m_lstWritingSystems.UseCompatibleStateImageBehavior = false;
			this.m_lstWritingSystems.View = System.Windows.Forms.View.Details;
			this.m_lstWritingSystems.SelectedIndexChanged += new System.EventHandler(this.m_lstWritingSystems_SelectedIndexChanged);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// m_FontAttributes
			//
			this.m_FontAttributes.FontFeaturesTag = true;
			resources.ApplyResources(this.m_FontAttributes, "m_FontAttributes");
			this.m_FontAttributes.Name = "m_FontAttributes";
			this.m_FontAttributes.ShowingInheritedProperties = false;
			this.m_FontAttributes.ValueChanged += new System.EventHandler(this.ValueChanged);
			//
			// m_cboFontSize
			//
			this.m_cboFontSize.AdjustedSelectedIndex = -1;
			this.m_cboFontSize.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboFontSize.FormattingEnabled = true;
			this.m_cboFontSize.Items.AddRange(new object[] {
			resources.GetString("m_cboFontSize.Items"),
			resources.GetString("m_cboFontSize.Items1"),
			resources.GetString("m_cboFontSize.Items2"),
			resources.GetString("m_cboFontSize.Items3"),
			resources.GetString("m_cboFontSize.Items4"),
			resources.GetString("m_cboFontSize.Items5"),
			resources.GetString("m_cboFontSize.Items6"),
			resources.GetString("m_cboFontSize.Items7"),
			resources.GetString("m_cboFontSize.Items8"),
			resources.GetString("m_cboFontSize.Items9"),
			resources.GetString("m_cboFontSize.Items10"),
			resources.GetString("m_cboFontSize.Items11"),
			resources.GetString("m_cboFontSize.Items12"),
			resources.GetString("m_cboFontSize.Items13"),
			resources.GetString("m_cboFontSize.Items14"),
			resources.GetString("m_cboFontSize.Items15"),
			resources.GetString("m_cboFontSize.Items16")});
			resources.ApplyResources(this.m_cboFontSize, "m_cboFontSize");
			this.m_cboFontSize.Name = "m_cboFontSize";
			this.m_cboFontSize.ShowingInheritedProperties = true;
			this.m_cboFontSize.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
			this.m_cboFontSize.TextUpdate += new System.EventHandler(this.m_cboFontSize_TextUpdate);
			//
			// m_cboFontNames
			//
			this.m_cboFontNames.AdjustedSelectedIndex = -1;
			this.m_cboFontNames.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboFontNames.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboFontNames.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboFontNames, "m_cboFontNames");
			this.m_cboFontNames.Name = "m_cboFontNames";
			this.m_cboFontNames.ShowingInheritedProperties = true;
			this.m_cboFontNames.SelectedIndexChanged += new System.EventHandler(this.m_cboFontNames_SelectedIndexChanged);
			//
			// FwFontTab
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(groupBox2);
			this.Controls.Add(label10);
			this.Controls.Add(this.m_lstWritingSystems);
			this.Controls.Add(label9);
			this.Name = "FwFontTab";
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboFontSize;
		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboFontNames;
		private System.Windows.Forms.ListView m_lstWritingSystems;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private FwFontAttributes m_FontAttributes;
	}
}
