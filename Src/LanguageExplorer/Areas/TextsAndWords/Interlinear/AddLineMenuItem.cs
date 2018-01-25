// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Used for Interlinear context menu items to Add a new InterlinLineSpec
	/// for a flid that is currently hidden.
	/// </summary>
	public class AddLineMenuItem : ToolStripMenuItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddLineMenuItem"/> class
		/// used for context (right-click) menus.
		/// </summary>
		/// <param name="flid">
		/// 	The flid of the InterlinLineSpec we might add.
		/// </param>
		public AddLineMenuItem(int flid)
		{
			Flid = flid;
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			base.Dispose(disposing);
		}

		public int Flid { get; }
	}
}