// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.PaneBar;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class is like its superclass, except it removes the superclass' top control and pane bar.
	/// </summary>
	internal class InterlinMasterNoTitleBar : InterlinMaster
	{
		private System.ComponentModel.IContainer components;

		internal InterlinMasterNoTitleBar()
		{
			InitializeComponent();
		}

		internal InterlinMasterNoTitleBar(XElement configurationParametersElement, MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList, Dictionary<string, PanelButton> paneBarButtons)
			: base(configurationParametersElement, majorFlexComponentParameters, recordList, paneBarButtons, false)
		{
			InitializeComponent();
			Dock = DockStyle.Fill;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
				components?.Dispose();
			}
			base.Dispose(disposing);
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
