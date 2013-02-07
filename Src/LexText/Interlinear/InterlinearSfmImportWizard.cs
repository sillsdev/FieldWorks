using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using Sfm2Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using SIL.Utils.FileDialog;
using XCore;

namespace SIL.FieldWorks.IText
{
	public partial class InterlinearSfmImportWizard : WizardDialog, IFwExtension
	{
//		private const string kSfmImportSettingsRegistryKeyName = "SFM import settings";
		protected FdoCache m_cache;
		private Mediator m_mediator;
		private IHelpTopicProvider m_helpTopicProvider;
		private List<InterlinearMapping> m_mappings = new List<InterlinearMapping>();
		// Maps from writing system name to most recently selected encoding converter for that WS.
		private Dictionary<string, string> m_preferredConverters = new Dictionary<string, string>();
		//Map of the information about which tags follow others, needed to count the number of resulting interlinear texts
		//after the users mapping has been applied.
		Dictionary<string, Dictionary<string, int>> followedBy = new Dictionary<string, Dictionary<string, int>>();
		private bool m_firstTimeInMappingsPane = true;

		public InterlinearSfmImportWizard()
		{
			InitializeComponent();
			tabSteps.KeyDown += OnKeyDown;
			tabSteps.KeyUp += OnKeyUp;
			LexImportWizard.EnsureWindows1252ConverterExists();
		}

		protected virtual void SetDialogTitle()
		{
			Text = String.Format(Text, ITextStrings.ksInterlinearTexts);
		}

		public void Init(FdoCache cache, Mediator mediator)
		{
			m_cache = cache;
			m_mediator = mediator;
			if (m_mediator != null)
				m_helpTopicProvider = m_mediator.HelpTopicProvider;
			//var settingsPath = FwRegistryHelper.FieldWorksRegistryKey.GetValue(kSfmImportSettingsRegistryKeyName) as string;
			//if (string.IsNullOrEmpty(settingsPath) || !File.Exists(settingsPath))
			//    settingsPath = GetDefaultInputSettingsPath();
			//m_loadSettingsFileBox.Text = settingsPath;
			SetDialogTitle();
		}

		private void m_browseInputFilesButton_Click(object sender, EventArgs e)
		{
			m_fileListBox.Text = GetFiles(m_fileListBox.Text);
			if (!string.IsNullOrEmpty(m_fileListBox.Text))
			{
				var input = InputFiles;
				if (input.Length > 0)
				{
					var settingsPath = GetDefaultOutputSettingsPath(input[0]);
					m_saveSettingsFileBox.Text = settingsPath;
				}
				if (input.Length == 1)
					MakeEndOfTextVisibleAndFocus(m_fileListBox);
				m_loadSettingsFileBox.Text = GetDefaultInputSettingsPath();
				foreach (var path in input)
				{
					var inputSettings = GetDefaultOutputSettingsPath(path);
					if (File.Exists(inputSettings))
					{
						m_loadSettingsFileBox.Text = inputSettings;
						break;
					}
				}
				MakeEndOfTextVisibleAndFocus(m_loadSettingsFileBox);
				m_browseLoadSettingsFileButon.Focus(); // a reasonable choice, and we've messed with focus making things visible
			}
		}

		/// <summary>
		/// Make sure that the end of the text in the given text box is visible.
		/// An (undesired) side effect is to focus the box and put the selection at the end of it.
		/// I cannot find any portable way to achieve the desired scrolling without doing this.
		/// </summary>
		/// <param name="textBox"></param>
		private void MakeEndOfTextVisibleAndFocus(TextBox textBox)
		{
			if (textBox.Text.Length == 0)
				return;
			// It would seem logical that we would not want the -1, so we would be asking for the position of the
			// imaginary character at the very end. However, that just always returns (0,0).
			Point endPosition = textBox.GetPositionFromCharIndex(textBox.Text.Length - 1);
			if (endPosition.X > textBox.Width)
			{
				textBox.Focus();
				textBox.Select(textBox.Text.Length, 0);
				textBox.ScrollToCaret();
			}
		}

