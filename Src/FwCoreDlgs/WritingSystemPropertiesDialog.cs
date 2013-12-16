// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: WritingSystemProperties.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using System.Linq;
using Palaso.UI.WindowsForms.WritingSystems;
using Palaso.WritingSystems;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SilEncConverters40;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgControls;
using SILUBS.SharedScrUtils;
using SIL.CoreImpl;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// The writing system properties dialog.
	/// </summary>
	public class WritingSystemPropertiesDialog : Form, IFWDisposable
	{
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
		#endregion

		internal Palaso.UI.WindowsForms.WritingSystems.WSKeyboardControl m_keyboardControl;
		/// <summary>Index(5) of the tab for writing systems PUA characters</summary>
		public const int kWsPUACharacters = 5;

		internal WritingSystemSetupModel m_modelForKeyboard;


		/// <summary>
		/// Shows the new writing system properties dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="wsManager">The ws manager.</param>
		/// <param name="wsContainer">The ws container.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="displayRelatedWss">if set to <c>true</c> related writing systems will be displayed.</param>
		/// <param name="defaultName">The default language name for the new writing system.</param>
		/// <param name="newWritingSystems">The new writing systems.</param>
		/// <returns></returns>
		public static bool ShowNewDialog(Form owner, FdoCache cache, IWritingSystemManager wsManager,
			IWritingSystemContainer wsContainer, IHelpTopicProvider helpTopicProvider, IApp app,
			IVwStylesheet stylesheet, bool displayRelatedWss, string defaultName,
			out IEnumerable<IWritingSystem> newWritingSystems)
		{
			newWritingSystems = null;
			LanguageSubtag languageSubtag;

			using (new WaitCursor(owner))
			using (var dlg = new LanguageSelectionDlg(wsManager, helpTopicProvider))
			{
				dlg.Text = FwCoreDlgs.kstidLanguageSelectionNewWsCaption;
				dlg.DefaultLanguageName = defaultName;

				if (dlg.ShowDialog(owner) != DialogResult.OK)
					return false;

				languageSubtag = dlg.LanguageSubtag;
			}

			using (new WaitCursor(owner))
			using (var wsPropsDlg = new WritingSystemPropertiesDialog(cache, wsManager, wsContainer, helpTopicProvider, app, stylesheet))
			{
				wsPropsDlg.SetupDialog(languageSubtag, displayRelatedWss);

				if (wsPropsDlg.ShowDialog(owner) == DialogResult.OK)
				{
					newWritingSystems = wsPropsDlg.NewWritingSystems;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Shows the modify writing system properties dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="selectedWs">The selected writing system.</param>
		/// <param name="addNewForLangOfSelectedWs">if set to <c>true</c> a new writing system with the
		/// same language as the selected writing system will be added.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="wsContainer">The ws container.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="newWritingSystems">The new writing systems.</param>
		/// <returns></returns>
		public static bool ShowModifyDialog(Form owner, IWritingSystem selectedWs, bool addNewForLangOfSelectedWs, FdoCache cache,
			IWritingSystemContainer wsContainer, IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet,
			out IEnumerable<IWritingSystem> newWritingSystems)
		{
			newWritingSystems = null;
			string path;
			if (!cache.ServiceLocator.WritingSystemManager.CanSave(selectedWs, out path))
			{
				MessageBox.Show(owner, string.Format(FwCoreDlgs.ksCannotSaveWritingSystem, path), FwCoreDlgs.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false; // nothing changed.
			}
			using (new WaitCursor(owner))
			using (var wsPropsDlg = new WritingSystemPropertiesDialog(cache, cache.ServiceLocator.WritingSystemManager,
				wsContainer, helpTopicProvider, app, stylesheet))
			{
				wsPropsDlg.SetupDialog(selectedWs, true);
				if (addNewForLangOfSelectedWs)
					wsPropsDlg.AddNewWsForLanguage();

				if (!ClientServerServices.Current.WarnOnOpeningSingleUserDialog(cache))
					return false; // nothing changed.

				if (wsPropsDlg.ShowDialog(owner) == DialogResult.OK)
				{
					if (wsPropsDlg.IsChanged)
					{
						newWritingSystems = wsPropsDlg.NewWritingSystems;
						return true;
					}
				}
			}

			return false;
		}

		#region Member variables

		private readonly Dictionary<IWritingSystem, IWritingSystem> m_tempWritingSystems = new Dictionary<IWritingSystem, IWritingSystem>();
		private HashSet<IWritingSystem> m_activeWritingSystems;
		private IWritingSystem m_prevSelectedWritingSystem;

		private readonly FdoCache m_cache;
		/// <summary></summary>
		protected readonly IWritingSystemManager m_wsManager;
		private readonly IWritingSystemContainer m_wsContainer;
		private readonly IVwStylesheet m_stylesheet;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly IApp m_app;
		private readonly ITsStrFactory m_tsf;

		// Change flags
		private bool m_fChanged;

		// Guards
		private bool m_userChangedLanguageName = true;
		private bool m_userChangedVariantControl = true;
		private bool m_userChangedSpellCheckDictionary = true;
		private bool m_userChangedSortUsing = true;
		private bool m_userChangedSortRules = true;

		private System.ComponentModel.Container components;
		private HelpProvider helpProvider;

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
		protected Button m_deleteButton;

		#endregion Ws ListBox

		#region LanguageName and Ethnologue Code

		/// <summary> </summary>
		protected TextBox m_tbLanguageName;
		/// <summary> </summary>
		protected Label m_LanguageCode;
		private LinkLabel m_linkToEthnologue;

		#endregion LanguageName and Ethnologue Code

		/// <summary> </summary>
		protected Button btnModifyEthnologueInfo;
		/// <summary> </summary>
		protected TabControl tabControl;
		/// <summary> </summary>
		protected Button btnOk;
		/// <summary> </summary>
		protected Button btnCancel;
		/// <summary> </summary>
		private Button btnHelp;

		#region General Tab

		/// <summary> Abbreviation: # </summary>
		protected TextBox m_ShortWsName;
		/// <summary> </summary>
		protected RegionVariantControl m_regionVariantControl;
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
		protected DefaultFontsControl m_defaultFontsControl;

		#endregion

		#region Keyboard Tab


		#endregion Keyboard Tab

		#region Converters Tab

		private Button btnEncodingConverter;
		private Label m_lblEncodingConverter;
		/// <summary> </summary>
		protected FwOverrideComboBox cbEncodingConverter;

		#endregion Converters Tab

		#region Sorting Tab

		private Panel m_sortRulesPanel;
		private ComboBox m_sortUsingComboBox;
		/// <summary></summary>
		protected FwTextBox m_sortRulesTextBox;
		private Label m_sortingHelpLabel;
		private Label m_sortUsingLabel;
		private Panel m_sortRulesButtonPanel;
		private Button m_angleBracketButton;
		private Button m_ampersandButton;
		private Panel m_sortRulesLoadPanel;
		private Label m_sortRulesLoadLabel;
		private LocaleMenuButton m_similarWsButton;
		private Label m_sortLanguageLabel;
		private ComboBox m_sortLanguageComboBox;
		private Panel m_sortLanguagePanel;

		#endregion Sorting Tab

		#region Characters Tab

		private Button btnValidChars;
		private Label m_lblValidCharacters;

		#endregion Characters Tab

		private Label m_lblPunctuation;
		private Button btnPunctuation;
		private Label lblFullCode;
		private Label m_FullCode;
		private Label lblScriptRegionVariant;

		#endregion

		#region Construction, deconstruction, and initialization

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WritingSystemPropertiesDialog"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsManager">The ws manager.</param>
		/// <param name="wsContainer">The ws container.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		public WritingSystemPropertiesDialog(FdoCache cache, IWritingSystemManager wsManager, IWritingSystemContainer wsContainer,
			IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet) : this()
		{
			m_cache = cache;
			m_wsManager = wsManager;
			m_wsContainer = wsContainer;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_stylesheet = stylesheet;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WritingSystemPropertiesDialog"/> class.
		/// </summary>
		private WritingSystemPropertiesDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			components = new Container();
			AccessibleName = GetType().Name;
			m_lblValidCharacters.Tag = m_lblValidCharacters.Text;
			m_lblPunctuation.Tag = m_lblPunctuation.Text;
			m_lblEncodingConverter.Tag = m_lblEncodingConverter.Text;
			m_tsf = TsStrFactoryClass.Create();

			LoadSortUsingComboBox();
			LoadSortLanguageComboBox();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (disposing && !IsDisposed)
			{
				if (components != null)
					components.Dispose();
				if (m_sortRulesTextBox != null && m_sortRulesTextBox.WritingSystemFactory != null)
				{
					var disposable = m_sortRulesTextBox.WritingSystemFactory as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		private void LoadSortUsingComboBox()
		{
			var types = new ArrayList();
			foreach (Enum customSortRulesType in Enum.GetValues(typeof(WritingSystemDefinition.SortRulesType)))
			{
				FieldInfo fi = customSortRulesType.GetType().GetField(customSortRulesType.ToString());

				var descriptions = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
				string description = descriptions.Length == 0 ? customSortRulesType.ToString() : descriptions[0].Description;
				types.Add(new { Id = customSortRulesType.ToString(), Name = description });
			}

			m_sortUsingComboBox.ValueMember = "Id";
			m_sortUsingComboBox.DataSource = types;
			m_sortUsingComboBox.DisplayMember = "Name";
		}

		private void LoadSortLanguageComboBox()
		{
			var languages = new ArrayList();
			foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(curCi => curCi.EnglishName))
				languages.Add(new { Id = ci.Name, Name = ci.EnglishName });

			m_sortLanguageComboBox.ValueMember = "Id";
			m_sortLanguageComboBox.DataSource = languages;
			m_sortLanguageComboBox.DisplayMember = "Name";
		}

		/// <summary>
		/// Set writing system and initialize some values for the dialog
		/// </summary>
		/// <param name="selectedWs">The writing system.</param>
		/// <param name="displayRelatedWss">if set to <c>true</c> related writing systems will be displayed.</param>
		public void SetupDialog(IWritingSystem selectedWs, bool displayRelatedWss)
		{
			CheckDisposed();

			SetupDialog(m_wsManager.CreateFrom(selectedWs), selectedWs, displayRelatedWss);
		}

		/// <summary>
		/// Setups the dialog.
		/// </summary>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="displayRelatedWss">if set to <c>true</c> related writing systems will be displayed.</param>
		public void SetupDialog(LanguageSubtag languageSubtag, bool displayRelatedWss)
		{
			CheckDisposed();

			RegionSubtag region = null;
			if (languageSubtag.Code == "zh" && languageSubtag.ISO3Code == "cmn")
				region = new RegionSubtag("CN", "China", false);
			SetupDialog(m_wsManager.Create(languageSubtag, null, region, null), null, displayRelatedWss);
		}

		private void SetupDialog(IWritingSystem tempWs, IWritingSystem origWs, bool displayRelatedWss)
		{
			m_tempWritingSystems[tempWs] = origWs;
			if (displayRelatedWss)
			{
				foreach (IWritingSystem ws in m_wsManager.LocalWritingSystems.Except(new[] {origWs}).Related(tempWs))
				{
					IWritingSystem newWs = m_wsManager.CreateFrom(ws);
					m_tempWritingSystems[newWs] = ws;
				}
			}

			LoadAvailableConverters();
			PopulateRelatedWSsListBox(tempWs);
		}

		/// <summary>
		/// Display WS's that are related to the 'ws' parameter of SetupDialog()
		/// </summary>
		/// <param name="selectedWs">The selected ws.</param>
		private void PopulateRelatedWSsListBox(IWritingSystem selectedWs)
		{
			m_listBoxRelatedWSs.BeginUpdate();
			m_listBoxRelatedWSs.Items.Clear();

			// ensure SelectedItem happens after all items added to m_listBoxRelatedWSs
			// This ensures more consistent behaviour across platforms.
			bool fSetSelectedItem = false;
			foreach (IWritingSystem tempWs in m_tempWritingSystems.Keys.OrderBy(ws => ws.DisplayLabel))
			{
				m_listBoxRelatedWSs.Items.Add(tempWs);
				if (selectedWs == tempWs)
					fSetSelectedItem = true;
			}

			if (fSetSelectedItem)
				m_listBoxRelatedWSs.SelectedItem = selectedWs;

			m_listBoxRelatedWSs.EndUpdate();
			// update buttons.
			UpdateListBoxButtons();
		}

		private void SetupDialogFromCurrentWritingSystem()
		{
			IWritingSystem ws = CurrentWritingSystem;
			UpdateListBoxButtons();
			// Setup General Tab information
			Set_tbLanguageName(ws.LanguageSubtag.Name ?? string.Empty);
			SetupEthnologueCode(ws);

			m_defaultFontsControl.WritingSystem = ws;

			//Switch Encoding Converters to the one for the user selected writing system
			Select_cbEncodingConverter();

			PopulateSpellingDictionaryComboBox();

			// Update all the labels using the selected language display name
			SetLanguageNameLabels();
			Set_regionVariantControl(ws);
			SetFullNameLabels(ws.DisplayLabel);
			if(tabControl.SelectedTab == tpSorting)
			{
				SetupSortTab(ws);
			}
			m_modelForKeyboard = new WritingSystemSetupModel((WritingSystemDefinition) ws);
			m_keyboardControl.BindToModel(m_modelForKeyboard);
		}

		private void SetupSortTab(IWritingSystem ws)
		{
			m_userChangedSortUsing = false;
			m_sortUsingComboBox.SelectedValue = ws.SortUsing.ToString();
			m_userChangedSortUsing = true;

			m_userChangedSortRules = false;
			var wsManager = FwUtils.CreateWritingSystemManager();
			var palasoWs = (PalasoWritingSystem)ws;
			var oldStoreId = palasoWs.StoreID;
			wsManager.Set(ws);
			// Setting it into the temporary WS manager will set its StoreID. This could cause
			// problems if we later add it to the real WS manager. So we need to restore it.
			palasoWs.StoreID = oldStoreId;
			m_sortRulesTextBox.WritingSystemFactory = wsManager;
			m_sortRulesTextBox.WritingSystemCode = ws.Handle;
			switch (ws.SortUsing)
			{
				case WritingSystemDefinition.SortRulesType.DefaultOrdering:
					m_sortLanguagePanel.Visible = false;
					m_sortRulesPanel.Visible = false;
					break;

				case WritingSystemDefinition.SortRulesType.CustomICU:
					m_sortLanguagePanel.Visible = false;
					m_sortRulesPanel.Visible = true;
					m_sortRulesButtonPanel.Visible = true;
					m_sortRulesLoadPanel.Visible = true;
					m_sortingHelpLabel.Text = string.Format(FwCoreDlgs.kstidIcuSortingHelp, Environment.NewLine);
					m_sortRulesTextBox.Tss = m_tsf.MakeString(ws.SortRules ?? "", ws.Handle);
					break;

				case WritingSystemDefinition.SortRulesType.CustomSimple:
					m_sortLanguagePanel.Visible = false;
					m_sortRulesPanel.Visible = true;
					m_sortRulesButtonPanel.Visible = false;
					m_sortRulesLoadPanel.Visible = false;
					m_sortingHelpLabel.Text = string.Format(FwCoreDlgs.kstidSimpleSortingHelp, Environment.NewLine);
					m_sortRulesTextBox.Tss = m_tsf.MakeString(ws.SortRules ?? "", ws.Handle);
					break;

				case WritingSystemDefinition.SortRulesType.OtherLanguage:
					m_sortRulesPanel.Visible = false;
					m_sortLanguagePanel.Visible = true;
					if (string.IsNullOrEmpty(ws.SortRules))
					{
						try
						{
							CultureInfo ci = CultureInfo.GetCultureInfo(ws.Id);
							m_sortLanguageComboBox.SelectedValue = ci.Name;
						}
						catch (ArgumentException)
						{
							m_sortLanguageComboBox.SelectedIndex = 0;
							ws.SortRules = (string)m_sortLanguageComboBox.SelectedValue;
						}
					}
					else
					{
						m_sortLanguageComboBox.SelectedValue = ws.SortRules;
					}
					break;
			}
			m_userChangedSortRules = true;
		}

		private void SetupEthnologueCode(IWritingSystem ws)
		{
			string strNone = FwCoreDlgs.kstidNone;
			var languageSubtag = ws.LanguageSubtag;
			var ethCode = languageSubtag.Code; // For most languages this is right.
			if (languageSubtag.IsPrivateUse)
				ethCode = FwCoreDlgs.kstidNone; // code is not from ethnologue
			else if (!string.IsNullOrEmpty(languageSubtag.ISO3Code))
				ethCode = languageSubtag.ISO3Code; // if it has a 3-letter code show that.
			SetLanguageCodeLabels(ethCode);
		}

		/// <summary>
		/// Load the Spelling Dictionaries ComboBox
		/// </summary>
		private void PopulateSpellingDictionaryComboBox()
		{
			var dictionaries = new ArrayList { new { Name = FwCoreDlgs.ksWsNoDictionaryMatches, Id = "<None>" } };

			string spellCheckingDictionary = CurrentWritingSystem.SpellCheckingId;
			if(spellCheckingDictionary == null)
			{
				dictionaries.Add(new { Name = CurrentWritingSystem.Id, Id = CurrentWritingSystem.Id.Replace('-', '_') });
			}

			bool fDictionaryExistsForLanguage = false;
			bool fAlternateDictionaryExistsForLanguage = false;
			string selectComboItem = "<None>";
			foreach (var languageId in SpellingHelper.GetDictionaryIds().OrderBy(di => GetDictionaryName(di)))
			{
				dictionaries.Add(new { Name = GetDictionaryName(languageId), Id = languageId });
				//If this WS.SpellCheckingDictionary matches a known Dictionary then
				//ensure the comboBox has that item selected.
				if (spellCheckingDictionary == languageId)
				{
					selectComboItem = languageId;
					fDictionaryExistsForLanguage = true;
				}
				else if (!fDictionaryExistsForLanguage && !fAlternateDictionaryExistsForLanguage)
				{
					// The first half of the OR handles things like choosing the dictionary for 'en_US' when seeking one
					// for 'en', as in our extension SpellingHelper. The second branch of the OR is unused at present
					// but will help if we extend SpellingHelper to the 'en' dictionary when asked for the 'en_US' one
					// (when it can't find an exact match).
					if (spellCheckingDictionary != null &&
						(languageId.StartsWith(spellCheckingDictionary) || spellCheckingDictionary.StartsWith(languageId)))
					{
						// Vernacular dictionaries may only be used if they match the requested ID exactly.
						if (!SpellingHelper.IsVernacular(languageId))
						{
							selectComboItem = languageId;
							fAlternateDictionaryExistsForLanguage = true;
						}
					}
				}
			}

			m_userChangedSpellCheckDictionary = false;
			cbDictionaries.ValueMember = "Id";
			cbDictionaries.DataSource = dictionaries;
			cbDictionaries.DisplayMember = "Name";

			cbDictionaries.SelectedValue = selectComboItem;
			m_userChangedSpellCheckDictionary = true;
		}

		private static string GetDictionaryName(String languageId)
		{
			Icu.UErrorCode err;
			string country;
			Icu.GetDisplayCountry(languageId, "en", out country, out err);
			string languageName;
			Icu.GetDisplayLanguage(languageId, "en", out languageName, out err);
			var languageAndCountry = new StringBuilder(languageName);
			if (!string.IsNullOrEmpty(country))
				languageAndCountry.AppendFormat(" ({0})", country);
			if (languageName != languageId)
				languageAndCountry.AppendFormat(" [{0}]", languageId);
			return languageAndCountry.ToString();
		}

		private bool IsWritingSystemHidden(IWritingSystem ws)
		{
			// Fix FWNX-563
			IWritingSystem origWs;
			if (!m_tempWritingSystems.TryGetValue(ws, out origWs) || origWs == null)
				return false;

			return !m_wsContainer.AllWritingSystems.Contains(origWs);
		}

		private void m_listBoxRelatedWSs_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index == -1)
				return;
			bool selected = ((e.State & DrawItemState.Selected) != 0);
			bool isWsHidden = IsWritingSystemHidden((IWritingSystem)m_listBoxRelatedWSs.Items[e.Index]);
			using (var drawFont = new Font(e.Font, isWsHidden ? FontStyle.Italic : FontStyle.Regular))
			{
				Brush textBrush = isWsHidden ? SystemBrushes.GrayText : SystemBrushes.ControlText;
				if (selected)
					textBrush = SystemBrushes.HighlightText;
				e.DrawBackground();
				e.Graphics.DrawString(m_listBoxRelatedWSs.Items[e.Index].ToString(), drawFont, textBrush, e.Bounds);
			}
		}

		private void UpdateListBoxButtons()
		{
			m_deleteButton.Enabled = IsNew(CurrentWritingSystem) && m_listBoxRelatedWSs.Items.Count > 1;
		}

		private bool IsNew(IWritingSystem ws)
		{
			IWritingSystem origWs;
			bool present = m_tempWritingSystems.TryGetValue(ws, out origWs);
			//IWritingSystem origWs = m_tempWritingSystems[ws];
			return origWs == null || origWs.Handle == 0;
		}

		/// <summary>
		/// Select the encoding converty for the currently selected writing system.
		/// </summary>
		private void Select_cbEncodingConverter()
		{
			string strLegacyMapping = CurrentWritingSystem.LegacyMapping ?? FwCoreDlgs.kstidNone;
			if (!cbEncodingConverter.Items.Contains(strLegacyMapping))
			{
				strLegacyMapping = strLegacyMapping + FwCoreDlgs.kstidNotInstalled;
				cbEncodingConverter.Items.Add(strLegacyMapping);
			}
			cbEncodingConverter.SelectedItem = strLegacyMapping;
		}

		/// <summary>
		/// Load the Available Encoding Converters.
		/// </summary>
		protected virtual void LoadAvailableConverters()
		{
			// Save the old selection so it can be restored after the combo box is filled
			string oldSelection = null;
			if (cbEncodingConverter.SelectedIndex != -1)
				oldSelection = (string)cbEncodingConverter.SelectedItem;
			try
			{
				var encConverters = new EncConverters();
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

		#region Properties

		// Allows us temporarily to override the normal behavior of CurrentWritingSystem.
		private IWritingSystem m_overrideCurrentWritingSystem;

		/// <summary>
		/// Gets the current writing system.
		/// </summary>
		/// <value>The current writing system.</value>
		protected IWritingSystem CurrentWritingSystem
		{
			get
			{
				if (m_overrideCurrentWritingSystem != null)
					return m_overrideCurrentWritingSystem; // occasionally we need to override this.
				return (IWritingSystem)m_listBoxRelatedWSs.SelectedItem;
			}
		}

		/// <summary>
		/// Gets the new writing systems.
		/// </summary>
		/// <value>The new writing systems.</value>
		public IEnumerable<IWritingSystem> NewWritingSystems
		{
			get
			{
				CheckDisposed();

				return m_tempWritingSystems.Keys.Where(IsNew);
			}
		}

		/// <summary>
		/// Returns <c>true</c> if writing system was changed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is changed; otherwise, <c>false</c>.
		/// </value>
		public bool IsChanged
		{
			get
			{
				CheckDisposed();
				return m_fChanged;
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
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
			this.m_keyboardControl = new Palaso.UI.WindowsForms.WritingSystems.WSKeyboardControl();
			this.tpConverters = new System.Windows.Forms.TabPage();
			this.btnEncodingConverter = new System.Windows.Forms.Button();
			this.m_lblEncodingConverter = new System.Windows.Forms.Label();
			this.cbEncodingConverter = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.tpSorting = new System.Windows.Forms.TabPage();
			this.m_sortUsingLabel = new System.Windows.Forms.Label();
			this.m_sortUsingComboBox = new System.Windows.Forms.ComboBox();
			this.m_sortRulesPanel = new System.Windows.Forms.Panel();
			this.m_sortRulesLoadPanel = new System.Windows.Forms.Panel();
			this.m_sortRulesLoadLabel = new System.Windows.Forms.Label();
			this.m_similarWsButton = new SIL.FieldWorks.FwCoreDlgControls.LocaleMenuButton();
			this.m_sortRulesButtonPanel = new System.Windows.Forms.Panel();
			this.m_angleBracketButton = new System.Windows.Forms.Button();
			this.m_ampersandButton = new System.Windows.Forms.Button();
			this.m_sortRulesTextBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_sortingHelpLabel = new System.Windows.Forms.Label();
			this.m_sortLanguagePanel = new System.Windows.Forms.Panel();
			this.m_sortLanguageComboBox = new System.Windows.Forms.ComboBox();
			this.m_sortLanguageLabel = new System.Windows.Forms.Label();
			this.tpPUACharacters = new System.Windows.Forms.TabPage();
			this.m_lblPunctuation = new System.Windows.Forms.Label();
			this.btnPunctuation = new System.Windows.Forms.Button();
			this.m_lblValidCharacters = new System.Windows.Forms.Label();
			this.btnValidChars = new System.Windows.Forms.Button();
			this.btnModifyEthnologueInfo = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_listBoxRelatedWSs = new System.Windows.Forms.ListBox();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnCopy = new System.Windows.Forms.Button();
			this.m_deleteButton = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.m_linkToEthnologue = new System.Windows.Forms.LinkLabel();
			this.m_LanguageCode = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.m_tbLanguageName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_writingSystemsFor = new System.Windows.Forms.Label();
			this.lblHiddenWss = new System.Windows.Forms.Label();
			this.tabControl.SuspendLayout();
			this.tpGeneral.SuspendLayout();
			this.gbDirection.SuspendLayout();
			this.tpFonts.SuspendLayout();
			this.tpKeyboard.SuspendLayout();
			this.tpConverters.SuspendLayout();
			this.tpSorting.SuspendLayout();
			this.m_sortRulesPanel.SuspendLayout();
			this.m_sortRulesLoadPanel.SuspendLayout();
			this.m_sortRulesButtonPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_sortRulesTextBox)).BeginInit();
			this.m_sortLanguagePanel.SuspendLayout();
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
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			this.tabControl.Deselecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Deselecting);
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
			this.helpProvider.SetShowHelp(this.m_FullCode, ((bool)(resources.GetObject("m_FullCode.ShowHelp"))));
			//
			// lblFullCode
			//
			resources.ApplyResources(this.lblFullCode, "lblFullCode");
			this.lblFullCode.Name = "lblFullCode";
			this.helpProvider.SetShowHelp(this.lblFullCode, ((bool)(resources.GetObject("lblFullCode.ShowHelp"))));
			//
			// lblSpellingDictionary
			//
			resources.ApplyResources(this.lblSpellingDictionary, "lblSpellingDictionary");
			this.lblSpellingDictionary.Name = "lblSpellingDictionary";
			this.helpProvider.SetShowHelp(this.lblSpellingDictionary, ((bool)(resources.GetObject("lblSpellingDictionary.ShowHelp"))));
			//
			// cbDictionaries
			//
			this.cbDictionaries.AllowSpaceInEditBox = false;
			this.cbDictionaries.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbDictionaries.FormattingEnabled = true;
			resources.ApplyResources(this.cbDictionaries, "cbDictionaries");
			this.cbDictionaries.Name = "cbDictionaries";
			this.helpProvider.SetShowHelp(this.cbDictionaries, ((bool)(resources.GetObject("cbDictionaries.ShowHelp"))));
			this.cbDictionaries.SelectedIndexChanged += new System.EventHandler(this.cbDictionaries_SelectedIndexChanged);
			//
			// m_regionVariantControl
			//
			resources.ApplyResources(this.m_regionVariantControl, "m_regionVariantControl");
			this.m_regionVariantControl.BackColor = System.Drawing.Color.Transparent;
			this.m_regionVariantControl.Name = "m_regionVariantControl";
			this.m_regionVariantControl.RegionName = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.m_regionVariantControl.RegionSubtag = null;
			this.m_regionVariantControl.ScriptName = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.m_regionVariantControl.ScriptSubtag = null;
			this.helpProvider.SetShowHelp(this.m_regionVariantControl, ((bool)(resources.GetObject("m_regionVariantControl.ShowHelp"))));
			this.m_regionVariantControl.VariantName = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.m_regionVariantControl.VariantSubtag = null;
			this.m_regionVariantControl.WritingSystem = null;
			this.m_regionVariantControl.ScriptRegionVariantChanged += new System.EventHandler(this.m_regionVariantControl_ScriptRegionVariantChanged);
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
			this.m_defaultFontsControl.DefaultNormalFont = "";
			this.helpProvider.SetHelpString(this.m_defaultFontsControl, resources.GetString("m_defaultFontsControl.HelpString"));
			this.m_defaultFontsControl.Name = "m_defaultFontsControl";
			this.helpProvider.SetShowHelp(this.m_defaultFontsControl, ((bool)(resources.GetObject("m_defaultFontsControl.ShowHelp"))));
			this.m_defaultFontsControl.WritingSystem = null;
			//
			// tpKeyboard
			//
			this.tpKeyboard.Controls.Add(this.m_keyboardControl);
			resources.ApplyResources(this.tpKeyboard, "tpKeyboard");
			this.tpKeyboard.Name = "tpKeyboard";
			this.helpProvider.SetShowHelp(this.tpKeyboard, ((bool)(resources.GetObject("tpKeyboard.ShowHelp"))));
			this.tpKeyboard.UseVisualStyleBackColor = true;
			//
			// m_keyboardControl
			//
			resources.ApplyResources(this.m_keyboardControl, "m_keyboardControl");
			this.m_keyboardControl.Name = "m_keyboardControl";
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
			this.cbEncodingConverter.SelectedIndexChanged += new System.EventHandler(this.cbEncodingConverter_SelectedIndexChanged);
			//
			// tpSorting
			//
			this.tpSorting.BackColor = System.Drawing.Color.Transparent;
			this.tpSorting.Controls.Add(this.m_sortUsingLabel);
			this.tpSorting.Controls.Add(this.m_sortUsingComboBox);
			this.tpSorting.Controls.Add(this.m_sortRulesPanel);
			this.tpSorting.Controls.Add(this.m_sortLanguagePanel);
			resources.ApplyResources(this.tpSorting, "tpSorting");
			this.tpSorting.Name = "tpSorting";
			this.helpProvider.SetShowHelp(this.tpSorting, ((bool)(resources.GetObject("tpSorting.ShowHelp"))));
			this.tpSorting.UseVisualStyleBackColor = true;
			//
			// m_sortUsingLabel
			//
			resources.ApplyResources(this.m_sortUsingLabel, "m_sortUsingLabel");
			this.m_sortUsingLabel.Name = "m_sortUsingLabel";
			//
			// m_sortUsingComboBox
			//
			this.m_sortUsingComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_sortUsingComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_sortUsingComboBox, "m_sortUsingComboBox");
			this.m_sortUsingComboBox.Name = "m_sortUsingComboBox";
			this.m_sortUsingComboBox.SelectedIndexChanged += new System.EventHandler(this.m_sortUsingComboBox_SelectedIndexChanged);
			//
			// m_sortRulesPanel
			//
			this.m_sortRulesPanel.Controls.Add(this.m_sortRulesLoadPanel);
			this.m_sortRulesPanel.Controls.Add(this.m_sortRulesButtonPanel);
			this.m_sortRulesPanel.Controls.Add(this.m_sortRulesTextBox);
			this.m_sortRulesPanel.Controls.Add(this.m_sortingHelpLabel);
			resources.ApplyResources(this.m_sortRulesPanel, "m_sortRulesPanel");
			this.m_sortRulesPanel.Name = "m_sortRulesPanel";
			this.helpProvider.SetShowHelp(this.m_sortRulesPanel, ((bool)(resources.GetObject("m_sortRulesPanel.ShowHelp"))));
			//
			// m_sortRulesLoadPanel
			//
			this.m_sortRulesLoadPanel.Controls.Add(this.m_sortRulesLoadLabel);
			this.m_sortRulesLoadPanel.Controls.Add(this.m_similarWsButton);
			resources.ApplyResources(this.m_sortRulesLoadPanel, "m_sortRulesLoadPanel");
			this.m_sortRulesLoadPanel.Name = "m_sortRulesLoadPanel";
			//
			// m_sortRulesLoadLabel
			//
			resources.ApplyResources(this.m_sortRulesLoadLabel, "m_sortRulesLoadLabel");
			this.m_sortRulesLoadLabel.Name = "m_sortRulesLoadLabel";
			this.helpProvider.SetShowHelp(this.m_sortRulesLoadLabel, ((bool)(resources.GetObject("m_sortRulesLoadLabel.ShowHelp"))));
			//
			// m_similarWsButton
			//
			this.m_similarWsButton.DisplayLocaleId = null;
			resources.ApplyResources(this.m_similarWsButton, "m_similarWsButton");
			this.m_similarWsButton.Name = "m_similarWsButton";
			this.m_similarWsButton.SelectedLocaleId = null;
			this.helpProvider.SetShowHelp(this.m_similarWsButton, ((bool)(resources.GetObject("m_similarWsButton.ShowHelp"))));
			this.m_similarWsButton.UseVisualStyleBackColor = true;
			this.m_similarWsButton.LocaleSelected += new System.EventHandler(this.m_similarWsButton_LocaleSelected);
			//
			// m_sortRulesButtonPanel
			//
			this.m_sortRulesButtonPanel.Controls.Add(this.m_angleBracketButton);
			this.m_sortRulesButtonPanel.Controls.Add(this.m_ampersandButton);
			resources.ApplyResources(this.m_sortRulesButtonPanel, "m_sortRulesButtonPanel");
			this.m_sortRulesButtonPanel.Name = "m_sortRulesButtonPanel";
			//
			// m_angleBracketButton
			//
			resources.ApplyResources(this.m_angleBracketButton, "m_angleBracketButton");
			this.m_angleBracketButton.Name = "m_angleBracketButton";
			this.helpProvider.SetShowHelp(this.m_angleBracketButton, ((bool)(resources.GetObject("m_angleBracketButton.ShowHelp"))));
			this.m_angleBracketButton.UseMnemonic = false;
			this.m_angleBracketButton.UseVisualStyleBackColor = true;
			this.m_angleBracketButton.Click += new System.EventHandler(this.m_angleBracketButton_Click);
			//
			// m_ampersandButton
			//
			resources.ApplyResources(this.m_ampersandButton, "m_ampersandButton");
			this.m_ampersandButton.Name = "m_ampersandButton";
			this.helpProvider.SetShowHelp(this.m_ampersandButton, ((bool)(resources.GetObject("m_ampersandButton.ShowHelp"))));
			this.m_ampersandButton.UseMnemonic = false;
			this.m_ampersandButton.UseVisualStyleBackColor = true;
			this.m_ampersandButton.Click += new System.EventHandler(this.m_ampersandButton_Click);
			//
			// m_sortRulesTextBox
			//
			this.m_sortRulesTextBox.AcceptsReturn = true;
			this.m_sortRulesTextBox.AdjustStringHeight = false;
			resources.ApplyResources(this.m_sortRulesTextBox, "m_sortRulesTextBox");
			this.m_sortRulesTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_sortRulesTextBox.controlID = null;
			this.m_sortRulesTextBox.HasBorder = true;
			this.m_sortRulesTextBox.Name = "m_sortRulesTextBox";
			this.helpProvider.SetShowHelp(this.m_sortRulesTextBox, ((bool)(resources.GetObject("m_sortRulesTextBox.ShowHelp"))));
			this.m_sortRulesTextBox.SuppressEnter = false;
			this.m_sortRulesTextBox.WordWrap = true;
			this.m_sortRulesTextBox.TextChanged += new System.EventHandler(this.m_sortRulesTextBox_TextChanged);
			//
			// m_sortingHelpLabel
			//
			resources.ApplyResources(this.m_sortingHelpLabel, "m_sortingHelpLabel");
			this.m_sortingHelpLabel.Name = "m_sortingHelpLabel";
			this.helpProvider.SetShowHelp(this.m_sortingHelpLabel, ((bool)(resources.GetObject("m_sortingHelpLabel.ShowHelp"))));
			this.m_sortingHelpLabel.UseMnemonic = false;
			//
			// m_sortLanguagePanel
			//
			this.m_sortLanguagePanel.Controls.Add(this.m_sortLanguageComboBox);
			this.m_sortLanguagePanel.Controls.Add(this.m_sortLanguageLabel);
			resources.ApplyResources(this.m_sortLanguagePanel, "m_sortLanguagePanel");
			this.m_sortLanguagePanel.Name = "m_sortLanguagePanel";
			//
			// m_sortLanguageComboBox
			//
			this.m_sortLanguageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_sortLanguageComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_sortLanguageComboBox, "m_sortLanguageComboBox");
			this.m_sortLanguageComboBox.Name = "m_sortLanguageComboBox";
			this.m_sortLanguageComboBox.SelectedIndexChanged += new System.EventHandler(this.m_sortLanguageComboBox_SelectedIndexChanged);
			//
			// m_sortLanguageLabel
			//
			resources.ApplyResources(this.m_sortLanguageLabel, "m_sortLanguageLabel");
			this.m_sortLanguageLabel.Name = "m_sortLanguageLabel";
			//
			// tpPUACharacters
			//
			this.tpPUACharacters.BackColor = System.Drawing.Color.Transparent;
			this.tpPUACharacters.Controls.Add(this.m_lblPunctuation);
			this.tpPUACharacters.Controls.Add(this.btnPunctuation);
			this.tpPUACharacters.Controls.Add(this.m_lblValidCharacters);
			this.tpPUACharacters.Controls.Add(this.btnValidChars);
			resources.ApplyResources(this.tpPUACharacters, "tpPUACharacters");
			this.tpPUACharacters.Name = "tpPUACharacters";
			this.helpProvider.SetShowHelp(this.tpPUACharacters, ((bool)(resources.GetObject("tpPUACharacters.ShowHelp"))));
			this.tpPUACharacters.UseVisualStyleBackColor = true;
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
			// m_lblValidCharacters
			//
			resources.ApplyResources(this.m_lblValidCharacters, "m_lblValidCharacters");
			this.m_lblValidCharacters.Name = "m_lblValidCharacters";
			this.helpProvider.SetShowHelp(this.m_lblValidCharacters, ((bool)(resources.GetObject("m_lblValidCharacters.ShowHelp"))));
			//
			// btnValidChars
			//
			this.helpProvider.SetHelpString(this.btnValidChars, resources.GetString("btnValidChars.HelpString"));
			resources.ApplyResources(this.btnValidChars, "btnValidChars");
			this.btnValidChars.Name = "btnValidChars";
			this.helpProvider.SetShowHelp(this.btnValidChars, ((bool)(resources.GetObject("btnValidChars.ShowHelp"))));
			this.btnValidChars.Click += new System.EventHandler(this.btnValidChars_Click);
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
			this.m_listBoxRelatedWSs.Sorted = true;
			this.m_listBoxRelatedWSs.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.m_listBoxRelatedWSs_DrawItem);
			this.m_listBoxRelatedWSs.SelectedIndexChanged += new System.EventHandler(this.m_listBoxRelatedWSs_SelectedIndexChanged);
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
			// m_deleteButton
			//
			resources.ApplyResources(this.m_deleteButton, "m_deleteButton");
			this.m_deleteButton.Name = "m_deleteButton";
			this.helpProvider.SetShowHelp(this.m_deleteButton, ((bool)(resources.GetObject("m_deleteButton.ShowHelp"))));
			this.m_deleteButton.UseVisualStyleBackColor = true;
			this.m_deleteButton.Click += new System.EventHandler(this.m_deleteButton_Click);
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
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.lblHiddenWss);
			this.Controls.Add(this.m_writingSystemsFor);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.m_deleteButton);
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
			this.tabControl.ResumeLayout(false);
			this.tpGeneral.ResumeLayout(false);
			this.tpGeneral.PerformLayout();
			this.gbDirection.ResumeLayout(false);
			this.tpFonts.ResumeLayout(false);
			this.tpKeyboard.ResumeLayout(false);
			this.tpConverters.ResumeLayout(false);
			this.tpConverters.PerformLayout();
			this.tpSorting.ResumeLayout(false);
			this.tpSorting.PerformLayout();
			this.m_sortRulesPanel.ResumeLayout(false);
			this.m_sortRulesLoadPanel.ResumeLayout(false);
			this.m_sortRulesButtonPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_sortRulesTextBox)).EndInit();
			this.m_sortLanguagePanel.ResumeLayout(false);
			this.m_sortLanguagePanel.PerformLayout();
			this.tpPUACharacters.ResumeLayout(false);
			this.tpPUACharacters.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Button click handlers

		private void btnEncodingConverter_Click(object sender, EventArgs e)
		{
			try
			{
				string prevEC = cbEncodingConverter.Text;
				using (var dlg = new AddCnvtrDlg(m_helpTopicProvider, m_app, null,
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
				var sb = new StringBuilder(ex.Message);
				sb.Append(Environment.NewLine);
				sb.Append(FwCoreDlgs.kstidErrorAccessingEncConverters);
				MessageBox.Show(this, sb.ToString(), ResourceHelper.GetResourceString("kstidCannotModifyWS"));
			}
		}

		/// <summary>
		/// Handles the Click event of the btnModifyEthnologueInfo control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected void btnModifyEthnologueInfo_Click(object sender, EventArgs e)
		{
			if (!CheckOkToChangeContext())
				return;

			using (var dlg = new LanguageSelectionDlg(m_wsManager, m_helpTopicProvider))
			{
				dlg.StartedInModifyState = true;	// started in modify state

				string origLangName = m_tbLanguageName.Text;
				dlg.LanguageSubtag = CurrentWritingSystem.LanguageSubtag;

				if (CallShowDialog(dlg) != DialogResult.OK)
					return;

				List<Tuple<IWritingSystem, LanguageSubtag, bool>> origWsData =
					m_listBoxRelatedWSs.Items.Cast<IWritingSystem>().Select(ws => Tuple.Create(ws, ws.LanguageSubtag, ws.Modified)).ToList();
				LanguageSubtag subtag = dlg.LanguageSubtag;
				foreach (IWritingSystem ws in m_listBoxRelatedWSs.Items)
				{
					ws.LanguageSubtag = subtag;
					if (ws.LanguageSubtag.Code == "zh" && ws.LanguageSubtag.ISO3Code == "cmn" && ws.RegionSubtag == null)
						ws.RegionSubtag = new RegionSubtag("CN", "China", false);
				}

				if (!CheckWsIdChange())
				{
					// revert back to original language
					foreach (Tuple<IWritingSystem, LanguageSubtag, bool> wsData in origWsData)
					{
						wsData.Item1.LanguageSubtag = wsData.Item2;
						wsData.Item1.Modified = wsData.Item3;
					}
				}
				else
				{
					Set_tbLanguageName(subtag.Name);
					SetupEthnologueCode(CurrentWritingSystem);
					if (m_tbLanguageName.Text != origLangName)
					{
						int len = Math.Min(3, m_tbLanguageName.Text.Length);
						m_ShortWsName.Text = m_tbLanguageName.Text.Substring(0, len);
					}

					UpdateDialogWithChangesToLanguageName();
				}
			}
		}

		/// <summary>
		/// Calls the ShowDialog of the LanguageSelectionDlg. Used for tests.
		/// </summary>
		/// <param name="dlg">The language selection dialog.</param>
		/// <returns></returns>
		protected virtual DialogResult CallShowDialog(LanguageSelectionDlg dlg)
		{
			return dlg.ShowDialog(this);
		}

		private void Set_regionVariantControl(IWritingSystem ws)
		{
			m_userChangedVariantControl = false;
			m_regionVariantControl.WritingSystem = ws;
			m_userChangedVariantControl = true;

			m_FullCode.Text = ws.Id;

			LoadShortWsNameFromCurrentWritingSystem();
			rbLeftToRight.Checked = !ws.RightToLeftScript;
			rbRightToLeft.Checked = ws.RightToLeftScript;
		}

		/// <summary>
		/// When changing the text of m_tbLanguageName we need to set a flag
		/// so that the TextChanged event handler will return without performing
		/// any changes.
		/// </summary>
		/// <param name="languageName"></param>
		private void Set_tbLanguageName(string languageName)
		{
			m_userChangedLanguageName = false;
			m_tbLanguageName.Text = languageName;
			m_userChangedLanguageName = true;
		}

		private void SetLanguageCodeLabels(String str)
		{
			m_LanguageCode.Text = str;
			m_linkToEthnologue.Text = String.Format(FwCoreDlgs.ksWSPropEthnologueEntryFor, str);
		}

		private void SetLanguageNameLabels()
		{
			LoadShortWsNameFromCurrentWritingSystem();
		}

		private void SetFullNameLabels(string fullName)
		{
			SetLabelParams(m_lblValidCharacters, fullName);
			SetLabelParams(m_lblPunctuation, fullName);
			SetLabelParams(m_lblEncodingConverter, fullName);
		}

		private static void SetLabelParams(Label lbl, params string[] parms)
		{
			lbl.Text = string.Format((string)lbl.Tag, parms);
		}

		/// <summary>
		/// User clicked the OK button - persist the changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		private void OnOk(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				//Make sure the dialog does not close if we return early.
				DialogResult = DialogResult.None;

				if (!CheckOkToChangeContext())
					return;
				if (ThereAreChanges && ClientServerServices.Current.WarnOnConfirmingSingleUserChanges(m_cache))
					SaveChanges();

				DialogResult = DialogResult.OK;
			}
		}

		bool ThereAreChanges
		{
			get
			{
				foreach (KeyValuePair<IWritingSystem, IWritingSystem> kvp in m_tempWritingSystems)
				{
					IWritingSystem tempWs = kvp.Key;

					if (IsNew(tempWs) || tempWs.Modified)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Saves the changes to the writing system manager.
		/// </summary>
		protected void SaveChanges()
		{
			var wssToUpdate = new List<Tuple<IWritingSystem, string>>();
			foreach (KeyValuePair<IWritingSystem, IWritingSystem> kvp in m_tempWritingSystems)
			{
				IWritingSystem tempWs = kvp.Key;
				IWritingSystem origWs = kvp.Value;

				if (IsNew(tempWs))
				{
					m_wsManager.Replace(tempWs);
					m_fChanged = true;
				}
				else if (tempWs.Modified)
				{
					if (tempWs.Id != origWs.Id)
						wssToUpdate.Add(Tuple.Create(origWs, origWs.Id));
					origWs.Copy(tempWs);
					m_fChanged = true;
				}
			}

			if (m_cache != null && wssToUpdate.Count > 0)
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					foreach (Tuple<IWritingSystem, string> ws in wssToUpdate)
					{
						WritingSystemServices.UpdateWritingSystemId(m_cache, ws.Item1, ws.Item2);
						//Save incrementally to avoid an inconsistancy between the cache and the manager.
						m_wsManager.Save();
					}
				});
			}
			m_wsManager.Save();
		}

		/// <summary>
		/// Open the appropriate Help file for selected tab (Name or Attributes).
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		private void btnHelp_Click(object sender, EventArgs e)
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

		private void btnValidChars_Click(object sender, EventArgs e)
		{
			using (var dlg = new ValidCharactersDlg(m_cache, m_wsContainer, m_helpTopicProvider,
				m_app, CurrentWritingSystem, CurrentWritingSystem.DisplayLabel))
			{
				dlg.ShowDialog(this);
			}
		}

		private void btnPunctuation_Click(object sender, EventArgs e)
		{
			using (var dlg = new PunctuationDlg(m_cache, m_wsContainer, m_helpTopicProvider, m_app,
				CurrentWritingSystem, CurrentWritingSystem.DisplayLabel, StandardCheckIds.kguidMatchedPairs))
			{
				dlg.ShowDialog(this);
			}
		}

		/// <summary>
		/// Handles the Click event of the btnRemove control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected void m_deleteButton_Click(object sender, EventArgs e)
		{
			// if we're removing from the end of the list, the new index will be the previous one,
			// otherwise, keep the index the same.
			int indexNext = m_listBoxRelatedWSs.SelectedIndex == m_listBoxRelatedWSs.Items.Count - 1 ?
				m_listBoxRelatedWSs.SelectedIndex - 1 : m_listBoxRelatedWSs.SelectedIndex;
			IWritingSystem ws = CurrentWritingSystem;
			IWritingSystem origWs = m_tempWritingSystems[ws];
			m_tempWritingSystems.Remove(ws);
			m_listBoxRelatedWSs.Items.Remove(ws);
			m_listBoxRelatedWSs.SelectedIndex = indexNext;
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			var cmsAddWs = components.ContextMenuStrip("cmsAddWs");
				FwProjPropertiesDlg.ShowAddWsContextMenu(cmsAddWs, WritingSystemUtils.GetAllDistinctWritingSystems(m_wsManager), m_listBoxRelatedWSs,
					sender as Button, btnAddWsItemClicked, null, btnNewWsItemClicked, CurrentWritingSystem);
			}

		/// <summary>
		/// Handles the Click event of the add writing system menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected void btnAddWsItemClicked(object sender, EventArgs e)
		{
			if (!CheckOkToChangeContext())
				return;

			var mnuItem = (FwProjPropertiesDlg.WsMenuItem)sender;
			AddWritingSystem(m_wsManager.CreateFrom(mnuItem.WritingSystem), mnuItem.WritingSystem, false);
		}

		/// <summary>
		/// Handles the Click event of the new writing system menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected void btnNewWsItemClicked(object sender, EventArgs e)
		{
			if (!CheckOkToChangeContext())
				return;

			AddNewWsForLanguage();
		}

		/// <summary>
		/// Adds a new writing system based on the selected language.
		/// </summary>
		public void AddNewWsForLanguage()
		{
			CheckDisposed();

			// Definitely copy China region for new zh ws. Probably not a bad idea to copy other regions as well if selected.
			IWritingSystem tempWs = m_wsManager.Create(CurrentWritingSystem.LanguageSubtag, null, CurrentWritingSystem.RegionSubtag, null);

			tempWs.ValidChars = CurrentWritingSystem.ValidChars;
			tempWs.MatchedPairs = CurrentWritingSystem.MatchedPairs;
			tempWs.PunctuationPatterns = CurrentWritingSystem.PunctuationPatterns;
			tempWs.QuotationMarks = CurrentWritingSystem.QuotationMarks;
			AddWritingSystem(tempWs, null, true);
		}

		private void AddWritingSystem(IWritingSystem tempWs, IWritingSystem origWs, bool fSwitchToGeneralTab)
		{
			try
			{
				m_fSkipCheckOkToChangeContext = true;

				m_listBoxRelatedWSs.Items.Add(tempWs);
				m_tempWritingSystems[tempWs] = origWs;
				m_listBoxRelatedWSs.SelectedItem = tempWs;
				if (fSwitchToGeneralTab)
					SwitchTab(kWsGeneral);
				// A revised Palaso WritingSystem implementation changed some message handling
				// related to changing indexes and the like.  So we now need to explicitly set the
				// subcontrol's writing system.  See FWNX-999 for details of what went wrong (test
				// failure and buggy dialog behavior).  The following line is the the primary fix
				// for this change.
				Set_regionVariantControl(CurrentWritingSystem);
			}
			finally
			{
				m_fSkipCheckOkToChangeContext = false;
			}
		}

		/// <summary>
		/// Handles the Click event of the btnCopy control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected void btnCopy_Click(object sender, EventArgs e)
		{
			if (!CheckOkToChangeContext())
				return;

			AddWritingSystem(m_wsManager.CreateFrom(CurrentWritingSystem), null, true);
		}

		#endregion

		#region Other event handlers

		private void m_similarWsButton_LocaleSelected(object sender, EventArgs e)
		{
			string baseLocale = m_similarWsButton.SelectedLocaleId;
			if (!string.IsNullOrEmpty(CurrentWritingSystem.SortRules))
			{
				// "Overwrite existing collation rules?";
				DialogResult res = MessageBox.Show(this, FwCoreDlgs.kstidOverwriteRules,
					FwCoreDlgs.kstidOverwriteRulesCaption, MessageBoxButtons.YesNo);
				if (res == DialogResult.No)
					return;
			}
			if (baseLocale == null)
				baseLocale = "";

			string sortRules = Icu.GetCollationRules(baseLocale);
			m_sortRulesTextBox.Tss = m_tsf.MakeString(sortRules == null ? "" : sortRules.Replace("&", Environment.NewLine + "&").Trim(),
				CurrentWritingSystem.Handle);

			var resources = new ComponentResourceManager(typeof(WritingSystemPropertiesDialog));
			// apply default text
			m_similarWsButton.Text = (string)resources.GetObject("m_similarWsButton.Text");
		}

		private void m_ShortWsName_TextChanged(object sender, EventArgs e)
		{
			CurrentWritingSystem.Abbreviation = m_ShortWsName.Text;
		}

		private void rbLeftToRight_CheckedChanged(object sender, EventArgs e)
		{
			CurrentWritingSystem.RightToLeftScript = !rbLeftToRight.Checked;
		}

		private bool m_fSkipCheckOkToChangeContext;
		/// <summary>
		/// some user actions (e.g. Add ("Define New...") involve switching tabs to a context
		/// (e.g. General tab)that will allow the user the opportunity to make it Ok to change context.
		/// In that case, m_fSkipCheckOkToChangeContext should be set to true, so that switching tabs
		/// does not prematurely detect we're in an invalid state.
		/// </summary>
		/// <returns></returns>
		protected bool CheckOkToChangeContext()
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
					fOkToChangeContext = m_regionVariantControl.CheckValid();
					break;
				case kWsSorting:
					fOkToChangeContext = CheckIfSortingIsOK();
					break;
				case kWsConverters:
					fOkToChangeContext = CheckEncodingConverter();
					break;
			}

			if (fOkToChangeContext && !CheckWsIdChange())
				fOkToChangeContext = false;
			return fOkToChangeContext;
		}

		private bool CheckIfSortingIsOK()
		{
			string message;
			if (CurrentWritingSystem.ValidateCollationRules(out message))
				return true;

			// Switch back to the sorting tab so user can see it if CheckValid displays an error.
			SwitchTab(kWsSorting);
			tabControl.Update();
			m_sortRulesTextBox.Select();
			m_sortRulesTextBox.SelectAll();
			string error = String.Format(FwCoreDlgs.ksInvalidSortSpec, message);
			MessageBox.Show(this, error, FwCoreDlgs.ksSortSpecError,
				MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

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

		/// <summary>
		/// Checks to see if the user writing system identifier is being changed, or if a
		/// writing system is using an identifier that already exists.
		/// </summary>
		/// <returns></returns>
		private bool CheckWsIdChange()
		{
			foreach (IWritingSystem tempWs in m_listBoxRelatedWSs.Items)
			{
				// ContainsKey check deals with m_tempWritingSystems and m_listBoxRelatedWSs.Items being
				// out of sync which can happen because of mono/.NET winform event differences.
				// (ie. SelectedIndexChange event being emitted on a Remove)
				if (!m_tempWritingSystems.ContainsKey(tempWs))
					continue;

				IWritingSystem origWs = m_tempWritingSystems[tempWs];

				if (origWs == null || tempWs.Id != origWs.Id)
				{
					// We can't let anyone change the user writing system (or "English"). Too many strings depend on
					// this, and we'd get numerous crashes and terrible behavior if it was changed.
					if (origWs != null && (origWs == m_wsManager.UserWritingSystem || origWs.Id == "en"))
					{
						ShowMsgCantChangeUserWs(tempWs, origWs);
						return false;
					}

					// Catch case where we are going to overwrite an existing writing system.
					if (m_wsManager.Exists(tempWs.Id)
						|| m_listBoxRelatedWSs.Items.Cast<IWritingSystem>().Any(ws => ws != tempWs && ws.Id == tempWs.Id))
					{
						ShowMsgBoxCantCreateDuplicateWs(tempWs, origWs);
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Shows the "cannot change user writing system" message.
		/// </summary>
		/// <param name="tempWs">The temp writing system.</param>
		/// <param name="origWs">The original writing system.</param>
		protected virtual void ShowMsgCantChangeUserWs(IWritingSystem tempWs, IWritingSystem origWs)
		{
			string msg = string.Format(FwCoreDlgs.kstidCantChangeUserWs, origWs.Id);
			MessageBox.Show(msg, FwCoreDlgs.kstidWspLabel);
		}

		/// <summary>
		/// Shows the "cannnot create duplicate writing system" message.
		/// </summary>
		/// <param name="tempWs">The temp writing system.</param>
		/// <param name="origWs">The original writing system.</param>
		protected virtual void ShowMsgBoxCantCreateDuplicateWs(IWritingSystem tempWs, IWritingSystem origWs)
		{
			string caption = FwCoreDlgs.kstidNwsCaption;
			string msg = string.Format(FwCoreDlgs.kstidCantCreateDuplicateWs, tempWs.DisplayLabel, Environment.NewLine);
			MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		/// <summary>
		/// Handles the SelectedIndexChanged event of the tabControl control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				switch (tabControl.SelectedIndex)
				{
					case kWsSorting:
						SetupSortTab(CurrentWritingSystem);
						break;

					case kWsKeyboard:
						break;
				}
			}
		}

		/// <summary>
		/// Handles the Deselecting event of the tabControl control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TabControlCancelEventArgs"/> instance containing the event data.</param>
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

		private void linkToEthnologue_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			string targetURL = String.Format("http://www.ethnologue.com/show_language.asp?code={0}", m_LanguageCode.Text);
			using (Process.Start(targetURL))
			{
			}
		}

		private void m_tbLanguageName_TextChanged(object sender, EventArgs e)
		{
			if (m_userChangedLanguageName)
				UpdateDialogWithChangesToLanguageName();
		}

		private void UpdateDialogWithChangesToLanguageName()
		{
			UpdateLanguageNameAndWSsFromTextBox();
			SetLanguageNameLabels();
			Set_regionVariantControl(CurrentWritingSystem);
			SetFullNameLabels(CurrentWritingSystem.DisplayLabel);
			PopulateRelatedWSsListBox(CurrentWritingSystem);
		}

		private void LoadShortWsNameFromCurrentWritingSystem()
		{
			string shortAbbr = CurrentWritingSystem.Abbreviation;
			string langName = CurrentWritingSystem.LanguageSubtag.Name;
			m_ShortWsName.Text = shortAbbr ?? langName.Substring(0, Math.Min(langName.Length, 3));
		}

		private void UpdateLanguageNameAndWSsFromTextBox()
		{
			foreach (IWritingSystem ws in m_listBoxRelatedWSs.Items)
			{
				LanguageSubtag languageSubtag = ws.LanguageSubtag;
				ws.LanguageSubtag = new LanguageSubtag(languageSubtag, m_tbLanguageName.Text ?? string.Empty);
			}
		}

		private void m_listBoxRelatedWSs_SelectedIndexChanged(object sender, EventArgs e)
		{
			// CurrentWritingSystem can be null when we remove a WS from the list when running on Mono
			if (CurrentWritingSystem == null || m_prevSelectedWritingSystem == CurrentWritingSystem)
			{
				return;
			}
			// While we are editing this writing system we can't enforce valid tags; the user may temporarily
			// do something invalid, or we may even go through an invalid state while changing two properties
			// at once. It will become true again at latest when we close the dialog and try to save changes.
			if (CurrentWritingSystem is WritingSystemDefinition)
				((WritingSystemDefinition) CurrentWritingSystem).RequiresValidTag = false;
			if (!m_fSkipCheckOkToChangeContext && m_prevSelectedWritingSystem != null
				&& m_listBoxRelatedWSs.Items.Contains(m_prevSelectedWritingSystem))
			{
				// Before switching to another writing system it is necessary to
				// ensure that the various settings the user has chosen for the previous one are validated.
				// Unfortunately m_listBoxRelatedWSs.SelectedItem has already changed, and CurrentWritingSystem
				// therefore has too. We need the old WS to be current so we can check it.
				// To allow this we have currentWritingSystem support an override.
				try
				{
					var prevSelWs = m_prevSelectedWritingSystem;
					m_prevSelectedWritingSystem = null; // prevents this check firing if we switch back.
					m_overrideCurrentWritingSystem = prevSelWs;
					if (!CheckOkToChangeContext())
					{
						m_overrideCurrentWritingSystem = null; // override not be in force while changing back.
						m_listBoxRelatedWSs.SelectedItem = prevSelWs; // reverse the change
						m_prevSelectedWritingSystem = prevSelWs; // normal when that one is current.
						return; // leave things set to old item; CheckOk has reported problem.
					}
				}
				finally
				{
					m_overrideCurrentWritingSystem = null; // override should only be in force for this method.
				}
			}

			if (CurrentWritingSystem != null)
				SetupDialogFromCurrentWritingSystem();
			m_prevSelectedWritingSystem = CurrentWritingSystem;
		}

		/// <summary>
		/// handles cases for tabControl.SelectedIndex = index,
		/// and allows tests to override so that it can trigger
		/// events tabControl_Deselecting() and tabControl_SelectedIndexChanged() since
		/// for some reason those events aren't getting triggered in the tests.
		/// </summary>
		/// <param name="index">The index.</param>
		public virtual void SwitchTab(int index)
		{
			CheckDisposed();

			tabControl.SelectedIndex = index;
			switch(index)
			{
				case kWsGeneral:
					{
						m_regionVariantControl.Select();
						break;
					}
				case kWsSorting:
					{
						tabControl.Update();
						m_sortRulesTextBox.Select();
						m_sortRulesTextBox.SelectAll();
						break;
					}
			}
		}

		private void cbDictionaries_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_userChangedSpellCheckDictionary)
			{
				var dictionary = (string) cbDictionaries.SelectedValue;
				CurrentWritingSystem.SpellCheckingId = string.IsNullOrEmpty(dictionary) ? "<None>" : dictionary;
			}
		}

		private void m_sortRulesTextBox_TextChanged(object sender, EventArgs e)
		{
			if (m_userChangedSortRules)
			{
				IWritingSystem ws = CurrentWritingSystem;
				ws.SortRules = !string.IsNullOrEmpty(m_sortRulesTextBox.Text.Trim()) ? m_sortRulesTextBox.Text.Trim() : "";
			}
		}

		private void m_ampersandButton_Click(object sender, EventArgs e)
		{
			m_sortRulesTextBox.Select();
			m_sortRulesTextBox.SelectedText = "&";
		}

		private void m_angleBracketButton_Click(object sender, EventArgs e)
		{
			m_sortRulesTextBox.Select();
			m_sortRulesTextBox.SelectedText = "<";
		}

		private void cbEncodingConverter_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (CurrentWritingSystem == null)
				return;

			// save the selected encoding converter
			var str = cbEncodingConverter.SelectedItem as string;
			if (str == FwCoreDlgs.kstidNone)
				str = null;
			CurrentWritingSystem.LegacyMapping = str;
		}

		private void m_regionVariantControl_ScriptRegionVariantChanged(object sender, EventArgs e)
		{
			if (!m_userChangedVariantControl)
				return;
			//This next assignment updates the DisplayName so it reflects the changes
			//made in the regionVariantControl
			IWritingSystem ws = CurrentWritingSystem;
			m_FullCode.Text = ws.Id;
			SetFullNameLabels(ws.DisplayLabel);
			PopulateRelatedWSsListBox(CurrentWritingSystem);
		}

		private void m_sortUsingComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			IWritingSystem ws = CurrentWritingSystem;
			if (!m_userChangedSortUsing || ws == null)
				return;

			var sortUsing = (WritingSystemDefinition.SortRulesType)Enum.Parse(typeof(WritingSystemDefinition.SortRulesType),
				(string) m_sortUsingComboBox.SelectedValue);
			ws.SortUsing = sortUsing;
			ws.SortRules = "";
			SetupSortTab(ws);
		}

		private void m_sortLanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			IWritingSystem ws = CurrentWritingSystem;
			if (ws != null)
				ws.SortRules = (string) m_sortLanguageComboBox.SelectedValue;
		}

		#endregion
	}
}
