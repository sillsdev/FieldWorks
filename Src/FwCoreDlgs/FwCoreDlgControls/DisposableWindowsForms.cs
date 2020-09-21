// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <remarks>Works around a MONO bug by explicitly disposing its Items. Even Windows neglects to dispose these on occasion.</remarks>
	public class DisposableToolStripMenuItem : ToolStripMenuItem
	{
		/// <c/>
		public DisposableToolStripMenuItem() { }

		/// <c/>
		public DisposableToolStripMenuItem(string text) : base(text) { }

		/// <c/>
		public DisposableToolStripMenuItem(Image image) : base(image) { }

		/// <c/>
		public DisposableToolStripMenuItem(string text, Image image) : base(text, image) { }

		/// <c/>
		public DisposableToolStripMenuItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }

		/// <summary>Works around a MONO bug by explicitly disposing its Items</summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (disposing)
			{
				// BUG: MONO neglects to dispose DropDownItems. Do it now:
				foreach (var disposable in DropDownItems.OfType<IDisposable>().ToArray())
					disposable.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
