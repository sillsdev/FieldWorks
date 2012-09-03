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
// File: BaseStyleListHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>Represents the method that will handle the StyleChosen event.</summary>
	public delegate void StyleChosenHandler(StyleListItem prevStyle,
		StyleListItem newStyle);

	/// <summary>Represents the method that will handle requests for a view's current style
	/// </summary>
	public delegate string GetCurrentStyleNameHandler(StyleType type);

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class BaseStyleListHelper: IDisposable
	{
		/// <summary>Occurs when a style is chosen from the list.</summary>
		public event StyleChosenHandler StyleChosen;

		/// <summary>Occurs when the drop-down items are drawn and the list needs to know
		/// what character and paragraph styles are current in a view's selection. (Typically
		/// a rootsite will subscribe to this event.)
		/// </summary>
		public event GetCurrentStyleNameHandler GetCurrentStyleName;

		#region Member variables
		/// <summary></summary>
		protected Control m_ctrl;

		/// <summary>The character style icon for unselected items</summary>
		private Bitmap m_charStyleIcon;
		/// <summary>The paragraph style icon for unselected items</summary>
		private Bitmap m_paraStyleIcon;
		/// <summary>The data property style icon for unselected items</summary>
		private Bitmap m_dataPropStyleIcon;
		/// <summary>The character style icon for selected items</summary>
		private Bitmap m_selectedCharStyleIcon;
		/// <summary>The paragraph style icon for selected items</summary>
		private Bitmap m_selectedParaStyleIcon;
		/// <summary>The data property style icon for selected items</summary>
		private Bitmap m_selectedDataPropStyleIcon;

		/// <summary>The character style icon for unselected item when the item
		/// represents the style of the currently selected text.</summary>
		private Bitmap m_currCharStyleIcon;
		/// <summary>The paragraph style icon for unselected item when the item
		/// represents the style of the currently selected text.</summary>
		private Bitmap m_currParaStyleIcon;
		/// <summary>The character style icon for selected item when the item
		/// represents the style of the currently selected text.</summary>
		private Bitmap m_currSelectedCharStyleIcon;
		/// <summary>The character style icon for selected item when the item
		/// represents the style of the currently selected text.</summary>
		private Bitmap m_currSelectedParaStyleIcon;

		private Bitmap m_origCharStyleIcon;
		private Bitmap m_origParaStyleIcon;
		private Bitmap m_origDataPropStyleIcon;
		private Bitmap m_origCurrCharStyleIcon;
		private Bitmap m_origCurrParaStyleIcon;
		/// <summary></summary>
		protected bool m_ignoreListRefresh = false;

		/// <summary>A stylesheet to get the styles from</summary>
		private FwStyleSheet m_styleSheet = null;

		/// <summary>
		/// The cache from which to create new StStyle objects used for the style list items.
		/// </summary>
		private FdoCache m_cache = null;

		/// <summary>True to show internal styles, false otherwise</summary>
		protected bool m_showInternalStyles = false;
		/// <summary>True to show only user-modified styles, false otherwise</summary>
		protected bool m_showOnlyUserModifiedStyles = false;

		/// <summary>
		/// Stores only what style types will be shown in the list. When this value is
		/// kstLim, all style types are shown.
		/// </summary>
		private StyleType m_typeFilter = StyleType.kstLim;

		/// <summary>
		/// Stores a collection of style contexts (i.e. ContextValues) to exclude from the list.
		/// </summary>
		private List<ContextValues> m_excludedContexts;

		/// <summary>
		/// Stores a collection of style functions (i.e. FunctionValues) to exclude from the list.
		/// </summary>
		private List<FunctionValues> m_excludedFunctions;

		/// <summary>
		/// Stores a collection of style contexts (i.e. ContextValues) to include in the list.
		/// </summary>
		private List<ContextValues> m_includedContexts;
		/// <summary>
		/// Stores a list of explicit stylenames to include in the displayed list. If this is
		/// set, then all other included/excluded contexts, functions, etc. are ignored and this
		/// list overrides the normal filtering. If this is null, then normal filtering happens.
		/// </summary>
		private List<string> m_explicitStylesToDisplay;
		/// <summary>
		/// Stores a list of explicit stylenames to exclude from the displayed list.  When this
		/// is set, the m_explicitStylesToDisplay is cleared, and vice versa.  If this is set,
		/// it overrides the normal filtering.  If this is null (and m_explicitStylesToDisplay
		/// is null), the normal filtering happens.
		/// </summary>
		private List<string> m_explicitStylesToExclude;
		/// <summary>
		/// Flag controlling how included contexts will be processed. If true, all style
		/// contexts included will be unioned with filtered types. Otherwise, an intersection
		/// is created.
		/// </summary>
		private bool m_unionIncludeAndTypeFilter = false;

		private StyleListItem m_prevStyle = null;
		private StyleListItem[] m_prevList;
		/// <summary></summary>
		protected bool m_ignoreChosenDelegate = false;
		/// <summary></summary>
		protected Dictionary<string, StyleListItem> m_styleItemList;
		/// <summary></summary>
		private string m_currParaStyleName = string.Empty;

		/// <summary></summary>
		private string m_currCharStyleName = string.Empty;

		private Control m_activeView;
		private int m_maxStyleLevel = int.MaxValue;

		private bool m_showUserDefinedStyles = true;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a new BaseStyleListHelper for the given control.
		/// </summary>
		/// <param name="ctrl">the given control</param>
		/// ------------------------------------------------------------------------------------
		public BaseStyleListHelper(Control ctrl)
		{
			Debug.Assert(ctrl != null);
			m_ctrl = ctrl;

			m_ctrl.SystemColorsChanged += new EventHandler(CtrlSystemColorsChanged);

			m_origCharStyleIcon = new Bitmap(ResourceHelper.CharStyleIcon);
			m_origParaStyleIcon = new Bitmap(ResourceHelper.ParaStyleIcon);
			m_origDataPropStyleIcon = new Bitmap(ResourceHelper.DataPropStyleIcon);
			m_origCurrCharStyleIcon = new Bitmap(ResourceHelper.SelectedCharStyleIcon);
			m_origCurrParaStyleIcon = new Bitmap(ResourceHelper.SelectedParaStyleIcon);

			// Forces the icon bitmaps to get created.
			CtrlSystemColorsChanged(null, null);

			m_charStyleIcon.MakeTransparent(Color.Magenta);
			m_paraStyleIcon.MakeTransparent(Color.Magenta);
			m_dataPropStyleIcon.MakeTransparent(Color.Magenta);
			m_selectedCharStyleIcon.MakeTransparent(Color.Magenta);
			m_selectedParaStyleIcon.MakeTransparent(Color.Magenta);
			m_selectedDataPropStyleIcon.MakeTransparent(Color.Magenta);
			m_currCharStyleIcon.MakeTransparent(Color.Magenta);
			m_currParaStyleIcon.MakeTransparent(Color.Magenta);
			m_currSelectedCharStyleIcon.MakeTransparent(Color.Magenta);
			m_currSelectedParaStyleIcon.MakeTransparent(Color.Magenta);
		}
		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~BaseStyleListHelper()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_charStyleIcon != null)
					m_charStyleIcon.Dispose();
				if (m_paraStyleIcon != null)
					m_paraStyleIcon.Dispose();
				if (m_dataPropStyleIcon != null)
					m_dataPropStyleIcon.Dispose();
				if (m_selectedCharStyleIcon != null)
					m_selectedCharStyleIcon.Dispose();
				if (m_selectedParaStyleIcon != null)
					m_selectedParaStyleIcon.Dispose();
				if (m_selectedDataPropStyleIcon != null)
					m_selectedDataPropStyleIcon.Dispose();
				if (m_currCharStyleIcon != null)
					m_currCharStyleIcon.Dispose();
				if (m_currCharStyleIcon != null)
					m_currParaStyleIcon.Dispose();
				if (m_currSelectedCharStyleIcon != null)
					m_currSelectedCharStyleIcon.Dispose();
				if (m_currSelectedParaStyleIcon != null)
					m_currSelectedParaStyleIcon.Dispose();
				if (m_origCharStyleIcon != null)
					m_origCharStyleIcon.Dispose();
				if (m_origParaStyleIcon != null)
					m_origParaStyleIcon.Dispose();
				if (m_origDataPropStyleIcon != null)
					m_origDataPropStyleIcon.Dispose();
				if (m_origCurrCharStyleIcon != null)
					m_origCurrCharStyleIcon.Dispose();
				if (m_origCurrParaStyleIcon != null)
					m_origCurrParaStyleIcon.Dispose();
			}
			m_charStyleIcon = null;
			m_paraStyleIcon = null;
			m_dataPropStyleIcon = null;
			m_selectedCharStyleIcon = null;
			m_selectedParaStyleIcon = null;
			m_selectedDataPropStyleIcon = null;
			m_currCharStyleIcon = null;
			m_currCharStyleIcon = null;
			m_currSelectedCharStyleIcon = null;
			m_currSelectedParaStyleIcon = null;
			m_origCharStyleIcon = null;
			m_origParaStyleIcon = null;
			m_origDataPropStyleIcon = null;
			m_origCurrCharStyleIcon = null;
			m_origCurrParaStyleIcon = null;
			IsDisposed = true;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this instance is supposed to be showing internal styles, either because
		/// client explicitly requested them to be shown (as in the styles dialog) or because
		/// the list of included contexts includes internal styles (as when an internal style is
		/// the currently selected style in a view).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ShowingInternalStyles
		{
			get
			{
				return m_showInternalStyles || (m_includedContexts != null &&
					m_includedContexts.Contains(ContextValues.Internal));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True to allow showing internal styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowInternalStyles
		{
			set { m_showInternalStyles = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets a value to ignore all updates to the list of styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IgnoreListRefresh
		{
			get {return m_ignoreListRefresh;}
			set {m_ignoreListRefresh = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the SelectedItem property from the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public abstract StyleListItem SelectedStyle
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Items property from the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		protected abstract ICollection Items
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control's selected style by name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public abstract string SelectedStyleName
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public FwStyleSheet StyleSheet
		{
			get	{return m_styleSheet;}
			set	{m_styleSheet = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the FDO cache from which StStyle objects will be
		/// created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get {return m_cache;}
			set	{m_cache = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating what type of styles are shown in the list. By
		/// default this property is set to StyleType.kstLim which will show styles of
		/// all types. (Note: for this property to have any effect, it must be set before
		/// a call to the AddStyles method.) This property is used in addition to the
		/// <see cref="ExcludeStylesWithContext"/> or <see cref="IncludeStylesWithContext"/>
		/// properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleType ShowOnlyStylesOfType
		{
			get {return m_typeFilter;}
			set {m_typeFilter = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets flag controlling how included contexts will be processed. If true, all
		/// style contexts included will be unioned with filtered types. Otherwise, an
		/// intersection is created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool UnionIncludeAndTypeFilter
		{
			get {return m_unionIncludeAndTypeFilter;}
			set {m_unionIncludeAndTypeFilter = value;}
		}

#if __MonoCS__
#pragma warning disable 419 // ambiguous reference; mono bug #639867
#endif
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a List containing a list of ContextValues of styles to exclude from
		/// the list.
		/// </summary>
		/// <remarks>
		/// (Note: for this property to have any effect, it must be set before
		/// a call to the
		/// <see cref="AddStyles(FwStyleSheet)"/>
		/// or <see cref="Refresh"/> methods.) This property is used in addition to the
		/// <see cref="ShowOnlyStylesOfType"/> property.
		/// When this property is set to something other than null, the
		/// <see cref="IncludeStylesWithContext"/> list is automatically cleared.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public List<ContextValues> ExcludeStylesWithContext
		{
			get
			{
				if (m_excludedContexts == null)
					m_excludedContexts = new List<ContextValues>();

				return m_excludedContexts;
			}
			set
			{
				if (value != null)
					m_includedContexts = null;

				m_excludedContexts = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a List containing a list of FunctionValues of styles to exclude
		/// from the list.
		/// </summary>
		/// <remarks>(Note: for this property to have any effect, it must be set before
		/// a call to the
		/// <see cref="AddStyles(FwStyleSheet)"/>
		/// or <see cref="Refresh"/> methods.) This property is used in addition to the
		/// <see cref="ShowOnlyStylesOfType"/> property.
		/// </remarks>
		/// <value>The exclude styles with function.</value>
		/// ------------------------------------------------------------------------------------
		public List<FunctionValues> ExcludeStylesWithFunction
		{
			get
			{
				if (m_excludedFunctions == null)
					m_excludedFunctions = new List<FunctionValues>();

				return m_excludedFunctions;
			}
			set
			{
				m_excludedFunctions = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a List containing a list of ContextValues of styles to include in
		/// the list. (Note: for this property to have any effect, it must be set before
		/// a call to the
		/// <see cref="AddStyles(FwStyleSheet)"/>
		/// or <see cref="Refresh"/> methods.) This property is used in addition to the
		/// <see cref="ShowOnlyStylesOfType"/> property. When this property is set to something
		/// other than null, the <see cref="ExcludeStylesWithContext"/> list is automatically
		/// cleared.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ContextValues> IncludeStylesWithContext
		{
			get
			{
				if (m_includedContexts == null)
					m_includedContexts = new List<ContextValues>();

				return m_includedContexts;
			}
			set
			{
				if (value != null)
					m_excludedContexts = null;

				m_includedContexts = value;
			}
		}
#if __MonoCS__
#pragma warning restore 419 // ambiguous reference; mono bug #639867
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a list of explicit stylenames to include in the displayed list. If this
		/// is set, then all other included/excluded contexts, functions, etc. are ignored and
		/// this list overrides the normal filtering. If this is null, then normal filtering
		/// happens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<string> ExplicitStylesToDisplay
		{
			get {return m_explicitStylesToDisplay;}
			set
			{
				m_explicitStylesToDisplay = value;
				m_explicitStylesToExclude = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stores a list of explicit stylenames to exclude from the displayed list.  When this
		/// is set, the m_explicitStylesToDisplay is cleared, and vice versa.  If this is set,
		/// it overrides the normal filtering.  If this is null (and m_explicitStylesToDisplay
		/// is null), the normal filtering happens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<string> ExplicitStylesToExclude
		{
			get { return m_explicitStylesToExclude; }
			set
			{
				m_explicitStylesToExclude = value;
				m_explicitStylesToDisplay = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Active View.  This is used to know which view has setup the
		/// contents of the style helper.  The view can know if is has been changed by another
		/// view if this property is not itself.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control ActiveView
		{
			get { return m_activeView; }
			set { m_activeView = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// maximum style level to include
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MaxStyleLevel
		{
			get { return m_maxStyleLevel; }
			set
			{
				if (m_maxStyleLevel != value)
				{
					m_maxStyleLevel = value;
					Refresh();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a filter to show the user-defined styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowUserDefinedStyles
		{
			get { return m_showUserDefinedStyles; }
			set { m_showUserDefinedStyles = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether to show only user-defined styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowOnlyUserModifiedStyles
		{
			get { return m_showOnlyUserModifiedStyles; }
			set { m_showOnlyUserModifiedStyles = value; }
		}
		#endregion

		#region Control Delegate Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look for Enter keys to select the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void StyleListHelper_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\r')
				CtrlSelectedIndexChanged(sender, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the combo drop down event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void ComboDropDown(object sender, EventArgs e)
		{
			m_currParaStyleName = string.Empty;
			m_currCharStyleName = string.Empty;

			if (GetCurrentStyleName != null)
			{
				m_currParaStyleName = GetCurrentStyleName(StyleType.kstParagraph);
				m_currCharStyleName = GetCurrentStyleName(StyleType.kstCharacter);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the selected style changes, apply the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CtrlSelectedIndexChanged(object sender, EventArgs e)
		{
			if (SelectedStyle == null &&
				(m_currParaStyleName != string.Empty || m_currCharStyleName != string.Empty))
			{
				SelectedStyleName = m_currParaStyleName == string.Empty ? m_currCharStyleName :
					m_currParaStyleName;

				Debug.Assert(SelectedStyle != null);
			}

			if (SelectedStyle == null)
				return;

			if (StyleChosen != null && m_ignoreChosenDelegate == false)
				StyleChosen(m_prevStyle, SelectedStyle);

			m_prevStyle = SelectedStyle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void CtrlSystemColorsChanged(object sender, EventArgs e)
		{
			m_charStyleIcon = new Bitmap(m_origCharStyleIcon);
			m_paraStyleIcon = new Bitmap(m_origParaStyleIcon);
			m_dataPropStyleIcon = new Bitmap(m_origDataPropStyleIcon);
			m_selectedCharStyleIcon = new Bitmap(m_origCharStyleIcon);
			m_selectedParaStyleIcon = new Bitmap(m_origParaStyleIcon);
			m_selectedDataPropStyleIcon = new Bitmap(m_origDataPropStyleIcon);
			m_currCharStyleIcon = new Bitmap(m_origCurrCharStyleIcon);
			m_currParaStyleIcon = new Bitmap(m_origCurrParaStyleIcon);
			m_currSelectedCharStyleIcon = new Bitmap(m_origCurrCharStyleIcon);
			m_currSelectedParaStyleIcon = new Bitmap(m_origCurrParaStyleIcon);

			SetBmpColor(m_charStyleIcon, SystemColors.WindowText);
			SetBmpColor(m_paraStyleIcon, SystemColors.WindowText);
			SetBmpColor(m_dataPropStyleIcon, SystemColors.WindowText);
			SetBmpColor(m_selectedCharStyleIcon, SystemColors.HighlightText);
			SetBmpColor(m_selectedParaStyleIcon, SystemColors.HighlightText);
			SetBmpColor(m_selectedDataPropStyleIcon, SystemColors.HighlightText);
			SetBmpColor(m_currCharStyleIcon, SystemColors.WindowText);
			SetBmpColor(m_currParaStyleIcon, SystemColors.WindowText);
			SetBmpColor(m_currSelectedCharStyleIcon, SystemColors.HighlightText);
			SetBmpColor(m_currSelectedParaStyleIcon, SystemColors.HighlightText);

			m_ctrl.Invalidate();
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the given style as a "current" style. This will allow it to be indicated in
		/// the list as one of the current styles.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// ------------------------------------------------------------------------------------
		public void MarkCurrentStyle(string styleName)
		{
			if (string.IsNullOrEmpty(styleName))
				return;
			if (m_styleItemList.ContainsKey(styleName))
			{
				StyleListItem item = m_styleItemList[styleName];
				switch (item.Type)
				{
					case StyleType.kstParagraph:
						m_currParaStyleName = styleName;
						break;
					case StyleType.kstCharacter:
						m_currCharStyleName = styleName;
						break;
					default:
						break;
				}
				item.IsCurrentStyle = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an StStyle object given a style name.
		/// </summary>
		/// <param name="styleName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle StyleFromName(string styleName)
		{
			//TE-5609 Prevent crash in case of missing style by using TryGetValue.
			StyleListItem item = null;
			if (m_styleItemList.TryGetValue(styleName, out item))
			{
				Debug.Assert(item.StyleInfo.RealStyle != null);
				return item.StyleInfo.RealStyle;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the styles in the m_ctrl list based on the ExcludeStylesWithContext and
		/// ShowOnlyStylesOfType properties. This should be called when the caller wants to
		/// update the m_ctrl list after changing one of those two properties, but doesn't want
		/// to rebuild the entire m_styleItemList.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Refresh()
		{
			if (m_styleItemList != null) // Added JohnT for robustness and to keep tests working.
			{
				// get the list of items into an array list that can be sorted
				List<StyleListItem> itemsList = new List<StyleListItem>(m_styleItemList.Values.Where(OkToAddItem));

				// If the list contains the default paragraph characters style then remove it
				// from the list so it can be removed from the list while sorting.
				StyleListItem defaultParaCharsStyle = itemsList.FirstOrDefault(
					item => item.Name == ResourceHelper.DefaultParaCharsStyleName);

				if (defaultParaCharsStyle != null)
					itemsList.Remove(defaultParaCharsStyle);

				// Sort the list, add the default paragraph chars style back in at the top of
				// the list, and add all of the items to the combo box.
				itemsList.Sort();
				if (defaultParaCharsStyle != null)
					itemsList.Insert(0, defaultParaCharsStyle);

				StyleListItem[] newItems = itemsList.ToArray();
				if (m_prevList == null || !newItems.SequenceEqual(m_prevList))
				{
					UpdateStyleList(newItems);
					m_prevList = newItems;
					if (newItems.Length == 0 || !itemsList.Contains(m_prevStyle))
						m_prevStyle = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the list of items in the control to be the specified list of items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract void UpdateStyleList(StyleListItem[] items);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the style names to the combo box or list box from the specified stylesheet.
		/// </summary>
		/// <param name="styleSheet">Stylesheet from which styles are read.</param>
		/// ------------------------------------------------------------------------------------
		public void AddStyles(FwStyleSheet styleSheet)
		{
			AddStyles(styleSheet, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the style names to the combo box or list box from the specified stylesheet.
		/// </summary>
		/// <param name="styleSheet">Stylesheet from which styles are read.</param>
		/// <param name="pseudoStyles">Array of strings representing pseudo-styles that can be
		/// displayed for the purpose of mapping markers to data properties</param>
		/// ------------------------------------------------------------------------------------
		public void AddStyles(FwStyleSheet styleSheet, string[] pseudoStyles)
		{
			Debug.Assert(styleSheet != null);
			BuildStyleItemList(styleSheet.CStyles, styleSheet.Styles, pseudoStyles);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the styles.
		/// </summary>
		/// <param name="styleTable">The style table.</param>
		/// <param name="pseudoStyles">The pseudo styles.</param>
		/// ------------------------------------------------------------------------------------
		public void AddStyles(StyleInfoTable styleTable, string[] pseudoStyles)
		{
			Debug.Assert(styleTable != null);
			BuildStyleItemList(styleTable.Count, styleTable.Values, pseudoStyles);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified new style.
		/// </summary>
		/// <param name="newStyle">The new style.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Add(BaseStyleInfo newStyle)
		{
			m_styleItemList.Add(newStyle.Name, new StyleListItem(newStyle));
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Renames a style.
		/// </summary>
		/// <param name="oldName">The old name.</param>
		/// <param name="newName">The new name.</param>
		/// ------------------------------------------------------------------------------------
		public void Rename(string oldName, string newName)
		{
			StyleListItem style = m_styleItemList[oldName];
			m_styleItemList.Remove(oldName);
			style.StyleInfo.Name = newName;
			m_styleItemList.Add(newName, style);
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified style from the list
		/// </summary>
		/// <param name="style">The style.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Remove(BaseStyleInfo style)
		{
			m_styleItemList.Remove(style.Name);
			Refresh();
		}
		#endregion

		#region Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the pseudo styles.
		/// </summary>
		/// <param name="pseudoStyles">The pseudo styles.</param>
		/// ------------------------------------------------------------------------------------
		private void AddPseudoStyles(string[] pseudoStyles)
		{
			if (pseudoStyles == null)
				return;
			foreach (string sPseudoStyle in pseudoStyles)
			{
				m_styleItemList.Add(sPseudoStyle,
					StyleListItem.CreateDataPropertyItem(sPseudoStyle));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stores all the available styles from the style sheet. Later, the list is used to
		/// fill the combo or list box with the appropriate styles.
		/// </summary>
		/// <param name="cStyles">Number of styles in <paramref name="styleInfos"/></param>
		/// <param name="styleInfos">The collection of style infos.</param>
		/// <param name="pseudoStyles">Array of strings representing pseudo-styles that can be
		/// displayed for the purpose of mapping markers to data properties</param>
		/// ------------------------------------------------------------------------------------
		protected void BuildStyleItemList(int cStyles, IEnumerable styleInfos,
			string[] pseudoStyles)
		{
			if (styleInfos == null)
				return;

			int cPseudoStyles = (pseudoStyles == null) ? 0 : pseudoStyles.Length;
			if (m_styleItemList == null)
				m_styleItemList = new Dictionary<string, StyleListItem>(1 + cStyles + cPseudoStyles);
			else
				m_styleItemList.Clear();

			// Add an item for the Default Paragraph Characters pseudo style.
			m_styleItemList.Add(ResourceHelper.DefaultParaCharsStyleName,
				StyleListItem.CreateDefaultParaCharItem());

			foreach (BaseStyleInfo styleInfo in styleInfos)
				m_styleItemList.Add(styleInfo.Name, new StyleListItem(styleInfo));

			AddPseudoStyles(pseudoStyles);
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will apply the style filter to your item and tell you if it is valid or not.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		/// <remarks>The include list behaves differently if there is a filter than if there
		/// is no filter applied.  If there isn't a filter then behave as an exclusive
		/// list (i.e. only contexts that are in the include list will be added). If there is a
		/// filter then behave as an additional list (i.e. contexts in the include list will be
		/// added even if they are excluded by the filter).
		/// REVIEW (TimS): Is this the best way to do the include list?</remarks>
		/// ------------------------------------------------------------------------------------
		protected bool OkToAddItem(StyleListItem item)
		{
			if (m_explicitStylesToDisplay != null)
				return (m_explicitStylesToDisplay.Contains(item.Name));
			// Some behavior for Flex is easier by excluding styles explicitly, rather than
			// displaying them explicitly.  See FWR-1178.
			if (m_explicitStylesToExclude != null)
				return !m_explicitStylesToExclude.Contains(item.Name);

			// Add the "Default Paragraph Characters" psuedo style in all cases except when
			// the filter tells us to only add paragraph styles.
			if (item.IsDefaultParaCharsStyle && m_typeFilter != StyleType.kstParagraph)
				return true;

			// Check the style level to see if the style is excluded
			if (item.UserLevel > MaxStyleLevel)
				return false;

			if (m_showOnlyUserModifiedStyles && !item.IsUserModifiedStyle)
				return false;

			// If there's an excluded context list and the item's context is in it,
			// it's not OK to add.
			if (m_excludedContexts != null && m_excludedContexts.Count > 0 &&
				m_excludedContexts.Contains(item.Context))
			{
				return false;
			}

			// If the context is internal and we aren't trying to show internal styles,
			// it's not OK to add
			// REVIEW: This should probably use StyleServices.IsContextInternal
			if (item.Context == ContextValues.Internal && !ShowingInternalStyles)
			{
				return false;
			}

			// If there's an excluded function list and the item's function is in it, it's not OK
			// to add.
			if (m_excludedFunctions != null && m_excludedFunctions.Count > 0 &&
				m_excludedFunctions.Contains(item.Function))
			{
				return false;
			}

			// Add or reject based on the Include context list
			if (!m_unionIncludeAndTypeFilter && m_includedContexts != null &&
				m_includedContexts.Count > 0 && !m_includedContexts.Contains(item.Context))
			{
				// include contexts used as intersection with filter type.
				return false;
			}
			else if (m_unionIncludeAndTypeFilter && m_includedContexts != null &&
				m_includedContexts.Count > 0 && m_includedContexts.Contains(item.Context))
			{
				// If there is a type filter then behave as an additional list (i.e. contexts in the
				// include list will be added even if they would be excluded by the filter).
				return true;
			}

			// See if the style should be excluded based on its type (character or paragraph)
			if (m_typeFilter == StyleType.kstParagraph && item.Type != StyleType.kstParagraph)
				return false;

			if (m_typeFilter == StyleType.kstCharacter && item.Type != StyleType.kstCharacter &&
				!item.IsDataPropertyStyle)
			{
				return false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is called during the drawing of combo box items and is used to determine
		/// the appropriate icon to draw next to the item's text. Determining the appropriate
		/// icon depends on the following factors:
		/// 1. whether or not the current item is selected
		/// 2. whether the current item is a character or paragraph style
		/// 3. whether the current item represents the style of the current view's selection
		/// </summary>
		/// <remarks>If the current item is selected we display the same icon but a different
		/// background color.</remarks>
		/// <param name="item">The item for which an icon is being returned</param>
		/// <param name="selected">whether or not the item is selected</param>
		/// <returns>The appropriate icon for the item</returns>
		/// ------------------------------------------------------------------------------------
		protected Image GetCorrectIcon(StyleListItem item, bool selected)
		{
			if (item.Type == StyleType.kstCharacter)
			{
				if (item.Name == m_currCharStyleName)
					return (selected ? m_currSelectedCharStyleIcon : m_currCharStyleIcon);
				else
					return (selected ? m_selectedCharStyleIcon : m_charStyleIcon);
			}
			else if (!item.IsDataPropertyStyle)
			{
				if (item.Name == m_currParaStyleName)
					return (selected ? m_currSelectedParaStyleIcon : m_currParaStyleIcon);
				else
					return (selected ? m_selectedParaStyleIcon : m_paraStyleIcon);
			}
			else
			{
				return (selected ? m_selectedDataPropStyleIcon : m_dataPropStyleIcon);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets each black (i.e. RGB = 0,0,0) pixel in a bitmap to the specified color.
		/// </summary>
		/// <param name="bmp">Bitmap to change</param>
		/// <param name="clrTo">Color to which black pixels will be changed.</param>
		/// ------------------------------------------------------------------------------------
		protected void SetBmpColor(Bitmap bmp, Color clrTo)
		{
			for (int x = 0; x < bmp.Width; x++)
			{
				for (int y = 0; y < bmp.Height; y++)
				{
					if (bmp.GetPixel(x, y) == Color.FromArgb(0, 0, 0))
						bmp.SetPixel(x, y, clrTo);
				}
			}
		}
		#endregion
	}
}
