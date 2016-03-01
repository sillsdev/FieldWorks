namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class ParagraphOptionsView
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParagraphOptionsView));
			this.labelParaStyle = new System.Windows.Forms.Label();
			this.buttonParaStyles = new System.Windows.Forms.Button();
			this.dropDownParaStyle = new System.Windows.Forms.ComboBox();
			this.labelContParaStyle = new System.Windows.Forms.Label();
			this.buttonContParaStyles = new System.Windows.Forms.Button();
			this.dropDownContParaStyle = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// labelParaStyle
			// 
			resources.ApplyResources(this.labelParaStyle, "labelParaStyle");
			this.labelParaStyle.Name = "labelParaStyle";
			// 
			// buttonParaStyles
			// 
			resources.ApplyResources(this.buttonParaStyles, "buttonParaStyles");
			this.buttonParaStyles.Name = "buttonParaStyles";
			this.buttonParaStyles.UseVisualStyleBackColor = true;
			// 
			// dropDownParaStyle
			// 
			resources.ApplyResources(this.dropDownParaStyle, "dropDownParaStyle");
			this.dropDownParaStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownParaStyle.FormattingEnabled = true;
			this.dropDownParaStyle.Name = "dropDownParaStyle";
			// 
			// labelContParaStyle
			// 
			resources.ApplyResources(this.labelContParaStyle, "labelContParaStyle");
			this.labelContParaStyle.Name = "labelContParaStyle";
			// 
			// buttonContParaStyles
			// 
			resources.ApplyResources(this.buttonContParaStyles, "buttonContParaStyles");
			this.buttonContParaStyles.Name = "buttonContParaStyles";
			this.buttonContParaStyles.UseVisualStyleBackColor = true;
			// 
			// dropDownContParaStyle
			// 
			resources.ApplyResources(this.dropDownContParaStyle, "dropDownContParaStyle");
			this.dropDownContParaStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownContParaStyle.FormattingEnabled = true;
			this.dropDownContParaStyle.Name = "dropDownContParaStyle";
			// 
			// ParagraphOptionsView
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.buttonContParaStyles);
			this.Controls.Add(this.dropDownContParaStyle);
			this.Controls.Add(this.labelContParaStyle);
			this.Controls.Add(this.buttonParaStyles);
			this.Controls.Add(this.dropDownParaStyle);
			this.Controls.Add(this.labelParaStyle);
			this.MaximumSize = new System.Drawing.Size(0, 113);
			this.MinimumSize = new System.Drawing.Size(291, 113);
			this.Name = "ParagraphOptionsView";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelParaStyle;
		private System.Windows.Forms.Button buttonParaStyles;
		private System.Windows.Forms.ComboBox dropDownParaStyle;
		private System.Windows.Forms.Label labelContParaStyle;
		private System.Windows.Forms.Button buttonContParaStyles;
		private System.Windows.Forms.ComboBox dropDownContParaStyle;
	}
}
