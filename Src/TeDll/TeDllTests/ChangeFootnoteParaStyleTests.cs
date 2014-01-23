// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ChangeFootnoteParaStyleTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System.Windows.Forms;

using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using SIL.Utils;

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

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
		public override void TestTearDown()
		{
			m_footnoteView = null;
			// m_footnoteForm made it, and disposes it.
			m_footnoteForm.Close();
			// This should also dispose it.
			m_footnoteForm = null;

			base.TestTearDown(); // If it isn't last, we get some C++ error
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
			IScrBook jude = AddBookToMockedScripture(65, "Jude");
			AddTitleToMockedBook(jude, "Jude");

			// Jude Scripture section
			IScrSection section = AddSectionToMockedBook(jude);
			AddSectionHeadParaToSection(section, "First section", "Section Head");
			IStTxtPara judePara = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(judePara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(judePara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(judePara, "This is the first verse", null);

			// Insert footnote into para 1 of Jude
			ITsStrBldr bldr = judePara.Contents.GetBldr();
			IScrFootnote foot = jude.InsertFootnoteAt(0, bldr, 10);
			IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				foot, ScrStyleNames.NormalFootnoteParagraph);
			footPara.Contents = strfact.MakeString("This is text for the footnote.", Cache.DefaultVernWs);
			judePara.Contents = bldr.GetString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests applying a different footnote paragraph style. Jira issue is TE-6159.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ApplyParaStyle()
		{
			m_footnoteView.EditingHelper.ApplyStyle(ScrStyleNames.NormalFootnoteParagraph);
			int tag;
			int hvoSimple;
			bool fGotIt = m_footnoteView.GetSelectedFootnote(out tag, out hvoSimple);
			Assert.IsTrue(fGotIt);
			Assert.AreEqual(StTextTags.kflidParagraphs, tag);
			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IStFootnoteRepository>().GetObject(hvoSimple);
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
	}
}
