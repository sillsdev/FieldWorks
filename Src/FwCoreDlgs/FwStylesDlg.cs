// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwStylesDlg.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region StyleChangeTypeEnumeration
	/// ------------------------------------------------------------------------------------
	/// <summary>Values indicating what changed as a result of the user actions taken in the
	/// Styles dialog</summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(true)]
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

	#region IFwStylesDlg interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for Styles dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("7B53B846-5D4B-4724-B001-B5D0CAE8E122")]
	public interface IFwStylesDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		int DisplayDialog();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwStylesDlg"/> class.
		/// </summary>
		/// <param name="rootSite">The root site.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="hvoStylesOwner">The hvo of the object which owns the style.</param>
		/// <param name="stylesTag">The "flid" in which the styles are owned.</param>
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
		void Init(IVwRootSite rootSite, FdoCache cache, int hvoStylesOwner,
			int stylesTag, bool defaultRightToLeft, bool showBiDiLabels, string normalStyleName,
			int customUserLevel, MsrSysType userMeasurementType, string paraStyleName,
			string charStyleName, int hvoRootObject, IApp app, IHelpTopicProvider helpTopicProvider);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the change.
		/// </summary>
		/// <value>The type of the change.</value>
		/// ------------------------------------------------------------------------------------
		StyleChangeType ChangeType
		{
			get;
		}
	}
	#endregion // IFwStylesDlg interface

	#region FwStylesDlg class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The new Styles Dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.FwStylesDlg")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("CD7118FF-E8C9-4b3f-B3E8-E30D627A42A4")]
	[ComVisible(true)]
	public partial class FwStylesDlg : Form, IFWDisposable, IFwStylesDlg
	{
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
		private UndoTaskHelper m_UndoTaskHelper;
		private IApp m_app;
		private bool m_fShowBidiLabels;
		private MsrSysType m_userMeasurementType;
		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructors and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwStylesDlg"/> class. This version is
		/// intended for use by COM clients, which must also call Init before displaying the
		/// dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStylesDlg()
		{
			InitializeComponent();
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
			Init(rootSite, cache, styleSheet, defaultRightToLeft, showBiDiLabels,
				normalStyleName, customUserLevel, userMeasurementType, paraStyleName, charStyleName,
				hvoRootObject, app, helpTopicProvider);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwStylesDlg"/> class.
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
		protected void Init(IVwRootSite rootSite, FdoCache cache, FwStyleSheet styleSheet,
			bool defaultRightToLeft, bool showBiDiLabels, string normalStyleName,
			int customUserLevel, MsrSysType userMeasurementType, string paraStyleName,
			string charStyleName, int hvoRootObject, IApp app, IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_rootSite = rootSite;
			m_cache = cache;
			m_customUserLevel = customUserLevel;
			m_hvoRootObject = hvoRootObject;
			m_app = app;
			showBiDiLabels |= defaultRightToLeft;
			m_fShowBidiLabels = showBiDiLabels;
			m_userMeasurementType = userMeasurementType;
			m_helpTopicProvider = helpTopicProvider;

			// Cache is null in tests
			if (cache == null)
				return;

			m_cboTypes.SelectedIndex = 1; // All Styles

			// Load the style information
			m_styleTable = new StyleInfoTable(normalStyleName,
				cache.LanguageWritingSystemFactoryAccessor);
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
			m_generalTab.StyleListHelper = m_styleListHelper;
			m_generalTab.StyleTable = m_styleTable;
			m_generalTab.ShowBiDiLabels = showBiDiLabels;
			m_generalTab.UserMeasurementType = m_userMeasurementType;
			m_generalTab.RenamedStyles = m_renamedStyles;

			// Load the font information
			m_fontTab.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
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
			if (!string.IsNullOrEmpty(paraStyleName))
				m_styleListHelper.SelectedStyleName = paraStyleName;
			else
				m_styleListHelper.SelectedStyleName = normalStyleName;
			m_styleListHelper_StyleChosen(null, m_styleListHelper.SelectedStyle);

			// Default is not to show the style type selection combo.
			if (!DesignMode)
				AllowSelectStyleTypes = false;
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
				IStStyle style = sheet.get_NthStyleObject(i);
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
			if (styleInfo.IsCharacterStyle && m_tabControl.TabPages.Contains(m_tbParagraph))
			{
				m_tabControl.TabPages.Remove(m_tbBorder);
				m_tabControl.TabPages.Remove(m_tbBullets);
				m_tabControl.TabPages.Remove(m_tbParagraph);
			}
			else if (styleInfo.IsParagraphStyle && !m_tabControl.TabPages.Contains(m_tbParagraph))
			{
				m_tabControl.TabPages.Add(m_tbParagraph);
				m_tabControl.TabPages.Add(m_tbBullets);
				m_tabControl.TabPages.Add(m_tbBorder);
			}

			m_fontTab.UpdateForStyle(styleInfo, -1);

			// Only update the rest of the tabs if the style is a paragraph style
			if (styleInfo.IsParagraphStyle)
			{
				m_paragraphTab.UpdateForStyle(styleInfo);
				m_bulletsTab.UpdateForStyle(styleInfo);
				m_borderTab.UpdateForStyle(styleInfo);
			}

			// Enable/disable the delete button based on the style being built-in
			m_btnDelete.Enabled = !styleInfo.IsBuiltIn;
			m_btnCopy.Enabled = styleInfo.CanInheritFrom;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the dialog to have default paragraph characters selected
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillForDefaultParagraphCharacters()
		{
			if (m_tabControl.TabPages.Contains(m_tbParagraph))
			{
				m_tabControl.TabPages.Remove(m_tbBorder);
				m_tabControl.TabPages.Remove(m_tbBullets);
				m_tabControl.TabPages.Remove(m_tbParagraph);
			}
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
			}
			m_styleListHelper.Refresh();
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
		/// Handles the Selecting event of the m_tabControl control.
		/// This handles the case of switching to the General tab without changing the
		/// selected style.  Choosing DefaultParagraphCharacters also fires this event
		/// but the previous style is updated by another method, and this method should not
		/// try to update Default Paragraph Characters.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TabControlCancelEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_tabControl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			StyleInfo info = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
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
			if (tabDeselecting != null)
			{
				if (info != null)
					tabDeselecting.SaveToInfo(info);
				m_styleTable.ConnectStyles();
			}
		}

		private void m_tabControl_Selecting(object sender, TabControlCancelEventArgs e)
		{
			StyleInfo info = (StyleInfo) m_styleListHelper.SelectedStyle.StyleInfo;
			Debug.Assert(info != null ||
						 (m_tabControl.SelectedTab == m_tbGeneral &&
						  m_styleListHelper.SelectedStyle.IsDefaultParaCharsStyle),
						 "StyleInfo should only be null for Default Paragraph Characters");
			IStylesTab tab = (IStylesTab) m_tabControl.SelectedTab.Controls[0];
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
				m_styleListHelper_StyleChosen(null, selectedStyle);
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
				// We can't use using() here because we might have to create a new undo task below.
				CreateUndoTaskHelper();
				try
				{
					// This makes sure the style sheet gets reinitialized after an Undo command.
					if (m_cache.ActionHandlerAccessor != null)
					{
						m_cache.ActionHandlerAccessor.AddAction(
							new UndoStyleChangesAction(m_app, m_cache, true));
					}

					// Save any edits from the dialog to the selected style
					if (m_styleListHelper.SelectedStyle != null)
					{
						StyleInfo styleInfo = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
						UpdateChanges(styleInfo);
					}

					// Check to make sure new styles are not going to result in duplicates
					// in the database
					m_styleSheet.CheckForDuplicates(m_styleTable);

					// Save any changed styles to the database
					foreach (StyleInfo style in m_styleTable.Values)
					{
						if (style.Dirty && style.IsValid)
						{
							// If there is already a real style, then the style has changed
							if (style.RealStyle != null)
							{
								style.SaveToDB(style.RealStyle, true);
								m_changeType |= StyleChangeType.DefChanged;
							}
							else
							{
								// otherwise, the style does not exist - it has been added
								// REVIEW: Don't we need to make sure some other user hasn't already
								// added this style before saving it in the DB?
								StStyle newStyle = new StStyle(m_cache, m_styleSheet.MakeNewStyle());
								style.SaveToDB(newStyle, false);
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
							// Because this might involve quite a bit of database interaction, the
							// styles dialog calls Save() before performing the rename/delete. This
							// ends the undo task (it's not undoable), so we can't end it again or
							// add new actions to it
							m_UndoTaskHelper.Dispose();
							m_cache.Save();
							CreateUndoTaskHelper();

							if (StylesRenamedOrDeleted != null)
								StylesRenamedOrDeleted();
						}
						else
						{
							// This makes sure the style sheet gets reinitialized after a Redo command.
							if (m_cache.ActionHandlerAccessor != null)
							{
								m_cache.ActionHandlerAccessor.AddAction(
									new UndoStyleChangesAction(m_app, m_cache, false));
							}
						}
						m_app.Synchronize(new SyncInfo(SyncMsg.ksyncStyle, 0, 0), m_cache);
					}
					else
					{
						// If nothing changed then we just pretend the user pressed Cancel.
						DialogResult = DialogResult.Cancel;
					}
				}
				finally
				{
					m_UndoTaskHelper.Dispose();
					m_UndoTaskHelper = null;
				}
			}
		}

		/// <summary>
		/// Check whether our rootsite has changed beneath us.  This can happen if the user
		/// opens both TE and Flex, opens the style dlg in Flex, and then performs certain
		/// operations in TE (while the style dlg is still open) that change the current tool
		/// in Flex.  See LT-8281.
		/// </summary>
		/// <returns></returns>
		private bool IsRootSiteDisposed()
		{
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
		}

		private void CreateUndoTaskHelper()
		{
			if (m_rootSite == null)
				m_UndoTaskHelper = new UndoTaskHelper(m_cache.MainCacheAccessor, null, "kstidUndoStyleChanges", false);
			else
				m_UndoTaskHelper = new UndoTaskHelper(m_rootSite, "kstidUndoStyleChanges", false);
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
			StyleInfo style = new StyleInfo(styleName, m_normalStyleInfo, fParaStyle ?
				StyleType.kstParagraph : StyleType.kstCharacter, m_cache);

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

			IFwDbMergeStyles styleMerger = FwDbMergeStylesClass.Create();
			Guid syncGuid = m_app.SyncGuid;
			styleMerger.InitializeEx(m_cache.DatabaseAccessor, Logger.Stream, m_hvoRootObject,
				ref syncGuid);
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

				// If the style is "Default Paragraph Characters" (string.Empty), we just
				// want to remove the named style from the properties rather than replace it.
				if (string.IsNullOrEmpty(defaultStyleName))
					styleMerger.AddStyleDeletion(styleName);
				else
					styleMerger.AddStyleReplacement(styleName, defaultStyleName);
			}
			foreach (string newName in m_renamedStyles.Keys)
				styleMerger.AddStyleReplacement(m_renamedStyles[newName], newName);

			styleMerger.Process((uint)Handle.ToInt32());

			m_changeType |= StyleChangeType.RenOrDel;
		}
		#endregion

		#region IFwStylesDlg Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		int IFwStylesDlg.DisplayDialog()
		{
			return DisplayDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwStylesDlg"/> class.
		/// </summary>
		/// <param name="rootSite">The root site.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="hvoStylesOwner">The hvo of the object which owns the style.</param>
		/// <param name="stylesTag">The "flid" in which the styles are owned.</param>
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
		void IFwStylesDlg.Init(IVwRootSite rootSite, FdoCache cache, int hvoStylesOwner,
			int stylesTag, bool defaultRightToLeft, bool showBiDiLabels, string normalStyleName,
			int customUserLevel, MsrSysType userMeasurementType, string paraStyleName,
			string charStyleName, int hvoRootObject, IApp app, IHelpTopicProvider helpTopicProvider)
		{
			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(cache, hvoStylesOwner, stylesTag, true);

			Init(rootSite, cache, styleSheet, defaultRightToLeft, showBiDiLabels,
				normalStyleName, customUserLevel, userMeasurementType, paraStyleName,
				charStyleName, hvoRootObject, app, helpTopicProvider);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the change.
		/// </summary>
		/// <value>The type of the change.</value>
		/// ------------------------------------------------------------------------------------
		StyleChangeType IFwStylesDlg.ChangeType
		{
			get { return ChangeType; }
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
		private FdoCache m_cache;
		private bool m_fForUndo;
		private IApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the <see cref="UndoStyleChangesAction"/> object.
		/// </summary>
		/// <param name="app">The application</param>
		/// <param name="cache"></param>
		/// <param name="fForUndo"><c>true</c> for Undo, <c>false</c> for Redo.</param>
		/// ------------------------------------------------------------------------------------
		public UndoStyleChangesAction(IApp app, FdoCache cache, bool fForUndo)
		{
			m_app = app;
			m_fForUndo = fForUndo;
			m_cache = cache;
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			// Inform all the application windows that the user issued an undo command after
			// having applied a style change via the StylesDialog box.
			if (m_fForUndo)
				m_app.Synchronize(new SyncInfo(SyncMsg.ksyncStyle, 0, 0), m_cache);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			// Inform all the application windows that the user issued a redo command after
			// an undo command after having applied a style change via the StylesDialog box.
			if (!m_fForUndo)
				m_app.Synchronize(new SyncInfo(SyncMsg.ksyncStyle, 0, 0), m_cache);
			return true;
		}

		#endregion
	}
	#endregion
}
