// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// We subclass the menu item so we can store a NamedWritingSystem for each menu item in
	/// the Add writing system popup list.
	/// </summary>
	internal class WsMenuItem : ToolStripMenuItem
	{
		/// <summary />
		public WsMenuItem(CoreWritingSystemDefinition ws, ListBox list, EventHandler handler)
			: base(ws.DisplayLabel, null, handler)
		{
			WritingSystem = ws;
			ListBox = list;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (disposing)
			{
				ListBox?.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary />
		public CoreWritingSystemDefinition WritingSystem { get; }

		/// <summary />
		public ListBox ListBox { get; }
	}
}