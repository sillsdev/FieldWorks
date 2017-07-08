// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.XWorks;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class is like its superclass, except it removes the superclass' top control and pane bar.
	/// </summary>
	public class InterlinMasterNoTitleBar : InterlinMaster
	{
		private System.ComponentModel.IContainer components = null;

		public InterlinMasterNoTitleBar()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		internal InterlinMasterNoTitleBar(XElement configurationParametersElement, LcmCache cache, RecordClerk recordClerk)
			:base(configurationParametersElement, cache, recordClerk, false)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			Dock = DockStyle.Fill;
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

			var removedControls = false;
			SuspendLayout();
			if (m_informationBar != null)
			{
				Controls.Remove(m_informationBar);
				m_informationBar.Dispose();
				m_informationBar = null;
				removedControls = true;
			}

			if (TitleContentsPane != null)
			{
				Controls.Remove(TitleContentsPane);
				TitleContentsPane.Dispose();
				TitleContentsPane = null;
				removedControls = true;
			}
			ResumeLayout(true);
			if (removedControls)
			{
				BringToFront();
			}
		}
	}
}
