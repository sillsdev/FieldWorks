// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ionic.Zip;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.Utils.FileDialog;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Handle the importing of dictionary configurations.
	/// </summary>
	public class DictionaryConfigurationImportController
	{
		private FdoCache _cache;
		private string _projectConfigDir;
		/// <summary>
		/// Registered configurations that we know about.
		/// </summary>
		internal List<DictionaryConfigurationModel> _configurations;
		/// <summary>
		/// View that this controller manipulates.
		/// </summary>
		private DictionaryConfigurationImportDlg _view;

		/// <summary>
		/// New configuration to be imported. And it was imported, if ImportHappened.
		/// </summary>
		public DictionaryConfigurationModel NewConfigToImport { get; set; }

		/// <summary>
		/// Path to the config that we are preparing to import.
		/// </summary>
		internal string _temporaryImportConfigLocation = null;

		/// <summary>
		/// New publications that will be added by the import.
		/// </summary>
		internal IEnumerable<string> _newPublications = null;

		/// Did the configuration get imported.
		/// </summary>
		public bool ImportHappened;

		/// <summary>
		/// Label of configuration in file being imported. May be different than final label used, such as if a configuration already exists with that label.
		/// </summary>
		internal string _originalConfigLabel;

		/// <summary>
		/// Label to use for configuration being imported if we use a label that isn't already in use.
		/// </summary>
		internal string _proposedNewConfigLabel;

		public DictionaryConfigurationImportController(FdoCache cache, string projectConfigDir,
			List<DictionaryConfigurationModel> configurations)
		{
			_cache = cache;
			_projectConfigDir = projectConfigDir;
			_configurations = configurations;
		}

		/// <summary>
		/// Perform the configuration import that the controller has prepared to do.
		/// </summary>
		internal void DoImport()
		{
			Debug.Assert(NewConfigToImport != null);

			// If the configuration to import has the same label as an existing configuration at this point, then overwrite the existing configuration.
			var existingConfigurationInTheWay = _configurations.FirstOrDefault(config => config.Label == NewConfigToImport.Label);
			if (existingConfigurationInTheWay != null)
			{
				// TODO Account for importing configurations with labels that are the same as the labels of shipped configurations.
				_configurations.Remove(existingConfigurationInTheWay);
				if (existingConfigurationInTheWay.FilePath != null)
				{
					FileUtils.Delete(existingConfigurationInTheWay.FilePath);
				}
			}

			// Set a filename for the new configuration. Use a unique filename that isn't either registered with another configuration, or existing on disk. Note that in this way, we ignore what the original filename was of the configuration file in the .zip file.
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigDir, _configurations, NewConfigToImport);

			var outputConfigPath = NewConfigToImport.FilePath;

			File.Move(_temporaryImportConfigLocation, outputConfigPath);

			_configurations.Add(NewConfigToImport);

			NewConfigToImport.Publications.ForEach(
				publication =>
				{
					AddPublicationTypeIfNotPresent(publication, _cache);
				});

			ImportHappened = true;
		}

		/// <summary>
		/// Add publication type if it's not in the project's list of publications.
		/// </summary>
		private static void AddPublicationTypeIfNotPresent(string name, FdoCache cache)
		{
			if (cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
				.Select(pub => pub.Name.get_String(cache.DefaultAnalWs).Text).Contains(name))
				return;
			AddPublicationType(name, cache);
		}

		public static ICmPossibility AddPublicationType(string name, FdoCache cache)
		{
			Debug.Assert(cache.LangProject.LexDbOA.PublicationTypesOA != null);

			var item = cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			NonUndoableUnitOfWorkHelper.DoSomehow(cache.ActionHandlerAccessor, () =>
			{
				cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(item);
				item.Name.set_String(cache.DefaultAnalWs, name);
			});
			return item;
		}


		/// <summary>
		/// Prepare this controller to import from a dictionary configuration zip file.
		///
		/// TODO Validate the XML first and/or handle failure to create DictionaryConfigurationModel object.
		/// TODO Handle if zip has no .fwdictconfig file.
		/// TODO Handle if file is not a zip, or a corrupted zip file.
		/// </summary>
		internal void PrepareImport(string configurationZipPath)
		{
			if (string.IsNullOrEmpty(configurationZipPath))
				throw new ArgumentException();

			try
			{
				using (var zip = new ZipFile(configurationZipPath))
				{
					var tmpPath = Path.GetTempPath();
					var configInZip = zip.SelectEntries("*" + DictionaryConfigurationModel.FileExtension).First();
					configInZip.Extract(tmpPath, ExtractExistingFileAction.OverwriteSilently);
					_temporaryImportConfigLocation = tmpPath + configInZip.FileName;
				}
			}
			catch (Exception)
			{
				ImportHappened = false;
				NewConfigToImport = null;
				_originalConfigLabel = null;
				_temporaryImportConfigLocation = null;
				_newPublications = null;
				return;
			}

			NewConfigToImport = new DictionaryConfigurationModel(_temporaryImportConfigLocation, _cache);

			// Reset flag
			ImportHappened = false;

			_newPublications =
				DictionaryConfigurationModel.PublicationsInXml(_temporaryImportConfigLocation).Except(NewConfigToImport.Publications);

			// Use the full list of publications in the XML file, even ones that don't exist in the project.
			NewConfigToImport.Publications = DictionaryConfigurationModel.PublicationsInXml(_temporaryImportConfigLocation).ToList();

			// Make a new, unique label for the imported configuration, if needed.
			var newConfigLabel = NewConfigToImport.Label;
			_originalConfigLabel = NewConfigToImport.Label;
			var i = 1;
			while (_configurations.Any(config => config.Label == newConfigLabel))
			{
				newConfigLabel = String.Format(xWorksStrings.kstidImportedSuffix, NewConfigToImport.Label, i++);
			}
			NewConfigToImport.Label = newConfigLabel;
			_proposedNewConfigLabel = newConfigLabel;

			// Not purporting to use any particular file location yet.
			NewConfigToImport.FilePath = null;
		}

		/// <summary>
		/// Connect to and show a view for the user to perform an import.
		/// </summary>
		/// <param name="dialog"></param>
		public void DisplayView(DictionaryConfigurationImportDlg dialog)
		{
			_view = dialog;
			_view.browseButton.Click += (a, b) => OnBrowse();
			_view.importPathTextBox.TextChanged += (a, b) => RefreshBasedOnNewlySelectedImportFile();
			_view.importButton.Click += (a, b) => DoImport();
			_view.doOverwriteRadioOption.CheckedChanged += (a, b) =>
			{
				if (_view.doOverwriteRadioOption.Checked)
					UserRequestsOverwrite();
				else
					UserRequestsNotOverwrite();
				RefreshStatusDisplay();
			};
			_view.overwriteGroupBox.Visible = false;
			_view.importButton.Enabled = false;
			_view.ShowDialog();
		}

		/// <summary>
		/// Respond to Browse button by letting user pick a .zip file to import.
		/// </summary>
		public void OnBrowse()
		{
			using (var openDialog = new OpenFileDialogAdapter())
			{
				openDialog.Title = xWorksStrings.kstidChooseFile;
				openDialog.Filter = xWorksStrings.kstidZipFiles + "|*.zip";
				openDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

				var result = openDialog.ShowDialog(_view);
				if (result != DialogResult.OK)
					return;
				var importFilePath = openDialog.FileName;
				_view.importPathTextBox.Text = importFilePath;
			}
		}

		public void RefreshStatusDisplay()
		{
			string mainStatus;
			var publicationStatus = string.Empty;
			_view.explanationLabel.Text = "";

			if (NewConfigToImport == null)
			{
				_view.explanationLabel.Text = xWorksStrings.kstidCannotImport;
				return;
			}
			if (_originalConfigLabel == _proposedNewConfigLabel)
			{
				mainStatus = string.Format(xWorksStrings.kstidImportingConfig, NewConfigToImport.Label);
			}
			else
			{
				mainStatus = string.Format(NewConfigToImport.Label == _proposedNewConfigLabel
						? xWorksStrings.kstidImportingConfigNewName
						: xWorksStrings.kstidImportingAndOverwritingConfiguration,
					NewConfigToImport.Label);
			}

			if (_newPublications != null && _newPublications.Any())
			{
				publicationStatus = "The following publications will be added to the project: " + string.Join(", ", _newPublications);
			}

			_view.explanationLabel.Text = string.Format("{0}{1}{2}", mainStatus, Environment.NewLine, publicationStatus);
		}

		/// <summary>
		/// When a new import file is specified, either by typing one in or selecting one using the file open dialog, prepare to import that file.
		/// </summary>
		public void RefreshBasedOnNewlySelectedImportFile()
		{
			PrepareImport(_view.importPathTextBox.Text);
			if (NewConfigToImport == null)
			{
				// We aren't ready to import. Something didn't work right.

				RefreshStatusDisplay();
				_view.overwriteGroupBox.Visible = false;
				_view.importButton.Enabled = false;
				return;
			}

			// Reset the overwrite setting when choosing a new file.
			_view.notOverwriteRadioOption.Checked = true;

			// Update overwrite radio labels
			_view.doOverwriteRadioOption.Text = string.Format(xWorksStrings.kstidOverwriteConfiguration, _originalConfigLabel);
			_view.notOverwriteRadioOption.Text = string.Format(xWorksStrings.kstidUseNewConfigName, NewConfigToImport.Label);

			// Give the option to overwrite only if there is something to overwrite.
			_view.overwriteGroupBox.Visible = _originalConfigLabel != _proposedNewConfigLabel;

			RefreshStatusDisplay();
			_view.importButton.Enabled = true;
		}

		/// <summary>
		/// Change what will be imported to overwrite an existing configuration with the same label as the configuration being imported.
		/// </summary>
		internal void UserRequestsOverwrite()
		{
			NewConfigToImport.Label = _originalConfigLabel;
		}

		/// <summary>
		/// Change what will be imported back to the default, which is to not overwrite an existing configuration.
		/// </summary>
		internal void UserRequestsNotOverwrite()
		{
			NewConfigToImport.Label = _proposedNewConfigLabel;
		}
	}
}
