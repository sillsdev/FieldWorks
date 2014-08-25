// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PubTestsNoDb.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.TE.TePrintLayoutComponents
{
	#region DummyScripturePublication class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyScripturePublication : ScripturePublication
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyScripturePublication"/> class.
		/// </summary>
		/// <param name="cache">The database connection</param>
		/// <param name="stylesheet">The stylesheet to be used for this publication (can be
		/// different from the one used for drafting, but should probably have all the same
		/// styles)</param>
		/// <param name="filterInstance">number used to make filters unique per main window</param>
		/// <param name="publication">The publication to get the information from (or
		/// null to keep the defaults)</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="printDateTime">Date/Time of the printing</param>
		/// ------------------------------------------------------------------------------------
		protected DummyScripturePublication(FdoCache cache, FwStyleSheet stylesheet, int filterInstance,
			IPublication publication, TeViewType viewType, DateTime printDateTime) :
			base(stylesheet, filterInstance, publication, viewType, printDateTime, null, null,
			cache.DefaultVernWs)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to test creation of a publication view.
		/// </summary>
		/// <param name="pub">The publication.</param>
		/// <param name="viewType">Type of the Translation Editor view.</param>
		/// <returns>
		/// a Scripture publication for the specified view type.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal static DummyScripturePublication Create(IPublication pub,
			TeViewType viewType, FwStyleSheet stylesheet)
		{
			if (viewType == TeViewType.BackTranslationParallelPrint)
			{
				Debug.Assert(false, "Not yet implemented.");
				return null;
			}

			DummyScripturePublication pubControl = new DummyScripturePublication(pub.Cache,
				stylesheet, 567, pub, viewType, DateTime.Now);
			pubControl.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			pubControl.Dock = DockStyle.Fill;
			pubControl.Name = TeEditingHelper.ViewTypeString(viewType);
			pubControl.Visible = false;
			return pubControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style sheet for this Scripture Publication. Exposes the stylesheet of
		/// the Scripture Publication for testing.
		/// </summary>
		/// <value>The style sheet.</value>
		/// ------------------------------------------------------------------------------------
		internal FwStyleSheet StyleSheet
		{
			get { return m_stylesheet; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't apply the book filter or create divisions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ApplyBookFilterAndCreateDivisions()
		{
			// no op
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests methods for scaling text (setting base font size and line spacing).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PubTestsNoDb : PrintLayoutTestBase
	{
		#region Data members
		private IPublication m_pub;
		private FwStyleSheet m_realStylesheet;
		#endregion

		#region Setup and teardown methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_pub = m_scr.PublicationsOC.ToArray()[0];
			Assert.IsNotNull(m_pub);
			m_realStylesheet = new FwStyleSheet();
			m_realStylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);
			SetFontSizeLineHeightAndSpaceBeforeAfter(m_realStylesheet, ScrStyleNames.Normal,
				null, 10000, -12000, -1, -1);
			SetFontSizeLineHeightAndSpaceBeforeAfter(m_realStylesheet, ScrStyleNames.NormalParagraph,
				ScrStyleNames.Normal, -1, -1, -1, -1);
			SetFontSizeLineHeightAndSpaceBeforeAfter(m_realStylesheet, ScrStyleNames.NormalFootnoteParagraph,
				ScrStyleNames.NormalParagraph, 8000, 10000, -1, -1);
			SetFontSizeLineHeightAndSpaceBeforeAfter(m_realStylesheet, ScrStyleNames.IntroParagraph,
				ScrStyleNames.NormalParagraph, 9000, -11000, -1, -1);
			SetFontSizeLineHeightAndSpaceBeforeAfter(m_realStylesheet, ScrStyleNames.IntroSectionHead,
				ScrStyleNames.SectionHead, 8000, -10000, -1, -1);
			SetFontSizeLineHeightAndSpaceBeforeAfter(m_realStylesheet, ScrStyleNames.SectionHead,
				ScrStyleNames.NormalParagraph, 9000, -1, 8000, 4000);
			SetFontSizeLineHeightAndSpaceBeforeAfter(m_realStylesheet, ScrStyleNames.MainBookTitle,
				ScrStyleNames.SectionHead, 20000, -24000, 36000, 12000);

			m_pub.DivisionsOS.Add(Cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create());
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a ScripturePublication for scaling font size below the minimum.
		/// (TE-5796)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SmallTextScaling()
		{
			// Set up a ScripturePublication of 4 on 6 pts (base character size on line spacing).
			m_pub.BaseFontSize = 4000;
			m_pub.BaseLineSpacing = -6000;
			InMemoryStyleSheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.PrintLayout, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet as InMemoryStyleSheet;
				Assert.IsNotNull(stylesheet);

				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will have specified settings for the publication.
				Assert.AreEqual(4000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-6000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles will be overridden for font size at 40% their original size.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(4000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(4000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(4000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect that these styles will be overridden for line spacing at 50% their original size.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(5000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-5500, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-5000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-12000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will be overridden for space before/after at 50% their original size.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(18000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(6000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(4000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(2000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a ScripturePublication for slightly decreased character sizes and
		/// line spacing. (TE-5606)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NormalTextScaling()
		{
			// Set up a ScripturePublication of 9 on 11 pts (base character size on line spacing).
			m_pub.BaseFontSize = 9000;
			m_pub.BaseLineSpacing = -11000;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.PrintLayout, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will have specified settings for the publication.
				Assert.AreEqual(9000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-11000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles will be overridden for font size at 90% their original size.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(7000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(18000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect these styles to be overridden for line spacing at 91.667% their original size.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(9167, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-10083, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-9167, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-22000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will be overridden for space before/after at 91.667% their original size.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(33000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(11000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(7333, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(3667, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a ScripturePublication for "normal" character sizes and line
		/// spacing. (TE-5606) In this test, we simulate a Print Layout view with default
		/// (full page) page size and unspecified paper size. This should cause TE to use the
		/// normal default (10/12). (TE-5856)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoTextScaling()
		{
			// Set up a ScripturePublication with no scaling specified.
			m_pub.Name = "Scripture Draft";
			m_pub.BaseFontSize = 0;
			m_pub.BaseLineSpacing = 0;
			m_pub.PageHeight = 0;
			m_pub.PageWidth = 0;
			m_pub.PaperHeight = 0;
			m_pub.PaperWidth = 0;
			m_pub.DivisionsOS[0].NumColumns = 2;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.PrintLayout, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will be unchanged.
				Assert.AreEqual(10000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-12000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles that have specific font sizes will have their original values.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(9000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(9000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(20000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect that these styles with specific line heights will have their original values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-11000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-24000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will have their original space before/after at values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(36000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(12000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(4000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a one-column Trial Publication when publication does not have
		/// explicit overrides for font size and line spacing. In this test, we simulate a
		/// Trial Publication view with explicit page size set to match one of the IPUB standard
		/// page sizes. This should cause the normal default (10/12) to be overridden and lay
		/// out using the 1-column default of 11/13. (TE-5856)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultTextScalingForOneColumnTrialPub()
		{
			// Set up a ScripturePublication with no scaling specified, but which looks like
			// a Trial Publication.
			m_pub.Name = "Trial Publication";
			m_pub.BaseFontSize = 0;
			m_pub.BaseLineSpacing = 0;
			m_pub.PageHeight = (int)(8.7 * 72000);
			m_pub.PageWidth = (int)(5.8 * 72000);
			m_pub.DivisionsOS[0].NumColumns = 1;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.TrialPublication, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will be bumped up to the official IPUB sizes for
				// a one-column publication.
				Assert.AreEqual(11000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-13000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles that have specific font sizes will have their original values.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(9000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(22000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect that these styles with specific line heights will have their original values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(10833, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-11917, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-10833, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-26000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will have their original space before/after at values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(39000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(13000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(8667, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(4333, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a one-column Print Layout when publication does not have
		/// explicit overrides for font size and line spacing. In this test, we simulate a
		/// Print Layout view with default (full page) page size but an explicit paper size
		/// set to match one of the IPUB standard page sizes. This should cause the normal
		/// default (10/12) to be overridden and lay out using the 1-column default of 11/13.
		/// (TE-5856)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultTextScaling_OneColFullPgStdBibleSize()
		{
			m_pub.Name = "Scripture Draft";
			m_pub.BaseFontSize = 0;
			m_pub.BaseLineSpacing = 0;
			m_pub.PaperHeight = (int)(8.7 * 72000);
			m_pub.PaperWidth = (int)(5.8 * 72000);
			m_pub.PageHeight = 0;
			m_pub.PageWidth = 0;
			m_pub.DivisionsOS[0].NumColumns = 1;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.PrintLayout, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will be bumped up to the official IPUB sizes for
				// a one-column publication.
				Assert.AreEqual(11000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-13000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles that have specific font sizes will have their original values.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(9000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(22000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect that these styles with specific line heights will have their original values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(10833, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-11917, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-10833, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-26000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will have their original space before/after at values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(39000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(13000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(8667, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(4333, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a one-column Print Layout when publication does not have
		/// explicit overrides for font size and line spacing. In this test, we simulate a
		/// Print Layout view with default (full page) page size but an explicit paper size
		/// of Letter (8.5 x 11 inches). This should cause the normal default (10/12) to be
		/// used. (TE-5856)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultTextScaling_OneColFullPgLetterPaper()
		{
			m_pub.Name = "Scripture Draft";
			m_pub.BaseFontSize = 0;
			m_pub.BaseLineSpacing = 0;
			m_pub.PaperHeight = (int)(11 * 72000);
			m_pub.PaperWidth = (int)(8.5 * 72000);
			m_pub.PageHeight = 0;
			m_pub.PageWidth = 0;
			m_pub.DivisionsOS[0].NumColumns = 1;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.PrintLayout, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will be unchanged.
				Assert.AreEqual(10000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-12000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles that have specific font sizes will have their original values.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(9000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(9000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(20000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect that these styles with specific line heights will have their original values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-11000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-24000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will have their original space before/after at values.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(36000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(12000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(4000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a ScripturePublication for a "normal" character sizes and line
		/// spacing. (TE-5606)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LargeTextScaling()
		{
			// Set up a ScripturePublication of 14 on 16 pts (base character size on line spacing).
			// (Original size is for 10 on 12).
			m_pub.BaseFontSize = 14000;
			m_pub.BaseLineSpacing = -16000;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.PrintLayout, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will have specified settings for the publication.
				Assert.AreEqual(14000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-16000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles will be overridden for font size at 140% their original size.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(13000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(11000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(13000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(28000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect that these styles will be overridden for font size at 133% their original size
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(13333, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-14667, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-13333, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-32000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will be overridden for space before/after at 133% their original size.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(48000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(16000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(10667, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(5333, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a ScripturePublication for scaling font size above the maximum.
		/// (TE-5796)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HugeTextScaling()
		{
			// Set up a ScripturePublication of 4 on 6 pts (base character size on line spacing).
			m_pub.BaseFontSize = 120000;
			m_pub.BaseLineSpacing = -120020;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Scripture | TeViewType.PrintLayout, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = scrPub.StyleSheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will have specified settings for the publication.
				Assert.AreEqual(120000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			}

			// We expect that these styles will be overridden for font size at 1200% their original size
			// (except where they would go above the maximum size).
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(108000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(96000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(108000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(240000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect these styles to be overridden for line spacing at 1000.1667% their original size.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(100017, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-110018, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-100017, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-240040, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will be overridden for space before/after at 1000.1667% their original size.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(360060, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(120020, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(80013, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(40007, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests line-spacing adjustment for the Correction Printout view. (TE-5722)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CorrectionPrintoutLineSpacing()
		{
			// Set up a ScripturePublication with no scaling specified.
			m_pub.BaseFontSize = 12000;
			m_pub.BaseLineSpacing = -36000;
			IVwStylesheet stylesheet;
			int nVar;
			using (DummyScripturePublication scrPub = DummyScripturePublication.Create(m_pub,
				TeViewType.Correction, m_realStylesheet))
			{
				stylesheet = scrPub.StyleSheet;
				ITsTextProps ttpNormalPara = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalParagraph);
				// We expect that the normal style will have specified settings for the publication.
				Assert.AreEqual(12000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
				Assert.AreEqual(-36000,
					ttpNormalPara.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			}

			// We expect that these styles will be overridden for font size at 120% their original size.
			ITsTextProps styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(11000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(10000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(11000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(24000, styleProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out nVar));

			// We expect these styles to be overridden for line spacing to exactly 36 pts.
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(-36000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroParagraph);
			Assert.AreEqual(-36000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(-36000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(-36000, styleProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out nVar));

			// We expect that these styles will have unchanged space before/after values.
			// REVIEW: Should we zero out any space before or after for the correction printout view to
			// achieve consistent line spacing?
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(36000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.MainBookTitle);
			Assert.AreEqual(12000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(8000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out nVar));
			styleProps = stylesheet.GetStyleRgch(0, ScrStyleNames.SectionHead);
			Assert.AreEqual(4000, styleProps.GetIntPropValues((int)FwTextPropType.ktptSpaceAfter, out nVar));
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the font size line height and space before after.
		/// </summary>
		/// <param name="stylesheet">The real stylesheet.</param>
		/// <param name="styleName">Name of the style to modify.</param>
		/// <param name="basedOnStyle">The name of the based-on style.</param>
		/// <param name="fontSize">Size of the font.</param>
		/// <param name="lineHeight">Height of the line.</param>
		/// <param name="spaceBefore">The space before.</param>
		/// <param name="spaceAfter">The space after.</param>
		/// ------------------------------------------------------------------------------------
		private static void SetFontSizeLineHeightAndSpaceBeforeAfter(FwStyleSheet stylesheet,
			string styleName, string basedOnStyle, int fontSize, int lineHeight, int spaceBefore,
			int spaceAfter)
		{
			IStStyle style = stylesheet.FindStyle(styleName);
			TsPropsBldr tpb = (TsPropsBldr)style.Rules.GetBldr();
			if (fontSize != -1)
			{
				tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, fontSize);
			}
			if (lineHeight != -1)
			{
				tpb.SetIntPropValues((int)FwTextPropType.ktptLineHeight,
					(int)FwTextPropVar.ktpvMilliPoint, lineHeight);
			}
			if (spaceBefore != -1)
			{
				tpb.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore,
					(int)FwTextPropVar.ktpvMilliPoint, spaceBefore);
			}
			if (spaceAfter != -1)
			{
				tpb.SetIntPropValues((int)FwTextPropType.ktptSpaceAfter,
					(int)FwTextPropVar.ktpvMilliPoint, spaceAfter);
			}
			int hvoBasedOn = 0;
			if (!string.IsNullOrEmpty(basedOnStyle))
			{
				IStStyle basedOn = stylesheet.FindStyle(basedOnStyle);
				if (basedOn != null)
					hvoBasedOn = basedOn.Hvo;
			}
			stylesheet.PutStyle(styleName, string.Empty, style.Hvo, hvoBasedOn, 0,
				(int)StyleType.kstParagraph, true, false, tpb.GetTextProps());
		}
		#endregion
	}
}
