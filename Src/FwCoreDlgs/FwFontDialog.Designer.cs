// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwFontDialog.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwFontDialog
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
			System.Windows.Forms.Label lblFontName;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwFontDialog));
			System.Windows.Forms.Label lblFontSize;
			System.Windows.Forms.GroupBox gbAttributes;
			System.Windows.Forms.Button btnOk;
			System.Windows.Forms.Button btnCancel;
			System.Windows.Forms.Button btnHelp;
			System.Windows.Forms.GroupBox gbPreview;
			this.m_FontAttributes = new SIL.FieldWorks.FwCoreDlgControls.FwFontAttributes();
			this.m_preview = new SIL.FieldWorks.Common.Widgets.FwLabel();
			this.m_tbFontName = new System.Windows.Forms.TextBox();
			this.m_lbFontNames = new System.Windows.Forms.ListBox();
			this.m_tbFontSize = new System.Windows.Forms.TextBox();
			this.m_lbFontSizes = new System.Windows.Forms.ListBox();
			lblFontName = new System.Windows.Forms.Label();
			lblFontSize = new System.Windows.Forms.Label();
			gbAttributes = new System.Windows.Forms.GroupBox();
			btnOk = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			gbPreview = new System.Windows.Forms.GroupBox();
			gbAttributes.SuspendLayout();
			gbPreview.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_preview)).BeginInit();
			this.SuspendLayout();
			//
			// lblFontName
			//
			resources.ApplyResources(lblFontName, "lblFontName");
			lblFontName.Name = "lblFontName";
			//
			// lblFontSize
			//
			resources.ApplyResources(lblFontSize, "lblFontSize");
			lblFontSize.Name = "lblFontSize";
			//
			// gbAttributes
			//
			gbAttributes.Controls.Add(this.m_FontAttributes);
			resources.ApplyResources(gbAttributes, "gbAttributes");
			gbAttributes.Name = "gbAttributes";
			gbAttributes.TabStop = false;
			//
			// m_FontAttributes
			//
			this.m_FontAttributes.AllowSuperSubScript = true;
			this.m_FontAttributes.FontFeaturesTag = true;
			resources.ApplyResources(this.m_FontAttributes, "m_FontAttributes");
			this.m_FontAttributes.Name = "m_FontAttributes";
			this.m_FontAttributes.ShowingInheritedProperties = false;
			this.m_FontAttributes.ValueChanged += new System.EventHandler(this.OnAttributeValueChanged);
			//
			// btnOk
			//
			btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(btnOk, "btnOk");
			btnOk.Name = "btnOk";
			btnOk.UseVisualStyleBackColor = true;
			//
			// btnCancel
			//
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.Name = "btnCancel";
			btnCancel.UseVisualStyleBackColor = true;
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.UseVisualStyleBackColor = true;
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// gbPreview
			//
			gbPreview.Controls.Add(this.m_preview);
			resources.ApplyResources(gbPreview, "gbPreview");
			gbPreview.Name = "gbPreview";
			gbPreview.TabStop = false;
			//
			// m_preview
			//
			this.m_preview.controlID = null;
			resources.ApplyResources(this.m_preview, "m_preview");
			this.m_preview.Name = "m_preview";
			this.m_preview.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// m_tbFontName
			//
			resources.ApplyResources(this.m_tbFontName, "m_tbFontName");
			this.m_tbFontName.Name = "m_tbFontName";
			this.m_tbFontName.TextChanged += new System.EventHandler(this.OnFontNameChanged);
			//
			// m_lbFontNames
			//
			this.m_lbFontNames.FormattingEnabled = true;
			resources.ApplyResources(this.m_lbFontNames, "m_lbFontNames");
			this.m_lbFontNames.Name = "m_lbFontNames";
			this.m_lbFontNames.SelectedIndexChanged += new System.EventHandler(this.OnSelectedFontNameIndexChanged);
			//
			// m_tbFontSize
			//
			resources.ApplyResources(this.m_tbFontSize, "m_tbFontSize");
			this.m_tbFontSize.Name = "m_tbFontSize";
			this.m_tbFontSize.TextChanged += new System.EventHandler(this.OnFontSizeTextChanged);
			//
			// m_lbFontSizes
			//
			this.m_lbFontSizes.FormattingEnabled = true;
			this.m_lbFontSizes.Items.AddRange(new object[] {
			resources.GetString("m_lbFontSizes.Items"),
			resources.GetString("m_lbFontSizes.Items1"),
			resources.GetString("m_lbFontSizes.Items2"),
			resources.GetString("m_lbFontSizes.Items3"),
			resources.GetString("m_lbFontSizes.Items4"),
			resources.GetString("m_lbFontSizes.Items5"),
			resources.GetString("m_lbFontSizes.Items6"),
			resources.GetString("m_lbFontSizes.Items7"),
			resources.GetString("m_lbFontSizes.Items8"),
			resources.GetString("m_lbFontSizes.Items9"),
			resources.GetString("m_lbFontSizes.Items10"),
			resources.GetString("m_lbFontSizes.Items11"),
			resources.GetString("m_lbFontSizes.Items12"),
			resources.GetString("m_lbFontSizes.Items13"),
			resources.GetString("m_lbFontSizes.Items14"),
			resources.GetString("m_lbFontSizes.Items15")});
			resources.ApplyResources(this.m_lbFontSizes, "m_lbFontSizes");
			this.m_lbFontSizes.Name = "m_lbFontSizes";
			this.m_lbFontSizes.SelectedIndexChanged += new System.EventHandler(this.OnSelectedFontSizesIndexChanged);
			//
			// FwFontDialog
			//
			this.AcceptButton = btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(this.m_lbFontSizes);
			this.Controls.Add(this.m_tbFontSize);
			this.Controls.Add(this.m_lbFontNames);
			this.Controls.Add(this.m_tbFontName);
			this.Controls.Add(btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(gbPreview);
			this.Controls.Add(gbAttributes);
			this.Controls.Add(lblFontSize);
			this.Controls.Add(lblFontName);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwFontDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			gbAttributes.ResumeLayout(false);
			gbPreview.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_preview)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private SIL.FieldWorks.FwCoreDlgControls.FwFontAttributes m_FontAttributes;
		private System.Windows.Forms.TextBox m_tbFontName;
		/// <summary/>
		protected System.Windows.Forms.ListBox m_lbFontNames;
		/// <summary/>
		protected System.Windows.Forms.TextBox m_tbFontSize;
		/// <summary/>
		protected System.Windows.Forms.ListBox m_lbFontSizes;
		private SIL.FieldWorks.Common.Widgets.FwLabel m_preview;

	}
}
