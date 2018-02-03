// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Dummy subclass so we can design the Chart tab.
	/// </summary>
	public class InterlinDocChart : UserControl
	{
		private void InitializeComponent()
		{
			SuspendLayout();
			AccessibleName = "InterlinDocChart";
			Name = "InterlinDocChart";
			ResumeLayout(false);
		}

		public InterlinDocChart()
		{
			InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");

			if (disposing)
			{
			}

			base.Dispose(disposing);
		}
	}
}