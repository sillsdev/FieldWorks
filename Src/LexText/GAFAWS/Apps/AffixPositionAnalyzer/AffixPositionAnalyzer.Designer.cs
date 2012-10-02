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
// File: AffixPositionAnalyzer.cs
// Responsibility: RandyR
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.GAFAWS.Apps.AffixPositionAnalyzer
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	partial class AffixPositionAnalyzer
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
			this.m_btnProcess = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_lvConverters = new System.Windows.Forms.ListView();
			this.m_chConverter = new System.Windows.Forms.ColumnHeader();
			this.label1 = new System.Windows.Forms.Label();
			this.m_tbDescription = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// m_btnProcess
			//
			this.m_btnProcess.Location = new System.Drawing.Point(110, 322);
			this.m_btnProcess.Name = "m_btnProcess";
			this.m_btnProcess.Size = new System.Drawing.Size(75, 23);
			this.m_btnProcess.TabIndex = 0;
			this.m_btnProcess.Text = "Process...";
			this.m_btnProcess.UseVisualStyleBackColor = true;
			this.m_btnProcess.Click += new System.EventHandler(this.m_btnProcess_Click);
			//
			// m_btnClose
			//
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnClose.Location = new System.Drawing.Point(219, 322);
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.Size = new System.Drawing.Size(75, 23);
			this.m_btnClose.TabIndex = 1;
			this.m_btnClose.Text = "Close";
			this.m_btnClose.UseVisualStyleBackColor = true;
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// m_lvConverters
			//
			this.m_lvConverters.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_chConverter});
			this.m_lvConverters.FullRowSelect = true;
			this.m_lvConverters.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvConverters.HideSelection = false;
			this.m_lvConverters.Location = new System.Drawing.Point(6, 24);
			this.m_lvConverters.MultiSelect = false;
			this.m_lvConverters.Name = "m_lvConverters";
			this.m_lvConverters.Size = new System.Drawing.Size(176, 280);
			this.m_lvConverters.TabIndex = 2;
			this.m_lvConverters.UseCompatibleStateImageBehavior = false;
			this.m_lvConverters.View = System.Windows.Forms.View.Details;
			this.m_lvConverters.DoubleClick += new System.EventHandler(this.m_lvConverters_DoubleClick);
			this.m_lvConverters.SelectedIndexChanged += new System.EventHandler(this.m_lvConverters_SelectedIndexChanged);
			//
			// m_chConverter
			//
			this.m_chConverter.Text = "Converter";
			this.m_chConverter.Width = 170;
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 5);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(104, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Available Converters";
			//
			// m_tbDescription
			//
			this.m_tbDescription.Enabled = false;
			this.m_tbDescription.Location = new System.Drawing.Point(198, 24);
			this.m_tbDescription.Multiline = true;
			this.m_tbDescription.Name = "m_tbDescription";
			this.m_tbDescription.Size = new System.Drawing.Size(203, 280);
			this.m_tbDescription.TabIndex = 4;
			//
			// AffixPositionAnalyzer
			//
			this.AcceptButton = this.m_btnClose;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(426, 351);
			this.Controls.Add(this.m_tbDescription);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_lvConverters);
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnProcess);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "AffixPositionAnalyzer";
			this.Text = "Affix Position Analyzer";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btnProcess;
		private System.Windows.Forms.Button m_btnClose;
		private System.Windows.Forms.ListView m_lvConverters;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ColumnHeader m_chConverter;
		private System.Windows.Forms.TextBox m_tbDescription;
	}
}
