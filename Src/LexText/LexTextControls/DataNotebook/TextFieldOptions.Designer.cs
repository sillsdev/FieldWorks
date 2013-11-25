// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TextFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class TextFieldOptions
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextFieldOptions));
			this.m_lblWritingSystem = new System.Windows.Forms.Label();
			this.m_cbWritingSystem = new System.Windows.Forms.ComboBox();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_btnStyles = new System.Windows.Forms.Button();
			this.m_cbStyles = new System.Windows.Forms.ComboBox();
			this.m_chkForEachLine = new System.Windows.Forms.CheckBox();
			this.m_chkAfterBlankLine = new System.Windows.Forms.CheckBox();
			this.m_chkWhenIndented = new System.Windows.Forms.CheckBox();
			this.m_chkAfterShortLine = new System.Windows.Forms.CheckBox();
			this.m_tbShortLength = new System.Windows.Forms.TextBox();
			this.m_lblStyle = new System.Windows.Forms.Label();
			this.m_lblStartPara = new System.Windows.Forms.Label();
			this.m_lblShortChars = new System.Windows.Forms.Label();
			this.m_btnAddWritingSystem = new SIL.FieldWorks.LexText.Controls.AddWritingSystemButton(this.components);
			this.SuspendLayout();
			//
			// m_lblWritingSystem
			//
			resources.ApplyResources(this.m_lblWritingSystem, "m_lblWritingSystem");
			this.m_lblWritingSystem.Name = "m_lblWritingSystem";
			//
			// m_cbWritingSystem
			//
			this.m_cbWritingSystem.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbWritingSystem, "m_cbWritingSystem");
			this.m_cbWritingSystem.Name = "m_cbWritingSystem";
			this.m_toolTip.SetToolTip(this.m_cbWritingSystem, resources.GetString("m_cbWritingSystem.ToolTip"));
			//
			// m_btnStyles
			//
			resources.ApplyResources(this.m_btnStyles, "m_btnStyles");
			this.m_btnStyles.Name = "m_btnStyles";
			this.m_toolTip.SetToolTip(this.m_btnStyles, resources.GetString("m_btnStyles.ToolTip"));
			this.m_btnStyles.UseVisualStyleBackColor = true;
			this.m_btnStyles.Click += new System.EventHandler(this.m_btnStyles_Click);
			//
			// m_cbStyles
			//
			resources.ApplyResources(this.m_cbStyles, "m_cbStyles");
			this.m_cbStyles.FormattingEnabled = true;
			this.m_cbStyles.Name = "m_cbStyles";
			this.m_toolTip.SetToolTip(this.m_cbStyles, resources.GetString("m_cbStyles.ToolTip"));
			//
			// m_chkForEachLine
			//
			resources.ApplyResources(this.m_chkForEachLine, "m_chkForEachLine");
			this.m_chkForEachLine.Name = "m_chkForEachLine";
			this.m_toolTip.SetToolTip(this.m_chkForEachLine, resources.GetString("m_chkForEachLine.ToolTip"));
			this.m_chkForEachLine.UseVisualStyleBackColor = true;
			//
			// m_chkAfterBlankLine
			//
			resources.ApplyResources(this.m_chkAfterBlankLine, "m_chkAfterBlankLine");
			this.m_chkAfterBlankLine.Name = "m_chkAfterBlankLine";
			this.m_toolTip.SetToolTip(this.m_chkAfterBlankLine, resources.GetString("m_chkAfterBlankLine.ToolTip"));
			this.m_chkAfterBlankLine.UseVisualStyleBackColor = true;
			//
			// m_chkWhenIndented
			//
			resources.ApplyResources(this.m_chkWhenIndented, "m_chkWhenIndented");
			this.m_chkWhenIndented.Name = "m_chkWhenIndented";
			this.m_toolTip.SetToolTip(this.m_chkWhenIndented, resources.GetString("m_chkWhenIndented.ToolTip"));
			this.m_chkWhenIndented.UseVisualStyleBackColor = true;
			//
			// m_chkAfterShortLine
			//
			resources.ApplyResources(this.m_chkAfterShortLine, "m_chkAfterShortLine");
			this.m_chkAfterShortLine.Name = "m_chkAfterShortLine";
			this.m_toolTip.SetToolTip(this.m_chkAfterShortLine, resources.GetString("m_chkAfterShortLine.ToolTip"));
			this.m_chkAfterShortLine.UseVisualStyleBackColor = true;
			this.m_chkAfterShortLine.CheckedChanged += new System.EventHandler(this.m_chkAfterShortLine_CheckedChanged);
			//
			// m_tbShortLength
			//
			resources.ApplyResources(this.m_tbShortLength, "m_tbShortLength");
			this.m_tbShortLength.Name = "m_tbShortLength";
			this.m_toolTip.SetToolTip(this.m_tbShortLength, resources.GetString("m_tbShortLength.ToolTip"));
			this.m_tbShortLength.TextChanged += new System.EventHandler(this.m_tbShortLength_TextChanged);
			//
			// m_lblStyle
			//
			resources.ApplyResources(this.m_lblStyle, "m_lblStyle");
			this.m_lblStyle.Name = "m_lblStyle";
			//
			// m_lblStartPara
			//
			resources.ApplyResources(this.m_lblStartPara, "m_lblStartPara");
			this.m_lblStartPara.Name = "m_lblStartPara";
			//
			// m_lblShortChars
			//
			resources.ApplyResources(this.m_lblShortChars, "m_lblShortChars");
			this.m_lblShortChars.Name = "m_lblShortChars";
			//
			// m_btnAddWritingSystem
			//
			resources.ApplyResources(this.m_btnAddWritingSystem, "m_btnAddWritingSystem");
			this.m_btnAddWritingSystem.Name = "m_btnAddWritingSystem";
			this.m_btnAddWritingSystem.UseVisualStyleBackColor = true;
			this.m_btnAddWritingSystem.WritingSystemAdded += new System.EventHandler(this.m_btnAddWritingSystem_WritingSystemAdded);
			//
			// TextFieldOptions
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnAddWritingSystem);
			this.Controls.Add(this.m_lblShortChars);
			this.Controls.Add(this.m_tbShortLength);
			this.Controls.Add(this.m_chkAfterShortLine);
			this.Controls.Add(this.m_chkWhenIndented);
			this.Controls.Add(this.m_chkAfterBlankLine);
			this.Controls.Add(this.m_chkForEachLine);
			this.Controls.Add(this.m_lblStartPara);
			this.Controls.Add(this.m_btnStyles);
			this.Controls.Add(this.m_cbStyles);
			this.Controls.Add(this.m_lblStyle);
			this.Controls.Add(this.m_cbWritingSystem);
			this.Controls.Add(this.m_lblWritingSystem);
			this.Name = "TextFieldOptions";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblWritingSystem;
		private System.Windows.Forms.ComboBox m_cbWritingSystem;
		private System.Windows.Forms.ToolTip m_toolTip;
		private System.Windows.Forms.Button m_btnStyles;
		private System.Windows.Forms.ComboBox m_cbStyles;
		private System.Windows.Forms.Label m_lblStyle;
		private System.Windows.Forms.Label m_lblStartPara;
		private System.Windows.Forms.CheckBox m_chkForEachLine;
		private System.Windows.Forms.CheckBox m_chkAfterBlankLine;
		private System.Windows.Forms.CheckBox m_chkWhenIndented;
		private System.Windows.Forms.CheckBox m_chkAfterShortLine;
		private System.Windows.Forms.TextBox m_tbShortLength;
		private System.Windows.Forms.Label m_lblShortChars;
		private AddWritingSystemButton m_btnAddWritingSystem;
	}
}
