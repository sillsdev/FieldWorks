// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView
{
	partial class ButtonOverPanel
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ButtonOverPanel));
			this.panel = new System.Windows.Forms.Panel();
			this.button = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// panel
			// 
			resources.ApplyResources(this.panel, "panel");
			this.panel.Name = "panel";
			// 
			// button
			// 
			resources.ApplyResources(this.button, "button");
			this.button.Name = "button";
			this.button.UseVisualStyleBackColor = true;
			// 
			// ButtonOverPanel
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.button);
			this.Controls.Add(this.panel);
			this.Name = "ButtonOverPanel";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel;
		private System.Windows.Forms.Button button;
	}
}
