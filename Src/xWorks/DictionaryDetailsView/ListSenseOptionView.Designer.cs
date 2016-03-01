namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class ListSenseOptionView
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ListSenseOptionView));
			this.listView = new System.Windows.Forms.ListView();
			this.invisibleHeaderToSetColWidth = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.checkBoxDisplayOption = new System.Windows.Forms.CheckBox();
			this.buttonDown = new System.Windows.Forms.Button();
			this.buttonUp = new System.Windows.Forms.Button();
			this.labelListView = new System.Windows.Forms.Label();
			this.checkBoxSenseInPara = new System.Windows.Forms.CheckBox();
			this.groupBoxSenseNumber = new System.Windows.Forms.GroupBox();
			this.buttonStyles = new System.Windows.Forms.Button();
			this.dropDownStyle = new System.Windows.Forms.ComboBox();
			this.checkBoxNumberSingleSense = new System.Windows.Forms.CheckBox();
			this.textBoxBefore = new System.Windows.Forms.TextBox();
			this.textBoxAfter = new System.Windows.Forms.TextBox();
			this.dropDownNumberingStyle = new System.Windows.Forms.ComboBox();
			this.labelBefore = new System.Windows.Forms.Label();
			this.labelNumberingStyle = new System.Windows.Forms.Label();
			this.labelAfter = new System.Windows.Forms.Label();
			this.labelStyle = new System.Windows.Forms.Label();
			this.checkBoxShowGrammarFirst = new System.Windows.Forms.CheckBox();
			this.groupBoxSenseNumber.SuspendLayout();
			this.SuspendLayout();
			// 
			// listView
			// 
			resources.ApplyResources(this.listView, "listView");
			this.listView.CheckBoxes = true;
			this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.invisibleHeaderToSetColWidth});
			this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView.HideSelection = false;
			this.listView.MultiSelect = false;
			this.listView.Name = "listView";
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = System.Windows.Forms.View.Details;
			// 
			// invisibleHeaderToSetColWidth
			// 
			resources.ApplyResources(this.invisibleHeaderToSetColWidth, "invisibleHeaderToSetColWidth");
			// 
			// checkBoxDisplayOption
			// 
			resources.ApplyResources(this.checkBoxDisplayOption, "checkBoxDisplayOption");
			this.checkBoxDisplayOption.Name = "checkBoxDisplayOption";
			this.checkBoxDisplayOption.UseVisualStyleBackColor = true;
			// 
			// buttonDown
			// 
			resources.ApplyResources(this.buttonDown, "buttonDown");
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.UseVisualStyleBackColor = true;
			// 
			// buttonUp
			// 
			resources.ApplyResources(this.buttonUp, "buttonUp");
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.UseVisualStyleBackColor = true;
			// 
			// labelListView
			// 
			resources.ApplyResources(this.labelListView, "labelListView");
			this.labelListView.Name = "labelListView";
			// 
			// checkBoxSenseInPara
			// 
			resources.ApplyResources(this.checkBoxSenseInPara, "checkBoxSenseInPara");
			this.checkBoxSenseInPara.Name = "checkBoxSenseInPara";
			this.checkBoxSenseInPara.UseVisualStyleBackColor = true;
			// 
			// groupBoxSenseNumber
			// 
			this.groupBoxSenseNumber.Controls.Add(this.buttonStyles);
			this.groupBoxSenseNumber.Controls.Add(this.dropDownStyle);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxNumberSingleSense);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxBefore);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxAfter);
			this.groupBoxSenseNumber.Controls.Add(this.dropDownNumberingStyle);
			this.groupBoxSenseNumber.Controls.Add(this.labelBefore);
			this.groupBoxSenseNumber.Controls.Add(this.labelNumberingStyle);
			this.groupBoxSenseNumber.Controls.Add(this.labelAfter);
			this.groupBoxSenseNumber.Controls.Add(this.labelStyle);
			resources.ApplyResources(this.groupBoxSenseNumber, "groupBoxSenseNumber");
			this.groupBoxSenseNumber.Name = "groupBoxSenseNumber";
			this.groupBoxSenseNumber.TabStop = false;
			// 
			// buttonStyles
			// 
			resources.ApplyResources(this.buttonStyles, "buttonStyles");
			this.buttonStyles.Name = "buttonStyles";
			this.buttonStyles.UseVisualStyleBackColor = true;
			// 
			// dropDownStyle
			// 
			resources.ApplyResources(this.dropDownStyle, "dropDownStyle");
			this.dropDownStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownStyle.FormattingEnabled = true;
			this.dropDownStyle.Name = "dropDownStyle";
			// 
			// checkBoxNumberSingleSense
			// 
			resources.ApplyResources(this.checkBoxNumberSingleSense, "checkBoxNumberSingleSense");
			this.checkBoxNumberSingleSense.Name = "checkBoxNumberSingleSense";
			this.checkBoxNumberSingleSense.UseVisualStyleBackColor = true;
			// 
			// textBoxBefore
			// 
			resources.ApplyResources(this.textBoxBefore, "textBoxBefore");
			this.textBoxBefore.Name = "textBoxBefore";
			// 
			// textBoxAfter
			// 
			resources.ApplyResources(this.textBoxAfter, "textBoxAfter");
			this.textBoxAfter.Name = "textBoxAfter";
			// 
			// dropDownNumberingStyle
			// 
			resources.ApplyResources(this.dropDownNumberingStyle, "dropDownNumberingStyle");
			this.dropDownNumberingStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownNumberingStyle.FormattingEnabled = true;
			this.dropDownNumberingStyle.Name = "dropDownNumberingStyle";
			// 
			// labelBefore
			// 
			resources.ApplyResources(this.labelBefore, "labelBefore");
			this.labelBefore.Name = "labelBefore";
			// 
			// labelNumberingStyle
			// 
			resources.ApplyResources(this.labelNumberingStyle, "labelNumberingStyle");
			this.labelNumberingStyle.Name = "labelNumberingStyle";
			// 
			// labelAfter
			// 
			resources.ApplyResources(this.labelAfter, "labelAfter");
			this.labelAfter.Name = "labelAfter";
			// 
			// labelStyle
			// 
			resources.ApplyResources(this.labelStyle, "labelStyle");
			this.labelStyle.Name = "labelStyle";
			// 
			// checkBoxShowGrammarFirst
			// 
			resources.ApplyResources(this.checkBoxShowGrammarFirst, "checkBoxShowGrammarFirst");
			this.checkBoxShowGrammarFirst.Name = "checkBoxShowGrammarFirst";
			this.checkBoxShowGrammarFirst.UseVisualStyleBackColor = true;
			// 
			// ListSenseOptionView
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.listView);
			this.Controls.Add(this.checkBoxDisplayOption);
			this.Controls.Add(this.buttonDown);
			this.Controls.Add(this.buttonUp);
			this.Controls.Add(this.labelListView);
			this.Controls.Add(this.checkBoxSenseInPara);
			this.Controls.Add(this.groupBoxSenseNumber);
			this.Controls.Add(this.checkBoxShowGrammarFirst);
			this.MaximumSize = new System.Drawing.Size(0, 330);
			this.MinimumSize = new System.Drawing.Size(320, 330);
			this.Name = "ListSenseOptionView";
			this.groupBoxSenseNumber.ResumeLayout(false);
			this.groupBoxSenseNumber.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.ColumnHeader invisibleHeaderToSetColWidth;
		private System.Windows.Forms.CheckBox checkBoxDisplayOption;
		private System.Windows.Forms.Button buttonDown;
		private System.Windows.Forms.Button buttonUp;
		private System.Windows.Forms.Label labelListView;
		private System.Windows.Forms.CheckBox checkBoxSenseInPara;
		private System.Windows.Forms.GroupBox groupBoxSenseNumber;
		private System.Windows.Forms.Button buttonStyles;
		private System.Windows.Forms.ComboBox dropDownStyle;
		private System.Windows.Forms.CheckBox checkBoxNumberSingleSense;
		private System.Windows.Forms.TextBox textBoxBefore;
		private System.Windows.Forms.TextBox textBoxAfter;
		private System.Windows.Forms.ComboBox dropDownNumberingStyle;
		private System.Windows.Forms.Label labelBefore;
		private System.Windows.Forms.Label labelNumberingStyle;
		private System.Windows.Forms.Label labelAfter;
		private System.Windows.Forms.Label labelStyle;
		private System.Windows.Forms.CheckBox checkBoxShowGrammarFirst;



	}
}
