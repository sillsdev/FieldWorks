// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Used for Interlinear context menu items to Add a new WritingSystem
	/// for a flid that is already visible.
	/// </summary>
	public class AddWritingSystemMenuItem : ToolStripMenuItem
	{
		/// <summary />
		public AddWritingSystemMenuItem(int flid, int ws)
		{
			Flid = flid;
			Ws = ws;
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			base.Dispose(disposing);
		}

		public int Flid { get; }

		public int Ws { get; }
	}
}