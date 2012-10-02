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
// File: StTxtParaBldrTests.cs
// Responsibility: FieldWorks Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region DummyProxy class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy Para Style Props Proxy
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyProxy : IParaStylePropsProxy
	{
		/// <summary>Style name</summary>
		protected string m_sStyleName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a simple proxy for a paragraph style
		/// </summary>
		/// <param name="styleName">Paragraph style name</param>
		/// ------------------------------------------------------------------------------------
		public DummyProxy(string styleName)
		{
			m_sStyleName = styleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph style name for this proxy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string IParaStylePropsProxy.StyleId
		{
			get { return m_sStyleName; }
		}
	}
	#endregion

	#region StTxtParaBldrTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ScrBook class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StTxtParaBldrTests: MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private int m_wsArbitrary;
		private IStText m_text;
		private StTxtParaBldr m_bldr;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_bldr = new StTxtParaBldr(Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_wsArbitrary = Cache.DefaultVernWs;
			IText itext = AddInterlinearTextToLangProj("My Interlinear Text");
			AddParaToInterlinearTextContents(itext, "Book of Genesis");
			m_text = itext.ContentsOA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the basic operation of the <see cref="StTxtParaBldr"/> class by creating a
		/// paragraph that only has one run (with a named charcter style).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AppendRunWithNamedCharStyle()
		{
			// create an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("Whatever");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps =
				StyleUtils.CharStyleTextProps("George's Favorite Char Style", m_wsArbitrary);
			m_bldr.AppendRun("My run", textProps);

			IStTxtPara para = m_bldr.CreateParagraph(m_text, 1);
			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.StyleId, para.StyleName);
			AssertEx.RunIsCorrect(para.Contents, 0, "My run", "George's Favorite Char Style",
				m_wsArbitrary);
			// Builder should now be cleared
			Assert.AreEqual(0, m_bldr.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the basic operation of the <see cref="StTxtParaBldr"/> class by creating a
		/// paragraph that only has two runs (one without a named charcter style and one with).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AppendTwoRuns_WithAndWithoutNamedCharStyles()
		{
			// Build an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("Para Meister");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun("Run 1 ", textProps);
			textProps = StyleUtils.CharStyleTextProps("Italic Run", m_wsArbitrary);
			// verify its length
			Assert.AreEqual(6, m_bldr.Length);
			// add another run
			m_bldr.AppendRun("Run 2", textProps);
			IStTxtPara para = m_bldr.CreateParagraph(m_text, 1);
			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.StyleId, para.StyleName);
			AssertEx.RunIsCorrect(para.Contents, 0, "Run 1 ", null, m_wsArbitrary);
			AssertEx.RunIsCorrect(para.Contents, 1, "Run 2", "Italic Run", m_wsArbitrary);
			// Builder should now be cleared
			Assert.AreEqual(0, m_bldr.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the basic operation of the <see cref="StTxtParaBldr"/> class by creating two
		/// paragraphs in succession with the same builder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateTwoParagraphs()
		{
			// Build First IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("MyParaStyle");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun("Para 1", textProps);
			int iPara = m_text.ParagraphsOS.Count;
			IStTxtPara para1 = m_bldr.CreateParagraph(m_text, iPara);

			// verify paragraph 1
			AssertEx.RunIsCorrect(para1.Contents, 0, "Para 1", null,
				m_wsArbitrary);

			// Build Second IStTxtPara -- Builder should have been cleared
			textProps = StyleUtils.CharStyleTextProps("BringBrangBrung", m_wsArbitrary);
			m_bldr.AppendRun("Para 2", textProps);
			IStTxtPara para2 = m_bldr.CreateParagraph(m_text, iPara + 1);

			Assert.AreEqual(m_text.ParagraphsOS[iPara], para1);
			Assert.AreEqual(m_text.ParagraphsOS[iPara + 1], para2);

			// Re-verify paragraph 1
			AssertEx.RunIsCorrect(para1.Contents, 0, "Para 1", null, m_wsArbitrary);
			// verify paragraph 2
			AssertEx.RunIsCorrect(para2.Contents, 0, "Para 2", "BringBrangBrung", m_wsArbitrary);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the basic operation of the <see cref="StTxtParaBldr"/> class by attempting to
		/// create a paragraph whose only run was actually empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateParagraphEmpty()
		{
			// create an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("EmptyPara");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun(string.Empty, textProps);
			Assert.AreEqual(0, m_bldr.Length);
			IStTxtPara para = m_bldr.CreateParagraph(m_text, 1);
			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.StyleId, para.StyleName);
			Assert.AreEqual(1, para.Contents.RunCount);
			Assert.IsNull(para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the basic operation of the <see cref="StTxtParaBldr"/> class by appending a
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AppendParagraph()
		{
			// Build an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("Para Meister");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun("Run 1", textProps);

			int iPara = m_text.ParagraphsOS.Count;
			IStTxtPara para = m_bldr.CreateParagraph(m_text);

			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.StyleId, para.StyleName);
			AssertEx.RunIsCorrect(para.Contents, 0, "Run 1", null,
				m_wsArbitrary);
			Assert.AreEqual(para.Hvo, m_text.ParagraphsOS[iPara].Hvo);
			// Builder should now be cleared
			Assert.AreEqual(0, m_bldr.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="StTxtParaBldr.TrimTrailingSpaceInPara"/> method on a paragraph
		/// that ends with a space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimTrailingSpaceWithSpaceAtEnd()
		{
			// create an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("Whatever");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun("My run ", textProps);
			Assert.AreEqual(7, m_bldr.Length);
			// trim the space off the end
			m_bldr.TrimTrailingSpaceInPara();
			Assert.AreEqual(6, m_bldr.Length);
			IStTxtPara para = m_bldr.CreateParagraph(m_text, 1);
			// verify paragraph contents
			AssertEx.RunIsCorrect(para.Contents, 0, "My run", null,
				m_wsArbitrary);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="StTxtParaBldr.TrimTrailingSpaceInPara"/> method on a paragraph
		/// that ends with two spaces.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimTrailingSpaceWithTwoSpacesAtEnd()
		{
			// create an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("Whatever");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun("My run  ", textProps);
			Assert.AreEqual(8, m_bldr.Length);
			// trim one of the spaces off the end
			m_bldr.TrimTrailingSpaceInPara();
			Assert.AreEqual(7, m_bldr.Length);
			IStTxtPara para = m_bldr.CreateParagraph(m_text, 1);
			// verify paragraph contents
			AssertEx.RunIsCorrect(para.Contents, 0, "My run ", null,
				m_wsArbitrary);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="StTxtParaBldr.TrimTrailingSpaceInPara"/> method on a paragraph that
		/// doesn't end with a space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimTrailingSpaceWithNoSpaceAtEnd()
		{
			// Build an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("Whatever");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun("My run", textProps);
			Assert.AreEqual(6, m_bldr.Length);
			// Attempt to trim the space off the end -- nothing should happen.
			m_bldr.TrimTrailingSpaceInPara();
			Assert.AreEqual(6, m_bldr.Length);
			IStTxtPara para = m_bldr.CreateParagraph(m_text, 1);
			// verify paragraph contents
			AssertEx.RunIsCorrect(para.Contents, 0, "My run", null,
				m_wsArbitrary);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests calling <see cref="StTxtParaBldr.TrimTrailingSpaceInPara"/> twice on a
		/// paragraph that ends with two spaces.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimTrailingSpaceTwiceWithTwoSpacesAtEnd()
		{
			// create an IStTxtPara
			IParaStylePropsProxy proxy = new DummyProxy("Whatever");
			m_bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			m_bldr.AppendRun("My run  ", textProps);
			Assert.AreEqual(8, m_bldr.Length);
			// trim the space off the end
			m_bldr.TrimTrailingSpaceInPara();
			Assert.AreEqual(7, m_bldr.Length);
			m_bldr.TrimTrailingSpaceInPara();
			Assert.AreEqual(6, m_bldr.Length);
			IStTxtPara para = m_bldr.CreateParagraph(m_text, 1);
			// verify paragraph contents
			AssertEx.RunIsCorrect(para.Contents, 0, "My run", null,
				m_wsArbitrary);
		}
	}
	#endregion
}
