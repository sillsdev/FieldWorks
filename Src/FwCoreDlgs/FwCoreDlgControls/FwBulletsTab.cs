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
// File: FwBulletsTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// -------------------------------------------------------------------------- --------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwBulletsTab : UserControl, IFWDisposable, IStylesTab
	{
		#region Member Data
		/// <summary>
		/// Fires when a change is made on the font tab to an unspecified state.
		/// </summary>
		public event EventHandler ChangedToUnspecified;

		/// <summary></summary>
		/// <returns></returns>
		public delegate IFontDialog FontDialogHandler(object sender, EventArgs args);
		/// <summary>Called to bring up the font dialog.</summary>
		public event FontDialogHandler FontDialog;

		private const int m_kDefaultBulletIndex = 1;
		private const int m_kDefaultNumberIndex = 0;
		private bool m_dontUpdateInheritance = true;
		private bool m_DefaultTextDirectionRtoL = false;
		private BulletInfo m_currentStyleBulletInfo;
		private StyleInfo m_StyleInfo;
		private FwStyleSheet m_styleSheet;
		/// <summary>Font info used when bullets is checked</summary>
		private FontInfo m_BulletsFontInfo;
		/// <summary>Font info used when numbered is checked</summary>
		private FontInfo m_NumberFontInfo;
		#endregion

		#region Construction and demolition
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwBulletsTab"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwBulletsTab()
		{
			InitializeComponent();
			m_currentStyleBulletInfo = new BulletInfo();
			UpdateGroupBoxes();
			m_cboBulletScheme.SelectedIndex = m_kDefaultBulletIndex;
			m_cboNumberScheme.SelectedIndex = m_kDefaultNumberIndex;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}

			base.Dispose(disposing);
		}

		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether default text direction is Right-toLeft or not.
		/// </summary>
		/// <remarks>Typically this is the default direction of the view from which this dialog
		/// is invoked.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool DefaultTextDirectionRtoL
		{
			set { CheckDisposed(); m_DefaultTextDirectionRtoL = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the style sheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStyleSheet StyleSheet
		{
			set { m_styleSheet = value; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the form based on a style being selected.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateForStyle(StyleInfo styleInfo)
		{
			CheckDisposed();
			m_dontUpdateInheritance = true;

			bool fDifferentStyle = m_StyleInfo == null ? true : (styleInfo.Name != m_StyleInfo.Name);

			m_StyleInfo = styleInfo;
			m_preview.IsRightToLeft = m_StyleInfo.DirectionIsRightToLeft == TriStateBool.triNotSet ?
				m_DefaultTextDirectionRtoL : m_StyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue;
			m_preview.WritingSystemFactory = m_StyleInfo.Cache.WritingSystemFactory;
			m_preview.WritingSystemCode = m_StyleInfo.Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;

			VwBulNum bulletType;
			// Note: don't assign m_currentStyleBulletInfo until the end of this method
			// since setting some of the values change m_currentStyleBulletInfo before we have set
			// everything.
			BulletInfo bulletInfo = new BulletInfo(styleInfo.IBullet.Value);
			bulletType = bulletInfo.m_numberScheme;

			// If we have a different style, we have to reload the font info. If it is the same
			// style we were here before so we keep the font info that we already have.
			if (fDifferentStyle)
			{
				if ((int)bulletType >= (int)VwBulNum.kvbnBulletBase)
				{
					// use font from style for bullets
					m_BulletsFontInfo = bulletInfo.FontInfo;
					// create a number font based on the font for bullets
					m_NumberFontInfo = new FontInfo(m_BulletsFontInfo);
					m_NumberFontInfo.m_fontName.ResetToInherited(FontInfo.GetUIFontName(
						styleInfo.FontInfoForWs(-1).m_fontName.Value));
				}
				else
				{
					// use font from style for numbers
					m_NumberFontInfo = bulletInfo.FontInfo;

					if (bulletType == VwBulNum.kvbnNone)
					{
						m_NumberFontInfo.m_fontName.ResetToInherited(FontInfo.GetUIFontName(
							styleInfo.FontInfoForWs(-1).m_fontName.Value));
					}

					// create a bullets font based on the font for numbers
					m_BulletsFontInfo = new FontInfo(m_NumberFontInfo);

					// The font for bullets is hard-coded in the views code, so there is no point
					// in letting the user select any other font for bullets.
					m_BulletsFontInfo.m_fontName.ResetToInherited("Quivira");
					m_BulletsFontInfo.m_fontName.SetDefaultValue("Quivira");
				}
			}

			m_nudStartAt.Value = bulletInfo.m_start;
			m_chkStartAt.Checked = (bulletInfo.m_start != 1);

			m_tbTextBefore.Text = bulletInfo.m_textBefore;
			m_tbTextAfter.Text = bulletInfo.m_textAfter;

			m_rbUnspecified.Enabled = styleInfo.Inherits;
			if (styleInfo.IBullet.IsInherited && styleInfo.Inherits)
				m_rbUnspecified.Checked = true;
			else if (bulletType == VwBulNum.kvbnNone)
				m_rbNone.Checked = true;
			else if ((int)bulletType >= (int)VwBulNum.kvbnBulletBase)
				m_rbBullet.Checked = true;
			else // NumberBase
				m_rbNumber.Checked = true;

			m_cboBulletScheme.SelectedIndex = GetBulletIndexForType(bulletType);
			m_cboNumberScheme.SelectedIndex = GetNumberSchemeIndexForType(bulletType);

			m_currentStyleBulletInfo = bulletInfo;
			UpdateBulletSchemeComboBox();

			m_dontUpdateInheritance = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves bullet info into a StyleInfo
		/// </summary>
		/// <param name="styleInfo">styleInfo to save into</param>
		/// ------------------------------------------------------------------------------------
		public void SaveToInfo(StyleInfo styleInfo)
		{
			CheckDisposed();

			// Save the bullet information
			BulletInfo bulInfo = new BulletInfo();
			UpdateBulletInfo(ref bulInfo);

			// Replace the value
			if (styleInfo.IBullet.Save(m_rbUnspecified.Checked, bulInfo))
				styleInfo.Dirty = true;
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index into the bullet combo for the given type of (non-numeric) bullet.
		/// </summary>
		/// <param name="bulletType">Type of the bullet.</param>
		/// <returns>Index (zero-based) into the bullet combo</returns>
		/// ------------------------------------------------------------------------------------
		private int GetBulletIndexForType(VwBulNum bulletType)
		{
			if ((int)bulletType >= (int)VwBulNum.kvbnBulletBase)
				return (int)bulletType - (int)VwBulNum.kvbnBulletBase;
			return m_kDefaultBulletIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the bullet info.
		/// </summary>
		/// <param name="bulInfo">The bullet info.</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateBulletInfo(ref BulletInfo bulInfo)
		{
			if (m_rbNone.Checked)
				bulInfo.m_numberScheme = VwBulNum.kvbnNone;
			else if (m_rbBullet.Checked)
			{
				bulInfo.m_numberScheme = (VwBulNum)((int)VwBulNum.kvbnBulletBase +
					m_cboBulletScheme.SelectedIndex);
				bulInfo.FontInfo = m_BulletsFontInfo;
			}
			else if (m_rbNumber.Checked)
			{
				switch (m_cboNumberScheme.SelectedIndex)
				{
					case 0: bulInfo.m_numberScheme = VwBulNum.kvbnArabic; break;
					case 1: bulInfo.m_numberScheme = VwBulNum.kvbnRomanUpper; break;
					case 2: bulInfo.m_numberScheme = VwBulNum.kvbnRomanLower; break;
					case 3: bulInfo.m_numberScheme = VwBulNum.kvbnLetterUpper; break;
					case 4: bulInfo.m_numberScheme = VwBulNum.kvbnLetterLower; break;
					case 5: bulInfo.m_numberScheme = VwBulNum.kvbnArabic01; break;
				}
				bulInfo.m_start = m_nudStartAt.Value;
				bulInfo.m_textBefore = m_tbTextBefore.Text;
				bulInfo.m_textAfter = m_tbTextAfter.Text;
				bulInfo.FontInfo = m_NumberFontInfo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index into the bullet combo for the given type of (numeric) bullet.
		/// </summary>
		/// <param name="bulletType">Type of the bullet.</param>
		/// <returns>Index (zero-based) into the bullet combo</returns>
		/// ------------------------------------------------------------------------------------
		private int GetNumberSchemeIndexForType(VwBulNum bulletType)
		{
			switch (bulletType)
			{
				case VwBulNum.kvbnArabic: return 0;
				case VwBulNum.kvbnRomanUpper: return 1;
				case VwBulNum.kvbnRomanLower: return 2;
				case VwBulNum.kvbnLetterUpper: return 3;
				case VwBulNum.kvbnLetterLower: return 4;
				case VwBulNum.kvbnArabic01: return 5;
				default:
					return m_kDefaultNumberIndex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the group boxes' enabled states.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateGroupBoxes()
		{
			m_grpBullet.Enabled = m_rbBullet.Checked;
			m_grpNumber.Enabled = m_rbNumber.Checked;
			m_btnFont.Enabled = m_rbBullet.Checked || m_rbNumber.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a radio button gets changed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TypeCheckedChanged(object sender, EventArgs e)
		{
			if (!m_dontUpdateInheritance)
			{
				if (sender == m_rbUnspecified && ChangedToUnspecified != null)
					ChangedToUnspecified(this, EventArgs.Empty);
			}

			if (sender == m_rbNumber && m_nudStartAt.Value == 0)
				m_nudStartAt.Value = 1;
			UpdateGroupBoxes();
			DataChange(sender, e);
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Builds a number string for the preview window based on the settings in the
//		/// Number group and the selected numbering scheme
//		/// </summary>
//		/// <param name="line">The line.</param>
//		/// ------------------------------------------------------------------------------------
//		private string GetNumberString(int line)
//		{
//			return GetNumberString(line, m_nudStartAt.Value, m_cboNumberScheme.SelectedIndex,
//				m_tbTextBefore.Text, m_tbTextAfter.Text);
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Builds a number string for the preview window based on the given values
//		/// </summary>
//		/// <param name="line">The line.</param>
//		/// <param name="nStartAt">The number to start at.</param>
//		/// <param name="iScheme">The i scheme.</param>
//		/// <param name="textBefore">The text before.</param>
//		/// <param name="textAfter">The text after.</param>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		private string GetNumberString(int line, int nStartAt, int iScheme, string textBefore,
//			string textAfter)
//		{
//			int number = nStartAt + line;
//			string numberString = string.Empty;
//			switch (iScheme)
//			{
//				case 0:		// 1, 2, 3'
//					numberString = number.ToString();
//					break;

//				case 1:		// I, II, III (Roman numerals)
//					numberString = RomanNumerals.IntToRoman(number);
//					break;

//				case 2:		// i, ii, iii (lower case Roman numerals)
//					numberString = RomanNumerals.IntToRoman(number).ToLowerInvariant();
//					break;

//				case 3:		// A, B, C
//					numberString = AlphaOutline.NumToAlphaOutline(number);
//					break;

//				case 4:		// a, b, c
//					numberString = AlphaOutline.NumToAlphaOutline(number).ToLowerInvariant();
//					break;

//				case 5:		// 01, 02, 03
//					numberString = number.ToString("d2");
//					break;
//			}

//			return textBefore + numberString + textAfter;
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When any data field changes, refresh the preview panel
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DataChange(object sender, EventArgs e)
		{
			// If the value in the "start at" spinner control is other than 1, then check
			// the start at check box
			if (m_nudStartAt.Value != 1)
				m_chkStartAt.Checked = true;

			UpdateBulletInfo(ref m_currentStyleBulletInfo);
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			m_currentStyleBulletInfo.ConvertAsTextProps(propsBldr);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore,
					(int)FwTextPropVar.ktpvMilliPoint, 6000);
			ITsTextProps propsFirst = propsBldr.GetTextProps();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBulNumStartAt, -1, -1);

			m_preview.SetProps(propsFirst, propsBldr.GetTextProps());
			m_preview.Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the m_chkStartAt control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_chkStartAt_CheckedChanged(object sender, EventArgs e)
		{
			// When the "start at" check box is unchecked, change the value in the
			// spinner control to 1.
			if (!m_chkStartAt.Checked)
				m_nudStartAt.Value = 1;
			DataChange(sender, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the number scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_cboNumberScheme_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (m_cboNumberScheme.SelectedIndex)
			{
				case 0:		// 1, 2, 3'
					m_nudStartAt.Mode = DataUpDownMode.Normal;
					break;

				case 1:		// I, II, III (Roman numerals)
					m_nudStartAt.Mode = DataUpDownMode.Roman;
					break;

				case 2:		// i, ii, iii (lower case Roman numerals)
					m_nudStartAt.Mode = DataUpDownMode.RomanLowerCase;
					break;

				case 3:		// A, B, C
					m_nudStartAt.Mode = DataUpDownMode.Letters;
					break;

				case 4:		// a, b, c
					m_nudStartAt.Mode = DataUpDownMode.LettersLowerCase;
					break;

				case 5:		// 01, 02, 03
					m_nudStartAt.Mode = DataUpDownMode.Normal;
					break;
			}
			DataChange(sender, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles clicking the "Bullet and Number Font" button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnFont_Click(object sender, EventArgs e)
		{
			if (FontDialog != null)
			{
				using (IFontDialog fontDialog = FontDialog(this, EventArgs.Empty))
				{
					FontInfo fontInfo;
					if (m_rbBullet.Checked)
					{
						fontInfo = m_BulletsFontInfo;
						fontDialog.CanChooseFont = false;
					}
					else
						fontInfo = m_NumberFontInfo;

					// ENHANCE: change the last parameter when the views code can handle font
					// features for bullets/numbers
					fontDialog.Initialize(
						fontInfo,
						false,
						m_StyleInfo.Cache.ServiceLocator.WritingSystemManager.UserWs,
						m_StyleInfo.Cache.WritingSystemFactory, m_styleSheet, true);

					if (fontDialog.ShowDialog(Parent) == DialogResult.OK)
					{
						if (m_rbBullet.Checked)
						{
							fontDialog.SaveFontInfo(m_BulletsFontInfo);

							// Update the combo box with the new values
							UpdateBulletSchemeComboBox();
						}
						else
							fontDialog.SaveFontInfo(m_NumberFontInfo);
						DataChange(sender, EventArgs.Empty);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the bullet scheme combo box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateBulletSchemeComboBox()
		{
			// NOTE: we don't show underline in the combo box, and we make the entire
			// combo box with the background color. If we want to reflect more closely
			// what the views code does we'd have to implement a views combo box.
			m_cboBulletScheme.ForeColor = m_BulletsFontInfo.m_fontColor.Value;
			m_cboBulletScheme.BackColor = m_BulletsFontInfo.m_backColor.Value;

			FontStyle newStyle = FontStyle.Regular;
			if (m_BulletsFontInfo.m_bold.Value)
				newStyle |= FontStyle.Bold;
			if (m_BulletsFontInfo.m_italic.Value)
				newStyle |= FontStyle.Italic;
			if (m_cboBulletScheme.Font.Style != newStyle)
				m_cboBulletScheme.Font = new Font(m_cboBulletScheme.Font, newStyle);

			//m_cboBulletScheme.Refresh();
		}
		#endregion
	}
}
