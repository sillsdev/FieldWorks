// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	partial class InterlinDocForAnalysis
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				if (ExistingFocusBox != null)
				{
					ExistingFocusBox.Visible = false; // Ensures that the program does not attempt to lay this box out.
					ExistingFocusBox.Dispose();
				}
				components?.Dispose();
			}
			ExistingFocusBox = null;
			components = null;
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// InterlinDocForAnalysis
			//
			this.ForEditing = true;
			this.Name = "InterlinDocForAnalysis";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
