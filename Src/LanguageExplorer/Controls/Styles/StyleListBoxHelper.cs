// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>Represents the method that will handle the StyleChosen event.</summary>
	internal delegate void StyleChosenHandler(StyleListItem prevStyle, StyleListItem newStyle);

	/// <summary />
	internal sealed class StyleListBoxHelper : IDisposable
	{
		/// <summary>Occurs when a style is chosen from the list.</summary>
		public event StyleChosenHandler StyleChosen;

		#region Member variables

		/// <summary />
		private Control m_ctrl;
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
		/// <summary>True to show internal styles, false otherwise</summary>
		private bool m_showInternalStyles;
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
		private StyleListItem m_prevStyle;
		private StyleListItem[] m_prevList;
		/// <summary />
		private bool m_ignoreChosenDelegate;
		/// <summary />
		private Dictionary<string, StyleListItem> m_styleItemList;
		/// <summary />
		private string m_currParaStyleName = string.Empty;
		/// <summary />
		private string m_currCharStyleName = string.Empty;
		private int m_maxStyleLevel = int.MaxValue;
		#endregion

		/// <summary />
		public StyleListBoxHelper(CaseSensitiveListBox listBox)
		{
			Debug.Assert(listBox != null);
			m_ctrl = listBox;
			m_ctrl.SystemColorsChanged += CtrlSystemColorsChanged;
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
			listBox.DrawMode = DrawMode.OwnerDrawFixed;
			listBox.DrawItem += CtrlDrawItem;
			listBox.SelectedIndexChanged += CtrlSelectedIndexChanged;
			listBox.Sorted = false;
		}
		#region Disposable stuff

		/// <summary />
		~StyleListBoxHelper()
		{
			Dispose(false);
		}

		/// <summary />
		private bool IsDisposed { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		private void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (fDisposing)
			{
				// dispose managed and unmanaged objects
				m_charStyleIcon?.Dispose();
				m_paraStyleIcon?.Dispose();
				m_dataPropStyleIcon?.Dispose();
				m_selectedCharStyleIcon?.Dispose();
				m_selectedParaStyleIcon?.Dispose();
				m_selectedDataPropStyleIcon?.Dispose();
				m_currCharStyleIcon?.Dispose();
				m_currParaStyleIcon?.Dispose();
				m_currSelectedCharStyleIcon?.Dispose();
				m_currSelectedParaStyleIcon?.Dispose();
				m_origCharStyleIcon?.Dispose();
				m_origParaStyleIcon?.Dispose();
				m_origDataPropStyleIcon?.Dispose();
				m_origCurrCharStyleIcon?.Dispose();
				m_origCurrParaStyleIcon?.Dispose();
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

		/// <summary>
		/// Gets whether this instance is supposed to be showing internal styles, either because
		/// client explicitly requested them to be shown (as in the styles dialog) or because
		/// the list of included contexts includes internal styles (as when an internal style is
		/// the currently selected style in a view).
		/// </summary>
		private bool ShowingInternalStyles => m_showInternalStyles || (m_includedContexts != null && m_includedContexts.Contains(ContextValues.Internal));

		/// <summary>
		/// True to allow showing internal styles
		/// </summary>
		public bool ShowInternalStyles
		{
			set { m_showInternalStyles = value; }
		}

		/// <summary>
		/// Gets/Sets a value to ignore all updates to the list of styles
		/// </summary>
		public bool IgnoreListRefresh { get; set; }

		/// <summary>
		/// Gets the control cast as a CaseSensitiveListBox
		/// </summary>
		private CaseSensitiveListBox ListBoxControl => (CaseSensitiveListBox)m_ctrl;

		/// <summary>
		/// Gets/sets the SelectedItem property for the style list box.
		/// </summary>
		public StyleListItem SelectedStyle => (StyleListItem)ListBoxControl.SelectedItem;

		/// <summary>
		/// Gets or sets the list box's selected style by name.
		/// </summary>
		public string SelectedStyleName
		{
			get
			{
				return (ListBoxControl.SelectedIndex != -1 ? SelectedStyle.ToString() : string.Empty);
			}
			set
			{
				if (value != null)
				{
					var i = -1;
					if (value != string.Empty)
					{
						i = ListBoxControl.FindStringExact(value);
					}
					m_ignoreChosenDelegate = true;
					ListBoxControl.SelectedIndex = i;
					m_ignoreChosenDelegate = false;
				}
			}
		}

		/// <summary>
		/// Gets or sets the stylesheet.
		/// </summary>
		[Browsable(false)]
		public LcmStyleSheet StyleSheet { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the LCM cache from which StStyle objects will be
		/// created.
		/// </summary>
		public LcmCache Cache { get; set; }

		/// <summary>
		/// Gets or sets a value indicating what type of styles are shown in the list. By
		/// default this property is set to StyleType.kstLim which will show styles of
		/// all types. (Note: for this property to have any effect, it must be set before
		/// a call to the AddStyles method.) This property is used in addition to the
		/// <see cref="ExcludeStylesWithContext"/> or <see cref="IncludeStylesWithContext"/>
		/// properties.
		/// </summary>
		public StyleType ShowOnlyStylesOfType { get; set; } = StyleType.kstLim;

		/// <summary>
		/// Gets or sets flag controlling how included contexts will be processed. If true, all
		/// style contexts included will be unioned with filtered types. Otherwise, an
		/// intersection is created.
		/// </summary>
		public bool UnionIncludeAndTypeFilter { get; set; }

		/// <summary>
		/// Gets or sets a List containing a list of ContextValues of styles to exclude from
		/// the list.
		/// </summary>
		/// <remarks>
		/// (Note: for this property to have any effect, it must be set before
		/// a call to the
		/// <see cref="AddStyles(LcmStyleSheet)"/>
		/// or <see cref="Refresh"/> methods.) This property is used in addition to the
		/// <see cref="ShowOnlyStylesOfType"/> property.
		/// When this property is set to something other than null, the
		/// <see cref="IncludeStylesWithContext"/> list is automatically cleared.
		/// </remarks>
		public List<ContextValues> ExcludeStylesWithContext
		{
			get { return m_excludedContexts ?? (m_excludedContexts = new List<ContextValues>()); }
			set
			{
				if (value != null)
				{
					m_includedContexts = null;
				}
				m_excludedContexts = value;
			}
		}

		/// <summary>
		/// Gets or sets a List containing a list of ContextValues of styles to include in
		/// the list. (Note: for this property to have any effect, it must be set before
		/// a call to the
		/// <see cref="AddStyles(LcmStyleSheet)"/>
		/// or <see cref="Refresh"/> methods.) This property is used in addition to the
		/// <see cref="ShowOnlyStylesOfType"/> property. When this property is set to something
		/// other than null, the <see cref="ExcludeStylesWithContext"/> list is automatically
		/// cleared.
		/// </summary>
		public List<ContextValues> IncludeStylesWithContext
		{
			get { return m_includedContexts ?? (m_includedContexts = new List<ContextValues>()); }
			set
			{
				if (value != null)
				{
					m_excludedContexts = null;
				}
				m_includedContexts = value;
			}
		}

		/// <summary>
		/// Gets or sets a list of explicit style names to include in the displayed list. If this
		/// is set, then all other included/excluded contexts, functions, etc. are ignored and
		/// this list overrides the normal filtering. If this is null, then normal filtering
		/// happens.
		/// </summary>
		public List<string> ExplicitStylesToDisplay
		{
			get { return m_explicitStylesToDisplay; }
			set
			{
				m_explicitStylesToDisplay = value;
				m_explicitStylesToExclude = null;
			}
		}

		/// <summary>
		/// Stores a list of explicit style names to exclude from the displayed list.  When this
		/// is set, the m_explicitStylesToDisplay is cleared, and vice versa.  If this is set,
		/// it overrides the normal filtering.  If this is null (and m_explicitStylesToDisplay
		/// is null), the normal filtering happens.
		/// </summary>
		public List<string> ExplicitStylesToExclude
		{
			get { return m_explicitStylesToExclude; }
			set
			{
				m_explicitStylesToExclude = value;
				m_explicitStylesToDisplay = null;
			}
		}

		/// <summary>
		/// Gets or sets the Active View.  This is used to know which view has setup the
		/// contents of the style helper.  The view can know if is has been changed by another
		/// view if this property is not itself.
		/// </summary>
		public Control ActiveView { get; set; }

		/// <summary>
		/// maximum style level to include
		/// </summary>
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

		/// <summary>
		/// Indicates whether to show only user-defined styles
		/// </summary>
		public bool ShowOnlyUserModifiedStyles { get; set; }

		#endregion

		#region List Control Delegate Methods

		/// <summary>
		/// When the selected style changes, apply the style
		/// </summary>
		private void CtrlSelectedIndexChanged(object sender, EventArgs e)
		{
			if (SelectedStyle == null && (m_currParaStyleName != string.Empty || m_currCharStyleName != string.Empty))
			{
				SelectedStyleName = m_currParaStyleName == string.Empty ? m_currCharStyleName : m_currParaStyleName;
				Debug.Assert(SelectedStyle != null);
			}
			if (SelectedStyle == null)
			{
				return;
			}
			if (StyleChosen != null && m_ignoreChosenDelegate == false)
			{
				StyleChosen(m_prevStyle, SelectedStyle);
			}
			m_prevStyle = SelectedStyle;
		}

		/// <summary />
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

		/// <summary>
		/// Draw the items in the list
		/// </summary>
		private void CtrlDrawItem(object sender, DrawItemEventArgs e)
		{
			var selected = ((e.State & DrawItemState.Selected) != 0);
			// Draw the item's background fill
			e.Graphics.FillRectangle(new SolidBrush((selected ? SystemColors.Highlight : SystemColors.Window)), e.Bounds);
			// Don't bother doing any more painting if there isn't anything to paint.
			if (e.Index < 0)
			{
				return;
			}
			var rc = e.Bounds;
			rc.Inflate(-1, 0);
			rc.X += 2;
			rc.Width -= 2;
			// Get the item being drawn.
			var item = (StyleListItem)ListBoxControl.Items[e.Index];
			// Determine what image to draw, considering the selection state of the item and
			// whether the item is a character style or a paragraph style.
			var icon = GetCorrectIcon(item, selected);
			// Draw the icon only if we're not drawing a combo box's edit portion.
			if ((e.State & DrawItemState.ComboBoxEdit) == 0)
			{
				e.Graphics.DrawImage(icon, rc.Left, rc.Top + (rc.Height - icon.Height) / 2);
			}
			// Draw the item's text, considering the item's selection state. Item text in the
			// edit portion will be draw further left than those in the drop-down because text
			// in the edit portion doesn't have the icon to the left.
			e.Graphics.DrawString(item.Name, m_ctrl.Font, selected
					? SystemBrushes.HighlightText
					: SystemBrushes.WindowText, rc.Left + ((e.State & DrawItemState.ComboBoxEdit) != 0 ? 0 : icon.Width),
				rc.Top);
		}
		#endregion

		#region Public Methods

		/// <summary>
		/// Adds the specified new style.
		/// </summary>
		public void Add(BaseStyleInfo newStyle)
		{
			m_styleItemList.Add(newStyle.Name, new StyleListItem(newStyle));
			Refresh();
			ListBoxControl.SelectedIndex = ListBoxControl.FindStringExact(newStyle.Name);
		}

		/// <summary>
		/// Removes the specified style from the list
		/// </summary>
		public void Remove(BaseStyleInfo style)
		{
			// Save the index of the selected item so it can be restored later.
			var oldSelectedIndex = ListBoxControl.SelectedIndex;
			m_styleItemList.Remove(style.Name);
			Refresh();
			if (oldSelectedIndex >= ListBoxControl.Items.Count)
			{
				--oldSelectedIndex;
			}
			ListBoxControl.SelectedIndex = oldSelectedIndex;
		}

		/// <summary>
		/// Marks the given style as a "current" style. This will allow it to be indicated in
		/// the list as one of the current styles.
		/// </summary>
		public void MarkCurrentStyle(string styleName)
		{
			if (string.IsNullOrEmpty(styleName))
			{
				return;
			}
			if (m_styleItemList.ContainsKey(styleName))
			{
				var item = m_styleItemList[styleName];
				switch (item.Type)
				{
					case StyleType.kstParagraph:
						m_currParaStyleName = styleName;
						break;
					case StyleType.kstCharacter:
						m_currCharStyleName = styleName;
						break;
				}
				item.IsCurrentStyle = true;
			}
		}

		/// <summary>
		/// Returns an StStyle object given a style name.
		/// </summary>
		public IStStyle StyleFromName(string styleName)
		{
			//TE-5609 Prevent crash in case of missing style by using TryGetValue.
			StyleListItem item;
			if (m_styleItemList.TryGetValue(styleName, out item))
			{
				Debug.Assert(item.StyleInfo.RealStyle != null);
				return item.StyleInfo.RealStyle;
			}
			return null;
		}

		/// <summary>
		/// Updates the styles in the m_ctrl list based on the ExcludeStylesWithContext and
		/// ShowOnlyStylesOfType properties. This should be called when the caller wants to
		/// update the m_ctrl list after changing one of those two properties, but doesn't want
		/// to rebuild the entire m_styleItemList.
		/// </summary>
		public void Refresh()
		{
			if (m_styleItemList != null) // Added JohnT for robustness and to keep tests working.
			{
				// get the list of items into an array list that can be sorted
				var itemsList = new List<StyleListItem>(m_styleItemList.Values.Where(OkToAddItem));
				// If the list contains the default paragraph characters style then remove it
				// from the list so it can be removed from the list while sorting.
				var defaultParaCharsStyle = itemsList.FirstOrDefault(item => item.Name == StyleUtils.DefaultParaCharsStyleName);
				if (defaultParaCharsStyle != null)
				{
					itemsList.Remove(defaultParaCharsStyle);
				}
				// Sort the list, add the default paragraph chars style back in at the top of
				// the list, and add all of the items to the combo box.
				itemsList.Sort();
				if (defaultParaCharsStyle != null)
				{
					itemsList.Insert(0, defaultParaCharsStyle);
				}
				var newItems = itemsList.ToArray();
				if (m_prevList == null || !newItems.SequenceEqual(m_prevList))
				{
					UpdateStyleList(newItems);
					m_prevList = newItems;
					if (newItems.Length == 0 || !itemsList.Contains(m_prevStyle))
					{
						m_prevStyle = null;
					}
				}
			}
		}

		/// <summary>
		/// Adds the style names to the combo box or list box from the specified stylesheet.
		/// </summary>
		public void AddStyles(LcmStyleSheet styleSheet)
		{
			AddStyles(styleSheet, null);
		}

		/// <summary>
		/// Adds the style names to the combo box or list box from the specified stylesheet.
		/// </summary>
		/// <param name="styleSheet">Stylesheet from which styles are read.</param>
		/// <param name="pseudoStyles">Array of strings representing pseudo-styles that can be
		/// displayed for the purpose of mapping markers to data properties</param>
		public void AddStyles(LcmStyleSheet styleSheet, string[] pseudoStyles)
		{
			Debug.Assert(styleSheet != null);
			BuildStyleItemList(styleSheet.CStyles, styleSheet.Styles, pseudoStyles);
		}

		/// <summary>
		/// Adds the styles.
		/// </summary>
		public void AddStyles(StyleInfoTable styleTable, string[] pseudoStyles)
		{
			Debug.Assert(styleTable != null);
			BuildStyleItemList(styleTable.Count, styleTable.Values, pseudoStyles);
		}

		/// <summary>
		/// Renames a style.
		/// </summary>
		public void Rename(string oldName, string newName)
		{
			var style = m_styleItemList[oldName];
			m_styleItemList.Remove(oldName);
			style.StyleInfo.Name = newName;
			m_styleItemList.Add(newName, style);
			Refresh();
		}

		/// <summary>
		/// Add item during refresh.
		/// </summary>
		private void UpdateStyleList(StyleListItem[] items)
		{
			if (IgnoreListRefresh)
			{
				return;
			}
			var selectedStyle = ListBoxControl.SelectedItem?.ToString() ?? string.Empty;
			ListBoxControl.Items.Clear();
			ListBoxControl.BeginUpdate();
			ListBoxControl.Items.AddRange(items);
			ListBoxControl.EndUpdate();
			SelectedStyleName = selectedStyle;
			// Ensure an item is selected, even if the previous selection is no longer
			// shown.
			if (!string.IsNullOrEmpty(selectedStyle) && ListBoxControl.SelectedItem == null)
			{
				if (ListBoxControl.Items.Count > 0)
				{
					ListBoxControl.SelectedIndex = 0;
				}
			}
		}
		#endregion

		#region Helper Methods

		/// <summary>
		/// Adds the pseudo styles.
		/// </summary>
		private void AddPseudoStyles(string[] pseudoStyles)
		{
			if (pseudoStyles == null)
			{
				return;
			}
			foreach (var sPseudoStyle in pseudoStyles)
			{
				m_styleItemList.Add(sPseudoStyle, StyleListItem.CreateDataPropertyItem(sPseudoStyle));
			}
		}

		/// <summary>
		/// Stores all the available styles from the style sheet. Later, the list is used to
		/// fill the combo or list box with the appropriate styles.
		/// </summary>
		/// <param name="cStyles">Number of styles in <paramref name="styleInfos"/></param>
		/// <param name="styleInfos">The collection of style infos.</param>
		/// <param name="pseudoStyles">Array of strings representing pseudo-styles that can be
		/// displayed for the purpose of mapping markers to data properties</param>
		private void BuildStyleItemList(int cStyles, IEnumerable styleInfos, string[] pseudoStyles)
		{
			if (styleInfos == null)
			{
				return;
			}
			var cPseudoStyles = pseudoStyles?.Length ?? 0;
			if (m_styleItemList == null)
			{
				m_styleItemList = new Dictionary<string, StyleListItem>(1 + cStyles + cPseudoStyles);
			}
			else
			{
				m_styleItemList.Clear();
			}
			// Add an item for the Default Paragraph Characters pseudo style.
			m_styleItemList.Add(StyleUtils.DefaultParaCharsStyleName, StyleListItem.CreateDefaultParaCharItem());
			foreach (BaseStyleInfo styleInfo in styleInfos)
			{
				m_styleItemList.Add(styleInfo.Name, new StyleListItem(styleInfo));
			}
			AddPseudoStyles(pseudoStyles);
			Refresh();
		}

		/// <summary>
		/// This will apply the style filter to your item and tell you if it is valid or not.
		/// </summary>
		/// <remarks>The include list behaves differently if there is a filter than if there
		/// is no filter applied.  If there isn't a filter then behave as an exclusive
		/// list (i.e. only contexts that are in the include list will be added). If there is a
		/// filter then behave as an additional list (i.e. contexts in the include list will be
		/// added even if they are excluded by the filter).
		/// REVIEW (TimS): Is this the best way to do the include list?
		/// </remarks>
		private bool OkToAddItem(StyleListItem item)
		{
			if (m_explicitStylesToDisplay != null)
			{
				return (m_explicitStylesToDisplay.Contains(item.Name));
			}
			// Some behavior for Flex is easier by excluding styles explicitly, rather than
			// displaying them explicitly.  See FWR-1178.
			if (m_explicitStylesToExclude != null)
			{
				return !m_explicitStylesToExclude.Contains(item.Name);
			}
			// Add the "Default Paragraph Characters" pseudo style in all cases except when
			// the filter tells us to only add paragraph styles.
			if (item.IsDefaultParaCharsStyle && ShowOnlyStylesOfType != StyleType.kstParagraph)
			{
				return true;
			}
			// Check the style level to see if the style is excluded
			if (item.UserLevel > MaxStyleLevel)
			{
				return false;
			}
			if (ShowOnlyUserModifiedStyles && !item.IsUserModifiedStyle)
			{
				return false;
			}
			// If there's an excluded context list and the item's context is in it,
			// it's not OK to add.
			if (m_excludedContexts != null && m_excludedContexts.Count > 0 && m_excludedContexts.Contains(item.Context))
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
			if (m_excludedFunctions != null && m_excludedFunctions.Count > 0 && m_excludedFunctions.Contains(item.Function))
			{
				return false;
			}
			// Add or reject based on the Include context list
			if (!UnionIncludeAndTypeFilter && m_includedContexts != null && m_includedContexts.Count > 0 && !m_includedContexts.Contains(item.Context))
			{
				// include contexts used as intersection with filter type.
				return false;
			}
			if (UnionIncludeAndTypeFilter && m_includedContexts != null && m_includedContexts.Count > 0 && m_includedContexts.Contains(item.Context))
			{
				// If there is a type filter then behave as an additional list (i.e. contexts in the
				// include list will be added even if they would be excluded by the filter).
				return true;
			}
			// See if the style should be excluded based on its type (character or paragraph)
			if (ShowOnlyStylesOfType == StyleType.kstParagraph && item.Type != StyleType.kstParagraph)
			{
				return false;
			}
			return ShowOnlyStylesOfType != StyleType.kstCharacter || item.Type == StyleType.kstCharacter || item.IsDataPropertyStyle;
		}

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
		private Image GetCorrectIcon(StyleListItem item, bool selected)
		{
			if (item.Type == StyleType.kstCharacter)
			{
				return item.Name == m_currCharStyleName ? selected ? m_currSelectedCharStyleIcon : m_currCharStyleIcon : selected ? m_selectedCharStyleIcon : m_charStyleIcon;
			}
			if (!item.IsDataPropertyStyle)
			{
				return item.Name == m_currParaStyleName ? selected ? m_currSelectedParaStyleIcon : m_currParaStyleIcon : selected ? m_selectedParaStyleIcon : m_paraStyleIcon;
			}
			return (selected ? m_selectedDataPropStyleIcon : m_dataPropStyleIcon);
		}

		/// <summary>
		/// Sets each black (i.e. RGB = 0,0,0) pixel in a bitmap to the specified color.
		/// </summary>
		/// <param name="bmp">Bitmap to change</param>
		/// <param name="clrTo">Color to which black pixels will be changed.</param>
		private static void SetBmpColor(Bitmap bmp, Color clrTo)
		{
			for (var x = 0; x < bmp.Width; x++)
			{
				for (var y = 0; y < bmp.Height; y++)
				{
					if (bmp.GetPixel(x, y) == Color.FromArgb(0, 0, 0))
					{
						bmp.SetPixel(x, y, clrTo);
					}
				}
			}
		}
		#endregion
	}
}