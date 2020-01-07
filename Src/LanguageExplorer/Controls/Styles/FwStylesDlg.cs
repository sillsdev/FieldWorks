// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>
	/// The new Styles Dialog
	/// </summary>
	internal partial class FwStylesDlg : Form
	{
		/// <summary>Delegate to set the properties of a StyleInfo to the factory default settings.</summary>
		public Action<StyleInfo> SetPropsToFactorySettings { get; set; }

		/// <summary />
		public delegate void StylesRenOrDelDelegate();
		/// <summary />
		public event StylesRenOrDelDelegate StylesRenamedOrDeleted;

		#region Data Members
		private StyleListBoxHelper m_styleListHelper;
		private StyleInfoTable m_styleTable;
		private LcmCache m_cache;
		private LcmStyleSheet m_styleSheet;
		/// <summary />
		protected HashSet<string> m_deletedStyleNames = new HashSet<string>();
		/// <summary />
		protected Dictionary<string, string> m_renamedStyles = new Dictionary<string, string>();
		private StyleInfo m_normalStyleInfo;
		private IVwRootSite m_rootSite;
		private IApp m_app;
		private MsrSysType m_userMeasurementType;
		private IHelpTopicProvider m_helpTopicProvider;
		private object m_lastStyleTypeEntryForOtherApp;
		private bool m_fOkToSaveTabsToStyle = true;
		private static string m_oldStyle = "Dictionary-Normal";
		#endregion

		#region Constructors and initialization

		/// <summary />
		private FwStylesDlg()
		{
			InitializeComponent();
			Debug.Assert(m_cboTypes.Items.Count == 5);
			m_lastStyleTypeEntryForOtherApp = m_cboTypes.Items[4];
			m_cboTypes.Items.RemoveAt(4);
			m_btnAdd.Image = ResourceHelper.ButtonMenuArrowIcon;
		}

		/// <summary>
		/// This version can be used by C# clients. There is no need for the client to call Init if this
		/// constructor is used.
		/// </summary>
		/// <param name="rootSite">The root site.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="defaultRightToLeft">Indicates whether current context (typically the
		/// default direction of the view from which this dialog is invoked) is right to left.</param>
		/// <param name="showBiDiLabels">Indicates whether to show labels that are meaningful
		/// for both left-to-right and right-to-left. If <c>defaultRightToLeft</c> is set to
		/// <c>true</c> the passed-in value for this parameter will be ignored and the display
		/// will automatically be BiDi enabled. If this value is false, then simple "Left" and
		/// "Right" labels will be used in the display, rather than "Leading" and "Trailing".</param>
		/// <param name="normalStyleName">Name of the normal style. Selected when the dialog starts if there is no paragraph style.</param>
		/// <param name="userMeasurementType">User's preferred measurement units.</param>
		/// <param name="paraStyleName">Name of the currently selected paragraph style. Selected when the dialog starts.</param>
		/// <param name="charStyleName">Name of the currently selected character style.</param>
		/// <param name="app">The application.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		public FwStylesDlg(IVwRootSite rootSite, LcmCache cache, LcmStyleSheet styleSheet, bool defaultRightToLeft, bool showBiDiLabels, string normalStyleName,
			MsrSysType userMeasurementType, string paraStyleName, string charStyleName, IApp app, IHelpTopicProvider helpTopicProvider)
			: this()
		{
			m_rootSite = rootSite;
			m_cache = cache;
			m_app = app;
			showBiDiLabels |= defaultRightToLeft;
			m_userMeasurementType = userMeasurementType;
			m_helpTopicProvider = helpTopicProvider;
			if (cache == null)
			{
				// Cache is null in tests
				return;
			}
			// All Styles
			m_cboTypes.SelectedIndex = 1;
			// Load the style information
			m_styleTable = new StyleInfoTable(normalStyleName, cache.ServiceLocator.WritingSystemManager);
			m_styleSheet = styleSheet;
			FillStyleTable(m_styleSheet);
			m_normalStyleInfo = (StyleInfo)m_styleTable[normalStyleName];
			Debug.Assert(m_normalStyleInfo != null);
			m_styleListHelper = new StyleListBoxHelper(m_lstStyles);
			m_styleListHelper.AddStyles(m_styleTable, null);
			m_styleListHelper.ShowInternalStyles = true;
			m_styleListHelper.StyleChosen += m_styleListHelper_StyleChosen;
			m_styleListHelper.Refresh();
			// Mark the current styles
			m_styleListHelper.MarkCurrentStyle(paraStyleName);
			m_styleListHelper.MarkCurrentStyle(charStyleName);
			// General tab
			m_generalTab.Application = m_app;
			m_generalTab.StyleListHelper = m_styleListHelper;
			m_generalTab.StyleTable = m_styleTable;
			m_generalTab.ShowBiDiLabels = showBiDiLabels;
			m_generalTab.UserMeasurementType = m_userMeasurementType;
			m_generalTab.RenamedStyles = m_renamedStyles;
			// Load the font information
			m_fontTab.WritingSystemFactory = cache.WritingSystemFactory;
			m_fontTab.FillFontInfo(cache);
			// Disable the background color on the paragraph tab.
			m_paragraphTab.DefaultTextDirectionRtoL = defaultRightToLeft;
			m_paragraphTab.ShowBiDiLabels = showBiDiLabels;
			m_paragraphTab.MeasureType = userMeasurementType;
			m_bulletsTab.DefaultTextDirectionRtoL = defaultRightToLeft;
			m_bulletsTab.StyleSheet = m_styleSheet;
			m_borderTab.DefaultTextDirectionRtoL = defaultRightToLeft;
			m_borderTab.ShowBiDiLabels = showBiDiLabels;
			// Select the current paragraph style in the list (or fall back to Normal)
			CurrentStyle = !string.IsNullOrEmpty(paraStyleName) ? paraStyleName : normalStyleName;
			m_fontTab.StyleDataChanged += OnStyleDataChanged;
			m_paragraphTab.StyleDataChanged += OnStyleDataChanged;
			m_bulletsTab.StyleDataChanged += OnStyleDataChanged;
			m_borderTab.StyleDataChanged += OnStyleDataChanged;
		}

		/// <summary>
		/// Run the styles dialog in order to configure styles for a combo box that selects a style.
		/// </summary>
		/// <param name="combo">The combo we are configuring. Items may be StyleComboItem or simple strings (style names)</param>
		/// <param name="fixCombo">An action to run after the dialog closes</param>
		/// <param name="defaultStyle">style to select in the combo if none chosen in the dialog</param>
		/// <param name="stylesheet">that the dialog will configure</param>
		/// <param name="cache"></param>
		/// <param name="owner">parent window</param>
		/// <param name="app"></param>
		/// <param name="helpTopicProvider"></param>
		/// <param name="setPropsToFactorySettings">Method to be called if user requests to reset a style to factory settings.</param>
		public static void RunStylesDialogForCombo(ComboBox combo, Action fixCombo, string defaultStyle, LcmStyleSheet stylesheet, LcmCache cache,
			IWin32Window owner, IApp app, IHelpTopicProvider helpTopicProvider, Action<StyleInfo> setPropsToFactorySettings)
		{
			var comboStartingSelectedStyle = combo == null ? defaultStyle : GetStyleName(combo.SelectedItem);
			var dialogStartingSelectedStyle = stylesheet.GetDefaultBasedOnStyleName();
			if (!string.IsNullOrEmpty(comboStartingSelectedStyle))
			{
				dialogStartingSelectedStyle = comboStartingSelectedStyle;
			}
			const bool fRightToLeft = false;
			using (var stylesDlg = new FwStylesDlg(null, cache, stylesheet, fRightToLeft, cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				stylesheet.GetDefaultBasedOnStyleName(), app.MeasurementSystem, dialogStartingSelectedStyle, comboStartingSelectedStyle, app, helpTopicProvider))
			{
				stylesDlg.CanSelectParagraphBackgroundColor = false;
				stylesDlg.SetPropsToFactorySettings = setPropsToFactorySettings;
				if (stylesDlg.ShowDialog(owner) == DialogResult.OK && stylesDlg.ChangeType != StyleChangeType.None)
				{
					app.Synchronize(SyncMsg.ksyncStyle);
					var selectedStyle = stylesDlg.SelectedStyle;
					m_oldStyle = comboStartingSelectedStyle;
					fixCombo?.Invoke();
					if (string.IsNullOrEmpty(selectedStyle))
					{
						selectedStyle = defaultStyle;
					}
					// Make the requested change if possible...otherwise restore the previous selection.
					if (combo != null && !SelectStyle(combo, selectedStyle))
					{
						SelectStyle(combo, m_oldStyle);
					}
				}
			}
		}

		private static bool SelectStyle(ComboBox combo, string selectedStyle)
		{
			for (var i = 0; i < combo.Items.Count; ++i)
			{
				var comboItem = combo.Items[i];
				var styleName = GetStyleName(comboItem);
				if (styleName != selectedStyle)
				{
					continue;
				}
				combo.SelectedIndex = i;
				return true;
			}
			return false;
		}

		private static string GetStyleName(object comboItem)
		{
			if (comboItem == null)
			{
				return null;
			}
			var styleName = comboItem as string;
			var sci1 = comboItem as StyleComboItem;
			if (sci1 != null && sci1.Style != null)
			{
				styleName = sci1.Style.Name;
			}
			return styleName;
		}

		/// <summary>
		/// Sets the current style.
		/// </summary>
		private string CurrentStyle
		{
			set
			{
				m_styleListHelper.SelectedStyleName = value;
				if (m_styleListHelper.SelectedStyle != null)
				{
					m_styleListHelper_StyleChosen(null, m_styleListHelper.SelectedStyle);
				}
			}
		}

		/// <summary>
		/// Fills the style table.
		/// </summary>
		private void FillStyleTable(LcmStyleSheet sheet)
		{
			for (var i = 0; i < sheet.CStyles; i++)
			{
				var style = sheet.get_NthStyleObject(i);
				m_styleTable.Add(style.Name, new StyleInfo(style));
			}
			m_styleTable.ConnectStyles();
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets or sets a value indicating whether or not to show the combo box where the user
		/// can select the type of styles to show (all, basic, or custom styles). This combo
		/// box is shown in TE but not in the other apps.
		/// </summary>
		public bool AllowSelectStyleTypes
		{
			get { return m_pnlTypesCombo.Visible; }
			set { m_pnlTypesCombo.Visible = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the user can select a paragraph background
		/// color.
		/// </summary>
		public bool CanSelectParagraphBackgroundColor
		{
			get { return m_paragraphTab.ShowBackgroundColor; }
			set { m_paragraphTab.ShowBackgroundColor = value; }
		}

		#endregion

		#region Interface methods

		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		public int DisplayDialog()
		{
			return (int)ShowDialog();
		}

		/// <summary>
		/// Gets the type of the change.
		/// </summary>
		public StyleChangeType ChangeType { get; private set; } = StyleChangeType.None;
		#endregion

		#region Event handlers

		/// <summary>
		/// Called when a style is chosen in the style list
		/// </summary>
		private void m_styleListHelper_StyleChosen(StyleListItem prevStyle, StyleListItem newStyle)
		{
			if (prevStyle != null)
			{
				// Make sure any previous changes are updated
				if (!prevStyle.IsDefaultParaCharsStyle)
				{
					UpdateChanges((StyleInfo)prevStyle.StyleInfo);
				}
				// If the new style is no longer selected, then select it. This can happen
				// when committing changes for a renamed style
				if (m_styleListHelper.SelectedStyle == null || m_styleListHelper.SelectedStyle.Name != newStyle.Name)
				{
					m_styleListHelper.SelectedStyleName = newStyle.Name;
				}
			}
			Debug.Assert((newStyle.StyleInfo != null && newStyle.StyleInfo is StyleInfo) || newStyle.IsDefaultParaCharsStyle);
			var styleInfo = (StyleInfo)newStyle.StyleInfo;
			// Need to do this BEFORE removing/adding tabs to avoid an unfortunate series of
			// events when switching from a paragraph to a character style
			m_generalTab.UpdateForStyle(styleInfo);
			// If the new style is Default Paragraph Characters then disable everything
			if (newStyle.IsDefaultParaCharsStyle)
			{
				FillForDefaultParagraphCharacters();
				return;
			}
			// If the font tab was taken off for default paragraph characters, then
			// put it back in.
			if (!m_tabControl.TabPages.Contains(m_tbFont))
			{
				m_tabControl.TabPages.Add(m_tbFont);
			}
			// For character styles, hide the "Paragraph", "Bullets", and "Border" tabs
			if (styleInfo.IsCharacterStyle)
			{
				RemoveParagraphStyleTabs();
			}
			else if (styleInfo.IsParagraphStyle)
			{
				EnsureParagraphStyleTabs();
			}
			UpdateTabsForStyle(styleInfo);
			m_btnCopy.Enabled = styleInfo.CanInheritFrom;
			RefreshDeleteAndResetButton();
		}

		private void RefreshDeleteAndResetButton()
		{
			var selectedItem = m_styleListHelper.SelectedStyle;
			// Depending on how we got here (like in the middle of Deleting and then
			// selecting a new style), there might not be a currently selected style.
			// Handle that situation gracefully.
			if (selectedItem == null)
			{
				m_btnDelete.Enabled = false;
				return;
			}
			var styleInfo = (StyleInfo)selectedItem.StyleInfo;
			if (styleInfo == null)
			{
				m_btnDelete.Enabled = false;
				return;
			}
			if (IsStyleUserCreated(styleInfo))
			{
				m_btnDelete.Text = "&Delete";
				m_btnDelete.Enabled = true;
			}
			else
			{
				m_btnDelete.Text = "&Reset";
				m_btnDelete.Enabled = IsCurrentStyleResettable();
			}
		}

		/// <summary>
		/// Is the style created by the user, as opposed to a style that ships with FW?
		/// </summary>
		private static bool IsStyleUserCreated(StyleInfo styleInfo)
		{
			return !styleInfo.IsBuiltIn;
		}

		/// <summary>
		/// Updates the controls on each of the tab pages being displayed to reflect the
		/// properties of the specified style.
		/// </summary>
		/// <param name="styleInfo">The style info (for the currently selected style).</param>
		private void UpdateTabsForStyle(StyleInfo styleInfo)
		{
			m_fontTab.UpdateForStyle(styleInfo, -1);
			// Only update the rest of the tabs if the style is a paragraph style
			if (styleInfo.IsParagraphStyle)
			{
				m_paragraphTab.UpdateForStyle(styleInfo);
				m_bulletsTab.UpdateForStyle(styleInfo);
				m_borderTab.UpdateForStyle(styleInfo);
			}
			m_generalTab.UpdateForStyle(styleInfo);
		}

		/// <summary>
		/// Sets up the dialog to have default paragraph characters selected
		/// </summary>
		private void FillForDefaultParagraphCharacters()
		{
			RemoveParagraphStyleTabs();
			if (m_tabControl.TabPages.Contains(m_tbFont))
			{
				m_tabControl.TabPages.Remove(m_tbFont);
			}
			RefreshDeleteAndResetButton();
			m_btnCopy.Enabled = false;
		}

		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboTypes control.
		/// </summary>
		private void m_cboTypes_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_styleListHelper == null)
			{
				return;
			}
			var current = m_styleListHelper.SelectedStyle;
			m_styleListHelper.MaxStyleLevel = int.MaxValue;
			m_styleListHelper.ExplicitStylesToDisplay = null;
			var stylesToExclude = GetExcludedFlexStyleNames();
			if (current != null && stylesToExclude.Contains(current.Name))
			{
				UpdateChanges((StyleInfo)current.StyleInfo);
				stylesToExclude = GetExcludedFlexStyleNames();  // in case style name changed in interesting way.
			}
			m_styleListHelper.ExplicitStylesToExclude = stylesToExclude;
			m_styleListHelper.Refresh();
			if (m_lstStyles.SelectedValue == null)
			{
				RefreshDeleteAndResetButton();
				m_btnCopy.Enabled = false;
				// Treat a non-existent style (from an empty list) as a character
				// style -- hide several tab pages, and force the General tab.
				if (m_tabControl.SelectedTab != m_tbGeneral)
				{
					m_tabControl.SelectedTab = m_tbGeneral;
				}
				RemoveParagraphStyleTabs();
			}
		}

		private List<string> GetExcludedFlexStyleNames()
		{
			var styles = new List<string>();
			switch (m_cboTypes.SelectedIndex)
			{
				case 0: // basic -- use current table of styles to obtain names
					foreach (var style in m_styleTable.Keys)
					{
						if (style.StartsWith("Dictionary") || style.StartsWith("Classified"))
						{
							styles.Add(style);
						}
					}
					break;
				case 1: // all
					break;
				case 2: // custom -- styleSheet contains all built-in styles (can't change names)
					foreach (var style in m_styleSheet.Styles)
					{
						if (style.IsBuiltIn)
						{
							styles.Add(style.Name);
						}
					}
					styles.Add(StyleUtils.DefaultParaCharsStyleName);
					break;
				case 3: // dictionary -- use current table of styles to obtain names
					foreach (var style in m_styleTable.Keys)
					{
						if (!style.StartsWith("Dictionary") && !style.StartsWith("Classified"))
						{
							styles.Add(style);
						}
					}
					styles.Add(StyleUtils.DefaultParaCharsStyleName);
					break;
			}
			return styles;
		}

		/// <summary>
		/// Called when the data in a tab control changes to an unspecified state
		/// </summary>
		private void TabDataChangedUnspecified(object sender, EventArgs e)
		{
			if (m_styleListHelper.SelectedStyle == null || !(sender is IStylesTab))
			{
				return;
			}
			// Changes to values that go back to unspecified
			var info = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
			var tab = (IStylesTab)sender;
			tab.SaveToInfo(info);
			m_styleTable.ConnectStyles();
			tab.UpdateForStyle(info);
		}

		/// <summary>
		/// Handles the Deselecting event of the m_tabControl control.
		/// </summary>
		private void m_tabControl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			StyleInfo info = null;
			var item = m_styleListHelper.SelectedStyle;
			if (item != null)
			{
				info = (StyleInfo)item.StyleInfo;
			}
			// NOTE: tabDeselecting (e.TabPage) can be null after removing a tabpage from the tabControl
			// hopefully SaveToInfo got called for the tab page being removed.
			var tabDeselecting = (IStylesTab)e.TabPage?.Controls[0];
			if (info != null && m_generalTab.StyleName != info.Name && m_styleTable.ContainsKey(m_generalTab.StyleName))
			{
				// duplicate style name so don't change tab selection from General (TE-6092)
				Debug.Assert(tabDeselecting == null || tabDeselecting == m_tbGeneral.Controls[0]);
				e.Cancel = true;
				return;
			}
			// If we have an empty list, and hence no style, restrict the user to the General tab.
			// (See FWR-3305.)
			if (item == null && m_lstStyles.Items.Count == 0 && tabDeselecting == m_tbGeneral.Controls[0])
			{
				e.Cancel = true;
				return;
			}
			if (tabDeselecting != null)
			{
				if (m_fOkToSaveTabsToStyle && info != null)
				{
					tabDeselecting.SaveToInfo(info);
				}
				m_styleTable.ConnectStyles();
			}
		}

		private void m_tabControl_Selecting(object sender, TabControlCancelEventArgs e)
		{
			StyleInfo info = null;
			var item = m_styleListHelper.SelectedStyle;
			if (item != null)
			{
				info = (StyleInfo)item.StyleInfo;
				Debug.Assert(info != null || m_tabControl.SelectedTab == m_tbGeneral && item.IsDefaultParaCharsStyle, "StyleInfo should only be null for Default Paragraph Characters");
			}
			var tab = (IStylesTab)m_tabControl.SelectedTab.Controls[0];
			tab.UpdateForStyle(info);
		}

		/// <summary>
		/// Handles the RequestStyleReconnect event of the m_fontTab control.
		/// </summary>
		private void m_fontTab_RequestStyleReconnect(object sender, EventArgs e)
		{
			m_styleTable.ConnectStyles();
		}

		/// <summary>
		/// Handles the Click event of the m_btnDelete control.
		/// Note that this contol might be being used for Delete or Reset depending
		/// on the context.
		/// </summary>
		private void m_btnDelete_Click(object sender, EventArgs e)
		{
			// Find the currently selected style
			if (m_styleListHelper.SelectedStyle == null)
			{
				return;
			}
			var style = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
			if (IsStyleUserCreated(style))
			{
				DeleteStyle(style);
				return;
			}
			if (IsCurrentStyleResettable())
			{
				ResetStyle(style);
			}
		}

		private void DeleteStyle(StyleInfo style)
		{
			if (style.IsBuiltIn)
			{
				return;
			}
			// If the style is a real style, then save its name in the deleted list
			if (style.RealStyle != null)
			{
				SaveDeletedStyle(style.Name);
			}
			// Remove the style from the list
			m_styleTable.Remove(style.Name);
			m_styleTable.ConnectStyles();
			m_styleListHelper.Remove(style);
			// Now update the control for the style that is selected after the delete.
			// The StyleChosen event is not raised automatically in this case because
			// the SelectedStyleName property intentionally causes the delegate to be
			// ignored to avoid unwanted side-effects in other situations, such as when
			// the selected item is change programmatically in response to a selection
			// change in a view.
			var selectedStyle = m_styleListHelper.SelectedStyle;
			if (selectedStyle != null)
			{
				m_styleListHelper_StyleChosen(null, selectedStyle);
			}
			else
			{
				m_cboTypes.SelectedIndex = 1; // All Styles
				CurrentStyle = m_oldStyle;
				RefreshDeleteAndResetButton();
				m_btnCopy.Enabled = false;
			}
		}

		/// <summary>
		/// Handles the Click event of the m_btnAdd control.
		/// </summary>
		private void m_btnAdd_Click(object sender, EventArgs e)
		{
			// before adding a new style, save any edits to the current style
			if (m_styleListHelper.SelectedStyle != null)
			{
				UpdateChanges((StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo);
			}
			m_contextMenuAddStyle.Show(m_btnAdd, 0, m_btnAdd.Height);
		}

		/// <summary>
		/// Handles the Click event of the m_btnCopy control.
		/// </summary>
		private void m_btnCopy_Click(object sender, EventArgs e)
		{
			// before copying a new style, save any edits to the current style
			if (m_styleListHelper.SelectedStyle != null)
			{
				UpdateChanges((StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo);
			}
			else
			{
				return;
			}
			// Get the selected style
			var copiedStyle = m_styleListHelper.SelectedStyle;
			if (copiedStyle.StyleInfo == null)
			{
				return;
			}
			// Generate a name for the copied style.
			var styleNum = 2;
			var styleFormatString = FwCoreDlgs.kstidCopyStyleNameFormat;
			var styleFormatNumberString = FwCoreDlgs.kstidCopyStyleNameFormatNumber;
			var newStyleName = string.Format(styleFormatString, copiedStyle.Name);
			while (m_styleTable.ContainsKey(newStyleName))
			{
				newStyleName = string.Format(styleFormatNumberString, styleNum++, copiedStyle.Name);
			}
			// create a new styleinfo
			var newStyle = new StyleInfo(copiedStyle.StyleInfo, newStyleName);
			// add the styleinfo to the style list and the list control
			m_styleTable.Add(newStyleName, newStyle);
			m_styleTable.ConnectStyles();
			m_styleListHelper.Add(newStyle);
			// select the name field
			m_tabControl.SelectedTab = m_tbGeneral;
			m_tbGeneral.Focus();
		}

		/// <summary>
		/// Handles the Click event of the m_btnOk control.
		/// </summary>
		private void m_btnOk_Click(object sender, EventArgs e)
		{
			if (IsRootSiteDisposed())
			{
				MessageBox.Show(FwCoreDlgs.kstidChangesAreLost, FwCoreDlgs.kstidWarning);
				DialogResult = DialogResult.Cancel;
				return;
			}
			using (new WaitCursor(this))
			{
				using (var undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor, m_rootSite, "kstidUndoStyleChanges"))
				{
					// This makes sure the style sheet gets reinitialized after an Undo command.
					if (m_cache.DomainDataByFlid.GetActionHandler() != null)
					{
						m_cache.DomainDataByFlid.GetActionHandler().AddAction(new UndoStyleChangesAction(m_app, true));
					}
					// Save any edits from the dialog to the selected style
					if (m_styleListHelper.SelectedStyle != null)
					{
						var styleInfo = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
						UpdateChanges(styleInfo);
					}
					try
					{
						// Check to make sure new styles are not going to result in duplicates
						// in the database
						m_styleSheet.CheckForDuplicates(m_styleTable);
					}
					catch (IncompatibleStyleExistsException isee)
					{
						MessageBoxUtils.Show(isee.Message, m_app.ApplicationName);
					}
					foreach (StyleInfo style in m_styleTable.Values)
					{
						if (style.IsParagraphStyle && !style.IsInternalStyle && (style.Context != style.NextStyle.Context || style.Structure == StructureValues.Body && style.NextStyle.Structure != style.Structure))
						{
							MessageBox.Show(this, string.Format(FwCoreDlgs.kstidStyleContextMismatchMsg, style.NextStyle.Name, style.Name), FwCoreDlgs.kstidStyleContextMismatchCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
							undoHelper.RollBack = true;
							DialogResult = DialogResult.None;
							CurrentStyle = style.Name;
							return;
						}
					}
					// Save any changed styles to the database
					foreach (StyleInfo style in m_styleTable.Values)
					{
						if (style.Dirty && style.IsValid)
						{
							// If there is already a real style, then the style has changed
							if (style.RealStyle != null)
							{
								style.SaveToDB(style.RealStyle, true, style.IsModified);
								ChangeType |= StyleChangeType.DefChanged;
							}
							else
							{
								// otherwise, the style does not exist - it has been added
								// REVIEW: Don't we need to make sure some other user hasn't already
								// added this style before saving it in the DB?
								var newStyle = m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(m_styleSheet.MakeNewStyle());
								style.SaveToDB(newStyle, false, true);
								ChangeType |= StyleChangeType.Added;
							}
						}
					}
					// Save the real styles for based-on and following style. Do this last so
					// all of the real styles for added styles will have been created.
					foreach (StyleInfo style in m_styleTable.Values)
					{
						if (style.Dirty && style.IsValid)
						{
							style.SaveBasedOnAndFollowingToDB();
						}
						style.Dirty = false;
					}
					DeleteAndRenameStylesInDB();
					// Has the user modified any of the styles?
					if (ChangeType > 0)
					{
						if ((ChangeType & StyleChangeType.RenOrDel) > 0)
						{
							// Styles were renamed or deleted.
							StylesRenamedOrDeleted?.Invoke();
						}
						else
						{
							// This makes sure the style sheet gets reinitialized after a Redo command.
							if (m_cache.DomainDataByFlid.GetActionHandler() != null)
							{
								m_cache.DomainDataByFlid.GetActionHandler().AddAction(new UndoStyleChangesAction(m_app, false));
							}
						}
					}
					else
					{
						// If nothing changed then we just pretend the user pressed Cancel.
						DialogResult = DialogResult.Cancel;
					}
					undoHelper.RollBack = false;
				}
			}
			SelectedStyle = m_styleListHelper.SelectedStyle == null ? string.Empty : m_styleListHelper.SelectedStyle.Name;
		}

		/// <summary>
		/// The style the user selected in the dialog; valid only if OK pressed.
		/// </summary>
		public string SelectedStyle { get; private set; }

		/// <summary>
		/// Check whether our rootsite has changed beneath us.  This can happen if the user
		/// opens both TE and Flex, opens the style dlg in Flex, and then performs certain
		/// operations in TE (while the style dlg is still open) that change the current tool
		/// in Flex.  See LT-8281.
		/// </summary>
		private bool IsRootSiteDisposed()
		{
			IVwRootBox rootb;
			try
			{
				// LT-8767 When this dialog is launched from the Configure Dictionary View dialog
				// m_rootSite == null so we need to handle this to prevent a crash.
				if (m_rootSite == null)
				{
					return false;
				}
				rootb = m_rootSite.RootBox;
			}
			catch (ObjectDisposedException)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Handles the Opening event of the contextMenuStyles control.
		/// </summary>
		private void contextMenuStyles_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			resetToolStripMenuItem.Enabled = IsCurrentStyleResettable();
		}

		/// <summary>
		/// Can this FwStylesDialog reset the currently selected style?
		/// </summary>
		private bool IsCurrentStyleResettable()
		{
			var selectedItem = m_styleListHelper.SelectedStyle;
			if (SetPropsToFactorySettings == null || !(selectedItem.StyleInfo is StyleInfo))
			{
				return false;
			}
			var styleInfo = (StyleInfo)selectedItem.StyleInfo;
			return (styleInfo.RealStyle != null && styleInfo.RealStyle.IsBuiltIn && styleInfo.IsModified);
		}

		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, sender == helpToolStripMenuItem ? $"style:{m_lstStyles.SelectedItem}" : $"kstidStylesDialogTab{m_tabControl.SelectedIndex + 1}");
		}

		/// <summary>
		/// Handles a Click event
		/// </summary>
		private void mnuReset_Click(object sender, EventArgs e)
		{
			var selectedItem = m_styleListHelper.SelectedStyle;
			Debug.Assert(selectedItem.StyleInfo != null && selectedItem.StyleInfo is StyleInfo);
			var styleInfo = (StyleInfo)selectedItem.StyleInfo;
			ResetStyle(styleInfo);
		}

		private void ResetStyle(StyleInfo styleInfo)
		{
			Debug.Assert(styleInfo.RealStyle != null && styleInfo.RealStyle.IsBuiltIn && styleInfo.RealStyle.IsModified);
			SetPropsToFactorySettings(styleInfo);
			UpdateTabsForStyle(styleInfo);
		}

		/// <summary>
		/// Handles the MouseDown event of the styles list. If the user clicks with the right
		/// mouse button we have to select the style.
		/// </summary>
		private void m_lstStyles_MouseDown(object sender, MouseEventArgs e)
		{
			m_lstStyles.Focus(); // This can fail if validation fails in control that had focus.
			if (m_lstStyles.Focused && e.Button == MouseButtons.Right)
			{
				m_lstStyles.SelectedIndex = m_lstStyles.IndexFromPoint(e.Location);
			}
		}

		/// <summary>
		/// Handles the MouseUp event of the styles list. If the user clicks with the right
		/// mouse button we have to bring up the context menu if the mouse up event occurs over
		/// the selected style.
		/// </summary>
		private void m_lstStyles_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && m_lstStyles.IndexFromPoint(e.Location) == m_lstStyles.SelectedIndex)
			{
				contextMenuStyles.Show(m_lstStyles, e.Location);
			}
		}

		/// <summary>
		/// Called when font button is clicked on the Bullets tab of the styles dialog.
		/// </summary>
		private IFontDialog OnBulletsFontDialog(object sender, EventArgs args)
		{
			return new FwFontDialog(m_helpTopicProvider);
		}
		#endregion

		#region Private helper methods

		/// <summary>
		/// Adds a new style to our internal table of styles (i.e., not yet to the DB) in
		/// response to the user clicking one of the style type submenu items.
		/// </summary>
		private void StyleTypeMenuItem_Click(object sender, EventArgs e)
		{
			var fParaStyle = (sender == paragraphStyleToolStripMenuItem);
			// generate a name for the new style
			var styleName = fParaStyle ? FwCoreDlgs.kstidNewParagraphStyle : FwCoreDlgs.kstidNewCharacterStyle;
			var styleNum = 2;
			var styleFormatString = fParaStyle ? FwCoreDlgs.kstidNewParagraphStyleWithNumber : FwCoreDlgs.kstidNewCharacterStyleWithNumber;
			while (m_styleTable.ContainsKey(styleName))
			{
				styleName = string.Format(styleFormatString, styleNum++);
			}
			// create a new styleinfo
			var style = new StyleInfo(styleName, fParaStyle ? m_normalStyleInfo : null, fParaStyle ? StyleType.kstParagraph : StyleType.kstCharacter, m_cache);
			// add the styleinfo to the style list and the list control
			m_styleTable.Add(style.Name, style);
			m_styleTable.ConnectStyles();
			m_styleListHelper.Add(style);
			// select the name field
			m_tabControl.SelectedTab = m_tbGeneral;
			m_tbGeneral.Focus();
		}

		/// <summary>
		/// Saves any changes in the dialog for the style
		/// </summary>
		private void UpdateChanges(StyleInfo styleInfo)
		{
			if (styleInfo == null)
			{
				return;
			}
			// save the changes from the other tabs
			m_generalTab.SaveToInfo(styleInfo);
			m_fontTab.SaveToInfo(styleInfo);
			if (styleInfo.IsParagraphStyle)
			{
				m_paragraphTab.SaveToInfo(styleInfo);
				m_bulletsTab.SaveToInfo(styleInfo);
				m_borderTab.SaveToInfo(styleInfo);
			}
			// do this last
			m_styleTable.ConnectStyles();
		}

		/// <summary>
		/// Saves the deleted style name.
		/// </summary>
		protected void SaveDeletedStyle(string styleName)
		{
			// Check if the style got previously renamed
			string oldName;
			if (m_renamedStyles.TryGetValue(styleName, out oldName))
			{
				// Since it is, we just want to delete the original style name
				// and not do the rename.
				m_renamedStyles.Remove(styleName);
				styleName = oldName;
			}
			// We don't want to put the same style name in twice,
			// but since it is a Set, it will ignore any duplicates.
			m_deletedStyleNames.Add(styleName);
		}

		/// <summary>
		/// Deletes the styles in the delete list. Also, rename instances of styles in the
		/// rename list.
		/// </summary>
		private void DeleteAndRenameStylesInDB()
		{
			if (m_deletedStyleNames.Count == 0 && m_renamedStyles.Keys.Count == 0)
			{
				return;
			}
			var replaceSpec = new Dictionary<string, string>();
			foreach (var styleName in m_deletedStyleNames)
			{
				var deleteStyle = m_styleSheet.FindStyle(styleName);
				var context = (int)deleteStyle.Context;
				var fIsCharStyle = deleteStyle.Type == StyleType.kstCharacter;
				m_styleSheet.Delete(deleteStyle.Hvo);
				// Note: instead of delete we replace the old style with the default style
				// for the correct context. Deleting a style always sets the style to "Normal"
				// which is wrong in TE where the a) the default style is "Paragraph" and b)
				// the default style for a specific paragraph depends on the current context
				// (e.g. in an intro paragraph the default paragraph style is "Intro Paragraph"
				// instead of "Paragraph"). This fixes TE-5873.
				var defaultStyleName = m_styleSheet.GetDefaultStyleForContext(context, fIsCharStyle);
				replaceSpec[styleName] = defaultStyleName;
			}
			foreach (var kvp in m_renamedStyles)
			{
				replaceSpec[kvp.Value] = kvp.Key;
			}
			StringServices.ReplaceStyles(m_cache, replaceSpec);
			ChangeType |= StyleChangeType.RenOrDel;
		}

		/// <summary>
		/// Makes sure the dialog tabs pertaining to paragraph styles are visible.
		/// </summary>
		private void EnsureParagraphStyleTabs()
		{
			if (!m_tabControl.TabPages.Contains(m_tbParagraph))
			{
				m_tabControl.TabPages.Add(m_tbParagraph);
			}
			if (!m_tabControl.TabPages.Contains(m_tbBullets))
			{
				m_tabControl.TabPages.Add(m_tbBullets);
			}
			if (!m_tabControl.TabPages.Contains(m_tbBorder))
			{
				m_tabControl.TabPages.Add(m_tbBorder);
			}
		}

		/// <summary>
		/// Removes from the dialog the tabs pertaining to paragraph styles.
		/// </summary>
		private void RemoveParagraphStyleTabs()
		{
			m_tabControl.TabPages.Remove(m_tbBorder);
			m_tabControl.TabPages.Remove(m_tbBullets);
			m_tabControl.TabPages.Remove(m_tbParagraph);
		}

		/// <summary>
		/// Handles the event of style information being changed by the dialog.
		/// (Like if the user clicks Bold on the Font tab or changes Indentation on the Paragraph tab.)
		/// </summary>
		private void OnStyleDataChanged(object sender, EventArgs args)
		{
			RefreshDeleteAndResetButton();
		}
		#endregion

		/// <summary>
		/// Undo action for style sheet changes
		/// </summary>
		private sealed class UndoStyleChangesAction : UndoActionBase
		{
			private bool m_fForUndo;
			private IApp m_app;

			/// <summary />
			public UndoStyleChangesAction(IApp app, bool fForUndo)
			{
				m_app = app;
				m_fForUndo = fForUndo;
			}

			#region Overrides of UndoActionBase

			/// <summary>
			/// Reverses (or "un-does") an action.
			/// </summary>
			public override bool Undo()
			{
				// Inform all the application windows that the user issued an undo command after
				// having applied a style change via the StylesDialog box.
				if (m_fForUndo)
				{
					m_app.Synchronize(SyncMsg.ksyncStyle);
				}
				return true;
			}

			/// <summary>
			/// Re-applies (or "re-does") an action.
			/// </summary>
			public override bool Redo()
			{
				// Inform all the application windows that the user issued a redo command after
				// an undo command after having applied a style change via the StylesDialog box.
				if (!m_fForUndo)
				{
					m_app.Synchronize(SyncMsg.ksyncStyle);
				}
				return true;
			}

			#endregion
		}
	}
}