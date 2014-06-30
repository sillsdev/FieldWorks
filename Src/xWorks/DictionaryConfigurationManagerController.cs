// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using Palaso.Linq;
using Utils.MessageBoxExLib;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Handles manipulation of the set of dictionary configurations ("views"), and their associations to dictionary publications.
	/// Controller for the DictionaryConfigurationManagerDlg View.
	/// </summary>
	class DictionaryConfigurationManagerController
	{
		private readonly DictionaryConfigurationManagerDlg _view;
		private readonly string _projectConfigDir;
		internal string _defaultConfigDir;

		/// <summary>
		/// Set of dictionary configurations (aka "views") in project.
		/// </summary>
		internal List<DictionaryConfigurationModel> Configurations;

		/// <summary>
		/// Set of the names of available dictionary publications in project.
		/// </summary>
		internal List<string> Publications;

		private readonly ListViewItem _allPublicationsItem;
		internal FdoCache Cache;
		private Mediator _mediator;

		/// <summary>
		/// The currently-selected item in the Configurations ListView, or null if none.
		/// </summary>
		private DictionaryConfigurationModel SelectedConfiguration
		{
			get
			{
				var selectedConfigurations = _view.configurationsListView.SelectedItems;
				// MultiSelect is not enabled, so just use the first selected item.
				return selectedConfigurations.Count < 1 ? null : (DictionaryConfigurationModel)selectedConfigurations[0].Tag;
			}
		}

		/// <summary>Get list of publications using a dictionary configuration.</summary>
		internal List<string> GetPublications(DictionaryConfigurationModel dictionaryConfiguration)
		{
			if (dictionaryConfiguration == null)
				throw new ArgumentNullException();
			return dictionaryConfiguration.Publications;
		}

		/// <summary>Associate a publication with a dictionary configuration.</summary>
		internal void AssociatePublication(string publication, DictionaryConfigurationModel configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException();
			if (publication == null)
				throw new ArgumentNullException();
			if (!Publications.Contains(publication))
				throw new ArgumentOutOfRangeException();

			if (!configuration.Publications.Contains(publication))
				configuration.Publications.Add(publication);
		}

		/// <summary>Disassociate a publication from a dictionary configuration.</summary>
		internal void DisassociatePublication(string publication, DictionaryConfigurationModel configuration)
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
		internal DictionaryConfigurationManagerController(string projectConfigDir)
		{
			_projectConfigDir = projectConfigDir;
		}

		public DictionaryConfigurationManagerController(DictionaryConfigurationManagerDlg view, Mediator mediator,
			List<DictionaryConfigurationModel> configurations, List<string> publications, string projectConfigDir,
			string defaultConfigDir)
		{
			_view = view;
			Configurations = configurations;
			Publications = publications;
			_projectConfigDir = projectConfigDir;
			_defaultConfigDir = defaultConfigDir;
			_mediator = mediator;
			Cache = (FdoCache) _mediator.PropertyTable.GetValue("cache");

			// Add special publication selection for All Publications.
			_allPublicationsItem = new ListViewItem
			{
				Text = xWorksStrings.Allpublications,
				Font = new Font(Control.DefaultFont, FontStyle.Italic),
			};
			_view.publicationsListView.Items.Add(_allPublicationsItem);

			// Populate lists of configurations and publications
			ReLoadConfigurations();
			foreach (var publication in Publications)
			{
				var item = new ListViewItem { Text = publication };
				_view.publicationsListView.Items.Add(item);
			}

			_view.Shown += OnShowDialog;
		}

		private void ReLoadConfigurations()
		{
			Configurations.Sort((lhs, rhs) => string.Compare(lhs.Label, rhs.Label));
			_view.configurationsListView.Items.Clear();
			_view.configurationsListView.Items.AddRange(
				Configurations.Select(configuration => new ListViewItem { Tag = configuration, Text = configuration.Label }).ToArray());
		}

		/// <summary>
		/// When the view is shown, register EventHandlers and select first configuration, which will cause publications to be checked or unchecked
		/// </summary>
		private void OnShowDialog(object sender, EventArgs eventArgs)
		{
			_view.configurationsListView.SelectedIndexChanged += OnSelectConfiguration;
			_view.configurationsListView.AfterLabelEdit += OnRenameConfiguration;
			_view.publicationsListView.ItemChecked += OnCheckPublication;
			_view.copyButton.Click += OnCopyConfiguration;
			_view.removeButton.Click += OnDeleteConfiguration;
			_view.configurationsListView.Items[0].Selected = true;
		}

		/// <summary>
		// Update which publications are checked in response to configuration selection.
		/// </summary>
		private void OnSelectConfiguration(object sender, EventArgs args)
		{
			if (SelectedConfiguration == null)
			{
				foreach (ListViewItem pubItem in _view.publicationsListView.Items)
				{
					pubItem.Checked = false;
				}
				_view.publicationsListView.Enabled = false;
				return;
			}

			_view.publicationsListView.Enabled = true;
			var associatedPublications = GetPublications(SelectedConfiguration);
			foreach (ListViewItem publicationItem in _view.publicationsListView.Items)
			{
				publicationItem.Checked = associatedPublications.Contains(publicationItem.Text);
			}
			_allPublicationsItem.Checked = SelectedConfiguration.AllPublications;
		}

		private void OnCheckPublication(object sender, ItemCheckedEventArgs itemCheckedEventArgs)
		{
			if (SelectedConfiguration == null)
				return;

			var publicationItem = itemCheckedEventArgs.Item;

			if (publicationItem == _allPublicationsItem)
			{
				SelectedConfiguration.AllPublications = publicationItem.Checked;
				// If "All publications" was checked, check all the publications.
				if (_allPublicationsItem.Checked)
					_view.publicationsListView.Items.Cast<ListViewItem>().ForEach(item => item.Checked = true);
			}
			else // "normal" item, not AllPublications
			{
				if (publicationItem.Checked)
					AssociatePublication(publicationItem.Text, SelectedConfiguration);
				else
				{
					DisassociatePublication(publicationItem.Text, SelectedConfiguration);
					// If a publication was unchecked, uncheck "All publications".
					_allPublicationsItem.Checked = false;
				}
			}
		}

		/// <remarks>
		/// Renaming a configuration won't be saved to disk until the user saves the parent dialog.
		/// </remarks>
		private void OnRenameConfiguration(object sender, LabelEditEventArgs labelEditEventArgs)
		{
			// WinForms renames the ListViewItem by index *after* this EventHandler runs. We want absolute control over sorting and
			// renaming, so "cancel" the event before doing anything.
			labelEditEventArgs.CancelEdit = true;
			var selectedItem = _view.configurationsListView.Items[labelEditEventArgs.Item];
			if (RenameConfiguration(selectedItem, labelEditEventArgs))
			{
				ReLoadConfigurations();
			}
			else
			{
				// If the user chose a duplicate name, warn the user and leave the Item's text open for edit
				MessageBox.Show(xWorksStrings.FailedToRename);
				selectedItem.Text = labelEditEventArgs.Label;
				selectedItem.BeginEdit();
			}
		}

		/// <summary>Verifies that the configuration is given a unique name; if not, displays a message and lets the user keep editing</summary>
		/// <returns>true if the user chose a unique name or canceled; false if the user chose a duplicate name</returns>
		internal bool RenameConfiguration(ListViewItem selectedItem, LabelEditEventArgs labelEditEventArgs)
		{
			var selectedConfig = (DictionaryConfigurationModel)selectedItem.Tag;
			if (string.IsNullOrWhiteSpace(labelEditEventArgs.Label))
			{
				// labelEditEventArgs.Label may be null or whitespace in the following cases:
				// - The user has pressed Escape (probably wants to cancel the edit)
				// - The user has entered no meaningful text (might as well cancel)
				// - The user has pressed Enter or clicked away without making any changes (can usually be safely interpreted as a cancel)
				// - Any of the above immediately after the warning to choose a unique name. It would require a fair amount of effort to distinguish
				//   between these cases, so simply revert any edits
				selectedItem.Text = selectedConfig.Label;
			}
			else if (Configurations.Any(config => config != selectedConfig && config.Label == labelEditEventArgs.Label))
			{
				return false;
			}
			else
			{
				selectedConfig.Label = labelEditEventArgs.Label;
			}

			// At this point, the user has chosen a unique name.  See if we should generate the filename.
			if (!File.Exists(selectedConfig.FilePath))
				GenerateFilePath(selectedConfig);

			return true;
		}

		/// <summary>Generates a unique file path for the configuration, based on its label</summary>
		internal void GenerateFilePath(DictionaryConfigurationModel config)
		{
			var filePath = FormatFilePath(config.Label);
			int i = 1;
			while (Configurations.Any(conf => Path.GetFileName(filePath).Equals(Path.GetFileName(conf.FilePath))))
			{
				filePath = FormatFilePath(string.Format("{0}_{1}", config.Label, i++));
			}
			config.FilePath = filePath;
		}

		/// <summary>Removes illegal characters, appends project config path and extension</summary>
		internal string FormatFilePath(string label)
		{
			return Path.Combine(_projectConfigDir,
				MiscUtils.FilterForFileName(label, MiscUtils.FilenameFilterStrength.kFilterBackup) + DictionaryConfigurationModel.FileExtension);
		}

		private void OnCopyConfiguration(object sender, EventArgs e)
		{
			if (SelectedConfiguration == null)
				return;

			var newConfig = CopyConfiguration(SelectedConfiguration);

			ReLoadConfigurations();

			// present the new configuration for rename
			var newConfigListViewItem = _view.configurationsListView.Items.Cast<ListViewItem>().First(item => item.Tag == newConfig);
			newConfigListViewItem.EnsureVisible();
			newConfigListViewItem.Selected = true;
			newConfigListViewItem.BeginEdit();
		}

		/// <summary>Copies a Configuration and adds it to the list</summary>
		internal DictionaryConfigurationModel CopyConfiguration(DictionaryConfigurationModel config)
		{
			// deep clone the selected configuration
			var newConfig = config.DeepClone();

			// generate a unique name (starting i=2 mimicks old behaviour)
			var newName = "Copy of " + newConfig.Label;
			int i = 2;
			while (Configurations.Any(conf => conf.Label == newName))
			{
				newName = String.Format("Copy of {0} ({1})", newConfig.Label, i++);
			}
			newConfig.Label = newName;
			newConfig.FilePath = null; // this will be set on the next rename, which will occur immediately

			// update the configurations list
			Configurations.Add(newConfig);

			return newConfig;
		}

		/// <summary>
		/// Remove configuration from list of configurations, and delete the corresponding XML file from disk.
		/// Unless the configuration is derived from a shipped default, in which case we'll just reset its data to the factory defaults.
		/// </summary>
		internal void DeleteConfiguration(DictionaryConfigurationModel configurationToDelete)
		{
			if (configurationToDelete == null)
				throw new ArgumentNullException("configurationToDelete");

			if (IsConfigurationACustomizedShippedDefault(configurationToDelete))
			{
				var origFilePath = configurationToDelete.FilePath;
				var filenameOfFilePath = Path.GetFileName(origFilePath);

				var pathToShippedFile = Path.Combine(_defaultConfigDir, filenameOfFilePath);

				configurationToDelete.FilePath = pathToShippedFile;
				// Recreate from shipped XML file.
				configurationToDelete.Load(Cache);
				configurationToDelete.FilePath = origFilePath;

				return;
			}

			Configurations.Remove(configurationToDelete);
			if (configurationToDelete.FilePath != null)
				FileUtils.Delete(configurationToDelete.FilePath);
		}

		private void OnDeleteConfiguration(object sender, EventArgs eventArgs)
		{
			if (SelectedConfiguration == null)
				return;

			using (var dlg = new ConfirmDeleteObjectDlg(_mediator.HelpTopicProvider))
			{
				dlg.WindowTitle = xWorksStrings.Confirm;
				var kindOfConfiguration = DictionaryConfigurationListener.GetDictionaryConfigurationType(_mediator);
				dlg.TopBodyText = string.Format("{0} {1}: {2}", kindOfConfiguration,xWorksStrings.Configuration, SelectedConfiguration.Label);

				if (IsConfigurationACustomizedShippedDefault(SelectedConfiguration))
				{
					dlg.TopMessage = xWorksStrings.YouAreResetting;
					dlg.BottomQuestion = xWorksStrings.WantContinue;
					dlg.DeleteButtonText = xWorksStrings.Reset;
				}

				if (dlg.ShowDialog() != DialogResult.Yes)
					return;
			}

			DeleteConfiguration(SelectedConfiguration);
			ReLoadConfigurations();
		}

		/// <summary>
		/// Whether a configuration is, or is a customization of, a shipped default configuration,
		/// such as the shipped Root-based, Stem-based, or Bartholomew configurations.
		/// </summary>
		public bool IsConfigurationACustomizedShippedDefault(DictionaryConfigurationModel configuration)
		{
			if (configuration.FilePath == null)
				return false;

			var defaultConfigurationFiles = FileUtils.GetFilesInDirectory(_defaultConfigDir).Select(Path.GetFileName);

			var filename = Path.GetFileName(configuration.FilePath);
			return defaultConfigurationFiles.Contains(filename);
		}

	}
}
