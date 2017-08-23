// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using Ionic.Zip;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Lift.Migration;
using SIL.Lift.Parsing;
using SIL.Linq;
using SIL.Reporting;
using SIL.LCModel.Utils;
using File = System.IO.File;


namespace LanguageExplorer.Works
{
	/// <summary>
	/// Handle the importing of dictionary configurations.
	/// </summary>
	public class DictionaryConfigurationImportController
	{
		private LcmCache _cache;
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

		/// <summary>
		/// The custom fields found in the lift file which will be added if they aren't present in the project
		/// </summary>
		internal IEnumerable<string> _customFieldsToImport;

		/// <summary>Did the configuration get imported.</summary>
		public bool ImportHappened;

		/// <summary>
		/// Label of configuration in file being imported. May be different than final label used, such as if a configuration already exists with that label.
		/// </summary>
		internal string _originalConfigLabel;

		/// <summary>
		/// Label to use for configuration being imported if we use a label that isn't already in use.
		/// </summary>
		internal string _proposedNewConfigLabel;

		/// <summary>
		/// Path to the lift file with the custom fields we are planning to import.
		/// </summary>
		private string _importLiftLocation;

		/// <summary>
		/// Location of the temporary styles file during import process.
		/// </summary>
		private string _importStylesLocation;

		/// <summary>
		/// Is the import config is valid to the current view.
		/// </summary>
		private bool _isInvalidConfigFile;

		/// <summary>
		/// The following style names are known to have unsupported features. We will avoid wiping out default styles of these types when
		/// importing a view.
		/// </summary>
		public static readonly HashSet<string> UnsupportedStyles = new HashSet<string>
		{
			"Bulleted List", "Numbered List", "Homograph-Number"
		};

		/// <summary/>
		public DictionaryConfigurationImportController(LcmCache cache, string projectConfigDir,
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

			ImportCustomFields(_importLiftLocation);

			// If the configuration to import has the same label as an existing configuration in the project folder
			// then overwrite the existing configuration.
			var existingConfigurationInTheWay = _configurations.FirstOrDefault(config => config.Label == NewConfigToImport.Label &&
				Path.GetDirectoryName(config.FilePath) == _projectConfigDir);

			NewConfigToImport.Publications.ForEach(
				publication =>
				{
					AddPublicationTypeIfNotPresent(publication, _cache);
				});
			try
			{
				ImportStyles(_importStylesLocation);
				ImportHappened = true;
			}
			catch (InstallationException e) // This is the exception thrown if the dtd guid in the style file doesn't match our program
			{
#if DEBUG
				if (_view == null) // _view is sometimes null in unit tests, and it's helpful to know what exactly went wrong.
					throw new Exception(xWorksStrings.kstidCannotImport, e);
#endif
				_view.explanationLabel.Text = xWorksStrings.kstidCannotImport;
			}

			// We have re-loaded the model from disk to preserve custom field state so the Label must be set here
			NewConfigToImport.FilePath = _temporaryImportConfigLocation;
			NewConfigToImport.Load(_cache);
			if (existingConfigurationInTheWay != null)
			{
				_configurations.Remove(existingConfigurationInTheWay);
				if (existingConfigurationInTheWay.FilePath != null)
				{
					FileUtils.Delete(existingConfigurationInTheWay.FilePath);
				}
			}
			else
			{
				NewConfigToImport.Label = _proposedNewConfigLabel;
			}

			// Set a filename for the new configuration. Use a unique filename that isn't either registered with another configuration, or existing on disk. Note that in this way, we ignore what the original filename was of the configuration file in the .zip file.
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigDir, _configurations, NewConfigToImport);

			var outputConfigPath = existingConfigurationInTheWay != null ? existingConfigurationInTheWay.FilePath : NewConfigToImport.FilePath;

			File.Move(_temporaryImportConfigLocation, outputConfigPath);

			NewConfigToImport.FilePath = outputConfigPath;
			_configurations.Add(NewConfigToImport);

			// phone home (analytics)
			var configType = NewConfigToImport.Type;
			var configDir = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(
				configType == DictionaryConfigurationModel.ConfigType.Reversal
					? DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName
					: DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var isCustomizedOriginal = DictionaryConfigurationManagerController.IsConfigurationACustomizedOriginal(NewConfigToImport, configDir, _cache);
			UsageReporter.SendEvent("DictionaryConfigurationImport", "Import", "Import Config",
				string.Format("Import of [{0}{1}]:{2}",
					configType, isCustomizedOriginal ? string.Empty : "-Custom", ImportHappened ? "succeeded" : "failed"), 0);
		}

