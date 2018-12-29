// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary />
	public partial class FwFontTab : UserControl, IStylesTab
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

		/// <summary>
		/// Fires when a change is made on this tab to style data.
		/// </summary>
		public event EventHandler StyleDataChanged;

		private bool m_dontUpdateInheritance = true;
		private bool m_fIgnoreWsSelectedIndexChange;
		private StyleInfo m_currentStyleInfo;
		private int m_currentWs = -1;
		private bool m_fFontListIncludesRealNames;

		#endregion

		/// <summary />
		public FwFontTab()
		{
			InitializeComponent();
		}

		#region Public properties
		/// <summary>
		/// Sets the writing system factory.
		/// </summary>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			set { m_FontAttributes.WritingSystemFactory = value; }
		}
		#endregion

		#region Event handlers
		/// <summary>
		/// Makes sure the font size control has a valid number in it
		/// </summary>
		private void m_cboFontSize_TextUpdate(object sender, EventArgs e)
		{
			var correctedText = new StringBuilder();
			var change = false;
			foreach (var ch in m_cboFontSize.Text)
			{
				// limit the text to 3 characters.
				if (correctedText.Length == 3)
				{
					change = true;
					break;
				}
				// make sure the text has all digits.
				if (char.IsDigit(ch))
				{
					correctedText.Append(ch);
				}
				else
				{
					change = true;
				}
			}
			//// TE-5238: Make sure that font size is not set to 0.
			//// LT-8812: Make sure that font size is not empty (which is interpreted as 0).
			if ((correctedText.Length > 0 && (int)char.GetNumericValue(correctedText.ToString()[0]) <= 0) || correctedText.Length == 0)
			{
				if (correctedText.Length > 0)
				{
					correctedText.Remove(0, correctedText.Length);
				}
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

		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboFontNames control.
		/// </summary>
		private void m_cboFontNames_SelectedIndexChanged(object sender, EventArgs e)
		{
			var fontInfoForWs = m_currentStyleInfo.FontInfoForWs(m_currentWs);
			var inheritedFontInfo = m_currentStyleInfo.BasedOnStyle?.FontInfoForWs(m_currentWs);
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
			else if (m_cboFontNames.SelectedIndex == 0 && inheritedFontInfo.m_fontName.IsInherited || inheritedFontInfo.m_fontName.ValueIsSet
			         && (m_cboFontNames.Text == inheritedFontInfo.m_fontName.Value || inheritedFontInfo.m_features.ValueIsSet && inheritedFontInfo.m_features.Value == null))
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

		/// <summary>
		/// Called when a control's value changes
		/// </summary>
		protected void ValueChanged(object sender, EventArgs e)
		{
			if (!m_dontUpdateInheritance && sender != null)
			{
				((Control)sender).ForeColor = SystemColors.WindowText;
				if (IsInherited((Control)sender))
				{
					ChangedToUnspecified?.Invoke(this, EventArgs.Empty);
				}
				SaveToInfo(m_currentStyleInfo);
				UpdateWritingSystemDescriptions();
				StyleDataChanged?.Invoke(this, null);
			}
		}

		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_lstWritingSystems control.
		/// </summary>
		private void m_lstWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			// when the selected writing system changes, need to fill in the controls
			// based on the new writing system.
			if (m_fIgnoreWsSelectedIndexChange || m_lstWritingSystems.SelectedItems.Count != 1)
			{
				return;
			}
			var item = m_lstWritingSystems.SelectedItems[0];
			SaveToInfo(m_currentStyleInfo);
			// reconnect styles and update
			RequestStyleReconnect?.Invoke(this, EventArgs.Empty);
			UpdateForStyle(m_currentStyleInfo, (int)item.Tag);
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Fills the font info controls
		/// </summary>
		public void FillFontInfo(LcmCache cache)
		{
			// Fill in the writing systems. Fill in the Tag with the WS HVO.
			var item = new ListViewItem(Strings.kstidDefaultSettings) { Tag = -1 };
			item.SubItems.Add(string.Empty);
			m_lstWritingSystems.Items.Add(item);
			foreach (var ws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				item = new ListViewItem(ws.DisplayLabel) { Tag = ws.Handle };
				item.SubItems.Add(string.Empty);
				m_lstWritingSystems.Items.Add(item);
			}
		}

		/// <summary>
		/// Updates the information on the font tab using the current writing system.
		/// </summary>
		public void UpdateForStyle(StyleInfo styleInfo)
		{
			UpdateForStyle(styleInfo, m_currentWs);
		}

		/// <summary>
		/// Updates the information on the font tab.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// <param name="ws">The writing system for the overrides. -1 for the default settings
		/// </param>
		public void UpdateForStyle(StyleInfo styleInfo, int ws)
		{
			if (styleInfo == null)
			{
				return;
			}
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
			m_FontAttributes.ShowingInheritedProperties = true; // Always allow re-setting to unspecified for font attributes
			// Initialize controls based on whether or not this style inherits from another style.
			InitControlBehavior(ShowingInheritedProperties);
			var fontInfo = styleInfo.FontInfoForWs(ws);
			m_FontAttributes.UpdateForStyle(fontInfo);
			// update the font size combobox
			if (!m_cboFontSize.SetInheritableProp(fontInfo.m_fontSize))
			{
				m_cboFontSize.Text = (fontInfo.m_fontSize.Value / 1000).ToString();
			}
			// update the font names
			if (!m_cboFontNames.SetInheritableProp(fontInfo.m_fontName))
			{
				var fontName = fontInfo.m_fontName.Value;
				if (string.IsNullOrEmpty(fontName))
				{
					Debug.Fail("How did this happen? Tim & Tom don't think it can.");
					m_cboFontNames.AdjustedSelectedIndex = 0;
				}
				else if (fontName == StyleServices.DefaultFont)
				{
					m_cboFontNames.AdjustedSelectedIndex = 1;
				}
				else
				{
					m_cboFontNames.Text = fontName;
				}
			}
			// Update the descriptions for the writing system overrides
			UpdateWritingSystemDescriptions();
			m_dontUpdateInheritance = false;
		}

		/// <summary>
		/// Saves the font information to the styleInfo
		/// </summary>
		public void SaveToInfo(StyleInfo styleInfo)
		{
			var fontInfo = styleInfo.FontInfoForWs(m_currentWs);
			// Font name
			var newInherit = IsInherited(m_cboFontNames);
			string newValue;
			switch (m_cboFontNames.AdjustedSelectedIndex)
			{
				case 1: newValue = StyleServices.DefaultFont; break;
				default: newValue = m_cboFontNames.Text; break;
			}
			if (fontInfo.m_fontName.Save(newInherit, newValue))
			{
				styleInfo.Dirty = true;
			}
			// font size
			newInherit = IsInherited(m_cboFontSize);
			var fontSize = (m_cboFontSize.Text == string.Empty || newInherit) ? 0 : int.Parse(m_cboFontSize.Text);
			if (fontInfo.m_fontSize.Save(newInherit, fontSize * 1000))
			{
				styleInfo.Dirty = true;
			}
			// color
			if (fontInfo.m_fontColor.Save(newInherit, m_FontAttributes.GetFontColor(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			// background color
			if (fontInfo.m_backColor.Save(newInherit, m_FontAttributes.GetBackgroundColor(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			// underline style
			if (fontInfo.m_underline.Save(newInherit, m_FontAttributes.GetUnderlineType(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			// underline color
			if (fontInfo.m_underlineColor.Save(newInherit, m_FontAttributes.GetUnderlineColor(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			// bold, italic, superscript, subscript
			if (fontInfo.m_bold.Save(newInherit, m_FontAttributes.GetBold(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			if (fontInfo.m_italic.Save(newInherit, m_FontAttributes.GetItalic(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			if (fontInfo.m_superSub.Save(newInherit, m_FontAttributes.GetSubSuperscript(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			// position
			if (fontInfo.m_offset.Save(newInherit, m_FontAttributes.GetFontPosition(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
			// features
			if (fontInfo.m_features.Save(newInherit, m_FontAttributes.GetFontFeatures(out newInherit)))
			{
				styleInfo.Dirty = true;
			}
		}
		#endregion

		internal ComboBox FontNamesComboBox => m_cboFontNames;

		#region private methods
		/// <summary>
		/// Fills the font names.
		/// </summary>
		/// <param name="fIncludeRealFontNames">if set to <c>true</c> list will include both the
		/// magic font names and real font names; otherwise only the magic font names will be in
		/// the list.</param>
		internal void FillFontNames(bool fIncludeRealFontNames)
		{
			if (m_fFontListIncludesRealNames == fIncludeRealFontNames && m_cboFontNames.Items.Count > 0)
			{
				return; // List is already in correct state.
			}
			m_cboFontNames.Items.Clear();
			if (m_cboFontNames.ShowingInheritedProperties)
			{
				m_cboFontNames.Items.Add(Strings.kstidUnspecified);
			}
			m_cboFontNames.Items.Add(ResourceHelper.GetResourceString("kstidDefaultFont"));
			if (fIncludeRealFontNames)
			{
				// Mono doesn't sort the font names currently 20100322. Workaround for FWNX-273: Fonts not in alphabetical order
				foreach (var name in FontFamily.Families.OrderBy(family => family.Name).Select(family => family.Name))
				{
					m_cboFontNames.Items.Add(name);
				}
			}
			m_fFontListIncludesRealNames = fIncludeRealFontNames;
		}

		/// <summary>
		/// Gets a value indicating whether this tab is currently displaying properties for a
		/// style which inherits from another style or for a WS-specific override for a style.
		/// </summary>
		private bool ShowingInheritedProperties => (m_currentStyleInfo != null && m_currentStyleInfo.Inherits) || m_currentWs > 0;

		/// <summary>
		/// Initialize controls based on whether or not current style inherits from
		/// another style. If not (i.e., this is the "Normal" style), then controls
		/// should not allow the user to pick "unspecified" as the value.
		/// </summary>
		private void InitControlBehavior(bool fInherited)
		{
			if (m_cboFontNames.ShowingInheritedProperties == fInherited)
			{
				return; // Already in the right state
			}
			m_cboFontNames.ShowingInheritedProperties = fInherited;
			m_cboFontSize.ShowingInheritedProperties = fInherited;
		}

		/// <summary>
		/// Updates the writing system descriptions.
		/// </summary>
		private void UpdateWritingSystemDescriptions()
		{
			foreach (ListViewItem item in m_lstWritingSystems.Items)
			{
				var ws = (int)item.Tag;
				var txt = m_currentStyleInfo.FontInfoForWs(ws).ToString(ws < 0 && !m_currentStyleInfo.Inherits);
				if (m_currentStyleInfo.BasedOnStyle == null)
				{
					item.SubItems[1].Text = txt;
				}
				else if (!string.IsNullOrEmpty(txt))
				{
					item.SubItems[1].Text = m_currentStyleInfo.BasedOnStyle.Name + " + " + txt;
				}
				else
				{
					item.SubItems[1].Text = string.Format(ResourceHelper.GetResourceString("kstidStyleWsOverideInheritedFromMsg"), m_currentStyleInfo.BasedOnStyle.Name);
				}
			}
		}

		/// <summary>
		/// Determines whether the specified control value is inherited.
		/// </summary>
		private bool IsInherited(Control c)
		{
			if (!ShowingInheritedProperties)
			{
				return false;
			}
			// This used to be at the last thing checked before returning
			if (c == m_cboFontSize && m_cboFontSize.Text == string.Empty)
			{
				return true;
			}
			if (c is FwInheritablePropComboBox)
			{
				return ((FwInheritablePropComboBox)c).IsInherited;
			}
			if (c is FwColorCombo && ((FwColorCombo)c).IsInherited)
			{
				return true;
			}
			if (c is CheckBox && ((CheckBox)c).CheckState == CheckState.Indeterminate)
			{
				return true;
			}
			return c.ForeColor.ToArgb() != SystemColors.WindowText.ToArgb();
		}

		#endregion
	}
}