// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.LcmUi.Dialogs;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.Linq;
using SIL.WritingSystems;
using Ionic.Zip;
using SIL.Code;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Handles manipulation of the set of dictionary configurations ("views"), and their associations to dictionary publications.
	/// Controller for the DictionaryConfigurationManagerDlg View.
	/// </summary>
	internal class DictionaryConfigurationManagerController : IFlexComponent
	{
		private readonly DictionaryConfigurationManagerDlg _view;
		private LcmCache _cache;
		internal readonly string _projectConfigDir;
		private readonly string _defaultConfigDir;

		/// <summary>
		/// Set of dictionary configurations (aka "views") in project.
		/// </summary>
		internal readonly List<DictionaryConfigurationModel> _configurations;

		/// <summary>
		/// Set of the names of available dictionary publications in project.
		/// </summary>
		private List<string> _publications;

		private readonly ListViewItem _allPublicationsItem;
		private readonly DictionaryConfigurationModel _initialConfig;

		public bool IsDirty { get; private set; }

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

		/// <summary>
		/// Event fired when the DictionaryConfigurationManager dialog is closed
		/// </summary>
		public event Action<DictionaryConfigurationModel> Finished;

		public event Action ConfigurationViewImported;

		/// <summary>Get list of publications using a dictionary configuration.</summary>
		internal List<string> GetPublications(DictionaryConfigurationModel dictionaryConfiguration)
		{
			Guard.AgainstNull(dictionaryConfiguration, nameof(dictionaryConfiguration));

			return dictionaryConfiguration.Publications;
		}

		/// <summary>Associate a publication with a dictionary configuration.</summary>
		internal void AssociatePublication(string publication, DictionaryConfigurationModel configuration)
		{
			Guard.AgainstNull(publication, nameof(publication));
			Guard.AgainstNull(configuration, nameof(configuration));
			if (!_publications.Contains(publication))
			{
				throw new ArgumentOutOfRangeException();
			}

			if (!configuration.Publications.Contains(publication))
			{
				configuration.Publications.Add(publication);
			}
		}

		/// <summary>Disassociate a publication from a dictionary configuration.</summary>
		internal void DisassociatePublication(string publication, DictionaryConfigurationModel configuration)
		{
			Guard.AgainstNull(publication, nameof(publication));
			Guard.AgainstNull(configuration, nameof(configuration));
			if (!_publications.Contains(publication))
			{
				throw new ArgumentOutOfRangeException();
			}

			configuration.Publications.Remove(publication);
		}

		/// <summary>
		/// For unit tests.
		/// </summary>
		internal DictionaryConfigurationManagerController(List<DictionaryConfigurationModel> configurations, List<string> publications, string projectConfigDir, string defaultConfigDir)
		{
			_configurations = configurations;
			_publications = publications;
			_projectConfigDir = projectConfigDir;
			_defaultConfigDir = defaultConfigDir;
		}

		public DictionaryConfigurationManagerController(DictionaryConfigurationManagerDlg view,
			List<DictionaryConfigurationModel> configurations, List<string> publications, string projectConfigDir, string defaultConfigDir, DictionaryConfigurationModel currentConfig) :
			this(configurations, publications, projectConfigDir, defaultConfigDir)
		{
			_view = view;
			_initialConfig = currentConfig;

			// Add special publication selection for All Publications.
			_allPublicationsItem = new ListViewItem
			{
				Text = DictionaryConfigurationStrings.Allpublications,
				Font = new Font(Control.DefaultFont, FontStyle.Italic),
			};
			_view.publicationsListView.Items.Add(_allPublicationsItem);
		}

		private void ReLoadConfigurations()
		{
			_configurations.Sort((lhs, rhs) => string.Compare(lhs.Label, rhs.Label));
			_view.configurationsListView.Items.Clear();
			_view.configurationsListView.Items.AddRange(_configurations.Select(configuration => new ListViewItem { Tag = configuration, Text = configuration.Label }).ToArray());
		}

		/// <summary>
		/// Fetch up-to-date list of publications from project and populate the list of publications
		/// </summary>
		private void ReLoadPublications()
		{
			_publications = DictionaryConfigurationController.GetAllPublications(_cache);
			foreach (var publication in _publications)
			{
				var item = new ListViewItem { Text = publication };
				_view.publicationsListView.Items.Add(item);
			}
		}

		/// <summary>
		/// When the view is shown, register EventHandlers and select first configuration, which will cause publications to be checked or unchecked
		/// </summary>
		private void OnShowDialog(object sender, EventArgs eventArgs)
		{
			_view.configurationsListView.SelectedIndexChanged += OnSelectConfiguration;
			_view.configurationsListView.BeforeLabelEdit += OnBeforeLabelEdit;
			_view.configurationsListView.AfterLabelEdit += OnRenameConfiguration;
			_view.publicationsListView.ItemChecked += OnCheckPublication;
			_view.copyButton.Click += OnCopyConfiguration;
			_view.removeButton.Click += OnDeleteConfiguration;
			_view.resetButton.Click += OnDeleteConfiguration; // REVIEW (Hasso) 2017.01: should call OnResetConfiguration
			_view.exportButton.Click += OnExportConfiguration;
			_view.importButton.Click += OnImportConfiguration;

			_view.Closing += (sndr, e) =>
			{
				if (SelectedConfiguration != null)
				{
					Finished?.Invoke(SelectedConfiguration);
				}
			};

			// Select the correct configuration
			var selectedConfigIdx = _configurations.FindIndex(config => config == _initialConfig);
			if (selectedConfigIdx >= 0)
			{
				_view.configurationsListView.Items[selectedConfigIdx].Selected = true;
			}
			else
			{
				_view.configurationsListView.Items[0].Selected = true;
			}

			IsDirty = false;
		}

		private void OnBeforeLabelEdit(object sender, LabelEditEventArgs args)
		{
			_view.copyButton.Enabled = false;
			_view.removeButton.Enabled = false;
			_view.resetButton.Enabled = false;
			_view.closeButton.Enabled = false;
			_view.exportButton.Enabled = false;
			_view.importButton.Enabled = false;
		}

		/// <summary>
		/// Update which publications are checked in response to configuration selection.
		/// Disable the copy and delete buttons if no configuration is selected.
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
				_view.copyButton.Enabled = false;
				_view.resetButton.Enabled = false;
				_view.removeButton.Enabled = false;
				_view.exportButton.Enabled = false;
				return;
			}

			if (IsConfigurationACustomizedOriginal(SelectedConfiguration))
			{
				_view.resetButton.Enabled = true;
				_view.removeButton.Enabled = false;
			}
			else
			{
				_view.removeButton.Enabled = true;
				_view.resetButton.Enabled = false;
			}

			_view.publicationsListView.Enabled = true;
			_view.copyButton.Enabled = true;
			_view.closeButton.Enabled = true;
			_view.exportButton.Enabled = true;
			_view.importButton.Enabled = true;
			var associatedPublications = GetPublications(SelectedConfiguration);
			foreach (ListViewItem publicationItem in _view.publicationsListView.Items)
			{
				// Don't try processing the all-pubs item and get into a muddle.
				if (publicationItem == _allPublicationsItem)
				{
					continue;
				}
				publicationItem.Checked = associatedPublications.Contains(publicationItem.Text);
			}
			_allPublicationsItem.Checked = SelectedConfiguration.AllPublications;
		}

		private void OnCheckPublication(object sender, ItemCheckedEventArgs itemCheckedEventArgs)
		{
			if (SelectedConfiguration == null)
			{
				return;
			}

			var publicationItem = itemCheckedEventArgs.Item;

			if (publicationItem == _allPublicationsItem)
			{
				SelectedConfiguration.AllPublications = publicationItem.Checked;
				// If "All publications" was checked, check all the publications.
				if (_allPublicationsItem.Checked)
				{
					_view.publicationsListView.Items.Cast<ListViewItem>().ForEach(item => item.Checked = true);
				}
			}
			else // "normal" item, not AllPublications
			{
				if (publicationItem.Checked)
				{
					AssociatePublication(publicationItem.Text, SelectedConfiguration);
				}
				else
				{
					DisassociatePublication(publicationItem.Text, SelectedConfiguration);
					// If a publication was unchecked, uncheck "All publications".
					_allPublicationsItem.Checked = false;
				}
			}
			IsDirty = true;
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
				// Re-select item that was just renamed, or not renamed, from the re-loaded list of configurations.
				var newName = labelEditEventArgs.Label ?? selectedItem.Text;
				_view.configurationsListView.Items.Cast<ListViewItem>().First(item => item.Text == newName).Selected = true;
			}
			else
			{
				// If the user chose a duplicate name, warn the user and leave the Item's text open for edit
				MessageBox.Show(DictionaryConfigurationStrings.FailedToRename);
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
			else if (_configurations.Any(config => config != selectedConfig && config.Label == labelEditEventArgs.Label))
			{
				return false;
			}
			else
			{
				selectedConfig.Label = labelEditEventArgs.Label;
				IsDirty = true;
			}

			// At this point, the user has chosen a unique name.  See if we should generate the filename.
			if (!File.Exists(selectedConfig.FilePath))
			{
				GenerateFilePath(_projectConfigDir, _configurations, selectedConfig);
			}

			return true;
		}

		/// <summary>Generates a unique file path for the configuration, based on its label.
		/// Take into account what files are claimed to be used by configurations as well as what files actually exist on disk.</summary>
		internal static void GenerateFilePath(string projectConfigDir, List<DictionaryConfigurationModel> existingConfigurations, DictionaryConfigurationModel config)
		{
			var filePath = FormatFilePath(projectConfigDir, config.Label);
			var i = 1;
			while (existingConfigurations.Any(conf => Path.GetFileName(filePath).Equals(Path.GetFileName(conf.FilePath))) || FileUtils.FileExists(Path.Combine(projectConfigDir,filePath)))
			{
				filePath = FormatFilePath(projectConfigDir, $"{config.Label}_{i++}");
			}
			config.FilePath = filePath;
		}

		/// <summary>Removes illegal characters, appends project config path and extension</summary>
		internal static string FormatFilePath(string projectConfigDir, string label)
		{
			return Path.Combine(projectConfigDir, MiscUtils.FilterForFileName(label, MiscUtils.FilenameFilterStrength.kFilterBackup) + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
		}

		private void OnCopyConfiguration(object sender, EventArgs e)
		{
			if (SelectedConfiguration == null)
			{
				return;
			}

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
			var i = 2;
			while (_configurations.Any(conf => conf.Label == newName))
			{
				newName = $"Copy of {newConfig.Label} ({i++})";
			}
			newConfig.Label = newName;
			newConfig.FilePath = null; // this will be set on the next rename, which will occur immediately

			// update the configurations list
			_configurations.Add(newConfig);

			IsDirty = true;
			return newConfig;
		}

		/// <summary>
		/// Remove configuration from list of configurations, and delete the corresponding XML file from disk.
		/// Unless the configuration is derived from a shipped default, in which case we'll just reset its data to the factory defaults.
		/// </summary>
		internal void DeleteConfiguration(DictionaryConfigurationModel configurationToDelete)
		{
			if (configurationToDelete == null)
			{
				throw new ArgumentNullException(nameof(configurationToDelete));
			}

			if (IsConfigurationACustomizedOriginal(configurationToDelete))
			{
				ResetConfigurationContents(configurationToDelete);

				IsDirty = true;
				return;
			}

			_configurations.Remove(configurationToDelete);
			if (configurationToDelete.FilePath != null)
			{
				FileUtils.Delete(configurationToDelete.FilePath);
			}
			IsDirty = true;
		}

		private void ResetConfigurationContents(DictionaryConfigurationModel configurationToDelete)
		{
			var origFilePath = configurationToDelete.FilePath;
			var filenameOfFilePath = Path.GetFileName(origFilePath);
			var origReversalLabel = configurationToDelete.Label;
			var origReversalWs = configurationToDelete.WritingSystem;

			const string allReversalsFileName = "AllReversalIndexes" + LanguageExplorerConstants.DictionaryConfigurationFileExtension;
			var resettingReversal = IsConfigurationAnOriginalReversal(configurationToDelete, _cache);
			// The reversals will be reset to what the user has configured under All Reversal Indexes. This makes it useful to actually change that.
			// If the user resets "AllReversalIndexes" it will reset to the shipping version.
			string pathToDefaultFile;
			if (resettingReversal)
			{
				pathToDefaultFile = Path.Combine(_projectConfigDir, allReversalsFileName);
				// If there are no changes to the AllReversalIndexes in this project then it won't exist, fallback to shipping defaults
				if (!File.Exists(pathToDefaultFile))
				{
					pathToDefaultFile = Path.Combine(_defaultConfigDir, allReversalsFileName);
				}
			}
			else
			{
				pathToDefaultFile = Path.Combine(_defaultConfigDir, filenameOfFilePath);
			}

			configurationToDelete.FilePath = pathToDefaultFile;
			// Recreate from shipped XML file.
			configurationToDelete.Load(_cache);
			configurationToDelete.FilePath = origFilePath;
			if (resettingReversal)
			{
				configurationToDelete.Label = origReversalLabel;
				configurationToDelete.WritingSystem = origReversalWs;
			}
		}

		private static bool IsAllReversalIndexConfig(DictionaryConfigurationModel configurationToDelete)
		{
			return Path.GetFileNameWithoutExtension(configurationToDelete.FilePath) == "AllReversalIndexes";
		}

		private void OnDeleteConfiguration(object sender, EventArgs eventArgs) // REVIEW (Hasso) 2017.01: this should be two methods, since there are two buttons.
		{
			var configurationToDelete = SelectedConfiguration;
			if (configurationToDelete == null)
			{
				return;
			}

			using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IFlexApp>("App")))
			{
				dlg.WindowTitle = DictionaryConfigurationStrings.Confirm + " " + DictionaryConfigurationStrings.Delete;
				var kindOfConfiguration = DictionaryConfigurationServices.GetDictionaryConfigurationType(PropertyTable);
				dlg.TopBodyText = $"{kindOfConfiguration} {DictionaryConfigurationStrings.View}: {configurationToDelete.Label}";

				if (IsConfigurationACustomizedOriginal(configurationToDelete))
				{
					dlg.TopMessage =
						IsConfigurationAnOriginalReversal(configurationToDelete, _cache) &&
						!IsAllReversalIndexConfig(configurationToDelete) ? DictionaryConfigurationStrings.YouAreResettingReversal : DictionaryConfigurationStrings.YouAreResetting;
					dlg.BottomQuestion = DictionaryConfigurationStrings.WantContinue;
					dlg.DeleteButtonText = DictionaryConfigurationStrings.Reset;
					dlg.WindowTitle = DictionaryConfigurationStrings.Confirm + " " + DictionaryConfigurationStrings.Reset;
				}

				if (dlg.ShowDialog() != DialogResult.Yes)
				{
					return;
				}
			}

			DeleteConfiguration(configurationToDelete);
			ReLoadConfigurations();

			// Re-select configuration that was reset, or select first configuration if we just deleted a
			// configuration.
			if (IsConfigurationACustomizedOriginal(configurationToDelete))
			{
				_view.configurationsListView.Items.Cast<ListViewItem>().First(item => item.Text == configurationToDelete.Label).Selected = true;
			}
			else
			{
				_view.configurationsListView.Items[0].Selected = true;
			}
		}

		/// <summary>
		/// Respond to an export UI button push by letting the user specify what file to export to, and starting the export process.
		/// </summary>
		private void OnExportConfiguration(object sender, EventArgs e)
		{
			// Not capable of exporting new configurations yet.
			if (IsDirty)
			{
				MessageBox.Show(_view, DictionaryConfigurationStrings.kstidConfigsChanged);
				return;
			}

			if (string.IsNullOrEmpty(SelectedConfiguration.FilePath))
			{
				throw new ArgumentNullException("The configuration selected for export has an empty file path.");
			}
			if (Path.GetDirectoryName(SelectedConfiguration.FilePath) == _defaultConfigDir)
			{
				SelectedConfiguration.FilePath = Path.Combine(_projectConfigDir, Path.GetFileName(SelectedConfiguration.FilePath));
				SelectedConfiguration.Save();
			}

			var disallowedCharacters = MiscUtils.GetInvalidProjectNameChars(MiscUtils.FilenameFilterStrength.kFilterBackup) + " $%";
			string outputPath;
			using (var saveDialog = new DialogAdapters.SaveFileDialogAdapter())
			{
				saveDialog.Title = DictionaryConfigurationStrings.kstidChooseExportFile;
				saveDialog.FileName = StringUtils.FilterForFileName(SelectedConfiguration + "_FLEx-Dictionary-Configuration_" + DateTime.Now.ToString("yyyy-MM-dd"), disallowedCharacters);
				saveDialog.DefaultExt = "zip";
				saveDialog.AddExtension = true;
				saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

				var result = saveDialog.ShowDialog(_view);
				if (result != DialogResult.OK)
				{
					return;
				}
				outputPath = saveDialog.FileName;
			}

			// Append ".zip" if user entered something like "foo.gif", which loses the hidden ".zip" extension.
			if (!outputPath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
			{
				outputPath += ".zip";
			}

			ExportConfiguration(SelectedConfiguration, outputPath, _cache);
		}

		/// <summary>
		/// Create a zip file containing a dictionary configuration for the user to share, into destinationZipPath. LT-17397.
		/// </summary>
		internal static void ExportConfiguration(DictionaryConfigurationModel configurationToExport, string destinationZipPath, LcmCache cache)
		{
			if (configurationToExport == null)
			{
				throw new ArgumentNullException(nameof(configurationToExport));
			}

			if (string.IsNullOrWhiteSpace(destinationZipPath))
			{
				throw new ArgumentNullException(nameof(destinationZipPath));
			}

			if (cache == null)
			{
				throw new ArgumentNullException(nameof(cache));
			}

			using (var zip = new ZipFile())
			{
				zip.AddFile(configurationToExport.FilePath, "/");
				PrepareCustomFieldsExport(cache).ForEach(file => zip.AddFile(file, "/"));
				zip.AddFile(PrepareStylesheetExport(cache), "/");
				zip.Save(destinationZipPath);
			}
		}

		/// <summary>
		/// Prepare custom fields to be included in dictionary configuration export. LT-17397.
		/// Returns paths to files to be included in a zipped export.
		/// </summary>
		internal static IEnumerable<string> PrepareCustomFieldsExport(LcmCache cache)
		{
			var exporter = new LiftExporter(cache);
			var liftFile = Path.Combine(Path.GetTempPath(), "DictExportCustomLift", "CustomFields.lift");
			var rangesFile = Path.Combine(Path.GetTempPath(), "DictExportCustomLift", "CustomFields.lift-ranges");
			Directory.CreateDirectory(Path.GetDirectoryName(liftFile));
			using (TextWriter textWriter = new StreamWriter(liftFile))
			{
				exporter.ExportLift(textWriter, Path.GetDirectoryName(liftFile), new ILexEntry[0], 0);
			}
			using (var stringWriter = new StringWriter())
			{
				exporter.ExportLiftRanges(stringWriter);
				stringWriter.Flush();
				File.WriteAllText(rangesFile, stringWriter.ToString());
			}
			return new[] {liftFile, rangesFile};
		}

		/// <summary>
		/// Prepare stylesheet to be included in dictionary configuration export. LT-17397.
		/// Returns paths to files to be included in a zipped export.
		/// </summary>
		internal static string PrepareStylesheetExport(LcmCache cache)
		{
			var projectStyles = new FlexStylesXmlAccessor(cache.LangProject.LexDbOA, true);
			var serializer = new XmlSerializer(typeof(FlexStylesXmlAccessor));

			var tempFile = Path.Combine(Path.GetTempPath(), "DictExportStyles", "CustomStyles.xml");
			Directory.CreateDirectory(Path.GetDirectoryName(tempFile));
			using (var textWriter = new StreamWriter(tempFile))
			{
				serializer.Serialize(textWriter, projectStyles);
			}
			return tempFile;
		}

		/// <summary>
		/// Handle configuration import request by user.
		/// </summary>
		private void OnImportConfiguration(object sender, EventArgs e)
		{
			// Not capable of exporting new configurations yet.
			if (IsDirty)
			{
				MessageBox.Show(_view, DictionaryConfigurationStrings.kstidConfigsChangedImport);
				return;
			}

			var importController = new DictionaryConfigurationImportController(_cache, _projectConfigDir, _configurations);
			using (var importDialog = new DictionaryConfigurationImportDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")) { HelpTopic = _view.HelpTopic })
			{
				importController.DisplayView(importDialog);
			}

			if (!importController.ImportHappened)
			{
				return;
			}
			CloseDialogAndRefreshProject();
		}

		private void CloseDialogAndRefreshProject()
		{
			_view.Close();
			ConfigurationViewImported?.Invoke();

			PropertyTable.GetValue<IFwMainWnd>("window").RefreshAllViews();
		}

		public bool IsConfigurationACustomizedOriginal(DictionaryConfigurationModel configuration)
		{
			return IsConfigurationACustomizedOriginal(configuration, _defaultConfigDir, _cache);
		}

		public static bool IsConfigurationACustomizedOriginal(DictionaryConfigurationModel config, string defaultConfigDir, LcmCache cache)
		{
			return IsConfigurationACustomizedShippedDefault(config, defaultConfigDir) || IsConfigurationAnOriginalReversal(config, cache);
		}

		/// <summary>
		/// Whether a configuration is, or is a customization of, a shipped default configuration,
		/// such as the shipped Root-based, Lexeme-based, or Bartholomew configurations.
		/// </summary>
		public static bool IsConfigurationACustomizedShippedDefault(DictionaryConfigurationModel configuration, string defaultConfigDir)
		{
			if (configuration.FilePath == null)
			{
				return false;
			}

			var defaultConfigurationFiles = FileUtils.GetFilesInDirectory(defaultConfigDir).Select(Path.GetFileName);

			var filename = Path.GetFileName(configuration.FilePath);
			return defaultConfigurationFiles.Contains(filename);
		}

		/// <summary>
		/// Whether a configuration represents a Reversal.
		/// </summary>
		public static bool IsConfigurationAnOriginalReversal(DictionaryConfigurationModel configuration, LcmCache cache)
		{
			if (configuration.FilePath == null)
			{
				return false;
			}
			// No configuration.WritingSystem means it is not a reversal, or that it is the AllReversalIndexes which doesn't act any different from a default config
			if (!string.IsNullOrWhiteSpace(configuration.WritingSystem) && IetfLanguageTag.IsValid(configuration.WritingSystem))
			{
				var writingSystem = (CoreWritingSystemDefinition)cache.WritingSystemFactory.get_Engine(configuration.WritingSystem);
				// The reversals start out with the filename matching the ws Id, copies will have a different file name
				return writingSystem.Id == Path.GetFileNameWithoutExtension(configuration.FilePath);
			}
			return false;
		}

		#region Implementation of IPropertyTableProvider
		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }
		#endregion

		#region Implementation of IPublisherProvider
		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }
		#endregion

		#region Implementation of ISubscriberProvider
		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }
		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			_cache = PropertyTable.GetValue<LcmCache>("cache");

			if (!PropertyTable.GetValue("SkipSomeTestInitialization", false))
			{
				// Populate lists of configurations and publications
				ReLoadConfigurations();
				ReLoadPublications();

				_view.Shown += OnShowDialog;
			}
		}
		#endregion
	}
}