		private void ImportStyles(string importStylesLocation)
		{
			NonUndoableUnitOfWorkHelper.DoSomehow(_cache.ActionHandlerAccessor, () =>
			{
				var stylesToRemove = _cache.LangProject.StylesOC.Where(style => !UnsupportedStyles.Contains(style.Name));

				// For LT-18267, record basedon and next properties of styles not
				// being exported, so they can be reconnected to the imported
				// styles of the same name.
				var preimportStyleLinks = _cache.LangProject.StylesOC.Where(style => UnsupportedStyles.Contains(style.Name)).ToDictionary(
					style => style.Name,
					style => new
					{
						BasedOn = style.BasedOnRA == null ? null : style.BasedOnRA.Name,
						Next = style.NextRA == null ? null : style.NextRA.Name
					});

				// Before importing styles, remove all the current styles, except
				// for styles that we don't support and so we don't expect will
				// be imported.
				foreach (var style in stylesToRemove)
				{
					_cache.LangProject.StylesOC.Remove(style);
				}

				// Import styles
				//var stylesAccessor = new FlexStylesXmlAccessor(_cache.LangProject.LexDbOA, true, importStylesLocation);

				var postimportStylesToReconnect = _cache.LangProject.StylesOC.Where(style => UnsupportedStyles.Contains(style.Name));

				postimportStylesToReconnect.ForEach(postimportStyleToRewire =>
				{
					var correspondingPreImportStyleInfo = preimportStyleLinks[postimportStyleToRewire.Name];

					postimportStyleToRewire.BasedOnRA = _cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == correspondingPreImportStyleInfo.BasedOn);

					postimportStyleToRewire.NextRA = _cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == correspondingPreImportStyleInfo.Next);
				});
			});
		}

		private void ImportCustomFields(string liftPathname)
		{
			if (string.IsNullOrEmpty(liftPathname))
				return;
			NonUndoableUnitOfWorkHelper.DoSomehow(_cache.ActionHandlerAccessor, () =>
			{
				string sFilename;
				var fMigrationNeeded = Migrator.IsMigrationNeeded(liftPathname);
				if (fMigrationNeeded)
				{
					var sOldVersion = SIL.Lift.Validation.Validator.GetLiftVersion(liftPathname);
					sFilename = Migrator.MigrateToLatestVersion(liftPathname);
				}
				else
				{
					sFilename = liftPathname;
				}
				var flexImporter = new FlexLiftMerger(_cache, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, true);
				var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);
				flexImporter.LiftFile = liftPathname;
				var liftRangesFile = liftPathname + "-ranges";
				if (File.Exists(liftRangesFile))
				{
					flexImporter.LoadLiftRanges(liftRangesFile);
				}

				parser.ReadLiftFile(sFilename);
			});
		}

		/// <summary>
		/// Add publication type if it's not in the project's list of publications.
		/// </summary>
		private static void AddPublicationTypeIfNotPresent(string name, LcmCache cache)
		{
			if (cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
				.Select(pub => pub.Name.get_String(cache.DefaultAnalWs).Text).Contains(name))
				return;
			AddPublicationType(name, cache);
		}

		public static ICmPossibility AddPublicationType(string name, LcmCache cache)
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
			{
				ImportHappened = false;
				NewConfigToImport = null;
				_originalConfigLabel = null;
				_temporaryImportConfigLocation = null;
				_newPublications = null;
				return;
			}

			try
			{
				using (var zip = new ZipFile(configurationZipPath))
				{
					var tmpPath = Path.GetTempPath();
					var configInZip = zip.SelectEntries("*" + DictionaryConfigurationModel.FileExtension).First();
					configInZip.Extract(tmpPath, ExtractExistingFileAction.OverwriteSilently);
					_temporaryImportConfigLocation = tmpPath + configInZip.FileName;
					if(!FileUtils.IsFileReadableAndWritable(_temporaryImportConfigLocation))
					{
						File.SetAttributes(_temporaryImportConfigLocation, FileAttributes.Normal);
					}
					var customFieldLiftFile = zip.SelectEntries("*.lift").First();
					customFieldLiftFile.Extract(tmpPath, ExtractExistingFileAction.OverwriteSilently);
					var liftRangesFile = zip.SelectEntries("*.lift-ranges").First();
					liftRangesFile.Extract(tmpPath, ExtractExistingFileAction.OverwriteSilently);
					_importLiftLocation = tmpPath + customFieldLiftFile.FileName;
					var stylesFile = zip.SelectEntries("*.xml").First();
					stylesFile.Extract(tmpPath, ExtractExistingFileAction.OverwriteSilently);
					_importStylesLocation = tmpPath + stylesFile.FileName;
				}
			}
			catch (Exception)
			{
				ClearValuesOnError();
				return;
			}

			NewConfigToImport = new DictionaryConfigurationModel(_temporaryImportConfigLocation, _cache);

			//Validating the user is not trying to import a Dictionary into a Reversal area or a Reversal into a Dictionary area
			var configDirectory = Path.GetFileName(_projectConfigDir);
			if (DictionaryConfigurationListener.DictionaryConfigurationDirectoryName.Equals(configDirectory) && NewConfigToImport.IsReversal
				|| !DictionaryConfigurationListener.DictionaryConfigurationDirectoryName.Equals(configDirectory) && !NewConfigToImport.IsReversal)
			{
				_isInvalidConfigFile = true;
				ClearValuesOnError();
				return;
			}
			_isInvalidConfigFile = false;

			// Reset flag
			ImportHappened = false;

			_newPublications =
				DictionaryConfigurationModel.PublicationsInXml(_temporaryImportConfigLocation).Except(NewConfigToImport.Publications);

			_customFieldsToImport = CustomFieldsInLiftFile(_importLiftLocation);
			// Use the full list of publications in the XML file, even ones that don't exist in the project.
			NewConfigToImport.Publications = DictionaryConfigurationModel.PublicationsInXml(_temporaryImportConfigLocation).ToList();

			// Make a new, unique label for the imported configuration, if needed.
			var newConfigLabel = NewConfigToImport.Label;
			_originalConfigLabel = NewConfigToImport.Label;
			var i = 1;
			while (_configurations.Any(config => config.Label == newConfigLabel))
			{
				newConfigLabel = string.Format(xWorksStrings.kstidImportedSuffix, NewConfigToImport.Label, i++);
			}
			NewConfigToImport.Label = newConfigLabel;
			_proposedNewConfigLabel = newConfigLabel;

			// Not purporting to use any particular file location yet.
			NewConfigToImport.FilePath = null;
		}

		private void ClearValuesOnError()
		{
			ImportHappened = false;
			NewConfigToImport = null;
			_originalConfigLabel = null;
			_temporaryImportConfigLocation = null;
			_newPublications = null;
		}

		/// <summary>
		/// Returns all custom fields from the given lift file
		/// </summary>
		private IEnumerable<string> CustomFieldsInLiftFile(string liftFilePath)
		{
			var liftDoc = XDocument.Load(liftFilePath);
			var customFields = liftDoc.XPathSelectElements("//field[form[@lang='qaa-x-spec']]");
			return customFields.Select(cf => cf.Attribute("tag").Value);
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
			var customFieldStatus = string.Empty;
			_view.explanationLabel.Text = "";

			if (NewConfigToImport == null)
			{
				string invalidConfigFileMsg = string.Empty;
				if (_isInvalidConfigFile)
				{
					var configType = Path.GetFileName(_projectConfigDir) == DictionaryConfigurationListener.DictionaryConfigurationDirectoryName
					? xWorksStrings.ReversalIndex : xWorksStrings.Dictionary;
					invalidConfigFileMsg = string.Format(xWorksStrings.DictionaryConfigurationMismatch, configType)
						+ Environment.NewLine;
				}
				_view.explanationLabel.Text = invalidConfigFileMsg + xWorksStrings.kstidCannotImport;
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
				publicationStatus = xWorksStrings.kstidPublicationsWillBeAdded + Environment.NewLine + string.Join(", ", _newPublications);
			}

			if (_customFieldsToImport != null && _customFieldsToImport.Any())
			{
				customFieldStatus = xWorksStrings.kstidCustomFieldsWillBeAdded + Environment.NewLine + string.Join(", ", _customFieldsToImport);
			}

			_view.explanationLabel.Text = string.Format("{0}{1}{2}{1}{3}{1}{4}",
				mainStatus, Environment.NewLine + Environment.NewLine, publicationStatus, customFieldStatus,
				xWorksStrings.DictionaryConfigurationDictionaryConfigurationUser_StyleOverwriteWarning);
			_view.Refresh();
		}

		/// <summary>
		/// When a new import file is specified, either by typing one in or selecting one using the file open dialog, prepare to import that file.
		/// </summary>
		public void RefreshBasedOnNewlySelectedImportFile()
		{
			PrepareImport(_view.importPathTextBox.Text);

			if (NewConfigToImport == null || _isInvalidConfigFile)
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
