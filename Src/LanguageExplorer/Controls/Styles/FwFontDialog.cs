// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary />
	public partial class FwFontDialog : Form, IFontDialog
	{
		#region Member variables
		/// <summary/>
		protected bool m_fInSelectedIndexChangedHandler;
		private int m_DefaultWs;
		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		/// <summary />
		public FwFontDialog(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			FillFontList();
			m_helpTopicProvider = helpTopicProvider;
		}

		/// <summary>
		/// Fills the font list.
		/// </summary>
		protected internal void FillFontList()
		{
			m_lbFontNames.Items.Clear();
			m_lbFontNames.Items.Add(ResourceHelper.GetResourceString("kstidDefaultFont"));
			// Mono doesn't sort the font names currently 20100322. Workaround for FWNX-273: Fonts not in alphabetical order
			foreach (var name in FontFamily.Families.OrderBy(family => family.Name).Select(family => family.Name))
			{
				m_lbFontNames.Items.Add(name);
			}
		}

		internal ListBox FontNamesListBox => m_lbFontNames;

		#region IFontDialog Members

		/// <summary>
		/// Initializes the specified font info.
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// <param name="fAllowSubscript"><c>true</c> to allow super/subscripts, <c>false</c>
		/// to disable the controls (used when called from Borders and Bullets tab)</param>
		/// <param name="ws">The default writing system (usually UI ws)</param>
		/// <param name="wsf">The writing system factory</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="fAlwaysDisableFontFeatures"><c>true</c> to disable the Font Features
		/// button even when a Graphite font is selected.</param>
		void IFontDialog.Initialize(FontInfo fontInfo, bool fAllowSubscript, int ws, ILgWritingSystemFactory wsf, LcmStyleSheet styleSheet, bool fAlwaysDisableFontFeatures)
		{
			m_DefaultWs = ws;
			m_preview.WritingSystemFactory = wsf;
			m_preview.WritingSystemCode = ws;
			m_preview.StyleSheet = styleSheet;
			m_tbFontName.Text = fontInfo.UIFontName();
			FontSize = fontInfo.m_fontSize.Value / 1000;
			m_tbFontSize.Text = FontSize.ToString(CultureInfo.InvariantCulture);
			m_FontAttributes.UpdateForStyle(fontInfo);
			m_FontAttributes.AllowSuperSubScript = fAllowSubscript;
			m_FontAttributes.AlwaysDisableFontFeatures = fAlwaysDisableFontFeatures;
			UpdatePreview();
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		DialogResult IFontDialog.ShowDialog(IWin32Window parent)
		{
			return ShowDialog(parent);
		}

		/// <summary>
		/// Saves the font info.
		/// </summary>
		void IFontDialog.SaveFontInfo(FontInfo fontInfo)
		{
			// Font name
			var newValue = GetInternalFontName(m_tbFontName.Text);
			fontInfo.IsDirty |= fontInfo.m_fontName.Save(false, newValue);
			// font size
			var fontSize = FontSize;
			fontInfo.IsDirty |= fontInfo.m_fontSize.Save(false, fontSize * 1000);
			// color
			bool fIsInherited;
			var color = m_FontAttributes.GetFontColor(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_fontColor.Save(fIsInherited, color);
			// background color
			color = m_FontAttributes.GetBackgroundColor(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_backColor.Save(fIsInherited, color);
			// underline style
			var underlineType = m_FontAttributes.GetUnderlineType(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_underline.Save(fIsInherited, underlineType);
			// underline color
			color = m_FontAttributes.GetUnderlineColor(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_underlineColor.Save(fIsInherited, color);
			// bold, italic, superscript, subscript
			var fFlag = m_FontAttributes.GetBold(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_bold.Save(fIsInherited, fFlag);
			fFlag = m_FontAttributes.GetItalic(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_italic.Save(fIsInherited, fFlag);
			if (m_FontAttributes.AllowSuperSubScript)
			{
				var superSub = m_FontAttributes.GetSubSuperscript(out fIsInherited);
				fontInfo.IsDirty |= fontInfo.m_superSub.Save(fIsInherited, superSub);
				// position
				var fontPos = m_FontAttributes.GetFontPosition(out fIsInherited);
				fontInfo.IsDirty |= fontInfo.m_offset.Save(fIsInherited, fontPos);
			}
			// features
			var fontFeatures = m_FontAttributes.GetFontFeatures(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_features.Save(fIsInherited, fontFeatures);
		}

		/// <summary>
		/// Sets a value indicating whether the user can choose a different font
		/// </summary>
		bool IFontDialog.CanChooseFont
		{
			set
			{
				m_tbFontName.Enabled = false;
				m_lbFontNames.Enabled = false;
			}
		}
		#endregion

		#region Event handler

		/// <summary>
		/// Called when the font name changed.
		/// </summary>
		private void OnFontNameChanged(object sender, EventArgs e)
		{
			if (m_fInSelectedIndexChangedHandler)
			{
				return;
			}
			var iFontName = m_lbFontNames.FindStringExact(m_tbFontName.Text);
			if (iFontName != ListBox.NoMatches)
			{
				// exact match - select the font name in the list
				m_lbFontNames.SelectedIndex = iFontName;
			}
			else
			{
				// find closest match and scroll that to the top of the list
				for (var text = m_tbFontName.Text; text.Length > 0 && iFontName == ListBox.NoMatches; text = text.Substring(0, text.Length - 1))
				{
					iFontName = m_lbFontNames.FindString(text);
				}
				m_lbFontNames.SelectedIndex = -1;
			}
			if (iFontName == ListBox.NoMatches)
			{
				iFontName = 0;
			}
			m_lbFontNames.TopIndex = iFontName;
			m_FontAttributes.FontName = m_tbFontName.Text;
			UpdatePreview();
		}

		/// <summary>
		/// Called when selected font name index changed.
		/// </summary>
		private void OnSelectedFontNameIndexChanged(object sender, EventArgs e)
		{
			m_fInSelectedIndexChangedHandler = true;
			try
			{
				if (m_lbFontNames.SelectedIndex > -1)
				{
					var iSelStart = m_tbFontName.SelectionStart;
					m_tbFontName.Text = m_lbFontNames.Text;
					m_FontAttributes.FontName = m_tbFontName.Text;
					if (m_tbFontName.Focused)
					{
						m_tbFontName.SelectionStart = iSelStart;
					}
					UpdatePreview();
				}
			}
			finally
			{
				m_fInSelectedIndexChangedHandler = false;
			}
		}

		/// <summary>
		/// Called when selected font sizes index changed.
		/// </summary>
		protected internal void OnSelectedFontSizesIndexChanged(object sender, EventArgs e)
		{
			m_fInSelectedIndexChangedHandler = true;
			try
			{
				if (m_lbFontSizes.SelectedIndex > -1)
				{
					m_tbFontSize.Text = m_lbFontSizes.Text;
					ApplyNewFontSizeIfValid(m_tbFontSize.Text);
					UpdatePreview();
				}
			}
			finally
			{
				m_fInSelectedIndexChangedHandler = false;
			}
		}

		/// <summary>
		/// Called when font size text changed.
		/// </summary>
		private void OnFontSizeTextChanged(object sender, EventArgs e)
		{
			if (m_fInSelectedIndexChangedHandler)
			{
				return;
			}
			if (!ApplyNewFontSizeIfValid(m_tbFontSize.Text))
			{
				if (m_lbFontSizes.SelectedIndex == -1)
				{
					m_lbFontSizes.SelectedIndex = m_lbFontSizes.FindStringExact(m_tbFontSize.Text);
				}
				return;
			}
			SelectFontSizeInList(FontSize.ToString());
			UpdatePreview();
		}

		/// <summary>
		/// Returns true if applied, or false if size is not valid or is not changed.
		/// </summary>
		protected internal bool ApplyNewFontSizeIfValid(string size)
		{
			var isNewAndValidSize = UpdateFontSizeIfValid(size);
			if (isNewAndValidSize)
			{
				return true;
			}
			var insertionPointLocationBeforeRevert = m_tbFontSize.SelectionStart;
			m_tbFontSize.Text = FontSize.ToString();
			// Move insertion point back to where it was before the invalid
			// character was rejected, rather than letting it jump to the beginning
			// of the textbox.
			var newInsertionPointLocation = insertionPointLocationBeforeRevert - 1;
			if (newInsertionPointLocation < 0)
			{
				newInsertionPointLocation = 0;
			}
			m_tbFontSize.Select(newInsertionPointLocation, 0);
			return false;
		}

		/// <summary>
		/// Update FontSize from size and return true.
		/// If text size is already set or is not a valid font size, does not update and
		/// returns false.
		/// </summary>
		protected internal bool UpdateFontSizeIfValid(string size)
		{
			int newSize;
			int.TryParse(size, out newSize);
			if (newSize <= 0)
			{
				return false;
			}
			if (newSize > 999)
			{
				return false;
			}
			if (newSize == FontSize)
			{
				return false;
			}
			FontSize = newSize;
			return true;
		}

		/// <summary>
		/// Update the position or selection in the font list based on size.
		/// </summary>
		private void SelectFontSizeInList(string size)
		{
			var iFontSize = m_lbFontSizes.FindStringExact(size);
			if (iFontSize != ListBox.NoMatches)
			{
				// exact match - select the font size in the list
				m_lbFontSizes.SelectedIndex = iFontSize;
			}
			else
			{
				// find closest match and scroll that to the top of the list
				for (var text = size; text.Length > 0 && iFontSize == ListBox.NoMatches; text = text.Substring(0, text.Length - 1))
				{
					iFontSize = m_lbFontSizes.FindString(text);
				}
				m_lbFontSizes.SelectedIndex = -1;
			}
			if (iFontSize == ListBox.NoMatches)
			{
				iFontSize = 0;
			}
			m_lbFontSizes.TopIndex = iFontSize;
		}

		/// <summary>
		/// Called when one of the font attribute values changed.
		/// </summary>
		private void OnAttributeValueChanged(object sender, EventArgs e)
		{
			UpdatePreview();
		}
		#endregion

		/// <summary />
		protected internal int FontSize
		{
			get; set;
		}

		/// <summary />
		internal bool InSelectedIndexChangedHandler
		{
			get { return m_fInSelectedIndexChangedHandler; }
			set { m_fInSelectedIndexChangedHandler = value; }
		}

		/// <summary />
		internal TextBox FontSizeTextBox => m_tbFontSize;

		/// <summary />
		internal ListBox FontSizesListBox => m_lbFontSizes;

		/// <summary>
		/// Updates the preview.
		/// </summary>
		protected virtual void UpdatePreview()
		{
			if (FontSize <= 0)
			{
				return;
			}
			var strBldr = TsStringUtils.MakeStrBldr();
			strBldr.Replace(0, 0, "______", StyleUtils.CharStyleTextProps(null, m_DefaultWs));
			var propsBldr = TsStringUtils.MakePropsBldr();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, m_tbFontName.Text);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, FontSize * 1000);
			bool fIsInherited;
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, m_FontAttributes.GetBold(out fIsInherited) ? 1 : 0);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, m_FontAttributes.GetItalic(out fIsInherited) ? 1 : 0);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, (int)m_FontAttributes.GetUnderlineType(out fIsInherited));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(m_FontAttributes.GetFontColor(out fIsInherited)));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(m_FontAttributes.GetBackgroundColor(out fIsInherited)));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(m_FontAttributes.GetUnderlineColor(out fIsInherited)));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_DefaultWs);
			strBldr.Replace(3, 3, m_tbFontName.Text, propsBldr.GetTextProps());
			m_preview.Tss = strBldr.GetString();
			m_preview.Invalidate();
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "kstidBulletsAndNumberingSelectFont");
		}

		/// <summary>
		/// Gets the internal font name for the given UI font.
		/// </summary>
		private static string GetInternalFontName(string fontNameUI)
		{
			return fontNameUI == ResourceHelper.GetResourceString("kstidDefaultFont") ? StyleServices.DefaultFont : fontNameUI;
		}
	}
}