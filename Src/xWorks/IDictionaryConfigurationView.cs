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
		event EventHandler ManageConfigurations;

		/// <summary>
		/// Tell the controller to save the model.
		/// </summary>
		event EventHandler SaveModel;

		/// <summary>
		/// Tell the controller that the user selected a different model (dictionary configuration)
		/// </summary>
		event SwitchConfigurationEvent SwitchConfiguration;

		/// <summary>
		/// Gets the tree hierarchy control.
		/// </summary>
		DictionaryConfigurationTreeControl TreeControl { get; }

		/// <summary>
		/// Sets the DetailsView
		/// </summary>
		IDictionaryDetailsView DetailsView { set; }

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
		void SetChoices(IEnumerable<DictionaryConfigurationModel> choices);

		/// <summary>
		/// Show the publications for the current dictionary configuration.
		/// </summary>
		/// <param name="publications"></param>
		void ShowPublicationsForConfiguration(String publications);

		/// <summary>
		/// Select current dictionary configuration in the combo box
		/// </summary>
		/// <param name="configuration"></param>
		void SelectConfiguration(DictionaryConfigurationModel configuration);
	}

	public delegate void SwitchConfigurationEvent(object sender, SwitchConfigurationEventArgs args);

	/// <summary>
	/// The arguments for a SwitchConfigurationEvent. Includes the configuration selected as a property.
	/// </summary>
	public class SwitchConfigurationEventArgs
	{
		public DictionaryConfigurationModel ConfigurationPicked { get; set; }
	}
}
