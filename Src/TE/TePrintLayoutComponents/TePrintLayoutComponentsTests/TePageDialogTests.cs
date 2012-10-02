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
// File: TePageDialogTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Printing;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyTePageSetupDlg : TePageSetupDlg
	{
		#region Constants
		/// <summary>Converts 100ths of an inch to millipoints</summary>
		public const int kCentiInchToMilliPoints = 720;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test class that mocks the PageSetupDlg.
		/// </summary>
		/// <param name="wsUser">The user writing system.</param>
		/// <param name="pgLayout">The page layout.</param>
		/// <param name="publication">The publication.</param>
		/// <param name="division">The division.</param>
		/// <param name="units">The user's prefered measurement units.</param>
		/// <param name="fIsTrialPub">if set to <c>true</c> view from which this dialog
		/// was brought up is "Trial Publication".</param>
		/// <param name="pubPageSizes">The publication page info.</param>
		/// ------------------------------------------------------------------------------------
		public DummyTePageSetupDlg(int wsUser, IPubPageLayout pgLayout, IPublication publication,
			IPubDivision division, MsrSysType units, bool fIsTrialPub, List<PubPageInfo> pubPageSizes)
			:
			base(wsUser, pgLayout, null, publication, division, null, null, units, fIsTrialPub, pubPageSizes)
		{
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the index of the page size combobox:
		///   0 - full page size
		///   1 - small Bible
		///   2 - large Bible
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageSizeComboIndex
		{
			set
			{
				cboPubPageSize.SelectedIndex = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page size combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox PageSizeCombo
		{
			get { return cboPubPageSize; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the index of the publication page size combobox:
		///   0 - full page size
		///   1 - small Bible
		///   2 - large Bible
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PubSizeComboIndex
		{
			set
			{
				cboPubPageSize.SelectedIndex = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page size combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox PaperSizeCombo
		{
			get { return cbPaperSize; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the pub size combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox PubSizeCombo
		{
			get { return cboPubPageSize; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the size of the paper in the paper size combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PaperSizeName
		{
			get
			{
				return cbPaperSize.SelectedIndex != -1 ?
					cbPaperSize.Items[cbPaperSize.SelectedIndex].ToString() : string.Empty;
			}
			set
			{
				int index = cbPaperSize.FindStringExact(value);
				cbPaperSize.SelectedIndex = index;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the height of the paper size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PaperSizeHeight
		{
			get { return m_udmPaperHeight.MeasureValue; }
			set { m_udmPaperHeight.MeasureValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the width of the paper size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PaperSizeWidth
		{
			get { return m_udmPaperWidth.MeasureValue; }
			set { m_udmPaperWidth.MeasureValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value in the numeric up-down control for the base character size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BaseCharSizeControlValue
		{
			set { m_nudBaseCharSize.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value in the numeric up-down control for the line spacing control value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LineSpacingControlValue
		{
			set { m_nudLineSpacing.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets a value indicating whether this instance has the "Allow non-standard choices"
		/// checkbox checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsAllowNonStandardChecked
		{
			get { return m_chkNonStdChoices.Checked; }
			set { m_chkNonStdChoices.Checked = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a two column print layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool IsTwoColumnPrintLayout
		{
			get { return base.IsTwoColumnPrintLayout; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a trial publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool IsTrialPublication
		{
			get { return base.IsTrialPublication; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the size of the base character in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new int BaseCharacterSize
		{
			get { return (int)base.BaseCharacterSize; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the line spacing in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new int BaseLineSpacing
		{
			get { return (int)base.BaseLineSpacing; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value in the numeric up-down control for the line spacing control value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BaseLineSpacingControlValue
		{
			set { m_nudLineSpacing.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the top margin according to iPub standards for Large or Small Bibles based
		/// on base character size and whether the layout is two-column.
		/// </summary>
		/// <remarks>This should only be used to get the margin for iPub standard publications,
		/// not for full-page layout.</remarks>
		/// ------------------------------------------------------------------------------------
		public new int MarginTop
		{
			get { return base.m_udmTop.MeasureValue; }
			set { base.m_udmTop.MeasureValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the bottom margin according to iPub standards for Large or Small Bibles based
		/// on base character size and whether the layout is two-column.
		/// </summary>
		/// <remarks>This should only be used to get the margin for iPub standard publications,
		/// not for full-page layout.</remarks>
		/// ------------------------------------------------------------------------------------
		public new int MarginBottom
		{
			get { return base.m_udmBottom.MeasureValue; }
			set { base.m_udmBottom.MeasureValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the right margin according to iPub standards for Large or Small Bibles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MarginRight
		{
			get { return base.m_udmRight.MeasureValue; }
			set { base.m_udmRight.MeasureValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the left margin according to iPub standards for Large or Small Bibles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MarginLeft
		{
			get { return base.m_udmLeft.MeasureValue; }
			set { base.m_udmLeft.MeasureValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the FollowsStandardSettings property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool FollowsStandardSettings
		{
			get { return base.FollowsStandardSettings; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets a value indicating whether or not non-standard choices are allowed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool AllowNonStandardChoices
		{
			set { m_chkNonStdChoices.Checked = value; }
		}
		#endregion

		#region Overridden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the dialog settings.
		/// </summary>
		/// <returns>the error status after checking the settings in the Page Setup dialog.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public PageSetupErrorType CallValidateDialogSettings()
		{
			return base.ValidateDialogSettings();
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the  paper size specified by name in the Page Setup dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PaperSize GetPaperSize(string name)
		{
			foreach (PaperSize size in cbPaperSize.Items)
			{
				if (size.PaperName == name)
					return size;
			}

			return null;
		}
		#endregion


	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for settings in the Page Setup dialog that are specific to Translation Editor.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TePageDialogTests : InMemoryFdoTestBase
	{
		#region ExpectedMargin struct
		internal struct ExpectedMarginType
		{
			internal int TopMargin;
			internal int BottomMargin;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Constructs an ExpectedMarginType structure.
			/// </summary>
			/// <param name="topMargin">The top margin.</param>
			/// <param name="bottomMargin">The bottom margin.</param>
			/// --------------------------------------------------------------------------------
			internal ExpectedMarginType(int topMargin, int bottomMargin)
			{
				TopMargin = topMargin;
				BottomMargin = bottomMargin;
			}
		}
		#endregion

		#region Constants
		// Define indices for the page layout size combo box.
		private const int kFullPage = 0;
		private const int kSmallBible = 1;
		private const int kLargeBible = 2;
		#endregion

		#region Member variables
		private DummyTePageSetupDlg m_dlg;
		private int m_wsUser;
		private IPublication m_pub;
		private IPubDivision m_div;
		private IPubPageLayout m_pgl;
		private List<PubPageInfo> m_pubPageInfo;
		#endregion

		#region Setup and teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_wsUser = m_inMemoryCache.Cache.DefaultUserWs;
			m_inMemoryCache.InitializeAnnotationDefs();
			m_pub = m_inMemoryCache.CreatePublication(0, 0, false, "TestPub", 0,
				BindingSide.Left, 0);

			m_div = m_inMemoryCache.AddDivisionToPub(m_pub, false, false,
				DivisionStartOption.Continuous);
			m_pgl = new PubPageLayout() as IPubPageLayout;
			m_div.PageLayoutOA = m_pgl;
			m_div.PageLayoutOAHvo = m_pgl.Hvo;

			m_pubPageInfo = new List<PubPageInfo>();
			m_pubPageInfo.Add(new PubPageInfo("Full Page", 0, 0));
			m_pubPageInfo.Add(new PubPageInfo("5.25 x 8.25", (int)(8.25 * 72000), (int)(5.25 * 72000)));
			m_pubPageInfo.Add(new PubPageInfo("5.8 x 8.7", (int)(8.7 * 72000), (int)(5.8 * 72000)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			try
			{
				if (m_dlg != null)
				{
					m_dlg.Dispose();
					m_dlg = null;
				}
				if (m_pubPageInfo != null)
				{
					m_pubPageInfo.Clear();
					m_pubPageInfo = null;
				}
			}
			finally
			{
				base.Exit();
			}
		}
		#endregion

		#region Tests for iPub settings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the iPub settings in the Page Setup dialog for large, one-column Bibles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void iPubSettings_LargeBible_OneColumn()
		{
			CheckDisposed();

			m_div.NumColumns = 1;
			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);
			m_dlg.PubSizeCombo.SelectedIndex = kLargeBible;
			m_dlg.BaseCharSizeControlValue = 11;
			m_dlg.PaperSizeName = ResourceHelper.GetResourceString("kstidPaperSizeA4");

			// Verify that the settings are according to iPub specs.
			Assert.AreEqual(53000, m_dlg.MarginTop);
			Assert.AreEqual(39400, m_dlg.MarginBottom);
			Assert.AreEqual(36000, m_dlg.MarginLeft);
			Assert.AreEqual(36000, m_dlg.MarginRight);
			Assert.AreEqual(13000, m_dlg.BaseLineSpacing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the iPub settings in the Page Setup dialog for large, two-column Bibles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void iPubSettings_LargeBible_TwoColumn()
		{
			CheckDisposed();

			m_div.NumColumns = 2;
			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);
			m_dlg.PubSizeCombo.SelectedIndex = kLargeBible;
			m_dlg.LineSpacingControlValue = 11;

			// Setup expected top and bottom margins for different line spacing sizes of
			// 11, 12 and 13. The left and right margins are always 36000 millipoints (1/2").
			Dictionary<int, ExpectedMarginType> ExpectedMargins =
				new Dictionary<int, ExpectedMarginType>(3);
			ExpectedMargins.Add(11, new ExpectedMarginType(50000, 36400));
			ExpectedMargins.Add(12, new ExpectedMarginType(49000, 36400));
			ExpectedMargins.Add(13, new ExpectedMarginType(49000, 36400));

			// Check the settings for the margins at different line spacing sizes.
			for (int lineSpacingSize = 11; lineSpacingSize <= 13; lineSpacingSize++)
			{
				m_dlg.LineSpacingControlValue = lineSpacingSize;

				// Verify that the settings are according to iPub specs.
				ExpectedMarginType expectedMargin = ExpectedMargins[lineSpacingSize];
				Assert.AreEqual(expectedMargin.TopMargin, m_dlg.MarginTop);
				Assert.AreEqual(expectedMargin.BottomMargin, m_dlg.MarginBottom);
				Assert.AreEqual(36000, m_dlg.MarginLeft);
				Assert.AreEqual(36000, m_dlg.MarginRight);
				Assert.AreEqual(lineSpacingSize - 2, m_dlg.BaseCharacterSize / 1000);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the iPub settings in the Page Setup dialog for small, one-column Bibles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void iPubSettings_SmallBible_OneColumn()
		{
			CheckDisposed();

			m_div.NumColumns = 1;
			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);
			m_dlg.PubSizeCombo.SelectedIndex = kSmallBible;
			m_dlg.BaseCharSizeControlValue = 11;

			// Verify that the settings are according to iPub specs.
			Assert.AreEqual(50000, m_dlg.MarginTop);
			Assert.AreEqual(36000, m_dlg.MarginBottom);
			Assert.AreEqual(36000, m_dlg.MarginLeft);
			Assert.AreEqual(36000, m_dlg.MarginRight);
			Assert.AreEqual(13000, m_dlg.BaseLineSpacing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the iPub settings in the Page Setup dialog for small, two-column Bibles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void iPubSettings_SmallBible_TwoColumn()
		{
			CheckDisposed();

			m_div.NumColumns = 2;
			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);
			m_dlg.PubSizeCombo.SelectedIndex = kSmallBible;

			// Setup expected top and bottom margins for different base character sizes of
			// 9, 10 and 11. The left and right margins are always 36000 millipoints (1/2").
			Dictionary<int, ExpectedMarginType> ExpectedMargins =
				new Dictionary<int, ExpectedMarginType>(3);
			ExpectedMargins.Add(9, new ExpectedMarginType(49000, 38000));
			ExpectedMargins.Add(10, new ExpectedMarginType(51000, 38000));
			ExpectedMargins.Add(11, new ExpectedMarginType(50000, 36000));

			// Check the settings for the margins at different base character sizes.
			for (int baseCharSize = 9; baseCharSize <= 11; baseCharSize++)
			{
				m_dlg.BaseCharSizeControlValue = baseCharSize;

				// Verify that the settings are according to iPub specs.
				ExpectedMarginType expectedMargin = ExpectedMargins[baseCharSize];
				Assert.AreEqual(expectedMargin.TopMargin, m_dlg.MarginTop);
				Assert.AreEqual(expectedMargin.BottomMargin, m_dlg.MarginBottom);
				Assert.AreEqual(36000, m_dlg.MarginLeft);
				Assert.AreEqual(36000, m_dlg.MarginRight);
				Assert.AreEqual(baseCharSize + 2, m_dlg.BaseLineSpacing / 1000);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests whether all possible base character sizes report standard settings in Full
		/// Page Layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FollowsStandardSettings_SmallBibleOneColumn()
		{
			m_div.NumColumns = 1;
			m_pub.PaperHeight = 11 * 72000;
			m_pub.PaperWidth = (int)(8.5 * 72000);
			m_pub.PageHeight = (int)(8.25 * 72000);
			m_pub.PageWidth = (int)(5.25 * 72000);
			m_pub.BaseFontSize = 11000;
			m_pub.BaseLineSpacing = -13000;
			m_pubPageInfo.RemoveAt(0);
			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, true, m_pubPageInfo);
			m_dlg.PageSizeComboIndex = 0;
			m_dlg.AllowNonStandardChoices = false;

			// Should return true for standard pair allowed for one-column
			m_dlg.BaseCharSizeControlValue = 11;
			Assert.IsTrue(m_dlg.FollowsStandardSettings);

			// Should return false for base character sizes too small or too big
			m_dlg.AllowNonStandardChoices = true;
			m_dlg.BaseCharSizeControlValue = 10;
			m_dlg.BaseLineSpacingControlValue = 12;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);
			m_dlg.BaseCharSizeControlValue = 12;
			m_dlg.BaseLineSpacingControlValue = 14;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);

			// Should return false for mismatched font/line spacing values
			m_dlg.BaseCharSizeControlValue = 11;
			m_dlg.BaseLineSpacingControlValue = 12;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);
			m_dlg.BaseLineSpacingControlValue = 14;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests whether all possible base character sizes report standard settings in Full
		/// Page Layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FollowsStandardSettings_LargeBibleTwoColumn()
		{
			m_div.NumColumns = 2;
			m_pub.PaperHeight = 11 * 72000;
			m_pub.PaperWidth = (int)(8.5 * 72000);
			m_pub.PageHeight = (int)(8.7 * 72000);
			m_pub.PageWidth = (int)(5.8 * 72000);
			m_pub.BaseFontSize = 10000;
			m_pub.BaseLineSpacing = -12000;
			m_pubPageInfo.RemoveAt(0);
			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, true, m_pubPageInfo);
			m_dlg.PageSizeComboIndex = 0;
			m_dlg.AllowNonStandardChoices = false;

			// Should return true for standard pairs allowed for two-column
			m_dlg.BaseCharSizeControlValue = 9;
			Assert.IsTrue(m_dlg.FollowsStandardSettings);
			m_dlg.BaseCharSizeControlValue = 10;
			Assert.IsTrue(m_dlg.FollowsStandardSettings);
			m_dlg.BaseCharSizeControlValue = 11;
			Assert.IsTrue(m_dlg.FollowsStandardSettings);

			// Should return false for base character sizes too small or too big
			m_dlg.AllowNonStandardChoices = true;
			m_dlg.BaseCharSizeControlValue = 8;
			m_dlg.BaseLineSpacingControlValue = 10;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);
			m_dlg.BaseCharSizeControlValue = 12;
			m_dlg.BaseLineSpacingControlValue = 14;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);

			// Should return false for mismatched font/line spacing values
			m_dlg.BaseCharSizeControlValue = 10;
			m_dlg.BaseLineSpacingControlValue = 11;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);
			m_dlg.BaseLineSpacingControlValue = 13;
			Assert.IsFalse(m_dlg.FollowsStandardSettings);
		}
		#endregion

		#region Other tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the "Allow non-standard choices" check box on the page setup dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AllowNonStandardChoices_LineSpacing()
		{
			CheckDisposed();

			m_div.NumColumns = 2;
			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);
			m_dlg.IsAllowNonStandardChecked = true;
			m_dlg.PubSizeComboIndex = kLargeBible;
			m_dlg.IsAllowNonStandardChecked = true;

			// Check base character size and line spacing.
			m_dlg.BaseCharSizeControlValue = 8;
			Assert.AreEqual(8000, m_dlg.BaseCharacterSize);
			m_dlg.LineSpacingControlValue = 7;
			Assert.AreEqual(7000, m_dlg.BaseCharacterSize);
			Assert.AreEqual(m_dlg.BaseCharacterSize, m_dlg.BaseLineSpacing, "Line spacing should not be smaller than base character size");
			m_dlg.LineSpacingControlValue = 14;
			Assert.AreEqual(14000, m_dlg.BaseLineSpacing);
			// Line spacing is now twice the font size. If we change the base character size, we
			// expect this proportion to be maintained.
			m_dlg.BaseCharSizeControlValue = 14;
			Assert.AreEqual(14000, m_dlg.BaseCharacterSize);
			Assert.AreEqual(28000, m_dlg.BaseLineSpacing);

			// If we uncheck the "Allow non-standard choices", we should go back to the maximum base
			// character size and line spacing since 16 is above the maximum.
			m_dlg.IsAllowNonStandardChecked = false;
			Assert.AreEqual(11000, m_dlg.BaseCharacterSize);
			Assert.AreEqual(13000, m_dlg.BaseLineSpacing);
		}
		#endregion

		#region Error status tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the error condition when the paper size is bigger than the publication size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ErrorStatus_PubPageTooBig()
		{
			m_div.NumColumns = 2;

			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);
			m_dlg.PubSizeComboIndex = kLargeBible;
			m_dlg.IsAllowNonStandardChecked = true;

			// Set paper size to A5 which is too small to fit a "Large" Bible
			m_dlg.PaperSizeName = m_dlg.GetPaperSize("A5").PaperName;
			Assert.AreEqual(PageSetupErrorType.PubPageTooBig, m_dlg.CallValidateDialogSettings());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the error condition when the horizontal margins are bigger than the
		/// publication size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ErrorStatus_HorizontalMarginsTooBig()
		{
			m_div.NumColumns = 2;

			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);
			m_dlg.PubSizeComboIndex = kFullPage;
			m_dlg.IsAllowNonStandardChecked = true;

			// Set paper size to A5 which is too small to fit a Large Bible
			m_dlg.PaperSizeName = m_dlg.GetPaperSize("A4").PaperName;

			// Set left and right margins too big
			// Left and Right margins are set to half the height plus 1/2"
			m_dlg.MarginLeft = (m_dlg.GetPaperSize("A4").Width * DummyTePageSetupDlg.kCentiInchToMilliPoints) / 2;
			m_dlg.MarginRight = (m_dlg.GetPaperSize("A4").Width * DummyTePageSetupDlg.kCentiInchToMilliPoints) / 2;
			Assert.AreEqual(PageSetupErrorType.HorizontalMarginsTooBig, m_dlg.CallValidateDialogSettings());

			m_dlg.MarginLeft = (m_dlg.GetPaperSize("A4").Width * DummyTePageSetupDlg.kCentiInchToMilliPoints);
			m_dlg.MarginRight = DummyTePageSetupDlg.kCentiInchToMilliPoints;
			Assert.AreEqual(PageSetupErrorType.HorizontalMarginsTooBig, m_dlg.CallValidateDialogSettings());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the error condition when the vertical margins are bigger than the publication
		/// size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ErrorStatus_VerticalMarginsTooBig()
		{
			m_div.NumColumns = 2;

			m_dlg = new DummyTePageSetupDlg(m_wsUser, m_pgl, m_pub, m_div, MsrSysType.Point, false, m_pubPageInfo);

			m_dlg.PubSizeComboIndex = kFullPage;
			m_dlg.IsAllowNonStandardChecked = true;

			// Set paper size to A5 which is too small to fit a Large Bible
			m_dlg.PaperSizeName = m_dlg.GetPaperSize("A4").PaperName;

			// Set left and right margins to legal size
			m_dlg.MarginLeft = 50 * DummyTePageSetupDlg.kCentiInchToMilliPoints; // 1/2 inch
			m_dlg.MarginRight = 50 * DummyTePageSetupDlg.kCentiInchToMilliPoints; // 1/2 inch
			Assert.AreEqual(PageSetupErrorType.NoError, m_dlg.CallValidateDialogSettings());

			// Set top and bottom margins too big
			// Left and Right margins are set to half the height plus 1/2"
			m_dlg.MarginTop = (m_dlg.GetPaperSize("A4").Height * DummyTePageSetupDlg.kCentiInchToMilliPoints) / 2;
			m_dlg.MarginBottom = (m_dlg.GetPaperSize("A4").Height * DummyTePageSetupDlg.kCentiInchToMilliPoints) / 2;
			Assert.AreEqual(PageSetupErrorType.VerticalMarginsTooBig, m_dlg.CallValidateDialogSettings());
		}
		#endregion
	}
}
