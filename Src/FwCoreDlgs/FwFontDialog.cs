// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwFontDialog.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwFontDialog : Form, IFontDialog
	{
		#region Member variables
		/// <summary/>
		protected bool m_fInSelectedIndexChangedHandler;
		private int m_DefaultWs;
		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwFontDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwFontDialog(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			FillFontList();
			m_helpTopicProvider = helpTopicProvider;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the font list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void FillFontList()
		{
			m_lbFontNames.Items.Clear();
			m_lbFontNames.Items.Add(ResourceHelper.GetResourceString("kstidDefaultFont"));

			// Mono doesn't sort the font names currently 20100322. Workaround for FWNX-273: Fonts not in alphabetical order
			var fontNames =
				from family in FontFamily.Families
				orderby family.Name
				select family.Name;
			foreach (var name in fontNames)
				m_lbFontNames.Items.Add(name);
		}

		#region IFontDialog Members
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		void IFontDialog.Initialize(FontInfo fontInfo, bool fAllowSubscript, int ws,
			ILgWritingSystemFactory wsf, FwStyleSheet styleSheet, bool fAlwaysDisableFontFeatures)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		DialogResult IFontDialog.ShowDialog(IWin32Window parent)
		{
			return ShowDialog(parent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the font info.
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		void IFontDialog.SaveFontInfo(FontInfo fontInfo)
		{
			// Font name
			string newValue = GetInternalFontName(m_tbFontName.Text);
			fontInfo.IsDirty |= fontInfo.m_fontName.Save(false, newValue);

			// font size
			int fontSize = FontSize;
			fontInfo.IsDirty |= fontInfo.m_fontSize.Save(false, fontSize * 1000);

			// color
			bool fIsInherited;
			Color color = m_FontAttributes.GetFontColor(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_fontColor.Save(fIsInherited, color);

			// background color
			color = m_FontAttributes.GetBackgroundColor(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_backColor.Save(fIsInherited, color);

			// underline style
			FwUnderlineType underlineType = m_FontAttributes.GetUnderlineType(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_underline.Save(fIsInherited, underlineType);

			// underline color
			color = m_FontAttributes.GetUnderlineColor(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_underlineColor.Save(fIsInherited, color);

			// bold, italic, superscript, subscript
			bool fFlag = m_FontAttributes.GetBold(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_bold.Save(fIsInherited, fFlag);

			fFlag = m_FontAttributes.GetItalic(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_italic.Save(fIsInherited, fFlag);

			if (m_FontAttributes.AllowSuperSubScript)
			{
				FwSuperscriptVal superSub = m_FontAttributes.GetSubSuperscript(out fIsInherited);
				fontInfo.IsDirty |= fontInfo.m_superSub.Save(fIsInherited, superSub);

				// position
				int fontPos = m_FontAttributes.GetFontPosition(out fIsInherited);
				fontInfo.IsDirty |= fontInfo.m_offset.Save(fIsInherited, fontPos);
			}

			// features
			string fontFeatures = m_FontAttributes.GetFontFeatures(out fIsInherited);
			fontInfo.IsDirty |= fontInfo.m_features.Save(fIsInherited, fontFeatures);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether the user can choose a different font
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the font name changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void OnFontNameChanged(object sender, EventArgs e)
		{
			if (m_fInSelectedIndexChangedHandler)
				return;

			int iFontName = m_lbFontNames.FindStringExact(m_tbFontName.Text);
			if (iFontName != ListBox.NoMatches)
			{
				// exact match - select the font name in the list
				m_lbFontNames.SelectedIndex = iFontName;
			}
			else
			{
				// find closest match and scroll that to the top of the list
				for (string text = m_tbFontName.Text; text.Length > 0 && iFontName == ListBox.NoMatches;
					text = text.Substring(0, text.Length - 1))
				{
					iFontName = m_lbFontNames.FindString(text);
				}
				m_lbFontNames.SelectedIndex = -1;
			}

			if (iFontName == ListBox.NoMatches)
				iFontName = 0;

			m_lbFontNames.TopIndex = iFontName;

			m_FontAttributes.FontName = m_tbFontName.Text;

			UpdatePreview();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when selected font name index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnSelectedFontNameIndexChanged(object sender, EventArgs e)
		{
			m_fInSelectedIndexChangedHandler = true;
			try
			{
				if (m_lbFontNames.SelectedIndex > -1)
				{
					int iSelStart = m_tbFontName.SelectionStart;
					m_tbFontName.Text = m_lbFontNames.Text;
					m_FontAttributes.FontName = m_tbFontName.Text;
					if (m_tbFontName.Focused)
						m_tbFontName.SelectionStart = iSelStart;

					UpdatePreview();
				}
			}
			finally
			{
				m_fInSelectedIndexChangedHandler = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when selected font sizes index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnSelectedFontSizesIndexChanged(object sender, EventArgs e)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when font size text changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnFontSizeTextChanged(object sender, EventArgs e)
		{
			if (m_fInSelectedIndexChangedHandler)
				return;

			if (!ApplyNewFontSizeIfValid(m_tbFontSize.Text))
				return;
			SelectFontSizeInList(FontSize.ToString());
			UpdatePreview();
		}

		/// <summary>
		/// Returns true if applied, or false if size is not valid or is not changed.
		/// </summary>
		protected bool ApplyNewFontSizeIfValid(string size)
		{
			bool isNewAndValidSize = UpdateFontSizeIfValid(size);
			if (isNewAndValidSize)
				return true;

			int insertionPointLocationBeforeRevert = m_tbFontSize.SelectionStart;
			m_tbFontSize.Text = FontSize.ToString();
			// Move insertion point back to where it was before the invalid
			// character was rejected, rather than letting it jump to the beginning
			// of the textbox.
			int newInsertionPointLocation = insertionPointLocationBeforeRevert - 1;
			if (newInsertionPointLocation < 0)
				newInsertionPointLocation = 0;
			m_tbFontSize.Select(newInsertionPointLocation, 0);
			return false;
		}

		/// <summary>
		/// Update FontSize from size and return true.
		/// If text size is already set or is not a valid font size, does not update and
		/// returns false.
		/// </summary>
		protected bool UpdateFontSizeIfValid(string size)
		{
			int newSize;
			Int32.TryParse(size, out newSize);
			if (newSize <= 0)
				return false;
			if (newSize > 999)
				return false;
			if (newSize == FontSize)
				return false;
			FontSize = newSize;
			return true;
		}

		/// <summary>
		/// Update the position or selection in the font list based on size.
		/// </summary>
		private void SelectFontSizeInList(string size)
		{
			int iFontSize = m_lbFontSizes.FindStringExact(size);

			if (iFontSize != ListBox.NoMatches)
			{
				// exact match - select the font size in the list
				m_lbFontSizes.SelectedIndex = iFontSize;
			}
			else
			{
				// find closest match and scroll that to the top of the list
				for (string text = size; text.Length > 0 && iFontSize == ListBox.NoMatches;
					text = text.Substring(0, text.Length - 1))
				{
					iFontSize = m_lbFontSizes.FindString(text);
				}
				m_lbFontSizes.SelectedIndex = -1;
			}

			if (iFontSize == ListBox.NoMatches)
				iFontSize = 0;

			m_lbFontSizes.TopIndex = iFontSize;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when one of the font attribute values changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnAttributeValueChanged(object sender, EventArgs e)
		{
			UpdatePreview();
		}
		#endregion

		/// <summary/>
		protected int FontSize
		{
			get; set;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the preview.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void UpdatePreview()
		{
			if (FontSize <= 0)
				return;

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "______", StyleUtils.CharStyleTextProps(null, m_DefaultWs));

			bool fIsInherited;
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, m_tbFontName.Text);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint,
				FontSize * 1000);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
				m_FontAttributes.GetBold(out fIsInherited) ? 1 : 0);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum,
				m_FontAttributes.GetItalic(out fIsInherited) ? 1 : 0);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
				(int)m_FontAttributes.GetUnderlineType(out fIsInherited));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(m_FontAttributes.GetFontColor(out fIsInherited)));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(m_FontAttributes.GetBackgroundColor(out fIsInherited)));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(m_FontAttributes.GetUnderlineColor(out fIsInherited)));
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
				m_DefaultWs);

			strBldr.Replace(3, 3, m_tbFontName.Text, propsBldr.GetTextProps());

			m_preview.Tss = strBldr.GetString();
			m_preview.Invalidate();
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "kstidBulletsAndNumberingSelectFont");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the internal font name for the given UI font.
		/// </summary>
		/// <param name="fontNameUI">UI name of the font.</param>
		/// <returns>Internal font name</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetInternalFontName(string fontNameUI)
		{
			if (fontNameUI == ResourceHelper.GetResourceString("kstidDefaultFont"))
				return StyleServices.DefaultFont;
			return fontNameUI;
		}
	}
}