		private string GetDefaultOutputSettingsPath(string input)
		{
			var pathWithoutExtension = input.Substring(0, input.Length - Path.GetExtension(input).Length);
			return Path.ChangeExtension(pathWithoutExtension + "-import-settings", ".map");
		}

		private string[] InputFiles
		{
			get { return m_fileListBox.Text.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries); }
		}

		string FirstInputFile
		{
			get { return InputFiles.FirstOrDefault(); }
		}

		private void m_browseLoadSettingsFileButon_Click(object sender, EventArgs e)
		{
			// Enahnce JohnT: possibly some validation of a mapping file?
			m_loadSettingsFileBox.Text = GetFile(m_loadSettingsFileBox.Text, FirstInputFile, new[] { FileFilterType.ImportMapping, FileFilterType.AllFiles }, true,
				ITextStrings.ksSelectMapFile, path => true);
		}

		private string GetFiles(string currentFiles)
		{
			using (var openFileDialog = new OpenFileDialogAdapter())
			{
				openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.InterlinearSfm,
					FileFilterType.AllFiles);
				openFileDialog.CheckFileExists = true;
				openFileDialog.Multiselect = true; // can import multiple files

				var files = currentFiles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				string dir = string.Empty;
				string initialFileName = string.Empty;
				openFileDialog.FileName = "";
				if (files.Length > 0)
				{
					var firstFilePath = files[0].Trim();
					// LT-6620 : putting in an invalid path was causing an exception in the openFileDialog.ShowDialog()
					// Now we make sure parts are valid before setting the values in the openfile dialog.
					try
					{
						dir = Path.GetDirectoryName(firstFilePath);
						if (File.Exists(firstFilePath))
							initialFileName = Path.GetFileName(firstFilePath);
					}
					catch
					{
					}
				}
				if (Directory.Exists(dir))
					openFileDialog.InitialDirectory = dir;
				// It doesn't seem to be possible to open the dialog with more than one file selected.
				// However there will often be only one so that's at least somewhat helpful.
				openFileDialog.FileName = initialFileName;
				openFileDialog.Title = ITextStrings.ksSelectInterlinFile;
				while (true) // loop until approved set of files or cancel
				{
					if (openFileDialog.ShowDialog() != DialogResult.OK)
						return currentFiles;
					var badFiles = new List<string>();
					foreach (var fileName in openFileDialog.FileNames)
					{
						if (!new Sfm2Xml.IsSfmFile(fileName).IsValid)
							badFiles.Add(fileName);
					}
					if (badFiles.Count > 0)
					{
						string msg = String.Format(ITextStrings.ksInvalidInterlinearFiles,
							string.Join(", ", badFiles.ToArray()));
						DialogResult dr = MessageBox.Show(this, msg,
							ITextStrings.ksPossibleInvalidFile,
							MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
						if (dr == DialogResult.Yes)
							return string.Join(", ", openFileDialog.FileNames);
						if (dr == DialogResult.No)
							continue; // loop and show dialog again...hopefully same files selected.
						break; // user must have chosen cancel, break out of loop
					}
					return string.Join(", ", openFileDialog.FileNames);
				}
				return currentFiles; // leave things unchanged.
			}
		}

