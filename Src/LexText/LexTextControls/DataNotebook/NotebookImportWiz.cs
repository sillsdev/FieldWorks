// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NotebookImportWiz.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using XCore;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;
using System.Reflection;
using System.Globalization;
using SIL.FieldWorks.Common.RootSites;
using SilEncConverters40;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This wizard steps the user through setting up to import a standard format anthropology
	/// database file (and then importing it).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
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

		private FdoCache m_cache;
		private IFwMetaDataCacheManaged m_mdc;
		private IVwStylesheet m_stylesheet;
		private Mediator m_mediator;
		private IWritingSystemManager m_wsManager;
		private IStTextFactory m_factStText;
		private IStTextRepository m_repoStText;
		private IStTxtParaFactory m_factPara;
		private ICmPossibilityListRepository m_repoList;

		private bool m_fCanceling = false;
		private bool m_fDirtySettings = false;
		private OpenFileDialogAdapter openFileDialog;

		/// <summary>
		/// This class defines an encapsulation of factories for ICmPossibility and its
		/// subclasses.  This allows some a sizable chunk of code to be written only once.
		/// </summary>
		public class CmPossibilityCreator
		{
			private ICmPossibilityFactory m_fact;
			public CmPossibilityCreator()
			{
			}
			public CmPossibilityCreator(ICmPossibilityFactory fact)
			{
				m_fact = fact;
			}
			public virtual ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private CmPossibilityCreator m_factPossibility;
		public CmPossibilityCreator PossibilityCreator
		{
			get
			{
				if (m_factPossibility == null)
					m_factPossibility = new CmPossibilityCreator(m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>());
				return m_factPossibility;
			}
		}

		/// <summary>
		/// This class encapsulates an ICmAnthroItemFactory to look like it's creating
		/// ICmPossibility objects.  This allows some a sizable chunk of code to be written
		/// only once.
		/// </summary>
		public class CmAnthroItemCreator : CmPossibilityCreator
		{
			private ICmAnthroItemFactory m_fact;
			public CmAnthroItemCreator(ICmAnthroItemFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private CmAnthroItemCreator m_factAnthroItem;
		public CmAnthroItemCreator AnthroItemCreator
		{
			get
			{
				if (m_factAnthroItem == null)
					m_factAnthroItem = new CmAnthroItemCreator(m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>());
				return m_factAnthroItem;
			}
		}

		/// <summary>
		/// This class encapsulates an ICmLocationFactory to look like it's creating
		/// ICmPossibility objects.  This allows some a sizable chunk of code to be written
		/// only once.
		/// </summary>
		public class CmLocationCreator : CmPossibilityCreator
		{
			private ICmLocationFactory m_fact;
			public CmLocationCreator(ICmLocationFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private CmLocationCreator m_factLocation;
		public CmLocationCreator LocationCreator
		{
			get
			{
				if (m_factLocation == null)
					m_factLocation = new CmLocationCreator(m_cache.ServiceLocator.GetInstance<ICmLocationFactory>());
				return m_factLocation;
			}
		}

		/// <summary>
		/// This class encapsulates an ICmPersonFactory to look like it's creating
		/// ICmPossibility objects.  This allows some a sizable chunk of code to be written
		/// only once.
		/// </summary>
		public class CmPersonCreator : CmPossibilityCreator
		{
			private ICmPersonFactory m_fact;
			public CmPersonCreator(ICmPersonFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private CmPersonCreator m_factPerson;
		public CmPersonCreator PersonCreator
		{
			get
			{
				if (m_factPerson == null)
					m_factPerson = new CmPersonCreator(m_cache.ServiceLocator.GetInstance<ICmPersonFactory>());
				return m_factPerson;
			}
		}

		/// <summary>
		/// Creator wrapping ICmCustomItemFactory
		/// </summary>
		public class CmCustomItemCreator : CmPossibilityCreator
		{
			private ICmCustomItemFactory m_fact;
			public CmCustomItemCreator(ICmCustomItemFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private CmCustomItemCreator m_factCustomItem;
		public CmCustomItemCreator CustomItemCreator
		{
			get
			{
				if (m_factCustomItem == null)
					m_factCustomItem = new CmCustomItemCreator(m_cache.ServiceLocator.GetInstance<ICmCustomItemFactory>());
				return m_factCustomItem;
			}
		}

		/// <summary>
		/// Creator wrapping ICmSemanticDomainFactory
		/// </summary>
		public class CmSemanticDomainCreator : CmPossibilityCreator
		{
			private ICmSemanticDomainFactory m_fact;
			public CmSemanticDomainCreator(ICmSemanticDomainFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private CmSemanticDomainCreator m_factSemanticDomain;
		public CmSemanticDomainCreator SemanticDomainCreator
		{
			get
			{
				if (m_factSemanticDomain == null)
					m_factSemanticDomain = new CmSemanticDomainCreator(m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>());
				return m_factSemanticDomain;
			}
		}

		/// <summary>
		/// Creator wrapping IMoMorphTypeFactory
		/// </summary>
		public class MoMorphTypeCreator : CmPossibilityCreator
		{
			private IMoMorphTypeFactory m_fact;
			public MoMorphTypeCreator(IMoMorphTypeFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private MoMorphTypeCreator m_factMorphType;
		public MoMorphTypeCreator MorphTypeCreator
		{
			get
			{
				if (m_factMorphType == null)
					m_factMorphType = new MoMorphTypeCreator(m_cache.ServiceLocator.GetInstance<IMoMorphTypeFactory>());
				return m_factMorphType;
			}
		}

		/// <summary>
		/// Creator wrapping IPartOfSpeechFactory
		/// </summary>
		public class PartOfSpeechCreator : CmPossibilityCreator
		{
			private IPartOfSpeechFactory m_fact;
			public PartOfSpeechCreator(IPartOfSpeechFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private PartOfSpeechCreator m_factPartOfSpeech;
		public PartOfSpeechCreator NewPartOfSpeechCreator
		{
			get
			{
				if (m_factPartOfSpeech == null)
					m_factPartOfSpeech = new PartOfSpeechCreator(m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>());
				return m_factPartOfSpeech;
			}
		}

		/// <summary>
		/// Creator wrapping ILexEntryTypeFactory
		/// </summary>
		public class LexEntryTypeCreator : CmPossibilityCreator
		{
			private ILexEntryTypeFactory m_fact;
			public LexEntryTypeCreator(ILexEntryTypeFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private LexEntryTypeCreator m_factLexEntryType;
		public LexEntryTypeCreator NewLexEntryTypeCreator
		{
			get
			{
				if (m_factLexEntryType == null)
					m_factLexEntryType = new LexEntryTypeCreator(m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>());
				return m_factLexEntryType;
			}
		}

		/// <summary>
		/// Creator wrapping ILexRefTypeFactory
		/// </summary>
		public class LexRefTypeCreator : CmPossibilityCreator
		{
			private ILexRefTypeFactory m_fact;
			public LexRefTypeCreator(ILexRefTypeFactory fact)
			{
				m_fact = fact;
			}
			public override ICmPossibility Create()
			{
				return m_fact.Create();
			}
		}
		private LexRefTypeCreator m_factLexRefType;
		public LexRefTypeCreator NewLexRefTypeCreator
		{
			get
			{
				if (m_factLexRefType == null)
					m_factLexRefType = new LexRefTypeCreator(m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>());
				return m_factLexRefType;
			}
		}

		/// <summary>
		/// Stores the information needed to make a link later, after all the records have
		/// been created.
		/// </summary>
		class PendingLink
		{
			public RnSfMarker Marker { get; set; }
			public Sfm2Xml.SfmField Field { get; set; }
			public IRnGenericRec Record { get; set; }
		}
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
		private bool m_QuickFinish = false;
		private int m_lastQuickFinishTab = 0;
		private string m_sFmtEncCnvLabel;

		DateTime m_dtStart;
		DateTime m_dtEnd;
		int m_cRecordsRead = 0;
		int m_cRecordsDeleted = 0;

		private Dictionary<int, string> m_mapFlidName = new Dictionary<int, string>();
		private Sfm2Xml.SfmFile m_SfmFile;

		private string m_recMkr = null;
		private string m_sInputMapFile = null;
		private string m_sSfmDataFile = null;
		private string m_sProjectFile = null;

		/// <summary>
		/// This class encapsulates the information for a log message.
		/// </summary>
		public class ImportMessage : IComparable
		{
			private string m_sMsg;
			private int m_lineNumber;

			public ImportMessage(string sMsg, int lineNumber)
			{
				m_sMsg = sMsg;
				m_lineNumber = lineNumber;
			}

			public string Message
			{
				get { return m_sMsg; }
			}

			public int LineNumber
			{
				get { return m_lineNumber; }
			}

			#region IComparable Members
			public int CompareTo(object obj)
			{
				ImportMessage that = obj as ImportMessage;
				if (that == null)
					return 1;
				if (this.Message == that.Message)
					return this.LineNumber.CompareTo(that.LineNumber);
				else
					return this.Message.CompareTo(that.Message);
			}
			#endregion
		}
		private List<ImportMessage> m_rgMessages = new List<ImportMessage>();

		public class EncConverterChoice
		{
			private string m_sConverter;
			private readonly IWritingSystem m_ws;
			private ECInterfaces.IEncConverter m_conv = null;

			/// <summary>
			/// Constructor using an XmlNode from the settings file.
			/// </summary>
			public EncConverterChoice(XmlNode xnConverter, IWritingSystemManager wsManager)
				: this(XmlUtils.GetManditoryAttributeValue(xnConverter, "ws"),
				XmlUtils.GetOptionalAttributeValue(xnConverter, "converter", null), wsManager)
			{
			}

			/// <summary>
			/// Constructor using the writing system identifier and Converter name explicitly.
			/// </summary>
			public EncConverterChoice(string sWs, string sConverter, IWritingSystemManager wsManager)
			{
				m_sConverter = sConverter;
				if (String.IsNullOrEmpty(m_sConverter))
					m_sConverter = Sfm2Xml.STATICS.AlreadyInUnicode;
				wsManager.GetOrSet(sWs, out m_ws);
			}

			/// <summary>
			/// Get the identifier for the writing system.
			/// </summary>
			public IWritingSystem WritingSystem
			{
				get { return m_ws; }
			}

			/// <summary>
			/// Get the encoding converter name for the writing system.
			/// </summary>
			public string ConverterName
			{
				get { return m_sConverter; }
				set
				{
					m_sConverter = value;
					if (String.IsNullOrEmpty(m_sConverter))
						m_sConverter = Sfm2Xml.STATICS.AlreadyInUnicode;
					m_conv = null;
				}
			}

			/// <summary>
			/// Get/set the actual encoding converter for the writing system (may be null).
			/// </summary>
			public ECInterfaces.IEncConverter Converter
			{
				get { return m_conv; }
				set { m_conv = value; }
			}

			/// <summary>
			/// Get the name of the writing system.
			/// </summary>
			public string Name
			{
				get { return m_ws.DisplayLabel; }
			}
		}

		private readonly Dictionary<string, EncConverterChoice> m_mapWsEncConv = new Dictionary<string, EncConverterChoice>();

		public enum SfFieldType
		{
			/// <summary>ignored</summary>
			Discard,
			/// <summary>Multi-paragraph text field</summary>
			Text,
			/// <summary>Simple string text field</summary>
			String,
			/// <summary>Date/Time type field</summary>
			DateTime,
			/// <summary>List item reference field</summary>
			ListRef,
			/// <summary>Link field</summary>
			Link,
			/// <summary>Invalid field -- not handled by program!</summary>
			Invalid
		};
		/// <summary>
		/// This struct stores the data associated with a single Standard Format Marker.
		/// </summary>
		public class RnSfMarker
		{
			//:> Data loaded from the settings file.
			internal string m_sMkr;			// The field marker (without the leading \).
			internal int m_flid;			// Field identifier for destination in FieldWorks database.
											// If zero, then this field is discarded on import.
			internal string m_sName;		// field name for display (read from resources)
			internal string m_sMkrOverThis;	// Field marker of parent field, if any.

			// If record specifier, level of the record in the hierarchy (1 = root, 0 = not a record
			// specifier).
			internal int m_nLevel;

			/// <summary>
			/// This struct stores the options data associated with a structured text destination.
			/// </summary>
			internal class TextOptions
			{
				internal string m_sStyle;
				internal bool m_fStartParaNewLine;
				internal bool m_fStartParaBlankLine;
				internal bool m_fStartParaIndented;
				internal bool m_fStartParaShortLine;
				internal int m_cchShortLim;
				internal string m_wsId;
				internal IWritingSystem m_ws;
			};
			internal TextOptions m_txo = new TextOptions();

			/// <summary>
			/// This struct stores the options data associated with a topics list destination.
			/// </summary>
			internal class TopicsListOptions
			{
				internal string m_wsId;
				internal IWritingSystem m_ws;
				internal bool m_fHaveMulti;
				internal string m_sDelimMulti;
				internal bool m_fHaveSub;
				internal string m_sDelimSub;
				internal bool m_fHaveBetween;
				internal string m_sMarkStart;
				internal string m_sMarkEnd;
				internal bool m_fHaveBefore;
				internal string m_sBefore;
				internal bool m_fIgnoreNewStuff;
				internal List<string> m_rgsMatch = new List<string>();
				internal List<string> m_rgsReplace = new List<string>();
				internal string m_sEmptyDefault;
				internal PossNameType m_pnt;
				// value looked up for m_sEmptyDefault.
				internal ICmPossibility m_default;
				// Parsed versions of the strings above, split into possibly multiple delimiters.
				internal string[] m_rgsDelimMulti;
				internal string[] m_rgsDelimSub;
				internal string[] m_rgsMarkStart;
				internal string[] m_rgsMarkEnd;
				internal string[] m_rgsBefore;
			};
			internal TopicsListOptions m_tlo = new TopicsListOptions();

			/// <summary>
			/// This struct stores the options data associated with a date destination.
			/// </summary>
			internal class DateOptions
			{
				internal List<string> m_rgsFmt = new List<string>();
			};
			internal DateOptions m_dto = new DateOptions();

			/// <summary>
			/// This struct stores the options data associated with a string destination.
			/// </summary>
			internal class StringOptions
			{
				internal string m_wsId;
				internal IWritingSystem m_ws;
			};
			internal StringOptions m_sto = new StringOptions();

			// not sure how/whether to use these (from the C++ code)
			//internal string m_sLng;		// Language of the field data.
			//internal int m_wsDefault;		// Default writing system for the field.
		}
		/// <summary>
		/// Dictionary of std format marker mapping objects loaded from the map file.
		/// </summary>
		Dictionary<string, RnSfMarker> m_mapMkrRsfFromFile = new Dictionary<string, RnSfMarker>();
		/// <summary>
		/// Dictionary of std format marker mapping objects that match up against the input file.
		/// These may be copied from m_rgsfmFromMapFile or created with default settings.
		/// </summary>
		Dictionary<string, RnSfMarker> m_mapMkrRsf = new Dictionary<string, RnSfMarker>();

		public class CharMapping
		{
			private string m_sBeginMkr;
			private string m_sEndMkr;
			private bool m_fEndWithWord;
			private string m_sDestWsId;
			private IWritingSystem m_ws;
			private string m_sDestStyle;
			private bool m_fIgnoreMarker;

			public CharMapping()
			{
			}

			public CharMapping(XmlNode xn)
			{
				m_sBeginMkr = XmlUtils.GetManditoryAttributeValue(xn, "begin");
				m_sEndMkr = XmlUtils.GetManditoryAttributeValue(xn, "end");
				m_fEndWithWord = XmlUtils.GetOptionalBooleanAttributeValue(xn, "endWithWord", false);
				m_fIgnoreMarker = XmlUtils.GetOptionalBooleanAttributeValue(xn, "ignore", false);
				m_sDestStyle = XmlUtils.GetOptionalAttributeValue(xn, "style", null);
				m_sDestWsId = XmlUtils.GetOptionalAttributeValue(xn, "ws", null);
			}

			public string BeginMarker
			{
				get { return m_sBeginMkr; }
				set { m_sBeginMkr = value; }
			}

			public string EndMarker
			{
				get { return m_sEndMkr; }
				set { m_sEndMkr = value; }
			}

			public bool EndWithWord
			{
				get { return m_fEndWithWord; }
				set { m_fEndWithWord = value; }
			}

			public string DestinationWritingSystemId
			{
				get { return m_sDestWsId; }
				set { m_sDestWsId = value; }
			}

			public IWritingSystem DestinationWritingSystem
			{
				get { return m_ws; }
				set { m_ws = value; }
			}

			public string DestinationStyle
			{
				get { return m_sDestStyle; }
				set { m_sDestStyle = value; }
			}

			public bool IgnoreMarkerOnImport
			{
				get { return m_fIgnoreMarker; }
				set { m_fIgnoreMarker = value; }
			}
		}

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public NotebookImportWiz()
		{
			InitializeComponent();

			openFileDialog = new OpenFileDialogAdapter();

			m_sStdImportMap = String.Format(DirectoryFinder.FWCodeDirectory +
				"{0}Language Explorer{0}Import{0}NotesImport.map", Path.DirectorySeparatorChar);
			m_ExtraButtonLeft = m_btnBack.Left - (m_btnCancel.Width + kdxpCancelHelpButtonGap);
			m_OriginalCancelButtonLeft = m_btnCancel.Left;
			m_btnQuickFinish.Visible = false;
			m_btnQuickFinish.Left = m_OriginalCancelButtonLeft;
			m_btnCancel.Visible = true;
			m_sFmtEncCnvLabel = lblMappingLanguagesInstructions.Text;

			// Need to align SaveMapFile and QuickFinish to top of other dialog buttons (FWNX-833)
			int normalDialogButtonTop = m_btnHelp.Top;
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
		public void Init(FdoCache cache, Mediator mediator)
		{
			m_cache = cache;
			m_mediator = mediator;
			m_mdc = cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			m_wsManager = m_cache.ServiceLocator.WritingSystemManager;
			lblMappingLanguagesInstructions.Text = String.Format(m_sFmtEncCnvLabel, cache.ProjectId.Name);

			m_tbDatabaseFileName.Text = m_mediator.PropertyTable.GetStringProperty("DataNotebookImportDb", String.Empty);
			m_tbProjectFileName.Text = m_mediator.PropertyTable.GetStringProperty("DataNotebookImportPrj", String.Empty);
			m_tbSettingsFileName.Text = m_mediator.PropertyTable.GetStringProperty("DataNotebookImportMap", String.Empty);
			if (String.IsNullOrEmpty(m_tbSettingsFileName.Text) || m_tbSettingsFileName.Text == m_sStdImportMap)
			{
				m_tbSettingsFileName.Text = m_sStdImportMap;
				if (!String.IsNullOrEmpty(m_tbDatabaseFileName.Text))

				{
					m_tbSaveAsFileName.Text = Path.Combine(Path.GetDirectoryName(m_tbDatabaseFileName.Text),
						Path.GetFileNameWithoutExtension(m_tbDatabaseFileName.Text) + "-import-settings.map");
				}
			}
			else
			{
				m_tbSaveAsFileName.Text = m_tbSettingsFileName.Text;
				m_fDirtySettings = false;
			}
			m_stylesheet = AnthroStyleSheetFromMediator(mediator);
			if (m_stylesheet == null)
			{
				FwStyleSheet styles = new FwStyleSheet();
				styles.Init(m_cache, m_cache.LangProject.Hvo, LangProjectTags.kflidStyles);
				m_stylesheet = styles;
			}
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

			if (helpTopic != null)
				ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, helpTopic);
		}

		protected override void OnCancelButton()
		{
			if (m_fCanceling)
				return;
			m_fCanceling = true;
			base.OnCancelButton();
			if (CurrentStepNumber == 0)
				return;

			this.DialogResult = DialogResult.Cancel;

			// if it's known to be dirty OR the shift key is down - ask to save the settings file
			if (m_fDirtySettings || (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				// LT-7057: if no settings file, don't ask to save
				if (UsesInvalidFileNames(true))
					return;	// finsih with out prompting to save...

				// ask to save the settings
				DialogResult result = MessageBox.Show(this,
					LexTextControls.ksAskRememberImportSettings,
					LexTextControls.ksSaveSettings_,
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button3);

				if (result == DialogResult.Yes)
				{
					// before saving we need to make sure all the data structures are populated
					while (CurrentStepNumber <= 6)
					{
						EnableNextButton();
						m_CurrentStepNumber++;
					}
					SaveSettings();
				}
				else if (result == DialogResult.Cancel)
				{
					// This is how do we stop the cancel process...
					this.DialogResult = DialogResult.None;
					m_fCanceling = false;
				}
			}
		}

		public static IVwStylesheet AnthroStyleSheetFromMediator(Mediator mediator)
		{
			if (mediator == null || mediator.PropertyTable == null)
				return null;
			Form mainWindow = (Form)mediator.PropertyTable.GetValue("window");
			PropertyInfo pi = null;
			if (mainWindow != null)
				pi = mainWindow.GetType().GetProperty("AnthroStyleSheet");
			if (pi != null)
				return pi.GetValue(mainWindow, null) as FwStyleSheet;
			else
				return mediator.PropertyTable.GetValue("AnthroStyleSheet") as FwStyleSheet;
		}

		private void FillLanguageMappingView()
		{
			m_lvMappingLanguages.Items.Clear();
			var wss = new HashSet<IWritingSystem>();
			foreach (string sWs in m_mapWsEncConv.Keys)
			{
				EncConverterChoice ecc = m_mapWsEncConv[sWs];
				wss.Add(ecc.WritingSystem);
				m_lvMappingLanguages.Items.Add(new ListViewItem(new[] { ecc.Name, ecc.ConverterName }) {Tag = ecc});
			}
			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				if (wss.Contains(ws))
					continue;
				wss.Add(ws);
				ListViewItem lvi = CreateListViewItemForWS(ws);
				m_lvMappingLanguages.Items.Add(lvi);
				m_fDirtySettings = true;
			}
			m_lvMappingLanguages.Sort();
			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
			m_btnAddWritingSystem.Initialize(m_cache, m_mediator.HelpTopicProvider, app, m_stylesheet, wss);
		}

		private ListViewItem CreateListViewItemForWS(IWritingSystem ws)
		{
			string sName = ws.DisplayLabel;
			string sEncCnv;
			if (String.IsNullOrEmpty(ws.LegacyMapping))
				sEncCnv = Sfm2Xml.STATICS.AlreadyInUnicode;
			else
				sEncCnv = ws.LegacyMapping;

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
			return new ListViewItem(new[] { sName, sEncCnv }) {Tag = ecc};
		}

		private void btnBackup_Click(object sender, EventArgs e)
		{
			using (var dlg = new BackupProjectDlg(m_cache, FwUtils.ksFlexAbbrev, m_mediator.HelpTopicProvider))
				dlg.ShowDialog(this);
		}

		enum OFType { Database, Project, Settings, SaveAs };	// openfile type

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
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ShoeboxAnthropologyDatabase,
						FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectAnthropologyStdFmtFile;
					break;
				case OFType.Project:
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ShoeboxProjectFiles,
						FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectShoeboxProjectFile;
					break;
				case OFType.Settings:
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ImportMapping,
						FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectLoadImportSettingsFile;
					break;
				case OFType.SaveAs:
					openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.ImportMapping,
						FileFilterType.AllFiles);
					openFileDialog.Title = LexTextControls.ksSelectSaveImportSettingsFile;
					break;
			}
			openFileDialog.FilterIndex = 1;
			// don't require file to exist if it's "SaveAs"
			openFileDialog.CheckFileExists = (fileType != OFType.SaveAs);
			openFileDialog.Multiselect = false;

			bool done = false;
			while (!done)
			{
				// LT-6620 : putting in an invalid path was causing an exception in the openFileDialog.ShowDialog()
				// Now we make sure parts are valid before setting the values in the openfile dialog.
				string dir = string.Empty;
				try
				{
					dir = Path.GetDirectoryName(currentFile);
				}
				catch { }
				if (Directory.Exists(dir))
					openFileDialog.InitialDirectory = dir;
				// if we don't set it to something, it remembers the last file it saw. This can be
				// a very poor default if we just opened a valuable data file and are now choosing
				// a place to save settings (LT-8126)
				if (File.Exists(currentFile) || (fileType == OFType.SaveAs && Directory.Exists(dir)))
					openFileDialog.FileName = currentFile;
				else
					openFileDialog.FileName = "";

				if (openFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					bool isValid = false;
					string sFileType;
					if (fileType == OFType.Database)
					{
						sFileType = LexTextControls.ksStandardFormat;
						isValid = IsValidSfmFile(openFileDialog.FileName);
					}
					else if (fileType == OFType.Project)
					{
						sFileType = SIL.FieldWorks.LexText.Controls.LexTextControls.ksShoeboxProject;
						Sfm2Xml.IsSfmFile validFile = new Sfm2Xml.IsSfmFile(openFileDialog.FileName);
						isValid = validFile.IsValid;
					}
					else if (fileType == OFType.SaveAs)
					{
						sFileType = LexTextControls.ksXmlSettings;
						isValid = true;		// no requirements since the file will be overridden
					}
					else
					{
						sFileType = LexTextControls.ksXmlSettings;
						isValid = IsValidMapFile(openFileDialog.FileName);
					}

					if (!isValid)
					{
						string msg = String.Format(LexTextControls.ksSelectedFileXInvalidY,
							openFileDialog.FileName, sFileType, System.Environment.NewLine);
						DialogResult dr = MessageBox.Show(this, msg,
							LexTextControls.ksPossibleInvalidFile,
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

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			// The wizard base class redraws the controls, so move the cancel button after it's
			// done ...
			m_OriginalCancelButtonLeft = m_btnHelp.Left - (m_btnCancel.Width + kdxpCancelHelpButtonGap);
			if (m_btnQuickFinish != null && m_btnBack != null && m_btnCancel != null &&
				m_OriginalCancelButtonLeft != 0)
			{
				m_ExtraButtonLeft = m_btnBack.Left - (m_btnCancel.Width + kdxpCancelHelpButtonGap);
				if (m_btnQuickFinish.Visible)
				{
					m_btnQuickFinish.Left = m_OriginalCancelButtonLeft;
					m_btnCancel.Left = m_ExtraButtonLeft;
				}
				else
					m_btnCancel.Left = m_OriginalCancelButtonLeft;
			}
		}

		private void btnModifyMappingLanguage_Click(object sender, EventArgs e)
		{
			if (m_lvMappingLanguages.SelectedItems.Count == 0)
				return;
			using (ImportEncCvtrDlg dlg = new ImportEncCvtrDlg())
			{
				ListViewItem lvi = m_lvMappingLanguages.SelectedItems[0];
				string sName = lvi.SubItems[0].Text;
				string sEncCnv = lvi.SubItems[1].Text;
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				dlg.Initialize(sName, sEncCnv, m_mediator.HelpTopicProvider, app);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					string sNewEncCnv = dlg.EncodingConverter;
					if (sNewEncCnv != sEncCnv)
					{
						lvi.SubItems[1].Text = sNewEncCnv;
						EncConverterChoice ecc = lvi.Tag as EncConverterChoice;
						ecc.ConverterName = sNewEncCnv;
						m_fDirtySettings = true;
					}
				}
			}
		}

		private void btnModifyContentMapping_Click(object sender, EventArgs e)
		{
			if (m_lvContentMapping.SelectedItems.Count == 0)
				return;
			using (AnthroFieldMappingDlg dlg = new AnthroFieldMappingDlg())
			{
				ListViewItem lvi = m_lvContentMapping.SelectedItems[0];
				RnSfMarker rsfm = lvi.Tag as RnSfMarker;
				var app = (IApp)m_mediator.PropertyTable.GetValue("App");
				dlg.Initialize(m_cache, m_mediator.HelpTopicProvider, app, rsfm,
					m_SfmFile, m_mapFlidName, m_stylesheet, m_mediator);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					rsfm = dlg.Results;
					lvi.SubItems[3].Text = rsfm.m_sName;
					lvi.Tag = rsfm;
					m_fDirtySettings = true;
				}
			}
		}

		private void cbRecordMarker_SelectedIndexChanged(object sender, EventArgs e)
		{
			string sRecMkr = m_cbRecordMarker.SelectedItem as string;
			Debug.Assert(sRecMkr != null);
			string sRecMkrBase = sRecMkr.Substring(1);
			foreach (string sMkr in m_mapMkrRsf.Keys)
			{
				RnSfMarker rsf = m_mapMkrRsf[sMkr];
				if (rsf.m_sMkr == sRecMkrBase)
					rsf.m_nLevel = 1;
				else
					rsf.m_nLevel = 0;
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
			using (ImportCharMappingDlg dlg = new ImportCharMappingDlg())
			{
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				dlg.Initialize(m_cache, m_mediator.HelpTopicProvider, app, m_stylesheet, null);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					CharMapping cmNew = new CharMapping();
					cmNew.BeginMarker = dlg.BeginMarker;
					cmNew.EndMarker = dlg.EndMarker;
					cmNew.EndWithWord = dlg.EndWithWord;
					cmNew.DestinationWritingSystemId = dlg.WritingSystemId;
					cmNew.DestinationStyle = dlg.StyleName;
					cmNew.IgnoreMarkerOnImport = dlg.IgnoreOnImport;
					m_rgcm.Add(cmNew);
					ListViewItem lvi = CreateListItemForCharMapping(cmNew);
					m_lvCharMappings.Items.Add(lvi);
					m_fDirtySettings = true;
				}
			}
		}

		private void btnModifyCharMapping_Click(object sender, EventArgs e)
		{
			if (m_lvCharMappings.SelectedItems.Count == 0)
				return;
			ListViewItem lvi = m_lvCharMappings.SelectedItems[0];
			using (ImportCharMappingDlg dlg = new ImportCharMappingDlg())
			{
				CharMapping cm = lvi.Tag as CharMapping;
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				dlg.Initialize(m_cache, m_mediator.HelpTopicProvider, app, m_stylesheet, cm);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					cm.BeginMarker = dlg.BeginMarker;
					cm.EndMarker = dlg.EndMarker;
					cm.EndWithWord = dlg.EndWithWord;
					cm.DestinationWritingSystemId = dlg.WritingSystemId;
					cm.DestinationStyle = dlg.StyleName;
					cm.IgnoreMarkerOnImport = dlg.IgnoreOnImport;
					ListViewItem lviNew = CreateListItemForCharMapping(cm);
					lvi.SubItems[0].Text = lviNew.SubItems[0].Text;
					lvi.SubItems[1].Text = lviNew.SubItems[1].Text;
					lvi.SubItems[2].Text = lviNew.SubItems[2].Text;
					lvi.SubItems[3].Text = lviNew.SubItems[3].Text;
					m_fDirtySettings = true;
				}
			}
		}

		private void btnDeleteCharMapping_Click(object sender, EventArgs e)
		{
			if (m_lvCharMappings.SelectedItems.Count == 0)
				return;
			ListViewItem lvi = m_lvCharMappings.SelectedItems[0];
			CharMapping cm = lvi.Tag as CharMapping;
			m_lvCharMappings.Items.Remove(lvi);
			m_rgcm.Remove(cm);
			m_fDirtySettings = true;
		}

		private void rbReplaceAllEntries_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbReplaceAllEntries.Checked)
				m_rbAddEntries.Checked = false;
		}

		private void rbAddEntries_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbAddEntries.Checked)
				m_rbReplaceAllEntries.Checked = false;
		}

		private void btnSaveMapFile_Click(object sender, EventArgs e)
		{
			SaveSettings();
		}

		private void SaveSettings()
		{
			m_mediator.PropertyTable.SetProperty("DataNotebookImportDb", m_tbDatabaseFileName.Text);
			m_mediator.PropertyTable.SetPropertyPersistence("DataNotebookImportDb", true);
			m_mediator.PropertyTable.SetProperty("DataNotebookImportPrj", m_tbProjectFileName.Text);
			m_mediator.PropertyTable.SetPropertyPersistence("DataNotebookImportPrj", true);
			m_mediator.PropertyTable.SetProperty("DataNotebookImportMap", m_tbSaveAsFileName.Text);
			m_mediator.PropertyTable.SetPropertyPersistence("DataNotebookImportMap", true);
			using (TextWriter tw = FileUtils.OpenFileForWrite(m_tbSaveAsFileName.Text, Encoding.UTF8))
			{
				try
				{
					string sRecMkr = m_cbRecordMarker.SelectedItem as string;
					string sRecMkrBase = String.Empty;
					if (String.IsNullOrEmpty(sRecMkr))
					{
						foreach (RnSfMarker rsf in m_mapMkrRsf.Values)
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
					using (XmlWriter xw = XmlWriter.Create(tw))
					{
						xw.WriteStartDocument();
						xw.WriteWhitespace(Environment.NewLine);
						string sDontEditEnglish = " DO NOT EDIT THIS FILE!  YOU HAVE BEEN WARNED! ";
						xw.WriteComment(sDontEditEnglish);
						xw.WriteWhitespace(Environment.NewLine);
						string sAutoEnglish = " The Fieldworks import process automatically maintains this file. ";
						xw.WriteComment(sAutoEnglish);
						xw.WriteWhitespace(Environment.NewLine);
						string sDontEdit = LexTextControls.ksDONOTEDIT;
						if (sDontEdit != sDontEditEnglish)
						{
							xw.WriteComment(sDontEdit);
							xw.WriteWhitespace(Environment.NewLine);
						}
						string sAuto = LexTextControls.ksAutomaticallyMaintains;
						if (sAuto != sAutoEnglish)
						{
							xw.WriteComment(sAuto);
							xw.WriteWhitespace(Environment.NewLine);
						}
						xw.WriteStartElement("ShoeboxImportSettings");
						foreach (string sWs in m_mapWsEncConv.Keys)
						{
							EncConverterChoice ecc = m_mapWsEncConv[sWs];
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteStartElement("EncodingConverter");
							xw.WriteAttributeString("ws", ecc.WritingSystem.Id);
							if (!String.IsNullOrEmpty(ecc.ConverterName) && ecc.ConverterName != Sfm2Xml.STATICS.AlreadyInUnicode)
								xw.WriteAttributeString("converter", ecc.ConverterName);
							xw.WriteEndElement();	// EncodingConverter
						}
						foreach (string sMkr in m_mapMkrRsf.Keys)
						{
							RnSfMarker rsf = m_mapMkrRsf[sMkr];
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteStartElement("Marker");
							xw.WriteAttributeString("tag", rsf.m_sMkr);
							xw.WriteAttributeString("flid", rsf.m_flid.ToString());
							if (!String.IsNullOrEmpty(rsf.m_sMkrOverThis))
								xw.WriteAttributeString("owner", rsf.m_sMkrOverThis);
							else if (rsf.m_nLevel == 0 && !String.IsNullOrEmpty(sRecMkrBase))
								xw.WriteAttributeString("owner", sRecMkrBase);
							WriteMarkerContents(xw, rsf);
							xw.WriteWhitespace(Environment.NewLine);
							xw.WriteEndElement();	// Marker
						}
						for (int i = 0; i < m_rgcm.Count; ++i)
						{
							CharMapping cm = m_rgcm[i];
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
								if (!String.IsNullOrEmpty(cm.DestinationStyle))
									xw.WriteAttributeString("style", cm.DestinationStyle);
								if (!String.IsNullOrEmpty(cm.DestinationWritingSystemId))
									xw.WriteAttributeString("ws", cm.DestinationWritingSystemId);
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
						m_fDirtySettings = false;
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
					foreach (string sFmt in rsf.m_dto.m_rgsFmt)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("DateFormat");
						xw.WriteAttributeString("value", sFmt);
						xw.WriteEndElement();
					}
					break;
				case SfFieldType.ListRef:
					if (!String.IsNullOrEmpty(rsf.m_tlo.m_sEmptyDefault))
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("Default");
						xw.WriteAttributeString("value", rsf.m_tlo.m_sEmptyDefault);
						xw.WriteEndElement();
					}
					xw.WriteWhitespace(Environment.NewLine);
					xw.WriteStartElement("Match");
					xw.WriteAttributeString("value",
						rsf.m_tlo.m_pnt == PossNameType.kpntName ? "name" : "abbr");
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
					for (int j = 0; j < rsf.m_tlo.m_rgsMatch.Count; ++j)
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("MatchReplaceChoice");
						xw.WriteAttributeString("match", rsf.m_tlo.m_rgsMatch[j]);
						xw.WriteAttributeString("replace", rsf.m_tlo.m_rgsReplace[j]);
						xw.WriteEndElement();
					}
					if (!String.IsNullOrEmpty(rsf.m_tlo.m_wsId))
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
					if (!String.IsNullOrEmpty(rsf.m_txo.m_sStyle))
					{
						xw.WriteWhitespace(Environment.NewLine);
						xw.WriteStartElement("TextStyle");
						xw.WriteAttributeString("value", rsf.m_txo.m_sStyle);
						xw.WriteEndElement();
					}
					xw.WriteWhitespace(Environment.NewLine);
					xw.WriteStartElement("StartPara");
					if (rsf.m_txo.m_fStartParaBlankLine)
						xw.WriteAttributeString("afterBlankLine", "true");
					if (rsf.m_txo.m_fStartParaIndented)
						xw.WriteAttributeString("forIndentedLine", "true");
					if (rsf.m_txo.m_fStartParaNewLine)
						xw.WriteAttributeString("forEachLine", "true");
					if (rsf.m_txo.m_fStartParaShortLine)
					{
						xw.WriteAttributeString("afterShortLine", "true");
						xw.WriteAttributeString("shortLineLim", rsf.m_txo.m_cchShortLim.ToString());
					}
					xw.WriteEndElement();
					if (!String.IsNullOrEmpty(rsf.m_txo.m_wsId))
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
				return SfFieldType.Discard;

			CellarPropertyType cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
			int clidDst = -1;
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
							int clidBase = clidDst;
							while (clidBase != 0 && clidBase != CmPossibilityTags.kClassId)
								clidBase = m_mdc.GetBaseClsId(clidBase);
							if (clidBase == CmPossibilityTags.kClassId)
								return SfFieldType.ListRef;
							else
								return SfFieldType.Invalid;
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
				case CellarPropertyType.MultiBigString:
				case CellarPropertyType.MultiBigUnicode:
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.String:
				case CellarPropertyType.BigString:
					return SfFieldType.String;
				case CellarPropertyType.GenDate:
				case CellarPropertyType.Time:
					return SfFieldType.DateTime;
				case CellarPropertyType.Unicode:
				case CellarPropertyType.BigUnicode:
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
		/// <param name="mapFile">file name to check</param>
		/// <returns>true if valid</returns>
		private static bool IsValidMapFile(string mapFile)
		{
			if (String.IsNullOrEmpty(mapFile) || !File.Exists(mapFile))
				return false;
			XmlDocument xmlMap = new System.Xml.XmlDocument();
			try
			{
				xmlMap.Load(mapFile);

				XmlNode root = xmlMap.DocumentElement;
				// make sure it has a root node of ShoeboxImportSettings
				if (root.Name != "ShoeboxImportSettings")
					return false;
				// make sure the top-level child nodes are all valid.
				foreach (XmlNode node in root.ChildNodes)
				{
					if (node.Name == "EncodingConverter")
						continue;
					if (node.Name == "Marker")
						continue;
					if (node.Name == "CharMapping")
						continue;
					if (node.Name == "ReplaceAll")
						continue;
					if (node.Name == "ShowLog")
						continue;
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
			if (String.IsNullOrEmpty(sFilename) || !File.Exists(sFilename))
				return false;
			Sfm2Xml.IsSfmFile validFile = new Sfm2Xml.IsSfmFile(sFilename);
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
					bool fStayHere = UsesInvalidFileNames(false);
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
				m_SfmFile = new Sfm2Xml.SfmFile(m_sSfmDataFile);
			}
			if (m_mapMkrRsf.Count == 0)
			{
				foreach (string sfm in m_SfmFile.SfmInfo)
				{
					if (sfm.StartsWith("_"))
						continue;
					RnSfMarker rsf = FindOrCreateRnSfMarker(sfm);
					m_mapMkrRsf.Add(rsf.m_sMkr, rsf);
				}
				m_lvContentMapping.Items.Clear();
				foreach (string sMkr in m_mapMkrRsf.Keys)
				{
					RnSfMarker rsf = m_mapMkrRsf[sMkr];
					ListViewItem lvi = new ListViewItem(new string[] {
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
				return;
			Sfm2Xml.ByteReader prjRdr = new Sfm2Xml.ByteReader(m_tbProjectFileName.Text);
			string sMkr;
			byte[] sfmData;
			byte[] badSfmData;
			string sDataFile = Path.GetFileName(m_tbDatabaseFileName.Text).ToLowerInvariant();
			bool fInDataDefs = false;
			while (prjRdr.GetNextSfmMarkerAndData(out sMkr, out sfmData, out badSfmData))
			{
				Sfm2Xml.Converter.MultiToWideError mwError;
				byte[] badData;
				switch (sMkr)
				{
					case "+db":
						if (sfmData.Length > 0)
						{
							string sData = Sfm2Xml.Converter.MultiToWideWithERROR(sfmData, 0,
								sfmData.Length - 1, Encoding.UTF8, out mwError, out badData);
							if (mwError == Sfm2Xml.Converter.MultiToWideError.None)
							{
								string sFile = Path.GetFileName(sData.Trim());
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
							string sData = Sfm2Xml.Converter.MultiToWideWithERROR(sfmData, 0,
								sfmData.Length - 1, Encoding.UTF8, out mwError, out badData);
							if (mwError == Sfm2Xml.Converter.MultiToWideError.None)
								m_recMkr = sData.Trim();
						}
						break;
				}
			}
		}

		private RnSfMarker FindOrCreateRnSfMarker(string mkr)
		{
			RnSfMarker rsf;
			if (m_mapMkrRsfFromFile.TryGetValue(mkr, out rsf))
				return rsf;
			RnSfMarker rsfNew = new RnSfMarker();
			rsfNew.m_sMkr = mkr;
			rsfNew.m_flid = 0;
			rsfNew.m_sName = LexTextControls.ksDoNotImport;
			return rsfNew;
		}

		private void InitializeKeyMarkers()
		{
			if (m_cbRecordMarker.Items.Count == 0)
			{
				Dictionary<int, string> mapOrderMarker = new Dictionary<int,string>();
				int select = -1;
				foreach (string sfm in m_SfmFile.SfmInfo)
				{
					int order = m_SfmFile.GetSFMOrder(sfm);
					mapOrderMarker[order] = sfm;
					if (sfm == m_recMkr)
						select = order;
				}
				for (int i = 1; i <= mapOrderMarker.Count; ++i)
				{
					string sMkr;
					if (mapOrderMarker.TryGetValue(i, out sMkr))
					{
						if (sMkr.StartsWith("_"))
							continue;
						string sShow = "\\" + sMkr;
						m_cbRecordMarker.Items.Add(sShow);
						if (i == select)
							m_cbRecordMarker.Text = sShow;
					}
				}
				if (select == -1)
					m_cbRecordMarker.SelectedIndex = 0;
			}
		}

		private void InitializeCharMappings()
		{
			if (m_lvCharMappings.Items.Count == 0)
			{
				foreach (CharMapping cm in m_rgcm)
				{
					ListViewItem lvi = CreateListItemForCharMapping(cm);
					m_lvCharMappings.Items.Add(lvi);
				}
			}
		}

		private ListViewItem CreateListItemForCharMapping(CharMapping cm)
		{
			string sWsName = String.Empty;
			string sWs = cm.DestinationWritingSystemId;
			if (!string.IsNullOrEmpty(sWs))
			{
				IWritingSystem ws;
				m_cache.ServiceLocator.WritingSystemManager.GetOrSet(sWs, out ws);
				Debug.Assert(ws != null);
				sWsName = ws.DisplayLabel;
			}
			string sStyle = cm.DestinationStyle;
			if (sStyle == null)
				sStyle = String.Empty;
			string sBegin = cm.BeginMarker;
			if (sBegin == null)
				sBegin = String.Empty;
			string sEnd = cm.EndMarker;
			if (sEnd == null)
				sEnd = String.Empty;
			return new ListViewItem(new[] { sBegin, sEnd, sWsName, sStyle }) {Tag = cm};
		}

		private void ShowSaveButtonOrNot()
		{
			if (!string.IsNullOrEmpty(m_tbSaveAsFileName.Text))
				m_btnSaveMapFile.Visible = true;
			else
				m_btnSaveMapFile.Visible = false;
		}

		private bool UsesInvalidFileNames(bool runSilent)
		{
			bool fStayHere = false;
			if (!IsValidMapFile(m_tbSettingsFileName.Text))
			{
				if (!runSilent)
				{
					string msg = String.Format(LexTextControls.ksInvalidSettingsFileX,
						m_tbSettingsFileName.Text);
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
				m_tbSettingsFileName.Focus();
			}
			else if (m_tbSaveAsFileName.Text.Length == 0)
			{
				if (!runSilent)
				{
					string msg = LexTextControls.ksUndefinedSettingsSaveFile;
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
				m_tbSaveAsFileName.Focus();
			}
			else if (m_tbSaveAsFileName.Text != m_tbSettingsFileName.Text)
			{
				try
				{
					System.IO.FileInfo fi = new System.IO.FileInfo(m_tbSaveAsFileName.Text);
					if (!fi.Exists)
					{
						// make sure we can create the file for future use
						using (var s2 = new FileStream(m_tbSaveAsFileName.Text, FileMode.Create))
							s2.Close();
						fi.Delete();
					}
				}
				catch
				{
					if (!runSilent)
					{
						string msg = String.Format(LexTextControls.ksInvalidSettingsSaveFileX, m_tbSaveAsFileName.Text);
						MessageBox.Show(this, msg, LexTextControls.ksInvalidFile,
							MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
					string msg = String.Format(LexTextControls.ksSettingsSaveFileSameAsDatabaseFile,
						m_tbSaveAsFileName.Text, m_tbDatabaseFileName.Text);
					MessageBox.Show(this, msg, LexTextControls.ksInvalidFile,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				fStayHere = true;
			}
			return fStayHere;
		}

		private bool EnableNextButton()
		{
			//AllowQuickFinishButton();	// this should be done at least before each step
			bool rval = false;
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
							m_tbSaveAsFileName.Text = Path.Combine(Path.GetDirectoryName(m_tbDatabaseFileName.Text),
								Path.GetFileNameWithoutExtension(m_tbDatabaseFileName.Text) + "-import-settings.map");
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
			if (m_CurrentStepNumber < 6 &&
				IsValidSfmFile(m_tbDatabaseFileName.Text) &&
				IsValidMapFile(m_tbSettingsFileName.Text))
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
				return;

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
			if (String.IsNullOrEmpty(m_tbSaveAsFileName.Text) &&
				IsValidSfmFile(m_tbDatabaseFileName.Text))
			{
				string sDatabase = m_tbDatabaseFileName.Text;
				string sSaveAs = Path.ChangeExtension(sDatabase, "map");
				if (sSaveAs.ToLowerInvariant() != sDatabase.ToLowerInvariant())
					m_tbSaveAsFileName.Text = sSaveAs;
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
			if (String.IsNullOrEmpty(m_tbSaveAsFileName.Text) &&
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
				FillFlidNameMap();

			XmlDocument xmlMap = new System.Xml.XmlDocument();
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
								ReadCharMapping(xn);
								break;
							case "ReplaceAll":
								bool fReplaceAll = XmlUtils.GetOptionalBooleanAttributeValue(xn, "value", false);
								if (fReplaceAll)
									m_rbReplaceAllEntries.Checked = true;
								else
									m_rbAddEntries.Checked = true;
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
				RnSfMarker sfm = new RnSfMarker();
				sfm.m_sMkr = XmlUtils.GetManditoryAttributeValue(xnMarker, "tag");
				sfm.m_flid = XmlUtils.GetMandatoryIntegerAttributeValue(xnMarker, "flid");
				sfm.m_sMkrOverThis = XmlUtils.GetOptionalAttributeValue(xnMarker, "owner");
				if (sfm.m_flid == 0)
				{
					sfm.m_sName = LexTextControls.ksDoNotImport;
				}
				else
				{
					sfm.m_sName = m_mapFlidName[sfm.m_flid];
					int clidDest = 0;
					switch ((CellarPropertyType)m_mdc.GetFieldType(sfm.m_flid))
					{
						case CellarPropertyType.Time:
						case CellarPropertyType.GenDate:
							foreach (XmlNode xn in xnMarker.SelectNodes("./DateFormat"))
							{
								string sFormat = XmlUtils.GetManditoryAttributeValue(xn, "value");
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
						case CellarPropertyType.MultiBigString:
						case CellarPropertyType.MultiBigUnicode:
						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
						case CellarPropertyType.BigString:
						case CellarPropertyType.String:
							foreach (XmlNode xn in xnMarker.SelectNodes("./StringWrtSys"))
							{
								sfm.m_sto.m_wsId = XmlUtils.GetManditoryAttributeValue(xn, "ws");
							}
							break;
						// The following types do not occur in RnGenericRec fields.
						case CellarPropertyType.BigUnicode:
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
							sfm.m_nLevel = XmlUtils.GetMandatoryIntegerAttributeValue(xn, "level");
					}
				}
				if (m_mapMkrRsfFromFile.ContainsKey(sfm.m_sMkr))
					m_mapMkrRsfFromFile[sfm.m_sMkr] = sfm;
				else
					m_mapMkrRsfFromFile.Add(sfm.m_sMkr, sfm);
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
						string sMatch = XmlUtils.GetManditoryAttributeValue(xn, "value");
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
							sfm.m_tlo.m_sDelimMulti = XmlUtils.GetManditoryAttributeValue(xn, "sep");
						}
						break;
					case "Subchoice":
						sfm.m_tlo.m_fHaveSub = true;
						sfm.m_tlo.m_sDelimSub = XmlUtils.GetManditoryAttributeValue(xn, "sep");
						break;
					case "Default":
						sfm.m_tlo.m_sEmptyDefault = XmlUtils.GetManditoryAttributeValue(xn, "value");
						sfm.m_tlo.m_default = null;
						break;
					case "DelimitChoice":
						sfm.m_tlo.m_fHaveBetween = true;
						sfm.m_tlo.m_sMarkStart = XmlUtils.GetManditoryAttributeValue(xn, "start");
						sfm.m_tlo.m_sMarkEnd = XmlUtils.GetManditoryAttributeValue(xn, "end");
						break;
					case "StopChoices":
						sfm.m_tlo.m_fHaveBefore = true;
						sfm.m_tlo.m_sBefore = XmlUtils.GetManditoryAttributeValue(xn, "value");
						break;
					case "IgnoreNewChoices":
						sfm.m_tlo.m_fIgnoreNewStuff = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "MatchReplaceChoice":
						sfm.m_tlo.m_rgsMatch.Add(XmlUtils.GetManditoryAttributeValue(xn, "match"));
						sfm.m_tlo.m_rgsReplace.Add(XmlUtils.GetOptionalAttributeValue(xn, "replace", String.Empty));
						break;
					case "ItemWrtSys":
						sfm.m_tlo.m_wsId = XmlUtils.GetManditoryAttributeValue(xn, "ws");
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
						sfm.m_txo.m_sStyle = XmlUtils.GetManditoryAttributeValue(xn, "value");
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
						sfm.m_txo.m_wsId = XmlUtils.GetManditoryAttributeValue(xn, "ws");
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

		private void ReadCharMapping(XmlNode xn)
		{
			CharMapping cm = new CharMapping(xn);
			m_rgcm.Add(cm);
		}

		private void FillFlidNameMap()
		{
			const int flidMin = RnGenericRecTags.kflidTitle;
			const int flidMax = RnGenericRecTags.kflidDiscussion;
			// We want to skip SubRecords in the list of fields -- everything else is more
			// or less fair game as an import target (unless it no longer exists).
			// Except that Reminders and CrossReferences have never been handled in the UI,
			// we want to skip them as well.
			int[] flidMissing = new int[] {
				RnGenericRecTags.kflidSubRecords,
				RnGenericRecTags.kflidCrossReferences,
				RnGenericRecTags.kflidReminders,
				4004028			// was kflidWeather at one time.
			};

			for (int flid = flidMin; flid <= flidMax; ++flid)
			{
				bool fSkip = false;
				for (int i = 0; i < flidMissing.Length; ++i)
				{
					if (flid == flidMissing[i])
					{
						fSkip = true;
						break;
					}
				}
				if (fSkip)
					continue;
				string stid = "kstid" + m_mdc.GetFieldName(flid);
				string sName = ResourceHelper.GetResourceString(stid);
				m_mapFlidName.Add(flid, sName);
			}
			// Look for custom fields belonging to RnGenericRec.
			foreach (int flid in m_mdc.GetFieldIds())
			{
				if (flid >= (RnGenericRecTags.kClassId * 1000 + 500) &&
					flid <= (RnGenericRecTags.kClassId * 1000 + 999))
				{
					string sName = m_mdc.GetFieldLabel(flid);
					m_mapFlidName.Add(flid, sName);
				}
			}
		}

		Process m_viewProcess = null;
		private void btnViewFile_Click(object sender, EventArgs e)
		{
			if (m_viewProcess == null || m_viewProcess.HasExited)
			{
				if (MiscUtils.IsUnix)
					// Open SFM file from users default text editor (FWNX-834)
					m_viewProcess = Process.Start(
						"xdg-open",
						m_sSfmDataFile);
				else
					m_viewProcess = Process.Start(
						Path.Combine(DirectoryFinder.FWCodeDirectory, "ZEdit.exe"),
						m_sSfmDataFile);
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
			IWritingSystem ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
			{
				ListViewItem lvi = CreateListViewItemForWS(ws);
				m_lvMappingLanguages.Items.Add(lvi);
				m_lvMappingLanguages.Sort();
				lvi.Selected = true;
				m_fDirtySettings = true;
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
			if (m_lvMappingLanguages.SelectedItems.Count == 0)
			{
				m_btnModifyMappingLanguage.Enabled = false;
			}
			else
			{
				m_btnModifyMappingLanguage.Enabled = true;
			}
		}

		private void listViewContentMapping_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_lvContentMapping.SelectedItems.Count == 0)
			{
				m_btnModifyContentMapping.Enabled = false;
			}
			else
			{
				m_btnModifyContentMapping.Enabled = true;
			}
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

		string m_sLogFile = null;

		private void DoImport()
		{
			using (new WaitCursor(this))
			{
				using (var progressDlg = new ProgressDialogWithTask(this, m_cache.ThreadHelper))
				{
					progressDlg.Minimum = 0;
					progressDlg.Maximum = 100;
					progressDlg.AllowCancel = true;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					progressDlg.Restartable = true;
					progressDlg.Title = String.Format(LexTextControls.ksImportingFrom0, m_sSfmDataFile);
					m_sLogFile = (string)progressDlg.RunTask(true, ImportStdFmtFile,
						m_sSfmDataFile);
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
			int lineNumber = 0;
			using (var uowHelper = new NonUndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor))
			{
				try
				{
					m_dtStart = DateTime.Now;
					FixSettingsForThisDatabase();
					int cLines = m_SfmFile.Lines.Count;
					progressDlg.Title = String.Format(LexTextControls.ksImportingFrom0, Path.GetFileName(m_sSfmDataFile));
					progressDlg.StepSize = 1;
					int cExistingRecords = m_cache.LangProject.ResearchNotebookOA.RecordsOC.Count;
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
					IRnGenericRec recPrev = null;
					IRnGenericRec rec = null;
					IRnGenericRecFactory factRec = m_cache.ServiceLocator.GetInstance<IRnGenericRecFactory>();
					IRnGenericRecRepository repoRec = m_cache.ServiceLocator.GetInstance<IRnGenericRecRepository>();
					ICmPossibilityRepository repoPoss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
					ICmPossibility defaultType = repoPoss.GetObject(RnResearchNbkTags.kguidRecObservation);
					for (int i = 0; i < cLines; ++i)
					{
						progressDlg.Step(1);
						if (progressDlg.Canceled)
						{
							LogMessage(LexTextControls.ksImportCanceledByUser, lineNumber);
							break;
						}
						Sfm2Xml.SfmField field = m_SfmFile.Lines[i];
						lineNumber = field.LineNumber;
						if (field.Marker.StartsWith("_"))
							continue;
						RnSfMarker rsf;
						if (!m_mapMkrRsf.TryGetValue(field.Marker, out rsf))
						{
							// complain?  log complaint? throw a fit?
							continue;
						}
						if (rsf.m_nLevel == 1)
						{
							recPrev = rec;
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
							continue;
						CellarPropertyType cpt = (CellarPropertyType) m_mdc.GetFieldType(rsf.m_flid);
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
										int clidBase = clidDst;
										while (clidBase != 0 && clidBase != CmPossibilityTags.kClassId)
											clidBase = m_mdc.GetBaseClsId(clidBase);
										if (clidBase == CmPossibilityTags.kClassId)
											SetListReference(rec, rsf, field);
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
							case CellarPropertyType.MultiBigString:
							case CellarPropertyType.MultiUnicode:
							case CellarPropertyType.MultiBigUnicode:
							case CellarPropertyType.String:
							case CellarPropertyType.BigString:
								SetStringValue(rec, rsf, field, cpt);
								break;
							case CellarPropertyType.GenDate:
								SetGenDateValue(rec, rsf, field);
								break;
							case CellarPropertyType.Time:
								SetDateTimeValue(rec, rsf, field);
								break;
							case CellarPropertyType.Unicode:
							case CellarPropertyType.BigUnicode:
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
					string sMsg = String.Format(LexTextControls.ksProblemImportingFrom,
												m_tbDatabaseFileName.Text, e.Message);
					LogMessage(sMsg, lineNumber);
					System.Windows.Forms.MessageBox.Show(this, sMsg);
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
			string sHtmlFile = Path.Combine(Path.GetTempPath(), "FwNotebookImportLog.htm");
			using (StreamWriter sw = File.CreateText(sHtmlFile))
			{
				sw.WriteLine("<html>");
				sw.WriteLine("<head>");
				string sHeadInfo = String.Format(LexTextControls.ksImportLogForX, m_sSfmDataFile);
				sw.WriteLine(String.Format("  <title>{0}</title>", sHeadInfo));
				WriteHtmlJavaScript(sw);	// add the script
				sw.WriteLine("</head>");
				sw.WriteLine("<body>");
				sw.WriteLine(String.Format("<h2>{0}</h2>", sHeadInfo));
				long deltaTicks = m_dtEnd.Ticks - m_dtStart.Ticks;	// number of 100-nanosecond intervals
				int deltaMsec = (int)((deltaTicks + 5000L) / 10000L);	// round off to milliseconds
				int deltaSec = deltaMsec / 1000;
				string sDeltaTime = String.Format(LexTextControls.ksImportingTookTime,
					System.IO.Path.GetFileName(m_sSfmDataFile), deltaSec, deltaMsec % 1000);
				sw.WriteLine("<p>{0}</p>", sDeltaTime);
				sw.Write("<h3>");
				if (m_cRecordsDeleted == 0)
					sw.Write(LexTextControls.ksRecordsCreatedByImport, m_cRecordsRead);
				else
					sw.Write(LexTextControls.ksRecordsDeletedAndCreated, m_cRecordsDeleted, m_cRecordsRead);
				sw.WriteLine("</h3>");
				WriteMessageLines(sw);
				ListNewPossibilities(sw, LexTextControls.ksNewAnthropologyListItems, m_rgNewAnthroItem, "anthroEdit");
				ListNewPossibilities(sw, LexTextControls.ksNewConfidenceListItems, m_rgNewConfidence, "confidenceEdit");
				ListNewPossibilities(sw, LexTextControls.ksNewLocationListItems, m_rgNewLocation, "locationsEdit");
				ListNewPossibilities(sw, LexTextControls.ksNewPeopleListItems, m_rgNewPeople, "peopleEdit");
				ListNewPossibilities(sw, LexTextControls.ksNewPhraseTagListItems, m_rgNewPhraseTag, "");
				ListNewPossibilities(sw, LexTextControls.ksNewRecordTypeListItems, m_rgNewRecType, "recTypeEdit");
				ListNewPossibilities(sw, LexTextControls.ksNewRestrictionListItems, m_rgNewRestriction, "restrictionsEdit");
				ListNewPossibilities(sw, LexTextControls.ksNewStatusListItems, m_rgNewStatus, "statusEdit");
				ListNewPossibilities(sw, LexTextControls.ksNewTimeOfDayListItems, m_rgNewTimeOfDay, "");
				// now for custom lists...
				foreach (Guid key in m_mapNewPossibilities.Keys)
				{
					ICmPossibilityList list = m_repoList.GetObject(key);
					string name = list.Name.BestAnalysisVernacularAlternative.Text;
					string message = String.Format(LexTextControls.ksNewCustomListItems, name);
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
			string sError = LexTextControls.ksErrorCaughtTryingOpenFile;
			string sCannot = String.Format(LexTextControls.ksNoFileHyperlinkThisBrowser,
				m_sSfmDataFile.Replace("\\", "\\\\"));
			sCannot = sCannot.Replace(Environment.NewLine, "\\n");
			sw.WriteLine("<script type=\"text/javascript\">");
			sw.WriteLine("var isIE = typeof window != 'undefined' && typeof window.ActiveXObject != 'undefined';");
			//var isNetscape = typeof window != 'undefined' && typeof window.netscape != 'undefined' && typeof window.netscape.security != 'undefined' && typeof window.opera != 'object';
			sw.WriteLine("function zedit (filename, line)");
			sw.WriteLine("{");
			string sProg = Path.Combine(DirectoryFinder.FWCodeDirectory, "zedit.exe");
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
			//This pops up a dialog on every click that allows the user to open a permanent gaping security hole.
			//        else if (isNetscape)
			//        {
			//            try
			//            {
			//                netscape.security.PrivilegeManager.enablePrivilege("UniversalXPConnect");
			//                var file = Components.classes["@mozilla.org/file/local;1"].createInstance(Components.interfaces.nsILocalFile);
			//                file.initWithPath(prog);
			//                var process = Components.classes["@mozilla.org/process/util;1"].createInstance(Components.interfaces.nsIProcess);
			//                process.init(file);
			//                var args = [filename, "-g", line];
			//                process.run(false, args, args.length);
			//                zeditfailed = false;
			//            }
			//            catch (err) { alert(\"{0}\" + err); } ", sError);
			//        }
			sw.WriteLine("    }");
			sw.WriteLine("    if (zeditfailed)");
			sw.WriteLine("        alert(\"{0}\")", sCannot);
			sw.WriteLine("}");
			sw.WriteLine("</script>");
		}

		private void WriteMessageLines(StreamWriter sw)
		{
			if (m_rgMessages.Count == 0)
				return;
			sw.WriteLine(String.Format("<h2>{0}</h2>", LexTextControls.ksMessagesFromAnthropologyImport));
			m_rgMessages.Sort();
			string currentMessage = null;
			string sEscapedDataFile = m_sSfmDataFile.Replace("\\", "\\\\");
			for (int i = 0; i < m_rgMessages.Count; ++i)
			{
				if (m_rgMessages[i].Message != currentMessage)
				{
					currentMessage = m_rgMessages[i].Message;
					// Need to quote any occurrences of <, >, or & in the message text.
					string sMsg = currentMessage.Replace("&", "&amp;");
					sMsg = sMsg.Replace("<", "&lt;");
					sMsg = sMsg.Replace(">", "&gt;");
					sw.WriteLine(String.Format("<h3>{0}</h3>", sMsg));
				}
				sw.Write("<ul><li>");
				if (m_rgMessages[i].LineNumber <= 0)
				{
					sw.Write(LexTextControls.ksNoLineNumberInFile, m_sSfmDataFile);
				}
				else
				{
					string sLineLink = String.Format(
						"<a HREF=\"javascript: void 0\" ONCLICK=\"zedit('{0}', '{1}'); return false\">{1}</a>",
						sEscapedDataFile, m_rgMessages[i].LineNumber);
					sw.Write(LexTextControls.ksOnOrBeforeLine, m_sSfmDataFile, sLineLink);
				}
				sw.WriteLine("</li></ul>");
			}
		}

		private static void ListNewPossibilities(StreamWriter writer, string sMsg,
			List<ICmPossibility> list, string tool)
		{
			if (list.Count > 0)
			{
				tool = null;		// FIXME when FwLink starts working again...
				writer.WriteLine("<h3>{0}</h3>", String.Format(sMsg, list.Count));
				writer.WriteLine("<ul>");
				foreach (ICmPossibility poss in list)
				{
					if (String.IsNullOrEmpty(tool))
					{
						writer.WriteLine("<li>{0}</li>", poss.AbbrAndName);
					}
					else
					{
						FwLinkArgs link = new FwLinkArgs(tool, poss.Guid);
						string href = link.ToString();
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
			foreach (EncConverterChoice ecc in m_mapWsEncConv.Values)
			{
				if (!String.IsNullOrEmpty(ecc.ConverterName) && ecc.ConverterName != Sfm2Xml.STATICS.AlreadyInUnicode)
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
			foreach (RnSfMarker rsf in m_mapMkrRsf.Values)
			{
				switch (FieldType(rsf.m_flid))
				{
					case SfFieldType.Link:
					case SfFieldType.DateTime:
						break;
					case SfFieldType.ListRef:
						SetDefaultForListRef(rsf);
						char[] rgchSplit = new char[1] { ' ' };
						if (!String.IsNullOrEmpty(rsf.m_tlo.m_sDelimMulti))
							rsf.m_tlo.m_rgsDelimMulti = rsf.m_tlo.m_sDelimMulti.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						if (!String.IsNullOrEmpty(rsf.m_tlo.m_sDelimSub))
							rsf.m_tlo.m_rgsDelimSub = rsf.m_tlo.m_sDelimSub.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						if (!String.IsNullOrEmpty(rsf.m_tlo.m_sMarkStart))
							rsf.m_tlo.m_rgsMarkStart = rsf.m_tlo.m_sMarkStart.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						if (!String.IsNullOrEmpty(rsf.m_tlo.m_sMarkEnd))
							rsf.m_tlo.m_rgsMarkEnd = rsf.m_tlo.m_sMarkEnd.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						if (!String.IsNullOrEmpty(rsf.m_tlo.m_sBefore))
							rsf.m_tlo.m_rgsBefore = rsf.m_tlo.m_sBefore.Split(rgchSplit, StringSplitOptions.RemoveEmptyEntries);
						if (String.IsNullOrEmpty(rsf.m_tlo.m_wsId))
							rsf.m_tlo.m_ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
						else
							m_cache.ServiceLocator.WritingSystemManager.GetOrSet(rsf.m_tlo.m_wsId, out rsf.m_tlo.m_ws);
						break;
					case SfFieldType.String:
						if (String.IsNullOrEmpty(rsf.m_sto.m_wsId))
							rsf.m_sto.m_ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
						else
							m_cache.ServiceLocator.WritingSystemManager.GetOrSet(rsf.m_sto.m_wsId, out rsf.m_sto.m_ws);
						break;
					case SfFieldType.Text:
						if (String.IsNullOrEmpty(rsf.m_txo.m_wsId))
							rsf.m_txo.m_ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
						else
							m_cache.ServiceLocator.WritingSystemManager.GetOrSet(rsf.m_txo.m_wsId, out rsf.m_txo.m_ws);
						break;
				}
			}
			foreach (CharMapping cm in m_rgcm)
			{
				if (!String.IsNullOrEmpty(cm.DestinationWritingSystemId))
				{
					IWritingSystem ws;
					m_cache.ServiceLocator.WritingSystemManager.GetOrSet(cm.DestinationWritingSystemId, out ws);
					cm.DestinationWritingSystem = ws;
				}
			}
			DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
			m_rgsDayAbbr = dtfi.AbbreviatedDayNames;
			m_rgsDayName = dtfi.DayNames;
			m_rgsMonthAbbr = dtfi.AbbreviatedMonthNames;
			m_rgsMonthName = dtfi.MonthNames;
		}

		private void SetDefaultForListRef(RnSfMarker rsf)
		{
			string sDefault = rsf.m_tlo.m_sEmptyDefault;
			if (sDefault == null)
				return;
			sDefault = sDefault.Trim();
			if (sDefault.Length == 0)
				return;
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
				return;
			switch (rsf.m_flid)
			{
				case RnGenericRecTags.kflidAnthroCodes:
					if (m_mapAnthroCode.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_mapAnthroCode);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapAnthroCode);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewAnthroItem(rgsHier);
					break;
				case RnGenericRecTags.kflidConfidence:
					if (m_mapConfidence.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS, m_mapConfidence);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapConfidence);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewConfidenceItem(rgsHier);
					break;
				case RnGenericRecTags.kflidLocations:
					if (m_mapLocation.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.LocationsOA.PossibilitiesOS, m_mapLocation);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapLocation);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewLocation(rgsHier);
					break;
				case RnGenericRecTags.kflidPhraseTags:
					if (m_mapPhraseTag.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS, m_mapPhraseTag);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapPhraseTag);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewPhraseTag(rgsHier);
					break;
				case RnGenericRecTags.kflidResearchers:
				case RnGenericRecTags.kflidSources:
					if (m_mapPeople.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapPeople);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewPerson(rgsHier);
					break;
				case RnGenericRecTags.kflidRestrictions:
					if (m_mapRestriction.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.RestrictionsOA.PossibilitiesOS, m_mapRestriction);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapRestriction);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewRestriction(rgsHier);
					break;
				case RnGenericRecTags.kflidStatus:
					if (m_mapStatus.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.StatusOA.PossibilitiesOS, m_mapStatus);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapStatus);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewStatus(rgsHier);
					break;
				case RnGenericRecTags.kflidTimeOfEvent:
					if (m_mapTimeOfDay.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.TimeOfDayOA.PossibilitiesOS, m_mapTimeOfDay);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapTimeOfDay);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewTimeOfDay(rgsHier);
					break;
				case RnGenericRecTags.kflidType:
					if (m_mapRecType.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS, m_mapRecType);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapRecType);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewRecType(rgsHier);
					break;
				case RnGenericRecTags.kflidParticipants:
					if (m_mapPeople.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
					rsf.m_tlo.m_default = FindPossibilityOrNull(rgsHier, m_mapPeople);
					if (rsf.m_tlo.m_default == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						rsf.m_tlo.m_default = CreateNewPerson(rgsHier);
					break;
				default:
					// must be a custom field.
					Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
					// We don't yet have the necessary information in the new FDO/MetaDataCache.
					break;
			}
		}

		private List<string> PruneEmptyStrings(List<string> rgsData)
		{
			List<string> rgsOut = new List<string>();
			for (int i = 0; i < rgsData.Count; ++i)
			{
				string sT = rgsData[i];
				if (sT == null)
					continue;
				string sOut = sT.Trim();
				if (sOut.Length > 0)
					rgsOut.Add(sOut);
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
			List<string> rgsParas = SplitIntoParagraphs(rsf, field);
			if (rgsParas.Count == 0)
				return;
			if (m_factStText == null)
				m_factStText = m_cache.ServiceLocator.GetInstance<IStTextFactory>();
			switch (rsf.m_flid)
			{
				case RnGenericRecTags.kflidConclusions:
					if (rec.ConclusionsOA == null)
						rec.ConclusionsOA = m_factStText.Create();
					StoreTextData(rec.ConclusionsOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidDescription:
					if (rec.DescriptionOA == null)
						rec.DescriptionOA = m_factStText.Create();
					StoreTextData(rec.DescriptionOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidDiscussion:
					if (rec.DiscussionOA == null)
						rec.DiscussionOA = m_factStText.Create();
					StoreTextData(rec.DiscussionOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidExternalMaterials:
					if (rec.ExternalMaterialsOA == null)
						rec.ExternalMaterialsOA = m_factStText.Create();
					StoreTextData(rec.ExternalMaterialsOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidFurtherQuestions:
					if (rec.FurtherQuestionsOA == null)
						rec.FurtherQuestionsOA = m_factStText.Create();
					StoreTextData(rec.FurtherQuestionsOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidHypothesis:
					if (rec.HypothesisOA == null)
						rec.HypothesisOA = m_factStText.Create();
					StoreTextData(rec.HypothesisOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidPersonalNotes:
					if (rec.PersonalNotesOA == null)
						rec.PersonalNotesOA = m_factStText.Create();
					StoreTextData(rec.PersonalNotesOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidResearchPlan:
					if (rec.ResearchPlanOA == null)
						rec.ResearchPlanOA = m_factStText.Create();
					StoreTextData(rec.ResearchPlanOA, rsf, rgsParas);
					break;
				case RnGenericRecTags.kflidVersionHistory:
					if (rec.VersionHistoryOA == null)
						rec.VersionHistoryOA = m_factStText.Create();
					StoreTextData(rec.VersionHistoryOA, rsf, rgsParas);
					break;
				default:
					// Handle custom field (don't think any can exist yet, but...)
					Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
					IStText text;
					int hvo = m_cache.DomainDataByFlid.get_ObjectProp(rec.Hvo, rsf.m_flid);
					if (hvo == 0)
					{
						text = m_factStText.Create();
						m_cache.DomainDataByFlid.SetObjProp(rec.Hvo, rsf.m_flid, text.Hvo);
					}
					else
					{
						if (m_repoStText == null)
							m_repoStText = m_cache.ServiceLocator.GetInstance<IStTextRepository>();
						text = m_repoStText.GetObject(hvo);
					}
					StoreTextData(text, rsf, rgsParas);
					break;
			}
		}

		private void StoreTextData(IStText text, RnSfMarker rsf, List<string> rgsParas)
		{
			if (m_factPara == null)
				m_factPara = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			if (rgsParas.Count == 0)
			{
				IStTxtPara para = m_factPara.Create();
				text.ParagraphsOS.Add(para);
				if (!String.IsNullOrEmpty(rsf.m_txo.m_sStyle))
					para.StyleName = rsf.m_txo.m_sStyle;
				para.Contents = MakeTsString(String.Empty, rsf.m_txo.m_ws.Handle);
			}
			else
			{
				for (int i = 0; i < rgsParas.Count; ++i)
				{
					IStTxtPara para = m_factPara.Create();
					text.ParagraphsOS.Add(para);
					if (!String.IsNullOrEmpty(rsf.m_txo.m_sStyle))
						para.StyleName = rsf.m_txo.m_sStyle;
					para.Contents = MakeTsString(rgsParas[i], rsf.m_txo.m_ws.Handle);
				}
			}
		}

		private List<string> SplitIntoParagraphs(RnSfMarker rsf, Sfm2Xml.SfmField field)
		{
			List<string> rgsParas = new List<string>();
			List<string> rgsLines = SplitIntoLines(field.Data);
			StringBuilder sbPara = new StringBuilder();
			for (int i = 0; i < rgsLines.Count; ++i)
			{
				bool fIndented = false;
				string sLine = rgsLines[i];
				if (sLine.Length > 0)
					fIndented = Char.IsWhiteSpace(sLine[0]);
				sLine = sLine.TrimStart();
				if (rsf.m_txo.m_fStartParaNewLine)
				{
					if (sLine.Length > 0)
						rgsParas.Add(sLine);
					continue;
				}
				if (sLine.Length == 0)
				{
					if (sbPara.Length > 0 &&
						(rsf.m_txo.m_fStartParaBlankLine || rsf.m_txo.m_fStartParaShortLine))
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
					sbPara.Append(sLine);
					continue;
				}
				if (rsf.m_txo.m_fStartParaShortLine && sLine.Length < rsf.m_txo.m_cchShortLim)
				{
					if (sbPara.Length > 0)
						sbPara.Append(" ");
					sbPara.Append(sLine);
					rgsParas.Add(sbPara.ToString());
					sbPara.Remove(0, sbPara.Length);
					continue;
				}
				if (sbPara.Length > 0)
					sbPara.Append(" ");
				sbPara.Append(sLine);
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
			List<string> rgsLines = SplitString(sData, Environment.NewLine);
			for (int i = 0; i < rgsLines.Count; ++i)
				rgsLines[i] = TrimLineData(rgsLines[i]);
			return rgsLines;
		}

		private string TrimLineData(string sData)
		{
			string sLine = sData;
			int idx;
			// The following 4 lines of code shouldn't be needed, but ...
			// Erase any leading newline type characters, then convert any others to spaces.
			while ((idx = sLine.IndexOfAny(new char[] { '\n', '\r' })) == 0)
				sLine = sLine.Substring(1);
			sLine = sLine.Replace('\n', ' ');
			sLine = sLine.Replace('\r', ' ');
			// Leave leading whitespace -- it may indicate the start of a new paragraph.
			return sLine.TrimEnd();
		}

		/// <summary>
		/// Store a value for either a simple formatted string or a multilingual string.
		/// </summary>
		private void SetStringValue(IRnGenericRec rec, RnSfMarker rsf, Sfm2Xml.SfmField field,
			CellarPropertyType cpt)
		{
			// REVIEW: SHOULD WE WORRY ABOUT EMBEDDED CHAR MAPPINGS THAT CHANGE THE WRITING SYSTEM
			// WHEN IT COMES TO ENCODING CONVERSION???
			ReconvertEncodedDataIfNeeded(field, rsf.m_sto.m_wsId);
			ITsString tss = MakeTsString(field.Data, rsf.m_sto.m_ws.Handle);
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
						case CellarPropertyType.MultiBigString:
						case CellarPropertyType.MultiUnicode:
						case CellarPropertyType.MultiBigUnicode:
							m_cache.DomainDataByFlid.SetMultiStringAlt(rec.Hvo, rsf.m_flid, rsf.m_sto.m_ws.Handle, tss);
							break;
						case CellarPropertyType.String:
						case CellarPropertyType.BigString:
							m_cache.DomainDataByFlid.SetString(rec.Hvo, rsf.m_flid, tss);
							break;
					}
					break;
			}
		}

		private void ReconvertEncodedDataIfNeeded(Sfm2Xml.SfmField field, string sWs)
		{
			if (String.IsNullOrEmpty(sWs))
				sWs = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultAnalWs);
			if (!String.IsNullOrEmpty(sWs))
			{
				EncConverterChoice ecc;
				if (m_mapWsEncConv.TryGetValue(sWs, out ecc))
				{
					if (ecc.Converter != null)
						field.Data = ecc.Converter.ConvertToUnicode(field.RawData);
				}
			}
			if (field.ErrorConvertingData)
			{
				LogMessage(
					String.Format(LexTextControls.ksEncodingConversionProblem, field.Marker),
					field.LineNumber);
			}
		}

		private ITsString MakeTsString(string sRaw, int ws)
		{
			List<string> rgsText = new List<string>();
			List<CharMapping> rgcmText = new List<CharMapping>();
			while (!String.IsNullOrEmpty(sRaw))
			{
				CharMapping cmText;
				int idx = IndexOfFirstCharMappingMarker(sRaw, out cmText);
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
							idx = sRaw.Length;
					}
					else
					{
						idx = sRaw.Length;
					}
				}
				rgsText.Add(sRaw.Substring(0, idx));
				if (cmText.IgnoreMarkerOnImport)
					rgcmText.Add(null);
				else
					rgcmText.Add(cmText);
				sRaw = sRaw.Substring(idx);
			}
			if (rgsText.Count == 0)
			{
				rgsText.Add(String.Empty);
				rgcmText.Add(null);
			}
			ITsIncStrBldr tisb = m_cache.TsStrFactory.GetIncBldr();
			for (int i = 0; i < rgsText.Count; ++i)
			{
				string sRun = rgsText[i];
				CharMapping cmRun = rgcmText[i];
				if (cmRun != null && cmRun.DestinationWritingSystem != null)
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
						cmRun.DestinationWritingSystem.Handle);
				else
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
				if (cmRun != null && !String.IsNullOrEmpty(cmRun.DestinationStyle))
					tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, cmRun.DestinationStyle);
				tisb.Append(sRun);
			}
			return tisb.GetString();
		}

		/// <summary>
		/// Find the next character mapping marker in the string (if any).
		/// </summary>
		private int IndexOfFirstCharMappingMarker(string sText, out CharMapping cmText)
		{
			int idx = -1;
			cmText = null;
			foreach (CharMapping cm in m_rgcm)
			{
				if (cm.BeginMarker.Length == 0)
					continue;
				int idxT = sText.IndexOf(cm.BeginMarker);
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
			string sData = field.Data.Trim();
			if (sData.Length == 0)
				return;		// nothing we can do without data!
			GenDate gdt;
			if (!TryParseGenDate(sData, rsf.m_dto.m_rgsFmt, out gdt))
			{
				LogMessage(String.Format(LexTextControls.ksCannotParseGenericDate, sData, field.Marker),
					field.LineNumber);
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

		struct GenDateInfo
		{
			public int mday;
			public int wday;
			public int ymon;
			public int year;
			public GenDate.PrecisionType prec;
			public bool error;
		};

		private bool TryParseGenDate(string sData, List<string> rgsFmt, out GenDate gdt)
		{
			GenDate.PrecisionType prec = GenDate.PrecisionType.Exact;
			if (sData[0] == '~')
			{
				prec = GenDate.PrecisionType.Approximate;
				sData = sData.Substring(1).Trim();
			}
			else if (sData[0] == '<')
			{
				prec = GenDate.PrecisionType.Before;
				sData = sData.Substring(1).Trim();
			}
			else if (sData[0] == '>')
			{
				prec = GenDate.PrecisionType.After;
				sData = sData.Substring(1).Trim();
			}
			if (sData.Length == 0)
			{
				gdt = new GenDate();
				return false;
			}
			int year = 0;
			bool fAD = true;
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
				string sResidue = ParseFormattedDate(sData, sFmt, out gdi);
				if (!gdi.error)
				{
					year = gdi.year;
					if (prec == GenDate.PrecisionType.Exact)
					{
						if (sResidue.Trim().StartsWith("?"))
							prec = GenDate.PrecisionType.Approximate;
						else
							prec = gdi.prec;
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
			gdi = new GenDateInfo();
			gdi.error = true;		// just in case...
			gdi.prec = GenDate.PrecisionType.Exact;
			char ch;
			int i;
			int cch;
			bool fError;
			bool fDayPresent = false;
			bool fMonthPresent = false;
			bool fYearPresent = false;
			string sDate = sDateString.Trim();
			for (; sFmt.Length > 0; sFmt = sFmt.Substring(cch))
			{
				ch = sFmt[0];
				for (i = 1; i < sFmt.Length; ++i)
				{
					if (sFmt[i] != ch)
						break;
				}
				cch = i;
				switch (ch)
				{
					case 'd':
						if (CheckForQuestionMarks(ref gdi, ref sDate))
						{
							if (sDate.Length == 0)
								return String.Empty;
							else
								break;
						}
						switch (cch)
						{
							case 1:	// d
							case 2:	// dd
								fDayPresent = true;
								fError = !TryParseLeadingNumber(ref sDate, out gdi.mday);
								if (fError || gdi.mday > 31)
									return sDate;
								break;
							case 3:	// ddd - Abbreviated day of week
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsDayAbbr, out gdi.wday);
								if (fError)
									return sDate;
								break;
							case 4:	// dddd - Unabbreviated day of the week
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsDayName, out gdi.wday);
								if (fError)
									return sDate;
								break;
							default:
								return sDate;
						}
						break;

					case 'M':
						if (CheckForQuestionMarks(ref gdi, ref sDate))
						{
							if (sDate.Length == 0)
								return String.Empty;
							else
								break;
						}
						fMonthPresent = true;
						switch (cch)
						{
							case 1:	// M
							case 2:	// MM
								fError = !TryParseLeadingNumber(ref sDate, out gdi.ymon);
								if (fError || gdi.ymon > 12)
									return sDate;
								break;
							case 3: // MMM - Abbreviated month name
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsMonthAbbr, out gdi.ymon);
								if (fError)
									return sDate;
								break;
							case 4:	// MMMM - Unabbreviated month name
								fError = !TryMatchAgainstNameList(ref sDate, m_rgsMonthName, out gdi.ymon);
								if (fError)
									return sDate;
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
						int thisyear = DateTime.Now.Year;
						switch (cch)
						{
							case 1:
								fError = !TryParseLeadingNumber(ref sDate, out year);
								if (fError || year > 9)
									return sDate;
								gdi.year = 2000 + year;
								if (gdi.year > thisyear)
									gdi.year -= 100;
								break;
							case 2:
								fError = !TryParseLeadingNumber(ref sDate, out year);
								if (fError || year > 99)
									return sDate;
								gdi.year = 2000 + year;
								if (gdi.year > thisyear)
									gdi.year -= 100;
								break;
							case 4:
								fError = !TryParseLeadingNumber(ref sDate, out year);
								if (fError)
									return sDate;
								break;
							default:
								return sDate;
						}
						break;

					case 'g':
						// TODO SteveMc: IMPLEMENT ME!
						return sDate;

					case '\'': // quoted text
						//cch = ParseFmtQuotedText (sFmt, sDate);
						//if (cch < 0)
						//    return 0; // text not found
						break;

					case ' ':
						sDate = sDate.Trim();
						break;

					default:
						// Check for matching separators.
						sDate = sDate.Trim();
						for (int j = 0; j < cch; ++j)
						{
							if (j >= sDate.Length || sDate[j] != ch)
								return sDate;
						}
						sDate = sDate.Substring(cch);
						sDate = sDate.Trim();
						break;
				}
			}
			gdi.error = !ValidateDate(fYearPresent ? gdi.year : 2000, fMonthPresent ? gdi.ymon : 1,
				fDayPresent ? gdi.mday : 1);
			return sDate;
		}

		private static bool CheckForQuestionMarks(ref GenDateInfo gdi, ref string sDate)
		{
			if (sDate.StartsWith("?"))
			{
				while (sDate.StartsWith("?"))
					sDate = sDate.Substring(1);
				gdi.prec = GenDate.PrecisionType.Approximate;
				if (sDate.Length == 0)
					gdi.error = gdi.year == 0;	// ok if we already have a year.
				return true;
			}
			else
			{
				return false;
			}
		}

		private static bool TryMatchAgainstNameList(ref string sDate, string[] rgsToMatch, out int val)
		{
			for (int j = 0; j < rgsToMatch.Length; ++j)
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
				return false;
			if (month < 1 || month > 12)
				return false;
			int days_in_month = 31;	// most common value
			if (year == 1752 && month == 9)
			{
				days_in_month = 19; // the month the calendar was changed
			}
			else
			{
				switch (month)
				{
					case 2:		// February
						days_in_month = (((year%4) == 0 && (year%100) != 0) || (year%1000) == 0) ? 29 : 28;
						break;
					case 4:		// April
					case 6:		// June
					case 9:		// September
					case 11:	// November
						days_in_month = 30;
						break;
				}
			}
			if (day < 1 || day > days_in_month)
				return false;
			return true;
		}

		private static bool TryParseLeadingNumber(ref string sDate, out int val)
		{
			val = 0;
			int cchUsed;
			for (cchUsed = 0; cchUsed < sDate.Length; ++cchUsed)
			{
				if (!Char.IsDigit(sDate[cchUsed]))
					break;
			}
			if (cchUsed < 1)
				return false;
			string sNum = sDate.Substring(0, cchUsed);
			sDate = sDate.Substring(cchUsed);
			return Int32.TryParse(sNum, out val);
		}

		/// <summary>
		/// Store a value with a "kcptTime" type value.  These are less forgiving than those with
		/// "kcptGenDate" values, because they are generally created by a computer program instead
		/// of typed by a user.
		/// </summary>
		private void SetDateTimeValue(IRnGenericRec rec, RnSfMarker rsf, Sfm2Xml.SfmField field)
		{
			string sData = field.Data.Trim();
			if (sData.Length == 0)
				return;		// nothing we can do without data!
			DateTime dt;
			if (!DateTime.TryParseExact(sData, rsf.m_dto.m_rgsFmt.ToArray(), null, DateTimeStyles.None, out dt))
			{
				LogMessage(String.Format(LexTextControls.ksCannotParseDateTime, field.Data, field.Marker),
					field.LineNumber);
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
			if (String.IsNullOrEmpty(field.Data))
				return;
			var pend = new PendingLink { Marker = rsf, Field = field, Record = rec };
			m_pendingLinks.Add(pend);
		}

		private void ProcessStoredLinkData()
		{
			if (m_pendingLinks.Count == 0)
				return;
			// 1. Get the titles and map them onto their records.
			// 2. Try to match link data against titles
			// 3. If successful, set link.
			// 4. If unsuccessful, provide error message for log.
			var mapTitleRec = new Dictionary<string, IRnGenericRec>();
			foreach (var rec in m_cache.ServiceLocator.GetInstance<IRnGenericRecRepository>().AllInstances())
			{
				var sTitle = rec.Title.Text;
				if (String.IsNullOrEmpty(sTitle))
					continue;
				if (!mapTitleRec.ContainsKey(sTitle))
					mapTitleRec.Add(sTitle, rec);
			}
			foreach (var pend in m_pendingLinks)
			{
				IRnGenericRec rec;
				var sData = pend.Field.Data;
				if (mapTitleRec.TryGetValue(sData, out rec))
				{
					if (SetLink(pend, rec))
						continue;
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
								continue;
						}
					}
				}
				// log an error.
				LogMessage(
					String.Format(SIL.FieldWorks.LexText.Controls.LexTextControls.ksCannotMakeDesiredLink,
						pend.Field.Marker, pend.Field.Data),
					pend.Field.LineNumber);

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
			string sData = field.Data;
			if (sData == null)
				sData = String.Empty;
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
				rgsData = new List<string>();
			rgsData = ApplyBeforeAndBetween(rsf, rgsData);
			if (rgsData.Count == 0)
				rgsData.Add(String.Empty);
			foreach (var sItem in rgsData)
			{
				switch (rsf.m_flid)
				{
					case RnGenericRecTags.kflidAnthroCodes:
						if (!StoreAnthroCode(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidConfidence:
						if (!StoreConfidence(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidLocations:
						if (!StoreLocation(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidPhraseTags:
						if (!StorePhraseTag(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidResearchers:
						if (!StoreResearcher(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidRestrictions:
						if (!StoreRestriction(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidSources:
						if (!StoreSource(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidStatus:
						if (!StoreStatus(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidTimeOfEvent:
						if (!StoreTimeOfEvent(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidType:
						if (!StoreRecType(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					case RnGenericRecTags.kflidParticipants:
						if (!StoreParticipant(rec, rsf, sItem))
							LogCannotFindListItem(sItem, field);
						break;
					default:
						// must be a custom field.
						Debug.Assert(rsf.m_flid >= (RnGenericRecTags.kClassId * 1000) + 500);
						Guid guidList = m_mdc.GetFieldListRoot(rsf.m_flid);
						if (guidList != Guid.Empty)
						{
							if (m_repoList == null)
								m_repoList = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
							ICmPossibilityList list = m_repoList.GetObject(guidList);
							if (list != null)
							{
								if (!StoreCustomListRefItem(rec, rsf, sItem, list))
									LogCannotFindListItem(sItem, field);
								break;
							}
						}
						LogMessage(
							String.Format(LexTextControls.ksCannotFindPossibilityList, field.Marker),
							field.LineNumber);
						return;
				}
			}
		}

		private void LogCannotFindListItem(string sItem, Sfm2Xml.SfmField field)
		{
			LogMessage(
				String.Format(LexTextControls.ksCannotFindMatchingListItem, sItem, field.Marker),
				field.LineNumber);
		}

		/// <summary>
		/// For each individual item,
		/// 1. remove any comment (using rsf.m_tlo.m_rgsBefore)
		/// 2. extract actual data (using rsf.m_tlo.m_rgsMarkStart/End)
		/// </summary>
		private static List<string> ApplyBeforeAndBetween(RnSfMarker rsf, List<string> rgsData)
		{
			List<string> rgsData2 = new List<string>();
			foreach (string sItem in rgsData)
			{
				string sT = sItem;
				if (rsf.m_tlo.m_fHaveBefore &&
					rsf.m_tlo.m_rgsBefore != null && sItem.Length > 0)
				{
					foreach (string sBefore in rsf.m_tlo.m_rgsBefore)
					{
						int idx = sItem.IndexOf(sBefore);
						if (idx > 0)
						{
							sT = sItem.Substring(0, idx).Trim();
						}
						else if (idx == 0)
						{
							sT = String.Empty;
						}
						if (sT.Length == 0)
							break;
					}
				}
				if (sT.Length > 0 && rsf.m_tlo.m_fHaveBetween &&
					rsf.m_tlo.m_rgsMarkStart != null && rsf.m_tlo.m_rgsMarkEnd != null)
				{
					// Ensure safe length even if the two lengths differ.
					// REVIEW: Should we complain if the lengths differ?
					int clen = rsf.m_tlo.m_rgsMarkEnd.Length;
					if (rsf.m_tlo.m_rgsMarkStart.Length < rsf.m_tlo.m_rgsMarkEnd.Length)
						clen = rsf.m_tlo.m_rgsMarkStart.Length;
					if (clen > 0)
					{
						string sT2 = String.Empty;
						for (int i = 0; i < clen; ++i)
						{
							int idx = sT.IndexOf(rsf.m_tlo.m_rgsMarkStart[i]);
							if (idx >= 0)
							{
								++idx;
								int idxEnd = sT.IndexOf(rsf.m_tlo.m_rgsMarkEnd[i], idx);
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
				if (!String.IsNullOrEmpty(sT))
					rgsData2.Add(sT);
			}
			return rgsData2;
		}

		private string ApplyChanges(RnSfMarker rsf, string sData)
		{
			if (rsf.m_tlo.m_rgsMatch == null || rsf.m_tlo.m_rgsReplace == null)
				return sData;
			int count = rsf.m_tlo.m_rgsMatch.Count;
			if (rsf.m_tlo.m_rgsReplace.Count < rsf.m_tlo.m_rgsMatch.Count)
				count = rsf.m_tlo.m_rgsReplace.Count;
			for (int i = 0; i < count; ++i)
				sData = sData.Replace(rsf.m_tlo.m_rgsMatch[i], rsf.m_tlo.m_rgsReplace[i]);
			return sData;
		}

		private List<string> SplitString(string sItem, string sDel)
		{
			List<string> rgsSplit = new List<string>();
			if (String.IsNullOrEmpty(sItem))
			{
				rgsSplit.Add(String.Empty);
				return rgsSplit;
			}
			int idx;
			while ((idx = sItem.IndexOf(sDel)) >= 0)
			{
				rgsSplit.Add(sItem.Substring(0, idx));
				sItem = sItem.Substring(idx + sDel.Length);
			}
			if (sItem.Length > 0)
				rgsSplit.Add(sItem);
			return rgsSplit;
		}

		private List<string> SplitString(string sData, string[] rgsDelims)
		{
			List<string> rgsData = new List<string>();
			rgsData.Add(sData);
			if (rgsDelims != null && rgsDelims.Length > 0)
			{
				foreach (string sDel in rgsDelims)
				{
					List<string> rgsSplit = new List<string>();
					foreach (string sItem in rgsData)
					{
						string s1 = sItem.Trim();
						if (s1.Length == 0)
							continue;
						List<string> rgsT = SplitString(s1, sDel);
						foreach (string s2 in rgsT)
						{
							string s3 = s2.Trim();
							if (s3.Length > 0)
								rgsSplit.Add(s3);
						}
					}
					rgsData = rgsSplit;
				}
			}
			return rgsData;
		}

		private ICmPossibility FindPossibilityOrNull(List<string> rgsHier,
			Dictionary<string, ICmPossibility> map)
		{
			ICmPossibility possParent = null;
			ICmPossibility poss = null;
			for (int i = 0; i < rgsHier.Count; ++i)
			{
				if (!map.TryGetValue(rgsHier[i].ToLowerInvariant(), out poss))
					return null;
				if (i > 0 && poss.Owner != possParent)
					return null;
				possParent = poss;
			}
			return poss;
		}

		private bool StoreAnthroCode(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				ICmAnthroItem def = rsf.m_tlo.m_default as ICmAnthroItem;
				if (def != null && !rec.AnthroCodesRC.Contains(def))
					rec.AnthroCodesRC.Add(def);
				return true;
			}
			if (m_mapAnthroCode.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_mapAnthroCode);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapAnthroCode);
			if (poss != null)
			{
				if (!rec.AnthroCodesRC.Contains(poss as ICmAnthroItem))
					rec.AnthroCodesRC.Add(poss as ICmAnthroItem);
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmAnthroItem item = CreateNewAnthroItem(rgsHier);
				if (item != null)
					rec.AnthroCodesRC.Add(item);
				return true;
			}
		}

		private ICmAnthroItem CreateNewAnthroItem(List<string> rgsHier)
		{
			return (ICmAnthroItem)CreateNewPossibility(rgsHier, AnthroItemCreator,
				m_cache.LangProject.AnthroListOA.PossibilitiesOS,
				m_mapAnthroCode, m_rgNewAnthroItem);
		}

		private bool StoreConfidence(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				rec.ConfidenceRA = rsf.m_tlo.m_default;
				return true;
			}
			if (m_mapConfidence.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS, m_mapConfidence);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapConfidence);
			if (poss != null)
			{
				rec.ConfidenceRA = poss;
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPossibility item = CreateNewConfidenceItem(rgsHier);
				rec.ConfidenceRA = item;
				return true;
			}
		}

		private ICmPossibility CreateNewConfidenceItem(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator,
				m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS,
				m_mapConfidence, m_rgNewConfidence);
		}

		private bool StoreLocation(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				ICmLocation def = rsf.m_tlo.m_default as ICmLocation;
				if (def != null && !rec.LocationsRC.Contains(def))
					rec.LocationsRC.Add(def);
				return true;
			}
			if (m_mapLocation.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.LocationsOA.PossibilitiesOS, m_mapLocation);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapLocation);
			if (poss != null)
			{
				if (!rec.LocationsRC.Contains(poss as ICmLocation))
					rec.LocationsRC.Add(poss as ICmLocation);
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmLocation item = CreateNewLocation(rgsHier);
				if (item != null)
					rec.LocationsRC.Add(item);
				return true;
			}
		}

		private ICmLocation CreateNewLocation(List<string> rgsHier)
		{
			return (ICmLocation)CreateNewPossibility(rgsHier, LocationCreator,
				m_cache.LangProject.LocationsOA.PossibilitiesOS,
				m_mapLocation, m_rgNewLocation);
		}

		private bool StorePhraseTag(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				if (rsf.m_tlo.m_default != null && !rec.PhraseTagsRC.Contains(rsf.m_tlo.m_default))
					rec.PhraseTagsRC.Add(rsf.m_tlo.m_default);
				return true;
			}
			if (m_mapPhraseTag.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS, m_mapPhraseTag);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapPhraseTag);
			if (poss != null)
			{
				if (!rec.PhraseTagsRC.Contains(poss))
					rec.PhraseTagsRC.Add(poss);
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPossibility item = CreateNewPhraseTag(rgsHier);
				if (item != null)
					rec.PhraseTagsRC.Add(item);
				return true;
			}
		}

		private ICmPossibility CreateNewPhraseTag(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator,
				m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS,
				m_mapPhraseTag, m_rgNewPhraseTag);
		}

		private bool StoreResearcher(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				ICmPerson def = rsf.m_tlo.m_default as ICmPerson;
				if (!rec.ResearchersRC.Contains(def))
					rec.ResearchersRC.Add(def);
				return true;
			}
			if (m_mapPeople.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
			if (poss != null)
			{
				if (!rec.ResearchersRC.Contains(poss as ICmPerson))
					rec.ResearchersRC.Add(poss as ICmPerson);
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPerson item = CreateNewPerson(rgsHier);
				if (item != null)
					rec.ResearchersRC.Add(item);
				return true;
			}
		}

		private ICmPerson CreateNewPerson(List<string> rgsHier)
		{
			return (ICmPerson)CreateNewPossibility(rgsHier, PersonCreator,
				m_cache.LangProject.PeopleOA.PossibilitiesOS,
				m_mapPeople, m_rgNewPeople);
		}

		private bool StoreSource(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				ICmPerson def = rsf.m_tlo.m_default as ICmPerson;
				if (def != null && !rec.SourcesRC.Contains(def))
					rec.SourcesRC.Add(def);
				return true;
			}
			if (m_mapPeople.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
			if (poss != null)
			{
				if (!rec.SourcesRC.Contains(poss as ICmPerson))
					rec.SourcesRC.Add(poss as ICmPerson);
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPerson item = CreateNewPerson(rgsHier);
				if (item != null)
					rec.SourcesRC.Add(item);
				return true;
			}
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
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				if (rsf.m_tlo.m_default != null && !rec.RestrictionsRC.Contains(rsf.m_tlo.m_default))
					rec.RestrictionsRC.Add(rsf.m_tlo.m_default);
				return true;
			}
			if (m_mapRestriction.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.RestrictionsOA.PossibilitiesOS, m_mapRestriction);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapRestriction);
			if (poss != null)
			{
				if (!rec.RestrictionsRC.Contains(poss))
					rec.RestrictionsRC.Add(poss);
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPossibility item = CreateNewRestriction(rgsHier);
				if (item != null)
					rec.RestrictionsRC.Add(item);
				return true;
			}
		}

		private ICmPossibility CreateNewRestriction(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator,
				m_cache.LangProject.RestrictionsOA.PossibilitiesOS,
				m_mapRestriction, m_rgNewRestriction);
		}

		private bool StoreStatus(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				rec.StatusRA = rsf.m_tlo.m_default;
				return true;
			}
			if (m_mapStatus.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.StatusOA.PossibilitiesOS, m_mapStatus);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapStatus);
			if (poss != null)
			{
				rec.StatusRA = poss;
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPossibility item = CreateNewStatus(rgsHier);
				rec.StatusRA = item;
				return true;
			}
		}

		private ICmPossibility CreateNewStatus(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator,
				m_cache.LangProject.StatusOA.PossibilitiesOS,
				m_mapStatus, m_rgNewStatus);
		}

		private bool StoreTimeOfEvent(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				if (rsf.m_tlo.m_default != null && !rec.TimeOfEventRC.Contains(rsf.m_tlo.m_default))
					rec.TimeOfEventRC.Add(rsf.m_tlo.m_default);
				return true;
			}
			if (m_mapTimeOfDay.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.TimeOfDayOA.PossibilitiesOS, m_mapTimeOfDay);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapTimeOfDay);
			if (poss != null)
			{
				if (!rec.TimeOfEventRC.Contains(poss))
					rec.TimeOfEventRC.Add(poss);
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPossibility item = CreateNewTimeOfDay(rgsHier);
				if (item != null)
					rec.TimeOfEventRC.Add(item);
				return true;
			}
		}

		private ICmPossibility CreateNewTimeOfDay(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator,
				m_cache.LangProject.TimeOfDayOA.PossibilitiesOS,
				m_mapTimeOfDay, m_rgNewTimeOfDay);
		}

		private bool StoreRecType(IRnGenericRec rec, RnSfMarker rsf, string sData)
		{
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				rec.TypeRA = rsf.m_tlo.m_default;
				return true;
			}
			if (m_mapRecType.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS, m_mapRecType);
			ICmPossibility poss = FindPossibilityOrNull(rgsHier, m_mapRecType);
			if (poss != null)
			{
				rec.TypeRA = poss;
				return true;
			}
			else if (rsf.m_tlo.m_fIgnoreNewStuff)
			{
				return false;
			}
			else
			{
				ICmPossibility item = CreateNewRecType(rgsHier);
				rec.TypeRA = item;
				return true;
			}
		}

		private ICmPossibility CreateNewRecType(List<string> rgsHier)
		{
			return CreateNewPossibility(rgsHier, PossibilityCreator,
				m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS,
				m_mapRecType, m_rgNewRecType);
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
					partic.ParticipantsRC.Add(def);
				return true;
			}
			if (m_mapPeople.Count == 0)
				FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
			var poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
			if (poss != null)
			{
				if (!partic.ParticipantsRC.Contains(poss as ICmPerson))
					partic.ParticipantsRC.Add(poss as ICmPerson);
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

		private bool StoreCustomListRefItem(IRnGenericRec rec, RnSfMarker rsf, string sData,
			ICmPossibilityList list)
		{
			// First, get the existing data so we can check whether the new item is needed,
			// and so that we know where to insert it (at the end) if it is.
			int chvo = m_cache.DomainDataByFlid.get_VecSize(rec.Hvo, rsf.m_flid);
			int[] hvosField;
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				m_cache.DomainDataByFlid.VecProp(rec.Hvo, rsf.m_flid, chvo, out chvo, arrayPtr);
				hvosField = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}
			ICmPossibility poss;
			List<string> rgsHier = SplitForSubitems(rsf, sData);
			if (rgsHier == null || rgsHier.Count == 0)
			{
				poss = rsf.m_tlo.m_default;
				if (poss != null && !hvosField.Contains(poss.Hvo) && poss.ClassID == list.ItemClsid)
				{
					m_cache.DomainDataByFlid.Replace(rec.Hvo, rsf.m_flid, chvo, chvo,
						new int[] { poss.Hvo }, 1);
				}
				return true;
			}
			switch (list.OwningFlid)
			{
				case LangProjectTags.kflidAnthroList:
					if (m_mapAnthroCode.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_mapAnthroCode);
					poss = FindPossibilityOrNull(rgsHier, m_mapAnthroCode);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewAnthroItem(rgsHier);
					break;
				case LangProjectTags.kflidConfidenceLevels:
					if (m_mapConfidence.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.ConfidenceLevelsOA.PossibilitiesOS, m_mapConfidence);
					poss = FindPossibilityOrNull(rgsHier, m_mapConfidence);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewConfidenceItem(rgsHier);
					break;
				case LangProjectTags.kflidLocations:
					if (m_mapLocation.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.LocationsOA.PossibilitiesOS, m_mapLocation);
					poss = FindPossibilityOrNull(rgsHier, m_mapLocation);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewLocation(rgsHier);
					break;
				case RnResearchNbkTags.kflidRecTypes:
					if (m_mapRecType.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.ResearchNotebookOA.RecTypesOA.PossibilitiesOS, m_mapRecType);
					poss = FindPossibilityOrNull(rgsHier, m_mapRecType);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewRecType(rgsHier);
					break;
				case LangProjectTags.kflidTextMarkupTags:
					if (m_mapPhraseTag.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS, m_mapPhraseTag);
					poss = FindPossibilityOrNull(rgsHier, m_mapPhraseTag);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewPhraseTag(rgsHier);
					break;
				case LangProjectTags.kflidPeople:
					if (m_mapPeople.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.PeopleOA.PossibilitiesOS, m_mapPeople);
					poss = FindPossibilityOrNull(rgsHier, m_mapPeople);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewPerson(rgsHier);
					break;
				case LangProjectTags.kflidRestrictions:
					if (m_mapRestriction.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.RestrictionsOA.PossibilitiesOS, m_mapRestriction);
					poss = FindPossibilityOrNull(rgsHier, m_mapRestriction);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewRestriction(rgsHier);
					break;
				case LangProjectTags.kflidStatus:
					if (m_mapStatus.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.StatusOA.PossibilitiesOS, m_mapStatus);
					poss = FindPossibilityOrNull(rgsHier, m_mapStatus);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewStatus(rgsHier);
					break;
				case LangProjectTags.kflidTimeOfDay:
					if (m_mapTimeOfDay.Count == 0)
						FillPossibilityMap(rsf, m_cache.LangProject.TimeOfDayOA.PossibilitiesOS, m_mapTimeOfDay);
					poss = FindPossibilityOrNull(rgsHier, m_mapTimeOfDay);
					if (poss == null && !rsf.m_tlo.m_fIgnoreNewStuff)
						poss = CreateNewTimeOfDay(rgsHier);
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
							poss =  CreateNewPossibility(rgsHier, creator, list.PossibilitiesOS, map, rgNew);
					}
					break;
			}
			if (poss != null && !hvosField.Contains(poss.Hvo) && poss.ClassID == list.ItemClsid)
			{
				m_cache.DomainDataByFlid.Replace(rec.Hvo, rsf.m_flid, chvo, chvo,
					new int[] { poss.Hvo }, 1);
				return true;
			}
			else
			{
				return !rsf.m_tlo.m_fIgnoreNewStuff;
			}
		}

		private static void FillPossibilityMap(RnSfMarker rsf, IFdoOwningSequence<ICmPossibility> seq,
			Dictionary<string, ICmPossibility> map)
		{
			if (seq == null || seq.Count == 0)
				return;
			bool fAbbrev = rsf.m_tlo.m_pnt == PossNameType.kpntAbbreviation;
			foreach (ICmPossibility poss in seq)
			{
				string sKey = fAbbrev ?
					poss.Abbreviation.AnalysisDefaultWritingSystem.Text :
					poss.Name.AnalysisDefaultWritingSystem.Text;
				if (String.IsNullOrEmpty(sKey))
					continue;
				sKey = sKey.ToLowerInvariant();
				if (map.ContainsKey(sKey))
					continue;
				map.Add(sKey, poss);
				FillPossibilityMap(rsf, poss.SubPossibilitiesOS, map);
			}
		}

		private ICmPossibility CreateNewPossibility(List<string> rgsHier,
			CmPossibilityCreator factory,
			IFdoOwningSequence<ICmPossibility> possList,
			Dictionary<string, ICmPossibility> map,
			List<ICmPossibility> rgNew)
		{
			ICmPossibility possParent = null;
			ICmPossibility poss = null;
			int i;
			for (i = 0; i < rgsHier.Count; ++i)
			{
				if (!map.TryGetValue(rgsHier[i].ToLowerInvariant(), out poss))
					break;
				if (i > 0 && poss.Owner != possParent)
					break;
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
					possList.Add(item);
				else
					itemParent.SubPossibilitiesOS.Add(item);
				ITsString tss = m_cache.TsStrFactory.MakeString(rgsHier[i], m_cache.DefaultAnalWs);
				item.Name.AnalysisDefaultWritingSystem = tss;
				item.Abbreviation.AnalysisDefaultWritingSystem = tss;
				map.Add(rgsHier[i].ToLowerInvariant(), item);
				rgNew.Add(item);
				itemParent = item;
			}
			return item;
		}

		public static bool InitializeWritingSystemCombo(string sWs, FdoCache cache, ComboBox cbWritingSystem)
		{
			return InitializeWritingSystemCombo(sWs, cache, cbWritingSystem,
				cache.ServiceLocator.WritingSystems.AllWritingSystems.ToArray());
		}


		public static bool InitializeWritingSystemCombo(string sWs, FdoCache cache, ComboBox cbWritingSystem, IWritingSystem[] writingSystems)
		{
			if (String.IsNullOrEmpty(sWs))
				sWs = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultAnalWs);
			cbWritingSystem.Items.Clear();
			cbWritingSystem.Sorted = true;
			cbWritingSystem.Items.AddRange(writingSystems);
			foreach (IWritingSystem ws in cbWritingSystem.Items)
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
			m_fDirtySettings = true;
		}
	}
}
