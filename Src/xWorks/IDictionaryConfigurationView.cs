// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;

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
		/// Tell the controller that the user selected a different model(dictionary view)
		/// </summary>
		event SwitchViewEvent SwitchView;

		/// <summary>
		/// Gets the tree hierarchy control.
		/// </summary>
		DictionaryConfigurationTreeControl TreeControl { get; }

		/// <summary>
		/// Sets the DetailsView
		/// </summary>
		DetailsView DetailsView { set; }

		/// <summary>
		/// Sets the XHTML to display in the preview control
		/// </summary>
		string PreviewData { set; }

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

	public delegate void SwitchViewEvent(object sender, SwitchViewEventArgs args);

	/// <summary>
	/// The arguments for a SwitchViewEvent. Includes the view selected as a property.
	/// </summary>
	public class SwitchViewEventArgs
	{
		public string ViewPicked { get; set; }
	}
}
