// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.SfmToXml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FwCoreDlgs.FileDialog;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public partial class InterlinearSfmImportWizard : WizardDialog, IFwExtension
	{
		protected LcmCache m_cache;
		private IPropertyTable m_propertyTable;
		private IHelpTopicProvider m_helpTopicProvider;
		private List<InterlinearMapping> m_mappings = new List<InterlinearMapping>();
		// Maps from writing system name to most recently selected encoding converter for that WS.
		// Map of the information about which tags follow others, needed to count the number of resulting interlinear texts
		// after the users mapping has been applied.
		private Dictionary<string, Dictionary<string, int>> followedBy = new Dictionary<string, Dictionary<string, int>>();
		private bool m_firstTimeInMappingsPane = true;

		public InterlinearSfmImportWizard()
		{
			InitializeComponent();
			tabSteps.KeyDown += OnKeyDown;
			tabSteps.KeyUp += OnKeyUp;
			ControlServices.EnsureWindows1252ConverterExists();
		}

		protected virtual void SetDialogTitle()
		{
			Text = string.Format(Text, ITextStrings.ksInterlinearTexts);
		}

		void IFwExtension.Init(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher)
		{
			m_cache = cache;
			m_propertyTable = propertyTable;
			if (m_propertyTable != null)
			{
				m_helpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider);
			}
			SetDialogTitle();
		}

		private void m_browseInputFilesButton_Click(object sender, EventArgs e)
		{
			m_fileListBox.Text = GetFiles(m_fileListBox.Text);
			if (string.IsNullOrEmpty(m_fileListBox.Text))
			{
				return;
			}
			var input = InputFiles;
			if (input.Length > 0)
			{
				var settingsPath = GetDefaultOutputSettingsPath(input[0]);
				m_saveSettingsFileBox.Text = settingsPath;
			}
			if (input.Length == 1)
			{
				MakeEndOfTextVisibleAndFocus(m_fileListBox);
			}
			m_loadSettingsFileBox.Text = GetDefaultInputSettingsPath();
			foreach (var path in input)
			{
				var inputSettings = GetDefaultOutputSettingsPath(path);
				if (!File.Exists(inputSettings))
				{
					continue;
				}
				m_loadSettingsFileBox.Text = inputSettings;
				break;
			}
			MakeEndOfTextVisibleAndFocus(m_loadSettingsFileBox);
			m_browseLoadSettingsFileButton.Focus(); // a reasonable choice, and we've messed with focus making things visible
		}

		/// <summary>
		/// Make sure that the end of the text in the given text box is visible.
		/// An (undesired) side effect is to focus the box and put the selection at the end of it.
		/// I cannot find any portable way to achieve the desired scrolling without doing this.
		/// </summary>
		private static void MakeEndOfTextVisibleAndFocus(TextBox textBox)
		{
			if (textBox.Text.Length == 0)
			{
				return;
			}
			// It would seem logical that we would not want the -1, so we would be asking for the position of the
			// imaginary character at the very end. However, that just always returns (0,0).
			var endPosition = textBox.GetPositionFromCharIndex(textBox.Text.Length - 1);
			if (endPosition.X <= textBox.Width)
			{
				return;
			}
			textBox.Focus();
			textBox.Select(textBox.Text.Length, 0);
			textBox.ScrollToCaret();
		}

		private static string GetDefaultOutputSettingsPath(string input)
		{
			return Path.ChangeExtension(input.Substring(0, input.Length - Path.GetExtension(input).Length) + "-import-settings", ".map");
		}

		private string[] InputFiles => SplitPaths(m_fileListBox.Text);

		private string FirstInputFile => InputFiles.FirstOrDefault();

		private void m_browseLoadSettingsFileButton_Click(object sender, EventArgs e)
		{
			// Enhance JohnT: possibly some validation of a mapping file?
			m_loadSettingsFileBox.Text = GetFile(m_loadSettingsFileBox.Text, FirstInputFile, new[]{ FileFilterType.ImportMapping, FileFilterType.AllFiles }, true,
				ITextStrings.ksSelectMapFile, path => true);
		}

		private string GetFiles(string currentFiles)
		{
			using (IOpenFileDialog openFileDialog = new OpenFileDialogAdapter())
			{
				openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.InterlinearSfm, FileFilterType.AllFiles);
				openFileDialog.CheckFileExists = true;
				openFileDialog.Multiselect = true; // can import multiple files
				var files = SplitPaths(currentFiles);
				var dir = string.Empty;
				var initialFileName = string.Empty;
				openFileDialog.FileName = string.Empty;
				if (files.Length > 0)
				{
					var firstFilePath = files[0].Trim();
					// LT-6620 : putting in an invalid path was causing an exception in the openFileDialog.ShowDialog()
					// Now we make sure parts are valid before setting the values in the openfile dialog.
					try
					{
						dir = Path.GetDirectoryName(firstFilePath);
						if (File.Exists(firstFilePath))
						{
							initialFileName = Path.GetFileName(firstFilePath);
						}
					}
					catch
					{
					}
				}
				if (Directory.Exists(dir))
				{
					openFileDialog.InitialDirectory = dir;
				}
				// It doesn't seem to be possible to open the dialog with more than one file selected.
				// However there will often be only one so that's at least somewhat helpful.
				openFileDialog.FileName = initialFileName;
				openFileDialog.Title = ITextStrings.ksSelectInterlinFile;
				while (true) // loop until approved set of files or cancel
				{
					if (openFileDialog.ShowDialog() != DialogResult.OK)
					{
						return currentFiles;
					}
					var badFiles = openFileDialog.FileNames.Where(fileName => !new IsSfmFile(fileName).IsValid).ToList();
					if (!badFiles.Any())
					{
						return JoinPaths(openFileDialog.FileNames);
					}
					var msg = string.Format(ITextStrings.ksInvalidInterlinearFiles, string.Join(", ", badFiles.ToArray()));
					var dr = MessageBox.Show(this, msg, ITextStrings.ksPossibleInvalidFile, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
					if (dr == DialogResult.Yes)
					{
						return JoinPaths(openFileDialog.FileNames);
					}
					if (dr == DialogResult.No)
					{
						continue; // loop and show dialog again...hopefully same files selected.
					}
					break; // user must have chosen cancel, break out of loop
				}
				return currentFiles; // leave things unchanged.
			}
		}

		// Join a list of file paths into something that will look reasonable to the user but can be parsed back into the
		// same list of paths. Unfortunately paths can contain spaces and commas. We take the approach of quoting any
		// path that contains spaces or commas. Paths can NOT contain quotes.
		internal static string JoinPaths(string[] paths)
		{
			return string.Join(", ", paths.Select(x => x.IndexOf(',') >= 0 || x.IndexOf(' ') >= 0 ? "\"" + x + "\"" : x));
		}

		// Split up a list in the format produced by JoinPaths. We need to handle pathological strings that could NOT be
		// so produced, too, because the user can type straight into the box.
		internal static string[] SplitPaths(string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				return new string[0];
			}
			var results = new List<string>();
			var remaining = input;
			for (; ; )
			{
				var index = remaining.IndexOf('"');
				if (index < 0)
				{
					AddSimpleItems(results, remaining);
					return results.ToArray();
				}
				var piece = remaining.Substring(0, index);
				AddSimpleItems(results, piece);
				var nextQuote = remaining.IndexOf('"', index + 1);
				if (nextQuote <= 0)
				{
					// unmatched...ugh!
					results.Add(remaining.Substring(index + 1));
					return results.ToArray();
				}
				results.Add(remaining.Substring(index + 1, nextQuote - index - 1));
				remaining = remaining.Substring(nextQuote + 1);
			}
		}

		private static void AddSimpleItems(List<string> results, string remaining)
		{
			if (string.IsNullOrWhiteSpace(remaining))
			{
				return;
			}
			results.AddRange(remaining.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)));
		}

		private string GetFile(string currentFile, string pathForInitialDirectory, FileFilterType[] types, bool checkFileExists, string title, Func<string, bool> isValidFile)
		{
			using (IOpenFileDialog openFileDialog = new OpenFileDialogAdapter())
			{
				openFileDialog.Filter = ResourceHelper.BuildFileFilter(types);
				openFileDialog.CheckFileExists = checkFileExists;
				openFileDialog.Multiselect = false;
				var done = false;
				while (!done)
				{
					// LT-6620 : putting in an invalid path was causing an exception in the openFileDialog.ShowDialog()
					// Now we make sure parts are valid before setting the values in the openfile dialog.
					var dir = string.Empty;
					try
					{
						dir = Path.GetDirectoryName(pathForInitialDirectory);
					}
					catch
					{
					}
					if (Directory.Exists(dir))
					{
						openFileDialog.InitialDirectory = dir;
					}
					if (File.Exists(currentFile))
					{
						openFileDialog.FileName = currentFile;
					}
					else
					{
						openFileDialog.FileName = string.Empty;
					}
					openFileDialog.Title = title;
					if (openFileDialog.ShowDialog() == DialogResult.OK)
					{
						if (isValidFile(openFileDialog.FileName))
						{
							return openFileDialog.FileName;
						}
						var dr = MessageBox.Show(this, string.Format(ITextStrings.ksInvalidFileAreYouSure, openFileDialog.FileName), ITextStrings.ksPossibleInvalidFile, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
						switch (dr)
						{
							case DialogResult.Yes:
								return openFileDialog.FileName;
							case DialogResult.No:
								continue;
						}
						break;  // exit with current still
					}
					done = true;
				}
				return currentFile;
			}
		}

		protected override void OnNextButton()
		{
			switch (CurrentStepNumber)
			{
				case 0:
					// Populate m_mappingsList based on the selected files.
					var sfmcounts = new Dictionary<string, int>();
					var sfmOrder = new Dictionary<int, string>(); // key is 100000*fileNum + orderInFile, value is a marker
					var fileNum = 0;
					foreach (var pathName in InputFiles)
					{
						var reader = new SfmFileReaderEx(pathName);
						followedBy = reader.MyFollowedByInfo;
						foreach (string marker in reader.SfmInfo)
						{
							if (!sfmcounts.TryGetValue(marker, out var oldVal))
							{
								// first time we've seen it: this file determines order;
								sfmOrder[fileNum * 100000 + reader.GetSFMOrder(marker)] = marker;
							}
							sfmcounts[marker] = oldVal + reader.GetSFMCount(marker);
						}
						fileNum++;
					}
					// Read the map file (unless we've been to this pane before...then use the saved settings), integrate with the sfmcount info.
					var savedMappings = new Dictionary<string, InterlinearMapping>();
					m_oldMappings = m_firstTimeInMappingsPane ? LoadSettings() : new List<InterlinearMapping>((m_mappings));
					m_firstTimeInMappingsPane = false;
					foreach (var mapping in m_oldMappings)
					{
						savedMappings[mapping.Marker] = mapping;
					}
					m_mappings.Clear();
					var keys = new List<int>(sfmOrder.Keys);
					keys.Sort();
					foreach (var key in keys)
					{
						var marker = sfmOrder[key];
						if (savedMappings.TryGetValue(marker, out var mapping))
						{
							mapping = new InterlinearMapping(mapping);
							if (string.IsNullOrEmpty(mapping.WritingSystem))
							{
								var ws = GetDefaultWs(mapping);
								if (ws != 0)
								{
									mapping.WritingSystem = m_cache.WritingSystemFactory.GetStrFromWs(ws);
								}
							}
							else if (mapping.WritingSystem == "{vern}")
							{
								mapping.WritingSystem = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultVernWs);
							}
						}
						else
						{
							mapping = new InterlinearMapping() { Marker = marker };
						}
						mapping.Count = sfmcounts[marker].ToString();
						m_mappings.Add(mapping);
					}
					m_mappingsList.SuspendLayout();
					m_mappingsList.Items.Clear();
					foreach (var mapping in m_mappings)
					{
						var item = new ListViewItem("\\" + mapping.Marker);
						item.SubItems.Add(mapping.Count);
						item.SubItems.Add(GetDestinationName(mapping.Destination));
						item.SubItems.Add(mapping.WritingSystem != null ? GetWritingSystemName(mapping.WritingSystem) : "");
						item.SubItems.Add(mapping.Converter ?? "");
						m_mappingsList.Items.Add(item);
					}
					if (m_mappingsList.Items.Count > 0)
					{
						m_mappingsList.SelectedIndices.Add(0);
					}
					m_mappingsList.ResumeLayout();
					break;
				case 1:
					var currentVernacWSs = m_cache.LanguageProject.VernacularWritingSystems;
					var currentAnalysWSs = m_cache.LanguageProject.AnalysisWritingSystems;
					var vernToAdd = new ArrayList();
					var analysToAdd = new ArrayList();
					var textCount = CalculateTextCount(m_mappings, followedBy);
					foreach (var mapping in m_mappings)
					{
						if (mapping.Destination == InterlinDestination.Ignored)
						{
							continue; // may well have no WS, in any case, we don't care whether it's in our list.
						}
						var creationCancelled = false;
						var ws = (CoreWritingSystemDefinition)m_cache.WritingSystemFactory.get_Engine(mapping.WritingSystem);
						if (mapping.Destination == InterlinDestination.Baseline || mapping.Destination == InterlinDestination.Wordform)
						{
							if (currentVernacWSs.Contains(ws) || vernToAdd.Contains(ws))
							{
								continue;
							}
							//Show creation dialog for Vernacular
							var result = MessageBox.Show(this, string.Format(ITextStrings.ksImportSFMInterlinNewVernac, ws), string.Format(ITextStrings.ksImportSFMInterlinNewWSTitle, ws), MessageBoxButtons.YesNo);
							if (result == DialogResult.Yes)
							{
								vernToAdd.Add(ws);
							}
							else //if they bail out we won't add any writing systems, they might change them all
							{
								return;
							}
						}
						else
						{
							if (currentAnalysWSs.Contains(ws) || analysToAdd.Contains(ws))
							{
								continue;
							}
							var result = MessageBox.Show(this, string.Format(ITextStrings.ksImportSFMInterlinNewAnalysis, ws), string.Format(ITextStrings.ksImportSFMInterlinNewWSTitle, ws), MessageBoxButtons.YesNo);
							if (result == DialogResult.Yes)
							{
								analysToAdd.Add(ws);
							}
							else  //if they bail out we won't add any writing systems, they might change them all
							{
								return;
							}
						}
					}
					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, () => //Add all the collected new languages into the project in their proper section.
					{
						foreach (CoreWritingSystemDefinition analysLang in analysToAdd)
						{
							m_cache.LanguageProject.AddToCurrentAnalysisWritingSystems(analysLang);
						}
						foreach (CoreWritingSystemDefinition vernLang in vernToAdd)
						{
							m_cache.LanguageProject.AddToCurrentVernacularWritingSystems(vernLang);
						}
					});
					numberOfTextsLabel.Text = textCount > 1 ? string.Format(ITextStrings.ksImportSFMInterlinTextCount, textCount) : string.Empty;

					break;
			}

			base.OnNextButton();
		}

		protected int GetDefaultWs(InterlinearMapping mapping)
		{
			int ws;
			switch (mapping.Destination)
			{
				default:
					ws = m_cache.DefaultAnalWs;
					break;
				case InterlinDestination.Ignored:
					ws = 0;
					break;
				case InterlinDestination.Baseline:
				case InterlinDestination.Wordform:
					ws = m_cache.DefaultVernWs;
					break;
			}
			return ws;
		}

		/// <summary>
		/// Given the mapping and followed by information from the file calculate how many texts will result from an import.
		/// </summary>
		private static int CalculateTextCount(List<InterlinearMapping> mMappings, Dictionary<string, Dictionary<string, int>> dictionary)
		{
			var count = 0;
			var headers = new HashSet<string>();
			foreach (var interlinearMapping in mMappings.Where(interlinearMapping => interlinearMapping.Destination == InterlinDestination.Id
																					 || interlinearMapping.Destination == InterlinDestination.Source
																					 || interlinearMapping.Destination == InterlinDestination.Comment
																					 || interlinearMapping.Destination == InterlinDestination.Title
																					 || interlinearMapping.Destination == InterlinDestination.Abbreviation))
			{
				headers.Add(interlinearMapping.Marker);
			}
			// if no headers were mapped then only one text could result (and 0 would be counted)
			if (headers.Count == 0)
			{
				return 1;
			}
			//iterate through the data of markers and the counts of markers that follow them
			foreach (var markerAndFollowing in dictionary)
			{
				//if the marker is a header
				if (!headers.Contains(markerAndFollowing.Key))
				{
					continue;
				}
				//every time a header marker is followed by a non header it is the start of a text.
				//for every non header that follows a header marker add the occurence count to count.
				count += markerAndFollowing.Value.Where(followingMarker => !headers.Contains(followingMarker.Key)).Sum(followingMarker => followingMarker.Value);
			}
			return count;
		}

		private string GetWritingSystemName(string wsid)
		{
			return m_cache.WritingSystemFactory.get_Engine(wsid).ToString();
		}

		protected override bool ValidToGoForward()
		{
			return CurrentStepNumber == 1 ? ValidateReadyToImport() : base.ValidToGoForward();
		}

		// Return true if all is well to proceed with the import. Otherwise display a message box and return false.
		protected virtual bool ValidateReadyToImport()
		{
			var gotBaseline = false;
			foreach (var mapping in m_mappings.Where(mapping => mapping.Destination == InterlinDestination.Baseline))
			{
				gotBaseline = true;
			}
			if (!gotBaseline)
			{
				MessageBox.Show(this, ITextStrings.ksMustHaveBaseline, LanguageExplorerResources.ksError, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			return true;
		}

		protected void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.ShiftKey)
			{
				return;
			}
			secretShiftText.Visible = true;
			Refresh();
		}

		protected void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.ShiftKey)
			{
				return;
			}
			secretShiftText.Visible = false;
			Refresh();
		}

		protected override void OnFinishButton()
		{
			base.OnFinishButton();
			SaveSettings();
			if (string.IsNullOrEmpty(m_fileListBox.Text))
			{
				return;
			}
			using (var dlg = new ProgressDialogWithTask(this))
			{
				dlg.AllowCancel = false;
				dlg.Minimum = 0;
				// Allow 100 units of progress for each file for now. This allows for plenty of resolution for the LL importer
				dlg.Maximum = InputFiles.Count() * 100;

				try
				{
					dlg.RunTask(true, DoConversion);
				}
				catch (WorkerThreadException ex) // any exception on the worker thread is converted to this
				{
					// JohnT: I hate to just report and otherwise ignore all exceptions, but have not been able to find any doc of which ones,
					// if any, EncConverters may throw.
					System.Diagnostics.Debug.WriteLine("Error: " + ex.InnerException.Message);
					MessageBox.Show(this, string.Format(ITextStrings.ksSfmImportProblem, ex.InnerException.Message), ITextStrings.ksUnhandledError, MessageBoxButtons.OK, MessageBoxIcon.Error);
					DialogResult = DialogResult.Cancel;
					Close();
				}
			}
			m_propertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window).RefreshAllViews();
			if (m_firstNewText != null)
			{
				// try to select it.
				var recordList = m_propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LanguageExplorerConstants.InterlinearTexts);
				recordList?.JumpToRecord(m_firstNewText.ContentsOA.Hvo);
			}
		}
		IText m_firstNewText;
		private List<InterlinearMapping> m_oldMappings;

		/// <summary>
		/// Do the conversion. The signature of this method is required for use with ProgressDialogWithTask.RunTask,
		/// but the parameters and return result are not actually used.
		/// </summary>
		private object DoConversion(IThreadedProgress dlg, object[] parameters)
		{
			m_firstNewText = null;
			foreach (var path1 in InputFiles)
			{
				var path = path1.Trim();
				if (!File.Exists(path))
				{
					continue; // report?
				}
				var stage1 = GetSfmConverter().Convert(new ByteReader(path), m_mappings, m_cache.ServiceLocator.WritingSystemManager);
				// Skip actual import if SHIFT was held down.
				if (secretShiftText.Visible)
				{
					continue;
				}
				DoStage2Conversion(stage1, dlg);
			}
			return null;
		}

		protected virtual void DoStage2Conversion(byte[] stage1, IThreadedProgress dlg)
		{
			using (var stage2Input = new MemoryStream(stage1))
			{
				var stage2Converter = new LinguaLinksImport(m_cache, null, null);
				// Until we have a better idea, assume we're half done with the import when we've produced the intermediate.
				// Allocate 5 progress units to the ImportInterlinear, in case it can do better resolution.
				// Enhance JohnT: we could divide the progress up based on the lengths of the files,
				// and possibly converter.Convert could move the bar along based on how far through the file it is.
				// ImportInterlinear could do something similar. However, procesing a single file is so quick
				// that this very crude approximation is good enough.
				dlg.Position += 50;
				stage2Converter.ImportInterlinear(dlg, stage2Input, 50, ref m_firstNewText);
			}
		}

		internal virtual Sfm2FlexTextBase<InterlinearMapping> GetSfmConverter()
		{
			return new Sfm2FlexText();
		}

		private void SaveSettings()
		{
			var path = m_saveSettingsFileBox.Text.Trim();
			if (string.IsNullOrEmpty(path))
			{
				return;
			}
			var mappingsToSave = new List<InterlinearMapping>(m_mappings);
			// We will save our current mappings and any others from the file we loaded (may be useful if these
			// settings are later applied to another file).
			if (m_oldMappings != null)
			{
				var currentMarkers = new HashSet<string>(m_mappings.Select(map => map.Marker));
				mappingsToSave.AddRange(m_oldMappings.Where(mapping => !currentMarkers.Contains(mapping.Marker)));
			}
			try
			{
				var serializer = new XmlSerializer(mappingsToSave.GetType());
				using (var writer = new StreamWriter(path))
				{
					serializer.Serialize(writer, mappingsToSave);
					writer.Close();
				}
			}
			catch (IOException ex)
			{
				var msg = string.Format(ITextStrings.ksErrorWritingSettings, path, ex.Message);
				MessageBox.Show(this, msg, LanguageExplorerResources.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private List<InterlinearMapping> LoadSettings()
		{
			var path = m_loadSettingsFileBox.Text;
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				path = GetDefaultInputSettingsPath();
			}
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return new List<InterlinearMapping>();
			}
			try
			{
				var serializer = new XmlSerializer(typeof(List<InterlinearMapping>));
				using (var reader = new StreamReader(path))
				{
					var result = (List<InterlinearMapping>)serializer.Deserialize(reader);
					reader.Close();
					return result;
				}
			}
			catch (IOException ex)
			{
				var msg = string.Format(ITextStrings.ksErrorReadingSettings, path, ex.Message);
				MessageBox.Show(this, msg, LanguageExplorerResources.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return new List<InterlinearMapping>();
			}
			catch (InvalidOperationException ex)
			{
				var msg = string.Format(ITextStrings.ksErrorReadingSettings, path, ex.Message + ". " + (ex.InnerException?.Message ?? string.Empty));
				MessageBox.Show(this, msg, LanguageExplorerResources.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return new List<InterlinearMapping>();
			}
		}

		private string GetDefaultInputSettingsPath()
		{
			return Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Import", SfmImportSettingsFileName);
		}

		protected virtual string SfmImportSettingsFileName => "InterlinearSfmImport.map";

		internal static string GetDestinationName(InterlinDestination dest)
		{
			return ITextStrings.ResourceManager.GetString("ksFld" + dest) ?? dest.ToString();
		}

		private void m_modifyMappingButton_Click(object sender, EventArgs e)
		{
			if (m_mappingsList.SelectedIndices.Count == 0)
			{
				return;
			}
			using (var dlg = new SfmToTextsAndWordsMappingDlg())
			{
				var index = m_mappingsList.SelectedIndices[0];
				var mapping = m_mappings[index];
				dlg.SetupDlg(m_helpTopicProvider, m_propertyTable.GetValue<IApp>(LanguageExplorerConstants.App), m_cache, mapping, GetDestinationsFilter());
				dlg.ShowDialog(this);
				var item = m_mappingsList.Items[index];
				item.SubItems[2].Text = GetDestinationName(mapping.Destination);
				item.SubItems[3].Text = mapping.WritingSystem != null ? GetWritingSystemName(mapping.WritingSystem) : string.Empty;
				item.SubItems[4].Text = mapping.Converter;
			}
		}

		/// <summary>
		/// Provides the InterlinearDestinations that should be available in Modify Mapping dlg
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerable<InterlinDestination> GetDestinationsFilter()
		{
			return new[]
			{
				InterlinDestination.Abbreviation,
				InterlinDestination.Baseline,
				InterlinDestination.Comment,
				InterlinDestination.FreeTranslation,
				InterlinDestination.Id,
				InterlinDestination.Ignored,
				InterlinDestination.LiteralTranslation,
				InterlinDestination.Note,
				InterlinDestination.ParagraphBreak,
				InterlinDestination.Reference,
				InterlinDestination.Source,
				InterlinDestination.Title
			};
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(m_loadSettingsFileBox.Text) || CurrentStepNumber == 0)
			{
				return;
			}
			var result = MessageBox.Show(this, ITextStrings.ksAskSaveSettings, ITextStrings.ksSaveSettingsCaption, MessageBoxButtons.YesNoCancel);
			switch (result)
			{
				case DialogResult.Cancel:
					DialogResult = DialogResult.None; // stop it closing.
					return;
				case DialogResult.Yes:
					SaveSettings();
					break;
			}
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_helpTopicID);
		}

		private void m_browseSaveSettingsFileButon_Click(object sender, EventArgs e)
		{
			m_saveSettingsFileBox.Text = GetFile(m_saveSettingsFileBox.Text, FirstInputFile, new[] { FileFilterType.ImportMapping, FileFilterType.AllFiles }, false, ITextStrings.ksSelectMapFile, path => true);
		}

		private void m_mappingsList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			m_modifyMappingButton_Click(sender, new EventArgs());
		}

		private void m_useDefaultSettingsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			m_loadSettingsFileBox.Text = GetDefaultInputSettingsPath();
		}

		private sealed class SfmFileReaderEx : SfmFileReader
		{
			private Dictionary<string, FollowedByInfo> m_innerFollowedByInfo;

			internal Dictionary<string, Dictionary<string, int>> MyFollowedByInfo
			{
				get;
				private set;
			}

			public SfmFileReaderEx(string filename) : base(filename)
			{
				GetByteCounts = new int[256];
				CountBytes();
			}

			/// <summary>
			/// property that returns an array 0-255 with counts for each occurence
			/// </summary>
			private int[] GetByteCounts { get; }

			/// <summary>
			/// read the internal file data and save the byte counts
			/// </summary>
			private void CountBytes()
			{
				foreach (var b in m_FileData)
				{
					GetByteCounts[b]++; // bump the count at this byte index
				}
			}

			private static string BuildKey(string first, string last)
			{
				return $"{first}-{last}";
			}

			/// <summary>
			/// Called by the base class in it's constructor, this is the method that
			/// reads the contents and gathers the sfm information.
			/// </summary>
			protected override void Init()
			{
				MyFollowedByInfo = new Dictionary<string, Dictionary<string, int>>();
				m_innerFollowedByInfo = new Dictionary<string, FollowedByInfo>();
				try
				{
					string sfmLast = null;
					while (GetNextSfmMarkerAndData(out var sfm, out _, out _))
					{
						if (sfm.Length == 0)
						{
							continue; // no action if empty sfm - case where data before first marker
						}
						if (m_sfmUsage.ContainsKey(sfm))
						{
							var val = m_sfmUsage[sfm] + 1;
							m_sfmUsage[sfm] = val;
						}
						else
						{
							if (sfm.Length > m_longestSfmSize)
							{
								m_longestSfmSize = sfm.Length;
							}
							//// LT-1926 Ignore all markers that start with underscore (shoebox markers)
							m_sfmUsage.Add(sfm, 1);
							m_sfmOrder.Add(sfm);
						}
						// handle the marker and following counts
						if (sfmLast != null)
						{
							if (MyFollowedByInfo.TryGetValue(sfmLast, out var markerHash))
							{
								if (markerHash.TryGetValue(sfm, out var count))
								{
									count++;
									markerHash[sfm] = count;
								}
								else
								{
									markerHash[sfm] = 1;
								}
							}
							else
							{
								markerHash = new Dictionary<string, int>
								{
									[sfm] = 1
								};
								MyFollowedByInfo[sfmLast] = markerHash;
							}
							// new logic with List container
							var key = BuildKey(sfmLast, sfm);
							if (m_innerFollowedByInfo.TryGetValue(key, out var fbi))
							{
								fbi.IncCount();
								m_innerFollowedByInfo[key] = fbi;
							}
							else
							{
								m_innerFollowedByInfo[key] = new FollowedByInfo(sfmLast, sfm);
							}

						}
						sfmLast = sfm;
					}
				}
				catch
				{
					// just eat the exception sense the data members will be empty
				}
			}

			private struct FollowedByInfo
			{
				internal FollowedByInfo(string a, string b)
				{
					First = a;
					Last = b;
					Count = 1;
				}
				private int Count { get; set; }

				internal void IncCount()
				{
					Count++;
				}

				private string First { get; }

				private string Last { get; }

				public override string ToString()
				{
					return $"{First}-{Last}-{Count}";
				}
			}
		}
	}
}