		private string GetFile(string currentFile, string pathForInitialDirectory,
			FileFilterType[] types, bool checkFileExists, string title, Func<string, bool> isValidFile)
		{
			using (var openFileDialog = new OpenFileDialogAdapter())
			{
				openFileDialog.Filter = ResourceHelper.BuildFileFilter(types);
				openFileDialog.CheckFileExists = checkFileExists;
				openFileDialog.Multiselect = false;

				bool done = false;
				while (!done)
				{
					// LT-6620 : putting in an invalid path was causing an exception in the openFileDialog.ShowDialog()
					// Now we make sure parts are valid before setting the values in the openfile dialog.
					string dir = string.Empty;
					try
					{
						dir = Path.GetDirectoryName(pathForInitialDirectory);
					}
					catch
					{
					}
					if (Directory.Exists(dir))
						openFileDialog.InitialDirectory = dir;
					if (File.Exists(currentFile))
						openFileDialog.FileName = currentFile;
					else
						openFileDialog.FileName = "";

					openFileDialog.Title = title;
					if (openFileDialog.ShowDialog() == DialogResult.OK)
					{
						if (!(isValidFile(openFileDialog.FileName)))
						{
							string msg = String.Format(ITextStrings.ksInvalidFileAreYouSure,
								openFileDialog.FileName);
							DialogResult dr = MessageBox.Show(this, msg,
								ITextStrings.ksPossibleInvalidFile,
								MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
							if (dr == DialogResult.Yes)
								return openFileDialog.FileName;
							else if (dr == DialogResult.No)
									continue;
								else
									break;	// exit with current still
						}
						return openFileDialog.FileName;
					}
					else
						done = true;
				}
				return currentFile;
			}
		}

		protected override void OnNextButton()
		{
			if (CurrentStepNumber == 0)
			{
				// Populate m_mappingsList based on the selected files.
				var sfmcounts = new Dictionary<string, int>();
				var sfmOrder = new Dictionary<int, string>(); // key is 100000*fileNum + orderInFile, value is a marker
				int fileNum = 0;
				foreach (var pathName in InputFiles)
				{
					var reader = new SfmFileReaderEx(pathName);
					followedBy = reader.GetFollowedByInfo();
					foreach (string marker in reader.SfmInfo)
					{
						int oldVal;
						if (!sfmcounts.TryGetValue(marker, out oldVal))
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
					savedMappings[mapping.Marker] = mapping;
				m_mappings.Clear();
				var keys = new List<int>(sfmOrder.Keys);
				keys.Sort();
				foreach (var key in keys)
				{
					var marker = sfmOrder[key];
					InterlinearMapping mapping;
					if (savedMappings.TryGetValue(marker, out mapping))
					{
						mapping = new InterlinearMapping(mapping);
						if (string.IsNullOrEmpty(mapping.WritingSystem))
						{
							var ws = GetDefaultWs(mapping);
							if (ws != 0)
								mapping.WritingSystem = m_cache.WritingSystemFactory.GetStrFromWs(ws);
						}
						else if (mapping.WritingSystem == "{vern}")
							mapping.WritingSystem = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultVernWs);
					}
					else
						mapping = new InterlinearMapping() {Marker = marker};
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
					m_mappingsList.SelectedIndices.Add(0);
				m_mappingsList.ResumeLayout();
			}
			else if(CurrentStepNumber == 1)
			{
				ICollection<IWritingSystem> currentVernacWSs = m_cache.LanguageProject.VernacularWritingSystems;
				ICollection<IWritingSystem> currentAnalysWSs = m_cache.LanguageProject.AnalysisWritingSystems;
				var vernToAdd = new ArrayList();
				var analysToAdd = new ArrayList();
				int textCount = CalculateTextCount(m_mappings, followedBy);
				foreach(var mapping in m_mappings)
				{
					if (mapping.Destination == InterlinDestination.Ignored)
						continue; // may well have no WS, in any case, we don't care whether it's in our list.
					bool creationCancelled = false;
					var ws = (IWritingSystem)m_cache.WritingSystemFactory.get_Engine(mapping.WritingSystem);
					if (mapping.Destination == InterlinDestination.Baseline || mapping.Destination == InterlinDestination.Wordform)
					{
						if(!currentVernacWSs.Contains(ws) && !vernToAdd.Contains(ws))
						{
							//Show creation dialog for Vernacular
							var result = MessageBox.Show(this,
														 String.Format(ITextStrings.ksImportSFMInterlinNewVernac, ws),
														 String.Format(ITextStrings.ksImportSFMInterlinNewWSTitle, ws), MessageBoxButtons.YesNo);
							if(result == DialogResult.Yes)
							{
								vernToAdd.Add(ws);
							}
							else //if they bail out we won't add any writing systems, they might change them all
							{
								return;
							}
						}
					}
					else
					{
						if(!currentAnalysWSs.Contains(ws) && !analysToAdd.Contains(ws))
						{
							var result = MessageBox.Show(this,
														 String.Format(ITextStrings.ksImportSFMInterlinNewAnalysis, ws),
														 String.Format(ITextStrings.ksImportSFMInterlinNewWSTitle, ws), MessageBoxButtons.YesNo);
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
				}
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() => //Add all the collected new languages into the project in their proper section.
					{
						foreach (IWritingSystem analysLang in analysToAdd)
						{
							m_cache.LanguageProject.AddToCurrentAnalysisWritingSystems(analysLang);
						}
						foreach (IWritingSystem vernLang in vernToAdd)
						{
							m_cache.LanguageProject.AddToCurrentVernacularWritingSystems(vernLang);
						}
					});
				if(textCount > 1)
				{
					numberOfTextsLabel.Text = String.Format(ITextStrings.ksImportSFMInterlinTextCount, textCount);
				}
				else
				{
					numberOfTextsLabel.Text = String.Empty;
				}
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
		/// <param name="mMappings"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		private int CalculateTextCount(List<InterlinearMapping> mMappings, Dictionary<string, Dictionary<string, int>> dictionary)
		{
			int count = 0;
			Set<string> headers = new Set<string>();
			foreach (InterlinearMapping interlinearMapping in mMappings)
			{
				if(interlinearMapping.Destination == InterlinDestination.Id ||
				   interlinearMapping.Destination == InterlinDestination.Source ||
				   interlinearMapping.Destination == InterlinDestination.Comment ||
				   interlinearMapping.Destination == InterlinDestination.Title ||
				   interlinearMapping.Destination == InterlinDestination.Abbreviation)
				{
					headers.Add(interlinearMapping.Marker);
				}
			}
			// if no headers were mapped then only one text could result (and 0 would be counted)
			if (headers.Count == 0)
				return 1;

			//iterate through the data of markers and the counts of markers that follow them
			foreach (KeyValuePair<string, Dictionary<string, int>> markerAndFollowing in dictionary)
			{
				//if the marker is a header
				if(headers.Contains(markerAndFollowing.Key))
				{
					//every time a header marker is followed by a non header it is the start of a text.

					//for every non header that follows a header marker add the occurence count to count.
					foreach (var followingMarker in markerAndFollowing.Value)
					{
						if (!headers.Contains(followingMarker.Key))
						{
							count += followingMarker.Value;
						}
					}
				}
			}
			return count;
		}

		private string GetWritingSystemName(string wsid)
		{
			var engine = m_cache.WritingSystemFactory.get_Engine(wsid);
			return engine.ToString();
		}

		protected override bool ValidToGoForward()
		{
			if (CurrentStepNumber == 1)
				return ValidateReadyToImport();
			return base.ValidToGoForward();
		}

		// Return true if all is well to proceed with the import. Otherwise display a message box and return false.
		protected virtual bool ValidateReadyToImport()
		{
			bool gotBaseline = false;
			foreach (var mapping in m_mappings)
			{
				if (mapping.Destination == InterlinDestination.Baseline)
					gotBaseline = true;
			}
			if (!gotBaseline)
			{
				MessageBox.Show(this, ITextStrings.ksMustHaveBaseline, ITextStrings.ksError, MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return false;
			}
			return true;
		}

		protected void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.ShiftKey)
			{
				secretShiftText.Visible = true;
				Refresh();
			}
		}

		protected void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.ShiftKey)
			{
				secretShiftText.Visible = false;
				Refresh();
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="RecordClerk.FindClerk() returns a reference")]
		protected override void OnFinishButton()
		{
			base.OnFinishButton();
			SaveSettings();
			if (string.IsNullOrEmpty(m_fileListBox.Text))
				return;
			using (var dlg = new ProgressDialogWithTask(this, m_cache.ThreadHelper))
			{
				dlg.AllowCancel = false;
				dlg.Minimum = 0;
				// Allow 100 units of progress for each file for now. This allows for plenty of resolution for the LL importer
				dlg.Maximum = m_fileListBox.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Count() * 100;

				try
				{
					dlg.RunTask(true, DoConversion);
				}
				catch (WorkerThreadException ex) // any exception on the worker thread is converted to this
				{
					// JohnT: I hate to just report and otherwise ignore all exceptions, but have not been able to find any doc of which ones,
					// if any, EncConverters may throw.
					System.Diagnostics.Debug.WriteLine("Error: " + ex.InnerException.Message);
					MessageBox.Show(this, String.Format(ITextStrings.ksSfmImportProblem, ex.InnerException.Message),
						ITextStrings.ksUnhandledError,
						MessageBoxButtons.OK, MessageBoxIcon.Error);
					DialogResult = DialogResult.Cancel;
					Close();
				}
			}
			m_mediator.SendMessage("MasterRefresh", ActiveForm);
			if (m_firstNewText != null)
			{
				// try to select it.
				var clerk = RecordClerk.FindClerk(m_mediator, "interlinearTexts");
				if (clerk != null)
					clerk.JumpToRecord(m_firstNewText.ContentsOA.Hvo);
			}
		}
		FDO.IText m_firstNewText;
		private List<InterlinearMapping> m_oldMappings;

		/// <summary>
		/// Do the conversion. The signature of this method is required for use with ProgressDialogWithTask.RunTask,
		/// but the parameters and return result are not actually used.
		/// </summary>
		private object DoConversion(IThreadedProgress dlg, object[] parameters)
		{
			m_firstNewText = null;
			foreach (var path1 in m_fileListBox.Text.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries))
			{
				var path = path1.Trim();
				if (!File.Exists(path))
					continue; // report?
				var input = new ByteReader(path);
				var converterStage1 = GetSfmConverter();
				var stage1 = converterStage1.Convert(input, m_mappings, m_cache.WritingSystemFactory);
				// Skip actual import if SHIFT was held down.
				if (secretShiftText.Visible == true)
					continue;
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

		protected virtual Sfm2FlexTextBase<InterlinearMapping> GetSfmConverter()
		{
			return new Sfm2FlexText();
		}

		private void SaveSettings()
		{
			var path = m_saveSettingsFileBox.Text.Trim();
			//if (!string.IsNullOrEmpty(path))
			//    FwRegistryHelper.FieldWorksRegistryKey.SetValue(kSfmImportSettingsRegistryKeyName, path);
			if (string.IsNullOrEmpty(path))
				return;
			var mappingsToSave = new List<InterlinearMapping>(m_mappings);
			// We will save our current mappings and any others from the file we loaded (may be useful if these
			// settings are later applied to another file).
			if (m_oldMappings != null)
			{
				var currentMarkers = new HashSet<string>(from map in m_mappings select map.Marker);
				foreach (var mapping in m_oldMappings)
				{
					if (!currentMarkers.Contains(mapping.Marker))
						mappingsToSave.Add(mapping);
				}
			}
			try
			{
				XmlSerializer serializer = new XmlSerializer(mappingsToSave.GetType());
				using (var writer = new StreamWriter(path))
				{
					serializer.Serialize(writer, mappingsToSave);
					writer.Close();
				}
			}
			catch (IOException ex)
			{
				var msg = string.Format(ITextStrings.ksErrorWritingSettings, path, ex.Message);
				MessageBox.Show(this, msg, ITextStrings.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
		}

		private List<InterlinearMapping> LoadSettings()
		{
			var path = m_loadSettingsFileBox.Text;
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				path = GetDefaultInputSettingsPath();
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return new List<InterlinearMapping>();
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(List<InterlinearMapping>));
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
				MessageBox.Show(this, msg, ITextStrings.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return new List<InterlinearMapping>();
			}
			catch (InvalidOperationException ex)
			{
				var msg = string.Format(ITextStrings.ksErrorReadingSettings, path, ex.Message + ". " +
					(ex.InnerException != null ? ex.InnerException.Message : ""));
				MessageBox.Show(this, msg, ITextStrings.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return new List<InterlinearMapping>();
			}
		}

		private string GetDefaultInputSettingsPath()
		{
			string path;
			string sRootDir = DirectoryFinder.FWCodeDirectory;
			string sTransformDir;
			if (!sRootDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
				sRootDir += Path.DirectorySeparatorChar;
			sTransformDir = sRootDir + String.Format("Language Explorer{0}Import{0}", Path.DirectorySeparatorChar);
			path = Path.Combine(sTransformDir, SfmImportSettingsFileName);
			return path;
		}

		protected virtual string SfmImportSettingsFileName
		{
			get
			{
				return "InterlinearSfmImport.map";
			}
		}

		internal static string GetDestinationName(InterlinDestination dest)
		{
			var stid = "ksFld" + dest;
			return ITextStrings.ResourceManager.GetString(stid) ?? dest.ToString();
		}

		private void m_modifyMappingButton_Click(object sender, EventArgs e)
		{
			if (m_mappingsList.SelectedIndices.Count == 0)
				return;
			using (var dlg = new SfmToTextsAndWordsMappingDlg())
			{
				var index = m_mappingsList.SelectedIndices[0];
				var mapping = m_mappings[index];
				var destinationsFilter = GetDestinationsFilter();
				dlg.SetupDlg(m_helpTopicProvider, (IApp)m_mediator.PropertyTable.GetValue("App"), m_cache,
					mapping, destinationsFilter);
				dlg.ShowDialog(this);
				var item = m_mappingsList.Items[index];
				item.SubItems[2].Text = GetDestinationName(mapping.Destination);
				item.SubItems[3].Text = mapping.WritingSystem != null ? GetWritingSystemName(mapping.WritingSystem) : "";
				item.SubItems[4].Text = mapping.Converter;
			}
		}

		/// <summary>
		/// Provides the InterlinearDestinations that should be available in Modify Mapping dlg
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerable<InterlinDestination> GetDestinationsFilter()
		{
			return new[] {InterlinDestination.Abbreviation, InterlinDestination.Baseline, InterlinDestination.Comment,
						  InterlinDestination.FreeTranslation,InterlinDestination.Id, InterlinDestination.Ignored, InterlinDestination.LiteralTranslation,
						  InterlinDestination.Note, InterlinDestination.ParagraphBreak, InterlinDestination.Reference, InterlinDestination.Source, InterlinDestination.Title };
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(m_loadSettingsFileBox.Text) || CurrentStepNumber == 0)
				return;
			var result = MessageBox.Show(this, ITextStrings.ksAskSaveSettings, ITextStrings.ksSaveSettingsCaption, MessageBoxButtons.YesNoCancel);
			if (result == DialogResult.Cancel)
			{
				DialogResult = DialogResult.None; // stop it closing.
				return;
			}
			if (result == DialogResult.Yes)
				SaveSettings();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, m_helpTopicID);
		}

		private void m_browseSaveSettingsFileButon_Click(object sender, EventArgs e)
		{
			m_saveSettingsFileBox.Text = GetFile(m_saveSettingsFileBox.Text, FirstInputFile, new[] { FileFilterType.ImportMapping, FileFilterType.AllFiles }, false,
				ITextStrings.ksSelectMapFile, path => true);

		}

		private void m_mappingsList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			m_modifyMappingButton_Click(sender, new EventArgs());
		}

		private void m_useDefaultSettingsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			m_loadSettingsFileBox.Text = GetDefaultInputSettingsPath();

		}
	}
}
