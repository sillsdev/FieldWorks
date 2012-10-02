namespace SIL.FieldWorks.IText
{
	partial class InterlinDocRootSiteBase
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
				if (components != null)
					components.Dispose();

				// Do this, before calling base.
				if (m_sda != null)
					m_sda.RemoveNotification(this);
				if (m_vc != null)
					m_vc.Dispose();
			}
			m_vc = null;
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
			// InterlinDocRootSiteBase
			//
			this.AccessibleName = "InterlinDocRootSiteBase";
			this.Name = "InterlinDocRootSiteBase";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
