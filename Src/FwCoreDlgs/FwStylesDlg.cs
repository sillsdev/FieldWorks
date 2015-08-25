// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwStylesDlg.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region StyleChangeTypeEnumeration
	/// ------------------------------------------------------------------------------------
	/// <summary>Values indicating what changed as a result of the user actions taken in the
	/// Styles dialog</summary>
	/// ------------------------------------------------------------------------------------
	[Flags]
	public enum StyleChangeType
	{
		/// <summary>Nothing changed</summary>
		None = 0,
		/// <summary>Definition of at least one style changed</summary>
		DefChanged = 1,
		/// <summary>At least one style got renamed or deleted</summary>
		RenOrDel = 2,
		/// <summary>At least one style got added</summary>
		Added = 4,
	}
	#endregion

	#region FwStylesDlg class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The new Styles Dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwStylesDlg : Form, IFWDisposable
	{
		/// <summary>Delegate to set the properties of a StyleInfo to the factory default settings.</summary>
		public Action<StyleInfo> SetPropsToFactorySettings { get; set; }

		/// <summary></summary>
		public delegate void StylesRenOrDelDelegate();
		/// <summary></summary>
		public event StylesRenOrDelDelegate StylesRenamedOrDeleted;

		#region Data Members
		private StyleListBoxHelper m_styleListHelper;
		private StyleInfoTable m_styleTable;
		private FdoCache m_cache;
		private FwStyleSheet m_styleSheet;
		/// <summary></summary>
		protected Set<string> m_deletedStyleNames = new Set<string>();
		/// <summary></summary>
		protected Dictionary<string, string> m_renamedStyles = new Dictionary<string, string>();
		private StyleInfo m_normalStyleInfo;
		private int m_hvoRootObject;

		private int m_customUserLevel = 0;
		private StyleChangeType m_changeType = StyleChangeType.None;
		private IVwRootSite m_rootSite;
		private IApp m_app;
		private MsrSysType m_userMeasurementType;
		private IHelpTopicProvider m_helpTopicProvider;

		private bool m_fShowTEStyleTypes = false;
		private object m_lastStyleTypeEntryForOtherApp;
		private bool m_fOkToSaveTabsToStyle = true;
		#endregion

		#region Constructors and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwStylesDlg"/> class. This version is
		/// required for the Designer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FwStylesDlg()
		{
			InitializeComponent();
			Debug.Assert(m_cboTypes.Items.Count == 5);
			m_lastStyleTypeEntryForOtherApp = m_cboTypes.Items[4];
			m_cboTypes.Items.RemoveAt(4);
			m_btnAdd.Image = ResourceHelper.ButtonMenuArrowIcon;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwStylesDlg"/> class. This version
		/// can be used by C# clients. There is no need for the client to call Init if this
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
		/// <param name="normalStyleName">Name of the normal style.</param>
		/// <param name="customUserLevel">The custom user level.</param>
		/// <param name="userMeasurementType">User's prefered measurement units.</param>
		/// <param name="paraStyleName">Name of the currently selected paragraph style.</param>
		/// <param name="charStyleName">Name of the currently selected character style.</param>
		/// <param name="hvoRootObject">The hvo of the root object in the current view.</param>
		/// <param name="app">The application.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public FwStylesDlg(IVwRootSite rootSite, FdoCache cache, FwStyleSheet styleSheet,
			bool defaultRightToLeft, bool showBiDiLabels, string normalStyleName,
			int customUserLevel, MsrSysType userMeasurementType, string paraStyleName,
			string charStyleName, int hvoRootObject, IApp app, IHelpTopicProvider helpTopicProvider)
			: this()
		{
			m_rootSite = rootSite;
			m_cache = cache;
			m_customUserLevel = customUserLevel;
			m_hvoRootObject = hvoRootObject;
			m_app = app;
			showBiDiLabels |= defaultRightToLeft;
			m_userMeasurementType = userMeasurementType;
			m_helpTopicProvider = helpTopicProvider;

			// Cache is null in tests
			if (cache == null)
				return;

			m_cboTypes.SelectedIndex = 1; // All Styles

			// Load the style information
			m_styleTable = new StyleInfoTable(normalStyleName,
				cache.ServiceLocator.WritingSystemManager);
			m_styleSheet = styleSheet;
			FillStyleTable(m_styleSheet);
			m_normalStyleInfo = (StyleInfo)m_styleTable[normalStyleName];
			Debug.Assert(m_normalStyleInfo != null);
			m_styleListHelper = new StyleListBoxHelper(m_lstStyles);
			m_styleListHelper.AddStyles(m_styleTable, null);
			m_styleListHelper.ShowInternalStyles = true;
			m_styleListHelper.StyleChosen += new StyleChosenHandler(m_styleListHelper_StyleChosen);
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
			CurrentStyle = (!string.IsNullOrEmpty(paraStyleName)) ? paraStyleName : normalStyleName;
		}

		/// <summary>
		/// Run the styles dialog in order to configure styles for a combo box that selects a style.
		/// </summary>
		/// <param name="combo">The combo we are configuring. Items may be StyleComboItem or simple strings (style names)</param>
		/// <param name="fixCombo">An action to run after the dialog closes</param>
		/// <param name="defaultStyle">style to select in the combo if none chosen in the dialog</param>
		/// <param name="stylesheet">that the dialog will configure</param>
		/// <param name="nMaxStyleLevel">optional constraint on which styles show</param>
		/// <param name="hvoAppRoot">root HVO for the application, e.g., Scripture for TE</param>
		/// <param name="cache"></param>
		/// <param name="owner">parent window</param>
		/// <param name="app"></param>
		/// <param name="helpTopicProvider"></param>
		public static void RunStylesDialogForCombo(ComboBox combo, Action fixCombo, string defaultStyle,
			FwStyleSheet stylesheet, int nMaxStyleLevel, int hvoAppRoot, FdoCache cache,
			IWin32Window owner, IApp app, IHelpTopicProvider helpTopicProvider)
		{
			var sci = combo.SelectedItem as StyleComboItem;
			string charStyleName = combo.SelectedItem as string;
			if (sci != null)
				charStyleName = (sci != null && sci.Style != null) ? sci.Style.Name : "";
			var paraStyleName = stylesheet.GetDefaultBasedOnStyleName();
			// Although we call this 'paraStyleName', it's actual function is to determine the style that
			// will be selected in the dialog when it launches. We want that to be the one from the style
			// combo we are editing, whether it's a paragraph or character one.
			if (!string.IsNullOrEmpty(charStyleName))
				paraStyleName = charStyleName;
			// ReSharper disable ConvertToConstant.Local
			bool fRightToLeft = false;
			// ReSharper restore ConvertToConstant.Local
			IVwRootSite site = null;		// Do we need something better?  We don't have anything!
			// ReSharper disable RedundantAssignment
			var selectedStyle = "";
			// ReSharper restore RedundantAssignment
			using (var stylesDlg = new FwStylesDlg(
				site,
				cache,
				stylesheet,
				// ReSharper disable ConditionIsAlwaysTrueOrFalse
				fRightToLeft,
				// ReSharper restore ConditionIsAlwaysTrueOrFalse
				cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				stylesheet.GetDefaultBasedOnStyleName(),
				nMaxStyleLevel,
				app.MeasurementSystem,
				paraStyleName,
				charStyleName,
				hvoAppRoot,
				app,
				helpTopicProvider))
			{
				stylesDlg.ShowTEStyleTypes = false;
				stylesDlg.CanSelectParagraphBackgroundColor = false;
				if (stylesDlg.ShowDialog(owner) == DialogResult.OK &&
					((stylesDlg.ChangeType & StyleChangeType.DefChanged) > 0 ||
					 (stylesDlg.ChangeType & StyleChangeType.Added) > 0))
				{
					app.Synchronize(SyncMsg.ksyncStyle);
					selectedStyle = stylesDlg.SelectedStyle;
					var oldStyle = GetStyleName(combo.SelectedItem);
					if (fixCombo != null)
						fixCombo();
					if (string.IsNullOrEmpty(selectedStyle))
						selectedStyle = defaultStyle;
					// Make the requested change if possible...otherwise restore the previous selction.
					if (!SelectStyle(combo, selectedStyle))
						SelectStyle(combo, oldStyle);
				}
			}
		}

		private static bool SelectStyle(ComboBox combo, string selectedStyle)
		{
			for (var i = 0; i < combo.Items.Count; ++i)
			{
				var comboItem = combo.Items[i];
				string styleName = GetStyleName(comboItem);
				if (styleName != selectedStyle)
					continue;
				combo.SelectedIndex = i;
				return true;
			}
			return false;
		}

		private static string GetStyleName(object comboItem)
		{
			if (comboItem == null)
				return null;
			var styleName = comboItem as string;
			var sci1 = comboItem as StyleComboItem;
			if (sci1 != null && sci1.Style != null)
				styleName = sci1.Style.Name;
			return styleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string CurrentStyle
		{
			set
			{
				m_styleListHelper.SelectedStyleName = value;
				if (m_styleListHelper.SelectedStyle != null)
					m_styleListHelper_StyleChosen(null, m_styleListHelper.SelectedStyle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the style table.
		/// </summary>
		/// <param name="sheet">The style sheet.</param>
		/// ------------------------------------------------------------------------------------
		private void FillStyleTable(FwStyleSheet sheet)
		{
			for (int i = 0; i < sheet.CStyles; i++)
			{
				var style = sheet.get_NthStyleObject(i);
				m_styleTable.Add(style.Name, new StyleInfo(style));
			}
			m_styleTable.ConnectStyles();
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
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the combo box where the user
		/// can select the type of styles to show (all, basic, or custom styles). This combo
		/// box is shown in TE but not in the other apps.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowSelectStyleTypes
		{
			get { return m_pnlTypesCombo.Visible; }
			set { m_pnlTypesCombo.Visible = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to show the TE list of style types (all,
		/// basic, or custom styles), or the FLEx list of style types (all, basic, dictionary,
		/// or custom styles).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowTEStyleTypes
		{
			get { return m_fShowTEStyleTypes; }
			set
			{
				if (m_fShowTEStyleTypes == value)
					return;

				// TE and FLEx need to show a different entry as the last style type in the
				// list, so swap which one is there.
				m_fShowTEStyleTypes = value;
				object temp = m_lastStyleTypeEntryForOtherApp;
				Debug.Assert(m_cboTypes.Items.Count == 4);
				m_lastStyleTypeEntryForOtherApp = m_cboTypes.Items[3];
				m_cboTypes.Items[3] = temp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the user can select a paragraph background
		/// color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanSelectParagraphBackgroundColor
		{
			get { return m_paragraphTab.ShowBackgroundColor; }
			set { m_paragraphTab.ShowBackgroundColor = value; }
		}

		#endregion

		#region Interface methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public int DisplayDialog()
		{
			CheckDisposed();

			return (int)this.ShowDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the change.
		/// </summary>
		/// <value>The type of the change.</value>
		/// ------------------------------------------------------------------------------------
		public StyleChangeType ChangeType
		{
			get { CheckDisposed(); return m_changeType; }
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a style is chosen in the style list
		/// </summary>
		/// <param name="prevStyle">The previous style</param>
		/// <param name="newStyle">The newly selected style</param>
		/// ------------------------------------------------------------------------------------
		void m_styleListHelper_StyleChosen(StyleListItem prevStyle, StyleListItem newStyle)
		{
			if (prevStyle != null)
			{
				// Make sure any previous changes are updated
				if (!prevStyle.IsDefaultParaCharsStyle)
					UpdateChanges((StyleInfo)prevStyle.StyleInfo);

				// If the new style is no longer selected, then select it. This can happen
				// when committing changes for a renamed style
				if (m_styleListHelper.SelectedStyle == null ||
					m_styleListHelper.SelectedStyle.Name != newStyle.Name)
				{
					m_styleListHelper.SelectedStyleName = newStyle.Name;
				}
			}

			Debug.Assert((newStyle.StyleInfo != null && newStyle.StyleInfo is StyleInfo) ||
				newStyle.IsDefaultParaCharsStyle);
			StyleInfo styleInfo = (StyleInfo)newStyle.StyleInfo;

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
				m_tabControl.TabPages.Add(m_tbFont);

			// For character styles, hide the "Paragraph", "Bullets", and "Border" tabs
			if (styleInfo.IsCharacterStyle)
			{
				RemoveParagraphStyleTabs();
			}
			else if (styleInfo.IsParagraphStyle)
				EnsureParagraphStyleTabs();

			UpdateTabsForStyle(styleInfo);

			// Enable/disable the delete button based on the style being built-in
			m_btnDelete.Enabled = !styleInfo.IsBuiltIn;
			m_btnCopy.Enabled = styleInfo.CanInheritFrom;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the controls on each of the tab pages being displayed to reflect the
		/// properties of the specified style.
		/// </summary>
		/// <param name="styleInfo">The style info (for the currently selected style).</param>
		/// ------------------------------------------------------------------------------------
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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the dialog to have default paragraph characters selected
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillForDefaultParagraphCharacters()
		{
			RemoveParagraphStyleTabs();
			if (m_tabControl.TabPages.Contains(m_tbFont))
				m_tabControl.TabPages.Remove(m_tbFont);
			m_btnDelete.Enabled = false;
			m_btnCopy.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboTypes control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_cboTypes_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_styleListHelper == null)
				return;
			if (m_fShowTEStyleTypes)
			{
				m_styleListHelper.ShowOnlyUserModifiedStyles = false;
				switch (m_cboTypes.SelectedIndex)
				{
					case 0:	// basic
						m_styleListHelper.MaxStyleLevel = 0;
						break;

					case 1:	// all
						m_styleListHelper.MaxStyleLevel = Int32.MaxValue;
						break;

					case 2:	// custom
						m_styleListHelper.MaxStyleLevel = m_customUserLevel;
						break;
					case 3:	// user-modified
						m_styleListHelper.MaxStyleLevel = Int32.MaxValue;
						m_styleListHelper.ShowOnlyUserModifiedStyles = true;
						break;
				}
			}
			else
			{
				StyleListItem current = m_styleListHelper.SelectedStyle;
				m_styleListHelper.MaxStyleLevel = Int32.MaxValue;
				m_styleListHelper.ExplicitStylesToDisplay = null;
				List<string> stylesToExclude = GetExcludedFlexStyleNames();
				if (current != null && stylesToExclude.Contains(current.Name))
				{
					UpdateChanges((StyleInfo)current.StyleInfo);
					stylesToExclude = GetExcludedFlexStyleNames();	// in case style name changed in interesting way.
				}
				m_styleListHelper.ExplicitStylesToExclude = stylesToExclude;
			}
			m_styleListHelper.Refresh();
			if (m_lstStyles.SelectedValue == null)
			{
				m_btnDelete.Enabled = false;
				m_btnCopy.Enabled = false;
				// Treat a non-existent style (from an empty list) as a character
				// style -- hide several tab pages, and force the General tab.
				if (m_tabControl.SelectedTab != m_tbGeneral)
					m_tabControl.SelectedTab = m_tbGeneral;
				RemoveParagraphStyleTabs();
			}
		}

		private List<string> GetExcludedFlexStyleNames()
		{
			List<string> styles = new List<string>();
			switch (m_cboTypes.SelectedIndex)
			{
				case 0:	// basic -- use current table of styles to obtain names
					foreach (var style in m_styleTable.Keys)
					{
						if (style.StartsWith("Dictionary") || style.StartsWith("Classified"))
							styles.Add(style);
					}
					break;

				case 1:	// all
					break;

				case 2:	// custom -- styleSheet contains all built-in styles (can't change names)
					foreach (var style in m_styleSheet.Styles)
					{
						if (style.IsBuiltIn)
							styles.Add(style.Name);
					}
					styles.Add(StyleUtils.DefaultParaCharsStyleName);
					break;

				case 3: // dictionary -- use current table of styles to obtain names
					foreach (var style in m_styleTable.Keys)
					{
						if (!style.StartsWith("Dictionary") && !style.StartsWith("Classified"))
							styles.Add(style);
					}
					styles.Add(StyleUtils.DefaultParaCharsStyleName);
					break;
			}
			return styles;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the data in a tab control changes to an unspecified state
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TabDataChangedUnspecified(object sender, EventArgs e)
		{
			if (m_styleListHelper.SelectedStyle == null || !(sender is IStylesTab))
				return;

			// Changes to values that go back to unspecified
			StyleInfo info = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
			IStylesTab tab = (IStylesTab)sender;
			tab.SaveToInfo(info);
			m_styleTable.ConnectStyles();
			tab.UpdateForStyle(info);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Deselecting event of the m_tabControl control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TabControlCancelEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_tabControl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			StyleInfo info = null;
			StyleListItem item = m_styleListHelper.SelectedStyle;
			if (item != null)
				info = (StyleInfo)item.StyleInfo;
			// NOTE: tabDeselecting (e.TabPage) can be null after removing a tabpage from the tabControl
			// hopefully SaveToInfo got called for the tab page being removed.
			IStylesTab tabDeselecting = e.TabPage != null ? (IStylesTab)e.TabPage.Controls[0] : null;
			if (info != null && m_generalTab.StyleName != info.Name &&
				m_styleTable.ContainsKey(m_generalTab.StyleName))
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
					tabDeselecting.SaveToInfo(info);
				m_styleTable.ConnectStyles();
			}
		}

		private void m_tabControl_Selecting(object sender, TabControlCancelEventArgs e)
		{
			StyleInfo info = null;
			StyleListItem item = m_styleListHelper.SelectedStyle;
			if (item != null)
			{
				info = (StyleInfo)item.StyleInfo;
				Debug.Assert(info != null ||
							 (m_tabControl.SelectedTab == m_tbGeneral && item.IsDefaultParaCharsStyle),
					"StyleInfo should only be null for Default Paragraph Characters");
			}
			IStylesTab tab = (IStylesTab)m_tabControl.SelectedTab.Controls[0];
			tab.UpdateForStyle(info);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RequestStyleReconnect event of the m_fontTab control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_fontTab_RequestStyleReconnect(object sender, EventArgs e)
		{
			m_styleTable.ConnectStyles();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnDelete control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnDelete_Click(object sender, EventArgs e)
		{
			// Find the currently selected style
			if (m_styleListHelper.SelectedStyle == null)
				return;
			StyleInfo style = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
			if (style.IsBuiltIn)
				return;

			// If the style is a real style, then save its name in the deleted list
			if (style.RealStyle != null)
				SaveDeletedStyle(style.Name);

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
			StyleListItem selectedStyle = m_styleListHelper.SelectedStyle;
			if (selectedStyle != null)
			{
				m_styleListHelper_StyleChosen(null, selectedStyle);
			}
			else
			{
				m_btnDelete.Enabled = false;
				m_btnCopy.Enabled = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnAdd control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnAdd_Click(object sender, EventArgs e)
		{
			// before adding a new style, save any edits to the current style
			if (m_styleListHelper.SelectedStyle != null)
				UpdateChanges((StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo);

			m_contextMenuAddStyle.Show(m_btnAdd, 0, m_btnAdd.Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnCopy control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnCopy_Click(object sender, EventArgs e)
		{
			// before copying a new style, save any edits to the current style
			if (m_styleListHelper.SelectedStyle != null)
				UpdateChanges((StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo);
			else
				return;

			// Get the selected style
			StyleListItem copiedStyle = m_styleListHelper.SelectedStyle;
			if (copiedStyle.StyleInfo == null)
				return;

			// Generate a name for the copied style.
			int styleNum = 2;
			string styleFormatString = FwCoreDlgs.kstidCopyStyleNameFormat;
			string styleFormatNumberString = FwCoreDlgs.kstidCopyStyleNameFormatNumber;
			string newStyleName = string.Format(styleFormatString, copiedStyle.Name);
			while (m_styleTable.ContainsKey(newStyleName))
				newStyleName = string.Format(styleFormatNumberString, styleNum++, copiedStyle.Name);

			// create a new styleinfo
			StyleInfo newStyle = new StyleInfo(copiedStyle.StyleInfo, newStyleName);

			// add the styleinfo to the style list and the list control
			m_styleTable.Add(newStyleName, newStyle);
			m_styleTable.ConnectStyles();
			m_styleListHelper.Add(newStyle);

			// select the name field
			m_tabControl.SelectedTab = m_tbGeneral;
			m_tbGeneral.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnOk control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
				using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor, m_rootSite, "kstidUndoStyleChanges"))
				{
					// This makes sure the style sheet gets reinitialized after an Undo command.
					if (m_cache.DomainDataByFlid.GetActionHandler() != null)
					{
						m_cache.DomainDataByFlid.GetActionHandler().AddAction(
							new UndoStyleChangesAction(m_app, true));
					}

					// Save any edits from the dialog to the selected style
					if (m_styleListHelper.SelectedStyle != null)
					{
						StyleInfo styleInfo = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
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
						if (style.IsParagraphStyle && !style.IsInternalStyle &&
							(style.Context != style.NextStyle.Context ||
							style.Structure == StructureValues.Body && style.NextStyle.Structure != style.Structure))
						{
							MessageBox.Show(this, string.Format(FwCoreDlgs.kstidStyleContextMismatchMsg, style.NextStyle.Name, style.Name),
								FwCoreDlgs.kstidStyleContextMismatchCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
								m_changeType |= StyleChangeType.DefChanged;
							}
							else
							{
								// otherwise, the style does not exist - it has been added
								// REVIEW: Don't we need to make sure some other user hasn't already
								// added this style before saving it in the DB?
								var newStyle = m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(m_styleSheet.MakeNewStyle());
								style.SaveToDB(newStyle, false, true);
								m_changeType |= StyleChangeType.Added;
							}
						}
					}

					// Save the real styles for based-on and following style. Do this last so
					// all of the real styles for added styles will have been created.
					foreach (StyleInfo style in m_styleTable.Values)
					{
						if (style.Dirty && style.IsValid)
							style.SaveBasedOnAndFollowingToDB();
						style.Dirty = false;
					}

					DeleteAndRenameStylesInDB();

					// Has the user modified any of the styles?
					if (m_changeType > 0)
					{
						if ((m_changeType & StyleChangeType.RenOrDel) > 0)
						{
							// Styles were renamed or deleted.
							if (StylesRenamedOrDeleted != null)
								StylesRenamedOrDeleted();
						}
						else
						{
							// This makes sure the style sheet gets reinitialized after a Redo command.
							if (m_cache.DomainDataByFlid.GetActionHandler() != null)
							{
								m_cache.DomainDataByFlid.GetActionHandler().AddAction(
									new UndoStyleChangesAction(m_app, false));
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
			SelectedStyle = m_styleListHelper.SelectedStyle == null ? "" : m_styleListHelper.SelectedStyle.Name;
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
		/// <returns></returns>
		private bool IsRootSiteDisposed()
		{
#pragma warning disable 0219 // error CS0219: The variable `rootb' is assigned but its value is never used
			IVwRootBox rootb;
			try
			{
				// LT-8767 When this dialog is launched from the Configure Dictionary View dialog
				// m_rootSite == null so we need to handle this to prevent a crash.
				if (m_rootSite == null)
					return false;
				rootb = m_rootSite.RootBox;
			}
			catch (ObjectDisposedException)
			{
				return true;
			}
			return false;
#pragma warning restore 0219
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opening event of the contextMenuStyles control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void contextMenuStyles_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			StyleListItem selectedItem = m_styleListHelper.SelectedStyle;
			if (SetPropsToFactorySettings == null || selectedItem.StyleInfo == null ||
				!(selectedItem.StyleInfo is StyleInfo))
			{
				resetToolStripMenuItem.Enabled = false;
			}
			else
			{
				StyleInfo styleInfo = (StyleInfo)selectedItem.StyleInfo;
				resetToolStripMenuItem.Enabled = (styleInfo.RealStyle != null &&
					styleInfo.RealStyle.IsBuiltIn && styleInfo.IsModified);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			string helpTopic;
			if (sender == helpToolStripMenuItem)
				helpTopic = string.Format("style:{0}", m_lstStyles.SelectedItem);
			else
				helpTopic = string.Format("kstidStylesDialogTab{0}", m_tabControl.SelectedIndex + 1);

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void mnuReset_Click(object sender, EventArgs e)
		{
			StyleListItem selectedItem = m_styleListHelper.SelectedStyle;
			Debug.Assert(selectedItem.StyleInfo != null && selectedItem.StyleInfo is StyleInfo);
			StyleInfo styleInfo = (StyleInfo)selectedItem.StyleInfo;
			Debug.Assert(styleInfo.RealStyle != null && styleInfo.RealStyle.IsBuiltIn &&
				styleInfo.RealStyle.IsModified);
			SetPropsToFactorySettings(styleInfo);
			UpdateTabsForStyle(styleInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseDown event of the styles list. If the user clicks with the right
		/// mouse button we have to select the style.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstStyles_MouseDown(object sender, MouseEventArgs e)
		{
			m_lstStyles.Focus(); // This can fail if validation fails in control that had focus.
			if (m_lstStyles.Focused && e.Button == MouseButtons.Right)
				m_lstStyles.SelectedIndex = m_lstStyles.IndexFromPoint(e.Location);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseUp event of the styles list. If the user clicks with the right
		/// mouse button we have to bring up the context menu if the mouse up event occurs over
		/// the selected style.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstStyles_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (m_lstStyles.IndexFromPoint(e.Location) == m_lstStyles.SelectedIndex)
					contextMenuStyles.Show(m_lstStyles, e.Location);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when font button is clicked on the Bullets tab of the styles dialog.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IFontDialog OnBulletsFontDialog(object sender, EventArgs args)
		{
			return new FwFontDialog(m_helpTopicProvider);
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new style to our internal table of styles (i.e., not yet to the DB) in
		/// response to the user clicking one of the style type submenu items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StyleTypeMenuItem_Click(object sender, EventArgs e)
		{
			bool fParaStyle = (sender == paragraphStyleToolStripMenuItem);

			// generate a name for the new style
			string styleName = fParaStyle ?
				FwCoreDlgs.kstidNewParagraphStyle : FwCoreDlgs.kstidNewCharacterStyle;
			int styleNum = 2;
			string styleFormatString = fParaStyle ?
				FwCoreDlgs.kstidNewParagraphStyleWithNumber :
				FwCoreDlgs.kstidNewCharacterStyleWithNumber;
			while (m_styleTable.ContainsKey(styleName))
				styleName = string.Format(styleFormatString, styleNum++);

			// create a new styleinfo
			StyleInfo style = new StyleInfo(styleName,
				fParaStyle ? m_normalStyleInfo : null,
				fParaStyle ? StyleType.kstParagraph : StyleType.kstCharacter,
				m_cache);

			// add the styleinfo to the style list and the list control
			m_styleTable.Add(style.Name, style);
			m_styleTable.ConnectStyles();
			m_styleListHelper.Add(style);

			// select the name field
			m_tabControl.SelectedTab = m_tbGeneral;
			m_tbGeneral.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves any changes in the dialog for the style
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateChanges(StyleInfo styleInfo)
		{
			if (styleInfo == null)
				return;

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the deleted style name.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// ------------------------------------------------------------------------------------
		protected void SaveDeletedStyle(string styleName)
		{
			// Check if the style got previously renamed
			string oldName = null;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the styles in the delete list. Also, rename instances of styles in the
		/// rename list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DeleteAndRenameStylesInDB()
		{
			if (m_deletedStyleNames.Count == 0 && m_renamedStyles.Keys.Count == 0)
				return;

			var replaceSpec = new Dictionary<string, string>();
			foreach (string styleName in m_deletedStyleNames)
			{
				IStStyle deleteStyle = m_styleSheet.FindStyle(styleName);
				int context = (int)deleteStyle.Context;
				bool fIsCharStyle = deleteStyle.Type == StyleType.kstCharacter;
				m_styleSheet.Delete(deleteStyle.Hvo);

				// Note: instead of delete we replace the old style with the default style
				// for the correct context. Deleting a style always sets the style to "Normal"
				// which is wrong in TE where the a) the default style is "Paragraph" and b)
				// the default style for a specific paragraph depends on the current context
				// (e.g. in an intro paragraph the default paragraph style is "Intro Paragraph"
				// instead of "Paragraph"). This fixes TE-5873.
				string defaultStyleName = m_styleSheet.GetDefaultStyleForContext(
					context, fIsCharStyle);
				replaceSpec[styleName] = defaultStyleName;
			}
			foreach (var kvp in m_renamedStyles)
				replaceSpec[kvp.Value] = kvp.Key;
			StringServices.ReplaceStyles(m_cache, replaceSpec);

			m_changeType |= StyleChangeType.RenOrDel;
		}

		/// <summary>
		/// Makes sure the dialog tabs pertaining to paragraph styles are visible.
		/// </summary>
		private void EnsureParagraphStyleTabs()
		{
			if (!m_tabControl.TabPages.Contains(m_tbParagraph))
				m_tabControl.TabPages.Add(m_tbParagraph);

			if (!m_tabControl.TabPages.Contains(m_tbBullets))
				m_tabControl.TabPages.Add(m_tbBullets);

			if (!m_tabControl.TabPages.Contains(m_tbBorder))
				m_tabControl.TabPages.Add(m_tbBorder);
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

		#endregion
	}
	#endregion // FwStylesDlg class

	#region UndoStyleChangesAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for style sheet changes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoStyleChangesAction : UndoActionBase
	{
		private bool m_fForUndo;
		private IApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the <see cref="UndoStyleChangesAction"/> object.
		/// </summary>
		/// <param name="app">The application</param>
		/// <param name="fForUndo"><c>true</c> for Undo, <c>false</c> for Redo.</param>
		/// ------------------------------------------------------------------------------------
		public UndoStyleChangesAction(IApp app, bool fForUndo)
		{
			m_app = app;
			m_fForUndo = fForUndo;
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			// Inform all the application windows that the user issued an undo command after
			// having applied a style change via the StylesDialog box.
			if (m_fForUndo)
				m_app.Synchronize(SyncMsg.ksyncStyle);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			// Inform all the application windows that the user issued a redo command after
			// an undo command after having applied a style change via the StylesDialog box.
			if (!m_fForUndo)
				m_app.Synchronize(SyncMsg.ksyncStyle);
			return true;
		}

		#endregion
	}
	#endregion
}
