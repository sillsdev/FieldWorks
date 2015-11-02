// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwFontTab.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwFontTab : UserControl, IFWDisposable, IStylesTab
	{
		#region Data Members
		/// <summary>
		/// Fires when a change is made on the font tab to an unspecified state.
		/// </summary>
		public event EventHandler ChangedToUnspecified;
		/// <summary>
		/// Fires when the font tab needs to have the style property inheritance recalculated
		/// </summary>
		public event EventHandler RequestStyleReconnect;

		private bool m_dontUpdateInheritance = true;
		private bool m_fIgnoreWsSelectedIndexChange = false;
		private StyleInfo m_currentStyleInfo;
		private int m_currentWs = -1;

		private bool m_fFontListIncludesRealNames = false;

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwFontTab"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwFontTab()
		{
			InitializeComponent();
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

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the writing system factory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory WritingSystemFactory
		{
			set { CheckDisposed(); m_FontAttributes.WritingSystemFactory = value; }
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure the font size control has a valid number in it
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_cboFontSize_TextUpdate(object sender, EventArgs e)
		{
			StringBuilder correctedText = new StringBuilder();
			bool change = false;
			foreach (char ch in m_cboFontSize.Text)
			{
				// limit the text to 3 characters.
				if (correctedText.Length == 3)
				{
					change = true;
					break;
				}
				// make sure the text has all digits.
				if (Char.IsDigit(ch))
					correctedText.Append(ch);
				else
					change = true;
			}

			//// TE-5238: Make sure that font size is not set to 0.
			//// LT-8812: Make sure that font size is not empty (which is interpreted as 0).
			if ((correctedText.Length > 0 && (int)Char.GetNumericValue(correctedText.ToString()[0]) <= 0) ||
				correctedText.Length == 0)
			{
				if (correctedText.Length > 0)
				correctedText.Remove(0, correctedText.Length);
				correctedText.Append('1');
				change = true;
			}

			if (change)
			{
				m_cboFontSize.Text = correctedText.ToString();
				m_cboFontSize.SelectionStart = correctedText.Length;
			}
			ValueChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboFontNames control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_cboFontNames_SelectedIndexChanged(object sender, EventArgs e)
		{
			FontInfo fontInfoForWs = m_currentStyleInfo.FontInfoForWs(m_currentWs);
			FontInfo inheritedFontInfo = (m_currentStyleInfo.BasedOnStyle == null) ? null :
				m_currentStyleInfo.BasedOnStyle.FontInfoForWs(m_currentWs);
			// (EricP) NOTE: changing m_features here, rather than in SaveToInfo
			// bypasses the mechanism for setting the Dirty flag.  However,
			// since this code only gets called when switching writing systems, the
			// font name is also getting changed, and that does result in Dirty flag getting
			// set.
			if (inheritedFontInfo == null)
			{
				fontInfoForWs.m_features.ResetToInherited((string)null);
				m_FontAttributes.FontFeaturesTag = true;
			}
			// IF
			// The style and its base style are both inherited
			// OR
			// User selected same font that the current style is based on (may be either
			// explicit or the result of reverting to unspecified);
			// OR
			// The base style has no font features specified
			// THEN, we can safely go back to "unspecified" font features
			// NOTE: fontInfoForWs.m_fontName.IsInherited doesn't get set until SaveToInfo()
			// and IsInherited(m_cboFontNames) can change in ValueChanged()...so just use SelectedIndex == 0
			else if ((m_cboFontNames.SelectedIndex == 0 && inheritedFontInfo.m_fontName.IsInherited) ||
				(inheritedFontInfo.m_fontName.ValueIsSet &&
				(m_cboFontNames.Text == inheritedFontInfo.m_fontName.Value ||
				inheritedFontInfo.m_features.ValueIsSet && inheritedFontInfo.m_features.Value == null)))
			{
				fontInfoForWs.m_features.ResetToInherited(inheritedFontInfo.m_features);
				m_FontAttributes.FontFeaturesTag = true;
			}
			else // switched to a different font and the base style has features set...
			{
				// (EricP) Not this follows from all the situations of the previous condition block.
				// The comment on the block says "switched to a different font and the base style
				// has features set..."  What if this font has features and the base doesn't? Are
				// we really sure at this point that the base has features?
				// ... so we need to explicitly reset our features for the current style
				// back to empty.
				fontInfoForWs.m_features.ExplicitValue = null;
				m_FontAttributes.FontFeaturesTag = false;
			}

			m_FontAttributes.FontName = m_cboFontNames.Text;
			ValueChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a control's value changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ValueChanged(object sender, EventArgs e)
		{
			if (!m_dontUpdateInheritance && sender != null)
			{
				((Control)sender).ForeColor = SystemColors.WindowText;

				if (IsInherited((Control)sender) && ChangedToUnspecified != null)
					ChangedToUnspecified(this, EventArgs.Empty);
				SaveToInfo(m_currentStyleInfo);
				UpdateWritingSystemDescriptions();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_lstWritingSystems control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_lstWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			// when the selected writing system changes, need to fill in the controls
			// based on the new writing system.
			if (m_fIgnoreWsSelectedIndexChange || m_lstWritingSystems.SelectedItems.Count != 1)
				return;
			ListViewItem item = m_lstWritingSystems.SelectedItems[0];
			SaveToInfo(m_currentStyleInfo);

			// reconnect styles and update
			if (RequestStyleReconnect != null)
				RequestStyleReconnect(this, EventArgs.Empty);
			UpdateForStyle(m_currentStyleInfo, (int)item.Tag);
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the font info controls
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FillFontInfo(FdoCache cache)
		{
			CheckDisposed();

			// Fill in the writing systems. Fill in the Tag with the WS HVO.
			ListViewItem item = new ListViewItem(FwCoreDlgControls.kstidDefaultSettings);
			item.Tag = -1;
			item.SubItems.Add(string.Empty);
			m_lstWritingSystems.Items.Add(item);

			foreach (CoreWritingSystemDefinition ws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				item = new ListViewItem(ws.DisplayLabel);
				item.Tag = ws.Handle;
				item.SubItems.Add(string.Empty);
				m_lstWritingSystems.Items.Add(item);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the information on the font tab using the current writing system.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateForStyle(StyleInfo styleInfo)
		{
			CheckDisposed();

			UpdateForStyle(styleInfo, m_currentWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the information on the font tab.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// <param name="ws">The writing system for the overrides. -1 for the default settings
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void UpdateForStyle(StyleInfo styleInfo, int ws)
		{
			CheckDisposed();

			if (styleInfo == null)
				return;

			m_dontUpdateInheritance = true;
			m_currentStyleInfo = styleInfo;
			m_currentWs = ws;
			if (ws == -1)
			{
				m_fIgnoreWsSelectedIndexChange = true;
				m_lstWritingSystems.SelectedIndices.Clear();
				m_lstWritingSystems.SelectedIndices.Add(0);
				m_fIgnoreWsSelectedIndexChange = false;
			}

			// If the first item is selected, it is the "default", so the font list should
			// only include the magic font names, not the real ones.
			FillFontNames(ws > -1);

			m_FontAttributes.ShowingInheritedProperties = ShowingInheritedProperties;

			// Initialize controls based on whether or not this style inherits from another style.
			InitControlBehavior(ShowingInheritedProperties);

			FontInfo fontInfo = styleInfo.FontInfoForWs(ws);
			m_FontAttributes.UpdateForStyle(fontInfo);

			// update the font size combobox
			if (!m_cboFontSize.SetInheritableProp(fontInfo.m_fontSize))
				m_cboFontSize.Text = (fontInfo.m_fontSize.Value / 1000).ToString();

			// update the font names
			if (!m_cboFontNames.SetInheritableProp(fontInfo.m_fontName))
			{
				string fontName = fontInfo.m_fontName.Value;
				if (string.IsNullOrEmpty(fontName))
				{
					Debug.Fail("How did this happen? Tim & Tom don't think it can.");
					m_cboFontNames.AdjustedSelectedIndex = 0;
				}
				else if (fontName == StyleServices.DefaultFont)
					m_cboFontNames.AdjustedSelectedIndex = 1;
				else
					m_cboFontNames.Text = fontName;
			}

			// Update the descriptions for the writing system overrides
			UpdateWritingSystemDescriptions();

			m_dontUpdateInheritance = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the font information to the styleInfo
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveToInfo(StyleInfo styleInfo)
		{
			CheckDisposed();

			FontInfo fontInfo = styleInfo.FontInfoForWs(m_currentWs);
			// Font name
			bool newInherit = IsInherited(m_cboFontNames);
			string newValue;
			switch (m_cboFontNames.AdjustedSelectedIndex)
			{
				case 1: newValue = StyleServices.DefaultFont; break;
				default: newValue = m_cboFontNames.Text; break;
			}
			if (fontInfo.m_fontName.Save(newInherit, newValue))
				styleInfo.Dirty = true;

			// font size
			newInherit = IsInherited(m_cboFontSize);
			int fontSize = (m_cboFontSize.Text == string.Empty || newInherit) ? 0 : Int32.Parse(m_cboFontSize.Text);
			if (fontInfo.m_fontSize.Save(newInherit, fontSize * 1000))
				styleInfo.Dirty = true;

			// color
			Color color = m_FontAttributes.GetFontColor(out newInherit);
			if (fontInfo.m_fontColor.Save(newInherit, color))
				styleInfo.Dirty = true;

			// background color
			color = m_FontAttributes.GetBackgroundColor(out newInherit);
			if (fontInfo.m_backColor.Save(newInherit, color))
				styleInfo.Dirty = true;

			// underline style
			FwUnderlineType underlineType = m_FontAttributes.GetUnderlineType(out newInherit);
			if (fontInfo.m_underline.Save(newInherit, underlineType))
				styleInfo.Dirty = true;

			// underline color
			color = m_FontAttributes.GetUnderlineColor(out newInherit);
			if (fontInfo.m_underlineColor.Save(newInherit, color))
				styleInfo.Dirty = true;

			// bold, italic, superscript, subscript
			bool fFlag = m_FontAttributes.GetBold(out newInherit);
			if (fontInfo.m_bold.Save(newInherit, fFlag))
				styleInfo.Dirty = true;

			fFlag = m_FontAttributes.GetItalic(out newInherit);
			if (fontInfo.m_italic.Save(newInherit, fFlag))
				styleInfo.Dirty = true;

			FwSuperscriptVal superSub = m_FontAttributes.GetSubSuperscript(out newInherit);
			if (fontInfo.m_superSub.Save(newInherit, superSub))
				styleInfo.Dirty = true;

			// position
			int fontPos = m_FontAttributes.GetFontPosition(out newInherit);
			if (fontInfo.m_offset.Save(newInherit, fontPos))
				styleInfo.Dirty = true;

			// features
			string fontFeatures = m_FontAttributes.GetFontFeatures(out newInherit);
			if (fontInfo.m_features.Save(newInherit, fontFeatures))
				styleInfo.Dirty = true;
		}
		#endregion

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the font names.
		/// </summary>
		/// <param name="fIncludeRealFontNames">if set to <c>true</c> list will include both the
		/// magic font names and real font names; otherwise only the magic font names will be in
		/// the list.</param>
		/// ------------------------------------------------------------------------------------
		private void FillFontNames(bool fIncludeRealFontNames)
		{
			if (m_fFontListIncludesRealNames == fIncludeRealFontNames &&
				m_cboFontNames.Items.Count > 0)
			{
				return; // List is already in correct state.
			}
			m_cboFontNames.Items.Clear();
			if (m_cboFontNames.ShowingInheritedProperties)
				m_cboFontNames.Items.Add(FwCoreDlgControls.kstidUnspecified);
			m_cboFontNames.Items.Add(ResourceHelper.GetResourceString("kstidDefaultFont"));
			if (fIncludeRealFontNames)
			{
				// Mono doesn't sort the font names currently 20100322. Workaround for FWNX-273: Fonts not in alphabetical order
				var fontNames =
					from family in FontFamily.Families
					orderby family.Name
					select family.Name;
				foreach (var name in fontNames)
					m_cboFontNames.Items.Add(name);
			}
			m_fFontListIncludesRealNames = fIncludeRealFontNames;
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Gets the color to use for painting the foreground of a control which displays an
//		/// inheritable property value.
//		/// </summary>
//		/// <param name="prop">The inheritable property.</param>
//		/// <returns>The system gray color if the property is inherited; otherwise the normal
//		/// window text color.</returns>
//		/// ------------------------------------------------------------------------------------
//		private Color GetCtrlForeColorForProp<T>(InheritableStyleProp<T> prop)
//		{
//			return (prop.IsInherited && ShowingInheritedProperties) ?
//				SystemColors.GrayText : SystemColors.WindowText;
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this tab is currently displaying properties for a
		/// style which inherits from another style or for a WS-specific override for a style.
		/// </summary>
		/// <value>
		/// 	<c>false</c> if no specific WS is selected and we're displaying the properties
		/// for the "Normal" style; otherwise, <c>true</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		private bool ShowingInheritedProperties
		{
			get { return (m_currentStyleInfo != null && m_currentStyleInfo.Inherits) || m_currentWs > 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize controls based on whether or not current style inherits from
		/// another style. If not (i.e., this is the "Normal" style), then controls
		/// should not allow the user to pick "unspecified" as the value.
		/// </summary>
		/// <param name="fInherited">Indicates whether current style is inherited.</param>
		/// ------------------------------------------------------------------------------------
		private void InitControlBehavior(bool fInherited)
		{
			if (m_cboFontNames.ShowingInheritedProperties == fInherited)
				return; // Already in the right state

			m_cboFontNames.ShowingInheritedProperties = fInherited;
			m_cboFontSize.ShowingInheritedProperties = fInherited;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the writing system descriptions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateWritingSystemDescriptions()
		{
			foreach (ListViewItem item in m_lstWritingSystems.Items)
			{
				int ws = (int)item.Tag;
				string txt = m_currentStyleInfo.FontInfoForWs(ws).ToString(ws < 0 && !m_currentStyleInfo.Inherits);
				if (m_currentStyleInfo.BasedOnStyle == null)
					item.SubItems[1].Text = txt;
				else if (!string.IsNullOrEmpty(txt))
					item.SubItems[1].Text = m_currentStyleInfo.BasedOnStyle.Name + " + " + txt;
				else
					item.SubItems[1].Text = string.Format(ResourceHelper.GetResourceString("kstidStyleWsOverideInheritedFromMsg"),
						m_currentStyleInfo.BasedOnStyle.Name);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified control value is inherited.
		/// </summary>
		/// <param name="c">The control</param>
		/// <returns>true if the specified control is inherited; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsInherited(Control c)
		{
			if (!ShowingInheritedProperties)
				return false;

			// This used to be at the last thing checked before returning
			if (c == m_cboFontSize && m_cboFontSize.Text == string.Empty)
				return true;

			if (c is FwInheritablePropComboBox)
				return ((FwInheritablePropComboBox)c).IsInherited;

			if (c is FwColorCombo && ((FwColorCombo)c).IsInherited)
				return true;

			if (c is CheckBox && ((CheckBox)c).CheckState == CheckState.Indeterminate)
				return true;

			return c.ForeColor.ToArgb() != SystemColors.WindowText.ToArgb();
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Gets the check state for the specified boolean inheritable prop.
//		/// </summary>
//		/// <param name="prop">The prop.</param>
//		/// <returns>The check state</returns>
//		/// ------------------------------------------------------------------------------------
//		private CheckState GetCheckStateFor(InheritableStyleProp<bool> prop)
//		{
//			if (prop.IsInherited && ShowingInheritedProperties)
//				return CheckState.Indeterminate;
//			return (prop.Value ? CheckState.Checked : CheckState.Unchecked);
//		}
		#endregion

	}
}
