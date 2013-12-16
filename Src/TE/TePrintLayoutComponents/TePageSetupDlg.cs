// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TePageSetupDlg.cs
// Responsibility: TE Team

using System.Collections.Generic;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TePageSetupDlg : PageSetupDlg
	{
		#region Member variables
		private bool m_fIsTrialPublication;
//		private decimal m_standardLeadingFactor;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TePageSetupDlg"/> class.
		/// </summary>
		/// <param name="pgLayout">The page layout.</param>
		/// <param name="scr">The Scripture object (which owns the publications).</param>
		/// <param name="publication">The publication.</param>
		/// <param name="division">The division. The NumberOfColumns in the division should be
		/// set before calling this dialog.</param>
		/// <param name="teMainWnd">TE main window (provides callbacks).</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="fIsTrialPub">if set to <c>true</c> view from which this dialog
		/// was brought up is Trial Publication view.</param>
		/// <param name="pubPageSizes">The page sizes available for publication.</param>
		/// ------------------------------------------------------------------------------------
		public TePageSetupDlg(IPubPageLayout pgLayout, IScripture scr,
			IPublication publication, IPubDivision division, IPageSetupCallbacks teMainWnd,
			IHelpTopicProvider helpTopicProvider, IApp app, bool fIsTrialPub,
			List<PubPageInfo> pubPageSizes) :
			base(pgLayout, scr, publication, division, teMainWnd, helpTopicProvider,
				app, pubPageSizes)
		{
			m_fIsTrialPublication = fIsTrialPub;
//			if (!m_chkNonStdChoices.Checked) // following the standard
//				m_standardLeadingFactor = m_nudLineSpacing.Value / m_nudBaseCharSize.Value;
		}
		#endregion

		#region Private and protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a trial publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool IsTrialPublication
		{
			get { return m_fIsTrialPublication; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the selected publication size corresponds to the
		/// IPUB standard "larger" Bible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsBiggishBible
		{
			get
			{
				return (SelectedPubPage.Height == (int)(8.7 * 72000) &&
				SelectedPubPage.Width == (int)(5.8 * 72000));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the selected publication size corresponds to the
		/// IPUB standard "smaller" Bible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsSmallishBible
		{
			get
			{
				return (SelectedPubPage.Height == (int)(8.25 * 72000) &&
				SelectedPubPage.Width == (int)(5.25 * 72000));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the top margin according to iPub standards for Large or Small Bibles based
		/// on base character size and whether the layout is two-column.
		/// </summary>
		/// <remarks>This should only be used to get the margin for iPub standard publications,
		/// not for full-page layout.</remarks>
		/// ------------------------------------------------------------------------------------
		protected int MarginTop
		{
			get
			{
				if (IsBiggishBible)
				{
					if (IsTwoColumnPrintLayout)
					{
						switch (BaseCharacterSize / 1000)
						{
							case 9:
								return 50000;
							case 10:
							case 11:
								return 49000;
						}
					}
					else // one-column
					{
						return 53000;
					}
				}
				else if (IsSmallishBible)
				{
					if (IsTwoColumnPrintLayout)
					{
						switch (BaseCharacterSize / 1000)
						{
							case 9:
								return 49000;
							case 10:
								return 51000;
							case 11:
								return 50000;
						}
					}
					else // one-column
					{
						return 50000;
					}
				}

				return base.m_udmTop.MeasureValue;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the bottom margin according to iPub standards for Large or Small Bibles based
		/// on base character size and whether the layout is two-column.
		/// </summary>
		/// <remarks>This should only be used to get the margin for iPub standard publications,
		/// not for full-page layout.</remarks>
		/// ------------------------------------------------------------------------------------
		protected int MarginBottom
		{
			get
			{
				if (IsBiggishBible)
				{
					if (IsTwoColumnPrintLayout)
					{
						return 36400;
					}
					else // one-column
					{
						return 39400;
					}
				}
				else if (IsSmallishBible)
				{
					if (IsTwoColumnPrintLayout)
					{
						switch (BaseCharacterSize / 1000)
						{
							case 9:
							case 10:
								return 38000;
							case 11:
								return 36000;
						}
					}
					else // one-column
					{
						return 36000;
					}
				}

				return base.m_udmBottom.MeasureValue;
			}
		}
		#endregion

		#region Overridden properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the settings are within iPub standards.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool FollowsStandardSettings
		{
			get
			{
				if (((PubPageInfo)cboPubPageSize.SelectedItem).Height == 0)
					return base.FollowsStandardSettings;

				if (IsTwoColumnPrintLayout)
				{
					if (m_nudBaseCharSize.Value < 9 || m_nudBaseCharSize.Value > 11)
						return false;
				}
				else // Is one column
				{
					if (m_nudBaseCharSize.Value != 11)
						return false;
				}
				return (m_nudLineSpacing.Value == StandardLineSpacingForBaseCharSize);
			}
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the standard leading percentage.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//protected override decimal StandardLeadingFactor
		//{
		//    get
		//    {
		//        return m_standardLeadingFactor > 0 ? m_standardLeadingFactor : base.StandardLeadingFactor;
		//    }
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the the standard line spacing based on the current base character size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override decimal StandardLineSpacingForBaseCharSize
		{
			get
			{
				if (m_nudBaseCharSize.Value >= 9 && m_nudBaseCharSize.Value <= 11)
					return m_nudBaseCharSize.Value + 2;
				return base.StandardLineSpacingForBaseCharSize;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the standard base char size based on the current line spacing value.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override decimal StandardBaseCharSizeForLineSpacing
		{
			get
			{
				if (m_nudLineSpacing.Value >= 11 && m_nudLineSpacing.Value <= 13)
					return m_nudLineSpacing.Value - 2;
				return base.StandardBaseCharSizeForLineSpacing;
			}
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the margin controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void UpdateMarginControls()
		{
			base.UpdateMarginControls();

			if (!IsFullPage)
			{
				m_udmTop.MeasureValue = MarginTop;
				m_udmBottom.MeasureValue = MarginBottom;
				m_udmLeft.MeasureValue = m_udmRight.MeasureValue = 36000; // 1/2 inch
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the min and max values for the base font size and line spacing controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void UpdateBaseSizeControlsMinAndMax()
		{
			if (!IsSpecialPageSize || m_chkNonStdChoices.Checked)
			{
				base.UpdateBaseSizeControlsMinAndMax();
				return;
			}

			// Changing the base sizes just for the sake of a column change should not cause
			// us to save the new values to the DB. We want to be able to keep some base font
			// size and line spacing values as unspecified (0) to use the values from the
			// normal style (i.e., if the user switches back from 1-column to 2-column, the
			// program should go back to using 10 on 12 spacing if the user hasn't changed
			// the defaults.)
			bool fSaveFlag = m_fSaveBaseFontAndLineSizes;
			if (NumberOfColumns == 1)
			{
				// The value needs to be in the proper range before we can change the minimum
				// and maximum.
				m_fSetFontSizeAndLineSpacing = true;
				m_nudBaseCharSize.Value = SnapValueToRange(11, 11, m_nudBaseCharSize.Value);
				m_nudLineSpacing.Value = SnapValueToRange(13, 13, m_nudLineSpacing.Value);
//				m_standardLeadingFactor = m_nudLineSpacing.Value / m_nudBaseCharSize.Value;
				m_fSetFontSizeAndLineSpacing = false;

				m_nudBaseCharSize.Minimum = m_nudBaseCharSize.Maximum = 11;
				m_nudLineSpacing.Minimum = m_nudLineSpacing.Maximum = 13;
			}
			else
			{
				m_fSetFontSizeAndLineSpacing = true;
				m_nudBaseCharSize.Value = SnapValueToRange(9, 11, m_nudBaseCharSize.Value);
				m_nudLineSpacing.Value = SnapValueToRange(11, 13, m_nudLineSpacing.Value);
//				m_standardLeadingFactor = m_nudLineSpacing.Value / m_nudBaseCharSize.Value;
				m_fSetFontSizeAndLineSpacing = false;

				m_nudBaseCharSize.Minimum = 9;
				m_nudBaseCharSize.Maximum = 11;
				m_nudLineSpacing.Minimum = 11;
				m_nudLineSpacing.Maximum = 13;
			}
			m_fSaveBaseFontAndLineSizes = fSaveFlag;
		}
		#endregion
	}
}
