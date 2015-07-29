// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Palaso.UI.WindowsForms.SIL;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class DetailsView
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
				components.Dispose();
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DetailsView));
			this.textBoxBefore = new System.Windows.Forms.TextBox();
			this.textBoxBetween = new System.Windows.Forms.TextBox();
			this.textBoxAfter = new System.Windows.Forms.TextBox();
			this.labelBefore = new System.Windows.Forms.Label();
			this.labelBetween = new System.Windows.Forms.Label();
			this.labelAfter = new System.Windows.Forms.Label();
			this.dropDownStyle = new System.Windows.Forms.ComboBox();
			this.labelStyle = new System.Windows.Forms.Label();
			this.buttonStyles = new System.Windows.Forms.Button();
			this.panelOptions = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			// 
			// textBoxBefore
			// 
			resources.ApplyResources(this.textBoxBefore, "textBoxBefore");
			this.textBoxBefore.Name = "textBoxBefore";
			// 
			// textBoxBetween
			// 
			resources.ApplyResources(this.textBoxBetween, "textBoxBetween");
			this.textBoxBetween.Name = "textBoxBetween";
			// 
			// textBoxAfter
			// 
			resources.ApplyResources(this.textBoxAfter, "textBoxAfter");
			this.textBoxAfter.Name = "textBoxAfter";
			// 
			// labelBefore
			// 
			resources.ApplyResources(this.labelBefore, "labelBefore");
			this.labelBefore.Name = "labelBefore";
			// 
			// labelBetween
			// 
			resources.ApplyResources(this.labelBetween, "labelBetween");
			this.labelBetween.Name = "labelBetween";
			// 
			// labelAfter
			// 
			resources.ApplyResources(this.labelAfter, "labelAfter");
			this.labelAfter.Name = "labelAfter";
			// 
			// dropDownStyle
			// 
			resources.ApplyResources(this.dropDownStyle, "dropDownStyle");
			this.dropDownStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownStyle.FormattingEnabled = true;
			this.dropDownStyle.Name = "dropDownStyle";
			// 
			// labelStyle
			// 
			resources.ApplyResources(this.labelStyle, "labelStyle");
			this.labelStyle.Name = "labelStyle";
			// 
			// buttonStyles
			// 
			resources.ApplyResources(this.buttonStyles, "buttonStyles");
			this.buttonStyles.Name = "buttonStyles";
			this.buttonStyles.UseVisualStyleBackColor = true;
			// 
			// panelOptions
			// 
			resources.ApplyResources(this.panelOptions, "panelOptions");
			this.panelOptions.Name = "panelOptions";
			// 
			// DetailsView
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.panelOptions);
			this.Controls.Add(this.buttonStyles);
			this.Controls.Add(this.dropDownStyle);
			this.Controls.Add(this.textBoxAfter);
			this.Controls.Add(this.textBoxBetween);
			this.Controls.Add(this.textBoxBefore);
			this.Controls.Add(this.labelBefore);
			this.Controls.Add(this.labelBetween);
			this.Controls.Add(this.labelStyle);
			this.Controls.Add(this.labelAfter);
			this.MinimumSize = new System.Drawing.Size(320, 100);
			this.Name = "DetailsView";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBoxBefore;
		private System.Windows.Forms.TextBox textBoxBetween;
		private System.Windows.Forms.TextBox textBoxAfter;
		private System.Windows.Forms.Label labelBefore;
		private System.Windows.Forms.Label labelBetween;
		private System.Windows.Forms.Label labelAfter;
		private System.Windows.Forms.ComboBox dropDownStyle;
		private System.Windows.Forms.Label labelStyle;
		private System.Windows.Forms.Button buttonStyles;
		private System.Windows.Forms.UserControl optionsView;
		private System.Windows.Forms.Panel panelOptions;
	}
}
