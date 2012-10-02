// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwStyleSheetTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwStyleSheet : FwStyleSheet
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a font face name for a style.
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="fontName"></param>
		/// ------------------------------------------------------------------------------------
		public void SetStyleFont(string styleName, string fontName)
		{
			IStStyle style = FindStyle(styleName);
			ITsPropsBldr ttpBldr = style.Rules.GetBldr();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, fontName);
			style.Rules = ttpBldr.GetTextProps();
			ComputeDerivedStyles();
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FwStyleSheet class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwStyleSheetTests : BaseTest
	{
		private FdoCache m_fdoCache;
		private IScripture m_scr;
		private DummyFwStyleSheet m_styleSheet;
		private ILgWritingSystemFactory m_wsf;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_fdoCache = FdoCache.Create("TestLangProj");
			m_scr = m_fdoCache.LangProject.TranslatedScriptureOA;
			// For these tests we don't need to run InstallLanguage.
			m_wsf = m_fdoCache.LanguageWritingSystemFactoryAccessor;
			m_wsf.BypassInstall = true;

		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				UndoResult ures = 0;
				if (m_fdoCache != null)
				{
					while (m_fdoCache.CanUndo)
						m_fdoCache.Undo(out ures);
					m_fdoCache.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fdoCache = null;
			m_styleSheet = null;
			m_scr = null;
			if (m_wsf != null)
			{
				Marshal.ReleaseComObject(m_wsf);
				m_wsf = null;
			}

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			m_styleSheet = new DummyFwStyleSheet();
			m_styleSheet.Init(m_fdoCache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			UndoResult ures = 0;
			while (m_fdoCache.CanUndo)
			{
				m_fdoCache.Undo(out ures);
				if (ures == UndoResult.kuresFailed  || ures == UndoResult.kuresError)
					Assert.Fail("ures should not be == " + ures.ToString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that adding a style and deleting a style work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDeleteStyle()
		{
			CheckDisposed();

			ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
			ITsTextProps ttpFormattingProps = tsPropsBldr.GetTextProps(); // default properties

			int nStylesOrig = m_styleSheet.CStyles;

			// get an hvo for the new style
			int hvoStyle = m_styleSheet.MakeNewStyle();
			StStyle style = new StStyle(m_styleSheet.Cache, hvoStyle);

			// PutStyle() adds the style to the stylesheet
			m_styleSheet.PutStyle("MyNewStyle", "bla", hvoStyle, 0,
				hvoStyle, 0, false, false, ttpFormattingProps);

			Assert.AreEqual(nStylesOrig + 1, m_styleSheet.CStyles);
			Assert.AreEqual(ttpFormattingProps, m_styleSheet.GetStyleRgch(0, "MyNewStyle"),
				"Should get correct format props for the style added");

			// Make style be based on section head and check context
			IStStyle baseOnStyle = m_scr.FindStyle(ScrStyleNames.SectionHead);
			m_styleSheet.PutStyle("MyNewStyle", "bla", hvoStyle, baseOnStyle.Hvo,
				hvoStyle, 0, false, false, ttpFormattingProps);
			Assert.AreEqual(baseOnStyle.Context, style.Context);

			// Now delete the new style
			m_styleSheet.Delete(hvoStyle);

			// Verfiy the deletion
			Assert.AreEqual(nStylesOrig, m_styleSheet.CStyles);
			Assert.IsNull(m_styleSheet.GetStyleRgch(0, "MyNewStyle"),
				"Should get null because style is not there");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure attempting to delete a built-in style throws an exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void DeleteBuiltInStyle()
		{
			CheckDisposed();

			// get the hvo of the Verse Number style
			int hvoStyle = -1;
			for (int i = 0; i < m_styleSheet.CStyles; i++)
			{
				if (m_styleSheet.get_NthStyleName(i) == "Verse Number")
				{
					hvoStyle = m_styleSheet.get_NthStyle(i);
				}
			}
			Assert.IsTrue(hvoStyle != -1, "Style 'Verse Number' should exist in DB");

			// attempting to delete this built-in style should throw an exception
			m_styleSheet.Delete(hvoStyle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFaceNameFromStyle method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFontFaceNameFromStyle()
		{
			CheckDisposed();

			// Get the default font names
			LgWritingSystem ws = new LgWritingSystem(m_fdoCache, m_fdoCache.DefaultVernWs);
			string defaultSerif = ws.DefaultSerif;
			string defaultSansSerif = ws.DefaultSansSerif;

			// do the tests
			m_styleSheet.SetStyleFont("Section Head", "Helvetica");
			Assert.AreEqual("Helvetica", m_styleSheet.GetFaceNameFromStyle("Section Head",
				m_fdoCache.LangProject.DefaultVernacularWritingSystem, m_wsf));

			m_styleSheet.SetStyleFont("Paragraph", "Symbol");
			Assert.AreEqual("Symbol", m_styleSheet.GetFaceNameFromStyle("Paragraph",
				m_fdoCache.LangProject.DefaultVernacularWritingSystem, m_wsf));

			m_styleSheet.SetStyleFont("Intro Section Head", StStyle.DefaultHeadingFont);
			Assert.AreEqual(defaultSansSerif, m_styleSheet.GetFaceNameFromStyle(
				"Intro Section Head", m_fdoCache.LangProject.DefaultVernacularWritingSystem, m_wsf));

			m_styleSheet.SetStyleFont("Intro Paragraph", StStyle.DefaultFont);
			Assert.AreEqual(defaultSerif, m_styleSheet.GetFaceNameFromStyle("Intro Paragraph",
				m_fdoCache.LangProject.DefaultVernacularWritingSystem, m_wsf));
		}
	}
}
