// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using LanguageExplorer.SfmToXml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.LCModel;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.FieldWorks.Resources;
using SilEncConverters40;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace LanguageExplorer.Controls.LexText
{
	internal sealed class LexImportWizard : WizardDialog, IFwExtension, IImportForm
	{
		private bool m_FeasabilityReportGenerated;  // has to run before import
		private LcmCache m_cache;
		private IApp m_app;
		private IVwStylesheet m_stylesheet;
		private bool m_formHasLoaded = false;   // so we don't process text changed msgs
		private int m_origWidth;    // ref value for dlg width
		private int m_origHeight;   // ref value for dlg height
		private bool m_origSet;     // this is set to false in the default constructor
		private CRC m_crcObj;   // CRC Object to use for telling if the input file has changed
		private DateTime m_lastDateTime;    // used to keep track of the last write date on the data file
		private uint m_crcOfInputFile;      // the computed crc of the data file
		private string m_processedInputFile;    // the name of the file last processed
		private string m_processedMapFile;  // name of the map file last processed
		private bool m_dirtyInputFile;  // dirty flag for the database (input) file
		private bool m_dirtyMapFile;    // dirty flag for the settings (map) file
		private bool m_isPhaseInputFile;    // true if the input file is a previous XML phase [1,2,3,4] output file
		public ILexImportFields m_LexFields;    // object that contains all the lex fields
		public ILexImportFields m_CustomFields; // object that contains all the currently defined custom fields
		private MarkerPresenter m_MappingMgr;
		private Label labelStep5Description;
		private Label lblStep5KeyMarkers;
		private TreeView tvBeginMarkers;    // object that has the data for the marker pg
		private bool m_DirtyStep5 = true;   // if true requires the display to be redrawn
		private bool m_fCanceling;  // used to tell if the cancel processing is in action
		private bool m_dirtySenseLastSave = true;   // used to tell if the save question is needed
		private string m_sTempDir;
		private string m_sImportFields;
		private string m_sMDFImportMap;
		private int m_cEntries;
		private string m_sPhase1Output;

		/// These strings contain the reserved names for the outputfile names at different phases.
		/// If these names are used for the dictionary input file then pick up the import process
		/// with these files.
		private Label lblFinishWOImport;
		private Button btnQuickFinish;
		private ColumnHeader columnHeader6;
		private Label label1;
		private Label lblGenerateReportMsg;
		private GroupBox gbGenerateReport;
		private Button btnSaveMapFile;
		private ListView listViewCharMappings;
		private Label label2;
		private Label label4;
		private ColumnHeader columnHeaderCM1;
		private ColumnHeader columnHeaderCM2;
		private ColumnHeader columnHeaderCM3;
		private Button btnAddCharMapping;
		private Button btnModifyCharMapping;
		private Button btnDeleteCharMapping;
		private ColumnHeader columnHeaderCM4;
		private ImageList imageList1;
		private OpenFileDialogAdapter openFileDialog;
		private CheckBox m_chkCreateMissingLinks;
		private const string kOptionKeyMissingLinkCheckbox = "chkCreateMissingLinks";
		private static LexImportWizard m_wizard;

		/// <summary>
		/// public static method to allow other objects in this namespace to get
		/// a copy of the current wizard.
		/// </summary>
		public static LexImportWizard Wizard()
		{
			return m_wizard;
		}

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		internal IPublisher Publisher { get; private set; }

		#region Dialog controls
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.TabPage tabPage6;
		private System.Windows.Forms.TabPage tabPage5;
		private System.Windows.Forms.TabPage tabPage7;
		private System.Windows.Forms.TabPage tabPage8;
		private System.Windows.Forms.ColumnHeader LangcolumnHeader1;
		private System.Windows.Forms.ColumnHeader LangcolumnHeader2;
		private System.Windows.Forms.ColumnHeader LangcolumnHeader3;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.ColumnHeader columnHeader4;
		private System.Windows.Forms.Label lblOverview;
		private System.Windows.Forms.Label lblOverviewInstructions;
		private System.Windows.Forms.Label lblBackupInstructions;
		private System.Windows.Forms.Label lblBackup;
		private System.Windows.Forms.Button btnBackup;
		private System.Windows.Forms.Label lblDatabaseInstructions;
		private System.Windows.Forms.Label lblDatabase;
		private System.Windows.Forms.Label lblSettingsInstructions;
		private System.Windows.Forms.Label lblSettings;
		private System.Windows.Forms.Label lblSaveAsInstructions;
		private System.Windows.Forms.Label lblSaveAs;
		private System.Windows.Forms.TextBox m_DatabaseFileName;
		private System.Windows.Forms.Button btnDatabaseBrowse;
		private System.Windows.Forms.Button btnSettingsBrowse;
		private System.Windows.Forms.Button btnSaveAsBrowse;
		private System.Windows.Forms.TextBox m_SaveAsFileName;
		private System.Windows.Forms.Label lblMappingLanguages;
		private System.Windows.Forms.Label lblMappingLanguagesInstructions;
		private System.Windows.Forms.ListView listViewMappingLanguages;
		private System.Windows.Forms.Button btnAddMappingLanguage;
		private System.Windows.Forms.Button btnModifyMappingLanguage;
		private System.Windows.Forms.Label lblContentMappings;
		private System.Windows.Forms.Label lblContentInstructions1;
		private System.Windows.Forms.Label lblContentInstructions2;
		private System.Windows.Forms.ListView listViewContentMapping;
		private System.Windows.Forms.Button btnModifyContentMapping;
		private FwOverrideComboBox m_SettingsFileName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label FeasabilityCheckInstructions;
		private System.Windows.Forms.Button btnGenerateReport;
		private System.Windows.Forms.Label lblReadyToImportInstructions;
		private System.Windows.Forms.Label lblReadyToImport;
		private System.Windows.Forms.CheckBox m_DisplayImportReport;
		private System.Windows.Forms.Label lblTotalMarkers;
		private System.Windows.Forms.Label lblFile;
		private System.Windows.Forms.Label lblSettingsTag;
		private System.Windows.Forms.ColumnHeader columnHeader5;
		private System.ComponentModel.IContainer components = null;
		#endregion

		#region Constructor and init routines
		/// <summary>
		/// Create the Wizard and require an LcmCache object.
		/// </summary>
		internal LexImportWizard()
		{
			InitializeComponent();
			openFileDialog = new OpenFileDialogAdapter();
			m_crcObj = new CRC();   // CRC Object to use for telling if the input file has changed
			m_lastDateTime = DateTime.MinValue;
			m_crcOfInputFile = 1;
			// Adjust the width of the steps panel if it's too narrow to show what it's
			// supposed to show.  See FWNX-514.  (This should be useful for localizations
			// as well as cross-platform work.)
			var panelwidth = StepNames.Select(name => ComputeDisplayWidth(name, StepTextFont)).Concat(new[] { StepPanelWidth }).Max();
			var delta = panelwidth - StepPanelWidth;
			if (delta > 0)
			{
				StepPanelWidth = panelwidth;
			}
		}

		private int ComputeDisplayWidth(string label, Font font)
		{
			using (var g = CreateGraphics())
			{
				var size = g.MeasureString(label, font, 1000);  // expected values are < 100
				var width = (int)Math.Ceiling(size.Width);
				return width + kdxpStepSquareWidth + 3 * kdxpStepListSpacing;   // add fudge factor for squares -- see WizardDialog.cs.
			}
		}

		/// <summary>
		/// From IFwExtension
		/// </summary>
		void IFwExtension.Init(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher)
		{
			m_wizard = this;
			m_cache = cache;
			PropertyTable = propertyTable;
			if (propertyTable != null)
			{
				m_app = propertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				m_stylesheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
			}
			Publisher = publisher;
			m_dirtyInputFile = true;
			m_dirtyMapFile = true;
			m_processedInputFile = m_processedMapFile = string.Empty;   // no files processed yet
			m_isPhaseInputFile = false;

			InitOutputFiles();
			SetDatabaseNameIntoLabel();

			// read in the Lex Import Fields
			m_LexFields = new LexImportFields();
			m_LexFields.ReadLexImportFields(m_sImportFields);

			// now read in any custom fields
			bool customFieldsChanged;
			m_CustomFields = ReadCustomFieldsFromDB(out customFieldsChanged);   // compare with map file before showing the UI and before the Import

			// set up default button states
			NextButtonEnabled = true;
			AcceptButton = null;
			btnModifyMappingLanguage.Enabled = false;
			btnModifyContentMapping.Enabled = false;
			m_chkCreateMissingLinks.Checked = false;
			string dictFileToImport;
			m_SettingsFileName.Items.Clear();
			m_SettingsFileName.Items.Add(m_sMDFImportMap);

			if (GetLastImportFile(out dictFileToImport))
			{
				FindFilesForDatabaseFile(dictFileToImport);
				m_DatabaseFileName.Text = dictFileToImport;

				HandleDBFileNameChanges();
			}
			else
			{
				m_SaveAsFileName.Text = string.Empty;   // empty if not found already
			}
			AllowQuickFinishButton();   // show it if it's valid

			// Copied from the previous LexImport dlg constructor (ImportLexicon.cs)
			// Ensure that we have the default encoding converter (to/from MS Windows Code Page
			// for Western European languages)
			EnsureWindows1252ConverterExists();

			ShowSaveButtonOrNot();
		}

		internal static void EnsureWindows1252ConverterExists()
		{
			var encConv = new EncConverters();
			var de = encConv.GetEnumerator();
			// REVIEW: SHOULD THIS NAME BE LOCALIZED?
			const string sEncConvName = "Windows1252<>Unicode";
			var fMustCreateEncCnv = true;
			while (de.MoveNext())
			{
				if ((string)de.Key != null && (string)de.Key == sEncConvName)
				{
					fMustCreateEncCnv = false;
					break;
				}
			}
			if (fMustCreateEncCnv)
			{
				try
				{
					encConv.AddConversionMap(sEncConvName, "1252", ECInterfaces.ConvType.Legacy_to_from_Unicode, "cp", "", "", ECInterfaces.ProcessTypeFlags.CodePageConversion);
				}
				catch (ECException exception)
				{
					MessageBox.Show(exception.Message, LexTextControls.ksConvMapError, MessageBoxButtons.OK);
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		public IPropertyTable PropertyTable { get; private set; }

		private void LexImportWizard_Load(object sender, EventArgs e)
		{
			m_formHasLoaded = true;
			ShowFinishLabel();
			m_DatabaseFileName.AppendText(string.Empty);
		}
		private void LexImportWizard_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (DialogResult != DialogResult.OK)    // only simulate a cancel button if not finished normally
			{
				OnCancelButton();
			}
		}
		#endregion

		/// Step 2 handles the selection of the database file and the settings
		/// and saveas file names.  Also interacts with the persistance part to
		/// retrieve previous settings for selected database file names.
		#region Step 2 event handlers and routines
		private enum OFType { Database, Settings, SaveAs }; // openfile type

		private void btnDatabaseBrowse_Click(object sender, System.EventArgs e)
		{
			m_DatabaseFileName.Text = GetFile(OFType.Database, m_DatabaseFileName.Text);
		}

		private void btnSettingsBrowse_Click(object sender, System.EventArgs e)
		{
			m_SettingsFileName.Text = GetFile(OFType.Settings, m_SettingsFileName.Text);
		}

		private void btnSaveAsBrowse_Click(object sender, System.EventArgs e)
		{
			m_SaveAsFileName.Text = GetFile(OFType.SaveAs, m_SaveAsFileName.Text);
		}

		private string GetFile(OFType fileType, string currentFile)
		{
			openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ImportMapping, FileFilterType.XML, FileFilterType.AllShoeboxDictionaryDatabases, FileFilterType.AllFiles);
			openFileDialog.FilterIndex = (fileType == OFType.Settings) ? 1 : (fileType == OFType.Database) ? 3 : 4;
			// only require the file to exist if its Database or Settings
			openFileDialog.CheckFileExists = true;
			if (fileType == OFType.SaveAs)
			{
				openFileDialog.CheckFileExists = false;
			}

			openFileDialog.Multiselect = false;

			var done = false;
			while (!done)
			{
				// LT-6620 : putting in an invalid path was causing an exception in the openFileDialog.ShowDialog()
				// Now we make sure parts are valid before setting the values in the openfile dialog.
				var dir = string.Empty;

				if (!string.IsNullOrEmpty(currentFile) && FileUtils.IsFilePathValid(currentFile))
				{
					dir = Path.GetDirectoryName(currentFile);
				}

				if (Directory.Exists(dir))
				{
					openFileDialog.InitialDirectory = dir;
				}
				// if we don't set it to something, it remembers the last file it saw. This can be
				// a very poor default if we just opened a valuable data file and are now choosing
				// a place to save settings (LT-8126)
				if (File.Exists(currentFile) || (fileType == OFType.SaveAs && Directory.Exists(dir)))
				{
					openFileDialog.FileName = currentFile;
				}
				else
				{
					openFileDialog.FileName = string.Empty;
				}

				openFileDialog.Title = string.Format(LexTextControls.ksSelectXFile, fileType);
				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					bool isValid;
					string text;

					// Before doing the 'fileType' based tests, make sure it's not a PhaseX file
					if (GetDictionaryFileAsPhaseFileNumber(openFileDialog.FileName) > 0)
					{
						isValid = true;     // trusting that phaseX files properly named are valid
						text = LexTextControls.ksPhaseFile;
					}
					else switch (fileType)
						{
							case OFType.Database:
								text = LexTextControls.ksStandardFormat;
								isValid = new IsSfmFile(openFileDialog.FileName).IsValid;
								break;
							case OFType.SaveAs:
								text = LexTextControls.ksXmlSettings;
								isValid = true;     // no requirements sense the file will be overridden
								break;
							default:
								text = LexTextControls.ksXmlSettings;
								isValid = MarkerPresenter.IsValidMapFile(openFileDialog.FileName);
								break;
						}

					if (!isValid)
					{
						var msg = string.Format(LexTextControls.ksSelectedFileXInvalidY, openFileDialog.FileName, text, Environment.NewLine);
						var dr = MessageBox.Show(this, msg, LexTextControls.ksPossibleInvalidFile, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
						switch (dr)
						{
							case DialogResult.Yes:
								return openFileDialog.FileName;
							case DialogResult.No:
								continue;
						}
						break;  // exit with current still
					}
					return openFileDialog.FileName;
				}
				done = true;
			}
			return currentFile;
		}

		private void m_SettingsFileName_TextChanged(object sender, System.EventArgs e)
		{
			if (m_formHasLoaded)
			{
				m_dirtySenseLastSave = true;
				m_dirtyMapFile = m_processedMapFile != m_SettingsFileName.Text;
				AllowQuickFinishButton();
			}
		}

		private void m_DatabaseFileName_TextChanged(object sender, System.EventArgs e)
		{
			if (m_formHasLoaded)
			{
				HandleDBFileNameChanges();
				AllowQuickFinishButton();
			}
		}

		private bool CheckForPhaseFileName()
		{
			m_isPhaseInputFile = GetDictionaryFileAsPhaseFileNumber() > 0;
			return m_isPhaseInputFile;
		}

		private void HandleDBFileNameChanges()
		{
			m_dirtySenseLastSave = true;
			if (m_DatabaseFileName.Text != string.Empty)
			{
				// if it's a phase file, disable other controls
				var enableOtherFiles = !CheckForPhaseFileName();

				m_SettingsFileName.Enabled = enableOtherFiles;
				m_SaveAsFileName.Enabled = enableOtherFiles;

				string settings, saveAs;

				// clear the values for the settings and save as files
				if (m_isPhaseInputFile)
				{
					m_SettingsFileName.Text = string.Empty;
					m_SaveAsFileName.Text = string.Empty;
					m_SettingsFileName.Items.Clear();
				}
				else if (FindDBSettingsSaved(m_DatabaseFileName.Text, out settings, out saveAs))
				{
					m_SettingsFileName.Text = settings;
					m_SaveAsFileName.Text = saveAs;

					// now update the Settings combo box to have the correct items and correct one selected.
					m_SettingsFileName.Items.Clear();
					var pos = m_SettingsFileName.Items.Add(m_sMDFImportMap);
					if (m_SaveAsFileName.Text != "" && m_sMDFImportMap != m_SaveAsFileName.Text)
					{
						pos = m_SettingsFileName.Items.Add(m_SaveAsFileName.Text);
					}

					m_SettingsFileName.SelectedIndex = pos;
				}
				else
				{
					SetDefaultSettings();
				}
			}
			m_dirtyInputFile = m_processedInputFile != m_DatabaseFileName.Text;

			if (CurrentStepNumber == 1)
			{
				NextButtonEnabled = EnableNextButton();
			}
		}

		private void SetDefaultSettings()
		{
			// reset them to m_sMDFImportMap and dbfile + .map
			m_SettingsFileName.Items.Clear();
			m_SettingsFileName.Items.Add(m_sMDFImportMap);
			m_SettingsFileName.SelectedIndex = 0;
			m_SaveAsFileName.Text = RemoveTheFileExtension(m_DatabaseFileName.Text) + "-import-settings.map";
			if (File.Exists(m_SaveAsFileName.Text))
			{
				var pos = m_SettingsFileName.Items.Add(m_SaveAsFileName.Text);
				m_SettingsFileName.SelectedIndex = pos;
			}
		}

		private static string RemoveTheFileExtension(string fileName)
		{
			var lastPos = fileName.LastIndexOf('.');
			return lastPos == -1 ? fileName : fileName.Substring(0, lastPos);
		}

		#endregion

		/// Step 3 handles the Mapping Languages.  Getting them, displaying them
		/// and the ability to change them (add, modify).
		#region Step 3 event handlers and routines

		private void ReadLanguageInfoFromMapFile()
		{
			var lc = new LangConverter();
			var langs = lc.Languages(m_SettingsFileName.Text);
			listViewMappingLanguages.Items.Clear();
			if (langs == null)
			{
				return;
			}

			listViewMappingLanguages.BeginUpdate();

			foreach (DictionaryEntry languageEntry in langs)
			{
				var lang = languageEntry.Value as SfmToXml.ClsLanguage;
				var encodingconverter = lang.EncCvtrMap;
				var langkey = lang.KEY;
				var xmlLang = lang.XmlLang;
				// now put the lang info into the language list view
				var fwName = ConvertNameFromIdtoFW(xmlLang);
				if (fwName == null)
				{
					var ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
					if (xmlLang == "Vernacular" && ws != null)
					{
						fwName = ws.DisplayLabel;
						xmlLang = lang.XmlLang = ws.Id;
						encodingconverter = ws.LegacyMapping;
					}
					else
					{
						fwName = SfmToXmlServices.Ignore;
					}
				}
				// make sure if there is no encoding converter, show it as already in unicode
				if (string.IsNullOrEmpty(encodingconverter))
				{
					encodingconverter = SfmToXmlServices.AlreadyInUnicode;
				}

				AddLanguage(langkey, fwName, encodingconverter, xmlLang);
			}
			listViewMappingLanguages.EndUpdate();

			// Select the first one by default (LT-2574)
			if (listViewMappingLanguages.Items.Count > 0)
			{
				listViewMappingLanguages.Items[0].Selected = true;
			}
		}

		private void SetDatabaseNameIntoLabel()
		{
			var resources = new System.Resources.ResourceManager(typeof(LexImportWizard));
			var lblText = resources.GetString("lblMappingLanguagesInstructions.Text");  //dbLabelText");
			lblMappingLanguagesInstructions.Text = string.Format(lblText, m_cache.ProjectId.Name);
		}

		// TODO need way to get language info out of UI for use in the marker step of wizard
		private Hashtable GetUILanguages()
		{
			var langs = new Hashtable();
			foreach (ListViewItem lvItem in listViewMappingLanguages.Items)
			{
				var langInfo = (LanguageInfoUI)lvItem.Tag;
				langs.Add(langInfo.Key, langInfo);
			}
			return langs;
		}

		private string GetCustomFieldHelp(FieldDescription fd)
		{
			// "About"											-> Field/@uiname			-> line
			// "When to use"									-> Field/Help/Usage			-> line
			// "Additional Settings"							-> Field/Help/Settings		-> line
			// "Interprets character mapping"					-> Field/Help/Mapping		-> line
			// "Allows multiple SFM fields"						-> Field/Help/Appends		-> line
			// "Uses list item"									-> Field/Help/List			-> line
			// "Allows an equivalent field for each langauge"	-> Field/Help/Multilingual	-> line
			// "Example(s)"										-> Field/Help/Examples		-> list
			// "Related Fields"									-> Field/Help/RelatedFields	-> line
			// "Limitations"									-> Field/Help/Limitations	-> bulleted list
			// "Extra things that will happen"					-> Field/Help/Extras		-> bulleted list

			var sbHelp = new StringBuilder();
			sbHelp.Append("<Field uiname=\"");
			sbHelp.Append(MakeValidXML(fd.Userlabel));
			sbHelp.Append("\"><Help>");
			if (fd.HelpString != null && fd.HelpString.Trim().Length > 0)
			{
				sbHelp.Append("<Usage>");
				sbHelp.Append(MakeValidXML(fd.HelpString));
				sbHelp.Append("</Usage>");
			}
			sbHelp.Append("<Settings>Set the Language Descriptor to the language of this field.</Settings>");
			if (fd.Type == CellarPropertyType.MultiUnicode)
			{
				sbHelp.Append("<Mapping>No</Mapping>");
				sbHelp.Append("<Appends>Yes, appends field contents into a single field.</Appends>");
				sbHelp.Append("<List>No</List>");
				sbHelp.Append("<Multilingual>No</Multilingual>");
			}
			else if (fd.Type == CellarPropertyType.String)
			{
				sbHelp.Append("<Mapping>Yes</Mapping>");
				sbHelp.Append("<Appends>Yes, appends field contents into a single field.</Appends>");
				sbHelp.Append("<List>No</List>");
				sbHelp.Append("<Multilingual>No</Multilingual>");
			}
			else if (fd.Type == CellarPropertyType.OwningAtomic && fd.DstCls == StTextTags.kClassId)
			{
				sbHelp.Append("<Mapping>Yes</Mapping>");
				sbHelp.Append("<Appends>Yes, appends field contents into a single field.</Appends>");
				sbHelp.Append("<List>No</List>");
				sbHelp.Append("<Multilingual>No</Multilingual>");
			}
			else if (fd.Type == CellarPropertyType.ReferenceAtomic && fd.ListRootId != Guid.Empty)
			{
				sbHelp.Append("<Mapping>No</Mapping>");
				sbHelp.Append("<Appends>No, does not append field contents into a single field.</Appends>");
				sbHelp.Append("<List>Yes</List>");
				sbHelp.Append("<Multilingual>No</Multilingual>");
			}
			else if (fd.Type == CellarPropertyType.ReferenceCollection && fd.ListRootId != Guid.Empty)
			{
				sbHelp.Append("<Mapping>No</Mapping>");
				sbHelp.Append("<Appends>Yes, appends field contents into a single field.</Appends>");
				sbHelp.Append("<List>Yes</List>");
				sbHelp.Append("<Multilingual>No</Multilingual>");
			}
			// JohnT: added these three for LT-11188. Not sure yet what else has to change or how this is used.
			else if (fd.Type == CellarPropertyType.GenDate || fd.Type == CellarPropertyType.Numeric || fd.Type == CellarPropertyType.Integer)
			{
				sbHelp.Append("<Mapping>No</Mapping>");
				sbHelp.Append("<Appends>No, does not append field contents into a single field.</Appends>");
				sbHelp.Append("<List>No</List>");
				sbHelp.Append("<Multilingual>No</Multilingual>");
			}
			else
			{
				throw new Exception("bad type for custom field - code has to change");
			}
			sbHelp.Append("</Help></Field>");
			return sbHelp.ToString();
		}

		List<uint> m_lastCrcs = new List<uint>();
		internal ILexImportFields ReadCustomFieldsFromDB(out bool changed)
		{
			var sb = new StringBuilder();
			var crcs = new List<uint>();
			ILexImportFields customFields = new LexImportFields();
			foreach (var fd in FieldDescription.FieldDescriptors(m_cache))
			{
				if (fd.IsCustomField && fd.Class > 4999 && fd.Class < 6000)
				{
					var lif = FieldDescriptionToLexImportField(fd);

					var helpString = GetCustomFieldHelp(fd);
					var doc = XDocument.Parse(helpString);

					var root = doc.Root;
					lif.ReadElement(root);
					sb.Append(lif.CRC); // for cumulative CRC (over the whole list)
					crcs.Add(lif.CRC);
					customFields.AddCustomField(fd.Class, lif);
				}
			}
			if (crcs.Count == m_lastCrcs.Count)
			{
				var ipos = 0;
				while (ipos < crcs.Count && crcs[ipos] == m_lastCrcs[ipos])
				{
					ipos++;
				}

				if (ipos == crcs.Count)
				{
					changed = false;
				}
				else
				{
					changed = true;
					m_lastCrcs = crcs;
				}
			}
			else
			{
				changed = true;
				m_lastCrcs = crcs;
			}

			return customFields;
		}

		private string MakeValidXML(string input)
		{
			return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
		}

		private bool AddInLineMarker(ClsInFieldMarker marker, bool makeSelected)
		{
			// now put the info into the list view
			string[] columns = { marker.Begin, marker.EndListToString(), marker.Language, marker.Style };
			var lvItem = new ListViewItem(columns)
			{
				Tag = marker,
				Selected = makeSelected,
				Focused = makeSelected
			};

			if (listViewCharMappings.Items.Contains(lvItem) == false)
			{
				listViewCharMappings.Items.Add(lvItem);
				if (marker.Ignore)  // this is ignored
				{
					lvItem.ForeColor = Color.Blue;      // show as ignored
				}
			}
			else
			{
				Debug.WriteLine("Found a matching IFM entry, not adding again.  (" + marker.ElementName + ")");
			}
			return true;
		}

		private void listViewMappingLanguages_SelectedIndexChanged(object sender, EventArgs e)
		{
			var selIndexes = listViewMappingLanguages.SelectedIndices;
			btnModifyMappingLanguage.Enabled = selIndexes.Count > 0;
		}

		private static ListViewItem CreateLanguageMappingItem(string langDesc, string ws, string ec, string wsId)
		{
			var lvItem = new ListViewItem(new[] { langDesc, ws, ec });
			var langInfo = new LanguageInfoUI(langDesc, ws, ec, wsId);
			if (langInfo.FwName == SfmToXmlServices.Ignore)  // this is ignored due to lang
			{
				lvItem.UseItemStyleForSubItems = false;
				lvItem.SubItems[1].ForeColor = Color.Blue;
			}
			lvItem.Tag = langInfo;

			return lvItem;
		}

		internal bool AddLanguage(string langDesc, string ws, string ec, string wsId)
		{
			// now put the lang info into the language list view
			var lvItem = CreateLanguageMappingItem(langDesc, ws, ec, wsId);
			listViewMappingLanguages.Items.Add(lvItem);

			return true;
		}

		private void btnAddMappingLanguage_Click(object sender, EventArgs e)
		{
			// get list of current Language descriptor values
			var langDescs = GetUILanguages();

			using (var dlg = new LexImportWizardLanguage(m_cache, langDescs, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_app))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_dirtySenseLastSave = true;
					string langDesc, ws, ec, wsId;
					// retrieve the new WS information from the dlg
					dlg.GetCurrentLangInfo(out langDesc, out ws, out ec, out wsId);

					// now put the lang info into the language list view
					AddLanguage(langDesc, ws, ec, wsId);
				}
			}
		}

		private void btnModifyMappingLanguage_Click(object sender, EventArgs e)
		{
			// get list of current Language descriptor values
			var langDescs = new Hashtable();
			var map = string.Empty;
			var name = string.Empty;
			var desc = string.Empty;
			var selectedFound = false;
			foreach (ListViewItem lvItem in listViewMappingLanguages.Items)
			{
				langDescs.Add(lvItem.Text, null);
				if (lvItem.Selected && !selectedFound)
				{
					desc = lvItem.Text;
					name = lvItem.SubItems[1].Text;
					map = lvItem.SubItems[2].Text;
					selectedFound = true;
					// only one selected at a time, but can't break as that
					// keeps the rest of the list from being added to the list of current
					// Language Descriptors which is used in the dlg for making sure that
					// there aren't duplicates.  Fix for LT-5745
				}
			}

			using (var dlg = new LexImportWizardLanguage(m_cache, langDescs, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_app))
			{
				dlg.LangToModify(desc, name, map);
				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				m_dirtySenseLastSave = true;
				string langDesc, ws, ec, wsId;
				// retrieve the new WS information from the dlg
				dlg.GetCurrentLangInfo(out langDesc, out ws, out ec, out wsId);

				var selectedIndex = listViewMappingLanguages.SelectedIndices[0];
				listViewMappingLanguages.Items[selectedIndex] = CreateLanguageMappingItem(langDesc, ws, ec, wsId);
				listViewMappingLanguages.Items[selectedIndex].Selected = true; // maintain the selection

				// Make sure we don't read in the file and clobber the memory changes if
				// the user cancels/saves right now.
				m_dirtyMapFile = false;

				if (m_MappingMgr == null)
				{
					return;
				}
				// now update any existing markers that have this langdescriptor
				var anyUpdated = false;
				var markers = m_MappingMgr.ContentMappingItems;
				foreach (DictionaryEntry markerEntry in markers)
				{
					var info = (ContentMapping)markerEntry.Value;
					if (info.LanguageDescriptorRaw != langDesc)
					{
						continue;
					}
					info.UpdateLanguageValues(ws, wsId, langDesc);
					anyUpdated = true;
				}
				if (anyUpdated)
					DisplayMarkerStep();
			}
		}

		private void listViewMappingLanguages_ColumnClick(object sender, ColumnClickEventArgs e)
		{
		}

		private string ConvertNameFromIdtoFW(string wsId)
		{
			//getting name for a writing system given the identifier.
			CoreWritingSystemDefinition ws;

			return m_cache.ServiceLocator.WritingSystemManager.TryGet(wsId, out ws) ? ws.DisplayLabel : null;
		}

		#endregion

		/// Step 4 is where the markers are managed: displayed and processed.
		#region Step 4 event handlers and routines
		private void ReadOptionInfoFromMapFile()
		{
			var oc = new OptionConverter();
			var options = oc.Options(m_SettingsFileName.Text);
			if (options == null) return;
			foreach (var option in options)
			{
				switch (option.Key)
				{
					case kOptionKeyMissingLinkCheckbox:
						m_chkCreateMissingLinks.Checked = option.Value;
						break;
					default:
						Debug.Fail($"Unknown option key {option.Key}");
						break;
				}
			}
		}

		private static void SetListViewItemColor(ref ListViewItem item)
		{
			var info = item.Tag as ContentMapping;
			if (info == null)
			{
				return;
			}
			if (info.AutoImport)    // this is an autoimport field
			{
				item.UseItemStyleForSubItems = false;
				for (var i = 0; i < 5; i++)
				{
					item.SubItems[i].ForeColor = Color.Red;
				}
			}
			if (info.Exclude)   // this is an excluded field
			{
				item.UseItemStyleForSubItems = false;
				item.SubItems[4].ForeColor = Color.Blue;
			}
			if (info.WritingSystem == SfmToXmlServices.Ignore)   // this is ignored due to lang
			{
				item.UseItemStyleForSubItems = false;
				item.SubItems[5].ForeColor = Color.Blue;
			}
			if (info.LanguageDescriptor == ContentMapping.Unknown())
			{
				item.UseItemStyleForSubItems = false;
				item.SubItems[5].ForeColor = Color.Red; // column 5 due to column 1 being hidden (zero width)
			}
		}

		private void DisplayMarkerStep()
		{
			listViewContentMapping.BeginUpdate();
			listViewContentMapping.Items.Clear();

			var markers = m_MappingMgr.ContentMappingItems;
			foreach (DictionaryEntry markerEntry in markers)
			{
				var info = (ContentMapping)markerEntry.Value;
				if (info.LexImportField is LexImportCustomField)
				{
					((LexImportCustomField)info.LexImportField).UIClass = info.DestinationClass;
				}
				var lvItem = new ListViewItem(info.ListViewStrings())
				{
					Tag = info
				};
				listViewContentMapping.Items.Add(lvItem);
				// adjust background color if needed
				SetListViewItemColor(ref lvItem);
			}
			// sort initially by the 'order of appearance'
			listViewContentMapping.ListViewItemSorter = new ListViewItemComparer(1, false);
			// now hide the column
			listViewContentMapping.Columns[1].Width = 0;
			listViewContentMapping.EndUpdate();
		}

		private void btnModifyContentMapping_Click(object sender, EventArgs e)
		{
			var selIndexes = listViewContentMapping.SelectedIndices;
			if (selIndexes.Count < 1 || selIndexes.Count > 1)
			{
				return; // only handle single selection at this time
			}

			var selIndex = selIndexes[0];   // only support 1
			var langDescs = GetUILanguages();

			var contentMapping = listViewContentMapping.Items[selIndex].Tag as ContentMapping;
			using (var dlg = new LexImportWizardMarker(m_LexFields))
			{
				dlg.Init(contentMapping, langDescs, m_cache, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_app);
				var dr = dlg.ShowDialog(this);

				// Custom fields have to be handled independantly of the dialogresult being ok sense they
				// can change the custom fields and still select cancel in the dlg
				m_CustomFields = dlg.CustomFields;
				m_MappingMgr.UpdateLexFieldsWithCustomFields(m_CustomFields);

				// make sure the display is made correct by compairing to the memory list (could have deleted custom fields that were ref'd)
				var uiItems = listViewContentMapping.Items;
				foreach (ListViewItem lvi in uiItems)
				{
					var content = (ContentMapping)lvi.Tag;

					if (!(content.LexImportField is ILexImportCustomField))
					{
						continue;
					}
					var customField = (LexImportCustomField)content.LexImportField;
					var cfChange = m_MappingMgr.GetCustomFieldChangeStatus(customField);
					switch (cfChange)
					{
						case CFChanges.NoChanges:
							continue;
						case CFChanges.DoesntExist:
							// default to the same as if field didn't exist when map was first read
							content = m_MappingMgr.DefaultContent(content.Marker);
							var index = lvi.Index;
							listViewContentMapping.Items.RemoveAt(index);
							m_MappingMgr.ReplaceContentMappingItem(content);

							var lvItem = new ListViewItem(content.ListViewStrings())
							{
								Tag = content
							};
							listViewContentMapping.Items.Add(lvItem);
							SetListViewItemColor(ref lvItem);
							continue;
					}

					if (!m_MappingMgr.IsValidCustomField(customField))
					{
						content = m_MappingMgr.ContentMappingItem(content.Marker);
						var lvItem = new ListViewItem(content.ListViewStrings())
						{
							Tag = content,
							Selected = true,
							Focused = true
						};
						listViewContentMapping.Items.Add(lvItem);
						listViewContentMapping.Focus();
						SetListViewItemColor(ref lvItem);
					}
				}

				if (dr == DialogResult.OK)
				{
					// have to reset the contentMapping variable as it is still set from the last one, and
					// in the case that I'm investigating the RefFunc is still set when it should be off -
					// other fields may need to be revised too.
					if (dlg.IsFuncField)
					{
						contentMapping.RefField = dlg.FuncField;
						contentMapping.RefFieldWS = dlg.FuncFieldWS;
					}
					else
					{
						contentMapping.ClearRef();
					}
					m_dirtySenseLastSave = true;
					// remove the old from the treeview display
					listViewContentMapping.Items[selIndex].Selected = false;
					listViewContentMapping.Items[selIndex].Focused = false;
					listViewContentMapping.Items.RemoveAt(selIndex);

					// now update the item and add it again and then select it
					contentMapping.Exclude = dlg.ExcludeFromImport;

					if (!contentMapping.Exclude)
					{
						contentMapping.AutoImport = dlg.AutoImport;
						// get the language values
						var userKey = dlg.LangDesc;
						var ws = dlg.WritingSystem;
						// it is possible through the GUI to have more UILanguages now, so get a fresh list
						langDescs = GetUILanguages();

						var langInfo = (LanguageInfoUI)langDescs[userKey];
						var shortName = langInfo.ICUName;
						contentMapping.UpdateLanguageValues(ws, shortName, userKey);

						if (!contentMapping.AutoImport) // auto import only allows lang so skip the following
						{
							contentMapping.FwId = dlg.FWDestID;

							string fname;
							var cname = dlg.FWDestinationClass;
							if (dlg.IsCustomField)
							{
								string cnameTmp;
								m_CustomFields.GetDestinationForName(contentMapping.FwId, out cnameTmp, out fname);
								contentMapping.AddLexImportCustomField(m_CustomFields.GetField(cname, contentMapping.FwId), cname);
								// Need to make sure the clscustom... member of the contentMapping object is a custom one now
								// this is needed for the modify dlg that comes up.
							}
							else
							{
								string cnameTmp;
								m_LexFields.GetDestinationForName(contentMapping.FwId, out cnameTmp, out fname);
								contentMapping.AddLexImportField(m_LexFields.GetField(cname, contentMapping.FwId));
								// need to make sure the clscustom... isn't set on the contentMapping object.
							}

							contentMapping.DestinationClass = cname;
							contentMapping.RawDestinationField = fname;
							if (contentMapping.IsAbbrvField)
							{
								// save the selection 'name' or 'abbreviation' from the dialog
								contentMapping.UpdateAbbrValue(dlg.IsAbbrNotName);
							}

							// update some more underlying fields now that we are associated with a destination:
							// datatype, etc...
							contentMapping.LexImportField.ClsFieldDescriptionWith(contentMapping.ClsFieldDescription);
						}
						else
						{
							contentMapping.DestinationClass = string.Empty;
							// is autoimport field, so empty out LexImportField and ClsFieldDescription from the contentMapping
							contentMapping.DoAutoImportWork();
						}
					}

					var lvItem = new ListViewItem(contentMapping.ListViewStrings())
					{
						Tag = contentMapping,
						Selected = true,
						Focused = true
					};
					listViewContentMapping.Items.Add(lvItem);
					listViewContentMapping.Focus();
					SetListViewItemColor(ref lvItem);
				}
			}
		}

		private void listViewContentMapping_SelectedIndexChanged(object sender, EventArgs e)
		{
			btnModifyContentMapping.Enabled = listViewContentMapping.SelectedIndices.Count > 0;
		}

		private void listViewContentMapping_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			// sort
			listViewContentMapping.ListViewItemSorter = new ListViewItemComparer(e.Column, m_MappingMgr.GetAndChangeColumnSortOrder(e.Column));
		}

		#endregion

		/// This is where the inline markers are managed.
		#region Step 6 event handlers and routines
		void ReadIFMFromMapFile()
		{
			var reader = new IFMReader();
			var htIFM = reader.IFMS(m_SettingsFileName.Text, this.GetUILanguages());
			listViewCharMappings.Items.Clear();
			if (htIFM == null)
			{
				return;
			}

			listViewCharMappings.BeginUpdate();

			foreach (DictionaryEntry ifm in htIFM)
			{
				var clsIFM = (ClsInFieldMarker)ifm.Value;
				AddInLineMarker(clsIFM, false);
			}
			listViewCharMappings.EndUpdate();
		}
		#endregion

		/// The data contained here is in the tree view.  The class nodes (root nodes) contain
		/// an arraylist in the tag field that has all the selected markers ("ps", "gn", ...).
		/// The child nodes contian the ContentMapping object for that marker.  With this information,
		/// the final output should be able to be produced.
		#region Step 5 event handlers and routines


		// The idea here is to sort the individual markers by their source order
		private sealed class SortClassMarkers : IComparer
		{
			#region IComparer Members

			public int Compare(object x, object y)
			{
				var a = (ContentMapping)x;
				var b = (ContentMapping)y;

				return a.Order.CompareTo(b.Order);
			}

			#endregion
		}

		// This sorts a collection of marker groups by the source order of the first marker in the group
		private sealed class SortClasses : IComparer
		{
			#region IComparer Members

			public int Compare(object x, object y)
			{
				var a = ((ContentMapping)((ArrayList)((DictionaryEntry)x).Value)[0]).Order;
				var b = ((ContentMapping)((ArrayList)((DictionaryEntry)y).Value)[0]).Order;

				return a.CompareTo(b);
			}

			#endregion
		}

		private void DisplayBeginMarkers()
		{
			ICollection classesAndMappingsList = listViewContentMapping.Items;
			var classMarkers = new Hashtable();

			// fill the hashtable with keys[classes] and the contentmapping objects that belong to them
			foreach (ListViewItem item in classesAndMappingsList)
			{
				var info = (ContentMapping)item.Tag;
				if (info.AutoImport)
				{
					continue;           // autoimport fields can't be taged to start certian classes...
				}

				ArrayList markers;
				if (classMarkers.ContainsKey(info.DestinationClass))
				{
					markers = (ArrayList)classMarkers[info.DestinationClass];
				}
				else
				{
					markers = new ArrayList();
					classMarkers[info.DestinationClass] = markers;
				}

				markers.Add(info);
			}

			// Sorting
			var sortedClassMarkers = new ArrayList();
			foreach (DictionaryEntry dict in classMarkers)
			{
				var markers = (ArrayList)dict.Value;
				if (markers.Count == 0)
				{
					continue; // We don't want classes without any markers
				}
				markers.Sort(new SortClassMarkers());
				sortedClassMarkers.Add(dict);
			}
			sortedClassMarkers.Sort(new SortClasses());

			// now have a hashtable of classes and each contains an arraylist of markers
			tvBeginMarkers.BeginUpdate();
			tvBeginMarkers.Nodes.Clear();

			TreeNode tempTreeNode = null;   // last node that needs a begin marker selected

			foreach (DictionaryEntry dict in sortedClassMarkers)
			{
				// skip Unknown, ignore and exclude markers
				if ((string)dict.Key == "Unknown")
				{
					continue;
				}

				var mappingInfo = dict.Value as ArrayList;  // MarkerPresenter.ContentMapping;
				var tnode = new TreeNode(dict.Key as string)
				{
					Tag = null,
					NodeFont = new Font(tvBeginMarkers.Font, FontStyle.Bold)
				};

				// Make it bold because this is a parent node
				tnode.SelectedImageKey = tnode.ImageKey = "Bullet";

				foreach (ContentMapping field in mappingInfo)
				{
					var cnode = new TreeNode("\\" + field.Marker + " (" + field.DestinationField + ")")
					{
						Tag = field
					};
					if (field.Exclude ||
						field.IsLangIgnore || // field.DestinationField == MarkerPresenter.ContentMapping.Ignore() ||
						field.DestinationField == ContentMapping.Unknown() ||
						field.LanguageDescriptor == ContentMapping.Unknown())   // can't pick field with unknown lang descriptor
					{
						continue;
					}
					cnode.Checked = field.IsBeginMarker;    // check it if it's already identified as a begin marker
					cnode.SelectedImageKey = cnode.ImageKey = cnode.Checked ? "CheckedBox" : "CheckBox";
					tnode.Nodes.Add(cnode);
				}
				tvBeginMarkers.Nodes.Add(tnode);
				if (HasCheckedChild(tnode) == 0)
				{
					tnode.BackColor = Color.Gold;
					tempTreeNode = tnode;           // get a reference so we can ensure visible later.
				}
				else
					tnode.Checked = true;           // has child that is selected, so show as selected
			}
			tvBeginMarkers.ExpandAll();
			tvBeginMarkers.EndUpdate();

			tempTreeNode?.EnsureVisible();  // make sure the node that needs a begin marker is visible
		}

		private bool Step5NextButtonEnabled()
		{
			var nextOk = true;
			foreach (TreeNode node in tvBeginMarkers.Nodes)
			{
				if (HasCheckedChild(node) == 0) // count of checked children
				{
					nextOk = false;
					break;
				}
			}
			return nextOk;
		}

		/// <summary>
		/// Get information about the number of checked children.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>
		/// This method returns the number of child items that are checked.
		/// It will return zero if there are children and none are selected.
		/// It will return -1 if there are no children.
		/// </returns>
		private static int HasCheckedChild(TreeNode node)
		{
			if (node.Nodes.Count == 0)
			{
				return -1;
			}
			return node.Nodes.Cast<TreeNode>().Count(child => child.Checked);
		}

		#endregion

		/// Step 7 is where the Feasability check is preformed.  This is the
		/// finial step before letting the whole import process run it's course.
		#region Step 7 event handlers and routines

		/// <summary>
		/// This method will check for changes to the data due to a different version: for example
		/// 4.5.1 release to 6.0 release.
		/// This should be improved in the future to pass in the previous version so more concrete
		/// decisions can be made.
		/// </summary>
		private void CheckForMapFileVersionChanges(FieldHierarchyInfo data)
		{
			var topAnalysisWs = m_cache.DefaultAnalWs;
			var topAnalysis = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(topAnalysisWs);

			if (SfmToXmlServices.MapFileVersion != "6.0")
			{
				return;
			}
			// LT-9936
			// For old lexical relation and cross reference fields - use top analysis ws for the
			// funcWS instead of letting it default to "".
			switch (data.FwDestID)
			{
				case "lxrel":   // Lexical relation
				case "cref":    // Cross reference fields
				case "funold": // Old lexical function field
					if (data.RefFuncWS == string.Empty)
					{
						data.RefFuncWS = topAnalysis;   // dont let it be blank for old map files
					}
					break;

				case "subd":    // Subentry (Derivation)
				case "subc":    // Subentry (Compound)
				case "subi":    // Subentry (Idiom)
				case "subk":    // Subentry (Keyterm Phrase)
				case "subpd":   // Subentry (Phrasal Verb)
				case "subs":    // Subentry (Saying)
					data.FwDestID_Changed = "sub";  // new value
					data.RefFuncWS = topAnalysis;
					break;

				case "vard":    // Variant (Dialectal)
				case "varf":    // Variant (Free)
				case "vari":    // Variant (Inflectional)
				case "vars":    // Variant (Spelling)
								//case "varc":	// Variant (Comment)
					data.FwDestID_Changed = "var";  // new value
					data.RefFuncWS = topAnalysis;
					break;
			}
		}

		/// <summary>
		/// This method produces the map file that will contain the info in the GUI up to this point.
		/// </summary>
		private void SaveNewMapFile()
		{
			// This where the code will go to compile all the information from the Wizard
			// and produce the map file to use for importing...
			var nl = Environment.NewLine;
			var XMLText = new StringBuilder(1024);
			m_FeasabilityReportGenerated = true;
			NextButtonEnabled = true;
			if (m_SaveAsFileName.Text.Length == 0)
			{
				m_SaveAsFileName.Text = m_sTempDir + m_cache.ProjectId.Name + ".map";
			}

			// try to extract out the sfm info so the same 'map builder' code can run
			var sfmInfo = new List<FieldHierarchyInfo>();
			foreach (ListViewItem lvItem in listViewContentMapping.Items)
			{
				var info = (ContentMapping)lvItem.Tag;
				if (info == null || info.DestinationField == SfmToXmlServices.Unknown)
				{
					continue;   // skip these from output
				}

				// convert from ContentMapping object to a FieldHierarchyInfo object
				FieldHierarchyInfo data;
				if (info.AutoImport)
				{
					data = new FieldHierarchyInfo(info.Marker, info.LanguageDescriptor);
				}
				else
				{
					var destClass = string.Empty;
					if (info.LexImportField is LexImportCustomField)
					{
						destClass = info.DestinationClass;  // (info.LexImportField as SfmToXml.LexImportCustomField).UIClass;
					}

					data = new FieldHierarchyInfo(info.Marker, info.FwId, info.LanguageDescriptor, info.IsBeginMarker, destClass);
				}
				// set all the remaining properties from the 'ContentMapping' object
				data.RefFuncWS = info.RefFieldWS;
				data.RefFunc = info.RefField;
				data.IsExcluded = info.Exclude;
				data.IsAbbrvField = info.IsAbbrvField;
				data.IsAbbr = info.IsAbbr;
				CheckForMapFileVersionChanges(data);    // make changes if the map file read in is older than the current version
				sfmInfo.Add(data);

				var xmlOutput = info.ClsFieldDescription.ToXmlString();
				XMLText.Append(xmlOutput + nl);
			}

			// Build the list of in field markers
			var ifMarker = new List<ClsInFieldMarker>();
			foreach (ListViewItem lvItem in listViewCharMappings.Items)
			{
				var marker = lvItem.Tag as ClsInFieldMarker;
				if (marker == null)
				{
					continue;
				}
				ifMarker.Add(marker);
			}

			var uiLangsNew = GetUILanguages();

			// Build the list of options
			var options = new List<ILexImportOption>
			{
				new LexImportOption(kOptionKeyMissingLinkCheckbox, "Checkbox", m_chkCreateMissingLinks.Checked)
			};

			// this is the external way through common objects to create the map file
			SfmToXmlServices.NewMapFileBuilder(uiLangsNew, m_LexFields, m_CustomFields, sfmInfo, ifMarker, m_SaveAsFileName.Text, options);

		}

		/// <summary>
		/// Return true if the 'lx' marker language is already in unicode.
		/// </summary>
		private bool IslxFieldAlreadyUnicode()
		{
			var info = m_MappingMgr.ContentMappingItems["lx"] as ContentMapping;
			if (info == null)
			{
				return false;
			}

			var uiLangs = GetUILanguages();
			var langInfo = uiLangs[info.LanguageDescriptor] as LanguageInfoUI;
			// no encoding converter == Unicode
			return langInfo != null && langInfo.EncodingConverterName.Length == 0;
		}

		/// <summary>
		/// This button runs the feasability check and then enables the next button
		/// so the user can continue on.
		/// </summary>
		private void btnGenerateReport_Click(object sender, System.EventArgs e)
		{
			using (new WaitCursor(this))
			{
				SaveSettings(); // saves to registry and creates the new map file
				btnGenerateReport.Enabled = false;
				Converter importConverter = new FlexConverter(m_cache);
				Converter.Log.Reset();  // remove any previous error msgs

				// if there are auto fields in the xml file, pass them on to the converter
				var autoFields = m_MappingMgr.LexImportFields.GetAutoFields();
				foreach (var kvp in autoFields)
				{
					var entryClass = kvp.Key;
					var lexField = kvp.Value;
					var fwDest = lexField.ID;
					importConverter.AddPossibleAutoField(entryClass, fwDest);
				}
				try
				{
					// Note: don't use the m_MappingMgr sense it already has been used for reading data.
					importConverter.Convert(m_processedInputFile, m_SaveAsFileName.Text, m_sPhase1Output);
					m_cEntries = importConverter.LevelOneElements;
					ProcessPhase1Errors(m_sPhase1Output, m_SaveAsFileName.Text, m_cEntries, false);
					var sHtmlFile = m_sTempDir + "ImportPreviewReport.htm";
					ViewHtmlReport(sHtmlFile);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Convert Exception: " + ex.Message);
					if (ProcessPhase1Errors(m_sPhase1Output, m_SaveAsFileName.Text, importConverter.LevelOneElements, false))
					{
						var sHtmlFile = m_sTempDir + "ImportPreviewReport.htm";
						ViewHtmlReport(sHtmlFile);
					}
					else
					{
						MessageBox.Show(this, string.Format(LexTextControls.ksConversionProblem, ex.Message), LexTextControls.ksConversionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
			btnGenerateReport.Enabled = true;
		}

		private void ViewHtmlReport(string sHtmlFile)
		{
			try
			{
				// make sure the file exists, otherwise the process will fail
				if (File.Exists(sHtmlFile))
				{
					using (Process.Start(sHtmlFile))
					{
					}
				}
			}
			catch (Exception e)
			{
				var nl = Environment.NewLine;
				var msg = string.Format(LexTextControls.ksCannotDisplayReportX, sHtmlFile, nl, e.Message);
				MessageBox.Show(this, msg, LexTextControls.ksErrorShowingReport);
			}
		}

		private static string GetTempDir()
		{
			// Use a FieldWorks specific temp directory.
			var sTempDir = Path.GetTempPath();
			if (!sTempDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				sTempDir += Path.DirectorySeparatorChar;
			}
			sTempDir += $"Language Explorer{Path.DirectorySeparatorChar}";
			return sTempDir;
		}

		private void InitOutputFiles()
		{
			var sRootDir = FwDirectoryFinder.CodeDirectory;
			if (!sRootDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				sRootDir += Path.DirectorySeparatorChar;
			}
			var sTransformDir = sRootDir + string.Format("Language Explorer{0}Import{0}", Path.DirectorySeparatorChar);
			// Use a FieldWorks specific temp directory.
			m_sTempDir = GetTempDir();
			if (!Directory.Exists(m_sTempDir))
			{
				Directory.CreateDirectory(m_sTempDir);
			}

			m_sMDFImportMap = sTransformDir + "MDFImport.map";

			// Output files
			m_sPhase1Output = Path.Combine(m_sTempDir, LexImport.s_sPhase1FileName);
			m_sImportFields = sTransformDir + "ImportFields.xml";
		}

		#endregion

		#region Step 8 event handlers and routines

		protected override void OnHelpButton()
		{
			string helpTopic = null;

			switch (CurrentStepNumber)
			{
				case 0:
					helpTopic = "khtpImportSFMStep1";
					break;
				case 1:
					helpTopic = "khtpImportSFMStep2";
					break;
				case 2:
					helpTopic = "khtpImportSFMStep3";
					break;
				case 3:
					helpTopic = "khtpImportSFMStep4";
					break;
				case 4:
					helpTopic = "khtpImportSFMStep5";
					break;
				case 5:
					helpTopic = "khtpImportSFMStep6";
					break;
				case 6:
					helpTopic = "khtpImportSFMStep7";
					break;
				case 7:
					helpTopic = "khtpImportSFMStep8";
					break;
				default:
					Debug.Assert(false, "Reached a step without a help file defined for it");
					break;
			}

			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), helpTopic);
		}

		protected override void OnFinishButton()
		{
			if (UsesInvalidFileNames(false))
			{
				return;
			}

			base.OnFinishButton();

			var runToCompletion = true;
			var lastStep = 5;
			// if the shift key is down, then just build the phaseNoutput files
			if ((ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				runToCompletion = false;
				lastStep = 4;
			}

			SaveSettings();

			using (var dlg = new ProgressDialogWithTask(this))
			{
				dlg.AllowCancel = true;
				dlg.Maximum = 200;
				using (new WaitCursor(this, true))
				{
					var startPhase = GetDictionaryFileAsPhaseFileNumber();  // see if starting with phase file
																			// XSLT files
					var sTransformDir = Path.Combine(FwDirectoryFinder.CodeDirectory, string.Format("Language Explorer{0}Import{0}", Path.DirectorySeparatorChar));

					var lexImport = new LexImport(m_cache, m_sTempDir, sTransformDir);
					lexImport.Error += OnImportError;
					var fRet = (bool)dlg.RunTask(true, lexImport.Import,
						runToCompletion, lastStep, startPhase, m_DatabaseFileName.Text, m_cEntries,
						m_DisplayImportReport.Checked, m_sPhase1HtmlReport, LexImport.s_sPhase1FileName,
						m_chkCreateMissingLinks.Checked);

					if (fRet)
					{
						DialogResult = DialogResult.OK; // only 'OK' if not exception
					}
				}
			}
		}

		private void OnImportError(object sender, string message, string caption)
		{
			if (InvokeRequired)
			{
				Invoke(new LexImport.ErrorHandler(OnImportError), sender, message, caption);
			}
			else
			{
				MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion

		#region Registry / persistance routines
		/// <summary>
		/// get the registry settings for this DB if they exist already
		/// </summary>
		/// <returns>true if the settings and saveAs parms were retrieved from the registry</returns>
		private bool FindDBSettingsSaved(string dbToImport, out string settings, out string saveAs)
		{
			settings = saveAs = string.Empty;
			var dbHash = dbToImport.GetHashCode().ToString();
			using (var key = m_app.SettingsKey.OpenSubKey("ImportFile" + dbHash))
			{
				if (key != null)
				{
					settings = key.GetValue("Settings", string.Empty) as string;
					saveAs = key.GetValue("SaveAs", string.Empty) as string;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// retrieve the last import file that was imported via the wizard from the registry.
		/// </summary>
		private bool GetLastImportFile(out string fileName)
		{
			fileName = string.Empty;
			m_DatabaseFileName.Text = string.Empty; // no default value to start with
			var key = m_app.SettingsKey;
			var sDictFile = key?.GetValue("LatestImportDictFile") as string;
			if (string.IsNullOrWhiteSpace(sDictFile))
			{
				return false;
			}
			fileName = sDictFile;
			return true;
		}

		/// <summary>
		/// Get the Settings and Save as values for the passed in DB file
		/// </summary>
		private void FindFilesForDatabaseFile(string dbToImport)
		{
			m_SettingsFileName.Text = string.Empty; // empty at start
			m_SaveAsFileName.Text = string.Empty;       // empty at start
			string settings, saveAs;
			if (FindDBSettingsSaved(dbToImport, out settings, out saveAs))
			{
				if (settings != null)
				{
					m_SettingsFileName.Text = settings;
				}

				if (saveAs != null)
				{
					m_SaveAsFileName.Text = saveAs;
				}
			}
		}

		/// <summary>
		/// Save / persist the settings file name and the save as file name for this database file name.
		/// </summary>
		private void SaveSettings()
		{
			var dbImportName = m_DatabaseFileName.Text;
			if (dbImportName != string.Empty)   // has value to save
			{
				using (var key = m_app.SettingsKey)
				{
					if (key == null)
					{
						return;
					}

					// save it as the most recent dictionary file for import
					key.SetValue("LatestImportDictFile", dbImportName);
				}

				var dbHash = dbImportName.GetHashCode().ToString();

				// save it to the folder of imported dictionary files
				using (var key = m_app.SettingsKey.CreateSubKey("ImportDictFiles"))
				{
					key.SetValue("ImportFile" + dbHash, dbImportName);
				}

				// save the support files for this: map and 'save as' files
				using (var key = m_app.SettingsKey.CreateSubKey("ImportFile" + dbHash))
				{
					if (key != null)
					{
						key.SetValue("Settings", m_SettingsFileName.Text);
						key.SetValue("SaveAs", m_SaveAsFileName.Text);
					}
				}
			}
			// also need to create the map file - or go down trying...
			SaveNewMapFile();
		}
		#endregion

		#region Wizard flow processing

		protected override void OnCancelButton()
		{
			if (m_fCanceling)
			{
				return;
			}

			m_fCanceling = true;
			base.OnCancelButton();
			if (CurrentStepNumber == 0)
			{
				return;
			}

			DialogResult = DialogResult.Cancel;

			// if it's known to be dirty OR the shift key is down - ask to save the settings file
			if (!m_dirtySenseLastSave && (ModifierKeys & Keys.Shift) != Keys.Shift)
			{
				return;
			}
			// LT-7057: if no settings file, don't ask to save
			if (UsesInvalidFileNames(true))
			{
				return; // finsih with out prompting to save...
			}

			// ask to save the settings
			var result = DialogResult.Yes;
			// if we're not importing a phaseX file, then ask
			if (GetDictionaryFileAsPhaseFileNumber() == 0)
			{
				result = MessageBox.Show(this, LexTextControls.ksAskRememberImportSettings, LexTextControls.ksSaveSettings_, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
			}

			switch (result)
			{
				case DialogResult.Yes:
					// before saving we need to make sure all the data structures are populated
					while (CurrentStepNumber <= 6)
					{
						EnableNextButton();
						m_CurrentStepNumber++;
					}
					SaveSettings();
					break;
				case DialogResult.Cancel:
					// This is how do we stop the cancel process...
					DialogResult = DialogResult.None;
					m_fCanceling = false;
					break;
			}
		}

		/// <summary>
		/// This method will allow or disallow the next buttong to function.
		/// </summary>
		protected override bool ValidToGoForward()
		{
			var rval = true;
			switch (CurrentStepNumber)
			{
				case 2:
					if (listViewMappingLanguages.Items.Count <= 0)
					{
						MessageBox.Show(LexTextControls.ksNeedALanguage, LexTextControls.ksIncompleteLangStep);
						rval = false;
					}
					break;
				case 6:
					rval = m_FeasabilityReportGenerated;
					break;
			}

			return rval;
		}

		private bool UpdateIfInputFileContentsChanged()
		{
			if (m_dirtyInputFile)
			{
				return false;
			}
			// check the date and time and possibly compute crc for the input file
			m_dirtyInputFile |= CheckIfInputFileHasChanged();
			if (!m_dirtyInputFile)
			{
				return false;
			}
			var msg = string.Format(LexTextControls.ksInputFileContentsChanged, m_processedInputFile);
			MessageBox.Show(this, msg, LexTextControls.ksInputFileContentsChangedTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

			m_lastDateTime = File.GetLastWriteTime(m_processedInputFile);   // used to keep track of the last write date on the data file
			m_crcOfInputFile = m_crcObj.FileCRC(m_processedInputFile);      // the computed crc of the data file
			m_dirtyInputFile = false;
			m_dirtyMapFile = false;
			var changeToUI = m_MappingMgr.UpdateSfmDataChanged();
			DisplayMarkerStep();
			m_FeasabilityReportGenerated = false;   // reset when intputs change
			return changeToUI;
		}

		private bool CheckIfInputFileHasChanged()
		{
			try
			{
				if (m_lastDateTime != File.GetLastWriteTime(m_DatabaseFileName.Text))
				{
					// now see if the content has changed
					if (m_crcOfInputFile != m_crcObj.FileCRC(m_DatabaseFileName.Text))
					{
						return true;    // content has changed
					}
				}
			}
			catch (ArgumentException)
			{
				// case where an invalid database file name path is given (could be empty too)
			}
			return false;
		}

		private bool EnableNextButton()
		{
			AllowQuickFinishButton();   // this should be done atleast before each step
			var rval = false;
			using (new WaitCursor(this))
			{
				switch (CurrentStepNumber)
				{
					case 1: // has to have a dictionary file to allow 'next'
						if (m_isPhaseInputFile || (m_DatabaseFileName.Text.Length > 0 && File.Exists(m_DatabaseFileName.Text) && m_SaveAsFileName.Text != m_sMDFImportMap))     // not same as MDFImport.map file
						{
							rval = true;
						}
						break;

					case 2: // preparing to display the languages info
						if (m_dirtyMapFile)
						{
							ReadLanguageInfoFromMapFile();
							m_processedMapFile = m_SettingsFileName.Text;
						}
						rval = true;

						// make sure there is a value for the 'Save as:' entry
						if (m_SaveAsFileName.Text.Length <= 0 && !m_isPhaseInputFile)
						{
							m_SaveAsFileName.Text = RemoveTheFileExtension(m_DatabaseFileName.Text) + "-import-settings.map";
						}
						break;

					case 3:
						// current technique for getting the custom fields in the DB
						bool customFieldsChanged;
						m_CustomFields = Wizard().ReadCustomFieldsFromDB(out customFieldsChanged);

						UpdateIfInputFileContentsChanged();
						if (m_dirtyInputFile || m_dirtyMapFile)
						{
							ReadIFMFromMapFile();   // do it now before setting the dirty map flag to false
							m_processedInputFile = m_DatabaseFileName.Text;
							m_lastDateTime = File.GetLastWriteTime(m_processedInputFile);   // used to keep track of the last write date on the data file
							m_crcOfInputFile = m_crcObj.FileCRC(m_processedInputFile);      // the computed crc of the data file
							m_dirtyInputFile = false;
							m_dirtyMapFile = false;
							var topAnalysisWS = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultAnalWs);
							m_MappingMgr = new MarkerPresenter(FwDirectoryFinder.CodeDirectory,
								Wizard().GetUILanguages(),
								topAnalysisWS,
								m_SettingsFileName.Text,
								m_DatabaseFileName.Text,
								m_sImportFields, 6);    // number of columns
							DisplayMarkerStep();
							m_FeasabilityReportGenerated = false;   // reset when intputs change
						}
						rval = true;
						break;

					case 4:
						UpdateIfInputFileContentsChanged();
						DisplayBeginMarkers();
						m_DirtyStep5 = false;
						rval = Step5NextButtonEnabled();
						break;

					case 5: // preparing to display the inline markers
						rval = true;
						break;

					case 6:
						rval = m_FeasabilityReportGenerated;
						break;

					default:
						rval = true;
						break;
				}
			}
			return rval;
		}

		/// <summary>
		/// Use the database file name on the dlg as the text to see if it's a PhaseX file
		/// </summary>
		private int GetDictionaryFileAsPhaseFileNumber()
		{
			return GetDictionaryFileAsPhaseFileNumber(m_DatabaseFileName.Text);
		}

		/// <summary>
		/// See if the file passed in is one of the valid phaseX file names
		/// </summary>
		private static int GetDictionaryFileAsPhaseFileNumber(string fileName)
		{
			if (fileName.EndsWith(LexImport.s_sPhase1FileName))
			{
				return 1;
			}

			if (fileName.EndsWith(LexImport.s_sPhase2FileName))
			{
				return 2;
			}

			if (fileName.EndsWith(LexImport.s_sPhase3FileName))
			{
				return 3;
			}
			return fileName.EndsWith(LexImport.s_sPhase4FileName) ? 4 : 0;
		}

		private void ShowSaveButtonOrNot()
		{
			btnSaveMapFile.Visible = !string.IsNullOrEmpty(m_SaveAsFileName.Text);
		}

		protected override void OnBackButton()
		{
			ShowSaveButtonOrNot();
			base.OnBackButton();
			if (m_QuickFinish)
			{
				// go back to the page where we came from
				tabSteps.SelectedIndex = m_lastQuickFinishTab + 1;
				m_CurrentStepNumber = m_lastQuickFinishTab;
				UpdateStepLabel();
				m_QuickFinish = false;  // going back, so turn off flag
			}
			else if (CurrentStepNumber == 6 && m_isPhaseInputFile)
			{
				// skip the pages in the middle that were skipped getting here
				tabSteps.SelectedIndex = 2; // 0-7
				m_CurrentStepNumber = 1;    // 1-8
				UpdateStepLabel();
			}

			NextButtonEnabled = true;   // make sure it's enabled if we go back from generated report
			AllowQuickFinishButton();   // make it visible if needed, or hidden if not
		}

		private bool UsesInvalidFileNames(bool runSilent)
		{
			var fStayHere = false;
			if (!File.Exists(m_SettingsFileName.Text))
			{
				SetDefaultSettings();
			}
			if (m_isPhaseInputFile)
			{
				;
				//just for blocking this case
			}
			else if (m_SettingsFileName.Text.Length != 0 && !File.Exists(m_SettingsFileName.Text))
			{
				if (!runSilent)
				{
					var msg = string.Format(LexTextControls.ksInvalidSettingsFileX, m_SettingsFileName.Text);
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
				m_SettingsFileName.Focus();
			}
			else if (!m_isPhaseInputFile && m_SaveAsFileName.Text.Length == 0)
			{
				if (!runSilent)
				{
					var msg = LexTextControls.ksUndefinedSettingsSaveFile;
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
				m_SaveAsFileName.Focus();
			}
			else if (m_SaveAsFileName.Text != m_SettingsFileName.Text)
			{
				try
				{
					var fi = new FileInfo(m_SaveAsFileName.Text);
					if (!fi.Exists)
					{
						// make sure we can create the file for future use
						using (var s2 = new FileStream(m_SaveAsFileName.Text, FileMode.Create))
						{
							s2.Close();
						}
						fi.Delete();
					}
				}
				catch
				{
					if (!runSilent)
					{
						var msg = string.Format(LexTextControls.ksInvalidSettingsSaveFileX, m_SaveAsFileName.Text);
						MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					fStayHere = true;
					m_SaveAsFileName.Focus();
				}
			}
			else if (m_SaveAsFileName.Text.ToLowerInvariant() == m_DatabaseFileName.Text.ToLowerInvariant())
			{
				// We don't want to overwrite the database with the settings!  See LT-8126.
				if (!runSilent)
				{
					var msg = string.Format(LexTextControls.ksSettingsSaveFileSameAsDatabaseFile, m_SaveAsFileName.Text, m_DatabaseFileName.Text);
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
			}
			return fStayHere;
		}

		protected override void OnNextButton()
		{
			ShowSaveButtonOrNot();
			base.OnNextButton();
			// handle the case where they've entered a paseXoutput file
			switch (CurrentStepNumber)
			{
				case 2:
					var fStayHere = UsesInvalidFileNames(false);
					if (fStayHere)
					{
						// Don't go anywhere, stay right here.
						m_CurrentStepNumber = 1;    // 1-8
						tabSteps.SelectedIndex = 0; // 0-7
						UpdateStepLabel();
					}
					if (GetDictionaryFileAsPhaseFileNumber() > 0)
					{
						// we need to skip to the final step now, also handle back processing from there
						m_CurrentStepNumber = 7;    // 1-8
						tabSteps.SelectedIndex = 6; // 0-7
						UpdateStepLabel();
					}
					ReadOptionInfoFromMapFile();
					break;
				case 4:
					if (UpdateIfInputFileContentsChanged())
					{
						// don't go to the next page, stay here due to change
						NextButtonEnabled = true;   //  EnableNextButton();
						m_CurrentStepNumber = 3;

						return; // don't do the default processing
					}
					break;
			}

			NextButtonEnabled = EnableNextButton();
		}

		#endregion

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexImportWizard));
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.label1 = new System.Windows.Forms.Label();
			this.btnBackup = new System.Windows.Forms.Button();
			this.lblBackupInstructions = new System.Windows.Forms.Label();
			this.lblBackup = new System.Windows.Forms.Label();
			this.lblOverviewInstructions = new System.Windows.Forms.Label();
			this.lblOverview = new System.Windows.Forms.Label();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.lblSettingsTag = new System.Windows.Forms.Label();
			this.lblFile = new System.Windows.Forms.Label();
			this.m_SettingsFileName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.btnSaveAsBrowse = new System.Windows.Forms.Button();
			this.m_SaveAsFileName = new System.Windows.Forms.TextBox();
			this.lblSaveAs = new System.Windows.Forms.Label();
			this.lblSaveAsInstructions = new System.Windows.Forms.Label();
			this.btnSettingsBrowse = new System.Windows.Forms.Button();
			this.lblSettings = new System.Windows.Forms.Label();
			this.lblSettingsInstructions = new System.Windows.Forms.Label();
			this.btnDatabaseBrowse = new System.Windows.Forms.Button();
			this.m_DatabaseFileName = new System.Windows.Forms.TextBox();
			this.lblDatabase = new System.Windows.Forms.Label();
			this.lblDatabaseInstructions = new System.Windows.Forms.Label();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.btnModifyMappingLanguage = new System.Windows.Forms.Button();
			this.btnAddMappingLanguage = new System.Windows.Forms.Button();
			this.listViewMappingLanguages = new System.Windows.Forms.ListView();
			this.LangcolumnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LangcolumnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LangcolumnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.lblMappingLanguagesInstructions = new System.Windows.Forms.Label();
			this.lblMappingLanguages = new System.Windows.Forms.Label();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.m_chkCreateMissingLinks = new System.Windows.Forms.CheckBox();
			this.lblTotalMarkers = new System.Windows.Forms.Label();
			this.btnModifyContentMapping = new System.Windows.Forms.Button();
			this.lblContentInstructions2 = new System.Windows.Forms.Label();
			this.lblContentInstructions1 = new System.Windows.Forms.Label();
			this.lblContentMappings = new System.Windows.Forms.Label();
			this.listViewContentMapping = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.tabPage6 = new System.Windows.Forms.TabPage();
			this.btnDeleteCharMapping = new System.Windows.Forms.Button();
			this.btnModifyCharMapping = new System.Windows.Forms.Button();
			this.btnAddCharMapping = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.listViewCharMappings = new System.Windows.Forms.ListView();
			this.columnHeaderCM1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderCM2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderCM3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderCM4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.tabPage5 = new System.Windows.Forms.TabPage();
			this.tvBeginMarkers = new System.Windows.Forms.TreeView();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.lblStep5KeyMarkers = new System.Windows.Forms.Label();
			this.labelStep5Description = new System.Windows.Forms.Label();
			this.tabPage7 = new System.Windows.Forms.TabPage();
			this.lblGenerateReportMsg = new System.Windows.Forms.Label();
			this.btnGenerateReport = new System.Windows.Forms.Button();
			this.FeasabilityCheckInstructions = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.gbGenerateReport = new System.Windows.Forms.GroupBox();
			this.tabPage8 = new System.Windows.Forms.TabPage();
			this.lblFinishWOImport = new System.Windows.Forms.Label();
			this.m_DisplayImportReport = new System.Windows.Forms.CheckBox();
			this.lblReadyToImportInstructions = new System.Windows.Forms.Label();
			this.lblReadyToImport = new System.Windows.Forms.Label();
			this.btnQuickFinish = new System.Windows.Forms.Button();
			this.btnSaveMapFile = new System.Windows.Forms.Button();
			this.tabSteps.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.tabPage6.SuspendLayout();
			this.tabPage5.SuspendLayout();
			this.tabPage7.SuspendLayout();
			this.tabPage8.SuspendLayout();
			this.SuspendLayout();
			//
			// panSteps
			//
			resources.ApplyResources(this.panSteps, "panSteps");
			//
			// lblSteps
			//
			resources.ApplyResources(this.lblSteps, "lblSteps");
			//
			// m_btnBack
			//
			resources.ApplyResources(this.m_btnBack, "m_btnBack");
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			//
			// m_btnNext
			//
			resources.ApplyResources(this.m_btnNext, "m_btnNext");
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			//
			// tabSteps
			//
			resources.ApplyResources(this.tabSteps, "tabSteps");
			this.tabSteps.Controls.Add(this.tabPage1);
			this.tabSteps.Controls.Add(this.tabPage2);
			this.tabSteps.Controls.Add(this.tabPage3);
			this.tabSteps.Controls.Add(this.tabPage4);
			this.tabSteps.Controls.Add(this.tabPage5);
			this.tabSteps.Controls.Add(this.tabPage6);
			this.tabSteps.Controls.Add(this.tabPage7);
			this.tabSteps.Controls.Add(this.tabPage8);
			//
			// tabPage1
			//
			resources.ApplyResources(this.tabPage1, "tabPage1");
			this.tabPage1.Controls.Add(this.label1);
			this.tabPage1.Controls.Add(this.btnBackup);
			this.tabPage1.Controls.Add(this.lblBackupInstructions);
			this.tabPage1.Controls.Add(this.lblBackup);
			this.tabPage1.Controls.Add(this.lblOverviewInstructions);
			this.tabPage1.Controls.Add(this.lblOverview);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.UseVisualStyleBackColor = true;
			//
			// label1
			//
			this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// btnBackup
			//
			resources.ApplyResources(this.btnBackup, "btnBackup");
			this.btnBackup.Name = "btnBackup";
			this.btnBackup.Click += new System.EventHandler(this.btnBackup_Click);
			//
			// lblBackupInstructions
			//
			resources.ApplyResources(this.lblBackupInstructions, "lblBackupInstructions");
			this.lblBackupInstructions.Name = "lblBackupInstructions";
			//
			// lblBackup
			//
			resources.ApplyResources(this.lblBackup, "lblBackup");
			this.lblBackup.Name = "lblBackup";
			//
			// lblOverviewInstructions
			//
			resources.ApplyResources(this.lblOverviewInstructions, "lblOverviewInstructions");
			this.lblOverviewInstructions.Name = "lblOverviewInstructions";
			//
			// lblOverview
			//
			resources.ApplyResources(this.lblOverview, "lblOverview");
			this.lblOverview.Name = "lblOverview";
			//
			// tabPage2
			//
			resources.ApplyResources(this.tabPage2, "tabPage2");
			this.tabPage2.Controls.Add(this.lblSettingsTag);
			this.tabPage2.Controls.Add(this.lblFile);
			this.tabPage2.Controls.Add(this.m_SettingsFileName);
			this.tabPage2.Controls.Add(this.btnSaveAsBrowse);
			this.tabPage2.Controls.Add(this.m_SaveAsFileName);
			this.tabPage2.Controls.Add(this.lblSaveAs);
			this.tabPage2.Controls.Add(this.lblSaveAsInstructions);
			this.tabPage2.Controls.Add(this.btnSettingsBrowse);
			this.tabPage2.Controls.Add(this.lblSettings);
			this.tabPage2.Controls.Add(this.lblSettingsInstructions);
			this.tabPage2.Controls.Add(this.btnDatabaseBrowse);
			this.tabPage2.Controls.Add(this.m_DatabaseFileName);
			this.tabPage2.Controls.Add(this.lblDatabase);
			this.tabPage2.Controls.Add(this.lblDatabaseInstructions);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.UseVisualStyleBackColor = true;
			//
			// lblSettingsTag
			//
			resources.ApplyResources(this.lblSettingsTag, "lblSettingsTag");
			this.lblSettingsTag.Name = "lblSettingsTag";
			//
			// lblFile
			//
			resources.ApplyResources(this.lblFile, "lblFile");
			this.lblFile.Name = "lblFile";
			//
			// m_SettingsFileName
			//
			resources.ApplyResources(this.m_SettingsFileName, "m_SettingsFileName");
			this.m_SettingsFileName.AllowSpaceInEditBox = true;
			this.m_SettingsFileName.Name = "m_SettingsFileName";
			this.m_SettingsFileName.TextChanged += new System.EventHandler(this.m_SettingsFileName_TextChanged);
			//
			// btnSaveAsBrowse
			//
			resources.ApplyResources(this.btnSaveAsBrowse, "btnSaveAsBrowse");
			this.btnSaveAsBrowse.Name = "btnSaveAsBrowse";
			this.btnSaveAsBrowse.Tag = "";
			this.btnSaveAsBrowse.Click += new System.EventHandler(this.btnSaveAsBrowse_Click);
			//
			// m_SaveAsFileName
			//
			resources.ApplyResources(this.m_SaveAsFileName, "m_SaveAsFileName");
			this.m_SaveAsFileName.Name = "m_SaveAsFileName";
			this.m_SaveAsFileName.TextChanged += new System.EventHandler(this.m_SaveAsFileName_TextChanged);
			//
			// lblSaveAs
			//
			resources.ApplyResources(this.lblSaveAs, "lblSaveAs");
			this.lblSaveAs.Name = "lblSaveAs";
			//
			// lblSaveAsInstructions
			//
			resources.ApplyResources(this.lblSaveAsInstructions, "lblSaveAsInstructions");
			this.lblSaveAsInstructions.Name = "lblSaveAsInstructions";
			//
			// btnSettingsBrowse
			//
			resources.ApplyResources(this.btnSettingsBrowse, "btnSettingsBrowse");
			this.btnSettingsBrowse.Name = "btnSettingsBrowse";
			this.btnSettingsBrowse.Tag = "";
			this.btnSettingsBrowse.Click += new System.EventHandler(this.btnSettingsBrowse_Click);
			//
			// lblSettings
			//
			resources.ApplyResources(this.lblSettings, "lblSettings");
			this.lblSettings.Name = "lblSettings";
			//
			// lblSettingsInstructions
			//
			resources.ApplyResources(this.lblSettingsInstructions, "lblSettingsInstructions");
			this.lblSettingsInstructions.Name = "lblSettingsInstructions";
			//
			// btnDatabaseBrowse
			//
			resources.ApplyResources(this.btnDatabaseBrowse, "btnDatabaseBrowse");
			this.btnDatabaseBrowse.Name = "btnDatabaseBrowse";
			this.btnDatabaseBrowse.Tag = "";
			this.btnDatabaseBrowse.Click += new System.EventHandler(this.btnDatabaseBrowse_Click);
			//
			// m_DatabaseFileName
			//
			resources.ApplyResources(this.m_DatabaseFileName, "m_DatabaseFileName");
			this.m_DatabaseFileName.Name = "m_DatabaseFileName";
			this.m_DatabaseFileName.TextChanged += new System.EventHandler(this.m_DatabaseFileName_TextChanged);
			//
			// lblDatabase
			//
			resources.ApplyResources(this.lblDatabase, "lblDatabase");
			this.lblDatabase.Name = "lblDatabase";
			//
			// lblDatabaseInstructions
			//
			resources.ApplyResources(this.lblDatabaseInstructions, "lblDatabaseInstructions");
			this.lblDatabaseInstructions.Name = "lblDatabaseInstructions";
			//
			// tabPage3
			//
			resources.ApplyResources(this.tabPage3, "tabPage3");
			this.tabPage3.Controls.Add(this.btnModifyMappingLanguage);
			this.tabPage3.Controls.Add(this.btnAddMappingLanguage);
			this.tabPage3.Controls.Add(this.listViewMappingLanguages);
			this.tabPage3.Controls.Add(this.lblMappingLanguagesInstructions);
			this.tabPage3.Controls.Add(this.lblMappingLanguages);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.UseVisualStyleBackColor = true;
			//
			// btnModifyMappingLanguage
			//
			resources.ApplyResources(this.btnModifyMappingLanguage, "btnModifyMappingLanguage");
			this.btnModifyMappingLanguage.Name = "btnModifyMappingLanguage";
			this.btnModifyMappingLanguage.Click += new System.EventHandler(this.btnModifyMappingLanguage_Click);
			//
			// btnAddMappingLanguage
			//
			resources.ApplyResources(this.btnAddMappingLanguage, "btnAddMappingLanguage");
			this.btnAddMappingLanguage.Name = "btnAddMappingLanguage";
			this.btnAddMappingLanguage.Click += new System.EventHandler(this.btnAddMappingLanguage_Click);
			//
			// listViewMappingLanguages
			//
			resources.ApplyResources(this.listViewMappingLanguages, "listViewMappingLanguages");
			this.listViewMappingLanguages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
				this.LangcolumnHeader1,
				this.LangcolumnHeader2,
				this.LangcolumnHeader3});
			this.listViewMappingLanguages.FullRowSelect = true;
			this.listViewMappingLanguages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listViewMappingLanguages.HideSelection = false;
			this.listViewMappingLanguages.MultiSelect = false;
			this.listViewMappingLanguages.Name = "listViewMappingLanguages";
			this.listViewMappingLanguages.UseCompatibleStateImageBehavior = false;
			this.listViewMappingLanguages.View = System.Windows.Forms.View.Details;
			this.listViewMappingLanguages.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewMappingLanguages_ColumnClick);
			this.listViewMappingLanguages.SelectedIndexChanged += new System.EventHandler(this.listViewMappingLanguages_SelectedIndexChanged);
			this.listViewMappingLanguages.DoubleClick += new System.EventHandler(this.listViewMappingLanguages_DoubleClick);
			//
			// LangcolumnHeader1
			//
			resources.ApplyResources(this.LangcolumnHeader1, "LangcolumnHeader1");
			//
			// LangcolumnHeader2
			//
			resources.ApplyResources(this.LangcolumnHeader2, "LangcolumnHeader2");
			//
			// LangcolumnHeader3
			//
			resources.ApplyResources(this.LangcolumnHeader3, "LangcolumnHeader3");
			//
			// lblMappingLanguagesInstructions
			//
			resources.ApplyResources(this.lblMappingLanguagesInstructions, "lblMappingLanguagesInstructions");
			this.lblMappingLanguagesInstructions.Name = "lblMappingLanguagesInstructions";
			//
			// lblMappingLanguages
			//
			resources.ApplyResources(this.lblMappingLanguages, "lblMappingLanguages");
			this.lblMappingLanguages.Name = "lblMappingLanguages";
			//
			// tabPage4
			//
			resources.ApplyResources(this.tabPage4, "tabPage4");
			this.tabPage4.Controls.Add(this.m_chkCreateMissingLinks);
			this.tabPage4.Controls.Add(this.lblTotalMarkers);
			this.tabPage4.Controls.Add(this.btnModifyContentMapping);
			this.tabPage4.Controls.Add(this.lblContentInstructions2);
			this.tabPage4.Controls.Add(this.lblContentInstructions1);
			this.tabPage4.Controls.Add(this.lblContentMappings);
			this.tabPage4.Controls.Add(this.listViewContentMapping);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.UseVisualStyleBackColor = true;
			//
			// m_chkCreateMissingLinks
			//
			resources.ApplyResources(this.m_chkCreateMissingLinks, "m_chkCreateMissingLinks");
			this.m_chkCreateMissingLinks.Name = "m_chkCreateMissingLinks";
			this.m_chkCreateMissingLinks.UseVisualStyleBackColor = true;
			//
			// lblTotalMarkers
			//
			resources.ApplyResources(this.lblTotalMarkers, "lblTotalMarkers");
			this.lblTotalMarkers.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblTotalMarkers.Name = "lblTotalMarkers";
			//
			// btnModifyContentMapping
			//
			resources.ApplyResources(this.btnModifyContentMapping, "btnModifyContentMapping");
			this.btnModifyContentMapping.Name = "btnModifyContentMapping";
			this.btnModifyContentMapping.Click += new System.EventHandler(this.btnModifyContentMapping_Click);
			//
			// lblContentInstructions2
			//
			resources.ApplyResources(this.lblContentInstructions2, "lblContentInstructions2");
			this.lblContentInstructions2.Name = "lblContentInstructions2";
			//
			// lblContentInstructions1
			//
			resources.ApplyResources(this.lblContentInstructions1, "lblContentInstructions1");
			this.lblContentInstructions1.Name = "lblContentInstructions1";
			//
			// lblContentMappings
			//
			resources.ApplyResources(this.lblContentMappings, "lblContentMappings");
			this.lblContentMappings.Name = "lblContentMappings";
			//
			// listViewContentMapping
			//
			resources.ApplyResources(this.listViewContentMapping, "listViewContentMapping");
			this.listViewContentMapping.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
				this.columnHeader1,
				this.columnHeader6,
				this.columnHeader5,
				this.columnHeader2,
				this.columnHeader3,
				this.columnHeader4});
			this.listViewContentMapping.FullRowSelect = true;
			this.listViewContentMapping.HideSelection = false;
			this.listViewContentMapping.MultiSelect = false;
			this.listViewContentMapping.Name = "listViewContentMapping";
			this.listViewContentMapping.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.listViewContentMapping.UseCompatibleStateImageBehavior = false;
			this.listViewContentMapping.View = System.Windows.Forms.View.Details;
			this.listViewContentMapping.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewContentMapping_ColumnClick);
			this.listViewContentMapping.SelectedIndexChanged += new System.EventHandler(this.listViewContentMapping_SelectedIndexChanged);
			this.listViewContentMapping.DoubleClick += new System.EventHandler(this.listViewContentMapping_DoubleClick);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader6
			//
			resources.ApplyResources(this.columnHeader6, "columnHeader6");
			//
			// columnHeader5
			//
			resources.ApplyResources(this.columnHeader5, "columnHeader5");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// columnHeader3
			//
			resources.ApplyResources(this.columnHeader3, "columnHeader3");
			//
			// columnHeader4
			//
			resources.ApplyResources(this.columnHeader4, "columnHeader4");
			//
			// tabPage6
			//
			resources.ApplyResources(this.tabPage6, "tabPage6");
			this.tabPage6.Controls.Add(this.btnDeleteCharMapping);
			this.tabPage6.Controls.Add(this.btnModifyCharMapping);
			this.tabPage6.Controls.Add(this.btnAddCharMapping);
			this.tabPage6.Controls.Add(this.label2);
			this.tabPage6.Controls.Add(this.label4);
			this.tabPage6.Controls.Add(this.listViewCharMappings);
			this.tabPage6.Name = "tabPage6";
			this.tabPage6.UseVisualStyleBackColor = true;
			//
			// btnDeleteCharMapping
			//
			resources.ApplyResources(this.btnDeleteCharMapping, "btnDeleteCharMapping");
			this.btnDeleteCharMapping.Name = "btnDeleteCharMapping";
			this.btnDeleteCharMapping.Click += new System.EventHandler(this.btnDeleteCharMapping_Click);
			//
			// btnModifyCharMapping
			//
			resources.ApplyResources(this.btnModifyCharMapping, "btnModifyCharMapping");
			this.btnModifyCharMapping.Name = "btnModifyCharMapping";
			this.btnModifyCharMapping.Click += new System.EventHandler(this.btnModifyCharMapping_Click);
			//
			// btnAddCharMapping
			//
			resources.ApplyResources(this.btnAddCharMapping, "btnAddCharMapping");
			this.btnAddCharMapping.Name = "btnAddCharMapping";
			this.btnAddCharMapping.Click += new System.EventHandler(this.btnAddCharMapping_Click);
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// listViewCharMappings
			//
			this.listViewCharMappings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
				this.columnHeaderCM1,
				this.columnHeaderCM2,
				this.columnHeaderCM3,
				this.columnHeaderCM4});
			this.listViewCharMappings.FullRowSelect = true;
			this.listViewCharMappings.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listViewCharMappings.HideSelection = false;
			resources.ApplyResources(this.listViewCharMappings, "listViewCharMappings");
			this.listViewCharMappings.MultiSelect = false;
			this.listViewCharMappings.Name = "listViewCharMappings";
			this.listViewCharMappings.UseCompatibleStateImageBehavior = false;
			this.listViewCharMappings.View = System.Windows.Forms.View.Details;
			this.listViewCharMappings.SelectedIndexChanged += new System.EventHandler(this.listViewCharMappings_SelectedIndexChanged);
			this.listViewCharMappings.DoubleClick += new System.EventHandler(this.listViewCharMappings_DoubleClick);
			//
			// columnHeaderCM1
			//
			resources.ApplyResources(this.columnHeaderCM1, "columnHeaderCM1");
			//
			// columnHeaderCM2
			//
			resources.ApplyResources(this.columnHeaderCM2, "columnHeaderCM2");
			//
			// columnHeaderCM3
			//
			resources.ApplyResources(this.columnHeaderCM3, "columnHeaderCM3");
			//
			// columnHeaderCM4
			//
			resources.ApplyResources(this.columnHeaderCM4, "columnHeaderCM4");
			//
			// tabPage5
			//
			resources.ApplyResources(this.tabPage5, "tabPage5");
			this.tabPage5.Controls.Add(this.tvBeginMarkers);
			this.tabPage5.Controls.Add(this.lblStep5KeyMarkers);
			this.tabPage5.Controls.Add(this.labelStep5Description);
			this.tabPage5.Name = "tabPage5";
			this.tabPage5.UseVisualStyleBackColor = true;
			//
			// tvBeginMarkers
			//
			resources.ApplyResources(this.tvBeginMarkers, "tvBeginMarkers");
			this.tvBeginMarkers.ImageList = this.imageList1;
			this.tvBeginMarkers.ItemHeight = 16;
			this.tvBeginMarkers.Name = "tvBeginMarkers";
			this.tvBeginMarkers.ShowLines = false;
			this.tvBeginMarkers.ShowRootLines = false;
			this.tvBeginMarkers.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvBeginMarkers_BeforeCollapse);
			this.tvBeginMarkers.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tvBeginMarkers_KeyUp);
			this.tvBeginMarkers.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tvBeginMarkers_MouseUp);
			//
			// imageList1
			//
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList1.Images.SetKeyName(0, "CheckedBox");
			this.imageList1.Images.SetKeyName(1, "Bullet");
			this.imageList1.Images.SetKeyName(2, "CheckBox");
			//
			// lblStep5KeyMarkers
			//
			resources.ApplyResources(this.lblStep5KeyMarkers, "lblStep5KeyMarkers");
			this.lblStep5KeyMarkers.Name = "lblStep5KeyMarkers";
			//
			// labelStep5Description
			//
			resources.ApplyResources(this.labelStep5Description, "labelStep5Description");
			this.labelStep5Description.Name = "labelStep5Description";
			//
			// tabPage7
			//
			resources.ApplyResources(this.tabPage7, "tabPage7");
			this.tabPage7.Controls.Add(this.lblGenerateReportMsg);
			this.tabPage7.Controls.Add(this.btnGenerateReport);
			this.tabPage7.Controls.Add(this.FeasabilityCheckInstructions);
			this.tabPage7.Controls.Add(this.label3);
			this.tabPage7.Controls.Add(this.gbGenerateReport);
			this.tabPage7.Name = "tabPage7";
			this.tabPage7.UseVisualStyleBackColor = true;
			//
			// lblGenerateReportMsg
			//
			resources.ApplyResources(this.lblGenerateReportMsg, "lblGenerateReportMsg");
			this.lblGenerateReportMsg.Name = "lblGenerateReportMsg";
			//
			// btnGenerateReport
			//
			resources.ApplyResources(this.btnGenerateReport, "btnGenerateReport");
			this.btnGenerateReport.Name = "btnGenerateReport";
			this.btnGenerateReport.Click += new System.EventHandler(this.btnGenerateReport_Click);
			//
			// FeasabilityCheckInstructions
			//
			resources.ApplyResources(this.FeasabilityCheckInstructions, "FeasabilityCheckInstructions");
			this.FeasabilityCheckInstructions.Name = "FeasabilityCheckInstructions";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// gbGenerateReport
			//
			resources.ApplyResources(this.gbGenerateReport, "gbGenerateReport");
			this.gbGenerateReport.Name = "gbGenerateReport";
			this.gbGenerateReport.TabStop = false;
			//
			// tabPage8
			//
			resources.ApplyResources(this.tabPage8, "tabPage8");
			this.tabPage8.Controls.Add(this.lblFinishWOImport);
			this.tabPage8.Controls.Add(this.m_DisplayImportReport);
			this.tabPage8.Controls.Add(this.lblReadyToImportInstructions);
			this.tabPage8.Controls.Add(this.lblReadyToImport);
			this.tabPage8.Name = "tabPage8";
			this.tabPage8.UseVisualStyleBackColor = true;
			//
			// lblFinishWOImport
			//
			this.lblFinishWOImport.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.lblFinishWOImport, "lblFinishWOImport");
			this.lblFinishWOImport.ForeColor = System.Drawing.SystemColors.ActiveCaption;
			this.lblFinishWOImport.Name = "lblFinishWOImport";
			//
			// m_DisplayImportReport
			//
			resources.ApplyResources(this.m_DisplayImportReport, "m_DisplayImportReport");
			this.m_DisplayImportReport.Checked = true;
			this.m_DisplayImportReport.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_DisplayImportReport.Name = "m_DisplayImportReport";
			this.m_DisplayImportReport.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_DisplayImportReport_KeyDown);
			this.m_DisplayImportReport.KeyUp += new System.Windows.Forms.KeyEventHandler(this.m_DisplayImportReport_KeyUp);
			//
			// lblReadyToImportInstructions
			//
			resources.ApplyResources(this.lblReadyToImportInstructions, "lblReadyToImportInstructions");
			this.lblReadyToImportInstructions.Name = "lblReadyToImportInstructions";
			//
			// lblReadyToImport
			//
			resources.ApplyResources(this.lblReadyToImport, "lblReadyToImport");
			this.lblReadyToImport.Name = "lblReadyToImport";
			//
			// btnQuickFinish
			//
			resources.ApplyResources(this.btnQuickFinish, "btnQuickFinish");
			this.btnQuickFinish.Name = "btnQuickFinish";
			this.btnQuickFinish.Click += new System.EventHandler(this.btnQuickFinish_Click);
			//
			// btnSaveMapFile
			//
			resources.ApplyResources(this.btnSaveMapFile, "btnSaveMapFile");
			this.btnSaveMapFile.Name = "btnSaveMapFile";
			this.btnSaveMapFile.Click += new System.EventHandler(this.btnSaveMapFile_Click);
			//
			// LexImportWizard
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.btnSaveMapFile);
			this.Controls.Add(this.btnQuickFinish);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			this.Name = "LexImportWizard";
			this.ShowInTaskbar = false;
			this.StepNames = new string[] {
				resources.GetString("$this.StepNames"),
				resources.GetString("$this.StepNames1"),
				resources.GetString("$this.StepNames2"),
				resources.GetString("$this.StepNames3"),
				resources.GetString("$this.StepNames4"),
				resources.GetString("$this.StepNames5"),
				resources.GetString("$this.StepNames6"),
				resources.GetString("$this.StepNames7")};
			this.StepPageCount = 8;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.LexImportWizard_Closing);
			this.Load += new System.EventHandler(this.LexImportWizard_Load);
			this.Controls.SetChildIndex(this.btnQuickFinish, 0);
			this.Controls.SetChildIndex(this.panSteps, 0);
			this.Controls.SetChildIndex(this.tabSteps, 0);
			this.Controls.SetChildIndex(this.lblSteps, 0);
			this.Controls.SetChildIndex(this.m_btnNext, 0);
			this.Controls.SetChildIndex(this.m_btnCancel, 0);
			this.Controls.SetChildIndex(this.m_btnBack, 0);
			this.Controls.SetChildIndex(this.m_btnHelp, 0);
			this.Controls.SetChildIndex(this.btnSaveMapFile, 0);
			this.tabSteps.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.tabPage3.ResumeLayout(false);
			this.tabPage4.ResumeLayout(false);
			this.tabPage4.PerformLayout();
			this.tabPage6.ResumeLayout(false);
			this.tabPage5.ResumeLayout(false);
			this.tabPage7.ResumeLayout(false);
			this.tabPage8.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		// We don't allow these to be collapsed
		void tvBeginMarkers_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
		{
			e.Cancel = true;
		}

		void tvBeginMarkers_KeyUp(object obj, KeyEventArgs kea)
		{
			var tv = (TreeView)obj;
			var tn = tv.SelectedNode;
			if (kea.KeyCode == Keys.Space)
			{
				tvBeginMarkers_HandleCheckBoxNode(tn);
			}
		}

		void tvBeginMarkers_MouseUp(object obj, MouseEventArgs mea)
		{
			if (mea.Button != MouseButtons.Left)
			{
				return;
			}
			var tv = (TreeView)obj;
			var tn = tv.GetNodeAt(mea.X, mea.Y);
			if (tn == null)
			{
				return;
			}
			var rec = tn.Bounds;
			rec.X += -18;       // include the image bitmap (16 pixels plus 2 pixels between the image and the text)
			rec.Width += 18;
			if (rec.Contains(mea.X, mea.Y))
			{
				tvBeginMarkers_HandleCheckBoxNode(tn);
			}
		}

		void tvBeginMarkers_HandleCheckBoxNode(TreeNode tn)
		{
			// parent node, don't allow user to change the checked status
			if (tn.Tag == null)
			{
				return;
			}

			if (tn.Checked)
			{
				tn.Checked = false;
				tn.ImageKey = tn.SelectedImageKey = "CheckBox";
			}
			else
			{
				tn.Checked = true;
				tn.ImageKey = tn.SelectedImageKey = "CheckedBox";
			}

			// Save this information selected begin marker or unselected begin marker into
			// the underlying data structure.
			var data = tn.Tag as ContentMapping;
			if (data != null)
			{
				data.IsBeginMarker = tn.Checked;

				if (!tn.Checked && HasCheckedChild(tn.Parent) == 0) // no sibling nodes selected either
				{
					tn.Parent.BackColor = Color.Gold;       // add attention to ones with out checked buttons
				}
				else
				{
					tn.Parent.BackColor = SystemColors.Window;
				}
			}

			// The checked status has changed, need to see if the next button should be enabled
			NextButtonEnabled = Step5NextButtonEnabled();
		}

		#region Process / show errors in the import process

		private string m_sPhase1HtmlReport;

		/// <summary>
		/// read and process the conversion log output data.
		/// </summary>
		/// <returns>true if there is a log file to show</returns>
		private bool ProcessPhase1Errors(string filename, string checkedMapFileName, int entries, bool show)
		{
			m_sPhase1HtmlReport = string.Empty;
			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(filename);
			}
			catch (XmlException e)
			{
				MessageBox.Show(string.Format(LexTextControls.ksCannotLoadPhase1OutputX, e.Message), LexTextControls.ksPhase1Error);
				return false;
			}
			// Added to catch the other exceptions that could be generated by non-xml related problems
			catch (SystemException e)
			{
				MessageBox.Show(string.Format(LexTextControls.ksCannotLoadPhase1OutputX, e.Message), LexTextControls.ksPhase1Error);
				return false;
			}

			var sNewLine = Environment.NewLine;
			var sMapFileMsg = string.Format(LexTextControls.ksMapFileWasX, checkedMapFileName);
			var sEntriesImportedMsg = string.Format(LexTextControls.ksXEntriesImported, entries);
			m_sPhase1HtmlReport = $"<p>{sMapFileMsg}{sNewLine}<h3>{sEntriesImportedMsg}</h3>{sNewLine}";

			var errorCount = ProcessErrorLogErrors(xmlMap, ref m_sPhase1HtmlReport);
			var warningCount = ProcessErrorLogWarnings(xmlMap, ref m_sPhase1HtmlReport);
			ProcessErrorLogCautions(xmlMap, ref m_sPhase1HtmlReport);
			ProcessErrorLogSfmInfo(xmlMap, ref m_sPhase1HtmlReport);

			if (show)
			{
				var bldr = new StringBuilder();
				if (entries == 1)
				{
					bldr.Append(LexTextControls.ks1EntryImported);
				}
				else
				{
					bldr.AppendFormat(LexTextControls.ksXEntriesImported, entries);
				}
				bldr.Append(sNewLine);
				if (errorCount > 0 && warningCount > 0)
				{
					if (errorCount == 1 && warningCount == 1)
					{
						bldr.AppendFormat(LexTextControls.ks1Error1WarningInX, m_processedInputFile);
					}
					else if (errorCount == 1)
					{
						bldr.AppendFormat(LexTextControls.ks1ErrorXWarningsInY, warningCount, m_processedInputFile);
					}
					else if (warningCount == 1)
					{
						bldr.AppendFormat(LexTextControls.ksXErrors1WarningInY, errorCount, m_processedInputFile);
					}
					else
					{
						bldr.AppendFormat(LexTextControls.ksXErrorsYWarningsInZ, errorCount, warningCount, m_processedInputFile);
					}
				}
				else if (errorCount > 0)
				{
					if (errorCount == 1)
					{
						bldr.AppendFormat(LexTextControls.ks1ErrorInX, m_processedInputFile);
					}
					else
					{
						bldr.AppendFormat(LexTextControls.ksXErrorsInY, errorCount, m_processedInputFile);
					}
				}
				else
				{
					if (warningCount == 1)
					{
						bldr.AppendFormat(LexTextControls.ks1WarningInX, m_processedInputFile);
					}
					else
					{
						bldr.AppendFormat(LexTextControls.ksXWarningsInY, warningCount, m_processedInputFile);
					}
				}
				bldr.Append(sNewLine);
				bldr.Append(LexTextControls.ksClickOnViewPreviewResults);
				MessageBox.Show(bldr.ToString(), LexTextControls.ksPreviewImportSummary);
			}
			// write the Html string to a file so that it can be displayed later at the
			// user's discretion.

			// create a string for storing the jscript html code for showing the link
			var script = LexImport.GetHtmlJavaScript();

			var sHtmlFile = Path.Combine(m_sTempDir, "ImportPreviewReport.htm");
			using (var sw = File.CreateText(sHtmlFile))
			{
				var sHeadInfo = string.Format(LexTextControls.ksImportPreviewResultsForX, m_processedInputFile);
				var unicodeTag = string.Empty; // only put out the tag if the data is unicode already
				if (IslxFieldAlreadyUnicode())
				{
					unicodeTag = "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />" + sNewLine + "  ";
				}
				var sHtml = string.Format("<html>{0}<head>{0}  {4}<title>{1}</title>{0}{2}{0}</head>{0}<body>{0}<h2>{1}</h2>{0}{3}</body>{0}</html>{0}", sNewLine, sHeadInfo, script, m_sPhase1HtmlReport, unicodeTag);
				sw.Write(sHtml);
				sw.Close();
			}
			return true;
		}

		private static int ProcessErrorLogErrors(XmlDocument xmlMap, ref string sHtml)
		{
			var errorCount = 0;
			var listedErrors = 0;   // only will be present if it's less than errorCount
			var errorsNode = xmlMap.SelectSingleNode("database/ErrorLog/Errors");
			if (errorsNode != null)
			{
				foreach (XmlAttribute Attribute in errorsNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							errorCount = Convert.ToInt32(Attribute.Value);
							break;
						case "listed":
							listedErrors = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (errorCount > 0)
				{
					var bldr = new StringBuilder();
					bldr.Append("<h3>");
					if (listedErrors > 0)
					{
						if (listedErrors == 1 && errorCount == 1)
						{
							// probably can't happen...
							bldr.AppendFormat(LexTextControls.ks1Error1Detailed);
						}
						else if (listedErrors == 1)
						{
							bldr.AppendFormat(LexTextControls.ksXErrors1Detailed, errorCount);
						}
						else if (errorCount == 1)
						{
							// probably can't happen...
							bldr.AppendFormat(LexTextControls.ks1ErrorXDetailed, listedErrors);
						}
						else
						{
							bldr.AppendFormat(LexTextControls.ksXErrorsYDetailed, errorCount, listedErrors);
						}
					}
					else if (errorCount == 1)
					{
						bldr.AppendFormat(LexTextControls.ks1Error);
					}
					else
					{
						bldr.AppendFormat(LexTextControls.ksXErrors, errorCount);
					}
					bldr.Append("</h3>");
					bldr.Append(System.Environment.NewLine);
					bldr.Append("<ul>");
					bldr.Append(System.Environment.NewLine);
					var errorList = errorsNode.SelectNodes("Error");
					foreach (XmlNode errorNode in errorList)
					{
						var fileName = string.Empty;
						var line = string.Empty;
						foreach (XmlAttribute attribute in errorNode.Attributes)
						{
							switch (attribute.Name)
							{
								case "File":
									fileName = attribute.Value.Replace(@"\", @"\\");
									break;
								case "Line":
									line = attribute.Value;
									break;
							}
						}
						if (fileName.Length > 0)
						{
							if (line.Length == 0)
							{
								line = "1";
							}
							fileName = fileName.Replace("'", "\\'");
							bldr.Append("  <li><A HREF=\"javascript: void 0\"");
							bldr.Append(Environment.NewLine);
							bldr.AppendFormat("ONCLICK=\"exec('{0} -g {1}'); return false;\" >{2}</A></li>", fileName, line, errorNode.InnerText);
						}
						else
						{
							bldr.AppendFormat("  <li>{0}</li>", errorNode.InnerText);
						}
						bldr.Append(Environment.NewLine);
					}
					bldr.Append("</ul>");
					bldr.Append(Environment.NewLine);

					sHtml += bldr.ToString();
				}
			}
			return errorCount;
		}

		private static int ProcessErrorLogWarnings(XmlDocument xmlMap, ref string sHtml)
		{
			var warningsNode = xmlMap.SelectSingleNode("database/ErrorLog/Warnings");
			if (warningsNode == null)
			{
				return 0;
			}
			var warningCount = 0;
			foreach (XmlAttribute Attribute in warningsNode.Attributes)
			{
				if (Attribute.Name == "count")
				{
					warningCount = Convert.ToInt32(Attribute.Value);
				}
			}
			if (warningCount == 0)
			{
				return warningCount;
			}
			var bldr = new StringBuilder();
			bldr.Append("<h3>");
			if (warningCount == 1)
			{
				bldr.AppendFormat(LexTextControls.ks1Warning);
			}
			else
			{
				bldr.AppendFormat(LexTextControls.ksXWarnings, warningCount);
			}
			bldr.Append("</h3>");
			bldr.Append(Environment.NewLine);
			bldr.Append("<ul>");
			bldr.Append(Environment.NewLine);
			var warningList = warningsNode.SelectNodes("Warning");
			foreach (XmlNode warningNode in warningList)
			{
				bldr.AppendFormat("  <li>{0}</li>{1}", warningNode.InnerText, Environment.NewLine);
			}
			bldr.Append("</ul>");
			bldr.Append(Environment.NewLine);

			sHtml += bldr.ToString();
			return warningCount;
		}

		private void ProcessErrorLogCautions(XmlDocument xmlMap, ref string sHtmlOUT)
		{
			var fileName = m_processedInputFile;
			fileName = fileName.Replace("'", "\\'");
			fileName = fileName.Replace("\\", "\\\\");
			var sHtml = new StringBuilder();
			var cautionCount = 0;
			var listedCautions = 0; // only will be present if it's less than errorCount
			var oooNode = xmlMap.SelectSingleNode("database/ErrorLog/OutOfOrder");
			if (oooNode != null)
			{
				foreach (XmlAttribute Attribute in oooNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							cautionCount = Convert.ToInt32(Attribute.Value);
							break;
						case "listed":
							listedCautions = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (cautionCount > 0)
				{
					sHtml.Append("<h3>");
					if (listedCautions > 0)
					{
						if (listedCautions == 1 && cautionCount == 1)
						{
							// probably can't happen.
							sHtml.Append(LexTextControls.ks1Entry1Detailed);
						}
						else if (listedCautions == 1)
						{
							sHtml.AppendFormat(LexTextControls.ksXEntries1Detailed, cautionCount);
						}
						else if (cautionCount == 1)
						{
							// probably can't happen
							sHtml.AppendFormat(LexTextControls.ks1EntryXDetailed, listedCautions);
						}
						else
						{
							sHtml.AppendFormat(LexTextControls.ksXEntriesYDetailed, cautionCount, listedCautions);
						}
					}
					else
					{
						if (cautionCount == 1)
						{
							sHtml.Append(LexTextControls.ks1EntryToReview);
						}
						else
						{
							sHtml.AppendFormat(LexTextControls.ksXEntriesToReview, cautionCount);
						}
					}
					sHtml.Append("</h3>");
					sHtml.Append(Environment.NewLine);

					var phase10utputName = Path.Combine(GetTempDir(), LexImport.s_sPhase1FileName);
					phase10utputName = phase10utputName.Replace(@"\", @"\\");

					sHtml.AppendFormat("<p>{0}", LexTextControls.ksMisorderedMarkers);
					sHtml.AppendFormat("<p>{0}", string.Format(LexTextControls.ksClickToPreviewAssumptions, "<A HREF=\"javascript: void 0\" ONCLICK=\"exec('" + phase10utputName + "'); return false;\" >", "</A>"));
					sHtml.AppendFormat("<p>{0}", LexTextControls.ksChoices_);
					sHtml.AppendFormat("<p>{0}", LexTextControls.ksWhyExaminePreview);
					sHtml.AppendFormat("<p>{0}", LexTextControls.ksWhyHowContinueImport);
					sHtml.Append("<ul>");
					var cautions = oooNode.SelectNodes("Caution");

					// Process each Caution element (each Entry)
					foreach (XmlNode cautionNode in cautions)
					{
						var entryName = string.Empty;
						foreach (XmlAttribute attribute in cautionNode.Attributes)
						{
							if (attribute.Name == "name")
							{
								entryName = attribute.Value;
							}
						}
						sHtml.AppendFormat("<li>{0}:   ", entryName);
						// Now process each Marker in the Entry
						var markers = cautionNode.SelectNodes("Marker");
						foreach (XmlNode markerNode in markers)
						{
							var markerName = string.Empty;
							foreach (XmlAttribute attribute in markerNode.Attributes)
							{
								if (attribute.Name == "name")
								{
									markerName = attribute.Value;
								}
							}
							sHtml.AppendFormat("{0}: ", markerName);
							var lines = new ArrayList();
							var lineNodes = markerNode.SelectNodes("Line");
							foreach (XmlNode lineNode in lineNodes)
							{
								foreach (XmlAttribute attribute in lineNode.Attributes)
								{
									if (attribute.Name == "value")
									{
										lines.Add(attribute.Value);
									}
								}
							}
							// put out the marker and the lines. EX: ab: 2,14,33,44
							for (var i = 0; i < lines.Count; i++)
							{
								sHtml.Append("<A HREF=\"javascript: void 0\"");
								sHtml.Append(Environment.NewLine);
								sHtml.AppendFormat("ONCLICK=\"exec('{0} -g {1}'); return false;\" >{1}</A>", fileName, lines[i]);
								if (i < lines.Count - 1)
								{
									sHtml.Append(",");
								}
							}
							sHtml.Append(" ; ");
						}
						sHtml.Append("</li>");
					}
					sHtml.Append("</ul>" + Environment.NewLine);
				}
			}
			sHtmlOUT += sHtml.ToString();
		}

		private static void ProcessErrorLogSfmInfo(XmlDocument xmlMap, ref string sHtml)
		{
			var infoNode = xmlMap.SelectSingleNode("database/ErrorLog/SfmInfoList");
			var infoList = infoNode?.SelectNodes("SfmInfo");
			if (!(infoList?.Count > 0))
			{
				return;
			}
			var bldr = new StringBuilder();
			bldr.AppendFormat("<h3>{0}</h3>{1}", LexTextControls.ksStatsForSFMarkers, System.Environment.NewLine);
			bldr.Append("<table border=\"1\" cellpadding=\"2\" cellspacing=\"2\">");
			bldr.Append(Environment.NewLine);
			bldr.Append("<tbody>");
			bldr.Append(Environment.NewLine);
			bldr.AppendFormat("<tr><th>{0}</th><th>{1}</th><th>{2}</th><th>{3}</th></tr>",
				LexTextControls.ksMarker,
				LexTextControls.ksOccurrences,
				LexTextControls.ksEmpty,
				LexTextControls.ksContainsData_);
			bldr.Append(Environment.NewLine);

			var rgRows = new ArrayList();
			foreach (XmlNode node in infoList)
			{
				var sMarker = string.Empty;
				var sCount = "0";
				var sEmpty = "0";
				var sUsage = "0";
				foreach (XmlAttribute attr in node.Attributes)
				{
					switch (attr.Name)
					{
						case "sfm":
							sMarker = node.Attributes["sfm"].Value;
							break;
						case "ttlCount":
							sCount = node.Attributes["ttlCount"].Value;
							break;
						case "emptyCount":
							sEmpty = node.Attributes["emptyCount"].Value;
							break;
						case "usagePercent":
							sUsage = node.Attributes["usagePercent"].Value;
							break;
						default:
							break; // ignore the additional attributes
					}
				}
				var sRow = $"<tr><td>\\{sMarker}</td><td>{sCount}</td><td>{sEmpty}</td><td>{sUsage}</td></tr>";
				rgRows.Insert(rgRows.Count, sRow);
			}
			rgRows.Sort();
			for (var i = 0; i < rgRows.Count; ++i)
			{
				bldr.Append(i);
				bldr.Append(Environment.NewLine);
			}
			bldr.Append("</tbody>");
			bldr.Append(Environment.NewLine);
			bldr.Append("</table>");
			bldr.Append(Environment.NewLine);
			sHtml += bldr.ToString();
		}

		#endregion

		private void listViewMappingLanguages_DoubleClick(object sender, EventArgs e)
		{
			btnModifyMappingLanguage.PerformClick();    // same as pressing the modify button
		}

		private void btnBackup_Click(object sender, EventArgs e)
		{
			using (var dlg = new BackupProjectDlg(m_cache, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
			{
				dlg.ShowDialog(this);
			}
		}

		private void listViewContentMapping_DoubleClick(object sender, EventArgs e)
		{
			btnModifyContentMapping.PerformClick();
		}

		private void ShowFinishLabel()
		{
			if (CurrentStepNumber == 7 && (ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				lblFinishWOImport.Visible = true;
			}
			else
			{
				lblFinishWOImport.Visible = false;
			}
		}

		private void m_DisplayImportReport_KeyDown(object sender, KeyEventArgs e)
		{
			ShowFinishLabel();
		}

		private void m_DisplayImportReport_KeyUp(object sender, KeyEventArgs e)
		{
			ShowFinishLabel();
		}

		private bool m_QuickFinish;
		private int m_lastQuickFinishTab;
		private bool AllowQuickFinishButton()
		{
			if (m_CurrentStepNumber > 5)
			{
				if (btnQuickFinish.Visible)
				{
					btnQuickFinish.Visible = false;
				}
				return false;
			}

			// if we have a phase file OR we have a dict file and a map file, allow it
			if (m_isPhaseInputFile || (m_DatabaseFileName.Text.Length > 0 && m_SettingsFileName.Text.Length > 0))
			{
				if (File.Exists(m_DatabaseFileName.Text) && (m_isPhaseInputFile || File.Exists(m_SettingsFileName.Text)))
				{
					if (!btnQuickFinish.Visible)
					{
						btnQuickFinish.Visible = true;
					}
					return true;
				}
			}

			if (btnQuickFinish.Visible)
			{
				btnQuickFinish.Visible = false;
			}
			return false;
		}

		private void btnQuickFinish_Click(object sender, EventArgs e)
		{
			// don't continue if there are invliad file names / paths
			if (UsesInvalidFileNames(false))
			{
				return;
			}

			if (AllowQuickFinishButton())
			{
				m_lastQuickFinishTab = m_CurrentStepNumber; // save for later

				// before jumping we need to make sure all the data structures are populated
				//  for (near) future use.
				while (CurrentStepNumber <= 6)
				{
					EnableNextButton();
					m_CurrentStepNumber++;
				}

				if (GetDictionaryFileAsPhaseFileNumber() > 0)
				{
					m_CurrentStepNumber = 7;    // 1-8
					tabSteps.SelectedIndex = 7; // 0-7
				}
				else
				{
					m_CurrentStepNumber = 6;    // 1-8
					tabSteps.SelectedIndex = 6; // 0-7
				}

				// we need to skip to the final step now, also handle back processing from there
				m_QuickFinish = true;
				UpdateStepLabel();


				// used in the finial steps of importing the data
				m_processedInputFile = m_DatabaseFileName.Text;
				m_processedMapFile = m_SettingsFileName.Text;
				btnQuickFinish.Visible = false;

				// added to take care of LT-2967
				NextButtonEnabled = EnableNextButton();
			}
		}

		private void m_SaveAsFileName_TextChanged(object sender, EventArgs e)
		{
			if (CurrentStepNumber == 1)
			{
				NextButtonEnabled = EnableNextButton();
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			m_origSet = true;   // At this point the framework has adjusted the size
								// for the finial time, so lock it down.
								// JohnT: this seems to be necessary for correct drawing at 120dpi.
								// I don't fully understand why, but it seems the base class does some
								// critical repositioning of buttons. See LT-4675.
			OnResize(e);

			if (!Platform.IsMono)
			{
				return;
			}

			// This button moving logic works on mono.  At this point, the sizes of the
			// list views have settled down.  See FWNX-847.
			int minY = listViewMappingLanguages.Bottom + 7;
			if (btnAddMappingLanguage.Top < minY)
			{
				MoveButton(btnAddMappingLanguage, null, minY);
				MoveButton(btnModifyMappingLanguage, btnAddMappingLanguage, minY);
			}
			minY = listViewContentMapping.Bottom + 7;
			if (btnModifyContentMapping.Top < minY)
			{
				MoveButton(btnModifyContentMapping, null, minY);
			}
			minY = listViewCharMappings.Bottom + 7;
			if (btnAddCharMapping.Top < minY)
			{
				MoveButton(btnAddCharMapping, null, minY);
				MoveButton(btnModifyCharMapping, btnAddCharMapping, minY);
				MoveButton(btnDeleteCharMapping, btnModifyCharMapping, minY);
			}
		}

		private void MoveButton(Button btn, Button btnLeft, int y)
		{
			Debug.Assert(Platform.IsMono, "only needed on Linux");
			if (btnLeft == null)
			{
				btn.Location = new Point(btn.Left, y);
			}
			else
			{
				btn.Location = new Point(btnLeft.Right + 7, y);
			}
		}

		// This moving button logic has issues on mono. (and on Windows, if truth be told!)
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (Platform.IsMono)
			{
				return;
			}

			// The following code is added to handle the adjustment that the framework
			// makes 'at some point' in the start up process of this dialog to handle
			// cases where the dpi is > 96.
			// This code captures the initial size of the dlg when one of the controls
			// that is to be adjusted has been created.  From there the differences are
			// added to the controls until the sizing stops.  At that point it looks
			// good / normal again.
			if (m_origWidth == 0 && listViewContentMapping != null) // have size info
			{
				// capture the original size information of the dialog
				m_origWidth = Width;
				m_origHeight = Height;
			}
			else
			{
				if (m_origSet == false && Width != 0 && m_origWidth != 0)   // adjust sizes
				{
					var diffWidth = Width - m_origWidth;
					var diffHeight = Height - m_origHeight;

					// adjust the width and height of problematic controls at 120 dpi
					listViewContentMapping.Width += diffWidth;
					listViewContentMapping.Height += diffHeight;
					listViewMappingLanguages.Width += diffWidth;
					listViewMappingLanguages.Height += diffHeight;
					tvBeginMarkers.Width += diffWidth;
					tvBeginMarkers.Height += diffHeight;
					listViewCharMappings.Width += diffWidth;
					listViewCharMappings.Height += diffHeight;

					// move the buttons now in both x and y directions
					diffHeight += 3;    // still need a little more ...  LT-4918
					diffHeight += 7;    // ... and more now after style change ... LT-9941

					var buttonYCoord = Convert.ToInt32(listViewCharMappings.Height * 1.03);

					if (btnAddMappingLanguage.Location.Y >= buttonYCoord)
					{
						//this is for 90dpi (100% Windows 7 display setting)
						MoveButton(btnAddMappingLanguage, diffWidth, diffHeight);
						MoveButton(btnModifyMappingLanguage, diffWidth, diffHeight);
						MoveButton(btnModifyContentMapping, diffWidth, diffHeight);
						MoveButton(btnAddCharMapping, diffWidth, diffHeight);
						MoveButton(btnModifyCharMapping, diffWidth, diffHeight);
						MoveButton(btnDeleteCharMapping, diffWidth, diffHeight);
						MoveButton(m_chkCreateMissingLinks, diffWidth, diffHeight);
					}
					else
					{
						//this is for 120 dpi (125% Windows 7 display settings)
						//LT-11558
						MoveButton2(btnAddMappingLanguage, diffWidth, buttonYCoord);
						MoveButton2(btnModifyMappingLanguage, diffWidth, buttonYCoord);
						MoveButton2(btnModifyContentMapping, diffWidth, buttonYCoord);
						MoveButton2(btnAddCharMapping, diffWidth, buttonYCoord);
						MoveButton2(btnModifyCharMapping, diffWidth, buttonYCoord);
						MoveButton2(btnDeleteCharMapping, diffWidth, buttonYCoord);
						MoveButton2(m_chkCreateMissingLinks, diffWidth, buttonYCoord);
					}

					// update the 'original' size for future OnSize msgs
					m_origWidth = Width;
					m_origHeight = Height;
				}
			}
		}

		private void MoveButton(ButtonBase btn, int dw, int dh)
		{
			Debug.Assert(!Platform.IsMono, "only needed on Windows");
			var oldPoint = btn.Location;
			oldPoint.X += dw;
			oldPoint.Y += dh;
			btn.Location = oldPoint;
		}

		private void MoveButton2(ButtonBase btn, int dw, int YCoord)
		{
			Debug.Assert(!Platform.IsMono, "only needed on Windows");
			var oldPoint = btn.Location;
			oldPoint.X += dw;
			oldPoint.Y = YCoord;
			btn.Location = oldPoint;
		}

		private void btnSaveMapFile_Click(object sender, System.EventArgs e)
		{
			// LT-6620
			if (UsesInvalidFileNames(false))
			{
				return;
			}

			// save current information
			var curStep = CurrentStepNumber;
			var curTab = tabSteps.SelectedIndex;

			var nextState = NextButtonEnabled;
			// before saving we need to make sure all the data structures are populated
			while (CurrentStepNumber <= 6)
			{
				EnableNextButton();
				m_CurrentStepNumber++;
			}
			SaveSettings();

			// restore back to pre-saved state/page/tab...
			tabSteps.SelectedIndex = curStep;
			m_CurrentStepNumber = curTab;
			NextButtonEnabled = nextState;
			UpdateStepLabel();

			AllowQuickFinishButton();   // make it visible if needed, or hidden if not

			MessageBox.Show(this, string.Format(LexTextControls.ksMappingFileXUpdated, m_SaveAsFileName.Text), LexTextControls.ksSettingsSaved, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			// The wizard base class redraws the controls, so
			// move the cancel button after it's done ...
			m_btnCancel.Left = m_btnBack.Left - (m_btnCancel.Width + kdxpNextCancelButtonGap);

			if (listViewMappingLanguages == null)
			{
				return;
			}

			// make sure the controls that tend to 'float' when the resx is checked out -
			//  still have a good screen position and size
			listViewMappingLanguages.Width = tabSteps.Width - 40;
			listViewMappingLanguages.Height = lblSteps.Top - btnAddMappingLanguage.Height - listViewMappingLanguages.Top - 20;

			listViewContentMapping.Width = tabSteps.Width - 40;
			// LT-10904 added checkbox
			listViewContentMapping.Height = tabSteps.Bottom - btnModifyContentMapping.Height - m_chkCreateMissingLinks.Height - listViewContentMapping.Top - 20;
			var nudge = Platform.IsUnix ? 25 : 0;
			// LT-17974 Adjust layout on Linux/Mono so checkbox and modify button are not overlapping.
			listViewContentMapping.Height -= nudge;

			listViewCharMappings.Width = tabSteps.Width - 40;
			listViewCharMappings.Height = tabSteps.Bottom - btnModifyCharMapping.Height - listViewCharMappings.Top - 20;

			tvBeginMarkers.Width = tabSteps.Width - 40;
			tvBeginMarkers.Height = tabSteps.Bottom - tvBeginMarkers.Top - 20;

			var buttonSpace = btnDatabaseBrowse.Location.X - (m_DatabaseFileName.Location.X + m_DatabaseFileName.Width);
			FixControlWidth(lblDatabaseInstructions, tabSteps.Width - 9);
			FixControlLocation(btnDatabaseBrowse, tabSteps.Width - 9);
			FixControlWidth(m_DatabaseFileName, btnDatabaseBrowse.Left - buttonSpace);

			FixControlWidth(lblSettingsInstructions, tabSteps.Width - 9);
			FixControlLocation(btnSettingsBrowse, tabSteps.Width - 9);
			FixControlWidth(m_SettingsFileName, btnSettingsBrowse.Left - buttonSpace);

			FixControlWidth(lblSaveAsInstructions, tabSteps.Width - 9);
			FixControlLocation(btnSaveAsBrowse, tabSteps.Width - 9);
			FixControlWidth(m_SaveAsFileName, btnSaveAsBrowse.Left - buttonSpace);

			FixControlWidth(FeasabilityCheckInstructions, tabSteps.Width - 9);
		}

		private void FixControlWidth(Control control, int maxRight)
		{
			if (control.Right == maxRight)
			{
				return;
			}
			var oldWidth = control.Width;
			var newWidth = oldWidth + (maxRight - control.Right);
			if (newWidth > 0)
			{
				control.Width = newWidth;
			}
		}

		private void FixControlLocation(Control control, int maxRight)
		{
			if (control.Right == maxRight)
			{
				return;
			}
			var loc = new Point(control.Location.X + (maxRight - control.Right), control.Location.Y);
			if (loc.X > 0)
			{
				control.Location = loc;
			}
		}

		private Hashtable ExtractExistingElementNames(bool fIgnoreSelected)
		{
			var names = new Hashtable();
			foreach (ListViewItem lvItem in listViewCharMappings.Items)
			{
				// skip the selected item in the list
				if (fIgnoreSelected && lvItem.Selected)
				{
					continue;
				}

				var marker = (ClsInFieldMarker)lvItem.Tag;
				if (names.ContainsKey(marker.ElementName) == false)
				{
					names.Add(marker.ElementName, null);
				}
			}
			return names;
		}

		private Hashtable ExtractExistingBeginMarkers(bool fIgnoreSelected)
		{
			var markers = new Hashtable();
			foreach (ListViewItem lvItem in listViewCharMappings.Items)
			{
				// skip the selected item in the list
				if (fIgnoreSelected && lvItem.Selected)
				{
					continue;
				}

				var marker = (ClsInFieldMarker)lvItem.Tag;
				if (markers.ContainsKey(marker.Begin) == false)
				{
					markers.Add(marker.Begin, null);
				}
			}
			return markers;
		}

		private Hashtable ExtractExistingEndMarkers(bool fIgnoreSelected)
		{
			var markers = new Hashtable();
			foreach (ListViewItem lvItem in listViewCharMappings.Items)
			{
				// skip the selected item in the list
				if (fIgnoreSelected && lvItem.Selected)
				{
					continue;
				}

				var marker = (ClsInFieldMarker)lvItem.Tag;
				foreach (string endMarker in marker.End)
				{
					if (markers.ContainsKey(endMarker) == false)
					{
						markers.Add(endMarker, null);
					}
				}
			}
			return markers;
		}

		private void btnAddCharMapping_Click(object sender, EventArgs e)
		{
			using (var dlg = new LexImportWizardCharMarkerDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_app, m_stylesheet))
			{
				dlg.Init(null, GetUILanguages(), m_cache);
				dlg.SetExistingBeginMarkers(ExtractExistingBeginMarkers(false));
				dlg.SetExistingEndMarkers(ExtractExistingEndMarkers(false));
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_dirtySenseLastSave = true;

					// now add the new item and then select it
					AddInLineMarker(dlg.IFM(), true);
					listViewCharMappings.Focus();
				}
			}
		}

		private void btnModifyCharMapping_Click(object sender, System.EventArgs e)
		{
			var selIndexes = listViewCharMappings.SelectedIndices;
			if (selIndexes.Count < 1 || selIndexes.Count > 1)
			{
				return; // only handle single selection at this time
			}

			var selIndex = selIndexes[0]; // only support 1
			var selectedIFM = (ClsInFieldMarker)listViewCharMappings.Items[selIndex].Tag;
			using (var dlg = new LexImportWizardCharMarkerDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_app, m_stylesheet))
			{
				dlg.Init(selectedIFM, GetUILanguages(), m_cache);
				dlg.SetExistingBeginMarkers(ExtractExistingBeginMarkers(true));
				dlg.SetExistingEndMarkers(ExtractExistingEndMarkers(true));
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_dirtySenseLastSave = true;
					// remove the old from the treeview display
					listViewCharMappings.Items[selIndex].Selected = false;
					listViewCharMappings.Items[selIndex].Focused = false;
					listViewCharMappings.Items.RemoveAt(selIndex);

					// now update the item and add it again and then select it
					AddInLineMarker(dlg.IFM(), true);
					listViewCharMappings.Focus();
				}
			}
		}

		private void btnDeleteCharMapping_Click(object sender, EventArgs e)
		{
			var selIndexes = listViewCharMappings.SelectedIndices;
			if (selIndexes.Count < 1 || selIndexes.Count > 1)
			{
				return; // only handle single selection at this time
			}

			var selIndex = selIndexes[0]; // only support 1
			m_dirtySenseLastSave = true;
			listViewCharMappings.Items.RemoveAt(selIndex);

			if (listViewCharMappings.Items.Count > 0)
			{
				if (listViewCharMappings.Items.Count <= selIndex)
				{
					selIndex--;
				}

				listViewCharMappings.Items[selIndex].Selected = true;
				listViewCharMappings.Items[selIndex].Focused = true;
			}
			SetCharMappingsButtons();
		}

		private void listViewCharMappings_DoubleClick(object sender, EventArgs e)
		{
			btnModifyCharMapping.PerformClick();
		}

		private void listViewCharMappings_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetCharMappingsButtons();
		}

		/// <summary>
		/// Called when the contents to the infield markers control change so that the
		/// modify and delete buttons can be enabled or disabled depending on if there
		/// are any selections selected.
		/// </summary>
		private void SetCharMappingsButtons()
		{
			var enableBtns = false;
			var selIndexes = listViewCharMappings.SelectedIndices;
			if (selIndexes.Count > 0)
			{
				enableBtns = true;
			}

			// only change the state if it's different - otherwise there can be a flashing in the GUI
			if (btnModifyCharMapping.Enabled != enableBtns)
			{
				btnModifyCharMapping.Enabled = enableBtns;
				btnDeleteCharMapping.Enabled = enableBtns;
			}
		}

		private LexImportCustomField FieldDescriptionToLexImportField(FieldDescription fd)
		{
			var sig = string.Empty;
			switch (fd.Type)
			{
				case CellarPropertyType.MultiUnicode:
					sig = "MultiUnicode";
					break;
				case CellarPropertyType.String:
					sig = "string";
					break;
				case CellarPropertyType.OwningAtomic:
					if (fd.DstCls == StTextTags.kClassId)
					{
						sig = "text";
					}
					break;
				case CellarPropertyType.ReferenceAtomic:
					if (fd.ListRootId != Guid.Empty)
					{
						sig = "ListRef";
					}
					break;
				case CellarPropertyType.ReferenceCollection:
					if (fd.ListRootId != Guid.Empty)
					{
						sig = "ListMultiRef";
					}
					break;
				// JohnT: added  GenDate and Numeric and Integer to prevent the crash in LT-11188.
				// Not sure these string values are actually used for anything; if they are, it might be a problem,
				// because I haven't been able to track down how or where they are used.
				case CellarPropertyType.GenDate:
					sig = "Date";
					break;
				case CellarPropertyType.Integer:
					sig = "Integer";
					break;
				case CellarPropertyType.Numeric:
					sig = "Number";
					break;
				default:
					throw new Exception("Error converting custom field to LexImportField - unexpected signature");
			}

			var lif = new LexImportCustomField(
				fd.Class,
				"NOT SURE YET _ Set In The Calling method???",
				fd.Id,
				fd.Big,
				fd.WsSelector,
				// end of custom specific field info
				fd.Name,
				fd.Userlabel,
				fd.Name,
				sig,
				sig.StartsWith("List"),
				true,
				false,
				"MDFVALUE");
			lif.ListRootId = fd.ListRootId;
			if (sig.StartsWith("List"))
			{
				lif.IsAbbrField = true;
			}
			return lif;
		}
	}
}
