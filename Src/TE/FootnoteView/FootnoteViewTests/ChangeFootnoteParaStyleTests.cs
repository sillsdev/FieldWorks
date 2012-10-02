// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChangeFootnoteParaStyleTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// -----------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for changing the paragraph style for a footnote in the FootnoteView.
	/// </summary>
	/// -----------------------------------------------------------------------------------
	[TestFixture]
	public class ChangeFootnoteParaStyleTests : ScrInMemoryFdoTestBase
	{
		private DummyFootnoteViewForm m_footnoteForm;
		private DummyFootnoteView m_footnoteView;

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
				if (m_footnoteForm != null)
					m_footnoteForm.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			m_footnoteForm = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_scr.CrossRefsCombinedWithFootnotes = true;
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			m_scr.FootnoteMarkerSymbol = "#";
			m_scr.DisplayFootnoteReference = true;

			m_footnoteForm = new DummyFootnoteViewForm();
			m_footnoteForm.DeleteRegistryKey();
			m_footnoteForm.CreateFootnoteView(Cache);

			m_footnoteForm.Show();
			m_footnoteView = m_footnoteForm.FootnoteView;
			m_footnoteView.RootBox.MakeSimpleSel(true, true, false, true);
			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			m_footnoteForm.Close(); // This should also dispose it.
			m_footnoteForm = null;

			base.Exit(); // If it isn't last, we get some C++ error
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			ITsStrFactory strfact = TsStrFactoryClass.Create();

			//Jude
			IScrBook jude = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			m_scrInMemoryCache.AddTitleToMockedBook(jude.Hvo, "Jude");

			// Jude Scripture section
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(jude.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "First section", "Section Head");
			StTxtPara judePara = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(judePara, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(judePara, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(judePara, "This is the first verse", null);

			// Insert footnote into para 1 of Jude
			ITsStrBldr bldr = judePara.Contents.UnderlyingTsString.GetBldr();
			StFootnote foot = ScrFootnote.InsertFootnoteAt(jude, 0, bldr, 10, "#");
			StTxtPara footPara = new StTxtPara();
			foot.ParagraphsOS.Append(footPara);
			footPara.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
			footPara.Contents.UnderlyingTsString = strfact.MakeString("This is text for the footnote.", Cache.DefaultVernWs);
			judePara.Contents.UnderlyingTsString = bldr.GetString();

			section.AdjustReferences();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests applying a different footnote paragraph style. Jira issue is TE-6159.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ApplyParaStyle()
		{
			CheckDisposed();

			m_footnoteView.EditingHelper.ApplyStyle(ScrStyleNames.NormalFootnoteParagraph);
			int tag;
			int hvoSimple;
			bool fGotIt = m_footnoteView.GetSelectedFootnote(out tag, out hvoSimple);
			Assert.IsTrue(fGotIt);
			Assert.AreEqual((int)StText.StTextTags.kflidParagraphs, tag);
			StFootnote footnote = new StFootnote(Cache, hvoSimple);
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
	}
}
