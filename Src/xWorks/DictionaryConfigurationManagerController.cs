// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Handles manipulation of the set of dictionary configurations ("views"), and their associations to dictionary publications.
	/// Controller for the DictionaryConfigurationManagerDlg View.
	/// </summary>
	class DictionaryConfigurationManagerController
	{
		private DictionaryConfigurationManagerDlg _view;

		/// <summary>
		/// Set of dictionary configurations (aka "views") in project.
		/// </summary>
		internal List<DictionaryConfigurationModel> Configurations;

		/// <summary>
		/// Set of the names of available dictionary publications in project.
		/// </summary>
		internal List<string> Publications;

		/// <summary>
		/// Get list of publications using a dictionary configuration.
		/// </summary>
		public List<string> GetPublications(DictionaryConfigurationModel dictionaryConfiguration)
		{
			if (dictionaryConfiguration==null)
				throw new ArgumentNullException();
			return dictionaryConfiguration.Publications;
		}

		/// <summary>
		/// Associate a publication with a dictionary configuration.
		/// </summary>
		public void AssociatePublication(string publication, DictionaryConfigurationModel configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException();
			if (publication == null)
				throw new ArgumentNullException();
			if (!Publications.Contains(publication))
				throw new ArgumentOutOfRangeException();

			configuration.Publications.Add(publication);
		}

		/// <summary>
		/// Disassociate a publication from a dictionary configuration.
		/// </summary>
		public void DisassociatePublication(string publication, DictionaryConfigurationModel configuration)
		{
			if (publication == null)
				throw new ArgumentNullException();
			if (configuration == null)
				throw new ArgumentNullException();
			if (!Publications.Contains(publication))
				throw new ArgumentOutOfRangeException();

			configuration.Publications.Remove(publication);
		}

		/// <summary>
		/// For unit tests.
		/// </summary>
		internal DictionaryConfigurationManagerController()
		{
		}

		public DictionaryConfigurationManagerController(DictionaryConfigurationManagerDlg view, List<DictionaryConfigurationModel> configurations, List<string> publications)
		{
			_view = view;
			Configurations = configurations;
			Publications = publications;

			_view.configurationsListBox.Items.AddRange(Configurations.Select(configuration=>configuration.Label).ToArray());
			_view.publicationsCheckedListBox.Items.AddRange(Publications.ToArray());

			// When a different dictionary configuration is selected, update which publications are checked.
			_view.configurationsListBox.SelectedIndexChanged += (sender, args) =>
			{
				var newConfigurationName = _view.configurationsListBox.SelectedItem.ToString();
				var newConfiguration = Configurations.Find(configuration => configuration.Label == newConfigurationName);
				var associatedPublications = GetPublications(newConfiguration);
				for (int index = 0; index < _view.publicationsCheckedListBox.Items.Count; index++)
				{
					var publication = _view.publicationsCheckedListBox.Items[index];
					_view.publicationsCheckedListBox.SetItemChecked(index, associatedPublications.Contains(publication));
				}
			};

			// Select first configuration, and cause publications to be checked or unchecked.
			_view.configurationsListBox.SelectedIndex = 0;
		}
	}
}
