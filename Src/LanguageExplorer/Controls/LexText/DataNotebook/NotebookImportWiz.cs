// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.Areas;
using Sfm2Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Utils;
using SilEncConverters40;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// This wizard steps the user through setting up to import a standard format anthropology
	/// database file (and then importing it).
	/// </summary>
	public partial class NotebookImportWiz : WizardDialog, IFwExtension
	{
		// Give names to the step numbers.
		const int kstepOverviewAndBackup = 1;
		const int kstepFileAndSettings = 2;
		const int kstepEncodingConversion = 3;
		const int kstepContentMapping = 4;
		const int kstepKeyMarkers = 5;
		const int kstepCharacterMapping = 6;
		const int kstepFinal = 7;

		private LcmCache m_cache;
		private IFwMetaDataCacheManaged m_mdc;
		private IVwStylesheet m_stylesheet;
		private WritingSystemManager m_wsManager;
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		private IStTextFactory m_factStText;
		private IStTextRepository m_repoStText;
		private IStTxtParaFactory m_factPara;
		private ICmPossibilityListRepository m_repoList;

		private bool m_fCanceling;
		private OpenFileDialogAdapter openFileDialog;

		private CmPossibilityCreator m_factPossibility;
		public CmPossibilityCreator PossibilityCreator => m_factPossibility ?? (m_factPossibility = new CmPossibilityCreator(m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>()));

		private CmAnthroItemCreator m_factAnthroItem;
		public CmAnthroItemCreator AnthroItemCreator => m_factAnthroItem ?? (m_factAnthroItem = new CmAnthroItemCreator(m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>()));

		private CmLocationCreator m_factLocation;
		public CmLocationCreator LocationCreator => m_factLocation ?? (m_factLocation = new CmLocationCreator(m_cache.ServiceLocator.GetInstance<ICmLocationFactory>()));

		private CmPersonCreator m_factPerson;
		public CmPersonCreator PersonCreator => m_factPerson ?? (m_factPerson = new CmPersonCreator(m_cache.ServiceLocator.GetInstance<ICmPersonFactory>()));

		private CmCustomItemCreator m_factCustomItem;
		public CmCustomItemCreator CustomItemCreator => m_factCustomItem ?? (m_factCustomItem = new CmCustomItemCreator(m_cache.ServiceLocator.GetInstance<ICmCustomItemFactory>()));

		private CmSemanticDomainCreator m_factSemanticDomain;
		public CmSemanticDomainCreator SemanticDomainCreator => m_factSemanticDomain ?? (m_factSemanticDomain = new CmSemanticDomainCreator(m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>()));
		private MoMorphTypeCreator m_factMorphType;
		public MoMorphTypeCreator MorphTypeCreator => m_factMorphType ?? (m_factMorphType = new MoMorphTypeCreator(m_cache.ServiceLocator.GetInstance<IMoMorphTypeFactory>()));
		private PartOfSpeechCreator m_factPartOfSpeech;
		public PartOfSpeechCreator NewPartOfSpeechCreator => m_factPartOfSpeech ?? (m_factPartOfSpeech = new PartOfSpeechCreator(m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>()));
		private LexEntryTypeCreator m_factLexEntryType;
		public LexEntryTypeCreator NewLexEntryTypeCreator => m_factLexEntryType ?? (m_factLexEntryType = new LexEntryTypeCreator(m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>()));
		private LexRefTypeCreator m_factLexRefType;
		public LexRefTypeCreator NewLexRefTypeCreator => m_factLexRefType ?? (m_factLexRefType = new LexRefTypeCreator(m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>()));

		public bool DirtySettings { get; set; }

		readonly List<PendingLink> m_pendingLinks = new List<PendingLink>();

		Dictionary<string, ICmPossibility> m_mapAnthroCode = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapConfidence = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapLocation = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapPhraseTag = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapPeople = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapRestriction = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapStatus = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapTimeOfDay = new Dictionary<string, ICmPossibility>();
		Dictionary<string, ICmPossibility> m_mapRecType = new Dictionary<string, ICmPossibility>();
		Dictionary<Guid, Dictionary<string, ICmPossibility>> m_mapListMapPossibilities = new Dictionary<Guid, Dictionary<string, ICmPossibility>>();

		List<ICmPossibility> m_rgNewAnthroItem = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewConfidence = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewLocation = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewPhraseTag = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewPeople = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewRestriction = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewStatus = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewTimeOfDay = new List<ICmPossibility>();
		List<ICmPossibility> m_rgNewRecType = new List<ICmPossibility>();
		Dictionary<Guid, List<ICmPossibility>> m_mapNewPossibilities = new Dictionary<Guid, List<ICmPossibility>>();

		private string m_sStdImportMap;
		private bool m_QuickFinish;
		private int m_lastQuickFinishTab;
		private string m_sFmtEncCnvLabel;

		DateTime m_dtStart;
		DateTime m_dtEnd;
		int m_cRecordsRead;
		int m_cRecordsDeleted;

		private Dictionary<int, string> m_mapFlidName = new Dictionary<int, string>();
		private Sfm2Xml.SfmFile m_SfmFile;

		private string m_recMkr;
		private string m_sInputMapFile;
		private string m_sSfmDataFile;
		private string m_sProjectFile;

		private List<ImportMessage> m_rgMessages = new List<ImportMessage>();

		private readonly Dictionary<string, EncConverterChoice> m_mapWsEncConv = new Dictionary<string, EncConverterChoice>();
		/// <summary>
		/// Dictionary of std format marker mapping objects loaded from the map file.
		/// </summary>
		Dictionary<string, RnSfMarker> m_mapMkrRsfFromFile = new Dictionary<string, RnSfMarker>();
		/// <summary>
		/// Dictionary of std format marker mapping objects that match up against the input file.
		/// These may be copied from m_rgsfmFromMapFile or created with default settings.
		/// </summary>
		Dictionary<string, RnSfMarker> m_mapMkrRsf = new Dictionary<string, RnSfMarker>();

		List<CharMapping> m_rgcm = new List<CharMapping>();

		/// <summary>
		/// Horizontal location of the cancel button when the "quick finish" button is shown.
		/// </summary>
		int m_ExtraButtonLeft;
		/// <summary>
		/// Original horizontal location of the cancel button, or the horizontal location of the
		/// "quick finish" button.
		/// </summary>
		int m_OriginalCancelButtonLeft;

		/// <summary />
		public NotebookImportWiz()
		{
			InitializeComponent();

			openFileDialog = new OpenFileDialogAdapter();
			m_sStdImportMap = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Import", "NotesImport.map");
			m_ExtraButtonLeft = m_btnBack.Left - (m_btnCancel.Width + kdxpCancelHelpButtonGap);
			m_OriginalCancelButtonLeft = m_btnCancel.Left;
			m_btnQuickFinish.Visible = false;
			m_btnQuickFinish.Left = m_OriginalCancelButtonLeft;
			m_btnCancel.Visible = true;
			m_sFmtEncCnvLabel = lblMappingLanguagesInstructions.Text;

			// Need to align SaveMapFile and QuickFinish to top of other dialog buttons (FWNX-833)
			var normalDialogButtonTop = m_btnHelp.Top;
			m_btnQuickFinish.Top = normalDialogButtonTop;
			m_btnSaveMapFile.Top = normalDialogButtonTop;

			// Disable all buttons that are enabled only by a selection being made in a list
			// view.
			m_btnModifyCharMapping.Enabled = false;
			m_btnDeleteCharMapping.Enabled = false;
			m_btnModifyMappingLanguage.Enabled = false;
			m_btnModifyContentMapping.Enabled = false;
			m_btnDeleteRecordMapping.Enabled = false;
			m_btnModifyRecordMapping.Enabled = false;

			// We haven't yet implemented the "advanced" features on that tab...
			m_btnAdvanced.Enabled = false;
			m_btnAdvanced.Visible = false;
		}

		#region IFwExtension Members

		/// <summary>
		/// Initialize the data values for this dialog.
		/// </summary>
		void IFwExtension.Init(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher)
		{
			m_cache = cache;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_mdc = cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			m_wsManager = m_cache.ServiceLocator.WritingSystemManager;
			lblMappingLanguagesInstructions.Text = string.Format(m_sFmtEncCnvLabel, cache.ProjectId.Name);

			m_tbDatabaseFileName.Text = m_propertyTable.GetValue("DataNotebookImportDb", string.Empty);
			m_tbProjectFileName.Text = m_propertyTable.GetValue("DataNotebookImportPrj", string.Empty);
			m_tbSettingsFileName.Text = m_propertyTable.GetValue("DataNotebookImportMap", string.Empty);
			if (string.IsNullOrEmpty(m_tbSettingsFileName.Text) || m_tbSettingsFileName.Text == m_sStdImportMap)
			{
				m_tbSettingsFileName.Text = m_sStdImportMap;
				if (!string.IsNullOrEmpty(m_tbDatabaseFileName.Text))
				{
					m_tbSaveAsFileName.Text = Path.Combine(Path.GetDirectoryName(m_tbDatabaseFileName.Text), Path.GetFileNameWithoutExtension(m_tbDatabaseFileName.Text) + "-import-settings.map");
				}
			}
			else
			{
				m_tbSaveAsFileName.Text = m_tbSettingsFileName.Text;
				DirtySettings = false;
			}
			m_stylesheet = m_propertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet");
			ShowSaveButtonOrNot();
		}

		#endregion

		protected override void OnHelpButton()
		{
			string helpTopic = null;

			switch (CurrentStepNumber)
			{
				case 0:
					helpTopic = "khtpDataNotebookImportWizStep1";
					break;
				case 1:
					helpTopic = "khtpDataNotebookImportWizStep2";
					break;
				case 2:
					helpTopic = "khtpDataNotebookImportWizStep3";
					break;
				case 3:
					helpTopic = "khtpDataNotebookImportWizStep4";
					break;
				case 4:
					helpTopic = "khtpDataNotebookImportWizStep5";
					break;
				case 5:
					helpTopic = "khtpDataNotebookImportWizStep6";
					break;
				case 6:
					helpTopic = "khtpDataNotebookImportWizStep7";
					break;
				default:
					Debug.Assert(false, "Reached a step without a help file defined for it");
					break;
			}

			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), helpTopic);
		}

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
			if (DirtySettings || (ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				// LT-7057: if no settings file, don't ask to save
				if (UsesInvalidFileNames(true))
				{
					return;	// finish without prompting to save...
				}

				// ask to save the settings
				var result = MessageBox.Show(this,
					LexTextControls.ksAskRememberImportSettings,
					LexTextControls.ksSaveSettings_,
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button3);

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
		}

		private void FillLanguageMappingView()
		{
			m_lvMappingLanguages.Items.Clear();
			var wss = new HashSet<CoreWritingSystemDefinition>();
			foreach (var sWs in m_mapWsEncConv.Keys)
			{
				var ecc = m_mapWsEncConv[sWs];
				wss.Add(ecc.WritingSystem);
				m_lvMappingLanguages.Items.Add(new ListViewItem(new[] { ecc.Name, ecc.ConverterName }) {Tag = ecc});
			}
			foreach (var ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				if (wss.Contains(ws))
				{
					continue;
				}
				wss.Add(ws);
				var lvi = CreateListViewItemForWS(ws);
				m_lvMappingLanguages.Items.Add(lvi);
				DirtySettings = true;
			}
			m_lvMappingLanguages.Sort();
			var app = m_propertyTable.GetValue<IApp>("App");
			m_btnAddWritingSystem.Initialize(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app, wss);
		}

		private ListViewItem CreateListViewItemForWS(CoreWritingSystemDefinition ws)
		{
			var sEncCnv = string.IsNullOrEmpty(ws.LegacyMapping) ? Sfm2Xml.STATICS.AlreadyInUnicode : ws.LegacyMapping;
			EncConverterChoice ecc;
			if (m_mapWsEncConv.TryGetValue(ws.Id, out ecc))
			{
				ecc.ConverterName = sEncCnv;
			}
			else
			{
				ecc = new EncConverterChoice(ws.Id, sEncCnv, m_wsManager);
				m_mapWsEncConv.Add(ecc.WritingSystem.Id, ecc);
			}
			return new ListViewItem(new[] { ws.DisplayLabel, sEncCnv }) {Tag = ecc};
		}

		private void btnBackup_Click(object sender, EventArgs e)
		{
			using (var dlg = new BackupProjectDlg(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				dlg.ShowDialog(this);
			}
		}

		private void btnDatabaseBrowse_Click(object sender, EventArgs e)
		{
			m_tbDatabaseFileName.Text = GetFile(OFType.Database, m_tbDatabaseFileName.Text);
		}

		private void btnProjectBrowse_Click(object sender, EventArgs e)
		{
			m_tbProjectFileName.Text = GetFile(OFType.Project, m_tbProjectFileName.Text);
		}

		private void btnSettingsBrowse_Click(object sender, EventArgs e)
		{
			m_tbSettingsFileName.Text = GetFile(OFType.Settings, m_tbSettingsFileName.Text);
		}

		private void btnSaveAsBrowse_Click(object sender, EventArgs e)
		{
			m_tbSaveAsFileName.Text = GetFile(OFType.SaveAs, m_tbSaveAsFileName.Text);
		}

		private string GetFile(OFType fileType, string currentFile)
		{
			switch (fileType)
			{
				case OFType.Database:
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ShoeboxAnthropologyDatabase, FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectAnthropologyStdFmtFile;
					break;
				case OFType.Project:
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ShoeboxProjectFiles, FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectShoeboxProjectFile;
					break;
				case OFType.Settings:
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ImportMapping, FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectLoadImportSettingsFile;
					break;
				case OFType.SaveAs:
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ImportMapping, FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectSaveImportSettingsFile;
					break;
			}
			openFileDialog.FilterIndex = 1;
			// don't require file to exist if it's "SaveAs"
			openFileDialog.CheckFileExists = (fileType != OFType.SaveAs);
			openFileDialog.Multiselect = false;

			var done = false;
			while (!done)
			{
				// LT-6620 : putting in an invalid path was causing an exception in the openFileDialog.ShowDialog()
				// Now we make sure parts are valid before setting the values in the openfile dialog.
				var dir = string.Empty;
				try
				{
					dir = Path.GetDirectoryName(currentFile);
				}
				catch { }

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

				if (openFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					bool isValid;
					string sFileType;
					switch (fileType)
					{
						case OFType.Database:
							sFileType = LexTextControls.ksStandardFormat;
							isValid = IsValidSfmFile(openFileDialog.FileName);
							break;
						case OFType.Project:
							sFileType = LexTextControls.ksShoeboxProject;
							var validFile = new Sfm2Xml.IsSfmFile(openFileDialog.FileName);
							isValid = validFile.IsValid;
							break;
						case OFType.SaveAs:
							sFileType = LexTextControls.ksXmlSettings;
							isValid = true;		// no requirements since the file will be overridden
							break;
						default:
							sFileType = LexTextControls.ksXmlSettings;
							isValid = IsValidMapFile(openFileDialog.FileName);
							break;
					}

					if (!isValid)
					{
						var msg = string.Format(LexTextControls.ksSelectedFileXInvalidY, openFileDialog.FileName, sFileType, System.Environment.NewLine);
						var dr = MessageBox.Show(this, msg, LexTextControls.ksPossibleInvalidFile, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
						if (dr == DialogResult.Yes)
						{
							return openFileDialog.FileName;
						}

						if (dr == DialogResult.No)
						{
							continue;
						}
						break;	// exit with current still
					}
					return openFileDialog.FileName;
				}
				done = true;
			}
			return currentFile;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			// The wizard base class redraws the controls, so move the cancel button after it's
			// done ...
			m_OriginalCancelButtonLeft = m_btnHelp.Left - (m_btnCancel.Width + kdxpCancelHelpButtonGap);
			if (m_btnQuickFinish != null && m_btnBack != null && m_btnCancel != null && m_OriginalCancelButtonLeft != 0)
			{
				m_ExtraButtonLeft = m_btnBack.Left - (m_btnCancel.Width + kdxpCancelHelpButtonGap);
				if (m_btnQuickFinish.Visible)
				{
					m_btnQuickFinish.Left = m_OriginalCancelButtonLeft;
					m_btnCancel.Left = m_ExtraButtonLeft;
				}
				else
				{
					m_btnCancel.Left = m_OriginalCancelButtonLeft;
				}
			}
		}

		private void btnModifyMappingLanguage_Click(object sender, EventArgs e)
		{
			if (m_lvMappingLanguages.SelectedItems.Count == 0)
			{
				return;
			}
			using (var dlg = new ImportEncCvtrDlg())
			{
				var lvi = m_lvMappingLanguages.SelectedItems[0];
				var sName = lvi.SubItems[0].Text;
				var sEncCnv = lvi.SubItems[1].Text;
				var app = m_propertyTable.GetValue<IApp>("App");
				dlg.Initialize(sName, sEncCnv, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					var sNewEncCnv = dlg.EncodingConverter;
					if (sNewEncCnv != sEncCnv)
					{
						lvi.SubItems[1].Text = sNewEncCnv;
						((EncConverterChoice)lvi.Tag).ConverterName = sNewEncCnv;
						DirtySettings = true;
					}
				}
			}
		}

		private void btnModifyContentMapping_Click(object sender, EventArgs e)
		{
			if (m_lvContentMapping.SelectedItems.Count == 0)
			{
				return;
			}
			using (var dlg = new AnthroFieldMappingDlg())
			{
				var lvi = m_lvContentMapping.SelectedItems[0];
				var rsfm = lvi.Tag as RnSfMarker;
				var app = m_propertyTable.GetValue<IApp>("App");
				dlg.Initialize(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app, rsfm, m_SfmFile, m_mapFlidName, m_stylesheet, m_propertyTable, m_publisher);
				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				rsfm = dlg.Results;
				lvi.SubItems[3].Text = rsfm.m_sName;
				lvi.Tag = rsfm;
				DirtySettings = true;
			}
		}

		private void cbRecordMarker_SelectedIndexChanged(object sender, EventArgs e)
		{
			var sRecMkr = m_cbRecordMarker.SelectedItem as string;
			Debug.Assert(sRecMkr != null);
			var sRecMkrBase = sRecMkr.Substring(1);
			foreach (var sMkr in m_mapMkrRsf.Keys)
			{
				var rsf = m_mapMkrRsf[sMkr];
				rsf.m_nLevel = rsf.m_sMkr == sRecMkrBase ? 1 : 0;
			}
		}

		private void btnAddRecordMapping_Click(object sender, EventArgs e)
		{
			//related to FWR-2846
			MessageBox.Show(this, "This feature is not yet implemented", "Please be patient");
		}

		private void btnModifyRecordMapping_Click(object sender, EventArgs e)
		{
			//related to FWR-2846
			MessageBox.Show(this, "This feature is not yet implemented", "Please be patient");
		}

		private void btnDeleteRecordMapping_Click(object sender, EventArgs e)
		{
			//related to FWR-2846
			MessageBox.Show(this, "This feature is not yet implemented", "Please be patient");
		}

		private void btnAddCharMapping_Click(object sender, EventArgs e)
		{
			using (var dlg = new ImportCharMappingDlg())
			{
				var app = m_propertyTable.GetValue<IApp>("App");
				dlg.Initialize(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app, m_stylesheet, null);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					var cmNew = new CharMapping
					{
						BeginMarker = dlg.BeginMarker,
						EndMarker = dlg.EndMarker,
						EndWithWord = dlg.EndWithWord,
						DestinationWritingSystemId = dlg.WritingSystemId,
						DestinationStyle = dlg.StyleName,
						IgnoreMarkerOnImport = dlg.IgnoreOnImport
					};
					m_rgcm.Add(cmNew);
					var lvi = CreateListItemForCharMapping(cmNew);
					m_lvCharMappings.Items.Add(lvi);
					DirtySettings = true;
				}
			}
		}

		private void btnModifyCharMapping_Click(object sender, EventArgs e)
		{
			if (m_lvCharMappings.SelectedItems.Count == 0)
			{
				return;
			}
			var lvi = m_lvCharMappings.SelectedItems[0];
			using (var dlg = new ImportCharMappingDlg())
			{
				var cm = lvi.Tag as CharMapping;
				var app = m_propertyTable.GetValue<IApp>("App");
				dlg.Initialize(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app, m_stylesheet, cm);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					cm.BeginMarker = dlg.BeginMarker;
					cm.EndMarker = dlg.EndMarker;
					cm.EndWithWord = dlg.EndWithWord;
					cm.DestinationWritingSystemId = dlg.WritingSystemId;
					cm.DestinationStyle = dlg.StyleName;
					cm.IgnoreMarkerOnImport = dlg.IgnoreOnImport;
					var lviNew = CreateListItemForCharMapping(cm);
					lvi.SubItems[0].Text = lviNew.SubItems[0].Text;
					lvi.SubItems[1].Text = lviNew.SubItems[1].Text;
					lvi.SubItems[2].Text = lviNew.SubItems[2].Text;
					lvi.SubItems[3].Text = lviNew.SubItems[3].Text;
					DirtySettings = true;
				}
			}
		}

		private void btnDeleteCharMapping_Click(object sender, EventArgs e)
		{
			if (m_lvCharMappings.SelectedItems.Count == 0)
			{
				return;
			}
			var lvi = m_lvCharMappings.SelectedItems[0];
			var cm = lvi.Tag as CharMapping;
			m_lvCharMappings.Items.Remove(lvi);
			m_rgcm.Remove(cm);
			DirtySettings = true;
		}

		private void rbReplaceAllEntries_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbReplaceAllEntries.Checked)
			{
				m_rbAddEntries.Checked = false;
			}
		}

		private void rbAddEntries_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbAddEntries.Checked)
			{
				m_rbReplaceAllEntries.Checked = false;
			}
		}

		private void btnSaveMapFile_Click(object sender, EventArgs e)
		{
			SaveSettings();
		}

		private void SaveSettings()
		{
			m_propertyTable.SetProperty("DataNotebookImportDb", m_tbDatabaseFileName.Text, true, true);
			m_propertyTable.SetProperty("DataNotebookImportPrj", m_tbProjectFileName.Text, true, true);
			m_propertyTable.SetProperty("DataNotebookImportMap", m_tbSaveAsFileName.Text, true, true);
			using (var tw = FileUtils.OpenFileForWrite(m_tbSaveAsFileName.Text, Encoding.UTF8))
			{
				try
				{
					var sRecMkr = m_cbRecordMarker.SelectedItem as string;
					var sRecMkrBase = string.Empty;
					if (string.IsNullOrEmpty(sRecMkr))
					{
						foreach (var rsf in m_mapMkrRsf.Values)
						{
							if (rsf.m_nLevel == 1)
							{
								sRecMkrBase = rsf.m_sMkr;
								break;
							}
						}
					}
					else
					{
						sRecMkrBase = sRecMkr.Substring(1);
						// strip leading backslash
					}
					using (var xw = XmlWriter.Create(tw))
					{
						xw.WriteStartDocument();
						xw.WriteWhitespace(Environment.NewLine);
						const string sDontEditEnglish = " DO NOT EDIT THIS FILE!  YOU HAVE BEEN WARNED! ";
						xw.WriteComment(sDontEditEnglish);
						xw.WriteWhitespace(Environment.NewLine);
						const string sAutoEnglish = " The Fieldworks import process automatically maintains this file. ";
						xw.WriteComment(sAutoEnglish);
						xw.WriteWhitespace(Environment.NewLine);
						var sDontEdit = LexTextControls.ksDONOTEDIT;
						if (sDontEdit != sDontEditEnglish)
						{
							xw.WriteComment(sDontEdit);
							xw.WriteWhitespace(Environment.NewLine);
						}
						var sAuto = LexTextControls.ksAutomaticallyMaintains;
						if (sAuto != sAutoEnglish)
						{
							xw.WriteComment(sAuto);
							xw.WriteWhitespace(Environment.NewLine);
						}
						xw.WriteStartElement("ShoeboxImportSettings");
						foreach (var sWs in m_mapWsEncConv.Keys)
						{
							var ecc = m_mapWsEncConv[sWs];
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteStartElement("EncodingConverter");
							xw.WriteAttributeString("ws", ecc.WritingSystem.Id);
							if (!string.IsNullOrEmpty(ecc.ConverterName) && ecc.ConverterName != Sfm2Xml.STATICS.AlreadyInUnicode)
							{
								xw.WriteAttributeString("converter", ecc.ConverterName);
							}
							xw.WriteEndElement();	// EncodingConverter
						}
						foreach (string sMkr in m_mapMkrRsf.Keys)
						{
							var rsf = m_mapMkrRsf[sMkr];
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteStartElement("Marker");
							xw.WriteAttributeString("tag", rsf.m_sMkr);
							xw.WriteAttributeString("flid", rsf.m_flid.ToString());
							if (!string.IsNullOrEmpty(rsf.m_sMkrOverThis))
							{
								xw.WriteAttributeString("owner", rsf.m_sMkrOverThis);
							}
							else if (rsf.m_nLevel == 0 && !string.IsNullOrEmpty(sRecMkrBase))
							{
								xw.WriteAttributeString("owner", sRecMkrBase);
							}
							WriteMarkerContents(xw, rsf);
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteEndElement();	// Marker
						}
						foreach (var cm in m_rgcm)
						{
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteStartElement("CharMapping");
							xw.WriteAttributeString("begin", cm.BeginMarker);
							xw.WriteAttributeString("end", cm.EndMarker);
							if (cm.IgnoreMarkerOnImport)
							{
								xw.WriteAttributeString("ignore", "true");
							}
							else
							{
								if (!string.IsNullOrEmpty(cm.DestinationStyle))
								{
									xw.WriteAttributeString("style", cm.DestinationStyle);
								}

								if (!string.IsNullOrEmpty(cm.DestinationWritingSystemId))
								{
									xw.WriteAttributeString("ws", cm.DestinationWritingSystemId);
								}
							}
							xw.WriteEndElement();
						}
						if (m_rbReplaceAllEntries.Checked)
						{
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteStartElement("ReplaceAll");
							xw.WriteAttributeString("value", "true");
							xw.WriteEndElement();	// ReplaceAll
						}
						if (m_chkDisplayImportReport.Checked)
						{
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteStartElement("ShowLog");
							xw.WriteAttributeString("value", "true");
							xw.WriteEndElement();	// ShowLog
						}
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteEndElement();	// ShoeboxImportSettings
						xw.WriteWhitespace(Environment.NewLine);
						xw.Flush();
						xw.Close();
						DirtySettings = false;
					}
				}
				catch (XmlException)
				{
				}
			}
		}

		private void WriteMarkerContents(XmlWriter xw, RnSfMarker rsf)
		{
			switch (FieldType(rsf.m_flid))
			{
				case SfFieldType.DateTime:
					foreach (var sFmt in rsf.m_dto.m_rgsFmt)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("DateFormat");
						xw.WriteAttributeString("value", sFmt);
						xw.WriteEndElement();
					}
					break;
				case SfFieldType.ListRef:
					if (!string.IsNullOrEmpty(rsf.m_tlo.m_sEmptyDefault))
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement(AreaServices.Default);
						xw.WriteAttributeString("value", rsf.m_tlo.m_sEmptyDefault);
						xw.WriteEndElement();
					}
					xw.WriteWhitespace(Environment.NewLine);
					xw.WriteStartElement("Match");
					xw.WriteAttributeString("value", rsf.m_tlo.m_pnt == PossNameType.kpntName ? "name" : "abbr");
					xw.WriteEndElement();
					if (rsf.m_tlo.m_fHaveMulti)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("Multiple");
						xw.WriteAttributeString("sep", rsf.m_tlo.m_sDelimMulti);
						xw.WriteEndElement();
					}
					if (rsf.m_tlo.m_fHaveSub)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("Subchoice");
						xw.WriteAttributeString("sep", rsf.m_tlo.m_sDelimSub);
						xw.WriteEndElement();
					}
					if (rsf.m_tlo.m_fHaveBetween)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("DelimitChoice");
						xw.WriteAttributeString("start", rsf.m_tlo.m_sMarkStart);
						xw.WriteAttributeString("end", rsf.m_tlo.m_sMarkEnd);
						xw.WriteEndElement();
					}
					if (rsf.m_tlo.m_fHaveBefore)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("StopChoices");
						xw.WriteAttributeString("value", rsf.m_tlo.m_sBefore);
						xw.WriteEndElement();
					}
					if (rsf.m_tlo.m_fIgnoreNewStuff)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("IgnoreNewChoices");
						xw.WriteAttributeString("value", "true");
						xw.WriteEndElement();
					}
					Debug.Assert(rsf.m_tlo.m_rgsMatch.Count == rsf.m_tlo.m_rgsReplace.Count);
					for (var j = 0; j < rsf.m_tlo.m_rgsMatch.Count; ++j)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("MatchReplaceChoice");
						xw.WriteAttributeString("match", rsf.m_tlo.m_rgsMatch[j]);
						xw.WriteAttributeString("replace", rsf.m_tlo.m_rgsReplace[j]);
						xw.WriteEndElement();
					}
					if (!string.IsNullOrEmpty(rsf.m_tlo.m_wsId))
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("ItemWrtSys");
						xw.WriteAttributeString("ws", rsf.m_tlo.m_wsId);
						xw.WriteEndElement();
					}
					break;
				case SfFieldType.String:
					xw.WriteWhitespace(Environment.NewLine);
					xw.WriteStartElement("StringWrtSys");
					xw.WriteAttributeString("ws", rsf.m_sto.m_wsId);
					xw.WriteEndElement();
					break;
				case SfFieldType.Text:
					if (!string.IsNullOrEmpty(rsf.m_txo.m_sStyle))
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("TextStyle");
						xw.WriteAttributeString("value", rsf.m_txo.m_sStyle);
						xw.WriteEndElement();
					}
					xw.WriteWhitespace(Environment.NewLine);
					xw.WriteStartElement("StartPara");
					if (rsf.m_txo.m_fStartParaBlankLine)
					{
						xw.WriteAttributeString("afterBlankLine", "true");
					}

					if (rsf.m_txo.m_fStartParaIndented)
					{
						xw.WriteAttributeString("forIndentedLine", "true");
					}

					if (rsf.m_txo.m_fStartParaNewLine)
					{
						xw.WriteAttributeString("forEachLine", "true");
					}
					if (rsf.m_txo.m_fStartParaShortLine)
					{
						xw.WriteAttributeString("afterShortLine", "true");
						xw.WriteAttributeString("shortLineLim", rsf.m_txo.m_cchShortLim.ToString());
					}
					xw.WriteEndElement();
					if (!string.IsNullOrEmpty(rsf.m_txo.m_wsId))
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("DefaultParaWrtSys");
						xw.WriteAttributeString("ws", rsf.m_txo.m_wsId);
						xw.WriteEndElement();
					}
					break;
				case SfFieldType.Link:
					break;
			}
		}

		/// <summary>
		/// Determine the general type of the field from its id.
		/// </summary>
		public SfFieldType FieldType(int flid)
		{
			if (flid == 0)
			{
				return SfFieldType.Discard;
			}

			var cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
			int clidDst;
			switch (cpt)
			{
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					clidDst = m_mdc.GetDstClsId(flid);
					switch (clidDst)
					{
						case RnGenericRecTags.kClassId:
							return SfFieldType.Link;
						case CrossReferenceTags.kClassId:
						case ReminderTags.kClassId:
							return SfFieldType.Invalid;
						default:
							var clidBase = clidDst;
							while (clidBase != 0 && clidBase != CmPossibilityTags.kClassId)
							{
								clidBase = m_mdc.GetBaseClsId(clidBase);
							}
							return clidBase == CmPossibilityTags.kClassId ? SfFieldType.ListRef : SfFieldType.Invalid;
					}
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.OwningSequence:
					clidDst = m_mdc.GetDstClsId(flid);
					switch (clidDst)
					{
						case StTextTags.kClassId:
							Debug.Assert(cpt == CellarPropertyType.OwningAtomic);
							return SfFieldType.Text;
						case RnRoledParticTags.kClassId:
							return SfFieldType.ListRef;	// closest choice.
						case RnGenericRecTags.kClassId:
							break;
					}
					return SfFieldType.Invalid;
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.String:
					return SfFieldType.String;
				case CellarPropertyType.GenDate:
				case CellarPropertyType.Time:
					return SfFieldType.DateTime;
				case CellarPropertyType.Unicode:
				case CellarPropertyType.Binary:
				case CellarPropertyType.Image:
				case CellarPropertyType.Boolean:
				case CellarPropertyType.Float:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Integer:
				case CellarPropertyType.Numeric:
					return SfFieldType.Invalid;
			}
			return SfFieldType.Discard;
		}

		/// <summary>
		/// See if the passed in file is a valid XML mapping file.
		/// </summary>
		private static bool IsValidMapFile(string mapFile)
		{
			if (string.IsNullOrEmpty(mapFile) || !File.Exists(mapFile))
			{
				return false;
			}
			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(mapFile);

				XmlNode root = xmlMap.DocumentElement;
				// make sure it has a root node of ShoeboxImportSettings
				if (root.Name != "ShoeboxImportSettings")
				{
					return false;
				}
				// make sure the top-level child nodes are all valid.
				foreach (XmlNode node in root.ChildNodes)
				{
					switch (node.Name)
					{
						case "EncodingConverter":
							continue;
						case "Marker":
							continue;
						case "CharMapping":
							continue;
						case "ReplaceAll":
							continue;
						case "ShowLog":
							continue;
					}

					return false;
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		private static bool IsValidSfmFile(string sFilename)
		{
			if (string.IsNullOrEmpty(sFilename) || !File.Exists(sFilename))
			{
				return false;
			}
			var validFile = new IsSfmFile(sFilename);
			return validFile.IsValid;
		}

		protected override void OnBackButton()
		{
			base.OnBackButton();
			ShowSaveButtonOrNot();
			if (m_QuickFinish)
			{
				// go back to the page where we came from
				tabSteps.SelectedIndex = m_lastQuickFinishTab + 1;
				m_CurrentStepNumber = m_lastQuickFinishTab;
				UpdateStepLabel();
				m_QuickFinish = false;	// going back, so turn off flag
			}
			NextButtonEnabled = true;	// make sure it's enabled if we go back from generated report
			AllowQuickFinishButton();	// make it visible if needed, or hidden if not
			OnResize(null);
		}

		protected override void OnNextButton()
		{
			ShowSaveButtonOrNot();

			base.OnNextButton();
			PrepareForNextTab(CurrentStepNumber);
			NextButtonEnabled = EnableNextButton();
			AllowQuickFinishButton();		// make it visible if needed, or hidden if not
			OnResize(null);
		}

		private void PrepareForNextTab(int nCurrent)
		{
			switch (nCurrent)
			{
				case kstepFileAndSettings:
					var fStayHere = UsesInvalidFileNames(false);
					if (fStayHere)
					{
						// Don't go anywhere, stay right here by going to the previous page.
						m_CurrentStepNumber = kstepFileAndSettings - 1;		// 1-based
						tabSteps.SelectedIndex = m_CurrentStepNumber - 1;	// 0-based
						UpdateStepLabel();
					}
					ReadSettings();
					break;
				case kstepEncodingConversion:
					InitializeContentMapping();
					break;
				case kstepContentMapping:
					InitializeKeyMarkers();
					break;
				case kstepKeyMarkers:
					InitializeCharMappings();
					break;
			}
		}

		protected override void OnFinishButton()
		{
			SaveSettings();
			DoImport();
			base.OnFinishButton();
		}

		private void ReadSettings()
		{
			if (m_sInputMapFile != m_tbSettingsFileName.Text)
			{
				m_sInputMapFile = m_tbSettingsFileName.Text;
				m_mapMkrRsfFromFile.Clear();
				LoadSettingsFile();
				m_mapMkrRsf.Clear();
			}
			FillLanguageMappingView();
		}

		private void InitializeContentMapping()
		{
			if (m_sProjectFile != m_tbProjectFileName.Text)
			{
				m_sProjectFile = m_tbProjectFileName.Text;
				ReadProjectFile();
			}
			if (m_sSfmDataFile != m_tbDatabaseFileName.Text)
			{
				m_sSfmDataFile = m_tbDatabaseFileName.Text;
				m_SfmFile = new SfmFile(m_sSfmDataFile);
			}
			if (m_mapMkrRsf.Count == 0)
			{
				foreach (string sfm in m_SfmFile.SfmInfo)
				{
					if (sfm.StartsWith("_"))
					{
						continue;
					}
					var rsf = FindOrCreateRnSfMarker(sfm);
					m_mapMkrRsf.Add(rsf.m_sMkr, rsf);
				}
				m_lvContentMapping.Items.Clear();
				foreach (var sMkr in m_mapMkrRsf.Keys)
				{
					var rsf = m_mapMkrRsf[sMkr];
					var lvi = new ListViewItem(new[] {
						"\\" + rsf.m_sMkr,
						m_SfmFile.GetSFMCount(rsf.m_sMkr).ToString(),
						m_SfmFile.GetSFMWithDataCount(rsf.m_sMkr).ToString(),
						rsf.m_sName
					});
					lvi.Tag = rsf;
					m_lvContentMapping.Items.Add(lvi);
				}
			}

		}

		/// <summary>
		/// Read the project file.  At this point, it appears that all we can get from this file
		/// is the identity of the record marker.
		/// </summary>
		private void ReadProjectFile()
		{
			if (!IsValidSfmFile(m_tbProjectFileName.Text))
			{
				return;
			}
			var prjRdr = new ByteReader(m_tbProjectFileName.Text);
			string sMkr;
			byte[] sfmData;
			byte[] badSfmData;
			var sDataFile = Path.GetFileName(m_tbDatabaseFileName.Text).ToLowerInvariant();
			var fInDataDefs = false;
			while (prjRdr.GetNextSfmMarkerAndData(out sMkr, out sfmData, out badSfmData))
			{
				Converter.MultiToWideError mwError;
				byte[] badData;
				switch (sMkr)
				{
					case "+db":
						if (sfmData.Length > 0)
						{
							var sData = Converter.MultiToWideWithERROR(sfmData, 0, sfmData.Length - 1, Encoding.UTF8, out mwError, out badData);
							if (mwError == Converter.MultiToWideError.None)
							{
								var sFile = Path.GetFileName(sData.Trim());
								fInDataDefs = sFile.ToLowerInvariant() == sDataFile;
							}
						}
						break;
					case "-db":
						fInDataDefs = false;
						break;
					case "mkrPriKey":
						if (fInDataDefs && sfmData.Length > 0)
						{
							var sData = Converter.MultiToWideWithERROR(sfmData, 0, sfmData.Length - 1, Encoding.UTF8, out mwError, out badData);
							if (mwError == Sfm2Xml.Converter.MultiToWideError.None)
							{
								m_recMkr = sData.Trim();
							}
						}
						break;
				}
			}
		}

		private RnSfMarker FindOrCreateRnSfMarker(string mkr)
		{
			RnSfMarker rsf;
			if (m_mapMkrRsfFromFile.TryGetValue(mkr, out rsf))
			{
				return rsf;
			}

			var rsfNew = new RnSfMarker
			{
				m_sMkr = mkr,
				m_flid = 0,
				m_sName = LexTextControls.ksDoNotImport
			};
			return rsfNew;
		}

		private void InitializeKeyMarkers()
		{
			if (m_cbRecordMarker.Items.Count == 0)
			{
				var mapOrderMarker = new Dictionary<int,string>();
				var select = -1;
				foreach (string sfm in m_SfmFile.SfmInfo)
				{
					var order = m_SfmFile.GetSFMOrder(sfm);
					mapOrderMarker[order] = sfm;
					if (sfm == m_recMkr)
					{
						select = order;
					}
				}
				for (var i = 1; i <= mapOrderMarker.Count; ++i)
				{
					string sMkr;
					if (mapOrderMarker.TryGetValue(i, out sMkr))
					{
						if (sMkr.StartsWith("_"))
						{
							continue;
						}
						var sShow = "\\" + sMkr;
						m_cbRecordMarker.Items.Add(sShow);
						if (i == select)
						{
							m_cbRecordMarker.Text = sShow;
						}
					}
				}
				if (select == -1)
				{
					m_cbRecordMarker.SelectedIndex = 0;
				}
			}
		}

		private void InitializeCharMappings()
		{
			if (m_lvCharMappings.Items.Count == 0)
			{
				foreach (var cm in m_rgcm)
				{
					var lvi = CreateListItemForCharMapping(cm);
					m_lvCharMappings.Items.Add(lvi);
				}
			}
		}

		private ListViewItem CreateListItemForCharMapping(CharMapping cm)
		{
			var sWsName = string.Empty;
			var sWs = cm.DestinationWritingSystemId;
			if (!string.IsNullOrEmpty(sWs))
			{
				CoreWritingSystemDefinition ws;
				m_cache.ServiceLocator.WritingSystemManager.GetOrSet(sWs, out ws);
				Debug.Assert(ws != null);
				sWsName = ws.DisplayLabel;
			}
			var sStyle = cm.DestinationStyle ?? string.Empty;
			var sBegin = cm.BeginMarker ?? string.Empty;
			var sEnd = cm.EndMarker ?? string.Empty;
			return new ListViewItem(new[] { sBegin, sEnd, sWsName, sStyle }) {Tag = cm};
		}

		private void ShowSaveButtonOrNot()
		{
			m_btnSaveMapFile.Visible = !string.IsNullOrEmpty(m_tbSaveAsFileName.Text);
		}

		private bool UsesInvalidFileNames(bool runSilent)
		{
			var fStayHere = false;
			if (!IsValidMapFile(m_tbSettingsFileName.Text))
			{
				if (!runSilent)
				{
					var msg = string.Format(LexTextControls.ksInvalidSettingsFileX, m_tbSettingsFileName.Text);
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
				m_tbSettingsFileName.Focus();
			}
			else if (m_tbSaveAsFileName.Text.Length == 0)
			{
				if (!runSilent)
				{
					var msg = LexTextControls.ksUndefinedSettingsSaveFile;
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
				m_tbSaveAsFileName.Focus();
			}
			else if (m_tbSaveAsFileName.Text != m_tbSettingsFileName.Text)
			{
				try
				{
					var fi = new FileInfo(m_tbSaveAsFileName.Text);
					if (!fi.Exists)
					{
						// make sure we can create the file for future use
						using (var s2 = new FileStream(m_tbSaveAsFileName.Text, FileMode.Create))
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
						var msg = string.Format(LexTextControls.ksInvalidSettingsSaveFileX, m_tbSaveAsFileName.Text);
						MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					fStayHere = true;
					m_tbSaveAsFileName.Focus();
				}
			}
			else if (m_tbSaveAsFileName.Text.ToLowerInvariant() == m_tbDatabaseFileName.Text.ToLowerInvariant())
			{
				// We don't want to overwrite the database with the settings!  See LT-8126.
				if (!runSilent)
				{
					var msg = string.Format(LexTextControls.ksSettingsSaveFileSameAsDatabaseFile, m_tbSaveAsFileName.Text, m_tbDatabaseFileName.Text);
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
			}
			return fStayHere;
		}

		private bool EnableNextButton()
		{
			//AllowQuickFinishButton();	// this should be done at least before each step
			var rval = false;
			using (new WaitCursor(this))
			{
				switch (CurrentStepNumber)
				{
					case kstepOverviewAndBackup:
						if (IsValidSfmFile(m_tbDatabaseFileName.Text) &&
							m_tbSaveAsFileName.Text.ToLowerInvariant() != m_tbDatabaseFileName.Text.ToLowerInvariant() &&
							m_tbSaveAsFileName.Text.ToLowerInvariant() != m_sStdImportMap.ToLowerInvariant())
						{
							rval = true;
						}
						break;

					case kstepFileAndSettings:
						// make sure there is a value for the 'Save as:' entry
						if (m_tbSaveAsFileName.Text.Length <= 0)
						{
							m_tbSaveAsFileName.Text = Path.Combine(Path.GetDirectoryName(m_tbDatabaseFileName.Text), Path.GetFileNameWithoutExtension(m_tbDatabaseFileName.Text) + "-import-settings.map");
						}
						rval = true;
						break;

					case kstepEncodingConversion:
						rval = true;
						break;

					case kstepContentMapping:
						rval = true;
						break;

					case kstepKeyMarkers:
						rval = true;
						break;

					case kstepCharacterMapping:
						rval = true;
						break;

					default:
						rval = true;
						break;
				}
			}
			return rval;
		}

		private bool AllowQuickFinishButton()
		{
			// if we're in an early tab and we have a dict file and a map file, allow it
			if (m_CurrentStepNumber < 6 && IsValidSfmFile(m_tbDatabaseFileName.Text) && IsValidMapFile(m_tbSettingsFileName.Text))
			{
				if (!m_btnQuickFinish.Visible)
				{
					m_btnCancel.Left = m_ExtraButtonLeft;
					m_btnCancel.Visible = true;
					m_btnQuickFinish.Visible = true;
				}
				return true;
			}
			if (m_btnQuickFinish.Visible)
			{
				m_btnQuickFinish.Visible = false;
				m_btnCancel.Left = m_OriginalCancelButtonLeft;
				m_btnCancel.Visible = true;
			}
			return false;
		}

		private void btnQuickFinish_Click(object sender, EventArgs e)
		{
			// don't continue if there are invalid file names / paths
			if (UsesInvalidFileNames(false))
			{
				return;
			}

			if (AllowQuickFinishButton())
			{
				m_lastQuickFinishTab = m_CurrentStepNumber;	// save for later

				// before jumping we need to make sure all the data structures are populated
				//  for (near) future use.
				while (CurrentStepNumber < kstepFinal)
				{
					PrepareForNextTab(CurrentStepNumber);
					m_CurrentStepNumber++;
				}

				m_CurrentStepNumber = kstepCharacterMapping;	// next to last tab (1-7)
				tabSteps.SelectedIndex = m_CurrentStepNumber;	// last tab (0-6)

				// we need to skip to the final step now, also handle back processing from there
				m_QuickFinish = true;
				UpdateStepLabel();

				// used in the final steps of importing the data
				m_btnQuickFinish.Visible = false;
				m_btnCancel.Location = m_btnQuickFinish.Location;
				m_btnCancel.Visible = true;

				NextButtonEnabled = EnableNextButton();
			}
		}

		/// <summary>
		/// 1. Set Save (Settings) As filename if we have a valid database file and the save as
		///    file is empty.
		/// 2. Enable (or disable) the Next button appropriately.
		/// </summary>
		private void m_DatabaseFileName_TextChanged(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(m_tbSaveAsFileName.Text) && IsValidSfmFile(m_tbDatabaseFileName.Text))
			{
				var sDatabase = m_tbDatabaseFileName.Text;
				var sSaveAs = Path.ChangeExtension(sDatabase, "map");
				if (sSaveAs.ToLowerInvariant() != sDatabase.ToLowerInvariant())
				{
					m_tbSaveAsFileName.Text = sSaveAs;
				}
			}
			NextButtonEnabled = EnableNextButton();
		}

		/// <summary>
		/// 1. Set the Save (Settings) As filename if we have a valid Settings filename that
		///    isn't the default standard mappings file, and Save As filename is empty.
		/// 2. Enable (or disable) the Next button appropriately.
		/// </summary>
		private void m_SettingsFileName_TextChanged(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(m_tbSaveAsFileName.Text) &&
				IsValidMapFile(m_tbSettingsFileName.Text) &&
				m_tbSettingsFileName.Text.ToLowerInvariant() != m_sStdImportMap.ToLowerInvariant())
			{
				m_tbSaveAsFileName.Text = m_tbSettingsFileName.Text;
			}
			NextButtonEnabled = EnableNextButton();
		}

		private void LoadSettingsFile()
		{
			if (m_mapFlidName.Count == 0)
			{
				FillFlidNameMap();
			}

			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(m_tbSettingsFileName.Text);
				XmlNode root = xmlMap.DocumentElement;
				m_mapWsEncConv.Clear();
				m_mapMkrRsfFromFile.Clear();
				if (root.Name == "ShoeboxImportSettings")
				{
					foreach (XmlNode xn in root.ChildNodes)
					{
						switch (xn.Name)
						{
							case "EncodingConverter":
								ReadConverterSettings(xn);
								break;
							case "Marker":
								ReadMarkerSetting(xn);
								break;
							case "CharMapping":
								AddCharMapping(xn);
								break;
							case "ReplaceAll":
								var fReplaceAll = XmlUtils.GetOptionalBooleanAttributeValue(xn, "value", false);
								if (fReplaceAll)
								{
									m_rbReplaceAllEntries.Checked = true;
								}
								else
								{
									m_rbAddEntries.Checked = true;
								}
								break;
							case "ShowLog":
								m_chkDisplayImportReport.Checked = XmlUtils.GetOptionalBooleanAttributeValue(xn, "value", false);
								break;
							default:
								break;
						}
					}
				}
			}
			catch
			{
			}
		}


		private void ReadConverterSettings(XmlNode xnConverter)
		{
			var ecc = new EncConverterChoice(xnConverter, m_wsManager);
			m_mapWsEncConv.Add(ecc.WritingSystem.Id, ecc);
		}

		private void ReadMarkerSetting(XmlNode xnMarker)
		{
			try
			{
				var sfm = new RnSfMarker
				{
					m_sMkr = XmlUtils.GetMandatoryAttributeValue(xnMarker, "tag"),
					m_flid = XmlUtils.GetMandatoryIntegerAttributeValue(xnMarker, "flid"),
					m_sMkrOverThis = XmlUtils.GetOptionalAttributeValue(xnMarker, "owner")
				};
				if (sfm.m_flid == 0)
				{
					sfm.m_sName = LexTextControls.ksDoNotImport;
				}
				else
				{
					sfm.m_sName = m_mapFlidName[sfm.m_flid];
					int clidDest;
					switch ((CellarPropertyType)m_mdc.GetFieldType(sfm.m_flid))
					{
						case CellarPropertyType.Time:
						case CellarPropertyType.GenDate:
							foreach (XmlNode xn in xnMarker.SelectNodes("./DateFormat"))
							{
								var sFormat = XmlUtils.GetMandatoryAttributeValue(xn, "value");
								sfm.m_dto.m_rgsFmt.Add(sFormat);
							}
							break;
						case CellarPropertyType.ReferenceAtomic:
							clidDest = m_mdc.GetDstClsId(sfm.m_flid);
							Debug.Assert(clidDest == CmPossibilityTags.kClassId);
							ReadPossibilityMarker(xnMarker, sfm, CellarPropertyType.ReferenceAtomic);
							break;
						case CellarPropertyType.ReferenceCollection:
							clidDest = m_mdc.GetDstClsId(sfm.m_flid);
							switch (clidDest)
							{
								case CmAnthroItemTags.kClassId:
								case CmLocationTags.kClassId:
								case CmPersonTags.kClassId:
								case CmPossibilityTags.kClassId:
									ReadPossibilityMarker(xnMarker, sfm, CellarPropertyType.ReferenceCollection);
									break;
								case CrossReferenceTags.kClassId:
									break;
								case ReminderTags.kClassId:
									break;
								case RnGenericRecTags.kClassId:
									break;
								default:
									break;
							}
							break;
						case CellarPropertyType.ReferenceSequence:
							clidDest = m_mdc.GetDstClsId(sfm.m_flid);
							Debug.Assert(clidDest == RnGenericRecTags.kClassId);
							break;
						case CellarPropertyType.OwningAtomic:
							clidDest = m_mdc.GetDstClsId(sfm.m_flid);
							Debug.Assert(clidDest == StTextTags.kClassId);
							ReadTextMarker(xnMarker, sfm);
							break;
						case CellarPropertyType.OwningCollection:
							clidDest = m_mdc.GetDstClsId(sfm.m_flid);
							Debug.Assert(clidDest == RnRoledParticTags.kClassId);
							ReadPossibilityMarker(xnMarker, sfm, CellarPropertyType.OwningCollection);
							break;
						case CellarPropertyType.OwningSequence:
							clidDest = m_mdc.GetDstClsId(sfm.m_flid);
							Debug.Assert(clidDest == RnGenericRecTags.kClassId);
							break;
						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
						case CellarPropertyType.String:
							foreach (XmlNode xn in xnMarker.SelectNodes("./StringWrtSys"))
							{
								sfm.m_sto.m_wsId = XmlUtils.GetMandatoryAttributeValue(xn, "ws");
							}
							break;
						// The following types do not occur in RnGenericRec fields.
						case CellarPropertyType.Binary:
						case CellarPropertyType.Boolean:
						case CellarPropertyType.Float:
						case CellarPropertyType.Guid:
						case CellarPropertyType.Image:
						case CellarPropertyType.Integer:
						case CellarPropertyType.Numeric:
						case CellarPropertyType.Unicode:
							break;
					}
					sfm.m_nLevel = 0;
					foreach (XmlNode xn in xnMarker.ChildNodes)
					{
						if (xn.Name == "Record")
						{
							sfm.m_nLevel = XmlUtils.GetMandatoryIntegerAttributeValue(xn, "level");
						}
					}
				}

				if (m_mapMkrRsfFromFile.ContainsKey(sfm.m_sMkr))
				{
					m_mapMkrRsfFromFile[sfm.m_sMkr] = sfm;
				}
				else
				{
					m_mapMkrRsfFromFile.Add(sfm.m_sMkr, sfm);
				}
			}
			catch
			{
			}
		}

		private void ReadPossibilityMarker(XmlNode xnMarker, RnSfMarker sfm, CellarPropertyType cpt)
		{
			foreach (XmlNode xn in xnMarker.ChildNodes)
			{
				switch (xn.Name)
				{
					case "Match":
						var sMatch = XmlUtils.GetMandatoryAttributeValue(xn, "value");
						switch (sMatch)
						{
							case "abbr":
								sfm.m_tlo.m_pnt = PossNameType.kpntAbbreviation;
								break;
							case "name":
								sfm.m_tlo.m_pnt = PossNameType.kpntName;
								break;
							default:
								sfm.m_tlo.m_pnt = PossNameType.kpntAbbreviation;
								break;
						}
						break;
					case "Multiple":
						if (cpt == CellarPropertyType.ReferenceCollection ||
							cpt == CellarPropertyType.ReferenceSequence ||
							cpt == CellarPropertyType.OwningCollection ||
							cpt == CellarPropertyType.OwningSequence)
						{
							sfm.m_tlo.m_fHaveMulti = true;
							sfm.m_tlo.m_sDelimMulti = XmlUtils.GetMandatoryAttributeValue(xn, "sep");
						}
						break;
					case "Subchoice":
						sfm.m_tlo.m_fHaveSub = true;
						sfm.m_tlo.m_sDelimSub = XmlUtils.GetMandatoryAttributeValue(xn, "sep");
						break;
					case AreaServices.Default:
						sfm.m_tlo.m_sEmptyDefault = XmlUtils.GetMandatoryAttributeValue(xn, "value");
						sfm.m_tlo.m_default = null;
						break;
					case "DelimitChoice":
						sfm.m_tlo.m_fHaveBetween = true;
						sfm.m_tlo.m_sMarkStart = XmlUtils.GetMandatoryAttributeValue(xn, "start");
						sfm.m_tlo.m_sMarkEnd = XmlUtils.GetMandatoryAttributeValue(xn, "end");
						break;
					case "StopChoices":
						sfm.m_tlo.m_fHaveBefore = true;
						sfm.m_tlo.m_sBefore = XmlUtils.GetMandatoryAttributeValue(xn, "value");
						break;
					case "IgnoreNewChoices":
						sfm.m_tlo.m_fIgnoreNewStuff = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "MatchReplaceChoice":
						sfm.m_tlo.m_rgsMatch.Add(XmlUtils.GetMandatoryAttributeValue(xn, "match"));
						sfm.m_tlo.m_rgsReplace.Add(XmlUtils.GetOptionalAttributeValue(xn, "replace", String.Empty));
						break;
					case "ItemWrtSys":
						sfm.m_tlo.m_wsId = XmlUtils.GetMandatoryAttributeValue(xn, "ws");
						break;
				}
			}
		}

		private void ReadTextMarker(XmlNode xnMarker, RnSfMarker sfm)
		{
			foreach (XmlNode xn in xnMarker.ChildNodes)
			{
				switch (xn.Name)
				{
					case "TextStyle":
						sfm.m_txo.m_sStyle = XmlUtils.GetMandatoryAttributeValue(xn, "value");
						break;
					case "StartPara":
						sfm.m_txo.m_fStartParaBlankLine = XmlUtils.GetOptionalBooleanAttributeValue(xn, "afterBlankLine", false);
						sfm.m_txo.m_fStartParaIndented = XmlUtils.GetOptionalBooleanAttributeValue(xn, "forIndentedLine", false);
						sfm.m_txo.m_fStartParaNewLine = XmlUtils.GetOptionalBooleanAttributeValue(xn, "forEachLine", false);
						sfm.m_txo.m_fStartParaShortLine = XmlUtils.GetOptionalBooleanAttributeValue(xn, "afterShortLine", false);
						sfm.m_txo.m_cchShortLim = 0;
						string sLim = XmlUtils.GetOptionalAttributeValue(xn, "shortLineLim");
						if (!String.IsNullOrEmpty(sLim))
							Int32.TryParse(sLim, out sfm.m_txo.m_cchShortLim);
						break;
					case "DefaultParaWrtSys":
						sfm.m_txo.m_wsId = XmlUtils.GetMandatoryAttributeValue(xn, "ws");
						break;
				}
			}
		}

		private void ReadLinkMarker(XmlNode xnMarker, RnSfMarker sfm)
		{
			foreach (XmlNode xn in xnMarker.ChildNodes)
			{
				switch (xn.Name)
				{
					case "IgnoreEmpty":
						break;
				}
			}
		}

		private void AddCharMapping(XmlNode xn)
		{
			m_rgcm.Add(new CharMapping(xn));
		}

		private void FillFlidNameMap()
		{
			const int flidMin = RnGenericRecTags.kflidTitle;
			const int flidMax = RnGenericRecTags.kflidDiscussion;
			// We want to skip SubRecords in the list of fields -- everything else is more
			// or less fair game as an import target (unless it no longer exists).
			// Except that Reminders and CrossReferences have never been handled in the UI,
			// we want to skip them as well.
			var flidsMissing = new[]
			{
				RnGenericRecTags.kflidSubRecords,
				RnGenericRecTags.kflidCrossReferences,
				RnGenericRecTags.kflidReminders,
				4004028			// was kflidWeather at one time.
			};

			for (var flid = flidMin; flid <= flidMax; ++flid)
			{
				var fSkip = false;
				foreach (var flidMissing in flidsMissing)
				{
					if (flid == flidMissing)
					{
						fSkip = true;
						break;
					}
				}

				if (fSkip)
				{
					continue;
				}
				var stid = "kstid" + m_mdc.GetFieldName(flid);
				var sName = ResourceHelper.GetResourceString(stid);
				m_mapFlidName.Add(flid, sName);
			}
			// Look for custom fields belonging to RnGenericRec.
			foreach (var flid in m_mdc.GetFieldIds())
			{
				if (flid >= (RnGenericRecTags.kClassId * 1000 + 500) && flid <= (RnGenericRecTags.kClassId * 1000 + 999))
				{
					var sName = m_mdc.GetFieldLabel(flid);
					m_mapFlidName.Add(flid, sName);
				}
			}
		}

		Process m_viewProcess;
		private void btnViewFile_Click(object sender, EventArgs e)
		{
			if (m_viewProcess == null || m_viewProcess.HasExited)
			{
				m_viewProcess = Process.Start(MiscUtils.IsUnix ? "xdg-open" : Path.Combine(FwDirectoryFinder.CodeDirectory, "ZEdit.exe"), m_sSfmDataFile);
			}
		}

		private void btnAdvanced_Click(object sender, EventArgs e)
		{
			if (m_lvHierarchy.Visible)
			{
				m_lvHierarchy.Visible = false;
				m_lvHierarchy.Enabled = false;
				m_btnAddRecordMapping.Visible = false;
				m_btnAddRecordMapping.Enabled = false;
				m_btnModifyRecordMapping.Visible = false;
				m_btnModifyRecordMapping.Enabled = false;
				m_btnDeleteRecordMapping.Visible = false;
				m_btnDeleteRecordMapping.Enabled = false;
				lblHierarchyInstructions.Visible = false;
				m_btnAdvanced.Text = LexTextControls.ksShowAdvanced;
			}
			else
			{
				m_lvHierarchy.Visible = true;
				m_lvHierarchy.Enabled = true;
				m_btnAddRecordMapping.Visible = true;
				m_btnAddRecordMapping.Enabled = true;
				m_btnModifyRecordMapping.Visible = true;
				m_btnModifyRecordMapping.Enabled = true;
				m_btnDeleteRecordMapping.Visible = true;
				m_btnDeleteRecordMapping.Enabled = true;
				lblHierarchyInstructions.Visible = true;
				m_btnAdvanced.Text = LexTextControls.ksHideAdvanced;
			}
		}

		private void m_btnAddWritingSystem_WritingSystemAdded(object sender, EventArgs e)
		{
			var ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
			{
				var lvi = CreateListViewItemForWS(ws);
				m_lvMappingLanguages.Items.Add(lvi);
				m_lvMappingLanguages.Sort();
				lvi.Selected = true;
				DirtySettings = true;
			}
		}

		private void listViewCharMappings_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_lvCharMappings.SelectedItems.Count == 0)
			{
				m_btnModifyCharMapping.Enabled = false;
				m_btnDeleteCharMapping.Enabled = false;
			}
			else
			{
				m_btnModifyCharMapping.Enabled = true;
				m_btnDeleteCharMapping.Enabled = true;
			}
		}

		private void listViewMappingLanguages_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnModifyMappingLanguage.Enabled = m_lvMappingLanguages.SelectedItems.Count != 0;
		}

		private void listViewContentMapping_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnModifyContentMapping.Enabled = m_lvContentMapping.SelectedItems.Count != 0;
		}

		private void listViewHierarchy_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_lvHierarchy.SelectedItems.Count == 0)
			{
				m_btnDeleteRecordMapping.Enabled = false;
				m_btnModifyRecordMapping.Enabled = false;
			}
			else
			{
				m_btnDeleteRecordMapping.Enabled = true;
				m_btnModifyRecordMapping.Enabled = true;
			}
		}

		string m_sLogFile;

		private void DoImport()
		{
			using (new WaitCursor(this))
			{
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					progressDlg.Minimum = 0;
					progressDlg.Maximum = 100;
					progressDlg.AllowCancel = true;
					progressDlg.Restartable = true;
					progressDlg.Title = String.Format(LexTextControls.ksImportingFrom0, m_sSfmDataFile);
					m_sLogFile = (string)progressDlg.RunTask(true, ImportStdFmtFile, m_sSfmDataFile);
					if (m_chkDisplayImportReport.Checked && !String.IsNullOrEmpty(m_sLogFile))
					{
						using (Process.Start(m_sLogFile))
						{
						}
					}
				}
			}
		}

		/// <summary>
		/// Here's where the rubber meets the road.  We have the settings, let's do the import!
		/// </summary>
		private object ImportStdFmtFile(IThreadedProgress progressDlg, object[] parameters)
		{
			var lineNumber = 0;
			using (var uowHelper = new NonUndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor))
			{
				try
				{
					m_dtStart = DateTime.Now;
					FixSettingsForThisDatabase();
					var cLines = m_SfmFile.Lines.Count;
					progressDlg.Title = string.Format(LexTextControls.ksImportingFrom0, Path.GetFileName(m_sSfmDataFile));
					progressDlg.StepSize = 1;
					var cExistingRecords = m_cache.LangProject.ResearchNotebookOA.RecordsOC.Count;
					if (m_rbReplaceAllEntries.Checked && cExistingRecords > 0)
					{
						progressDlg.Minimum = 0;
						progressDlg.Maximum = cLines + 50;
						progressDlg.Message = LexTextControls.ksDeletingExistingRecords;
						// This is rather drastic, but it's what the user asked for!
						// REVIEW: Should we ask for confirmation before doing this?
						m_cRecordsDeleted = cExistingRecords;
						m_cache.LangProject.ResearchNotebookOA.RecordsOC.Clear();
						progressDlg.Step(50);
					}
					else
					{
						m_cRecordsDeleted = 0;
						progressDlg.Minimum = 0;
						progressDlg.Maximum = cLines;
					}
					progressDlg.Message = LexTextControls.ksImportingNewRecords;
					IRnGenericRec rec = null;
					var factRec = m_cache.ServiceLocator.GetInstance<IRnGenericRecFactory>();
					var repoPoss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
					var defaultType = repoPoss.GetObject(RnResearchNbkTags.kguidRecObservation);
					for (var i = 0; i < cLines; ++i)
					{
						progressDlg.Step(1);
						if (progressDlg.Canceled)
						{
							LogMessage(LexTextControls.ksImportCanceledByUser, lineNumber);
							break;
						}
						var field = m_SfmFile.Lines[i];
						lineNumber = field.LineNumber;
						if (field.Marker.StartsWith("_"))
						{
							continue;
						}
						RnSfMarker rsf;
						if (!m_mapMkrRsf.TryGetValue(field.Marker, out rsf))
						{
							// complain?  log complaint? throw a fit?
							continue;
						}
						if (rsf.m_nLevel == 1)
						{
							rec = factRec.Create();
							m_cache.LangProject.ResearchNotebookOA.RecordsOC.Add(rec);
							rec.TypeRA = defaultType;
							++m_cRecordsRead;
						}
						else if (rsf.m_nLevel > 1)
						{
							// we don't handle this yet!
						}

						if (rsf.m_flid == 0)
						{
							continue;
						}
						var cpt = (CellarPropertyType) m_mdc.GetFieldType(rsf.m_flid);
						int clidDst;
						switch (cpt)
						{
							case CellarPropertyType.ReferenceAtomic:
							case CellarPropertyType.ReferenceCollection:
							case CellarPropertyType.ReferenceSequence:
								clidDst = m_mdc.GetDstClsId(rsf.m_flid);
								switch (clidDst)
								{
									case RnGenericRecTags.kClassId:
										StoreLinkData(rec, rsf, field);
										break;
									case CrossReferenceTags.kClassId:
									case ReminderTags.kClassId:
										// we don't handle these yet
										break;
									default:
										var clidBase = clidDst;
										while (clidBase != 0 && clidBase != CmPossibilityTags.kClassId)
										{
											clidBase = m_mdc.GetBaseClsId(clidBase);
										}
										if (clidBase == CmPossibilityTags.kClassId)
										{
											SetListReference(rec, rsf, field);
										}
										break;
								}
								break;
							case CellarPropertyType.OwningAtomic:
							case CellarPropertyType.OwningCollection:
							case CellarPropertyType.OwningSequence:
								clidDst = m_mdc.GetDstClsId(rsf.m_flid);
								switch (clidDst)
								{
									case StTextTags.kClassId:
										Debug.Assert(cpt == CellarPropertyType.OwningAtomic);
										SetTextContent(rec, rsf, field);
										break;
									case RnRoledParticTags.kClassId:
										SetListReference(rec, rsf, field);
										break;
									case RnGenericRecTags.kClassId:
										break;
									default:
										// we don't handle these yet.
										MessageBox.Show("Need to handle owned RnGenericRec", "DEBUG");
										break;
								}
								break;
							case CellarPropertyType.MultiString:
							case CellarPropertyType.MultiUnicode:
							case CellarPropertyType.String:
								SetStringValue(rec, rsf, field, cpt);
								break;
							case CellarPropertyType.GenDate:
								SetGenDateValue(rec, rsf, field);
								break;
							case CellarPropertyType.Time:
								SetDateTimeValue(rec, rsf, field);
								break;
							case CellarPropertyType.Unicode:
							case CellarPropertyType.Binary:
							case CellarPropertyType.Image:
							case CellarPropertyType.Boolean:
							case CellarPropertyType.Float:
							case CellarPropertyType.Guid:
							case CellarPropertyType.Integer:
							case CellarPropertyType.Numeric:
								break;
						}
					}
					ProcessStoredLinkData();
					uowHelper.RollBack = false;
				}
				catch (Exception e)
				{
					var sMsg = string.Format(LexTextControls.ksProblemImportingFrom, m_tbDatabaseFileName.Text, e.Message);
					LogMessage(sMsg, lineNumber);
					MessageBox.Show(this, sMsg);
				}
			}
			m_dtEnd = DateTime.Now;
			progressDlg.Message = LexTextControls.ksCreatingImportLog;
			return CreateImportReport();
		}

		private void LogMessage(string sMsg, int lineNumber)
		{
			m_rgMessages.Add(new ImportMessage(sMsg, lineNumber));
		}

		private string CreateImportReport()
		{
			var sHtmlFile = Path.Combine(Path.GetTempPath(), "FwNotebookImportLog.htm");
			using (var sw = File.CreateText(sHtmlFile))
			{
				sw.WriteLine("<html>");
				sw.WriteLine("<head>");
				var sHeadInfo = string.Format(LexTextControls.ksImportLogForX, m_sSfmDataFile);
				sw.WriteLine($"  <title>{sHeadInfo}</title>");
				WriteHtmlJavaScript(sw);	// add the script
				sw.WriteLine("</head>");
				sw.WriteLine("<body>");
				sw.WriteLine($"<h2>{sHeadInfo}</h2>");
				var deltaTicks = m_dtEnd.Ticks - m_dtStart.Ticks;	// number of 100-nanosecond intervals
				var deltaMsec = (int)((deltaTicks + 5000L) / 10000L);	// round off to milliseconds
				var deltaSec = deltaMsec / 1000;
				var sDeltaTime = string.Format(LexTextControls.ksImportingTookTime, Path.GetFileName(m_sSfmDataFile), deltaSec, deltaMsec % 1000);
				sw.WriteLine("<p>{0}</p>", sDeltaTime);
				sw.Write("<h3>");
				if (m_cRecordsDeleted == 0)
				{
					sw.Write(LexTextControls.ksRecordsCreatedByImport, m_cRecordsRead);
				}
				else
				{
					sw.Write(LexTextControls.ksRecordsDeletedAndCreated, m_cRecordsDeleted, m_cRecordsRead);
				}
				sw.WriteLine("</h3>");
				WriteMessageLines(sw);
				ListNewPossibilities(sw, LexTextControls.ksNewAnthropologyListItems, m_rgNewAnthroItem, AreaServices.AnthroEditMachineName);
				ListNewPossibilities(sw, LexTextControls.ksNewConfidenceListItems, m_rgNewConfidence, AreaServices.ConfidenceEditMachineName);
				ListNewPossibilities(sw, LexTextControls.ksNewLocationListItems, m_rgNewLocation, AreaServices.LocationsEditMachineName);
				ListNewPossibilities(sw, LexTextControls.ksNewPeopleListItems, m_rgNewPeople, AreaServices.PeopleEditMachineName);
				ListNewPossibilities(sw, LexTextControls.ksNewPhraseTagListItems, m_rgNewPhraseTag, string.Empty);
				ListNewPossibilities(sw, LexTextControls.ksNewRecordTypeListItems, m_rgNewRecType, AreaServices.RecTypeEditMachineName);
				ListNewPossibilities(sw, LexTextControls.ksNewRestrictionListItems, m_rgNewRestriction, AreaServices.RestrictionsEditMachineName);
				ListNewPossibilities(sw, LexTextControls.ksNewStatusListItems, m_rgNewStatus, AreaServices.StatusEditMachineName);
				ListNewPossibilities(sw, LexTextControls.ksNewTimeOfDayListItems, m_rgNewTimeOfDay, string.Empty);
				// now for custom lists...
				foreach (var key in m_mapNewPossibilities.Keys)
				{
					var list = m_repoList.GetObject(key);
					var name = list.Name.BestAnalysisVernacularAlternative.Text;
					var message = string.Format(LexTextControls.ksNewCustomListItems, name);
					ListNewPossibilities(sw, message, m_mapNewPossibilities[key], "");
				}
				sw.WriteLine("</body>");
				sw.WriteLine("</html>");
				sw.Close();
			}
			return sHtmlFile;
		}

		private void WriteHtmlJavaScript(StreamWriter sw)
		{
			var sError = LexTextControls.ksErrorCaughtTryingOpenFile;
			var sCannot = string.Format(LexTextControls.ksNoFileHyperlinkThisBrowser, m_sSfmDataFile.Replace("\\", "\\\\"));
			sCannot = sCannot.Replace(Environment.NewLine, "\\n");
			sw.WriteLine("<script type=\"text/javascript\">");
			sw.WriteLine("var isIE = typeof window != 'undefined' && typeof window.ActiveXObject != 'undefined';");
			//var isNetscape = typeof window != 'undefined' && typeof window.netscape != 'undefined' && typeof window.netscape.security != 'undefined' && typeof window.opera != 'object';
			sw.WriteLine("function zedit (filename, line)");
			sw.WriteLine("{");
			string sProg = Path.Combine(FwDirectoryFinder.CodeDirectory, "zedit.exe");
			sw.WriteLine("    var prog = \"{0}\";", sProg.Replace("\\", "\\\\"));
			sw.WriteLine("    var zeditfailed = true;");
			sw.WriteLine("    if (navigator.platform == 'Win32')");
			sw.WriteLine("    {");
			sw.WriteLine("        if (isIE)");
			sw.WriteLine("        {");
			sw.WriteLine("            try");
			sw.WriteLine("            {");
			sw.WriteLine("                var command = '\"' + prog + '\" ' + filename + ' -g ' + line");
			sw.WriteLine("                var wsh = new ActiveXObject('WScript.Shell');");
			sw.WriteLine("                wsh.Run(command);");
			sw.WriteLine("                zeditfailed = false;");
			sw.WriteLine("            }");
			sw.WriteLine("            catch (err) {{ alert(\"{0}\" + err); }}", sError);
			sw.WriteLine("        }");
			sw.WriteLine("    }");
			sw.WriteLine("    if (zeditfailed)");
			sw.WriteLine("        alert(\"{0}\")", sCannot);
			sw.WriteLine("}");
			sw.WriteLine("</script>");
		}

		private void WriteMessageLines(StreamWriter sw)
		{
			if (m_rgMessages.Count == 0)
			{
				return;
			}
			sw.WriteLine($"<h2>{LexTextControls.ksMessagesFromAnthropologyImport}</h2>");
			m_rgMessages.Sort();
			string currentMessage = null;
			var sEscapedDataFile = m_sSfmDataFile.Replace("\\", "\\\\");
			foreach (var importMessage in m_rgMessages)
			{
				if (importMessage.Message != currentMessage)
				{
					currentMessage = importMessage.Message;
					// Need to quote any occurrences of <, >, or & in the message text.
					var sMsg = currentMessage.Replace("&", "&amp;");
					sMsg = sMsg.Replace("<", "&lt;");
					sMsg = sMsg.Replace(">", "&gt;");
					sw.WriteLine($"<h3>{sMsg}</h3>");
				}
				sw.Write("<ul><li>");
				if (importMessage.LineNumber <= 0)
				{
					sw.Write(LexTextControls.ksNoLineNumberInFile, m_sSfmDataFile);
				}
				else
				{
					var sLineLink = string.Format("<a HREF=\"javascript: void 0\" ONCLICK=\"zedit('{0}', '{1}'); return false\">{1}</a>", sEscapedDataFile, importMessage.LineNumber);
					sw.Write(LexTextControls.ksOnOrBeforeLine, m_sSfmDataFile, sLineLink);
				}
				sw.WriteLine("</li></ul>");
			}
		}

		private static void ListNewPossibilities(StreamWriter writer, string sMsg, List<ICmPossibility> list, string tool)
		{
			if (list.Any())
			{
				tool = null;		// FIXME when FwLink starts working again...
				writer.WriteLine("<h3>{0}</h3>", string.Format(sMsg, list.Count));
				writer.WriteLine("<ul>");
				foreach (var poss in list)
				{
					if (string.IsNullOrEmpty(tool))
					{
						writer.WriteLine("<li>{0}</li>", poss.AbbrAndName);
					}
					else
					{
						var link = new FwLinkArgs(tool, poss.Guid);
						var href = link.ToString();
						writer.WriteLine("<li><a href=\"{0}\">{1}</a></li>", href, poss.AbbrAndName);
					}
				}
				writer.WriteLine("</ul>");
			}
		}

		// These are used in our home-grown date parsing.
		string[] m_rgsDayAbbr;
		string[] m_rgsDayName;
		string[] m_rgsMonthAbbr;
		string[] m_rgsMonthName;

		private void FixSettingsForThisDatabase()
		{
			ECInterfaces.IEncConverters encConverters = new EncConverters();
			foreach (var ecc in m_mapWsEncConv.Values)
			{
				if (!string.IsNullOrEmpty(ecc.ConverterName) && ecc.ConverterName != STATICS.AlreadyInUnicode)
				{
					foreach (string convName in encConverters.Keys)
					{
						if (convName == ecc.ConverterName)
						{
							ecc.Converter = encConverters[convName];
							break;
						}
					}
				}
			}
			foreach (var rsf in m_mapMkrRsf.Values)
			{
				switch (FieldType(rsf.m_flid))
				{
					case SfFieldType.Link:
					case SfFieldType.DateTime:
						break;
					case SfFieldType.ListRef:
						SetDefaultForListRef(rsf);
						var rgchSplit = new[] { ' ' };
						if (!string.IsNullOrEmpty(rsf.m_tlo.m_sDelimMulti))
						{
							rsf.m_tlo.m_rgsDelimMulti = rsf.m_tlo.m_sDelimMulti.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						}

						if (!string.IsNullOrEmpty(rsf.m_tlo.m_sDelimSub))
						{
							rsf.m_tlo.m_rgsDelimSub = rsf.m_tlo.m_sDelimSub.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						}

						if (!string.IsNullOrEmpty(rsf.m_tlo.m_sMarkStart))
						{
							rsf.m_tlo.m_rgsMarkStart = rsf.m_tlo.m_sMarkStart.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						}

						if (!string.IsNullOrEmpty(rsf.m_tlo.m_sMarkEnd))
						{
							rsf.m_tlo.m_rgsMarkEnd = rsf.m_tlo.m_sMarkEnd.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						}

						if (!string.IsNullOrEmpty(rsf.m_tlo.m_sBefore))
						{
							rsf.m_tlo.m_rgsBefore = rsf.m_tlo.m_sBefore.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						}

						if (string.IsNullOrEmpty(rsf.m_tlo.m_wsId))
						{
							rsf.m_tlo.m_ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
						}
						else
						{
							m_cache.ServiceLocator.WritingSystemManager.GetOrSet(rsf.m_tlo.m_wsId, out rsf.m_tlo.m_ws);
						}
						break;
					case SfFieldType.String:
						if (string.IsNullOrEmpty(rsf.m_sto.m_wsId))
						{
							rsf.m_sto.m_ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
						}
						else
							m_cache.ServiceLocator.WritingSystemManager.GetOrSet(rsf.m_sto.m_wsId, out rsf.m_sto.m_ws);
						break;
					case SfFieldType.Text:
						if (string.IsNullOrEmpty(rsf.m_txo.m_wsId))
						{
							rsf.m_txo.m_ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
						}
						else
						{
							m_cache.ServiceLocator.WritingSystemManager.GetOrSet(rsf.m_txo.m_wsId, out rsf.m_txo.m_ws);
						}
						break;
				}
			}
			foreach (var cm in m_rgcm)
			{
				if (!string.IsNullOrEmpty(cm.DestinationWritingSystemId))
				{
					CoreWritingSystemDefinition ws;
					m_cache.ServiceLocator.WritingSystemManager.GetOrSet(cm.DestinationWritingSystemId, out ws);
					cm.DestinationWritingSystem = ws;
				}
			}
			var dtfi = DateTimeFormatInfo.CurrentInfo;
			m_rgsDayAbbr = dtfi.AbbreviatedDayNames;
			m_rgsDayName = dtfi.DayNames;
			m_rgsMonthAbbr = dtfi.AbbreviatedMonthNames;
			m_rgsMonthName = dtfi.MonthNames;
		}

		private void SetDefaultForListRef(RnSfMarker rsf)
		{
			var sDefault = rsf.m_tlo.m_sEmptyDefault;
			if (sDefault == null)
			{
				return;
			}
			sDefault = sDefault.Trim();
			if (sDefault.Length == 0)
			{
				return;
			}
			List<string> rgsHier;
			if (rsf.m_tlo.m_fHaveSub)
			{
				rgsHier = SplitString(sDefault, rsf.m_tlo.m_rgsDelimSub);
			}
			else
			{
				rgsHier = new List<string>();
				rgsHier.Add(sDefault);
			}
			rgsHier = PruneEmptyStrings(rgsHier);
			if (rgsHier.Count == 0)
			{
				return;
			}
			switch (rsf.m_flid)
			{
				case RnGenericRecTags.kflidAnthroCodes:
					if (m_mapAnthroCode.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_mapAnthroCode);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapAnthroCode);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewAnthroItem(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidConfidence:
					if (m_mapConfidence.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS, m_mapConfidence);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapConfidence);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewConfidenceItem(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidLocations:
					if (m_mapLocation.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.LocationsOA.PossibilitiesOS, m_mapLocation);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapLocation);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewLocation(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidPhraseTags:
					if (m_mapPhraseTag.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS, m_mapPhraseTag);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapPhraseTag);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewPhraseTag(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidResearchers:
				case RnGenericRecTags.kflidSources:
					if (m_mapPeople.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapPeople);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewPerson(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidRestrictions:
					if (m_mapRestriction.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.RestrictionsOA.PossibilitiesOS, m_mapRestriction);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapRestriction);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewRestriction(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidStatus:
					if (m_mapStatus.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.StatusOA.PossibilitiesOS, m_mapStatus);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapStatus);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewStatus(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidTimeOfEvent:
					if (m_mapTimeOfDay.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.TimeOfDayOA.PossibilitiesOS, m_mapTimeOfDay);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapTimeOfDay);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewTimeOfDay(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidType:
					if (m_mapRecType.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS, m_mapRecType);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapRecType);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewRecType(rgsHier);
					}
					break;
				case RnGenericRecTags.kflidParticipants:
					if (m_mapPeople.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
					}
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapPeople);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						rsf.m_tlo.m_default = CreateNewPerson(rgsHier);
					}
					break;
				default:
					// must be a custom field.
					Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
					// We don't yet have the necessary information in the new LCM MetaDataCache.
					break;
			}
		}

		private List<string> PruneEmptyStrings(List<string> rgsData)
		{
			var rgsOut = new List<string>();
			foreach (var sT in rgsData)
			{
				var sOut = sT?.Trim();
				if (sOut?.Length > 0)
				{
					rgsOut.Add(sOut);
				}
			}
			return rgsOut;
		}

		/// <summary>
		/// Store the data for a multi-paragraph text field.
		/// </summary>
		private void SetTextContent(IRnGenericRec rec, RnSfMarker rsf, Sfm2Xml.SfmField field)
		{
			// REVIEW: SHOULD WE WORRY ABOUT EMBEDDED CHAR MAPPINGS THAT CHANGE THE WRITING SYSTEM
			// WHEN IT COMES TO ENCODING CONVERSION???
			ReconvertEncodedDataIfNeeded(field, rsf.m_txo.m_wsId);
			var rgsParas = SplitIntoParagraphs(rsf, field);
			if (rgsParas.Count == 0)
			{
				return;
			}

			if (m_factStText == null)
			{
				m_factStText = m_cache.ServiceLocator.GetInstance<IStTextFactory>();
			}
			switch (rsf.m_flid)
			{
				case RnGenericRecTags.kflidConclusions:
					if (rec.ConclusionsOA == null)
					{
						rec.ConclusionsOA = m_factStText.Create();
					}
					StoreTextData(rec.ConclusionsOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidDescription:
					if (rec.DescriptionOA == null)
					{
						rec.DescriptionOA = m_factStText.Create();
					}
					StoreTextData(rec.DescriptionOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidDiscussion:
					if (rec.DiscussionOA == null)
					{
						rec.DiscussionOA = m_factStText.Create();
					}
					StoreTextData(rec.DiscussionOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidExternalMaterials:
					if (rec.ExternalMaterialsOA == null)
					{
						rec.ExternalMaterialsOA = m_factStText.Create();
					}
					StoreTextData(rec.ExternalMaterialsOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidFurtherQuestions:
					if (rec.FurtherQuestionsOA == null)
					{
						rec.FurtherQuestionsOA = m_factStText.Create();
					}
					StoreTextData(rec.FurtherQuestionsOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidHypothesis:
					if (rec.HypothesisOA == null)
					{
						rec.HypothesisOA = m_factStText.Create();
					}
					StoreTextData(rec.HypothesisOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidPersonalNotes:
					if (rec.PersonalNotesOA == null)
					{
						rec.PersonalNotesOA = m_factStText.Create();
					}
					StoreTextData(rec.PersonalNotesOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidResearchPlan:
					if (rec.ResearchPlanOA == null)
					{
						rec.ResearchPlanOA = m_factStText.Create();
					}
					StoreTextData(rec.ResearchPlanOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidVersionHistory:
					if (rec.VersionHistoryOA == null)
					{
						rec.VersionHistoryOA = m_factStText.Create();
					}
					StoreTextData(rec.VersionHistoryOA, rsf, rgsParas);
					break;
				default:
					// Handle custom field (don't think any can exist yet, but...)
					Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
					IStText text;
					var hvo = m_cache.DomainDataByFlid.get_ObjectProp(rec.Hvo, rsf.m_flid);
					if (hvo == 0)
					{
						text = m_factStText.Create();
						m_cache.DomainDataByFlid.SetObjProp(rec.Hvo, rsf.m_flid, text.Hvo);
					}
					else
					{
						if (m_repoStText == null)
						{
							m_repoStText = m_cache.ServiceLocator.GetInstance<IStTextRepository>();
						}
						text = m_repoStText.GetObject(hvo);
					}
					StoreTextData(text, rsf, rgsParas);
					break;
			}
		}

		private void StoreTextData(IStText text, RnSfMarker rsf, List<string> rgsParas)
		{
			if (m_factPara == null)
			{
				m_factPara = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			}
			if (rgsParas.Count == 0)
			{
				var para = m_factPara.Create();
				text.ParagraphsOS.Add(para);
				if (!string.IsNullOrEmpty(rsf.m_txo.m_sStyle))
				{
					para.StyleName = rsf.m_txo.m_sStyle;
				}
				para.Contents = MakeTsString(string.Empty, rsf.m_txo.m_ws.Handle);
			}
			else
			{
				foreach (var paragraphText in rgsParas)
				{
					var para = m_factPara.Create();
					text.ParagraphsOS.Add(para);
					if (!string.IsNullOrEmpty(rsf.m_txo.m_sStyle))
					{
						para.StyleName = rsf.m_txo.m_sStyle;
					}
					para.Contents = MakeTsString(paragraphText, rsf.m_txo.m_ws.Handle);
				}
			}
		}

		private List<string> SplitIntoParagraphs(RnSfMarker rsf, SfmField field)
		{
			var rgsParas = new List<string>();
			var rgsLines = SplitIntoLines(field.Data);
			var sbPara = new StringBuilder();
			foreach (var line in rgsLines)
			{
				var fIndented = false;
				var lineCopy = line;
				if (lineCopy.Length > 0)
				{
					fIndented = char.IsWhiteSpace(lineCopy[0]);
				}
				lineCopy = lineCopy.TrimStart();
				if (rsf.m_txo.m_fStartParaNewLine)
				{
					if (lineCopy.Length > 0)
					{
						rgsParas.Add(lineCopy);
					}
					continue;
				}
				if (lineCopy.Length == 0)
				{
					if (sbPara.Length > 0 && (rsf.m_txo.m_fStartParaBlankLine || rsf.m_txo.m_fStartParaShortLine))
					{
						rgsParas.Add(sbPara.ToString());
						sbPara.Remove(0, sbPara.Length);
					}
					continue;
				}
				if (rsf.m_txo.m_fStartParaIndented && fIndented)
				{
					if (rsf.m_txo.m_fStartParaBlankLine && sbPara.Length > 0)
					{
						rgsParas.Add(sbPara.ToString());
						sbPara.Remove(0, sbPara.Length);
					}
					sbPara.Append(lineCopy);
					continue;
				}
				if (rsf.m_txo.m_fStartParaShortLine && lineCopy.Length < rsf.m_txo.m_cchShortLim)
				{
					if (sbPara.Length > 0)
					{
						sbPara.Append(" ");
					}
					sbPara.Append(lineCopy);
					rgsParas.Add(sbPara.ToString());
					sbPara.Remove(0, sbPara.Length);
					continue;
				}

				if (sbPara.Length > 0)
				{
					sbPara.Append(" ");
				}
				sbPara.Append(lineCopy);
			}
			if (sbPara.Length > 0)
			{
				rgsParas.Add(sbPara.ToString());
				sbPara.Remove(0, sbPara.Length);
			}
			return rgsParas;
		}

		private List<string> SplitIntoLines(string sData)
		{
			var rgsLines = SplitString(sData, Environment.NewLine);
			for (var i = 0; i < rgsLines.Count; ++i)
			{
				rgsLines[i] = TrimLineData(rgsLines[i]);
			}
			return rgsLines;
		}

		private string TrimLineData(string sData)
		{
			var sLine = sData;
			// The following 4 lines of code shouldn't be needed, but ...
			// Erase any leading newline type characters, then convert any others to spaces.
			while (sLine.IndexOfAny(new[] { '\n', '\r' }) == 0)
			{
				sLine = sLine.Substring(1);
			}
			sLine = sLine.Replace('\n', ' ');
			sLine = sLine.Replace('\r', ' ');
			// Leave leading whitespace -- it may indicate the start of a new paragraph.
			return sLine.TrimEnd();
		}

		/// <summary>
		/// Store a value for either a simple formatted string or a multilingual string.
		/// </summary>
		private void SetStringValue(IRnGenericRec rec, RnSfMarker rsf, SfmField field, CellarPropertyType cpt)
		{
			// REVIEW: SHOULD WE WORRY ABOUT EMBEDDED CHAR MAPPINGS THAT CHANGE THE WRITING SYSTEM
			// WHEN IT COMES TO ENCODING CONVERSION???
			ReconvertEncodedDataIfNeeded(field, rsf.m_sto.m_wsId);
			var tss = MakeTsString(field.Data, rsf.m_sto.m_ws.Handle);
			switch (rsf.m_flid)
			{
				case RnGenericRecTags.kflidTitle:
					rec.Title = tss;
					break;
				default:
					// must be a custom field.
					Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
					switch (cpt)
					{
						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
							m_cache.DomainDataByFlid.SetMultiStringAlt(rec.Hvo, rsf.m_flid, rsf.m_sto.m_ws.Handle, tss);
							break;
						case CellarPropertyType.String:
							m_cache.DomainDataByFlid.SetString(rec.Hvo, rsf.m_flid, tss);
							break;
					}
					break;
			}
		}

		private void ReconvertEncodedDataIfNeeded(SfmField field, string sWs)
		{
			if (string.IsNullOrEmpty(sWs))
			{
				sWs = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultAnalWs);
			}
			if (!string.IsNullOrEmpty(sWs))
			{
				EncConverterChoice ecc;
				if (m_mapWsEncConv.TryGetValue(sWs, out ecc))
				{
					if (ecc.Converter != null)
					{
						field.Data = ecc.Converter.ConvertToUnicode(field.RawData);
					}
				}
			}
			if (field.ErrorConvertingData)
			{
				LogMessage(string.Format(LexTextControls.ksEncodingConversionProblem, field.Marker), field.LineNumber);
			}
		}

		private ITsString MakeTsString(string sRaw, int ws)
		{
			var rgsText = new List<string>();
			var rgcmText = new List<CharMapping>();
			while (!string.IsNullOrEmpty(sRaw))
			{
				CharMapping cmText;
				var idx = IndexOfFirstCharMappingMarker(sRaw, out cmText);
				if (idx == -1)
				{
					rgsText.Add(sRaw);		// save trailing text
					rgcmText.Add(null);
					break;
				}
				if (idx > 0)
				{
					rgsText.Add(sRaw.Substring(0, idx));	// save leading text
					rgcmText.Add(null);
				}
				sRaw = sRaw.Substring(idx + cmText.BeginMarker.Length);
				idx = sRaw.IndexOf(cmText.EndMarker);
				if (idx == -1)
				{
					if (cmText.EndWithWord)
					{
						// TODO: Generalized search for whitespace?
						idx = sRaw.IndexOfAny(new char[] { ' ', '\t', '\r', '\n' });
						if (idx == -1)
						{
							idx = sRaw.Length;
						}
					}
					else
					{
						idx = sRaw.Length;
					}
				}
				rgsText.Add(sRaw.Substring(0, idx));
				rgcmText.Add(cmText.IgnoreMarkerOnImport ? null : cmText);
				sRaw = sRaw.Substring(idx);
			}
			if (rgsText.Count == 0)
			{
				rgsText.Add(string.Empty);
				rgcmText.Add(null);
			}
			var tisb = TsStringUtils.MakeIncStrBldr();
			for (var i = 0; i < rgsText.Count; ++i)
			{
				var sRun = rgsText[i];
				var cmRun = rgcmText[i];
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, cmRun?.DestinationWritingSystem?.Handle ?? ws);
				if (!string.IsNullOrEmpty(cmRun?.DestinationStyle))
				{
					tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, cmRun.DestinationStyle);
				}
				tisb.Append(sRun);
			}
			return tisb.GetString();
		}

		/// <summary>
		/// Find the next character mapping marker in the string (if any).
		/// </summary>
		private int IndexOfFirstCharMappingMarker(string sText, out CharMapping cmText)
		{
			var idx = -1;
			cmText = null;
			foreach (var cm in m_rgcm)
			{
				if (cm.BeginMarker.Length == 0)
				{
					continue;
				}
				var idxT = sText.IndexOf(cm.BeginMarker);
				if (idxT != -1)
				{
					if (idx == -1 || idxT < idx)
					{
						cmText = cm;
						idx = idxT;
					}
				}
			}
			return idx;
		}

		/// <summary>
		/// Store a value with a "kcptGenDate" type value.  Try to handle incomplete data if
		/// possible, since this value is typed by hand.  The user may have substituted
		/// question marks for the date, and may even the month.
		/// </summary>
		private void SetGenDateValue(IRnGenericRec rec, RnSfMarker rsf, Sfm2Xml.SfmField field)
		{
			var sData = field.Data.Trim();
			if (sData.Length == 0)
			{
				return;		// nothing we can do without data!
			}
			GenDate gdt;
			if (!TryParseGenDate(sData, rsf.m_dto.m_rgsFmt, out gdt))
			{
				LogMessage(string.Format(LexTextControls.ksCannotParseGenericDate, sData, field.Marker), field.LineNumber);
				return;
			}
			switch (rsf.m_flid)
			{
				case RnGenericRecTags.kflidDateOfEvent:
					rec.DateOfEvent = gdt;
					break;
				default:
					// must be a custom field.
					Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
					// There's no way to pass a GenDate item into a custom field!
					break;
			}
		}

		private bool TryParseGenDate(string sData, List<string> rgsFmt, out GenDate gdt)
		{
			var prec = GenDate.PrecisionType.Exact;
			switch (sData[0])
			{
				case '~':
					prec = GenDate.PrecisionType.Approximate;
					sData = sData.Substring(1).Trim();
					break;
				case '<':
					prec = GenDate.PrecisionType.Before;
					sData = sData.Substring(1).Trim();
					break;
				case '>':
					prec = GenDate.PrecisionType.After;
					sData = sData.Substring(1).Trim();
					break;
			}
			if (sData.Length == 0)
			{
				gdt = new GenDate();
				return false;
			}
			int year;
			var fAD = true;
			DateTime dt;
			if (DateTime.TryParseExact(sData, rgsFmt.ToArray(), null, DateTimeStyles.None, out dt))
			{
				if (dt.Year > 0)
				{
					year = dt.Year;
				}
				else
				{
					year = -dt.Year;
					fAD = false;
				}
				gdt = new GenDate(prec, dt.Month, dt.Day, year, fAD);
				return true;
			}
			foreach (string sFmt in rgsFmt)
			{
				GenDateInfo gdi;
				var sResidue = ParseFormattedDate(sData, sFmt, out gdi);
				if (!gdi.error)
				{
					year = gdi.year;
					if (prec == GenDate.PrecisionType.Exact)
					{
						prec = sResidue.Trim().StartsWith("?") ? GenDate.PrecisionType.Approximate : gdi.prec;
					}
					if (year < 0)
					{
						year = -year;
						fAD = false;
					}
					gdt = new GenDate(prec, gdi.ymon, gdi.mday, year, fAD);
					return true;
				}
			}
			gdt = new GenDate();
			return false;
		}

		string ParseFormattedDate(string sDateString, string sFmt, out GenDateInfo gdi)
		{
			gdi = new GenDateInfo
			{
				error = true,
				prec = GenDate.PrecisionType.Exact
			};
			int cch;
			var fDayPresent = false;
			var fMonthPresent = false;
			var fYearPresent = false;
			var sDate = sDateString.Trim();
			for (; sFmt.Length > 0; sFmt = sFmt.Substring(cch))
			{
				var ch = sFmt[0];
				int i;
				for (i = 1; i < sFmt.Length; ++i)
				{
					if (sFmt[i] != ch)
					{
						break;
					}
				}
				cch = i;
				bool fError;
				switch (ch)
				{
					case 'd':
						if (CheckForQuestionMarks(ref gdi, ref sDate))
						{
							if (sDate.Length == 0)
							{
								return string.Empty;
							}
							break;
						}
						switch (cch)
						{
							case 1:	// d
							case 2:	// dd
								fDayPresent = true;
								fError = !TryParseLeadingNumber(ref sDate, out gdi.mday);
								if (fError || gdi.mday > 31)
								{
									return sDate;
								}
								break;
							case 3:	// ddd - Abbreviated day of week
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsDayAbbr, out gdi.wday);
								if (fError)
								{
									return sDate;
								}
								break;
							case 4:	// dddd - Unabbreviated day of the week
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsDayName, out gdi.wday);
								if (fError)
								{
									return sDate;
								}
								break;
							default:
								return sDate;
						}
						break;

					case 'M':
						if (CheckForQuestionMarks(ref gdi, ref sDate))
						{
							if (sDate.Length == 0)
							{
								return string.Empty;
							}
							break;
						}
						fMonthPresent = true;
						switch (cch)
						{
							case 1:	// M
							case 2:	// MM
								fError = !TryParseLeadingNumber(ref sDate, out gdi.ymon);
								if (fError || gdi.ymon > 12)
								{
									return sDate;
								}
								break;
							case 3: // MMM - Abbreviated month name
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsMonthAbbr, out gdi.ymon);
								if (fError)
								{
									return sDate;
								}
								break;
							case 4:	// MMMM - Unabbreviated month name
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsMonthName, out gdi.ymon);
								if (fError)
								{
									return sDate;
								}
								break;
							default:
								return sDate;
						}
						break;

					case 'y':
						if (sDate.StartsWith("?"))
						{
							gdi.error = true;
							return sDate;
						}
						fYearPresent = true;
						int year;
						var thisyear = DateTime.Now.Year;
						switch (cch)
						{
							case 1:
								fError = !TryParseLeadingNumber(ref sDate, out year);
								if (fError || year > 9)
								{
									return sDate;
								}
								gdi.year = 2000 + year;
								if (gdi.year > thisyear)
								{
									gdi.year -= 100;
								}
								break;
							case 2:
								fError = !TryParseLeadingNumber(ref sDate, out year);
								if (fError || year > 99)
								{
									return sDate;
								}
								gdi.year = 2000 + year;
								if (gdi.year > thisyear)
								{
									gdi.year -= 100;
								}
								break;
							case 4:
								fError = !TryParseLeadingNumber(ref sDate, out year);
								if (fError)
								{
									return sDate;
								}
								break;
							default:
								return sDate;
						}
						break;

					case 'g':
						// TODO SteveMc: IMPLEMENT ME!
						return sDate;

					case '\'': // quoted text
						break;

					case ' ':
						sDate = sDate.Trim();
						break;

					default:
						// Check for matching separators.
						sDate = sDate.Trim();
						for (var j = 0; j < cch; ++j)
						{
							if (j >= sDate.Length || sDate[j] != ch)
							{
								return sDate;
							}
						}
						sDate = sDate.Substring(cch);
						sDate = sDate.Trim();
						break;
				}
			}
			gdi.error = !ValidateDate(fYearPresent ? gdi.year : 2000, fMonthPresent ? gdi.ymon : 1, fDayPresent ? gdi.mday : 1);
			return sDate;
		}

		private static bool CheckForQuestionMarks(ref GenDateInfo gdi, ref string sDate)
		{
			if (sDate.StartsWith("?"))
			{
				while (sDate.StartsWith("?"))
				{
					sDate = sDate.Substring(1);
				}
				gdi.prec = GenDate.PrecisionType.Approximate;
				if (sDate.Length == 0)
				{
					gdi.error = gdi.year == 0;	// ok if we already have a year.
				}
				return true;
			}
			return false;
		}

		private static bool TryMatchAgainstNameList(ref string sDate, string[] rgsToMatch, out int val)
		{
			for (var j = 0; j < rgsToMatch.Length; ++j)
			{
				if (sDate.StartsWith(rgsToMatch[j]))
				{
					val = j + 1;
					sDate = sDate.Substring(rgsToMatch[j].Length);
					sDate = sDate.Trim();
					return false;
				}
			}
			val = 0;
			return true;
		}

		private static bool ValidateDate(int year, int month, int day)
		{
			if (year < -9999 || year > 9999 || year == 0)
			{
				return false;
			}

			if (month < 1 || month > 12)
			{
				return false;
			}
			int days_in_month;
			if (year == 1752 && month == 9)
			{
				days_in_month = 19; // the month the calendar was changed
			}
			else
			{
				switch (month)
				{
					case 2: // February
						if (year % 400 == 0)
						{
							// Every evenly divided 400 years: IS a leap year.
							days_in_month = 29;
							break;
						}
						if (year % 100 == 0)
						{
							// Remaining evenly divided 100 years: IS NOT a leap year.
							days_in_month = 28;
							break;
						}
						if (year % 4 == 0)
						{
							// Remaining evenly divided 4: IS a leap year.
							days_in_month = 29;
							break;
						}
						else
						{
							// Remaining years: NOT a leap year.
							days_in_month = 28;
							break;
						}
					case 4:		// April
					case 6:		// June
					case 9:		// September
					case 11:	// November
						days_in_month = 30;
						break;
					default:
						days_in_month = 31;
						break;
				}
			}

			if (day < 1 || day > days_in_month)
			{
				return false;
			}
			return true;
		}

		private static bool TryParseLeadingNumber(ref string sDate, out int val)
		{
			val = 0;
			int cchUsed;
			for (cchUsed = 0; cchUsed < sDate.Length; ++cchUsed)
			{
				if (!char.IsDigit(sDate[cchUsed]))
				{
					break;
				}
			}

			if (cchUsed < 1)
			{
				return false;
			}
			var sNum = sDate.Substring(0, cchUsed);
			sDate = sDate.Substring(cchUsed);
			return int.TryParse(sNum, out val);
		}

		/// <summary>
		/// Store a value with a "kcptTime" type value.  These are less forgiving than those with
		/// "kcptGenDate" values, because they are generally created by a computer program instead
		/// of typed by a user.
		/// </summary>
		private void SetDateTimeValue(IRnGenericRec rec, RnSfMarker rsf, Sfm2Xml.SfmField field)
		{
			var sData = field.Data.Trim();
			if (sData.Length == 0)
			{
				return;		// nothing we can do without data!
			}
			DateTime dt;
			if (!DateTime.TryParseExact(sData, rsf.m_dto.m_rgsFmt.ToArray(), null, DateTimeStyles.None, out dt))
			{
				LogMessage(string.Format(LexTextControls.ksCannotParseDateTime, field.Data, field.Marker), field.LineNumber);
				return;
			}
			switch (rsf.m_flid)
			{
				case RnGenericRecTags.kflidDateCreated:
					rec.DateCreated = dt;
					break;
				case RnGenericRecTags.kflidDateModified:
					rec.DateModified = dt;
					break;
				default:
					// must be a custom field.
					Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
					SilTime.SetTimeProperty(m_cache.DomainDataByFlid, rec.Hvo, rsf.m_flid, dt);
					break;
			}
		}

		/// <summary>
		/// Store the information needed to make any cross reference links after all the records
		/// have been created.
		/// </summary>
		private void StoreLinkData(IRnGenericRec rec, RnSfMarker rsf, Sfm2Xml.SfmField field)
		{
			if (string.IsNullOrEmpty(field.Data))
			{
				return;
			}
			var pend = new PendingLink { Marker = rsf, Field = field, Record = rec };
			m_pendingLinks.Add(pend);
		}

		private void ProcessStoredLinkData()
		{
			if (m_pendingLinks.Count == 0)
			{
				return;
			}
			// 1. Get the titles and map them onto their records.
			// 2. Try to match link data against titles
			// 3. If successful, set link.
			// 4. If unsuccessful, provide error message for log.
			var mapTitleRec = new Dictionary<string, IRnGenericRec>();
			foreach (var rec in m_cache.ServiceLocator.GetInstance<IRnGenericRecRepository>().AllInstances())
			{
				var sTitle = rec.Title.Text;
				if (string.IsNullOrEmpty(sTitle))
				{
					continue;
				}

				if (!mapTitleRec.ContainsKey(sTitle))
				{
					mapTitleRec.Add(sTitle, rec);
				}
			}
			foreach (var pend in m_pendingLinks)
			{
				IRnGenericRec rec;
				var sData = pend.Field.Data;
				if (mapTitleRec.TryGetValue(sData, out rec))
				{
					if (SetLink(pend, rec))
					{
						continue;
					}
				}
				else
				{
					var idx1 = sData.IndexOf(" - ");
					var idx2 = sData.LastIndexOf(" - ");
					if (idx1 != idx2)
					{
						idx1 += 3;
						var sTitle = sData.Substring(idx1, idx2 - idx1);
						if (mapTitleRec.TryGetValue(sTitle, out rec))
						{
							if (SetLink(pend, rec))
							{
								continue;
							}
						}
					}
				}
				// log an error.
				LogMessage(string.Format(LexTextControls.ksCannotMakeDesiredLink, pend.Field.Marker, pend.Field.Data), pend.Field.LineNumber);

			}
		}

		private static bool SetLink(PendingLink pend, IRnGenericRec rec)
		{
			switch (pend.Marker.m_flid)
			{
				case RnGenericRecTags.kflidCounterEvidence:
					pend.Record.CounterEvidenceRS.Add(rec);
					return true;
				case RnGenericRecTags.kflidSeeAlso:
					pend.Record.SeeAlsoRC.Add(rec);
					return true;
				case RnGenericRecTags.kflidSupersededBy:
					pend.Record.SupersededByRC.Add(rec);
					return true;
				case RnGenericRecTags.kflidSupportingEvidence:
					pend.Record.SupportingEvidenceRS.Add(rec);
					return true;
			}
			return false;
		}

		/// <summary>
		/// Store the data for a field that contains one or more references to a possibility
		/// list.
		/// </summary>
		private void SetListReference(IRnGenericRec rec, RnSfMarker rsf, Sfm2Xml.SfmField field)
		{
			ReconvertEncodedDataIfNeeded(field, rsf.m_tlo.m_wsId);
			var sData = field.Data ?? string.Empty;
			sData = ApplyChanges(rsf, sData);
			sData = sData.Trim();
			List<string> rgsData = null;
			if (sData.Length > 0)
			{
				if (rsf.m_tlo.m_fHaveMulti)
				{
					rgsData = SplitString(sData, rsf.m_tlo.m_rgsDelimMulti);
					rgsData = PruneEmptyStrings(rgsData);
				}
				else
				{
					rgsData = new List<string> {sData};
				}
			}
			if ((rgsData == null || rgsData.Count == 0) && rsf.m_tlo.m_default == null)
			{
				return;
			}

			if (rgsData == null)
			{
				rgsData = new List<string>();
			}
			rgsData = ApplyBeforeAndBetween(rsf, rgsData);
			if (rgsData.Count == 0)
			{
				rgsData.Add(string.Empty);
			}
			foreach (var sItem in rgsData)
			{
				switch (rsf.m_flid)
				{
					case RnGenericRecTags.kflidAnthroCodes:
						if (!StoreAnthroCode(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidConfidence:
						if (!StoreConfidence(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidLocations:
						if (!StoreLocation(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidPhraseTags:
						if (!StorePhraseTag(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidResearchers:
						if (!StoreResearcher(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidRestrictions:
						if (!StoreRestriction(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidSources:
						if (!StoreSource(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidStatus:
						if (!StoreStatus(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidTimeOfEvent:
						if (!StoreTimeOfEvent(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidType:
						if (!StoreRecType(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					case RnGenericRecTags.kflidParticipants:
						if (!StoreParticipant(rec, rsf, sItem))
						{
							LogCannotFindListItem(sItem, field);
						}
						break;
					default:
						// must be a custom field.
						Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
						var guidList = m_mdc.GetFieldListRoot(rsf.m_flid);
						if (guidList != Guid.Empty)
						{
							if (m_repoList == null)
							{
								m_repoList = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
							}
							var list = m_repoList.GetObject(guidList);
							if (list != null)
							{
								if (!StoreCustomListRefItem(rec, rsf, sItem, list))
								{
									LogCannotFindListItem(sItem, field);
								}
								break;
							}
						}
						LogMessage(string.Format(LexTextControls.ksCannotFindPossibilityList, field.Marker), field.LineNumber);
						return;
				}
			}
		}

		private void LogCannotFindListItem(string sItem, Sfm2Xml.SfmField field)
		{
			LogMessage(string.Format(LexTextControls.ksCannotFindMatchingListItem, sItem, field.Marker), field.LineNumber);
		}

		/// <summary>
		/// For each individual item,
		/// 1. remove any comment (using rsf.m_tlo.m_rgsBefore)
		/// 2. extract actual data (using rsf.m_tlo.m_rgsMarkStart/End)
		/// </summary>
		private static List<string> ApplyBeforeAndBetween(RnSfMarker rsf, List<string> rgsData)
		{
			var rgsData2 = new List<string>();
			foreach (var sItem in rgsData)
			{
				var sT = sItem;
				if (rsf.m_tlo.m_fHaveBefore && rsf.m_tlo.m_rgsBefore != null && sItem.Length > 0)
				{
					foreach (var sBefore in rsf.m_tlo.m_rgsBefore)
					{
						var idx = sItem.IndexOf(sBefore);
						if (idx > 0)
						{
							sT = sItem.Substring(0, idx).Trim();
						}
						else if (idx == 0)
						{
							sT = string.Empty;
						}

						if (sT.Length == 0)
						{
							break;
						}
					}
				}
				if (sT.Length > 0 && rsf.m_tlo.m_fHaveBetween && rsf.m_tlo.m_rgsMarkStart != null && rsf.m_tlo.m_rgsMarkEnd != null)
				{
					// Ensure safe length even if the two lengths differ.
					// REVIEW: Should we complain if the lengths differ?
					var clen = rsf.m_tlo.m_rgsMarkEnd.Length;
					if (rsf.m_tlo.m_rgsMarkStart.Length < rsf.m_tlo.m_rgsMarkEnd.Length)
					{
						clen = rsf.m_tlo.m_rgsMarkStart.Length;
					}
					if (clen > 0)
					{
						var sT2 = string.Empty;
						for (var i = 0; i < clen; ++i)
						{
							var idx = sT.IndexOf(rsf.m_tlo.m_rgsMarkStart[i]);
							if (idx >= 0)
							{
								++idx;
								var idxEnd = sT.IndexOf(rsf.m_tlo.m_rgsMarkEnd[i], idx);
								if (idxEnd >= 0)
								{
									sT2 = sT.Substring(idx, idxEnd - idx);
									break;
								}
							}
						}
						sT = sT2;
					}
				}
				if (!string.IsNullOrEmpty(sT))
				{
					rgsData2.Add(sT);
				}
			}
			return rgsData2;
		}

		private string ApplyChanges(RnSfMarker rsf, string sData)
		{
			if (rsf.m_tlo.m_rgsMatch == null || rsf.m_tlo.m_rgsReplace == null)
			{
				return sData;
			}
			var count = rsf.m_tlo.m_rgsMatch.Count;
			if (rsf.m_tlo.m_rgsReplace.Count < rsf.m_tlo.m_rgsMatch.Count)
			{
				count = rsf.m_tlo.m_rgsReplace.Count;
			}
			for (var i = 0; i < count; ++i)
			{
				sData = sData.Replace(rsf.m_tlo.m_rgsMatch[i], rsf.m_tlo.m_rgsReplace[i]);
			}
			return sData;
		}

		private List<string> SplitString(string sItem, string sDel)
		{
			var rgsSplit = new List<string>();
			if (string.IsNullOrEmpty(sItem))
			{
				rgsSplit.Add(string.Empty);
				return rgsSplit;
			}
			int idx;
			while ((idx = sItem.IndexOf(sDel)) >= 0)
			{
				rgsSplit.Add(sItem.Substring(0, idx));
				sItem = sItem.Substring(idx + sDel.Length);
			}
			if (sItem.Length > 0)
			{
				rgsSplit.Add(sItem);
			}
			return rgsSplit;
		}

		private List<string> SplitString(string sData, string[] rgsDelims)
		{
			var rgsData = new List<string>();
			rgsData.Add(sData);
			if (rgsDelims != null && rgsDelims.Length > 0)
			{
				foreach (var sDel in rgsDelims)
				{
					var rgsSplit = new List<string>();
					foreach (var sItem in rgsData)
					{
						var s1 = sItem.Trim();
						if (s1.Length == 0)
						{
							continue;
						}
						var rgsT = SplitString(s1, sDel);
						foreach (var s2 in rgsT)
						{
							var s3 = s2.Trim();
							if (s3.Length > 0)
							{
								rgsSplit.Add(s3);
							}
						}
					}
					rgsData = rgsSplit;
				}
			}
			return rgsData;
		}

		private ICmPossibility FindPossibilityOrNull(List<string> rgsHier, Dictionary<string, ICmPossibility> map)
		{
			ICmPossibility possParent = null;
			ICmPossibility poss = null;
			for (var i = 0; i < rgsHier.Count; ++i)
			{
				if (!map.TryGetValue(rgsHier[i].ToLowerInvariant(), out poss))
				{
					return null;
				}

				if (i > 0 && poss.Owner != possParent)
				{
					return null;
				}
				possParent = poss;
			}
			return poss;
		}

		private bool StoreAnthroCode(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				var def = rsf.m_tlo.m_default as ICmAnthroItem;
				if (def != null && !rec.AnthroCodesRC.Contains(def))
				{
					rec.AnthroCodesRC.Add(def);
				}
				return true;
			}

			if (m_mapAnthroCode.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_mapAnthroCode);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapAnthroCode);
			if (poss != null)
			{
				if (!rec.AnthroCodesRC.Contains(poss as ICmAnthroItem))
				{
					rec.AnthroCodesRC.Add(poss as ICmAnthroItem);
				}
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewAnthroItem(rgsHier);
			if (item != null)
			{
				rec.AnthroCodesRC.Add(item);
			}
			return true;
		}

		private ICmAnthroItem CreateNewAnthroItem(List<string> rgsHier)
		{
			return (ICmAnthroItem)CreateNewPossibility(rgsHier, AnthroItemCreator, m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_mapAnthroCode, m_rgNewAnthroItem);
		}

		private bool StoreConfidence(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				rec.ConfidenceRA = rsf.m_tlo.m_default;
				return true;
			}

			if (m_mapConfidence.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS, m_mapConfidence);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapConfidence);
			if (poss != null)
			{
				rec.ConfidenceRA = poss;
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewConfidenceItem(rgsHier);
			rec.ConfidenceRA = item;
			return true;
		}

		private ICmPossibility CreateNewConfidenceItem(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator, m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS, m_mapConfidence, m_rgNewConfidence);
		}

		private bool StoreLocation(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				var def = rsf.m_tlo.m_default as ICmLocation;
				if (def != null && !rec.LocationsRC.Contains(def))
				{
					rec.LocationsRC.Add(def);
				}
				return true;
			}

			if (m_mapLocation.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.LocationsOA.PossibilitiesOS, m_mapLocation);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapLocation);
			if (poss != null)
			{
				if (!rec.LocationsRC.Contains(poss as ICmLocation))
				{
					rec.LocationsRC.Add(poss as ICmLocation);
				}
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewLocation(rgsHier);
			if (item != null)
			{
				rec.LocationsRC.Add(item);
			}
			return true;
		}

		private ICmLocation CreateNewLocation(List<string> rgsHier)
		{
			return (ICmLocation)CreateNewPossibility(rgsHier, LocationCreator, m_cache.LangProject.LocationsOA.PossibilitiesOS, m_mapLocation, m_rgNewLocation);
		}

		private bool StorePhraseTag(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				if (rsf.m_tlo.m_default != null && !rec.PhraseTagsRC.Contains(rsf.m_tlo.m_default))
				{
					rec.PhraseTagsRC.Add(rsf.m_tlo.m_default);
				}
				return true;
			}

			if (m_mapPhraseTag.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS, m_mapPhraseTag);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapPhraseTag);
			if (poss != null)
			{
				if (!rec.PhraseTagsRC.Contains(poss))
				{
					rec.PhraseTagsRC.Add(poss);
				}
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewPhraseTag(rgsHier);
			if (item != null)
			{
				rec.PhraseTagsRC.Add(item);
			}
			return true;
		}

		private ICmPossibility CreateNewPhraseTag(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator, m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS, m_mapPhraseTag, m_rgNewPhraseTag);
		}

		private bool StoreResearcher(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				var def = rsf.m_tlo.m_default as ICmPerson;
				if (!rec.ResearchersRC.Contains(def))
				{
					rec.ResearchersRC.Add(def);
				}
				return true;
			}

			if (m_mapPeople.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
			if (poss != null)
			{
				if (!rec.ResearchersRC.Contains(poss as ICmPerson))
				{
					rec.ResearchersRC.Add(poss as ICmPerson);
				}
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewPerson(rgsHier);
			if (item != null)
			{
				rec.ResearchersRC.Add(item);
			}
			return true;
		}

		private ICmPerson CreateNewPerson(List<string> rgsHier)
		{
			return (ICmPerson)CreateNewPossibility(rgsHier, PersonCreator, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople, m_rgNewPeople);
		}

		private bool StoreSource(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				var def = rsf.m_tlo.m_default as ICmPerson;
				if (def != null && !rec.SourcesRC.Contains(def))
				{
					rec.SourcesRC.Add(def);
				}
				return true;
			}

			if (m_mapPeople.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
			if (poss != null)
			{
				if (!rec.SourcesRC.Contains(poss as ICmPerson))
				{
					rec.SourcesRC.Add(poss as ICmPerson);
				}
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewPerson(rgsHier);
			if (item != null)
			{
				rec.SourcesRC.Add(item);
			}
			return true;
		}

		private List<string> SplitForSubitems(RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = null;
			if (sData.Length > 0)
			{
				if (rsf.m_tlo.m_fHaveSub)
				{
					rgsHier = SplitString(sData, rsf.m_tlo.m_rgsDelimSub);
					rgsHier = PruneEmptyStrings(rgsHier);
				}
				else
				{
					rgsHier = new List<string>();
					rgsHier.Add(sData);
				}
			}
			return rgsHier;
		}

		private bool StoreRestriction(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				if (rsf.m_tlo.m_default != null && !rec.RestrictionsRC.Contains(rsf.m_tlo.m_default))
				{
					rec.RestrictionsRC.Add(rsf.m_tlo.m_default);
				}
				return true;
			}

			if (m_mapRestriction.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.RestrictionsOA.PossibilitiesOS, m_mapRestriction);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapRestriction);
			if (poss != null)
			{
				if (!rec.RestrictionsRC.Contains(poss))
				{
					rec.RestrictionsRC.Add(poss);
				}
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewRestriction(rgsHier);
			if (item != null)
			{
				rec.RestrictionsRC.Add(item);
			}
			return true;
		}

		private ICmPossibility CreateNewRestriction(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator, m_cache.LangProject.RestrictionsOA.PossibilitiesOS, m_mapRestriction, m_rgNewRestriction);
		}

		private bool StoreStatus(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				rec.StatusRA = rsf.m_tlo.m_default;
				return true;
			}

			if (m_mapStatus.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.StatusOA.PossibilitiesOS, m_mapStatus);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapStatus);
			if (poss != null)
			{
				rec.StatusRA = poss;
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewStatus(rgsHier);
			rec.StatusRA = item;
			return true;
		}

		private ICmPossibility CreateNewStatus(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator, m_cache.LangProject.StatusOA.PossibilitiesOS, m_mapStatus, m_rgNewStatus);
		}

		private bool StoreTimeOfEvent(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				if (rsf.m_tlo.m_default != null && !rec.TimeOfEventRC.Contains(rsf.m_tlo.m_default))
				{
					rec.TimeOfEventRC.Add(rsf.m_tlo.m_default);
				}
				return true;
			}

			if (m_mapTimeOfDay.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.TimeOfDayOA.PossibilitiesOS, m_mapTimeOfDay);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapTimeOfDay);
			if (poss != null)
			{
				if (!rec.TimeOfEventRC.Contains(poss))
				{
					rec.TimeOfEventRC.Add(poss);
				}
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewTimeOfDay(rgsHier);
			if (item != null)
			{
				rec.TimeOfEventRC.Add(item);
			}
			return true;
		}

		private ICmPossibility CreateNewTimeOfDay(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator, m_cache.LangProject.TimeOfDayOA.PossibilitiesOS, m_mapTimeOfDay, m_rgNewTimeOfDay);
		}

		private bool StoreRecType(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				rec.TypeRA = rsf.m_tlo.m_default;
				return true;
			}

			if (m_mapRecType.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS, m_mapRecType);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapRecType);
			if (poss != null)
			{
				rec.TypeRA = poss;
				return true;
			}
			if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			var item = CreateNewRecType(rgsHier);
			rec.TypeRA = item;
			return true;
		}

		private ICmPossibility CreateNewRecType(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator, m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS, m_mapRecType, m_rgNewRecType);
		}

		private bool StoreParticipant(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			var partic = rec.ParticipantsOC.FirstOrDefault(part => part.RoleRA == null);
			if (partic == null)
			{
				partic = m_cache.ServiceLocator.GetInstance<IRnRoledParticFactory>().Create();
				rec.ParticipantsOC.Add(partic);
			}
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				var def = rsf.m_tlo.m_default as ICmPerson;
				if (def != null && !partic.ParticipantsRC.Contains(def))
				{
					partic.ParticipantsRC.Add(def);
				}
				return true;
			}

			if (m_mapPeople.Count == 0)
			{
				FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
			}
			var poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
			if (poss != null)
			{
				if (!partic.ParticipantsRC.Contains(poss as ICmPerson))
				{
					partic.ParticipantsRC.Add(poss as ICmPerson);
				}
				return true;
			}
			if (!rsf.m_tlo.m_fIgnoreNewStuff)
			{
				var item = CreateNewPerson(rgsHier);
				if (item != null)
				{
					partic.ParticipantsRC.Add(item);
					return true;
				}
			}
			return false;
		}

		private bool StoreCustomListRefItem(IRnGenericRec rec, RnSfMarker rsf, string sData, ICmPossibilityList list)
		{
			// First, get the existing data so we can check whether the new item is needed,
			// and so that we know where to insert it (at the end) if it is.
			var chvo = m_cache.DomainDataByFlid.get_VecSize(rec.Hvo, rsf.m_flid);
			int[] hvosField;
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				m_cache.DomainDataByFlid.VecProp(rec.Hvo, rsf.m_flid, chvo, out chvo, arrayPtr);
				hvosField = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}
			ICmPossibility poss;
			var rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				poss = rsf.m_tlo.m_default;
				if (poss != null && !hvosField.Contains(poss.Hvo) && poss.ClassID == list.ItemClsid)
				{
					m_cache.DomainDataByFlid.Replace(rec.Hvo, rsf.m_flid, chvo, chvo, new[] { poss.Hvo }, 1);
				}
				return true;
			}
			switch (list.OwningFlid)
			{
				case LangProjectTags.kflidAnthroList:
					if (m_mapAnthroCode.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_mapAnthroCode);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapAnthroCode);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewAnthroItem(rgsHier);
					}
					break;
				case LangProjectTags.kflidConfidenceLevels:
					if (m_mapConfidence.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS, m_mapConfidence);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapConfidence);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewConfidenceItem(rgsHier);
					}
					break;
				case LangProjectTags.kflidLocations:
					if (m_mapLocation.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.LocationsOA.PossibilitiesOS, m_mapLocation);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapLocation);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewLocation(rgsHier);
					}
					break;
				case RnResearchNbkTags.kflidRecTypes:
					if (m_mapRecType.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS, m_mapRecType);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapRecType);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewRecType(rgsHier);
					}
					break;
				case LangProjectTags.kflidTextMarkupTags:
					if (m_mapPhraseTag.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS, m_mapPhraseTag);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapPhraseTag);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewPhraseTag(rgsHier);
					}
					break;
				case LangProjectTags.kflidPeople:
					if (m_mapPeople.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewPerson(rgsHier);
					}
					break;
				case LangProjectTags.kflidRestrictions:
					if (m_mapRestriction.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.RestrictionsOA.PossibilitiesOS, m_mapRestriction);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapRestriction);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewRestriction(rgsHier);
					}
					break;
				case LangProjectTags.kflidStatus:
					if (m_mapStatus.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.StatusOA.PossibilitiesOS, m_mapStatus);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapStatus);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewStatus(rgsHier);
					}
					break;
				case LangProjectTags.kflidTimeOfDay:
					if (m_mapTimeOfDay.Count == 0)
					{
						FillPossibilityMap(rsf, m_cache.LangProject.TimeOfDayOA.PossibilitiesOS, m_mapTimeOfDay);
					}
					poss = FindPossibilityOrNull(rgsHier, m_mapTimeOfDay);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						poss = CreateNewTimeOfDay(rgsHier);
					}
					break;
				default:
					Dictionary<string, ICmPossibility> map;
					if (!m_mapListMapPossibilities.TryGetValue(list.Guid, out map))
					{
						map = new Dictionary<string, ICmPossibility>();
						FillPossibilityMap(rsf, list.PossibilitiesOS, map);
						m_mapListMapPossibilities.Add(list.Guid, map);
					}
					List<ICmPossibility> rgNew;
					if (!m_mapNewPossibilities.TryGetValue(list.Guid, out rgNew))
					{
						rgNew = new List<ICmPossibility>();
						m_mapNewPossibilities.Add(list.Guid, rgNew);
					}
					poss = FindPossibilityOrNull(rgsHier, map);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
					{
						CmPossibilityCreator creator = null;
						switch (list.ItemClsid)
						{
							case CmPossibilityTags.kClassId:
								creator = PossibilityCreator;
								break;
							case CmLocationTags.kClassId:
								creator = LocationCreator;
								break;
							case CmPersonTags.kClassId:
								creator = PersonCreator;
								break;
							case CmAnthroItemTags.kClassId:
								creator = AnthroItemCreator;
								break;
							case CmCustomItemTags.kClassId:
								creator = CustomItemCreator;
								break;
							case CmSemanticDomainTags.kClassId:
								creator = SemanticDomainCreator;
								break;
							// These are less likely, but legal, so we have to allow for them.
							case MoMorphTypeTags.kClassId:
								creator = MorphTypeCreator;
								break;
							case PartOfSpeechTags.kClassId:
								creator = NewPartOfSpeechCreator;
								break;
							case LexEntryTypeTags.kClassId:
								creator = NewLexEntryTypeCreator;
								break;
							case LexRefTypeTags.kClassId:
								creator = NewLexRefTypeCreator;
								break;
						}

						if (creator != null)
						{
							poss =  CreateNewPossibility(rgsHier, creator, list.PossibilitiesOS, map, rgNew);
						}
					}
					break;
			}
			if (poss != null && !hvosField.Contains(poss.Hvo) && poss.ClassID == list.ItemClsid)
			{
				m_cache.DomainDataByFlid.Replace(rec.Hvo, rsf.m_flid, chvo, chvo, new[] { poss.Hvo }, 1);
				return true;
			}
			return !rsf.m_tlo.m_fIgnoreNewStuff;
		}

		private static void FillPossibilityMap(RnSfMarker rsf, ILcmOwningSequence<ICmPossibility> seq, Dictionary<string, ICmPossibility> map)
		{
			if (seq == null || seq.Count == 0)
			{
				return;
			}
			var fAbbrev = rsf.m_tlo.m_pnt == PossNameType.kpntAbbreviation;
			foreach (ICmPossibility poss in seq)
			{
				var sKey = fAbbrev ? poss.Abbreviation.AnalysisDefaultWritingSystem.Text : poss.Name.AnalysisDefaultWritingSystem.Text;
				if (string.IsNullOrEmpty(sKey))
				{
					continue;
				}
				sKey = sKey.ToLowerInvariant();
				if (map.ContainsKey(sKey))
				{
					continue;
				}
				map.Add(sKey, poss);
				FillPossibilityMap(rsf, poss.SubPossibilitiesOS, map);
			}
		}

		private ICmPossibility CreateNewPossibility(List<string> rgsHier, CmPossibilityCreator factory, ILcmOwningSequence<ICmPossibility> possList, Dictionary<string, ICmPossibility> map, List<ICmPossibility> rgNew)
		{
			ICmPossibility possParent = null;
			ICmPossibility poss = null;
			int i;
			for (i = 0; i < rgsHier.Count; ++i)
			{
				if (!map.TryGetValue(rgsHier[i].ToLowerInvariant(), out poss))
				{
					break;
				}

				if (i > 0 && poss.Owner != possParent)
				{
					break;
				}
				possParent = poss;
			}
			if (i == rgsHier.Count)
			{
				// program bug -- shouldn't get here!
				Debug.Assert(i < rgsHier.Count);
				return null;
			}
			if (poss != null && i > 0 && poss.Owner != possParent)
			{
				// we can't create a duplicate name at a lower level in our current alogrithm!
				// Complain and do nothing...
				return null;
			}
			ICmPossibility itemParent = possParent as ICmAnthroItem;
			ICmPossibility item = null;
			for (; i < rgsHier.Count; ++i)
			{
				item = factory.Create();
				if (itemParent == null)
				{
					possList.Add(item);
				}
				else
				{
					itemParent.SubPossibilitiesOS.Add(item);
				}
				var tss = TsStringUtils.MakeString(rgsHier[i], m_cache.DefaultAnalWs);
				item.Name.AnalysisDefaultWritingSystem = tss;
				item.Abbreviation.AnalysisDefaultWritingSystem = tss;
				map.Add(rgsHier[i].ToLowerInvariant(), item);
				rgNew.Add(item);
				itemParent = item;
			}
			return item;
		}

		public static bool InitializeWritingSystemCombo(string sWs, LcmCache cache, ComboBox cbWritingSystem)
		{
			return InitializeWritingSystemCombo(sWs, cache, cbWritingSystem, cache.ServiceLocator.WritingSystems.AllWritingSystems.ToArray());
		}


		public static bool InitializeWritingSystemCombo(string sWs, LcmCache cache, ComboBox cbWritingSystem, CoreWritingSystemDefinition[] writingSystems)
		{
			if (string.IsNullOrEmpty(sWs))
			{
				sWs = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultAnalWs);
			}
			cbWritingSystem.Items.Clear();
			cbWritingSystem.Sorted = true;
			cbWritingSystem.Items.AddRange(writingSystems);
			foreach (CoreWritingSystemDefinition ws in cbWritingSystem.Items)
			{
				if (ws.Id == sWs)
				{
					cbWritingSystem.SelectedItem = ws;
					return true;
				}
			}
			return false;
		}

		private void m_tbSaveAsFileName_TextChanged(object sender, EventArgs e)
		{
			DirtySettings = true;
		}
		private enum OFType { Database, Project, Settings, SaveAs } // openfile type
	}
}
