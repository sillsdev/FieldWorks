// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WritingSystemProperties.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Text;
using System.Resources;
using System.Data.SqlClient;
using Microsoft.Win32;
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Drawing;
using ECInterfaces;
using SilEncConverters31;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FwCoreDlgControls;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region IWritingSystemPropertiesDialog interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for writing system properties dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("5F00D289-1F0D-4DBC-B292-4F164B2FA4C5")]
	public interface IWritingSystemPropertiesDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the WritingSystemPropertiesDialog class. Clients written in .Net with an FdoCache
		/// should use the version of the constructor that accepts an FdoCache. COM clients that
		/// do not have an FdoCache should use the default constructor and then call this method
		/// to initialize the object.
		/// </summary>
		/// <param name="ode"></param>
		/// <param name="mdc"></param>
		/// <param name="oleDbAccess"></param>
		/// <param name="helpTopicProvider">IHelpTopicProvider object used to get help information</param>
		/// <param name="stylesheet">Used for the FwTextBox</param>
		/// ------------------------------------------------------------------------------------
		void Initialize(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess, IHelpTopicProvider helpTopicProvider, IVwStylesheet stylesheet);


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <param name="ws">The writing system which properties will be displayed</param>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		int ShowDialog(IWritingSystem ws);
	}
	#endregion //IWritingSystemPropertiesDialog interface

	#region WritingSystemPropertiesDialog dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Modify Writing System Properties dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.WritingSystemPropertiesDialog")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("36CFE045-BFDD-476a-81B8-C23925965702")]
	[ComVisible(true)]
	public class WritingSystemPropertiesDialog : Form, IFWDisposable, IWritingSystemPropertiesDialog
	{
		#region Member variables
		// List of the ranges that are acceptable range[0] is the start, range[1]
		// is the end, range[2] is a bool to indicate if it has been loaded
		private int[][] m_cachedIcuRanges = new int[][] {
			new int[] {0x0000, 0x07B1, 0},
			new int[] {0x0901, 0x19FF, 0},
			new int[] {0x1D00, 0x2B0D, 0},
			new int[] {0x2E81, 0x33FF, 0},
			new int[] {0x4DC0, 0x4DFF, 0},
			new int[] {0xA000, 0xA4C6, 0},

			// custom PUA ranges
			new int[] {0xE000, 0xEFFF, 0},
			new int[] {0xF000, 0xF8FF, 0},

			new int[] {0xFB00, 0x1083F, 0},
			new int[] {0x1D000, 0x1D7FF, 0},

			// CJKV ranges
			new int[] {0x2F800, 0x2FA10, 0},
			new int[] {0xE0001, 0xE01EF, 0},
			new int[] {0x3400, 0x4DB5, 0},
			new int[] {0x4000, 0x4DB5, 0},

			new int[] {0x4E00, 0x5FFF, 0},
			new int[] {0x6000, 0x6FFF, 0},
			new int[] {0x7000, 0x7FFF, 0},
			new int[] {0x8000, 0x8FFF, 0},
			new int[] {0x9000, 0x9FA5, 0},

			new int[] {0xAC00, 0xAFFF, 0},
			new int[] {0xB000, 0xBFFF, 0},
			new int[] {0xC000, 0xCFFF, 0},
			new int[] {0xD000, 0xD7A3, 0}
		};

		List<LanguageDefinition> m_listFinalLangDefsBackup = null;

		#region Constants
		/// <summary>Index(0) of the tab for writing systems General</summary>
		public const int kWsGeneral = 0;
		/// <summary>Index(1) of the tab for writing systems Fonts</summary>
		public const int kWsFonts = 1;
		/// <summary>Index(2) of the tab for writing systems Keyboard</summary>
		public const int kWsKeyboard = 2;
		/// <summary>Index(3) of the tab for writing systems Converters</summary>
		public const int kWsConverters = 3;
		/// <summary>Index(4) of the tab for writing system sorting</summary>
		public const int kWsSorting = 4;
		/// <summary>Index(5) of the tab for writing systems PUA characters</summary>
		public const int kWsPUACharacters = 5;

		#endregion

		#region Ws ListBox

		/// <summary> </summary>
		private Label m_writingSystemsFor;
		private Label lblHiddenWss;
		/// <summary> </summary>
		protected ListBox m_listBoxRelatedWSs;
		/// <summary> </summary>
		protected Button btnAdd;
		/// <summary> </summary>
		protected Button btnCopy;
		/// <summary> </summary>
		protected Button btnRemove;

		#endregion Ws ListBox

		#region LanguageName and Ethnologue Code

		/// <summary> </summary>
		protected TextBox m_tbLanguageName;
		/// <summary> </summary>
		protected Label m_LanguageCode;
		private LinkLabel m_linkToEthnologue;

		#endregion LanguageName and Ethnologue Code

		/// <summary> </summary>
		protected System.Windows.Forms.Button btnModifyEthnologueInfo;
		/// <summary> </summary>
		protected System.Windows.Forms.TabControl tabControl;
		/// <summary> </summary>
		protected System.Windows.Forms.Button btnOk;
		/// <summary> </summary>
		protected System.Windows.Forms.Button btnCancel;
		/// <summary> </summary>
		private System.Windows.Forms.Button btnHelp;

		/// <summary> </summary>
		public event EventHandler OnAboutToMergeWritingSystems;

		/// <summary> </summary>
		protected LanguageDefinitionFactory m_LanguageDefinitionFactory;
		private Set<NamedWritingSystem> m_NamedWritingSystemsDb;
		/// <summary> </summary>
		protected LanguageDefinition m_langDefCurrent;
		private int m_langDefCurrentIndex = 0;
		private List<LanguageDefinition> m_listOrigLangDefs = new List<LanguageDefinition>();
		/// <summary> language definitions which may have been modified by the dialog </summary>
		protected List<LanguageDefinition> m_listFinalLangDefs = new List<LanguageDefinition>();
		/// <summary> </summary>
		protected int m_displayWs;

		Set<int> m_activeWss = null;

		private string m_LanguageName = String.Empty;
		private bool m_fChanged = false;
		private bool m_fHandleSelectedIndexChanged = true;
		private bool m_fUserChangedText = false;
		private bool m_fUserChangedVariantControl = true;
		private bool m_fChangeSpellCheckDictionaryAllowed = true;
		private IHelpTopicProvider m_helpTopicProvider;
		private System.ComponentModel.IContainer components;
		// from the initialize method
		/// <summary> cache for determining related Wss.</summary>
		protected SIL.FieldWorks.FDO.FdoCache m_cache = null;
		/// <summary>stylesheet needed for the FwTextBox</summary>
		private IVwStylesheet m_stylesheet = null;
		private bool m_cacheMadeLocally = false;
		private System.Windows.Forms.HelpProvider helpProvider;
		// for merging writing systems
		private IFwTool m_fwt;
		private IApp m_app;
		private string m_strServer;
		private string m_strDatabase;
		private IStream m_strmLog;
		private int m_hvoProj;
		private int m_hvoRootObj;
		private System.Windows.Forms.ToolTip m_toolTip;
		private String m_FullNameGeneral;

		private int m_wsUser;
		private bool m_SortModified = false;
		private bool m_fNewRendering = false;

		private TabPage tpGeneral;
		private TabPage tpFonts;
		private TabPage tpKeyboard;
		private TabPage tpConverters;
		private TabPage tpSorting;
		private TabPage tpPUACharacters;
		private GroupBox groupBox2;
		private Label label1;
		private Label label3;
		private Label label5;

		#region General Tab

		/// <summary> Abbreviation: # </summary>
		protected TextBox m_ShortWsName;
		/// <summary> </summary>
		protected SIL.FieldWorks.FwCoreDlgControls.RegionVariantControl m_regionVariantControl;
		private GroupBox gbDirection;
		private RadioButton rbLeftToRight;
		/// <summary> Direction : () (#)</summary>
		protected RadioButton rbRightToLeft;
		private FwOverrideComboBox cbDictionaries;
		private Label lblSpellingDictionary;

		#endregion General Tab

		#region Fonts Tab

		/// <summary>
		/// </summary>
		protected SIL.FieldWorks.FwCoreDlgControls.DefaultFontsControl m_defaultFontsControl;

		#endregion

		#region Keyboard Tab

		private Label m_lblKeyboardInstruction;
		/// <summary> </summary>
		protected SIL.FieldWorks.FwCoreDlgControls.KeyboardControl m_KeyboardControl;
		private LinkLabel m_linkWindowsKeyboard;
		private Label m_lblKeyboardSetupInst;
		private LinkLabel m_linkKeymanConfiguration;
		private Label m_lblKeyboardTestInstr;

		#endregion Keyboard Tab

		#region Converters Tab

		private Button btnEncodingConverter;
		private Label m_lblEncodingConverter;
		/// <summary> </summary>
		protected FwOverrideComboBox cbEncodingConverter;

		#endregion Converters Tab

		#region Sorting Tab

		/// <summary>
		/// At some point we should turn this into a FwTextBox, unfortunately FwTextBox
		/// does not support multiple lines separated by new line characters and scrolling,
		/// for now we will activate the WS's keyboard when it is focused and set it to use
		/// the default font for the WS.
		/// </summary>
		protected System.Windows.Forms.TextBox txtIcuRules;
		private System.Windows.Forms.Label lblSortingAboveRules;
		private System.Windows.Forms.Label lblSortingRule1;
		private System.Windows.Forms.Label lblSortingRule2;
		private System.Windows.Forms.Label lblSortingRule3;
		private System.Windows.Forms.Label lblSortingBelowRules;
		private Label lblSimilarWss;
		private SIL.FieldWorks.FwCoreDlgControls.LocaleMenuButton btnSimilarWs;

		#endregion Sorting Tab

		#region Characters Tab

		/// <summary></summary>
		protected System.Windows.Forms.CheckedListBox m_lstPUACharacters;
		private Label m_lblCustomCharInstructions;
		private System.Windows.Forms.Button m_btnModifyPUA;
		private System.Windows.Forms.Button m_btnNewPUA;
		private Label lblCustomPUA;
		private Button btnValidChars;
		private Label m_lblValidCharacters;

		#endregion Characters Tab
		private Label m_lblPunctuation;
		private Button btnPunctuation;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_fwTextBoxTestWs;
		private Label lblFullCode;
		private Label m_FullCode;
		private Label lblScriptRegionVariant;
		private Button m_ampersandButton;
		private Button m_angleBracketButton;
		private Label m_lblCustomCharCondition1;
		private Label m_lblCustomCharCondition3;
		private Label m_lblCustomCharCondition2;


		/// <summary>
		/// Every unicode character stored in Icu parsed into <c>PUACharacter</c>s and cached for quick lookup.
		/// </summary>
		private RedBlackTree m_cachedIcu = null;

		#endregion

		#region Construction, deconstruction, and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public WritingSystemPropertiesDialog(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			 IVwStylesheet stylesheet)
			: this()
		{
			Initialize(cache, helpTopicProvider, stylesheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Initialize(FdoCache cache, IHelpTopicProvider helpTopicProvider, IVwStylesheet stylesheet)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = helpTopicProvider as IApp;
			SetupForWsMerges(null, null, 0, 0, 0);
			//Initialize(cache, helpTopicProvider);
			m_stylesheet = stylesheet;

			//Deliberately do NOT set the WSF of the text box. We want to be able to modify its (temporary, in-memory)
			//WS without affecting the real one.
			//ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			//m_fwTextBoxTestWs.WritingSystemFactory = wsf;
			//m_fwTextBoxTestWs.WritingSystemCode = m_cache.DefaultVernWs;
			//m_fwTextBoxTestWs.Tss = StringUtils.MakeTss(string.Empty, m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WritingSystemPropertiesDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public WritingSystemPropertiesDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			m_lblCustomCharInstructions.Tag = m_lblCustomCharInstructions.Text;
			m_lblCustomCharCondition2.Tag = m_lblCustomCharCondition2.Text;
			m_lblValidCharacters.Tag = m_lblValidCharacters.Text;
			m_lblPunctuation.Tag = m_lblPunctuation.Text;
			m_lblEncodingConverter.Tag = m_lblEncodingConverter.Text;
			m_lblKeyboardInstruction.Tag = m_lblKeyboardInstruction.Text;
			m_lblKeyboardTestInstr.Tag = m_lblKeyboardTestInstr.Text;

			//btnAdvanced.Tag = btnAdvanced.Text;
			//btnAdvanced.Image = ResourceHelper.MoreButtonDoubleArrowIcon;

			m_btnModifyPUA.Enabled = false;
			this.m_regionVariantControl.OnRegionVariantNameChanged += new System.EventHandler(this.m_regionVariantControlChanged);
			this.txtIcuRules.GotFocus += new EventHandler(txtIcuRules_GotFocus);
			this.txtIcuRules.LostFocus += new EventHandler(txtIcuRules_LostFocus);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// release managed objects
				if (components != null)
					components.Dispose();
				if (m_fwTextBoxTestWs != null)
					m_fwTextBoxTestWs.Dispose();
				// We may have made the cache from COM objects given to us by a COM client.
				// In that case, we have to dispose it.
				if (m_cacheMadeLocally && m_cache != null)
					m_cache.Dispose();
				if (m_langDefCurrent != null)
					m_langDefCurrent.ReleaseRootRb();
				if (cbDictionaries != null)
					cbDictionaries.Dispose();
			}

			// release unmanaged objects regardless of disposing flag
			if (m_fwt != null && Marshal.IsComObject(m_fwt))
			{
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_fwt);
				m_fwt = null;
			}

			if (m_strmLog != null && Marshal.IsComObject(m_strmLog))
			{
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_strmLog);
				m_strmLog = null;
			}

			PUACharacter.ReleaseTheCom();

			// Garbage collect the cached ICU
			m_cachedIcu = null;
			// GC.Collect(); Can't be deterministic about when it happens, even by calling for a collection.
			m_langDefCurrent = null;
			m_fwTextBoxTestWs = null;
			cbDictionaries = null;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide the information needed to merge one writing system into another, just in
		/// case the user decides to do so.
		/// </summary>
		/// <param name="fwt">IFwTool object used to open/close application window</param>
		/// <param name="strmLog">optional log file stream</param>
		/// <param name="hvoProj">Hvo of the Language Project</param>
		/// <param name="hvoRootObj">Hvo of the root object</param>
		/// <param name="wsUser">user interface writing system id</param>
		/// ------------------------------------------------------------------------------------
		public void SetupForWsMerges(IFwTool fwt, IStream strmLog, int hvoProj, int hvoRootObj, int wsUser)
		{
			CheckDisposed();

			string strServer = m_cache.ServerName;
			string strDatabase = m_cache.DatabaseName;
			CheckServerAndDatabaseInfo(strServer, strDatabase);

			m_fwt = fwt;
			m_strServer = strServer;
			m_strDatabase = strDatabase;
			m_strmLog = strmLog;
			m_hvoProj = hvoProj;
			m_hvoRootObj = hvoRootObj;
			m_wsUser = wsUser;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CheckServerAndDatabaseInfo(string strServer, string strDatabase)
		{
			if (strServer == null || strServer.Length == 0 ||
				strDatabase == null || strDatabase.Length == 0)
			{
				throw new ArgumentException();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the writing system information from the XML file or database.
		/// </summary>
		/// <param name="ws">Writing system the user chose to Modify</param>
		/// <returns><c>true</c> if successful, <c>false</c> if something went wrong.</returns>
		/// ------------------------------------------------------------------------------------
		private LanguageDefinition CreateLangDef(IWritingSystem ws)
		{
			LanguageDefinition newLangDef = null;
			try
			{
				newLangDef = CreateLanguageDefFromXml(ws.WritingSystemFactory, ws.IcuLocale);
				// ICU Locale should be case-insensitive, but import lets user get past
				// this restriction at the moment.  See LT-7299.
				if (newLangDef == null || newLangDef.WritingSystem.IcuLocale != ws.IcuLocale)
				{
					// The XML file was garbaged somehow -- retrieve what you can from the database.
					newLangDef = m_LanguageDefinitionFactory.Initialize(ws) as LanguageDefinition;
				}

				// It should never happen, but it did (hence TE-7177). The valid characters
				// read from the XML file should never be composed. I suspect the valid
				// characters written to the XML file for the WS used in TE-7177 was written
				// before valid characters were being decomposed before they were saved.
				// But just in case, this check will make sure they're decomposed.
				if (newLangDef.ValidChars != null)
				{
					ILgCharacterPropertyEngine cpe = LgIcuCharPropEngineClass.Create();
					newLangDef.ValidChars = cpe.NormalizeD(newLangDef.ValidChars);
				}
			}
			catch (FileNotFoundException)
			{
				string msg =
					string.Format(FwCoreDlgs.kstidNoLanguageDefinition,	ws.IcuLocale);
				MessageBox.Show(msg);
			}
			catch (Exception)
			{
				MessageBox.Show(string.Format(FwCoreDlgs.kstidInvalidXMlFile, ws.IcuLocale));
			}

			return newLangDef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LanguageDefinition EstablishCurrentLangDefFromSelectedIndex()
		{
			m_langDefCurrentIndex = m_listBoxRelatedWSs.SelectedIndex;
			m_langDefCurrent = m_listFinalLangDefs[m_langDefCurrentIndex];
			m_listBoxRelatedWSs.Focus();
			UpdateListBoxButtons();
			return m_langDefCurrent;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set writing system and initialize some values for the dialog
		/// </summary>
		/// <param name="wsSelected">The writing system.</param>
		/// <returns><c>true</c> if successful, <c>false</c> if something went wrong.</returns>
		/// ------------------------------------------------------------------------------------
		internal protected bool TrySetupDialog(IWritingSystem wsSelected)
		{
			// for now, always set the displayWs to "en" so we read good information from LDFs (LT-8628)
			//m_displayWs = wsSelected.WritingSystemFactory.UserWs;
			m_displayWs = wsSelected.WritingSystemFactory.get_Engine("en").WritingSystem;
			LoadAvailableConverters();
			m_LanguageDefinitionFactory = new LanguageDefinitionFactory();
			m_NamedWritingSystemsDb = m_cache.LangProject.GetDbNamedWritingSystems();

			//set up the initial state of the following lists
			CreateLanguageDefinitionsFromNamedWss(m_listOrigLangDefs, wsSelected);
			CreateLanguageDefinitionsFromNamedWss(m_listFinalLangDefs, wsSelected);
			PopulateRelatedWSsListBox();
			int selectedIndex = IndexOfFinalLangDef(wsSelected.IcuLocale);
			SelectIndexOfListBoxRelatedWss(selectedIndex);
			EstablishCurrentLangDefFromSelectedIndex();
			SetupDialogFromCurrentLanguageDefinition();
			m_fNewRendering = false;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the current m_langDefCurrent values to m_listFinalLangDefs
		/// m_langDefCurrentIndex is used to put the values in the correct location
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveCurrentLangDef()
		{
			//1) Need to save the PUAList characters
			SavePUACheckedChars(m_langDefCurrent);

			//2) Need to save ICU rules
			SaveICUrules(m_langDefCurrent);

			//3) Save Encoding Converters
			Save_cbEncodingConverter(m_langDefCurrent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveICUrules(LanguageDefinition langDef)
		{
			if (txtIcuRules.Enabled)
			{
				ICollation coll = UseOrCreateCollation(langDef);
				coll.IcuRules = txtIcuRules.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static ICollation UseOrCreateCollation(LanguageDefinition langDef)
		{
			ICollation coll = null;
			if (langDef.WritingSystem.CollationCount > 0)
				coll = langDef.WritingSystem.get_Collation(0);
			if (coll == null)
				coll = CollationClass.Create();
			langDef.WritingSystem.set_Collation(0, coll);
			return coll;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SavePUACheckedChars(LanguageDefinition langDef)
		{
			if (m_lstPUACharacters.Items.Count == 0)
				return;

			ValidCharacters validChars = ValidCharacters.Load(langDef);
			if (validChars == null)
				return;

			// First clear out any characters in the PUA list for the writing system
			// unless they are still in the list of checked items.
			// Also remove them from the valid characters list.
			foreach (PuaListItem puaItem in m_lstPUACharacters.Items)
			{
				if (!m_lstPUACharacters.CheckedItems.Contains(puaItem))
				{
					langDef.RemovePuaDefinition(puaItem.PUAChar.CharDef);

					char c = (char)puaItem.PUAChar.Character;
					validChars.RemoveCharacter(c.ToString());
				}
			}

			// Add the characters which are checked to the PUA list for the writing system and
			// the valid characters list.
			foreach (PuaListItem puaItem in m_lstPUACharacters.CheckedItems)
			{
				langDef.AddPuaDefinition(puaItem.PUAChar.CharDef);

				// Now add the character to the valid characters list if it is not a diacritic
				// or some other category that is disallowed in the valid characters list.
				string chr = ((char)puaItem.PUAChar.Character).ToString();
				ValidCharacterType chrType = langDef.GetOverrideCharType(chr);
				if (chrType != ValidCharacterType.None)
					validChars.AddCharacter(chr, chrType);
			}

			// Put the updated valid characters back into the final lang. definition.
			langDef.ValidChars = validChars.XmlString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set writing system and initialize some values for the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SwitchDialogToSelectedWS()
		{
			//before we change the dialog to reflect the newly selected writing system
			//we need to save all the values from the tabs that the user has changed.
			// Catch case where we are going to overwrite an existing writing system in the Db.
			SaveCurrentLangDef();
			EstablishCurrentLangDefFromSelectedIndex();
			SetupDialogFromCurrentLanguageDefinition();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupDialogFromCurrentLanguageDefinition()
		{
			LanguageDefinition langDef = m_langDefCurrent;
			// ensure our WsList matches the current language definition
			int indexCurrentLangDef = m_listFinalLangDefs.IndexOf(m_langDefCurrent);
			if (m_langDefCurrentIndex != indexCurrentLangDef)
				m_langDefCurrentIndex = indexCurrentLangDef;
			if (m_listBoxRelatedWSs.SelectedIndex != m_langDefCurrentIndex)
				SelectIndexOfListBoxRelatedWss(m_langDefCurrentIndex);
			m_ListBoxRelatedWSsSetDisplayname(m_langDefCurrentIndex, m_langDefCurrent.DisplayName);
			UpdateListBoxButtons();
			// Setup General Tab information
			m_LanguageName = langDef.LocaleName;
			if (m_LanguageName == null)
				m_LanguageName = String.Empty;
			Set_tbLanguageName(m_LanguageName);
			SetupEthnologueCode(langDef);

			m_defaultFontsControl.LangDef = langDef;

			m_KeyboardControl.LangDef = langDef;
			m_KeyboardControl.InitLanguageCombo();
			m_KeyboardControl.InitKeymanCombo();

			//Switch Encoding Converters to the one for the user selected writing system
			Select_cbEncodingConverter();

			PopulateSpellingDictionaryComboBox();

			// Setup Sorting tab
			SetupSortTab(langDef);
			SetupPUAList();

			// Update all the labels using the selected language display name
			SetLanguageNameLabels();
			Set_regionVariantControl(langDef);
			SetFullNameLabels(langDef.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupSortTab(LanguageDefinition langDef)
		{
			if (m_langDefCurrent.BaseLocale != null)
			{
				btnSimilarWs.SelectedLocaleId = m_langDefCurrent.BaseLocale;
			}
			else
			{
				System.ComponentModel.ComponentResourceManager resources =
					new System.ComponentModel.ComponentResourceManager(typeof(WritingSystemPropertiesDialog));
				// apply default text
				btnSimilarWs.Text = (string)resources.GetObject("btnSimilarWs.Text");
			}
			SetupICURulesAndBtnSimilarWs();
			// Copy the ICU rules from the first collation if it exists.
			if (langDef.WritingSystem.CollationCount > 0)
			{
				string icuRules = GetIcuRules(langDef);
				if (txtIcuRules.Enabled)
					txtIcuRules.Text = icuRules;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupEthnologueCode(LanguageDefinition langDef)
		{
			string strNone = FwCoreDlgs.kstidNone;
			if (langDef.EthnoCode != null && langDef.EthnoCode.Length > 0)
			{
				SetLanguageCodeLabels(langDef.EthnoCode);
			}
			else
			{
				SqlConnection dbConnection = null;
				SqlCommand sqlCommand = null;
				string sConnection = string.Format("Server={0}; Database=Ethnologue; User ID=FWDeveloper; " +
					"Password=careful; Pooling=false;", MiscUtils.LocalServerName);

				dbConnection = new SqlConnection(sConnection);
				dbConnection.Open();
				sqlCommand = dbConnection.CreateCommand();
				// REVIEW (SteveMiller): Isn't there a better way to get the output variable
				// than to do the IF and SELECT in dynamic SQL code? Seems like there should
				// be something in C# that would do this.
				sqlCommand.CommandText = string.Format("declare @EthnoCode nchar(3); " +
					"exec GetIsoCode '{0}', @EthnoCode output; " +
					"if @EthnoCode is not null select @EthnoCode",
					langDef.LocaleAbbr);
				SqlDataReader reader =
					sqlCommand.ExecuteReader(System.Data.CommandBehavior.Default);
				if (reader.HasRows)
				{
					reader.Read();
					SetLanguageCodeLabels(reader.GetString(0));
					langDef.EthnoCode = reader.GetString(0);
				}
				else
				{
					SetLanguageCodeLabels(strNone);
				}
				reader.Close();
				dbConnection.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save WS's that are related to the 'ws' parameter of SetupDialog()
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateLanguageDefinitionsFromNamedWss(List<LanguageDefinition> langDefList, IWritingSystem wsSelected)
		{
			langDefList.Clear();
			Set<NamedWritingSystem> relatedWss = GetRelatedWss(m_NamedWritingSystemsDb, wsSelected);
			List<NamedWritingSystem> relatedWssToSort = new List<NamedWritingSystem>(relatedWss.ToArray());
			relatedWssToSort.Sort();
			foreach (NamedWritingSystem namedWs in relatedWssToSort)
			{
				IWritingSystem tempWs = m_cache.LanguageWritingSystemFactoryAccessor.get_Engine(namedWs.IcuLocale);
				LanguageDefinition tempLangDef = CreateLangDef(tempWs);
				if (tempLangDef != null)
					langDefList.Add(tempLangDef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the Spelling Dictionaries fwComboBox
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PopulateSpellingDictionaryComboBox()
		{
			// REVIEW (SteveMiller): The doc says that the AddRange() method is the preferred
			// way to add items in the combo box. (See ComboBox.BeginUpdate Method in help.)
			// The BeginUpdate() and EndUpdate methods prevents the control from repainting
			// when using the Add() method as below. We may want to change this code to use
			// AddRange().
			cbDictionaries.BeginUpdate();
			cbDictionaries.Items.Clear(); //FwCoreDlgs.ksWsNoDictionaryMatches
			cbDictionaries.Items.Add(new NameAbbrComboItem(FwCoreDlgs.ksWsNoDictionaryMatches, ""));
			m_fChangeSpellCheckDictionaryAllowed = false;
			cbDictionaries.Text = FwCoreDlgs.ksWsNoDictionaryMatches;
			m_fChangeSpellCheckDictionaryAllowed = true;

			String spellCheckingDictionary = m_langDefCurrent.WritingSystem.SpellCheckDictionary;

			bool fDictionaryExistsForLanguage = false;
			bool fAlternateDictionaryExistsForLanguage = false;
			String sSelectComboItem = "";
			foreach (Enchant.DictionaryInfo info in Enchant.Broker.Default.Dictionaries)
			{
				Icu.UErrorCode err = Icu.UErrorCode.U_ZERO_ERROR;
				string languageName;
				string country;
				int len = Icu.GetDisplayCountry(info.Language, "en", out country, out err);
				len = Icu.GetDisplayLanguage(info.Language, "en", out languageName, out err);
				StringBuilder LanguageAndCountry = new StringBuilder(languageName);
				if (!String.IsNullOrEmpty(country))
				{
					LanguageAndCountry.AppendFormat(" ({0})", country);
				}
				if (languageName != info.Language)
					LanguageAndCountry.AppendFormat(" [{0}]", info.Language);
				cbDictionaries.Items.Add(new NameAbbrComboItem(LanguageAndCountry.ToString(), info.Language));
				//If this WS.SpellCheckingDictionary matches an Enchant Dictionary then
				//ensure the comboBox has that item selected.
				if (spellCheckingDictionary == info.Language)
				{
					sSelectComboItem = LanguageAndCountry.ToString();
					fDictionaryExistsForLanguage = true;
				}
				else if (fDictionaryExistsForLanguage == false && fAlternateDictionaryExistsForLanguage == false)
				{
					// The first half of the OR handles things like choosing the dictionary for 'en_US' when seeking one
					// for 'en', as in our extension to Enchant in EnchantHelper.DictionaryId. The second branch of the OR
					// handles Enchant's built-in behavior of returning the 'en' dictionary when asked for the 'en_US' one
					// (when it can't find an exact match).
					if (info.Language.StartsWith(spellCheckingDictionary) || spellCheckingDictionary.StartsWith(info.Language))
					{
						// Private dictionaries may only be used if they match the requested ID exactly.
						// See EnchantHelper.GetDictionary().
						if (!EnchantHelper.IsPrivateDictionary(info.Language))
						{
							sSelectComboItem = LanguageAndCountry.ToString();
							fAlternateDictionaryExistsForLanguage = true;
						}
					}
				}
			}
			if (fDictionaryExistsForLanguage || fAlternateDictionaryExistsForLanguage)
			{
				m_fChangeSpellCheckDictionaryAllowed = false;
				cbDictionaries.Text = sSelectComboItem;
				m_fChangeSpellCheckDictionaryAllowed = true;
			}

			cbDictionaries.EndUpdate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Filter namedWss with wss that have the same language name as wsSelected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Set<NamedWritingSystem> GetRelatedWss(Set<NamedWritingSystem> namedWss, IWritingSystem wsSelected)
		{
			Set<NamedWritingSystem> relatedWss = new Set<NamedWritingSystem>();
			foreach (NamedWritingSystem namedWs in namedWss)
			{
				if (namedWs.IsRelatedWs(wsSelected))
					relatedWss.Add(namedWs);
			}
			return relatedWss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the index of the langDef in the list of related languageDefinitions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int IndexOfFinalLangDef(string targetIcuLocale)
		{
			int index = 0;
			foreach (LanguageDefinition langCheck in m_listFinalLangDefs)
			{
				if (targetIcuLocale.Equals(langCheck.WritingSystem.IcuLocale))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display WS's that are related to the 'ws' parameter of SetupDialog()
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PopulateRelatedWSsListBox()
		{
			m_listBoxRelatedWSs.BeginUpdate();
			m_listBoxRelatedWSs.Items.Clear();
			//Change this dialog to populate the list box based on the DisplayName of
			//each WS contained in m_listFinalLangDefs
			foreach (LanguageDefinition tempLangDef in m_listFinalLangDefs)
			{
				//Then add this WS to the "RelatedWS ListBox"
				m_listBoxRelatedWSs.Items.Add(tempLangDef.DisplayName);
			}
			m_listBoxRelatedWSs.EndUpdate();
			// update buttons.
			UpdateListBoxButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use to allow a parent dialog to pass in the list of current active writing systems.
		/// FwProjPropertiesDlg maintains its own state of current active writing systems
		/// that does not get reflected in LangProject.GetActiveNamedWritingSystems() until
		/// after the user hits OK.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Set<int> ActiveWss
		{
			get
			{
				if (m_activeWss == null)
				{
					m_activeWss = new Set<int>();
					foreach (NamedWritingSystem nws in m_cache.LangProject.GetActiveNamedWritingSystems())
					{
						m_activeWss.Add(nws.Hvo);
					}
				}
				return m_activeWss;
			}

			set
			{
				m_activeWss = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsLangDefHidden(int index)
		{
			LanguageDefinition langDef = m_listFinalLangDefs[index];
			if (IsNew(langDef))
				return false;
			ILgWritingSystemFactory lgwsf = langDef.WritingSystem.WritingSystemFactory;
			int wsId = lgwsf.GetWsFromStr(langDef.IcuLocaleOriginal);
			ILangProject langProj = m_cache.LangProject;

			foreach (int activeWsId in this.ActiveWss)
			{
				if (activeWsId == wsId)
				{
					return false;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_listBoxRelatedWSs_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index == -1)
				return;
			bool selected = ((e.State & DrawItemState.Selected) != 0);
			Brush textBrush = SystemBrushes.ControlText;
			Font drawFont = new Font(e.Font, FontStyle.Regular);
			if (IsLangDefHidden(e.Index))
			{
				textBrush = SystemBrushes.GrayText;
				drawFont = new Font(e.Font, FontStyle.Italic);
			}
			if (selected)
				textBrush = SystemBrushes.HighlightText;
			e.DrawBackground();
			e.Graphics.DrawString(m_listBoxRelatedWSs.Items[e.Index].ToString(), drawFont, textBrush, e.Bounds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void UpdateListBoxButtons()
		{
			// Add & Copy  & Remove buttons
			bool enableAddCopyBtns = m_listBoxRelatedWSs.Items.Count > 0;
			btnAdd.Enabled = enableAddCopyBtns;
			btnCopy.Enabled = enableAddCopyBtns;
			UpdateRemoveButton();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateRemoveButton()
		{
			if (m_langDefCurrent != null && m_langDefCurrentIndex == m_listBoxRelatedWSs.SelectedIndex)
				btnRemove.Enabled = IsNew(m_langDefCurrent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ContextMenuStrip PopulateAddWsContextMenu()
		{
			ContextMenuStrip cmnuAddWs = new ContextMenuStrip();
			FwProjPropertiesDlg.PopulateWsContextMenu(cmnuAddWs, GetAllNamedWritingSystems(),
				m_listBoxRelatedWSs, btnAddWsItemClicked, null, btnNewWsItemClicked, m_listFinalLangDefs[0].WritingSystem);
			return cmnuAddWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual Set<NamedWritingSystem> GetAllNamedWritingSystems()
		{
			return m_cache.LangProject.GetAllNamedWritingSystems();
		}

		#region IWritingSystemPropertiesDialog Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the WritingSystemPropertiesDialog class. Clients written in .Net with an FdoCache
		/// should use the version of the constructor that accepts an FdoCache. COM clients that
		/// do not have an FdoCache should use the default constructor and then call this method
		/// to initialize the object.
		/// </summary>
		/// <param name="ode"></param>
		/// <param name="mdc"></param>
		/// <param name="oleDbAccess"></param>
		/// <param name="helpTopicProvider">IHelpTopicProvider object used to get help
		/// information</param>
		/// <param name="stylesheet">Used for the FwTextBox</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess,
			IHelpTopicProvider helpTopicProvider, IVwStylesheet stylesheet)
		{
			CheckDisposed();

			FdoCache cache = new FdoCache(ode, mdc, oleDbAccess);
			m_cacheMadeLocally = true;
			Initialize(cache, helpTopicProvider, stylesheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <param name="ws">The writing system which properties will be displayed</param>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int ShowDialog(IWritingSystem ws)
		{
			CheckDisposed();

			if (!TrySetupDialog(ws))
				return (int)DialogResult.Abort;
			return (int)base.ShowDialog();
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Select the encoding converty for the currently selected languageDefinition.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Select_cbEncodingConverter()
		{
			string strLegacyMapping = m_langDefCurrent.WritingSystem.LegacyMapping;
			if (strLegacyMapping == null)
				strLegacyMapping = FwCoreDlgs.kstidNone;
			if (!cbEncodingConverter.Items.Contains(strLegacyMapping))
			{
				strLegacyMapping = strLegacyMapping + FwCoreDlgs.kstidNotInstalled;
				cbEncodingConverter.Items.Add(strLegacyMapping);
			}
			cbEncodingConverter.SelectedItem = strLegacyMapping;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the selected encoding converter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Save_cbEncodingConverter(LanguageDefinition langdef)
		{
			// save the selected encoding converter
			string str = cbEncodingConverter.SelectedItem as string;
			if (str == FwCoreDlgs.kstidNone)
				str = null;
			Debug.Assert(str == null || !str.Contains(FwCoreDlgs.kstidNotInstalled));
			langdef.WritingSystem.LegacyMapping = str;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the Available Encoding Converters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void LoadAvailableConverters()
		{
			// Save the old selection so it can be restored after the combo box is filled
			string oldSelection = null;
			if (cbEncodingConverter.SelectedIndex != -1)
				oldSelection = (string)cbEncodingConverter.SelectedItem;
			try
			{
				EncConverters encConverters = new SilEncConverters31.EncConverters();
				cbEncodingConverter.Items.Clear();
				cbEncodingConverter.Items.Add(FwCoreDlgs.kstidNone);
				foreach (string convName in encConverters.Keys)
					cbEncodingConverter.Items.Add(convName);
				if (oldSelection != null)
					cbEncodingConverter.SelectedItem = oldSelection;
			}
			catch (Exception e)
			{
				// If the encoding converters failed, just put in a None entry
				Debug.WriteLine(e.Message);
				cbEncodingConverter.Items.Clear();
				cbEncodingConverter.Items.Add(FwCoreDlgs.kstidNone);
				cbEncodingConverter.SelectedIndex = 0;
			}
		}

		#endregion

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WritingSystemPropertiesDialog));
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tpGeneral = new System.Windows.Forms.TabPage();
			this.lblScriptRegionVariant = new System.Windows.Forms.Label();
			this.m_FullCode = new System.Windows.Forms.Label();
			this.lblFullCode = new System.Windows.Forms.Label();
			this.lblSpellingDictionary = new System.Windows.Forms.Label();
			this.cbDictionaries = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_regionVariantControl = new SIL.FieldWorks.FwCoreDlgControls.RegionVariantControl();
			this.gbDirection = new System.Windows.Forms.GroupBox();
			this.rbLeftToRight = new System.Windows.Forms.RadioButton();
			this.rbRightToLeft = new System.Windows.Forms.RadioButton();
			this.m_ShortWsName = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.tpFonts = new System.Windows.Forms.TabPage();
			this.m_defaultFontsControl = new SIL.FieldWorks.FwCoreDlgControls.DefaultFontsControl();
			this.tpKeyboard = new System.Windows.Forms.TabPage();
			this.m_fwTextBoxTestWs = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_KeyboardControl = new SIL.FieldWorks.FwCoreDlgControls.KeyboardControl();
			this.m_lblKeyboardTestInstr = new System.Windows.Forms.Label();
			this.m_linkKeymanConfiguration = new System.Windows.Forms.LinkLabel();
			this.m_linkWindowsKeyboard = new System.Windows.Forms.LinkLabel();
			this.m_lblKeyboardSetupInst = new System.Windows.Forms.Label();
			this.m_lblKeyboardInstruction = new System.Windows.Forms.Label();
			this.tpConverters = new System.Windows.Forms.TabPage();
			this.btnEncodingConverter = new System.Windows.Forms.Button();
			this.m_lblEncodingConverter = new System.Windows.Forms.Label();
			this.cbEncodingConverter = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.tpSorting = new System.Windows.Forms.TabPage();
			this.m_angleBracketButton = new System.Windows.Forms.Button();
			this.m_ampersandButton = new System.Windows.Forms.Button();
			this.btnSimilarWs = new SIL.FieldWorks.FwCoreDlgControls.LocaleMenuButton();
			this.lblSimilarWss = new System.Windows.Forms.Label();
			this.lblSortingBelowRules = new System.Windows.Forms.Label();
			this.lblSortingRule3 = new System.Windows.Forms.Label();
			this.lblSortingRule2 = new System.Windows.Forms.Label();
			this.lblSortingRule1 = new System.Windows.Forms.Label();
			this.txtIcuRules = new System.Windows.Forms.TextBox();
			this.lblSortingAboveRules = new System.Windows.Forms.Label();
			this.tpPUACharacters = new System.Windows.Forms.TabPage();
			this.m_lblCustomCharCondition3 = new System.Windows.Forms.Label();
			this.m_lblCustomCharCondition2 = new System.Windows.Forms.Label();
			this.m_lblCustomCharCondition1 = new System.Windows.Forms.Label();
			this.m_lblPunctuation = new System.Windows.Forms.Label();
			this.btnPunctuation = new System.Windows.Forms.Button();
			this.m_lblCustomCharInstructions = new System.Windows.Forms.Label();
			this.m_lblValidCharacters = new System.Windows.Forms.Label();
			this.lblCustomPUA = new System.Windows.Forms.Label();
			this.btnValidChars = new System.Windows.Forms.Button();
			this.m_btnModifyPUA = new System.Windows.Forms.Button();
			this.m_btnNewPUA = new System.Windows.Forms.Button();
			this.m_lstPUACharacters = new System.Windows.Forms.CheckedListBox();
			this.btnModifyEthnologueInfo = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_listBoxRelatedWSs = new System.Windows.Forms.ListBox();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnCopy = new System.Windows.Forms.Button();
			this.btnRemove = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.m_linkToEthnologue = new System.Windows.Forms.LinkLabel();
			this.m_LanguageCode = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.m_tbLanguageName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_writingSystemsFor = new System.Windows.Forms.Label();
			this.lblHiddenWss = new System.Windows.Forms.Label();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.tabControl.SuspendLayout();
			this.tpGeneral.SuspendLayout();
			this.gbDirection.SuspendLayout();
			this.tpFonts.SuspendLayout();
			this.tpKeyboard.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxTestWs)).BeginInit();
			this.tpConverters.SuspendLayout();
			this.tpSorting.SuspendLayout();
			this.tpPUACharacters.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			//
			// tabControl
			//
			this.tabControl.Controls.Add(this.tpGeneral);
			this.tabControl.Controls.Add(this.tpFonts);
			this.tabControl.Controls.Add(this.tpKeyboard);
			this.tabControl.Controls.Add(this.tpConverters);
			this.tabControl.Controls.Add(this.tpSorting);
			this.tabControl.Controls.Add(this.tpPUACharacters);
			this.tabControl.HotTrack = true;
			resources.ApplyResources(this.tabControl, "tabControl");
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.helpProvider.SetShowHelp(this.tabControl, ((bool)(resources.GetObject("tabControl.ShowHelp"))));
			this.tabControl.Deselecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Deselecting);
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			//
			// tpGeneral
			//
			this.tpGeneral.Controls.Add(this.lblScriptRegionVariant);
			this.tpGeneral.Controls.Add(this.m_FullCode);
			this.tpGeneral.Controls.Add(this.lblFullCode);
			this.tpGeneral.Controls.Add(this.lblSpellingDictionary);
			this.tpGeneral.Controls.Add(this.cbDictionaries);
			this.tpGeneral.Controls.Add(this.m_regionVariantControl);
			this.tpGeneral.Controls.Add(this.gbDirection);
			this.tpGeneral.Controls.Add(this.m_ShortWsName);
			this.tpGeneral.Controls.Add(this.label5);
			resources.ApplyResources(this.tpGeneral, "tpGeneral");
			this.tpGeneral.Name = "tpGeneral";
			this.helpProvider.SetShowHelp(this.tpGeneral, ((bool)(resources.GetObject("tpGeneral.ShowHelp"))));
			this.tpGeneral.UseVisualStyleBackColor = true;
			//
			// lblScriptRegionVariant
			//
			resources.ApplyResources(this.lblScriptRegionVariant, "lblScriptRegionVariant");
			this.lblScriptRegionVariant.Name = "lblScriptRegionVariant";
			this.helpProvider.SetShowHelp(this.lblScriptRegionVariant, ((bool)(resources.GetObject("lblScriptRegionVariant.ShowHelp"))));
			//
			// m_FullCode
			//
			resources.ApplyResources(this.m_FullCode, "m_FullCode");
			this.m_FullCode.Name = "m_FullCode";
			//
			// lblFullCode
			//
			resources.ApplyResources(this.lblFullCode, "lblFullCode");
			this.lblFullCode.Name = "lblFullCode";
			//
			// lblSpellingDictionary
			//
			resources.ApplyResources(this.lblSpellingDictionary, "lblSpellingDictionary");
			this.lblSpellingDictionary.Name = "lblSpellingDictionary";
			//
			// cbDictionaries
			//
			this.cbDictionaries.AllowSpaceInEditBox = false;
			this.cbDictionaries.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbDictionaries.FormattingEnabled = true;
			resources.ApplyResources(this.cbDictionaries, "cbDictionaries");
			this.cbDictionaries.Name = "cbDictionaries";
			this.cbDictionaries.Sorted = true;
			this.cbDictionaries.TextChanged += new System.EventHandler(this.cbDictionaries_TextChanged);
			//
			// m_regionVariantControl
			//
			this.m_regionVariantControl.BackColor = System.Drawing.Color.Transparent;
			this.m_regionVariantControl.LangDef = null;
			resources.ApplyResources(this.m_regionVariantControl, "m_regionVariantControl");
			this.m_regionVariantControl.Name = "m_regionVariantControl";
			this.m_regionVariantControl.PropDlg = false;
			this.helpProvider.SetShowHelp(this.m_regionVariantControl, ((bool)(resources.GetObject("m_regionVariantControl.ShowHelp"))));
			//
			// gbDirection
			//
			this.gbDirection.Controls.Add(this.rbLeftToRight);
			this.gbDirection.Controls.Add(this.rbRightToLeft);
			resources.ApplyResources(this.gbDirection, "gbDirection");
			this.gbDirection.Name = "gbDirection";
			this.helpProvider.SetShowHelp(this.gbDirection, ((bool)(resources.GetObject("gbDirection.ShowHelp"))));
			this.gbDirection.TabStop = false;
			//
			// rbLeftToRight
			//
			this.helpProvider.SetHelpString(this.rbLeftToRight, resources.GetString("rbLeftToRight.HelpString"));
			resources.ApplyResources(this.rbLeftToRight, "rbLeftToRight");
			this.rbLeftToRight.Name = "rbLeftToRight";
			this.helpProvider.SetShowHelp(this.rbLeftToRight, ((bool)(resources.GetObject("rbLeftToRight.ShowHelp"))));
			this.rbLeftToRight.TabStop = true;
			this.rbLeftToRight.CheckedChanged += new System.EventHandler(this.rbLeftToRight_CheckedChanged);
			//
			// rbRightToLeft
			//
			this.helpProvider.SetHelpString(this.rbRightToLeft, resources.GetString("rbRightToLeft.HelpString"));
			resources.ApplyResources(this.rbRightToLeft, "rbRightToLeft");
			this.rbRightToLeft.Name = "rbRightToLeft";
			this.helpProvider.SetShowHelp(this.rbRightToLeft, ((bool)(resources.GetObject("rbRightToLeft.ShowHelp"))));
			this.rbRightToLeft.CheckedChanged += new System.EventHandler(this.rbLeftToRight_CheckedChanged);
			//
			// m_ShortWsName
			//
			this.helpProvider.SetHelpString(this.m_ShortWsName, resources.GetString("m_ShortWsName.HelpString"));
			resources.ApplyResources(this.m_ShortWsName, "m_ShortWsName");
			this.m_ShortWsName.Name = "m_ShortWsName";
			this.helpProvider.SetShowHelp(this.m_ShortWsName, ((bool)(resources.GetObject("m_ShortWsName.ShowHelp"))));
			this.m_ShortWsName.TextChanged += new System.EventHandler(this.m_ShortWsName_TextChanged);
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			this.helpProvider.SetShowHelp(this.label5, ((bool)(resources.GetObject("label5.ShowHelp"))));
			//
			// tpFonts
			//
			this.tpFonts.Controls.Add(this.m_defaultFontsControl);
			resources.ApplyResources(this.tpFonts, "tpFonts");
			this.tpFonts.Name = "tpFonts";
			this.helpProvider.SetShowHelp(this.tpFonts, ((bool)(resources.GetObject("tpFonts.ShowHelp"))));
			this.tpFonts.UseVisualStyleBackColor = true;
			//
			// m_defaultFontsControl
			//
			resources.ApplyResources(this.m_defaultFontsControl, "m_defaultFontsControl");
			this.m_defaultFontsControl.DefaultHeadingFont = "";
			this.m_defaultFontsControl.DefaultNormalFont = "";
			this.m_defaultFontsControl.DefaultPublicationFont = "";
			this.helpProvider.SetHelpString(this.m_defaultFontsControl, resources.GetString("m_defaultFontsControl.HelpString"));
			this.m_defaultFontsControl.LangDef = null;
			this.m_defaultFontsControl.Name = "m_defaultFontsControl";
			this.helpProvider.SetShowHelp(this.m_defaultFontsControl, ((bool)(resources.GetObject("m_defaultFontsControl.ShowHelp"))));
			//
			// tpKeyboard
			//
			this.tpKeyboard.Controls.Add(this.m_fwTextBoxTestWs);
			this.tpKeyboard.Controls.Add(this.m_KeyboardControl);
			this.tpKeyboard.Controls.Add(this.m_lblKeyboardTestInstr);
			this.tpKeyboard.Controls.Add(this.m_linkKeymanConfiguration);
			this.tpKeyboard.Controls.Add(this.m_linkWindowsKeyboard);
			this.tpKeyboard.Controls.Add(this.m_lblKeyboardSetupInst);
			this.tpKeyboard.Controls.Add(this.m_lblKeyboardInstruction);
			resources.ApplyResources(this.tpKeyboard, "tpKeyboard");
			this.tpKeyboard.Name = "tpKeyboard";
			this.helpProvider.SetShowHelp(this.tpKeyboard, ((bool)(resources.GetObject("tpKeyboard.ShowHelp"))));
			this.tpKeyboard.UseVisualStyleBackColor = true;
			//
			// m_fwTextBoxTestWs
			//
			this.m_fwTextBoxTestWs.AdjustStringHeight = true;
			this.m_fwTextBoxTestWs.AllowMultipleLines = true;
			this.m_fwTextBoxTestWs.BackColor = System.Drawing.SystemColors.Window;
			this.m_fwTextBoxTestWs.controlID = null;
			resources.ApplyResources(this.m_fwTextBoxTestWs, "m_fwTextBoxTestWs");
			this.m_fwTextBoxTestWs.HasBorder = true;
			this.m_fwTextBoxTestWs.Name = "m_fwTextBoxTestWs";
			this.m_fwTextBoxTestWs.SelectionLength = 0;
			this.m_fwTextBoxTestWs.SelectionStart = 0;
			this.m_fwTextBoxTestWs.Enter += new System.EventHandler(this.m_fwTextBoxTestWs_Enter);
			//
			// m_KeyboardControl
			//
			this.m_KeyboardControl.LangDef = null;
			resources.ApplyResources(this.m_KeyboardControl, "m_KeyboardControl");
			this.m_KeyboardControl.Name = "m_KeyboardControl";
			this.helpProvider.SetShowHelp(this.m_KeyboardControl, ((bool)(resources.GetObject("m_KeyboardControl.ShowHelp"))));
			this.m_KeyboardControl.Enter += new System.EventHandler(this.OnGetFocus);
			//
			// m_lblKeyboardTestInstr
			//
			resources.ApplyResources(this.m_lblKeyboardTestInstr, "m_lblKeyboardTestInstr");
			this.m_lblKeyboardTestInstr.Name = "m_lblKeyboardTestInstr";
			this.helpProvider.SetShowHelp(this.m_lblKeyboardTestInstr, ((bool)(resources.GetObject("m_lblKeyboardTestInstr.ShowHelp"))));
			//
			// m_linkKeymanConfiguration
			//
			resources.ApplyResources(this.m_linkKeymanConfiguration, "m_linkKeymanConfiguration");
			this.m_linkKeymanConfiguration.Name = "m_linkKeymanConfiguration";
			this.helpProvider.SetShowHelp(this.m_linkKeymanConfiguration, ((bool)(resources.GetObject("m_linkKeymanConfiguration.ShowHelp"))));
			this.m_linkKeymanConfiguration.TabStop = true;
			this.m_linkKeymanConfiguration.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkKeymanConfiguration_LinkClicked);
			//
			// m_linkWindowsKeyboard
			//
			resources.ApplyResources(this.m_linkWindowsKeyboard, "m_linkWindowsKeyboard");
			this.m_linkWindowsKeyboard.Name = "m_linkWindowsKeyboard";
			this.helpProvider.SetShowHelp(this.m_linkWindowsKeyboard, ((bool)(resources.GetObject("m_linkWindowsKeyboard.ShowHelp"))));
			this.m_linkWindowsKeyboard.TabStop = true;
			this.m_linkWindowsKeyboard.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkWindowsKeyboard_LinkClicked);
			//
			// m_lblKeyboardSetupInst
			//
			resources.ApplyResources(this.m_lblKeyboardSetupInst, "m_lblKeyboardSetupInst");
			this.m_lblKeyboardSetupInst.Name = "m_lblKeyboardSetupInst";
			this.helpProvider.SetShowHelp(this.m_lblKeyboardSetupInst, ((bool)(resources.GetObject("m_lblKeyboardSetupInst.ShowHelp"))));
			//
			// m_lblKeyboardInstruction
			//
			resources.ApplyResources(this.m_lblKeyboardInstruction, "m_lblKeyboardInstruction");
			this.m_lblKeyboardInstruction.Name = "m_lblKeyboardInstruction";
			this.helpProvider.SetShowHelp(this.m_lblKeyboardInstruction, ((bool)(resources.GetObject("m_lblKeyboardInstruction.ShowHelp"))));
			//
			// tpConverters
			//
			this.tpConverters.Controls.Add(this.btnEncodingConverter);
			this.tpConverters.Controls.Add(this.m_lblEncodingConverter);
			this.tpConverters.Controls.Add(this.cbEncodingConverter);
			resources.ApplyResources(this.tpConverters, "tpConverters");
			this.tpConverters.Name = "tpConverters";
			this.helpProvider.SetShowHelp(this.tpConverters, ((bool)(resources.GetObject("tpConverters.ShowHelp"))));
			this.tpConverters.UseVisualStyleBackColor = true;
			//
			// btnEncodingConverter
			//
			this.helpProvider.SetHelpString(this.btnEncodingConverter, resources.GetString("btnEncodingConverter.HelpString"));
			resources.ApplyResources(this.btnEncodingConverter, "btnEncodingConverter");
			this.btnEncodingConverter.Name = "btnEncodingConverter";
			this.helpProvider.SetShowHelp(this.btnEncodingConverter, ((bool)(resources.GetObject("btnEncodingConverter.ShowHelp"))));
			this.btnEncodingConverter.Click += new System.EventHandler(this.btnEncodingConverter_Click);
			//
			// m_lblEncodingConverter
			//
			resources.ApplyResources(this.m_lblEncodingConverter, "m_lblEncodingConverter");
			this.m_lblEncodingConverter.Name = "m_lblEncodingConverter";
			this.helpProvider.SetShowHelp(this.m_lblEncodingConverter, ((bool)(resources.GetObject("m_lblEncodingConverter.ShowHelp"))));
			//
			// cbEncodingConverter
			//
			this.cbEncodingConverter.AllowSpaceInEditBox = false;
			this.cbEncodingConverter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider.SetHelpString(this.cbEncodingConverter, resources.GetString("cbEncodingConverter.HelpString"));
			resources.ApplyResources(this.cbEncodingConverter, "cbEncodingConverter");
			this.cbEncodingConverter.Name = "cbEncodingConverter";
			this.helpProvider.SetShowHelp(this.cbEncodingConverter, ((bool)(resources.GetObject("cbEncodingConverter.ShowHelp"))));
			this.cbEncodingConverter.Sorted = true;
			//
			// tpSorting
			//
			this.tpSorting.BackColor = System.Drawing.Color.Transparent;
			this.tpSorting.Controls.Add(this.m_angleBracketButton);
			this.tpSorting.Controls.Add(this.m_ampersandButton);
			this.tpSorting.Controls.Add(this.btnSimilarWs);
			this.tpSorting.Controls.Add(this.lblSimilarWss);
			this.tpSorting.Controls.Add(this.lblSortingBelowRules);
			this.tpSorting.Controls.Add(this.lblSortingRule3);
			this.tpSorting.Controls.Add(this.lblSortingRule2);
			this.tpSorting.Controls.Add(this.lblSortingRule1);
			this.tpSorting.Controls.Add(this.txtIcuRules);
			this.tpSorting.Controls.Add(this.lblSortingAboveRules);
			resources.ApplyResources(this.tpSorting, "tpSorting");
			this.tpSorting.Name = "tpSorting";
			this.helpProvider.SetShowHelp(this.tpSorting, ((bool)(resources.GetObject("tpSorting.ShowHelp"))));
			this.tpSorting.UseVisualStyleBackColor = true;
			//
			// m_angleBracketButton
			//
			resources.ApplyResources(this.m_angleBracketButton, "m_angleBracketButton");
			this.m_angleBracketButton.Name = "m_angleBracketButton";
			this.helpProvider.SetShowHelp(this.m_angleBracketButton, ((bool)(resources.GetObject("m_angleBracketButton.ShowHelp"))));
			this.m_angleBracketButton.UseVisualStyleBackColor = true;
			this.m_angleBracketButton.Click += new System.EventHandler(this.m_angleBracketButton_Click);
			//
			// m_ampersandButton
			//
			resources.ApplyResources(this.m_ampersandButton, "m_ampersandButton");
			this.m_ampersandButton.Name = "m_ampersandButton";
			this.m_ampersandButton.UseVisualStyleBackColor = true;
			this.m_ampersandButton.Click += new System.EventHandler(this.m_ampersandButton_Click);
			//
			// btnSimilarWs
			//
			this.btnSimilarWs.DisplayLocaleId = null;
			resources.ApplyResources(this.btnSimilarWs, "btnSimilarWs");
			this.btnSimilarWs.Name = "btnSimilarWs";
			this.btnSimilarWs.SelectedLocaleId = null;
			this.helpProvider.SetShowHelp(this.btnSimilarWs, ((bool)(resources.GetObject("btnSimilarWs.ShowHelp"))));
			this.btnSimilarWs.UseVisualStyleBackColor = true;
			this.btnSimilarWs.LocaleSelected += new System.EventHandler(this.btnSimilarWs_LocaleSelected);
			//
			// lblSimilarWss
			//
			resources.ApplyResources(this.lblSimilarWss, "lblSimilarWss");
			this.lblSimilarWss.Name = "lblSimilarWss";
			this.helpProvider.SetShowHelp(this.lblSimilarWss, ((bool)(resources.GetObject("lblSimilarWss.ShowHelp"))));
			//
			// lblSortingBelowRules
			//
			resources.ApplyResources(this.lblSortingBelowRules, "lblSortingBelowRules");
			this.lblSortingBelowRules.Name = "lblSortingBelowRules";
			this.helpProvider.SetShowHelp(this.lblSortingBelowRules, ((bool)(resources.GetObject("lblSortingBelowRules.ShowHelp"))));
			//
			// lblSortingRule3
			//
			resources.ApplyResources(this.lblSortingRule3, "lblSortingRule3");
			this.lblSortingRule3.Name = "lblSortingRule3";
			this.helpProvider.SetShowHelp(this.lblSortingRule3, ((bool)(resources.GetObject("lblSortingRule3.ShowHelp"))));
			this.lblSortingRule3.UseMnemonic = false;
			//
			// lblSortingRule2
			//
			resources.ApplyResources(this.lblSortingRule2, "lblSortingRule2");
			this.lblSortingRule2.Name = "lblSortingRule2";
			this.helpProvider.SetShowHelp(this.lblSortingRule2, ((bool)(resources.GetObject("lblSortingRule2.ShowHelp"))));
			this.lblSortingRule2.UseMnemonic = false;
			//
			// lblSortingRule1
			//
			resources.ApplyResources(this.lblSortingRule1, "lblSortingRule1");
			this.lblSortingRule1.Name = "lblSortingRule1";
			this.helpProvider.SetShowHelp(this.lblSortingRule1, ((bool)(resources.GetObject("lblSortingRule1.ShowHelp"))));
			this.lblSortingRule1.UseMnemonic = false;
			//
			// txtIcuRules
			//
			this.txtIcuRules.AcceptsReturn = true;
			resources.ApplyResources(this.txtIcuRules, "txtIcuRules");
			this.txtIcuRules.Name = "txtIcuRules";
			this.helpProvider.SetShowHelp(this.txtIcuRules, ((bool)(resources.GetObject("txtIcuRules.ShowHelp"))));
			//
			// lblSortingAboveRules
			//
			resources.ApplyResources(this.lblSortingAboveRules, "lblSortingAboveRules");
			this.lblSortingAboveRules.Name = "lblSortingAboveRules";
			this.helpProvider.SetShowHelp(this.lblSortingAboveRules, ((bool)(resources.GetObject("lblSortingAboveRules.ShowHelp"))));
			//
			// tpPUACharacters
			//
			this.tpPUACharacters.BackColor = System.Drawing.Color.Transparent;
			this.tpPUACharacters.Controls.Add(this.m_lblCustomCharCondition3);
			this.tpPUACharacters.Controls.Add(this.m_lblCustomCharCondition2);
			this.tpPUACharacters.Controls.Add(this.m_lblCustomCharCondition1);
			this.tpPUACharacters.Controls.Add(this.m_lblPunctuation);
			this.tpPUACharacters.Controls.Add(this.btnPunctuation);
			this.tpPUACharacters.Controls.Add(this.m_lblCustomCharInstructions);
			this.tpPUACharacters.Controls.Add(this.m_lblValidCharacters);
			this.tpPUACharacters.Controls.Add(this.lblCustomPUA);
			this.tpPUACharacters.Controls.Add(this.btnValidChars);
			this.tpPUACharacters.Controls.Add(this.m_btnModifyPUA);
			this.tpPUACharacters.Controls.Add(this.m_btnNewPUA);
			this.tpPUACharacters.Controls.Add(this.m_lstPUACharacters);
			resources.ApplyResources(this.tpPUACharacters, "tpPUACharacters");
			this.tpPUACharacters.Name = "tpPUACharacters";
			this.helpProvider.SetShowHelp(this.tpPUACharacters, ((bool)(resources.GetObject("tpPUACharacters.ShowHelp"))));
			this.tpPUACharacters.UseVisualStyleBackColor = true;
			//
			// m_lblCustomCharCondition3
			//
			resources.ApplyResources(this.m_lblCustomCharCondition3, "m_lblCustomCharCondition3");
			this.m_lblCustomCharCondition3.Name = "m_lblCustomCharCondition3";
			this.helpProvider.SetShowHelp(this.m_lblCustomCharCondition3, ((bool)(resources.GetObject("m_lblCustomCharCondition3.ShowHelp"))));
			//
			// m_lblCustomCharCondition2
			//
			resources.ApplyResources(this.m_lblCustomCharCondition2, "m_lblCustomCharCondition2");
			this.m_lblCustomCharCondition2.Name = "m_lblCustomCharCondition2";
			this.helpProvider.SetShowHelp(this.m_lblCustomCharCondition2, ((bool)(resources.GetObject("m_lblCustomCharCondition2.ShowHelp"))));
			//
			// m_lblCustomCharCondition1
			//
			resources.ApplyResources(this.m_lblCustomCharCondition1, "m_lblCustomCharCondition1");
			this.m_lblCustomCharCondition1.Name = "m_lblCustomCharCondition1";
			this.helpProvider.SetShowHelp(this.m_lblCustomCharCondition1, ((bool)(resources.GetObject("m_lblCustomCharCondition1.ShowHelp"))));
			//
			// m_lblPunctuation
			//
			resources.ApplyResources(this.m_lblPunctuation, "m_lblPunctuation");
			this.m_lblPunctuation.Name = "m_lblPunctuation";
			this.helpProvider.SetShowHelp(this.m_lblPunctuation, ((bool)(resources.GetObject("m_lblPunctuation.ShowHelp"))));
			//
			// btnPunctuation
			//
			this.helpProvider.SetHelpString(this.btnPunctuation, resources.GetString("btnPunctuation.HelpString"));
			resources.ApplyResources(this.btnPunctuation, "btnPunctuation");
			this.btnPunctuation.Name = "btnPunctuation";
			this.helpProvider.SetShowHelp(this.btnPunctuation, ((bool)(resources.GetObject("btnPunctuation.ShowHelp"))));
			this.btnPunctuation.Click += new System.EventHandler(this.btnPunctuation_Click);
			//
			// m_lblCustomCharInstructions
			//
			resources.ApplyResources(this.m_lblCustomCharInstructions, "m_lblCustomCharInstructions");
			this.m_lblCustomCharInstructions.Name = "m_lblCustomCharInstructions";
			//
			// m_lblValidCharacters
			//
			resources.ApplyResources(this.m_lblValidCharacters, "m_lblValidCharacters");
			this.m_lblValidCharacters.Name = "m_lblValidCharacters";
			//
			// lblCustomPUA
			//
			resources.ApplyResources(this.lblCustomPUA, "lblCustomPUA");
			this.lblCustomPUA.Name = "lblCustomPUA";
			//
			// btnValidChars
			//
			this.helpProvider.SetHelpString(this.btnValidChars, resources.GetString("btnValidChars.HelpString"));
			resources.ApplyResources(this.btnValidChars, "btnValidChars");
			this.btnValidChars.Name = "btnValidChars";
			this.helpProvider.SetShowHelp(this.btnValidChars, ((bool)(resources.GetObject("btnValidChars.ShowHelp"))));
			this.btnValidChars.Click += new System.EventHandler(this.btnValidChars_Click);
			//
			// m_btnModifyPUA
			//
			this.helpProvider.SetHelpNavigator(this.m_btnModifyPUA, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("m_btnModifyPUA.HelpNavigator"))));
			resources.ApplyResources(this.m_btnModifyPUA, "m_btnModifyPUA");
			this.m_btnModifyPUA.Name = "m_btnModifyPUA";
			this.helpProvider.SetShowHelp(this.m_btnModifyPUA, ((bool)(resources.GetObject("m_btnModifyPUA.ShowHelp"))));
			this.m_toolTip.SetToolTip(this.m_btnModifyPUA, resources.GetString("m_btnModifyPUA.ToolTip"));
			this.m_btnModifyPUA.Click += new System.EventHandler(this.m_btnModifyPUA_Click);
			//
			// m_btnNewPUA
			//
			this.helpProvider.SetHelpNavigator(this.m_btnNewPUA, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("m_btnNewPUA.HelpNavigator"))));
			resources.ApplyResources(this.m_btnNewPUA, "m_btnNewPUA");
			this.m_btnNewPUA.Name = "m_btnNewPUA";
			this.helpProvider.SetShowHelp(this.m_btnNewPUA, ((bool)(resources.GetObject("m_btnNewPUA.ShowHelp"))));
			this.m_toolTip.SetToolTip(this.m_btnNewPUA, resources.GetString("m_btnNewPUA.ToolTip"));
			this.m_btnNewPUA.Click += new System.EventHandler(this.m_btnNewPUA_Click);
			//
			// m_lstPUACharacters
			//
			resources.ApplyResources(this.m_lstPUACharacters, "m_lstPUACharacters");
			this.m_lstPUACharacters.Name = "m_lstPUACharacters";
			this.helpProvider.SetShowHelp(this.m_lstPUACharacters, ((bool)(resources.GetObject("m_lstPUACharacters.ShowHelp"))));
			this.m_lstPUACharacters.Sorted = true;
			this.m_lstPUACharacters.SelectedIndexChanged += new System.EventHandler(this.m_lstPUACharacters_SelectedIndexChanged);
			this.m_lstPUACharacters.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_lstPUACharacters_ItemCheck);
			//
			// btnModifyEthnologueInfo
			//
			this.helpProvider.SetHelpString(this.btnModifyEthnologueInfo, resources.GetString("btnModifyEthnologueInfo.HelpString"));
			resources.ApplyResources(this.btnModifyEthnologueInfo, "btnModifyEthnologueInfo");
			this.btnModifyEthnologueInfo.Name = "btnModifyEthnologueInfo";
			this.helpProvider.SetShowHelp(this.btnModifyEthnologueInfo, ((bool)(resources.GetObject("btnModifyEthnologueInfo.ShowHelp"))));
			this.btnModifyEthnologueInfo.Click += new System.EventHandler(this.btnModifyEthnologueInfo_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.helpProvider.SetHelpString(this.btnHelp, resources.GetString("btnHelp.HelpString"));
			this.btnHelp.Name = "btnHelp";
			this.helpProvider.SetShowHelp(this.btnHelp, ((bool)(resources.GetObject("btnHelp.ShowHelp"))));
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider.SetHelpString(this.btnCancel, resources.GetString("btnCancel.HelpString"));
			this.btnCancel.Name = "btnCancel";
			this.helpProvider.SetShowHelp(this.btnCancel, ((bool)(resources.GetObject("btnCancel.ShowHelp"))));
			this.btnCancel.Click += new System.EventHandler(this.OnCancel);
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.helpProvider.SetHelpString(this.btnOk, resources.GetString("btnOk.HelpString"));
			this.btnOk.Name = "btnOk";
			this.helpProvider.SetShowHelp(this.btnOk, ((bool)(resources.GetObject("btnOk.ShowHelp"))));
			this.btnOk.Click += new System.EventHandler(this.OnOk);
			//
			// m_listBoxRelatedWSs
			//
			this.m_listBoxRelatedWSs.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_listBoxRelatedWSs.FormattingEnabled = true;
			resources.ApplyResources(this.m_listBoxRelatedWSs, "m_listBoxRelatedWSs");
			this.m_listBoxRelatedWSs.Name = "m_listBoxRelatedWSs";
			this.helpProvider.SetShowHelp(this.m_listBoxRelatedWSs, ((bool)(resources.GetObject("m_listBoxRelatedWSs.ShowHelp"))));
			this.m_listBoxRelatedWSs.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.m_listBoxRelatedWSs_DrawItem);
			this.m_listBoxRelatedWSs.SelectedIndexChanged += new System.EventHandler(this.m_listBoxRelatedWSs_UserSelectionChanged);
			//
			// btnAdd
			//
			resources.ApplyResources(this.btnAdd, "btnAdd");
			this.btnAdd.Name = "btnAdd";
			this.helpProvider.SetShowHelp(this.btnAdd, ((bool)(resources.GetObject("btnAdd.ShowHelp"))));
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// btnCopy
			//
			resources.ApplyResources(this.btnCopy, "btnCopy");
			this.btnCopy.Name = "btnCopy";
			this.helpProvider.SetShowHelp(this.btnCopy, ((bool)(resources.GetObject("btnCopy.ShowHelp"))));
			this.btnCopy.UseVisualStyleBackColor = true;
			this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
			//
			// btnRemove
			//
			resources.ApplyResources(this.btnRemove, "btnRemove");
			this.btnRemove.Name = "btnRemove";
			this.helpProvider.SetShowHelp(this.btnRemove, ((bool)(resources.GetObject("btnRemove.ShowHelp"))));
			this.btnRemove.UseVisualStyleBackColor = true;
			this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
			//
			// groupBox2
			//
			this.groupBox2.Controls.Add(this.btnModifyEthnologueInfo);
			this.groupBox2.Controls.Add(this.m_linkToEthnologue);
			this.groupBox2.Controls.Add(this.m_LanguageCode);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.m_tbLanguageName);
			this.groupBox2.Controls.Add(this.label1);
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Name = "groupBox2";
			this.helpProvider.SetShowHelp(this.groupBox2, ((bool)(resources.GetObject("groupBox2.ShowHelp"))));
			this.groupBox2.TabStop = false;
			//
			// m_linkToEthnologue
			//
			resources.ApplyResources(this.m_linkToEthnologue, "m_linkToEthnologue");
			this.m_linkToEthnologue.Name = "m_linkToEthnologue";
			this.helpProvider.SetShowHelp(this.m_linkToEthnologue, ((bool)(resources.GetObject("m_linkToEthnologue.ShowHelp"))));
			this.m_linkToEthnologue.TabStop = true;
			this.m_linkToEthnologue.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkToEthnologue_LinkClicked);
			//
			// m_LanguageCode
			//
			resources.ApplyResources(this.m_LanguageCode, "m_LanguageCode");
			this.m_LanguageCode.Name = "m_LanguageCode";
			this.helpProvider.SetShowHelp(this.m_LanguageCode, ((bool)(resources.GetObject("m_LanguageCode.ShowHelp"))));
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			this.helpProvider.SetShowHelp(this.label3, ((bool)(resources.GetObject("label3.ShowHelp"))));
			//
			// m_tbLanguageName
			//
			resources.ApplyResources(this.m_tbLanguageName, "m_tbLanguageName");
			this.m_tbLanguageName.Name = "m_tbLanguageName";
			this.helpProvider.SetShowHelp(this.m_tbLanguageName, ((bool)(resources.GetObject("m_tbLanguageName.ShowHelp"))));
			this.m_tbLanguageName.TextChanged += new System.EventHandler(this.m_tbLanguageName_TextChanged);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			this.helpProvider.SetShowHelp(this.label1, ((bool)(resources.GetObject("label1.ShowHelp"))));
			//
			// m_writingSystemsFor
			//
			resources.ApplyResources(this.m_writingSystemsFor, "m_writingSystemsFor");
			this.m_writingSystemsFor.Name = "m_writingSystemsFor";
			this.helpProvider.SetShowHelp(this.m_writingSystemsFor, ((bool)(resources.GetObject("m_writingSystemsFor.ShowHelp"))));
			//
			// lblHiddenWss
			//
			resources.ApplyResources(this.lblHiddenWss, "lblHiddenWss");
			this.lblHiddenWss.Name = "lblHiddenWss";
			this.helpProvider.SetShowHelp(this.lblHiddenWss, ((bool)(resources.GetObject("lblHiddenWss.ShowHelp"))));
			//
			// WritingSystemPropertiesDialog
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.lblHiddenWss);
			this.Controls.Add(this.m_writingSystemsFor);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.btnRemove);
			this.Controls.Add(this.btnCopy);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.m_listBoxRelatedWSs);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.tabControl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WritingSystemPropertiesDialog";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Activated += new System.EventHandler(this.OnGetFocus);
			this.tabControl.ResumeLayout(false);
			this.tpGeneral.ResumeLayout(false);
			this.tpGeneral.PerformLayout();
			this.gbDirection.ResumeLayout(false);
			this.tpFonts.ResumeLayout(false);
			this.tpKeyboard.ResumeLayout(false);
			this.tpKeyboard.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxTestWs)).EndInit();
			this.tpConverters.ResumeLayout(false);
			this.tpConverters.PerformLayout();
			this.tpSorting.ResumeLayout(false);
			this.tpSorting.PerformLayout();
			this.tpPUACharacters.ResumeLayout(false);
			this.tpPUACharacters.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a hash table from converter name to writing system name for converters
		/// that are currently in use by some writing system. (This is a needed argument
		/// for initializeing an AddCnvtrDlg, so that it can avoid deleting converters that
		/// are currently in use by writing systems.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Set<string> ConvertersInUse(ILgWritingSystemFactory wsf)
		{
			Set<string> wsInUse = new Set<string>();
			// Make a hash of the writing systems currently in use.
			IWritingSystem currentWS;
			int cws = wsf.NumberOfWs;

			using (ArrayPtr ptr = MarshalEx.ArrayToNative(cws, typeof(int)))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vws = (int[])MarshalEx.NativeToArray(ptr, cws, typeof(int));

				for (int iws = 0; iws < cws; iws++)
				{
					if (vws[iws] == 0)
						continue;
					currentWS = wsf.get_EngineOrNull(vws[iws]);
					if (currentWS == null)
						continue;
					string legMapping = currentWS.LegacyMapping;
					if (legMapping == null)
						continue;
					wsInUse.Add(legMapping);
				}
			}
			return wsInUse;
		}

		#region Button click handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show New Converter Dialog.  Copied to FwCoreDlgs.WritingSystemWizard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnEncodingConverter_Click(object sender, EventArgs e)
		{
			ILgWritingSystemFactoryBuilder wsFactBuilder = LgWritingSystemFactoryBuilderClass.Create();
			ILgWritingSystemFactory wsFact = wsFactBuilder.GetWritingSystemFactoryNew(m_strServer, m_strDatabase, null);
			Set<string> wsInUse = ConvertersInUse(wsFact);
			wsFact.Shutdown(); // Without this we leave a OleDbCommand pointer open.
			Marshal.ReleaseComObject(wsFactBuilder);
			wsFactBuilder = null;
			Marshal.ReleaseComObject(wsFact);
			wsFact = null;

			try
			{
				string prevEC = cbEncodingConverter.Text;
				using (AddCnvtrDlg dlg = new AddCnvtrDlg(m_helpTopicProvider, null,
					cbEncodingConverter.Text, null, false))
				{
					dlg.ShowDialog();

					// Reload the converter list in the combo to reflect the changes.
					LoadAvailableConverters();

					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !String.IsNullOrEmpty(dlg.SelectedConverter))
						cbEncodingConverter.SelectedItem = dlg.SelectedConverter;
					else if (cbEncodingConverter.Items.Count > 0)
						cbEncodingConverter.SelectedItem = prevEC; // preserve selection if possible
				}
			}
			catch (Exception ex)
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder(ex.Message);
				sb.Append(System.Environment.NewLine);
				sb.Append(FwCoreDlgs.kstidErrorAccessingEncConverters);
				MessageBox.Show(this, sb.ToString(),
					ResourceHelper.GetResourceString("kstidCannotModifyWS"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the first cch characters of the input (or as many as are available).
		/// </summary>
		/// <param name="input"></param>
		/// <param name="cch"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static string LeftSubstring(string input, int cch)
		{
			return input.Substring(0, Math.Min(input.Length, cch));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lanches the Dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnModifyEthnologueInfo_Click(object sender, System.EventArgs e)
		{
			if (!CheckOkToChangeContext())
				return;
			SaveCurrentLangDef();
			using (LanguageSelectionDlg dlg = new LanguageSelectionDlg())
			{
				dlg.SetDialogProperties(m_helpTopicProvider);
				dlg.StartedInModifyState = true;	// started in modify state

				string origLangName = m_LanguageName;
				string origEthCode = null;
				dlg.LangName = origLangName;
				if (m_LanguageCode.Text != FwCoreDlgs.kstidNone)
					origEthCode = m_LanguageCode.Text;
				dlg.EthCode = origEthCode;

				if (CallShowDialog(dlg) != DialogResult.OK)
					return;
				CreateTemporaryLanguageDefs();
				// If the language name changes, change the abbreviation as well.  See LT-8652.
				string oldLangName = m_LanguageName;
				SetupDialogFromLanguageSelectionDlg(dlg);
				if (m_LanguageName != oldLangName)
				{
					int len = Math.Min(3, m_LanguageName.Length);
					m_ShortWsName.Text = m_LanguageName.Substring(0, len);
				}
				if (!CheckIcuNames())
				{
					// something wasn't right about the name changes, so revert to previous state.
					RestoreFinalLangDefs();
					btnModifyEthnologueInfo.Focus();
					return;
				}
				UpdateDialogWithChangesToLanguageName();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateTemporaryLanguageDefs()
		{
			m_listFinalLangDefsBackup = m_listFinalLangDefs;
			List<LanguageDefinition> listFinalLangDefs = new List<LanguageDefinition>();
			foreach (LanguageDefinition langDef in m_listFinalLangDefsBackup)
			{
				LanguageDefinition clone = (langDef as ICloneable).Clone() as LanguageDefinition;
				listFinalLangDefs.Add(clone);
				// new language definitions must match the corresponding index in m_listOrigLangDefs
				if (IsNew(langDef))
				{
					int index = m_listOrigLangDefs.IndexOf(langDef);
					m_listOrigLangDefs[index] = clone;
				}
			}
			m_listFinalLangDefs = listFinalLangDefs;

			EstablishCurrentLangDefFromSelectedIndex();
			SetupDialogFromCurrentLanguageDefinition();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RestoreFinalLangDefs()
		{
			// first replace new language defns with those in the backed up list.
			List<LanguageDefinition> newTempLanguageDefns = NewlyAddedLanguageDefns();
			foreach (LanguageDefinition tempLangDef in newTempLanguageDefns)
			{
				int index = m_listOrigLangDefs.IndexOf(tempLangDef);
				m_listOrigLangDefs[index] = m_listFinalLangDefsBackup[index];
			}
			m_listFinalLangDefs = m_listFinalLangDefsBackup;
			m_listFinalLangDefsBackup = null;

			EstablishCurrentLangDefFromSelectedIndex();
			SetupDialogFromCurrentLanguageDefinition();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual DialogResult CallShowDialog(LanguageSelectionDlg dlg)
		{
			return dlg.ShowDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// write out the new ethnologue codes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupDialogFromLanguageSelectionDlg(LanguageSelectionDlg dlg)
		{
			foreach (LanguageDefinition langDef in m_listFinalLangDefs)
			{
				langDef.SetEthnologueCode(dlg.EthCode, dlg.LangName.Trim());
				langDef.LocaleName = dlg.LangName.Trim();
			}
			SetupDialogFromCurrentLanguageDefinition();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Set_regionVariantControl(LanguageDefinition langDef)
		{
			m_fUserChangedVariantControl = false;
			m_regionVariantControl.LangDef = langDef;
			m_fUserChangedVariantControl = true;

			m_FullCode.Text = langDef.CurrentFullLocale();

			m_regionVariantControl.PropDlg = true;

			LoadShortWsNameFromCurrentLanguageDefn();
			rbLeftToRight.Checked = !langDef.WritingSystem.RightToLeft;
			rbRightToLeft.Checked = langDef.WritingSystem.RightToLeft;
		}

		/// <summary>
		/// When changing the text of m_tbLanguageName we need to set a flag
		/// so that the TextChanged event handler will return without performing
		/// any changes.
		/// </summary>
		/// <param name="languageName"></param>
		private void Set_tbLanguageName(string languageName)
		{
			m_fUserChangedText = false;
			m_tbLanguageName.Text = languageName;
			m_fUserChangedText = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetLanguageCodeLabels(String str)
		{
			m_LanguageCode.Text = str;
			m_linkToEthnologue.Text = String.Format(FwCoreDlgs.ksWSPropEthnologueEntryFor, str);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetLanguageNameLabels()
		{
			LoadShortWsNameFromCurrentLanguageDefn();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetFullNameLabels(string fullName)
		{
			m_FullNameGeneral = fullName;
			SetLabelParams(m_lblCustomCharInstructions, fullName);
			SetLabelParams(m_lblCustomCharCondition2, Icu.UnicodeVersion);
			SetLabelParams(m_lblValidCharacters, fullName);
			SetLabelParams(m_lblPunctuation, fullName);
			SetLabelParams(m_lblEncodingConverter, fullName);
			SetLabelParams(m_lblKeyboardInstruction, fullName);
			SetLabelParams(m_lblKeyboardTestInstr, fullName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void SetLabelParams(Label lbl, params string[] parms)
		{
			lbl.Text = String.Format((string)lbl.Tag, parms);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_regionVariantControlChanged(object sender, System.EventArgs e)
		{
			if (!m_fUserChangedVariantControl)
				return;
			//This next assignment updates the DisplayName so it reflects the changes
			//made in the regionVariantControl
			m_FullCode.Text = m_regionVariantControl.ConstructIcuLocaleFromAbbreviations();
			SetFullNameLabels(m_langDefCurrent.DisplayName);
			m_ListBoxRelatedWSsSetDisplayname(m_langDefCurrentIndex, m_langDefCurrent.DisplayName);
			// make sure IcuRules reflect latest icuCode state.
			SetupSortTab(m_langDefCurrent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked the OK button - persist the changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void OnOk(object sender, System.EventArgs e)
		{
			// Quick-and-dirty workaround for LT-4401 is to release root.res here. Better way
			// is to implement the call back in LanguageDefintion.
			//m_langDef.ReleaseRootRb();

			//Make sure the dialog does not close if we return early.
			DialogResult = DialogResult.None;
			using (new WaitCursor(this))
			{
				if (!CheckOkToChangeContext())
					return;

				//First of all we probably need to save the current lang def settings into
				//m_listFinalLangDefs
				SaveCurrentLangDef();

				//now compare each LangDef with the original one and see if anything needs to be
				//saved in the Database.
				bool fIcuLocaleChanged = false;
				bool fIcuRulesChanged = false;
				bool fPuaDefinitionsChanged = false;
				bool fValidCharsChanged = false;
				bool fSpellDictionaryChanged = false;
				for (int i = 0; i < m_listFinalLangDefs.Count; i++)
				{
					LanguageDefinition originalLangDef = m_listOrigLangDefs[i];
					LanguageDefinition finalLangDef = m_listFinalLangDefs[i];
					// skip any language defs marked for merging, otherwise we'll overwrite
					// the original ws in the db, with the new ws information, before the merge happens.
					if (finalLangDef.HasPendingMerge())
						continue;

					fValidCharsChanged = (originalLangDef.ValidChars != finalLangDef.ValidChars);
					fPuaDefinitionsChanged = !ComparePuaDefinitions(originalLangDef, finalLangDef);
					fIcuLocaleChanged = !CompareIcuLocale(originalLangDef, finalLangDef);
					fSpellDictionaryChanged = !CompareSpellDictionary(originalLangDef, finalLangDef);
					// (EricP) Not sure if CompareIcuRules is necessary, since SaveWritingSystem does
					// its own comparison internally. But we'll keep it to maintain the same logic
					// as before.
					fIcuRulesChanged = !CompareIcuRules(originalLangDef, finalLangDef);
					m_SortModified |= fIcuRulesChanged;

					try
					{
						// Let go of ICU resource memory mapping so we can update ICU files.
						finalLangDef.ReleaseRootRb();
						// save changes to the XML file
						Serialize(finalLangDef);
						string icuLocaleOfWsToWriteTo = originalLangDef.IcuLocaleOriginal;
						if (IsNew(finalLangDef))
							icuLocaleOfWsToWriteTo = finalLangDef.CurrentFullLocale();

						// Not sure why this is necessary, but if it's left out and the user
						// adds a new writing system, all the valid characters for writing
						// systems that existed before get lost.
						finalLangDef.WritingSystem.ValidChars = finalLangDef.ValidChars;

						m_fChanged |= finalLangDef.SaveWritingSystem(icuLocaleOfWsToWriteTo,
							fPuaDefinitionsChanged || fIcuRulesChanged || fValidCharsChanged);
						m_fChanged |= fIcuLocaleChanged;
						m_fChanged |= fSpellDictionaryChanged;
					}
					catch (Exception ex)
					{
						// The exception message is likely something like this:
						// Access to the path "C:\Program Files\FieldWorks\languages\fr.xml" is denied.
						System.Text.StringBuilder sb = new System.Text.StringBuilder(ex.Message);
						sb.Append(System.Environment.NewLine);
						sb.Append(ResourceHelper.GetResourceString("kstidErrorModifyingWS"));
						MessageBox.Show(this, sb.ToString(),
							ResourceHelper.GetResourceString("kstidCannotModifyWS"));
					}
				}
				List<LanguageDefinition> newlyAdded = NewlyAddedLanguageDefns();
				if (newlyAdded.Count > 0)
					m_cache.ResetLanguageEncodings();
				DoPendingWsMerges();
				DialogResult = DialogResult.OK;
				CallClose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CallClose()
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests can override this to specify a temp location to store the output.
		/// </summary>
		/// <param name="finalLangDef"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Serialize(LanguageDefinition finalLangDef)
		{
			finalLangDef.Serialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns>true if the two LanguageDefinitions' PuaDefinitions are the same</returns>
		/// ------------------------------------------------------------------------------------
		private static bool ComparePuaDefinitions(LanguageDefinition originalLangDef, LanguageDefinition finalLangDef)
		{
			//determine if originalLangDef.PuaDefinitions==null before this....
			if (originalLangDef.PuaDefinitions == null && finalLangDef.PuaDefinitions == null)
			{
				return true;
			}
			else if ((originalLangDef.PuaDefinitions == null && finalLangDef.PuaDefinitions != null) ||
				(originalLangDef.PuaDefinitions != null && finalLangDef.PuaDefinitions == null))
			{
				return false;
			}
			//it is safe to perform further checks
			else if (originalLangDef.PuaDefinitions.Length != finalLangDef.PuaDefinitions.Length)
			{
				return false;
			}
			else
			{
				for (int j = 0; j < originalLangDef.PuaDefinitions.Length; j++)
				{
					if (!originalLangDef.PuaDefinitions[j].Equals(finalLangDef.PuaDefinitions[j]))
						return false;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="originalLangDef"></param>
		/// <param name="finalLangDef"></param>
		/// <returns>true if the two LanguageDefinitions' IcuLocale are the same</returns>
		/// ------------------------------------------------------------------------------------
		private static bool CompareIcuLocale(LanguageDefinition originalLangDef, LanguageDefinition finalLangDef)
		{
			string oldIcuLocale = originalLangDef.WritingSystem.IcuLocale;
			string newIcuLocale = finalLangDef.WritingSystem.IcuLocale;
			//I suppose this is what we really want to check for the name change????
			return oldIcuLocale.Equals(newIcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="originalLangDef"></param>
		/// <param name="finalLangDef"></param>
		/// <returns>true if the two LanguageDefinitions' SpellCheckDictionary are the same</returns>
		/// ------------------------------------------------------------------------------------
		private static bool CompareSpellDictionary(LanguageDefinition originalLangDef, LanguageDefinition finalLangDef)
		{
			string oldSpellDictionary = originalLangDef.WritingSystem.SpellCheckDictionary;
			string newSpellDictionary = finalLangDef.WritingSystem.SpellCheckDictionary;
			//I suppose this is what we really want to check for the name change????
			return oldSpellDictionary.Equals(newSpellDictionary);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool CompareIcuRules(LanguageDefinition originalLangDef, LanguageDefinition finalLangDef)
		{
			return GetIcuRules(originalLangDef) == GetIcuRules(finalLangDef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="langDef"></param>
		/// <returns>icuRules string for first collation, or empty string if no icu rules were found for given language definition</returns>
		/// ------------------------------------------------------------------------------------
		protected static string GetIcuRules(LanguageDefinition langDef)
		{
			string icuRules = "";
			if (langDef.WritingSystem.CollationCount > 0)
				icuRules = langDef.WritingSystem.get_Collation(0).IcuRules;
			if (icuRules == null)
				icuRules = "";
			return icuRules;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked the Cancel button - clean up and quit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void OnCancel(object sender, System.EventArgs e)
		{
			// Let go of ICU resource memory mapping so we can update ICU files.
			m_langDefCurrent.ReleaseRootRb();
			DialogResult = DialogResult.Cancel;
			return;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the language definitions whose writing systems are marked for us to merge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal protected List<LanguageDefinition> GetPendingWsMerges()
		{
			List<LanguageDefinition> pendingMerges = new List<LanguageDefinition>();
			foreach (LanguageDefinition langDef in this.FinalLanguageDefns)
			{
				if (langDef.HasPendingMerge())
					pendingMerges.Add(langDef);
			}
			return pendingMerges;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the language definitions whose writing systems are marked for us to
		/// load from an existing xml language definition in order to overwrite
		/// the original ws data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal protected List<LanguageDefinition> GetPendingWsOverwrites()
		{
			List<LanguageDefinition> pendingOverwrites = new List<LanguageDefinition>();
			foreach (LanguageDefinition langDef in this.FinalLanguageDefns)
			{
				if (langDef.HasPendingOverwrite())
					pendingOverwrites.Add(langDef);
			}
			return pendingOverwrites;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do any pending ws merges.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal protected void DoPendingWsMerges()
		{
			if (!IsSetupForMergingWss)
				return;
			List<LanguageDefinition> pendingMerges = GetPendingWsMerges();
			if (pendingMerges.Count > 0)
			{
				// collect all the information needed for the merges.
				Dictionary<int, int> oldToNewWsMap = new Dictionary<int, int>();
				Dictionary<int, string> idToName = new Dictionary<int, string>();
				foreach (LanguageDefinition langDef in pendingMerges)
				{
					ILgWritingSystemFactory lgwsf = m_langDefCurrent.WritingSystem.WritingSystemFactory;
					IWritingSystem lgWsOld = lgwsf.get_Engine(langDef.IcuLocaleTarget);
					int wsIdOld = lgwsf.GetWsFromStr(lgWsOld.IcuLocale);
					string oldWsName = lgWsOld.get_UiName(m_displayWs);
					int wsIdNew = langDef.FindWritingSystemInDb();
					if (wsIdNew == 0)
						throw new ArgumentException("Expected language definition to have a valid ws id.");
					string newWsName = lgwsf.get_EngineOrNull(wsIdNew).get_UiName(m_displayWs);
					oldToNewWsMap[wsIdOld] = wsIdNew;
					idToName[wsIdOld] = oldWsName;
					idToName[wsIdNew] = newWsName;

					// remove this from Pending Merge list.
					langDef.IcuLocaleTarget = "";
				}

				CloseDbAndWindows();
				// do all any pending merges.
				IFwDbMergeWrtSys dmws = CreateFwDbMergeWrtSysClass();
				foreach (int wsIdOld in oldToNewWsMap.Keys)
				{
					int wsIdNew = oldToNewWsMap[wsIdOld];
					DoPendingWsMerge(dmws, wsIdOld, idToName[wsIdOld], wsIdNew, idToName[wsIdNew]);
				}
				CreateNewMainWnd();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For some reason this crashes in tests, so override it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual IFwDbMergeWrtSys CreateFwDbMergeWrtSysClass()
		{
			IFwDbMergeWrtSys dmws = FwDbMergeWrtSysClass.Create();
			dmws.Initialize(m_fwt, m_strServer, m_strDatabase, m_strmLog, m_hvoProj, m_hvoRootObj, m_wsUser);
			return dmws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// close the windows associated with our database.
		/// </summary>
		protected virtual void CloseDbAndWindows()
		{
			if (OnAboutToMergeWritingSystems != null)
				OnAboutToMergeWritingSystems(this, EventArgs.Empty);
			m_fwt.CloseDbAndWindows(m_strServer, m_strDatabase, false);
		}

		/// <summary>
		/// create a new window associated with our database.
		/// </summary>
		protected virtual void CreateNewMainWnd()
		{
			int newHandleId;
			m_fwt.NewMainWnd(m_strServer, m_strDatabase, m_hvoProj, m_hvoRootObj, m_wsUser, 0, 0, out newHandleId);
		}


		/// <summary>
		/// performs the ws merge
		/// </summary>
		/// <param name="dmws"></param>
		/// <param name="wsIdOld"></param>
		/// <param name="oldWsName"></param>
		/// <param name="wsIdNew"></param>
		/// <param name="newWsName"></param>
		protected virtual void DoPendingWsMerge(IFwDbMergeWrtSys dmws, int wsIdOld, string oldWsName,
			int wsIdNew, string newWsName)
		{
			dmws.Process(wsIdOld, oldWsName, wsIdNew, newWsName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the appropriate Help file for selected tab (Name or Attributes).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			string helpTopicKey = null;

			switch (tabControl.SelectedIndex)
			{
				case kWsGeneral:
					helpTopicKey = "khtpWsGeneral";
					break;
				case kWsFonts:
					helpTopicKey = "khtpWsFonts";
					break;
				case kWsKeyboard:
					helpTopicKey = "khtpWsKeyboard";
					break;
				case kWsConverters:
					helpTopicKey = "khtpWsConverters";
					break;
				case kWsSorting:
					helpTopicKey = "khtpWsSorting";
					break;
				case kWsPUACharacters:
					helpTopicKey = "khtpWsPUACharacters";
					break;
			}
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Modifies the selected item in the list of PUA characters using the <c>PUACharacterDlg</c>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnModifyPUA_Click(object sender, System.EventArgs e)
		{
			PuaListItem puaLstItem = (PuaListItem)m_lstPUACharacters.SelectedItem;

			// If nothing is selected, merely ignore
			if (puaLstItem != null)
				DisplayPUACharacterDlg(puaLstItem.PUAChar, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new PUACharacter to the list.
		/// The new PUACharacter is filled with data in the PUACharacterDlg.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnNewPUA_Click(object sender, System.EventArgs e)
		{
			// Set the inital values to match the unicode standard.
			DisplayPUACharacterDlg(PUACharacter.UnicodeDefault, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnValidChars control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnValidChars_Click(object sender, EventArgs e)
		{
			// Save the current language definition so the checked PUA
			// characters show up in the valid characters dialog.
			SaveCurrentLangDef();

			using (ValidCharactersDlg dlg = new ValidCharactersDlg(m_cache, m_helpTopicProvider,
				m_langDefCurrent, m_FullNameGeneral))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
					SetupPUAList();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnPunctuation control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnPunctuation_Click(object sender, EventArgs e)
		{
			using (PunctuationDlg dlg = new PunctuationDlg(m_cache, m_helpTopicProvider,
				m_langDefCurrent, m_FullNameGeneral, StandardCheckIds.kguidMatchedPairs))
			{
				dlg.ShowDialog(this);
			}
		}
		#endregion

		#region Other event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnSimilarWs_LocaleSelected(object sender, EventArgs e)
		{
			// TE-7767 says not to persist this locale id.  It's enough to persist the loaded
			// rules (if any).
			//m_langDefCurrent.BaseLocale = btnSimilarWs.SelectedLocaleId;
			// But we do want to use the newly selected locale!  (TE-8450, LT-10190)
			m_langDefCurrent.BaseLocale = null;
			StoreInheritedCollation(btnSimilarWs.SelectedLocaleId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StoreInheritedCollation(string baseLocale)
		{
			ICollation coll = UseOrCreateCollation(m_langDefCurrent);
			string sRules = coll.IcuRules;
			if (sRules != null && sRules.Length > 0)
			{
				// "Overwrite existing collation rules?";
				DialogResult res = MessageBox.Show(this, FwCoreDlgs.kstidOverwriteRules,
					FwCoreDlgs.kstidOverwriteRulesCaption, MessageBoxButtons.YesNo);
				if (res == DialogResult.No)
					return;
			}
			if (baseLocale == null)
				baseLocale = "";
			coll.LoadIcuRules(baseLocale);
			if (txtIcuRules.Enabled)
				txtIcuRules.Text = coll.IcuRules;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the full code changes, we may need to disable the base locale button,
		/// which is allowed only if the locale is a custom one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupICURulesAndBtnSimilarWs()
		{
			btnSimilarWs.SetupForSimilarLocale(m_langDefCurrent.CurrentFullLocale(),
				m_langDefCurrent.RootRb);
			if (!btnSimilarWs.Enabled)
			{
				// The contents of the ICU Rules textbox should have the same text as the button.
				// See TE-4122.
				txtIcuRules.Text = btnSimilarWs.Text;
			}
			else if (txtIcuRules.Enabled != btnSimilarWs.Enabled)
			{
				// If the ICU Rules textbox gets enabled we want to delete the "Built-In" text.
				txtIcuRules.Text = string.Empty;
			}

			// The ICU Rules textbox should be disabled for built-in writing systems.
			txtIcuRules.Enabled = btnSimilarWs.Enabled;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the short ws name when the data changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_ShortWsName_TextChanged(object sender, System.EventArgs e)
		{
			((ILanguageDefinition)m_langDefCurrent).WritingSystem.set_Abbr(m_displayWs,
				m_ShortWsName.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the rbLeftToRight (radio button for left-to-right)
		/// control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void rbLeftToRight_CheckedChanged(object sender, System.EventArgs e)
		{
			m_langDefCurrent.WritingSystem.RightToLeft = !rbLeftToRight.Checked;
			m_fNewRendering = true;		// the writing system rendering has changed.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_lstPUACharacters control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstPUACharacters_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_btnModifyPUA.Enabled = m_lstPUACharacters.SelectedIndex >= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ItemCheck event of the m_lstPUACharacters control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.ItemCheckEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstPUACharacters_ItemCheck(object sender,
			System.Windows.Forms.ItemCheckEventArgs e)
		{
			// Get the selected character

			PuaListItem item = (PuaListItem)m_lstPUACharacters.Items[e.Index];
			//if (item.IsNew)
			//{
			//    if (e.NewValue == CheckState.Checked)
			//    {
			//        MessageBox.Show(this, "Unfortunately, this newly added custom character has not yet been saved and installed. If you're happy with the changes you have made to the writing system properties, save them and then return to this dialog to add this as a valid character. Sorry!", Application.ProductName);
			//        e.NewValue = CheckState.Unchecked;
			//    }
			//    return;
			//}

			// Mark the pua character as modified, updating the Language Definition as necessary
			MarkAsModified(item.PUAChar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void tabControl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			//If we were switching away from the Sorting tab then this check
			//will ensure we return to it and force the user to correct it
			//if there is a problem with it.
			//First ensure we have not just switched to this tab to prevent an infinite loop
			//Then we want to know if we are swithing away from this tab.
			//lastly see if the sorting string is valid.
			e.Cancel = !CheckOkToChangeContext();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// To be valid, the language name and the language portion of the ICULocale (up to first _)
		/// needs to checked against what's currently in ICU. If either part is currently in ICU
		/// but with a different corresponding part, then we need to warn the user and require them
		/// to change something.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool CheckIcuNames()
		{
			// If we are changing IcuLocale, we need to handle various situations.
			string caption = FwCoreDlgs.kstidWspLabel;
			ILgWritingSystemFactory qwsf = m_langDefCurrent.WritingSystem.WritingSystemFactory;

			for (int i = 0; i < m_listFinalLangDefs.Count; i++)
			{
				LanguageDefinition originalLangDef = m_listOrigLangDefs[i];
				LanguageDefinition finalLangDef = m_listFinalLangDefs[i];
				string origIcuLocale = originalLangDef.WritingSystem.IcuLocale;

				// Check if the IcuLocale has changed
				//this changes if the VariantName or Region have been changed.
				if (finalLangDef.HasChangedIcuLocale &&
					!finalLangDef.HasPendingMerge() &&
					!finalLangDef.HasPendingOverwrite())
				{
					// We can't let anyone change the user writing system (or "English"). Too many strings depend on
					// this, and we'd get numerous crashes and terrible behavior if it was changed.
					string strUserWs = qwsf.GetStrFromWs(qwsf.UserWs);
					if (origIcuLocale == strUserWs || origIcuLocale == "en")
					{
						ShowMsgCantChangeUserWs(origIcuLocale);
						return false;
					}

					// Catch case where we are going to overwrite an existing writing system in the Db.
					int wsExisting = finalLangDef.FindWritingSystemInDb();
					if (wsExisting != 0)
					{
						string strExisting = qwsf.get_EngineOrNull(wsExisting).get_UiName(m_displayWs);

						if (!IsSetupForMergingWss)
						{
							ShowMsgTooBadWsAlreadyInDb(finalLangDef, origIcuLocale, strExisting);
							return false;
						}
						else
						{
							int wsIdOld = qwsf.GetWsFromStr(origIcuLocale);
							IWritingSystem wsOld = qwsf.get_EngineOrNull(wsIdOld);

							DialogResult dr = ShowMsgWsAlreadyInDb(finalLangDef, origIcuLocale, strExisting, wsOld);
							if (dr == DialogResult.OK)
							{
								finalLangDef.IcuLocaleTarget = origIcuLocale;
							}
							else
							{
								return false;
							}
						}
					}
					else
					{
						// Catch case where we are going to overwrite an existing LD.xml file.
						// also check that we've changed the value of the locale.
						if (IsLocaleInLanguagesDir(finalLangDef))
						{
							DialogResult dr = ShowMsgLocaleAlreadyInLanguages(finalLangDef);
							// If the user cancels, we don't leave the dialog.
							if (dr == DialogResult.Yes)
							{
								// Mark this as needing to be reloaded from the existing LD.xml file
								// overwriting any current changes to the language definition before we
								// overwrite the original writing system.
								finalLangDef.IcuLocaleTarget = origIcuLocale;
							}
							else
							{
								return false;
							}
						}
					}

				}
			}

			// Regenerate any LDFs marked for overwriting the original LDF.
			foreach (LanguageDefinition langDef in GetPendingWsOverwrites())
			{
				if (langDef.WritingSystem.IcuLocale == langDef.IcuLocaleOriginal)
				{
					// if we've already regnerated the LDF, then just continue.
					// we don't need to do this more than once.
					continue;
				}
				// since the user approved all the overwrites we can now
				// reload each language definition from pre-existing LD.xml file.
				// Note: Reloading now (without closing the dialog),
				// allows the user to make further edits before hitting OK.
				ReloadLangDefFromExistingLangDefInLanguageDir(langDef);
				// When the user hits OK, we will then write it out to the
				// database, overwriting the original writing system.
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LanguageDefinition ReloadLangDefFromExistingLangDefInLanguageDir(LanguageDefinition langDef)
		{
			ILgWritingSystemFactory qwsf = langDef.WritingSystem.WritingSystemFactory;
			Debug.Assert(langDef.IcuLocaleOriginal != langDef.WritingSystem.IcuLocale);
			string origIcuLocale = langDef.IcuLocaleOriginal;
			string locale = langDef.WritingSystem.IcuLocale;
			langDef.ReleaseRootRb(); // Ensure this is clear before setting to null.
			int indexToReplace = FinalLanguageDefns.IndexOf(langDef);
			langDef = null;
			langDef = CreateLanguageDefFromXml(qwsf, locale);
			Debug.Assert(langDef.IcuLocaleOriginal == langDef.WritingSystem.IcuLocale);
			langDef.IcuLocaleTarget = origIcuLocale;
			// replace the existing language definition with this one.
			Debug.Assert(langDef != null);
			FinalLanguageDefns.RemoveAt(indexToReplace);
			FinalLanguageDefns.Insert(indexToReplace, langDef);
			if (indexToReplace == m_langDefCurrentIndex)
			{
				EstablishCurrentLangDefFromSelectedIndex();
				SetupDialogFromCurrentLanguageDefinition();
			}
			return langDef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsLocaleInLanguagesDir(LanguageDefinition finalLangDef)
		{
			return finalLangDef.IsLocaleInLanguagesDir();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the dialog can handle merging Wss if the users wants to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsSetupForMergingWss
		{
			get { return m_fwt != null && m_hvoProj != 0 && m_hvoRootObj != 0 && m_wsUser != 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual DialogResult ShowMsgLocaleAlreadyInLanguages(LanguageDefinition finalLangDef)
		{
			string msg = string.Format(FwCoreDlgs.kstidLocaleAlreadyInLanguages,
									 finalLangDef.DisplayName, finalLangDef.WritingSystem.IcuLocale, Environment.NewLine);
			DialogResult dr = MessageBox.Show(msg, FwCoreDlgs.kstidWspLabel, MessageBoxButtons.YesNo,
				MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			return dr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual DialogResult ShowMsgWsAlreadyInDb(LanguageDefinition finalLangDef, string origIcuLocale, string strExisting, IWritingSystem wsOld)
		{
			string msg = string.Format(FwCoreDlgs.kstidWsAlreadyInDb,
									 strExisting, finalLangDef.WritingSystem.IcuLocale,
									 origIcuLocale, wsOld.LanguageName, Environment.NewLine);
			using (MergeToExistingWsDlg dlg = new MergeToExistingWsDlg(m_helpTopicProvider))
			{
				dlg.Initialize(msg, m_app);
				dlg.ShowDialog(this);
				return dlg.DialogResult;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowMsgCantChangeUserWs(string origIcuLocale)
		{
			string msg = string.Format(FwCoreDlgs.kstidCantChangeUserWs, origIcuLocale);
			MessageBox.Show(msg, FwCoreDlgs.kstidWspLabel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowMsgTooBadWsAlreadyInDb(LanguageDefinition finalLangDef, string origIcuLocale, string strExisting)
		{
			string msg = string.Format(FwCoreDlgs.kstidTooBadWsAlreadyInDb,
				strExisting, finalLangDef.WritingSystem.IcuLocale, origIcuLocale);

			MessageBox.Show(msg, FwCoreDlgs.kstidWspLabel, MessageBoxButtons.OK);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual LanguageDefinition CreateLanguageDefFromXml(ILgWritingSystemFactory qwsf,
			string locale)
		{
			return m_LanguageDefinitionFactory.InitializeFromXml(qwsf, locale) as LanguageDefinition;
		}

		private bool m_fSkipCheckOkToChangeContext = false;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// some user actions (e.g. Add ("Define New...") involve switching tabs to a context
		/// (e.g. General tab)that will allow the user the opportunity to make it Ok to change context.
		/// In that case, m_fSkipCheckOkToChangeContext should be set to true, so that switching tabs
		/// does not prematurely detect we're in an invalid state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool CheckOkToChangeContext()
		{
			if (m_fSkipCheckOkToChangeContext)
				return true;
			bool fOkToChangeContext = true;
			// Check the validity of the current tab control.
			switch (tabControl.SelectedIndex)
			{
				default:
					break;
				case kWsGeneral:
					fOkToChangeContext = m_regionVariantControl.CheckValid() && CheckUniqueWs();
					if (!fOkToChangeContext)
					{
						// select Variant control to give the user a place to consider changing.
						m_regionVariantControl.Focus();
					}
					break;
				case kWsSorting:
					fOkToChangeContext = CheckIfSortingIsOK();
					break;
				case kWsConverters:
					fOkToChangeContext = CheckEncodingConverter();
					break;
			}
			// Check the validity of the Language name and icuLocale combination.
			if (fOkToChangeContext && !CheckIcuNames())
			{
				fOkToChangeContext = false;
			}
			return fOkToChangeContext;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool CheckEncodingConverter()
		{
			bool fOkToChangeContext = true;
			if (cbEncodingConverter.SelectedIndex >= 0)
			{
				string str = cbEncodingConverter.Text;
				if (str == FwCoreDlgs.kstidNone)
					str = null;
				if (str != null && str.Contains(FwCoreDlgs.kstidNotInstalled))
				{
					fOkToChangeContext = false;
					MessageBox.Show(this, FwCoreDlgs.kstidEncoderNotAvailable);
				}
			}
			return fOkToChangeContext;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the tabControl control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void tabControl_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			switch (tabControl.SelectedIndex)
			{
				case kWsPUACharacters:
					// Check all the characters that are in the LDF
					SetupPUAList();
					break;

				case kWsSorting:
					txtIcuRules.Font = new Font(m_defaultFontsControl.DefaultNormalFont, 12.0f);
					break;
			}
		}

		#endregion

		#region Misc. stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether the specified locale is a custom one the user is allowed to modify.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsCustomLocale(string localeId)
		{
			ILgIcuResourceBundle rbCustom = m_langDefCurrent.RootRb.get_GetSubsection("Custom");
			if (rbCustom == null)
				return false; // No Custom section yet.
			try
			{
				string itemName = null;
				return RbItemWithLocaleIdOrNameExists(ref localeId, ref itemName, rbCustom, "LocalesAdded");
			}
			finally
			{
				if (rbCustom != null)
					System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCustom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool RbItemWithLocaleIdOrNameExists(ref string localeId, ref string itemFullName, ILgIcuResourceBundle rb, string subsection)
		{
			string localeIdOrig = localeId;
			string itemNameOrig = itemFullName;
			if (localeId == null && itemFullName == null)
				throw new ArgumentException("Either localeId or itemName should be null, but not both");
			ILgIcuResourceBundle rbLocales = null;
			try
			{
				rbLocales = rb.get_GetSubsection(subsection);
				if (rbLocales == null)
					return false; // Should never be.
				while (rbLocales.HasNext)
				{
					ILgIcuResourceBundle rbItem = null;
					try
					{
						rbItem = rbLocales.Next;
						if (localeIdOrig != null && rbItem.Key == localeId)
							itemFullName = rbItem.String;
						if (itemNameOrig != null && rbItem.String == itemNameOrig)
							localeId = rbItem.Key;
						// NOTE: it's okay to rename the fullName, if the key(localeId) is found.
						// but not okay to rename the Key if the fullName is found.
						if (localeIdOrig != null && localeIdOrig == rbItem.Key)
						{
							return true;
						}
						else if (itemNameOrig != null && itemNameOrig == rbItem.String)
						{
							return (localeIdOrig == null || localeIdOrig == rbItem.Key);
						}
					}
					finally
					{
						if (rbItem != null)
							System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
					}
				}
				return false;
			}
			finally
			{
				if (rbLocales != null)
					System.Runtime.InteropServices.Marshal.ReleaseComObject(rbLocales);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsLanguageInIcu(string localeId, string itemName)
		{
				return (RbItemWithLocaleIdOrNameExists(ref localeId, ref itemName, m_langDefCurrent.RootRb, "Languages"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether a Pua Character has already been defined for a certain codepoint
		/// </summary>
		/// <param name="codepoint">The codepoint you wish to check</param>
		/// <returns><c>true</c> if it contains codepoint, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool IsDefinedPuaCharacter(string codepoint)
		{
			CheckDisposed();

			foreach (PuaListItem puaCharListItem in m_lstPUACharacters.Items)
			{
				if (puaCharListItem.PUAChar.CodePoint == codepoint)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether a Pua Character has already been defined for a certain codepoint
		/// </summary>
		/// <param name="puaChar">The codepoint you wish to check</param>
		/// <returns><c>true</c> if it contains codepoint, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool IsDefinedPuaCharacter(PUACharacter puaChar)
		{
			CheckDisposed();

			return IsDefinedPuaCharacter(puaChar.CodePoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the PUACharacter from the list box, or <c>null</c> if there is none.
		/// </summary>
		/// <param name="codepoint">The codepoint of the character to retrieve.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public PUACharacter GetDefinedPuaCharacter(string codepoint)
		{
			CheckDisposed();

			// Find the character List Item
			PuaListItem puaCharListItem = FindListItem(codepoint);
			return (puaCharListItem != null ? puaCharListItem.PUAChar : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the given PUACharacter via its codepoint and sets it as modified.
		/// </summary>
		/// <param name="puaChar">The PUACharacter to mark as modified</param>
		/// ------------------------------------------------------------------------------------
		public void MarkAsModified(PUACharacter puaChar)
		{
			CheckDisposed();

			PuaListItem puaListItem = FindListItem(puaChar.CodePoint);
			if (puaListItem != null)
			{
				puaListItem.Modified = true;
				// Update the underlying LD data
				m_langDefCurrent.RemovePuaDefinition(puaListItem.PUAChar.CharDef);
				m_langDefCurrent.AddPuaDefinition(puaListItem.PUAChar.CharDef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the given item in the list via its codepoint.</summary>
		/// <param name="codepoint"></param><returns></returns>
		/// ------------------------------------------------------------------------------------
		private PuaListItem FindListItem(string codepoint)
		{
			foreach (PuaListItem puaCharListItem in m_lstPUACharacters.Items)
			{
				if (puaCharListItem.PUAChar.CodePoint == codepoint)
					return puaCharListItem;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// updates the keyboard list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnGetFocus(object sender, System.EventArgs e)
		{
			if (m_KeyboardControl.LangDef == null)
				return;	// we got here too early in the init process.
			m_KeyboardControl.InitLanguageCombo();
			m_KeyboardControl.InitKeymanCombo();
			// Fill in all the custom PUA characters from the ICU
			FillPUAList();
			// Refill the encoding converters list in case it changed
			LoadAvailableConverters();
			Select_cbEncodingConverter();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if writing system was changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsChanged
		{
			get
			{
				CheckDisposed();
				return m_fChanged;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if a font or a font property changed, or if the Right-To-Left
		/// flag changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NewRenderingNeeded
		{
			get
			{
				CheckDisposed();
				return m_fChanged &&
					(m_fNewRendering || m_defaultFontsControl.NewRenderingNeeded);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if a sort property changed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SortChanged
		{
			get
			{
				CheckDisposed();

				return m_SortModified;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The language definitions after being modified by the dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<LanguageDefinition> FinalLanguageDefns
		{
			get { return m_listFinalLangDefs; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The language definitions that were added by using the Add or Copy buttons.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<LanguageDefinition> NewlyAddedLanguageDefns()
		{
			List<LanguageDefinition> newLanguageDefns = new List<LanguageDefinition>();
			for (int i = 0; i < m_listFinalLangDefs.Count; i++)
			{
				// currently, we identify new language definitions by finding the same object
				// in the original list. language definitions loaded with the dialog are clones.
				if (m_listFinalLangDefs[i] == m_listOrigLangDefs[i])
					newLanguageDefns.Add(m_listFinalLangDefs[i]);
			}

			return newLanguageDefns;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the PUA character dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual PUACharacterDlg CreatePUACharacterDlg()
		{
			return new PUACharacterDlg();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the PUACharacterDlg dialog to edit the given <c>puaChar</c>.
		/// </summary>
		/// <param name="puaChar">The PUACharacter to edit.</param>
		/// <param name="modify"><c>true</c> if we are modifying an existing character rather
		///  than making a new one.</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayPUACharacterDlg(PUACharacter puaChar, bool modify)
		{
			using (PUACharacterDlg puaDlg = CreatePUACharacterDlg())
			{
				puaDlg.PUAChar = puaChar;
				puaDlg.Modify = modify;
				puaDlg.SetDialogProperties(m_helpTopicProvider);
				puaDlg.ParentDialog = this;

				if (puaDlg.CallShowDialog() == DialogResult.OK)
				{
					// For the Add dialog, create a new list item
					if (!puaDlg.Modify)
					{
						// puaChar will be modified by the dialog box if the user presses okay
						PuaListItem puaLstItem = new PuaListItem(puaChar, true);
						m_lstPUACharacters.Items.Add(puaLstItem, true);
					}
					else
					{
						PuaListItem puaListItem = FindListItem(puaChar.CodePoint);
						if (puaListItem != null)
						{
							m_lstPUACharacters.SetItemChecked(
								m_lstPUACharacters.Items.IndexOf(puaListItem), true);
						}
					}
					// Mark the character as modified if they actually clicked okay.
					MarkAsModified(puaChar);
				}
			}
			m_lstPUACharacters.Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the PUA character list with all the custom PUA characters in ICU.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillPUAList()
		{
			// Don't fill the list if it has already been filled.
			if (m_lstPUACharacters.Items.Count == 0)
			{
				foreach (PUACharacter puaChar in StringUtils.GetDefinedCustomPUACharsFromICU())
					m_lstPUACharacters.Items.Add(new PuaListItem(puaChar), false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check all the codepoints in the associated Writing System Language Definition File.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupPUAList()
		{
			// Empty the event handler of the ItemCheck method so that we can set the values
			// without the automatic rigamorale of updating the LDF
			this.m_lstPUACharacters.ItemCheck -=
				new System.Windows.Forms.ItemCheckEventHandler(this.m_lstPUACharacters_ItemCheck);

			// Make sure the PUA list is filled before we try to check them
			FillPUAList();

			PuaListItem puaListItem;

			if (m_langDefCurrent == null)
			{
				MessageBox.Show(FwCoreDlgs.ksLDFNotLoaded);
				return;
			}
			// First of all assume all PUA characters should be set unchecked.
			for (int i = 0; i < m_lstPUACharacters.Items.Count; i++)
			{
				m_lstPUACharacters.SetItemChecked(i, false);
			}
			// If there are no PuaDefinitions then do not check any of them
			if (m_langDefCurrent.PuaDefinitions == null)
				return;

			//If there are some PUA characters set for this writing system then
			//set them checked in the listBox
			foreach (CharDef charDef in m_langDefCurrent.PuaDefinitions)
			{
				for (int i = 0; i < m_lstPUACharacters.Items.Count; i++)
				{
					puaListItem = (PuaListItem)m_lstPUACharacters.Items[i];
					if (puaListItem.PUAChar.CodePoint == charDef.code)
						m_lstPUACharacters.SetItemChecked(i, true);
				}
			}

			// Reset the listener
			this.m_lstPUACharacters.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_lstPUACharacters_ItemCheck);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read all of the Icu into a cached list of every character in the ICU database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillIcuCache()
		{
			InitializeIcuCache();
			foreach (int[] range in m_cachedIcuRanges)
			{
				FillRange(range);
			}
			// REVIEW (TimS/EberhardB): We don't think we should call GC.Collect().
			// If there seems to be a need for it, then we probably need to call
			// Dispose() on one of the variables we want disposed.
			GC.Collect();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a new empty Icu Cache, if it doesn't already exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeIcuCache()
		{
			// Only fill the cache if it hasn't been done.
			if (m_cachedIcu == null)
			{
				// Set the initial capacity to include the entire first range
				m_cachedIcu = new RedBlackTree(new PUACharacter(""));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillRange(int[] range)
		{
			// Makes a new empty Icu Cache, if it doesn't already exist
			InitializeIcuCache();

			PUACharacter newIcuChar;

			for (int codepoint = range[0]; codepoint <= range[1]; codepoint++)
			{
				// TODO: Make sure that leaving off leading zeros is okay.
				newIcuChar = new PUACharacter(codepoint.ToString("x").ToUpper());
				// Fill in the character from the ICU database.
				newIcuChar.RefreshFromIcu(true);
				// Add the character to the cache
				m_cachedIcu.Insert(newIcuChar);

				// REVIEW (TimS/EberhardB): We don't think we should call GC.Collect().
				// If there seems to be a need for it, then we probably need to call
				// Dispose() on one of the variables we want disposed.
				if (codepoint % 0x100 == 0)
					GC.Collect();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the cached character associated with the given codepoint
		/// </summary>
		/// <param name="codepoint">The integer codepoint of the cached character.</param>
		/// <returns>The character as a PUACharacter</returns>
		/// ------------------------------------------------------------------------------------
		public PUACharacter FindCachedIcuEntry(int codepoint)
		{
			CheckDisposed();

			//? Need to have at least 4 digits in the string.
			//? string codepointAsString = string.Format("{0:x4}", codepoint).ToUpper();
			string codepointAsString = codepoint.ToString("x").ToUpper();
			foreach (int[] range in m_cachedIcuRanges)
			{
				if (IsInRange(codepoint, range))
				{
					// If the range is not loaded, load it
					if (range[2]++ == 0)
						FillRange(range);
					return (PUACharacter)m_cachedIcu.Find(codepointAsString, new UCDComparer());
				}
			}

			// If we get here, we didn't find the codepoint
			PUACharacter newIcuChar = new PUACharacter(codepointAsString);
			// Load the character if it exists and has a name
			if (newIcuChar.RefreshFromIcu(false))
			{
				// This can be called from PuaCharacterDlg where m_cachedIcu has not been initialized.
				InitializeIcuCache();
				m_cachedIcu.Insert(newIcuChar);
				return newIcuChar;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the cached character associated with the given codepoint
		/// </summary>
		/// <param name="codepoint">The codepoint as a character string.</param>
		/// <returns>The character as a PUACharacter</returns>
		/// ------------------------------------------------------------------------------------
		public PUACharacter FindCachedIcuEntry(string codepoint)
		{
			CheckDisposed();

			return FindCachedIcuEntry(PUACharacter.ConvertToIntegerCodepoint(codepoint));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Helper function that returns 'true' when the codepoint is in the given range.</summary>
		/// <param name="codepoint"></param><param name="rangeToCheck"></param><returns></returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsInRange(int codepoint, int[] rangeToCheck)
		{
			// if range[0] < code < range[1] then code is in the range
			return (rangeToCheck[0] <= codepoint && rangeToCheck[1] >= codepoint);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void linkToEthnologue_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			string targetURL = String.Format("http://www.ethnologue.com/show_language.asp?code={0}", m_LanguageCode.Text);
			System.Diagnostics.Process.Start(targetURL);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_linkWindowsKeyboard_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.OperatingSystem osInfo = System.Environment.OSVersion;
			System.Diagnostics.ProcessStartInfo processInfo = new ProcessStartInfo("C:\\WINDOWS\\system32\\control.exe", "input.dll");
			Process.Start(processInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_linkKeymanConfiguration_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			int version = 0;
			string keymanPath = GetKeymanRegistryValue("root path", ref version);
			if (keymanPath != null)
			{
				string keyman = Path.Combine(keymanPath, "kmshell.exe");
				if (File.Exists(keyman))
				{
					// From Marc Durdin (7/16/09):
					// Re LT-9902, in Keyman 6, you could launch the configuration dialog reliably by running kmshell.exe.
					// However, Keyman 7 works slightly differently.  The recommended approach is to use the COM API:
					// http://www.tavultesoft.com/keymandev/documentation/70/comapi_interface_IKeymanProduct_OpenConfiguration.html
					// Sample code:
					//	dim kmcom, product
					//	Set kmcom = CreateObject("kmcomapi.TavultesoftKeyman")
					//	rem  Pro = ProductID 1; Light = ProductID 8
					//	rem  Following line will raise exception if product is not installed, so try/catch it
					//	Set product = kmcom.Products.ItemsByProductID(1)
					//	Product.OpenConfiguration
					// But if that is not going to be workable for you, then use the parameter  "-c" to start configuration.
					// Without a parameter, the action is to start Keyman Desktop itself; v7.0 would fire configuration if restarted,
					// v7.1 just flags to the user that Keyman is running and where to find it.  This change was due to feedback that
					// users would repeatedly try to start Keyman when it was already running, and get confused when they got the
					// Configuration dialog.  Sorry for the unannounced change... 9
					// The -c parameter will not work with Keyman 6, so you would need to test for the specific version.  For what it's worth, the
					// COM API is static and should not change, while the command line parameters are not guaranteed to change from version to version.
					string param = "";
					if (version > 6)
						param = "-c";
					System.Diagnostics.Process.Start(keyman, param);
					return;
				}
			}
			MessageBox.Show("Keyman 5.0 or later is not Installed.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method returns the path to Keyman Configuration if it is installed. Otherwise it returns null.
		/// It also sets the version of Keyman that it found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetKeymanRegistryValue(string key, ref int version)
		{
			RegistryKey rkTavultesoft;
			RegistryKey rkKeyman = null;
			RegistryKey rkApplication;

			rkTavultesoft = Registry.LocalMachine.OpenSubKey("Software", false).OpenSubKey("Tavultesoft", false);
			if (rkTavultesoft != null)
				rkKeyman = rkTavultesoft.OpenSubKey("Keyman", false);
			if (rkKeyman != null)
			{
				//May 2008 version 7.0 is the lastest version. The others are here for
				//future versions.
				int[] versions = { 10, 9, 8, 7, 6, 5 };
				foreach (int vers in versions)
				{
					rkApplication = rkKeyman.OpenSubKey(vers.ToString() + ".0", false);
					if (rkApplication != null)
					{
						foreach (string sKey in rkApplication.GetValueNames())
						{
							if (sKey == key)
							{
								version = vers;
								return (string)rkApplication.GetValue(sKey);
							}
						}
					}
				}
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_tbLanguageName_TextChanged(object sender, EventArgs e)
		{
			if (m_fUserChangedText)
				UpdateDialogWithChangesToLanguageName();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateDialogWithChangesToLanguageName()
		{
			UpdateLanguageNameAndDefnsFromTextBox();
			PopulateRelatedWSsListBox();
			SelectIndexOfListBoxRelatedWss(m_langDefCurrentIndex);

			SetLanguageNameLabels();
			Set_regionVariantControl(m_langDefCurrent);
			SetFullNameLabels(m_langDefCurrent.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadShortWsNameFromCurrentLanguageDefn()
		{
			string shortAbbr = m_langDefCurrent.WritingSystem.get_Abbr(m_displayWs);
			// reset m_ShortWsName before we set it, so that m_ShortWsName_TextChanged
			// will always get triggered.
			m_ShortWsName.Text = "";
			m_ShortWsName.Text = (shortAbbr == null ?
				CreateDefaultLanguageNameAbbr(m_LanguageName) : shortAbbr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the language name, construct a default abbr.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string CreateDefaultLanguageNameAbbr(string enLanguageName)
		{
			return LeftSubstring(enLanguageName, 3).ToUpper();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateLanguageNameAndDefnsFromTextBox()
		{
			m_LanguageName = m_tbLanguageName.Text;
			if (m_LanguageName == null)
				m_LanguageName = String.Empty;
			foreach (LanguageDefinition langDef in m_listFinalLangDefs)
			{
				langDef.LocaleName = m_LanguageName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool CheckIfSortingIsOK()
		{
			if (!txtIcuRules.Enabled)
				return true;
			string result = Icu.CheckRules(txtIcuRules.Text);
			if (result == null)
			{
				return true;
			}
			else
			{
				// Switch back to the sorting tab so user can see it if CheckValid displays an error.
				SwitchTab(kWsSorting);
				tabControl.Update();
				txtIcuRules.Focus();
				txtIcuRules.SelectAll();
				string error = String.Format(FwCoreDlgs.ksInvalidSortSpec, result);
				MessageBox.Show(this, error, FwCoreDlgs.ksSortSpecError,
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is really SelectedIndexChanged and several operations can trigger this event
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_listBoxRelatedWSs_UserSelectionChanged(object sender, EventArgs e)
		{
			if (m_fHandleSelectedIndexChanged == false)
				return;

			//before switching to another writing system it is necessary to
			//ensure that the various settings the user has chosen are validated
			if (!CheckOkToChangeContext())
			{
				SelectIndexOfListBoxRelatedWss(m_langDefCurrentIndex);
				return;
			}

			SwitchDialogToSelectedWS();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the variant control Region/Variant name has changed and matches a Ws already in the database
		/// or in our list, bring up a dialog to help the user change the names appropriate.
		/// </summary>
		/// <returns>true Ws is unique in database and in our current list, false otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		private bool CheckUniqueWs()
		{
			if (m_langDefCurrent.HasChangedIcuLocale &&
				m_regionVariantControl.RegionOrVariantOrScriptChanged && m_langDefCurrent.IsWritingSystemInDb())
			{
				ShowMsgBoxCantOverwriteWsInDb();
				return false;
			}
			else if (IsNew(m_langDefCurrent))
			{
				// there are three problem cases for a new one
				// 1) matches another one in the database (handled below)
				// 2) matches another one in the Languages Directory (handled by CheckIcuNames)
				//		-- in that case we can just ask the user if they want to load the existing one.
				// 3) matches another one in our list (handled below)
				if (m_langDefCurrent.IsWritingSystemInDb())
				{
					ShowMsgBoxCantOverwriteWsInDb();
					return false;
				}
				else
				{
					foreach (LanguageDefinition langDef in m_listFinalLangDefs)
					{
						if (langDef == m_langDefCurrent)
							continue;
						if (langDef.CurrentFullLocale() == m_langDefCurrent.CurrentFullLocale())
						{
							ShowMsgBoxCantCreateDuplicateWs();
							return false;
						}
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsNew(LanguageDefinition langDef)
		{
			// the givin language definition is new if its object is in both
			// the original list and the final list.
			int indexInFinalLangDefs = m_listFinalLangDefs.IndexOf(langDef);
			int indexInOrigLangDefs = m_listOrigLangDefs.IndexOf(langDef);
			return indexInOrigLangDefs != -1 && indexInFinalLangDefs != -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowMsgBoxCantOverwriteWsInDb()
		{
			string caption = FwCoreDlgs.kstidNwsCaption;
			string strLoc = m_langDefCurrent.WritingSystem.IcuLocale;

			ILgWritingSystemFactory wsf = m_langDefCurrent.XmlWritingSystem.WritingSystem.WritingSystemFactory;
			int defWs = m_displayWs;
			int ws = wsf.GetWsFromStr(strLoc);
			IWritingSystem qws = wsf.get_EngineOrNull(ws);
			string strDispName = qws.get_UiName(defWs);

			string msg = string.Format(FwCoreDlgs.kstidCantOverwriteWsInDb,
				strDispName, strLoc, Environment.NewLine, m_langDefCurrent.DisplayName);
			MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowMsgBoxCantCreateDuplicateWs()
		{
			string caption = FwCoreDlgs.kstidNwsCaption;
			string strLoc = m_langDefCurrent.WritingSystem.IcuLocale;

			string msg = string.Format(FwCoreDlgs.kstidCantCreateDuplicateWs,
				m_langDefCurrent.DisplayName, Environment.NewLine);
			MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When changing m_listBoxRelatedWSs we need to set a flag
		/// so that the m_listBoxRelatedWSs_UserSelectionChanged event handler will return
		/// without performing any changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_ListBoxRelatedWSsSetDisplayname(int index, string DisplayName)
		{
			m_fHandleSelectedIndexChanged = false;
			m_listBoxRelatedWSs.Items[index] = DisplayName;
			m_fHandleSelectedIndexChanged = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When changing m_listBoxRelatedWSs we need to set a flag
		/// so that the m_listBoxRelatedWSs_UserSelectionChanged event handler will return
		/// without performing any changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SelectIndexOfListBoxRelatedWss(int index)
		{
			m_fHandleSelectedIndexChanged = false;
			m_listBoxRelatedWSs.SelectedIndex = index;
			m_fHandleSelectedIndexChanged = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnRemove_Click(object sender, EventArgs e)
		{
			// if we're removing from the end of the list, the new index will be the previous one,
			// otherwise, keep the index the same.
			int indexNext = m_listBoxRelatedWSs.SelectedIndex == m_listBoxRelatedWSs.Items.Count - 1 ?
				m_listBoxRelatedWSs.SelectedIndex - 1 : m_listBoxRelatedWSs.SelectedIndex;
			m_listOrigLangDefs.Remove(m_langDefCurrent);
			m_listFinalLangDefs.Remove(m_langDefCurrent);
			PopulateRelatedWSsListBox();
			m_langDefCurrent = m_listFinalLangDefs[indexNext];
			SetupDialogFromCurrentLanguageDefinition();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnAdd_Click(object sender, EventArgs e)
		{
			ContextMenuStrip cmsAddWs = new ContextMenuStrip();
			FwProjPropertiesDlg.ShowAddWsContextMenu(cmsAddWs, GetAllNamedWritingSystems(), m_listBoxRelatedWSs, sender as Button,
				btnAddWsItemClicked, null, btnNewWsItemClicked, m_langDefCurrent.WritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnAddWsItemClicked(object sender, System.EventArgs e)
		{
			if (!CheckOkToChangeContext())
				return;
			FwProjPropertiesDlg.WsMenuItem mnuItem = (FwProjPropertiesDlg.WsMenuItem)sender;
			LanguageDefinition langDef = CreateLanguageDefFromXml(m_cache.LanguageWritingSystemFactoryAccessor,
				mnuItem.WritingSystem.IcuLocale);
			AddLangDefinition(langDef, false);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnNewWsItemClicked(object sender, System.EventArgs e)
		{
			AddNewWsForLanguage();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new ws for the language that the dialog has been setup for.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal protected void AddNewWsForLanguage()
		{
			if (!CheckOkToChangeContext())
				return;
			// first see if it's okay to switch contexts.
			// TODO: Make CheckOkToChangeContext() have a mode that doesn't show dialog boxes for errors, but logs them
			LanguageDefinition newLangDef = m_LanguageDefinitionFactory.CreateNewFrom(m_cache.LanguageWritingSystemFactoryAccessor, m_langDefCurrent) as LanguageDefinition;
			AddLangDefinition(newLangDef, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddLangDefinition(LanguageDefinition newLangDef, bool fSwitchToGeneralTab)
		{
			SaveCurrentLangDef();
			// add the new language definition to our language definitions.
			// Note: we'll add the same object to original and final definitions to indicate these are new
			// and will always be equivalent in comparisons.
			m_listOrigLangDefs.Add(newLangDef);
			m_listFinalLangDefs.Add(newLangDef);
			// make this new current language def.
			m_langDefCurrent = newLangDef;
			PopulateRelatedWSsListBox();
			SetupDialogFromCurrentLanguageDefinition();
			if (fSwitchToGeneralTab)
			{
				try
				{
					// switch to General tab to allow the user to make the current ws valid but don't do context checking.
					m_fSkipCheckOkToChangeContext = true;
					SwitchTab(kWsGeneral);
				}
				finally
				{
					m_fSkipCheckOkToChangeContext = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnCopy_Click(object sender, EventArgs e)
		{
			if (!CheckOkToChangeContext())
				return;
			AddLangDefinition((m_langDefCurrent as ICloneable).Clone() as LanguageDefinition, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// handles cases for tabControl.SelectedIndex = index,
		/// and allows tests to override so that it can trigger
		/// events tabControl_Deselecting() and tabControl_SelectedIndexChanged() since
		/// for some reason those events aren't getting triggered in the tests.
		/// </summary>
		/// <param name="index"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SwitchTab(int index)
		{
			tabControl.SelectedIndex = index;
			switch(index)
			{
				case kWsGeneral:
					{
						m_regionVariantControl.Focus();
						break;
					}
				case kWsSorting:
					{
						tabControl.Update();
						txtIcuRules.Focus();
						txtIcuRules.SelectAll();
						break;
					}
			}
		}

		private void m_fwTextBoxTestWs_Enter(object sender, EventArgs e)
		{
			int currentWs = m_cache.LanguageEncodings.GetWsFromIcuLocale(m_langDefCurrent.IcuLocaleOriginal);
			// Ensure the engine exists in the test WSF for the current WS and copy relevant settings for testing.
			IWritingSystem realEngine = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(currentWs);
			IWritingSystem testEngine = m_fwTextBoxTestWs.WritingSystemFactory.get_Engine(realEngine.IcuLocale);
			testEngine.DefaultBodyFont = testEngine.DefaultSerif = m_defaultFontsControl.DefaultNormalFont;
			testEngine.DefaultSansSerif = m_defaultFontsControl.DefaultHeadingFont;
			testEngine.Locale = m_langDefCurrent.WritingSystem.Locale;
			testEngine.KeymanKbdName = m_langDefCurrent.WritingSystem.KeymanKbdName;

			// It's no good setting the stylesheet, because the test WS won't have the same ID as currentWs.
			// Also, we want to demonstrate the effect of the defaults, without the overrides in the stylesheet.
			// Except, we will produce the effect of whatever font size is set in Normal.
			//m_fwTextBoxTestWs.StyleSheet = m_stylesheet;
			int height = FontHeightAdjuster.GetFontHeightForStyle("Normal", m_stylesheet, currentWs,
																  m_cache.LanguageWritingSystemFactoryAccessor);
			m_fwTextBoxTestWs.WritingSystemCode = testEngine.WritingSystem;
			ITsStrBldr bldr = StringUtils.MakeTss(string.Empty, testEngine.WritingSystem).GetBldr();
			bldr.SetIntPropValues(0, 0, (int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, height);
			m_fwTextBoxTestWs.Tss = bldr.GetString();
		}

		private void cbDictionaries_TextChanged(object sender, EventArgs e)
		{
			if (m_fChangeSpellCheckDictionaryAllowed)
			{
				if (String.IsNullOrEmpty(((NameAbbrComboItem)(cbDictionaries.SelectedItem)).Abbr))
					m_langDefCurrent.WritingSystem.SpellCheckDictionary = "<None>";
				else
					m_langDefCurrent.WritingSystem.SpellCheckDictionary = ((NameAbbrComboItem)(cbDictionaries.SelectedItem)).Abbr;
			}
		}

		void txtIcuRules_GotFocus(object sender, EventArgs e)
		{
			KeyboardHelper.ActivateKeyboard(m_langDefCurrent.WritingSystem.Locale, m_langDefCurrent.WritingSystem.KeymanKbdName);
		}

		void txtIcuRules_LostFocus(object sender, EventArgs e)
		{
			KeyboardHelper.ActivateDefaultKeyboard();
		}

		private void m_ampersandButton_Click(object sender, EventArgs e)
		{
			txtIcuRules.SelectedText = "&";
		}

		private void m_angleBracketButton_Click(object sender, EventArgs e)
		{
			txtIcuRules.SelectedText = "<";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			DisableSaveIfRemoteDb(this, m_cache, m_tbLanguageName.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-8606: Disable editing writing system properties for remote users
		/// </summary>
		/// <param name="dlg">The parent dialog box (whose OK button will be disabled if
		/// necessary).</param>
		/// <param name="cache">The cache.</param>
		/// <param name="languageName">Name of the language being modified (needed for display
		/// in the message box).</param>
		/// ------------------------------------------------------------------------------------
		internal static void DisableSaveIfRemoteDb(Form dlg, FdoCache cache, string languageName)
		{
			Debug.Assert(dlg != null && dlg.AcceptButton is Button && ((Button)dlg.AcceptButton).Name == "btnOk");
			if (!MiscUtils.IsServerLocal(cache.ServerName))
			{
				((Button)dlg.AcceptButton).Enabled = false;
				// Suppress display of the message if the given dialog is owned by the
				// WritingSystemPropertiesDialog since it would have already shown the message.
				if (!(dlg.Owner is WritingSystemPropertiesDialog))
				{
					MessageBox.Show(dlg, String.Format(Properties.Resources.kstidMultiUserCantEditWs,
						languageName, Environment.MachineName.ToUpperInvariant(), cache.DatabaseName,
						cache.ServerMachineName.ToUpperInvariant()),
						Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
		}
	}

	#endregion

	#region PuaListItem class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is a wrapper class for the PUACharacter class. It overrides the ToString so
	/// we can display things the way we want when a list item is added.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PuaListItem
	{
		PUACharacter m_puaChar;
		private bool m_fIsModified = false;
		private readonly bool m_fIsNew;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The PUACharacter that this class represents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PUACharacter PUAChar
		{
			get { return m_puaChar; }
			set { m_puaChar = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether the PUACharacter has been modified.
		/// If <c>Modified == false</c> then we don't need to write the PUACharacter to the LDF.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Modified
		{
			get { return m_fIsModified; }
			set { m_fIsModified = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the PUACharacter is a newly added one (not yet saved/installed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsNew
		{
			get { return m_fIsNew; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new list item for the PUACharacterDlg wrapped around <c>puaChar</c>
		/// </summary>
		/// <param name="puaChar">The PUACharacter that this represents.</param>
		/// ------------------------------------------------------------------------------------
		public PuaListItem(PUACharacter puaChar) : this(puaChar, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new list item for the PUACharacterDlg wrapped around <c>puaChar</c>
		/// </summary>
		/// <param name="puaChar">The PUACharacter that this represents.</param>
		/// <param name="fIsNew">Indicates whether the PUACharacter is a newly added one (not
		/// yet saved/installed).</param>
		/// ------------------------------------------------------------------------------------
		public PuaListItem(PUACharacter puaChar, bool fIsNew)
		{
			m_puaChar = puaChar;
			m_fIsNew = fIsNew;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides toString so we can choose how to display our puaCharacter in our list box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_puaChar.CodePoint + ": " + m_puaChar.Name;
		}
	}

	#endregion
}
