// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MultiLingualFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class StringFieldOptions
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StringFieldOptions));
			this.m_cbWritingSystem = new System.Windows.Forms.ComboBox();
			this.m_lblWritingSystem = new System.Windows.Forms.Label();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_btnAddWritingSystem = new SIL.FieldWorks.LexText.Controls.AddWritingSystemButton(this.components);
			this.SuspendLayout();
			//
			// m_cbWritingSystem
			//
			this.m_cbWritingSystem.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbWritingSystem, "m_cbWritingSystem");
			this.m_cbWritingSystem.Name = "m_cbWritingSystem";
			this.m_toolTip.SetToolTip(this.m_cbWritingSystem, resources.GetString("m_cbWritingSystem.ToolTip"));
			//
			// m_lblWritingSystem
			//
			resources.ApplyResources(this.m_lblWritingSystem, "m_lblWritingSystem");
			this.m_lblWritingSystem.Name = "m_lblWritingSystem";
			//
			// m_btnAddWritingSystem
			//
			resources.ApplyResources(this.m_btnAddWritingSystem, "m_btnAddWritingSystem");
			this.m_btnAddWritingSystem.Name = "m_btnAddWritingSystem";
			this.m_btnAddWritingSystem.UseVisualStyleBackColor = true;
			this.m_btnAddWritingSystem.WritingSystemAdded += new System.EventHandler(this.m_btnAddWritingSystem_WritingSystemAdded);
			//
			// StringFieldOptions
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnAddWritingSystem);
			this.Controls.Add(this.m_cbWritingSystem);
			this.Controls.Add(this.m_lblWritingSystem);
			this.Name = "StringFieldOptions";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox m_cbWritingSystem;
		private System.Windows.Forms.Label m_lblWritingSystem;
		private System.Windows.Forms.ToolTip m_toolTip;
		private AddWritingSystemButton m_btnAddWritingSystem;
	}
}
