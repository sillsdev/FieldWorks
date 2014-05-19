// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Palaso.Linq;

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

			// Populate lists of configurations and publications
			foreach (var configuration in Configurations)
			{
				var item = new ListViewItem {Tag = configuration, Text = configuration.Label};
				_view.configurationsListView.Items.Add(item);
			}
			foreach (var publication in Publications)
			{
				var item = new ListViewItem {Text = publication};
				_view.publicationsListView.Items.Add(item);
			}

			// When a different dictionary configuration is selected, update which publications are checked.
			_view.configurationsListView.SelectedIndexChanged += OnSelectConfiguration;

			_view.configurationsListView.AfterLabelEdit += OnRenameConfiguration;

			// Select first configuration, and cause publications to be checked or unchecked.
			_view.configurationsListView.Items[0].Selected = true;
		}

		/// <summary>
		// Update which publications are checked in response to configuration selection.
		/// </summary>
		private void OnSelectConfiguration(object sender, EventArgs args)
		{
			if (_view.configurationsListView.SelectedIndices.Count < 1)
				return;
			// MultiSelect is not enabled, so can just use the first selected item.
			var newConfiguration = _view.configurationsListView.SelectedItems[0].Tag as DictionaryConfigurationModel;
			var associatedPublications = GetPublications(newConfiguration);
			foreach (ListViewItem publicationItem in _view.publicationsListView.Items)
			{
				publicationItem.Checked = associatedPublications.Contains(publicationItem.Text);
			}
		}

		/// <remarks>
		/// Renaming a configuration won't be saved to disk until the user saves the parent dialog.
		/// </remarks>
		private void OnRenameConfiguration(object sender, LabelEditEventArgs labelEditEventArgs)
		{
			var newName = labelEditEventArgs.Label;
			if (string.IsNullOrWhiteSpace(newName))
			{
				// newName may be 'null' if there was no change. In either case, don't allow renaming to null, empty, or whitespace.
				labelEditEventArgs.CancelEdit = true;
				return;
			}
			var itemIndex = labelEditEventArgs.Item;
			var item = _view.configurationsListView.Items[itemIndex];
			var configuration = (DictionaryConfigurationModel)item.Tag;
			configuration.Label = newName;
		}
	}
}
