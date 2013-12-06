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
// File: PubTestsNoDb.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region Class ReallyStupidPubCtrl
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ReallyStupidPubCtrl : DummyPublication
	{
		#region Constructor
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReallyStupidPubCtrl"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ReallyStupidPubCtrl(IPublication pub, FwStyleSheet stylesheet,
			DivisionLayoutMgr div, DateTime printDateTime, bool fApplyStyleOverrides)
			: base(pub, stylesheet, div, printDateTime, fApplyStyleOverrides)
		{
		}
		#endregion

		#region Exposed data members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the PublicationControl.m_stylesheet data member
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStyleSheet PrintLayoutStylesheet
		{
			get
			{
				CheckDisposed();
				return m_stylesheet;
			}
		}
		#endregion

		#region Overridden properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font size (in millipoints) to be used when the publication doesn't specify
		/// it explicitly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int DefaultFontSize
		{
			get
			{
				return 14000;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the line height (in millipoints) to be used when the publication doesn't
		/// specify it explicitly. (Value is negative for "exact" line spacing.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int DefaultLineHeight
		{
			get
			{
				return -16000;
			}
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests methods for Publication Control class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PubTestsNoDb : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IPublication m_pub;
		#endregion

		#region Setup and teardown methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_pub = Cache.ServiceLocator.GetInstance<IPublicationFactory>().Create();
			Cache.LangProject.LexDbOA.PublicationsOC.Add(m_pub);
		}
		#endregion

		#region Gutter and Font Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the page size is adjusted for gutter on the left side.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GutterNotIncludedInPrintLayout_LeftBound()
		{
			m_pub.PageWidth = 0;
			m_pub.PageHeight = 0;
			m_pub.PaperHeight = 2 * MiscUtils.kdzmpInch;
			// 2 inches
			m_pub.PaperWidth = 8 * MiscUtils.kdzmpInch;
			// 8 inches
			m_pub.GutterMargin = 3 * MiscUtils.kdzmpInch;
			// 3 inch gutter
			m_pub.BindingEdge = BindingSide.Left;

			using (DummyDivision divLayoutMgr = new DummyDivision(new DummyPrintConfigurer(Cache, null), 1))
			{
				using (DummyPublication pubControl = new ReallyStupidPubCtrl(m_pub, null, divLayoutMgr,
					DateTime.Now, false))
				{
					Assert.AreEqual(5 * MiscUtils.kdzmpInch, pubControl.PageWidth);
					Assert.AreEqual(2 * MiscUtils.kdzmpInch, pubControl.PageHeight);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the page size is adjusted for gutter on the top side.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GutterNotIncludedInPrintLayout_TopBound()
		{
			m_pub.PageWidth = 0;
			m_pub.PageHeight = 0;
			m_pub.PaperHeight = 6 * MiscUtils.kdzmpInch;
			// 2 inches
			m_pub.PaperWidth = 8 * MiscUtils.kdzmpInch;
			// 8 inches
			m_pub.GutterMargin = 3 * MiscUtils.kdzmpInch;
			// 3 inch gutter
			m_pub.BindingEdge = BindingSide.Top;

			using (DummyDivision divLayoutMgr = new DummyDivision(new DummyPrintConfigurer(Cache, null), 1))
			{
				using (ReallyStupidPubCtrl pubControl = new ReallyStupidPubCtrl(m_pub, null, divLayoutMgr,
						DateTime.Now, false))
				{
					Assert.AreEqual(8 * MiscUtils.kdzmpInch, pubControl.PageWidth);
					Assert.AreEqual(3 * MiscUtils.kdzmpInch, pubControl.PageHeight);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the default font size and line height are used to override the stylesheet
		/// if the IPublication doesn't contain explicit values (TE-5856).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultFontAndLineHeightUsed()
		{
			m_pub.BaseFontSize = 0;
			m_pub.BaseLineSpacing = 0;
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo,
				ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);

			using (DummyDivision divLayoutMgr = new DummyDivision(new DummyPrintConfigurer(Cache, null), 1))
			{
				using (ReallyStupidPubCtrl pubControl = new ReallyStupidPubCtrl(m_pub, stylesheet,
						divLayoutMgr, DateTime.Now, true))
				{
					ITsTextProps normalProps = pubControl.PrintLayoutStylesheet.GetStyleRgch(-1, "Normal");
					Assert.IsNotNull(normalProps);
					int var;
					Assert.AreEqual(pubControl.DefaultFontSize,
						normalProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out var));
					Assert.AreEqual((int)FwTextPropVar.ktpvMilliPoint, var);
					Assert.AreEqual(pubControl.DefaultLineHeight,
						normalProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out var));
					Assert.AreEqual((int)FwTextPropVar.ktpvMilliPoint, var);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the default font size and line height are not used to override the
		/// stylesheet if the IPublication contains explicit values (TE-5856).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PublicationFontAndLineHeightUsed()
		{
			m_pub.BaseFontSize = 9000;
			m_pub.BaseLineSpacing = -11000;
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo,
				ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);

			using (DummyDivision divLayoutMgr = new DummyDivision(new DummyPrintConfigurer(Cache, null), 1))
			{
				using (ReallyStupidPubCtrl pubControl = new ReallyStupidPubCtrl(m_pub, stylesheet,
				divLayoutMgr, DateTime.Now, true))
				{
					ITsTextProps normalProps = pubControl.PrintLayoutStylesheet.GetStyleRgch(-1, "Normal");
					Assert.IsNotNull(normalProps);
					int var;
					Assert.AreEqual(9000,
						normalProps.GetIntPropValues((int)FwTextPropType.ktptFontSize, out var));
					Assert.AreEqual((int)FwTextPropVar.ktpvMilliPoint, var);
					Assert.AreEqual(-11000,
						normalProps.GetIntPropValues((int)FwTextPropType.ktptLineHeight, out var));
					Assert.AreEqual((int)FwTextPropVar.ktpvMilliPoint, var);
				}
			}
		}
		#endregion
	}
}
