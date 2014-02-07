// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	interface IDictionaryConfigurationView
	{
		/// <summary>
		/// Gets the tree hierarchy widget.
		/// </summary>
		TreeView GetTreeView();

		/// <summary>
		/// Redraw the widgets, updating anything that has changed.
		/// </summary>
		void Redraw();
	}
}
