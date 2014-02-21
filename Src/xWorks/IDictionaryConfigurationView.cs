// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	interface IDictionaryConfigurationView
	{
		/// <summary>
		/// Tell the controller to launch the dialog where different dictionary configurations (or views) are managed.
		/// </summary>
		event EventHandler ManageViews;

		/// <summary>
		/// Tell the controller to save the model.
		/// </summary>
		event EventHandler SaveModel;

		/// <summary>
		/// Gets the tree hierarchy widget.
		/// </summary>
		TreeView GetTreeView();

		/// <summary>
		/// Redraw the widgets, updating anything that has changed.
		/// </summary>
		void Redraw();

		/// <summary>
		/// Sets the choices of configuration options in the view
		/// </summary>
		/// <param name="choices"></param>
		void SetChoices(IEnumerable<string> choices);
	}
}
