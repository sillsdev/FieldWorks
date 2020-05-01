// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Dummy subclass so we can design the Chart tab.
	/// </summary>
	public class InterlinDocChart : UserControl
	{
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// InterlinDocChart
			//
			this.AccessibleName = "Interlinear Document Chart";
			this.Name = "InterlinDocChart";
			this.ResumeLayout(false);
		}

		public InterlinDocChart()
		{
			InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			base.Dispose(disposing);
		}
	}
}