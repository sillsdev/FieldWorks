namespace SIL.FieldWorks.IText
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (disposing)
			{
				if (ExistingFocusBox != null)
				{
					ExistingFocusBox.Visible = false; // Ensures that the program does not attempt to lay this box out.
					ExistingFocusBox.Dispose();
				}
				if (components != null)
					components.Dispose();
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
