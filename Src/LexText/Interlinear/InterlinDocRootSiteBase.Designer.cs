// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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

				if (m_contextButton != null && !Controls.Contains(m_contextButton))
					m_contextButton.Dispose();
			}
			m_vc = null;
			m_contextButton = null;
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
