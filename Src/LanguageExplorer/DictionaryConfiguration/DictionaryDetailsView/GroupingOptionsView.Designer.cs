// Copyright (c) 2016-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;

namespace LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView
{
	partial class GroupingOptionsView
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GroupingOptionsView));
			this.descriptionBox = new System.Windows.Forms.TextBox();
			this.displayInParagraph = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// descriptionBox
			// 
			resources.ApplyResources(this.descriptionBox, "descriptionBox");
			this.descriptionBox.Name = "descriptionBox";
			// 
			// displayInParagraph
			// 
			resources.ApplyResources(this.displayInParagraph, "displayInParagraph");
			this.displayInParagraph.Name = "displayInParagraph";
			this.displayInParagraph.UseVisualStyleBackColor = true;
			// 
			// GroupingOptionsView
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.displayInParagraph);
			this.Controls.Add(this.descriptionBox);
			this.Name = "GroupingOptionsView";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox descriptionBox;
		private System.Windows.Forms.CheckBox displayInParagraph;
	}
}
