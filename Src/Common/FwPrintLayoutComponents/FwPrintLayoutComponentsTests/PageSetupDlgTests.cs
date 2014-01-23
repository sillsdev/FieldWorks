// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PageSetupDlgTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;

using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.Common.PrintLayout
{
	public class DummyPageSetupDlg : PageSetupDlg
	{
		#region Constants
		public const int kCentiInchToMilliPoints = 720;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test class that mocks the PageSetupDlg.
		/// </summary>
		/// <param name="pgLayout">The page layout.</param>
		/// <param name="publication">The publication.</param>
		/// <param name="division">The division.</param>
		/// <param name="pubPageInfo">The publication page info.</param>
		/// ------------------------------------------------------------------------------------
		public DummyPageSetupDlg(IPubPageLayout pgLayout, IPublication publication,
			IPubDivision division, List<PubPageInfo> pubPageInfo)
			: base(pgLayout, null, publication, division, null, null, new DummyApp(), pubPageInfo)
		{
			// need to set selected index to something other than 0, otherwise tests fail
			// on machines with default printer set to A4.
			PaperSizeName = "A5";
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
		/// Gets or sets the size of the paper in the paper size combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PaperSizeName
		{
			get
			{
				return cbPaperSize.SelectedIndex != -1 ?
					((PaperSize)(cbPaperSize.Items[cbPaperSize.SelectedIndex])).PaperName : string.Empty;
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
		public int BaseLineSpacingControlValue
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the FollowsStandardSettings property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool FollowsStandardSettings
		{
			get { return base.FollowsStandardSettings; }
		}
		#endregion

		#region DummyApp class
		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification="This dummy class doesn't do anything (and doesn't create any disposable objects) - there is no point in implementing IDisposable")]
		private class DummyApp : IApp
		{
			#region IApp Members

			public Form ActiveMainWindow
			{
				get { throw new NotImplementedException(); }
			}
			public SIL.FieldWorks.Common.Framework.PictureHolder PictureHolder { get; private set; }

			public string ApplicationName
			{
				get { throw new NotImplementedException(); }
			}

			public MsrSysType MeasurementSystem
			{
				get
				{
					return MsrSysType.Cm;
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public void RefreshAllViews()
			{
				throw new NotImplementedException();
			}

			public string ResourceString(string stid)
			{
				throw new NotImplementedException();
			}

			public Microsoft.Win32.RegistryKey SettingsKey
			{
				get { throw new NotImplementedException(); }
			}

			public Guid SyncGuid
			{
				get { throw new NotImplementedException(); }
			}

			public bool Synchronize(SyncMsg sync)
			{
				throw new NotImplementedException();
			}

			public void EnableMainWindows(bool fEnable)
			{
				throw new NotImplementedException();
			}

			public void RemoveFindReplaceDialog()
			{
				throw new NotImplementedException();
			}

			public bool ShowFindReplaceDialog(bool fReplace, RootSite rootsite)
			{
				throw new NotImplementedException();
			}

			public void HandleOutgoingLink(FwAppArgs link)
			{
				throw new NotImplementedException();
			}

			public string SupportEmailAddress
			{
				get { throw new NotImplementedException(); }
			}

			public void RestartSpellChecking()
			{
				throw new NotImplementedException();
			}

			#endregion
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for PageSetupDlgTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PageSetupDlgTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Constants
		// Define indices for the page layout size combo box.
		private const int kFullPage = 0;
		#endregion

		#region Member variables
		private DummyPageSetupDlg m_dlg;
		private IPublication m_pub;
		private IPubDivision m_div;
		private IPubPageLayout m_pgl;
		private List<PubPageInfo> m_pubPageInfo;
		#endregion

		#region Setup and teardown
		/// <summary>
		///
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();


			m_pub = CreatePublication(0, 0, false, "TestPub", 0,
				BindingSide.Left, 0);

			m_div = AddDivisionToPub(m_pub, false, false,
				DivisionStartOption.Continuous);
			m_pgl = Cache.ServiceLocator.GetInstance<IPubPageLayoutFactory>().Create();
			m_div.PageLayoutOA = m_pgl;

			m_pubPageInfo = new List<PubPageInfo>();
			m_pubPageInfo.Add(new PubPageInfo("Full Page", 0, 0));
			m_pubPageInfo.Add(new PubPageInfo("5.25 x 8.25", (int)(8.25 * 72000), (int)(5.25 * 72000)));
			m_pubPageInfo.Add(new PubPageInfo("5.8 x 8.7", (int)(8.7 * 72000), (int)(5.8 * 72000)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the division to the publication.
		/// </summary>
		/// <param name="pub">The publication where the division will be added.</param>
		/// <param name="fDifferentFirstHF">if set to <c>true</c> publication has a different
		/// first header/footer].</param>
		/// <param name="fDifferentEvenHF">if set to <c>true</c> publication has a different even
		/// header/footer.</param>
		/// <param name="startAt">Enumeration of options for where the content of the division
		/// begins</param>
		/// <returns>the new division</returns>
		/// ------------------------------------------------------------------------------------
		public IPubDivision AddDivisionToPub(IPublication pub, bool fDifferentFirstHF,
			bool fDifferentEvenHF, DivisionStartOption startAt)
		{
			IPubDivision div = Cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
			pub.DivisionsOS.Add(div);
			div.DifferentFirstHF = fDifferentFirstHF;
			div.DifferentEvenHF = fDifferentEvenHF;
			div.StartAt = startAt;
			return div;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a publication and adds it to a collection of Publications on the
		/// CmMajorObject AnnotationDefs.
		/// </summary>
		/// <param name="pageHeight">Height of the page.</param>
		/// <param name="pageWidth">Width of the page.</param>
		/// <param name="fIsLandscape">if set to <c>true</c> the publication is landscape.</param>
		/// <param name="name">The name of the publication.</param>
		/// <param name="gutterMargin">The gutter margin.</param>
		/// <param name="bindingSide">The side on which the publication will be bound (i.e., the
		/// gutter location).</param>
		/// <param name="footnoteSepWidth">Width of the footnote seperator.</param>
		/// <returns>the new publication</returns>
		/// <remarks>Adds the publication to AnnotationDefs because we need a
		/// CmMajorObject where we can attach the Publication and Scripture is not visible here.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private IPublication CreatePublication(int pageHeight, int pageWidth, bool fIsLandscape,
			string name, int gutterMargin, BindingSide bindingSide, int footnoteSepWidth)
		{
			IPublication pub = Cache.ServiceLocator.GetInstance<IPublicationFactory>().Create();
			Cache.LanguageProject.AnnotationDefsOA.PublicationsOC.Add(pub);
			pub.PageHeight = pageHeight;
			pub.PageWidth = pageWidth;
			pub.IsLandscape = fIsLandscape;
			pub.Name = name;
			pub.GutterMargin = gutterMargin;
			pub.BindingEdge = bindingSide;
			pub.FootnoteSepWidth = footnoteSepWidth;

			return pub;
		}

		public override void TestTearDown()
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

			base.TestTearDown();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of paper sizes for testing the Page Setup dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, PaperSize> TestPaperSizes
		{
			get
			{
				Dictionary<string, PaperSize> paperSizes = new Dictionary<string, PaperSize>();
				paperSizes.Add(ResourceHelper.GetResourceString("kstidPaperSizeA4"),
					new PaperSize(ResourceHelper.GetResourceString("kstidPaperSizeA4"), 827, 1169));
				paperSizes.Add(ResourceHelper.GetResourceString("kstidPaperSizeLetter"),
					new PaperSize(ResourceHelper.GetResourceString("kstidPaperSizeLetter"), 850, 1100));
				paperSizes.Add(ResourceHelper.GetResourceString("kstidPaperSizeA5"),
					new PaperSize(ResourceHelper.GetResourceString("kstidPaperSizeA5"), 583, 827));
				paperSizes.Add(ResourceHelper.GetResourceString("kstidPaperSizeLegalStd"),
					new PaperSize(ResourceHelper.GetResourceString("kstidPaperSizeLegalStd"), 850, 1400));
				paperSizes.Add(ResourceHelper.GetResourceString("kstidPaperSizeLegalPhil"),
					new PaperSize(ResourceHelper.GetResourceString("kstidPaperSizeLegalPhil"), 850, 1300));
				paperSizes.Add(ResourceHelper.GetResourceString("kstidPaperSizeCustom"),
					new PaperSize(ResourceHelper.GetResourceString("kstidPaperSizeCustom"), 0, 0));

				return paperSizes;
			}
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Creates a dictionary containing the paper sizes for Scripture.
//		/// </summary>
//		/// <returns>a dictionary of paper sizes for Scripture</returns>
//		/// ------------------------------------------------------------------------------------
//		private Dictionary<string, PaperSize> ScripturePaperSizes
//		{
//			get
//			{
//				Dictionary<string, PaperSize> scripturePaperSizes =
//					new Dictionary<string, PaperSize>(TestPaperSizes);
//				scripturePaperSizes.Add("5.25 x 8.25in", new PaperSize("5.25 x 8.25in", 525, 825));
//				scripturePaperSizes.Add("5.8 x 8.7in", new PaperSize("5.8 x 8.7in", 580, 870));
//				return scripturePaperSizes;
//			}
//		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests AdjustPaperSize to confirm that it sets the correct paper size based on the
		/// set height and width of the paper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustPaperSize()
		{
			m_div.NumColumns = 2;
			m_pub.PageWidth = 50000;
			m_pub.PageHeight = 80000;
			m_dlg = new DummyPageSetupDlg(m_pgl, m_pub, m_div, m_pubPageInfo);
			m_dlg.PageSizeComboIndex = 0;
			m_dlg.IsAllowNonStandardChecked = true;

			// Iterate through the paper sizes to make sure the appropriate name is selected in
			// the paper size combo box. We have to convert from 100ths of inches to millipoints.
			foreach (string paperSizeName in TestPaperSizes.Keys)
			{
				if (paperSizeName != ResourceHelper.GetResourceString("kstidPaperSizeCustom"))
				{
					PaperSize paperSize = TestPaperSizes[paperSizeName];
					m_dlg.PaperSizeHeight = paperSize.Height * DummyPageSetupDlg.kCentiInchToMilliPoints;
					m_dlg.PaperSizeWidth = paperSize.Width * DummyPageSetupDlg.kCentiInchToMilliPoints;

					Assert.AreEqual(paperSize.PaperName, m_dlg.PaperSizeName);
				}
			}

			// Now set the dimensions to something that is not in the standard list of paper sizes.
			// We expect that the selected paper size would be "Custom".
			m_dlg.PaperSizeHeight = 250 * DummyPageSetupDlg.kCentiInchToMilliPoints;
			m_dlg.PaperSizeWidth = 250 * DummyPageSetupDlg.kCentiInchToMilliPoints;
			Assert.AreEqual(ResourceHelper.GetResourceString("kstidPaperSizeCustom"),
				m_dlg.PaperSizeName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that selecting a name of the paper size also sets the corresponding height
		/// and width.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectPaperSizeName()
		{
			m_div.NumColumns = 2;
			m_dlg = new DummyPageSetupDlg(m_pgl, m_pub, m_div, m_pubPageInfo);
			m_dlg.PageSizeComboIndex = 0;
			m_dlg.IsAllowNonStandardChecked = true;

			int lastPaperHeight = 0;
			int lastPaperWidth = 0;
			// Iterate through the paper sizes to make sure the appropriate height and width
			// are displayed on the dialog. Values in PaperSize are stored in hundredths of
			// inches so we have to convert to millipoints.
			foreach (string paperSizeName in TestPaperSizes.Keys)
			{
				if (paperSizeName != ResourceHelper.GetResourceString("kstidPaperSizeCustom"))
				{
					PaperSize paperSize = TestPaperSizes[paperSizeName];
					m_dlg.PaperSizeName = paperSize.PaperName;

					Assert.AreEqual(paperSize.Height * DummyPageSetupDlg.kCentiInchToMilliPoints,
						m_dlg.PaperSizeHeight, "Height of " + paperSizeName);
					Assert.AreEqual(paperSize.Width * DummyPageSetupDlg.kCentiInchToMilliPoints,
						m_dlg.PaperSizeWidth, "Width of " + paperSizeName);
					lastPaperHeight = m_dlg.PaperSizeHeight;
					lastPaperWidth = m_dlg.PaperSizeWidth;
				}
			}

			// Now test with "Custom" paper size. We expect that the paper size will be the same
			// as the last selected paper size.
			m_dlg.PaperSizeName = ResourceHelper.GetResourceString("kstidPaperSizeCustom");
			Assert.AreEqual(lastPaperHeight, m_dlg.PaperSizeHeight, "Custom height");
			Assert.AreEqual(lastPaperWidth, m_dlg.PaperSizeWidth, "Custom width");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests selecting a non-standard paper size dimension after a standard paper size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectPaperSizeName_ChangeToNonStandardDimension()
		{
			m_div.NumColumns = 2;
			m_dlg = new DummyPageSetupDlg(m_pgl, m_pub, m_div, m_pubPageInfo);
			m_dlg.PageSizeComboIndex = 0;
			m_dlg.IsAllowNonStandardChecked = true;

			// Set paper size to A4 and confirm that the dialog has A4 settings.
			m_dlg.PaperSizeName = TestPaperSizes["A4"].PaperName;
			Assert.AreEqual(TestPaperSizes["A4"].Height * DummyPageSetupDlg.kCentiInchToMilliPoints,
				m_dlg.PaperSizeHeight);
			Assert.AreEqual(TestPaperSizes["A4"].Width * DummyPageSetupDlg.kCentiInchToMilliPoints,
				m_dlg.PaperSizeWidth);

			// Now change the dimensions of the paper size. The page size combo should be set
			// to "Custom".
			m_dlg.PaperSizeHeight *= 2;
			Assert.AreEqual(ResourceHelper.GetResourceString("kstidPaperSizeCustom"),
				m_dlg.PaperSizeName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests whether all possible base character sizes report standard settings in Full
		/// Page Layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FollowsStandardSettings()
		{
			m_div.NumColumns = 1;
			m_pub.PageHeight = m_pub.PaperHeight = 11 * 72000;
			m_pub.PageWidth = m_pub.PaperWidth = (int)(8.5 * 72000);
			m_pub.BaseFontSize = 10000;
			m_pub.BaseLineSpacing = -12000;
			m_dlg = new DummyPageSetupDlg(m_pgl, m_pub, m_div, m_pubPageInfo);
			m_dlg.AllowNonStandardChoices = false;

			for (int baseCharSize = 4; baseCharSize < 100; baseCharSize++)
			{
				m_dlg.BaseCharSizeControlValue = baseCharSize;
				Assert.IsTrue(m_dlg.FollowsStandardSettings);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test handling the paper that has no specified size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleZeroPageSize()
		{
			m_div.NumColumns = 2;
			m_pub.PageHeight = m_pub.PageWidth = 0;
			m_dlg = new DummyPageSetupDlg(m_pgl, m_pub, m_div, m_pubPageInfo);
			m_dlg.PageSizeComboIndex = 0;

			// TODO: confirm that the dialog returns the default paper size of the printer
			// when the page size is set to 0.
		}
		#endregion
	}
}
