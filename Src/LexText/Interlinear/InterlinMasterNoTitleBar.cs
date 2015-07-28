// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is like its superclass, except it reomves the superclass' top control and pane bar.
	/// </summary>
	public class InterlinMasterNoTitleBar : InterlinMaster
	{
		private System.ComponentModel.IContainer components = null;

		public InterlinMasterNoTitleBar()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		/// <summary>
		/// Override method to get rid of up to two controls.
		/// </summary>
		protected override void AddPaneBar()
		{
			base.AddPaneBar();

			if (m_informationBar != null)
			{
				Controls.Remove(m_informationBar);
				m_informationBar.Dispose();
				m_informationBar = null;
			}

			if (TitleContentsPane != null)
			{
				Controls.Remove(TitleContentsPane);
				TitleContentsPane.Dispose();
				TitleContentsPane = null;
			}
		}
	}
}
