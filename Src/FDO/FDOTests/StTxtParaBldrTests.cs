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
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

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
		/// Gets the paragraph properties for this proxy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsTextProps IParaStylePropsProxy.Props
		{
			get
			{
				return StyleUtils.ParaStyleTextProps(m_sStyleName);
			}
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
	public class StTxtParaBldrTests: InMemoryFdoTestBase
	{
		private int m_wsArbitrary;
		private IStText m_text;

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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_text = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();

			m_wsArbitrary = Cache.LanguageEncodings.Item(0).Hvo;
			IText itext = m_inMemoryCache.AddInterlinearTextToLangProj("My Interlinear Text");
			m_inMemoryCache.AddParaToInterlinearTextContents(itext, "Book of Genesis");
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
			CheckDisposed();

			// create an StTxtPara
			StTxtParaBldr bldr = new StTxtParaBldr(Cache);
			IParaStylePropsProxy proxy = new DummyProxy("Whatever");
			bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps =
				StyleUtils.CharStyleTextProps("George's Favorite Char Style", m_wsArbitrary);
			bldr.AppendRun("My run", textProps);

			StTxtPara para = bldr.CreateParagraph(m_text.Hvo, 1);
			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.Props, para.StyleRules);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "My run", "George's Favorite Char Style",
				m_wsArbitrary);
			// Builder should now be cleared
			Assert.AreEqual(0, bldr.Length);
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
			CheckDisposed();

			// Build an StTxtPara
			StTxtParaBldr bldr = new StTxtParaBldr(Cache);
			IParaStylePropsProxy proxy = new DummyProxy("Para Meister");
			bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			bldr.AppendRun("Run 1 ", textProps);
			textProps = StyleUtils.CharStyleTextProps("Italic Run", m_wsArbitrary);
			// verify its length
			Assert.AreEqual(6, bldr.Length);
			// add another run
			bldr.AppendRun("Run 2", textProps);
			StTxtPara para = bldr.CreateParagraph(m_text.Hvo, 1);
			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.Props, para.StyleRules);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Run 1 ", null, m_wsArbitrary);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "Run 2", "Italic Run", m_wsArbitrary);
			// Builder should now be cleared
			Assert.AreEqual(0, bldr.Length);
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
			CheckDisposed();

			// Build First StTxtPara
			StTxtParaBldr bldr = new StTxtParaBldr(Cache);
			IParaStylePropsProxy proxy = new DummyProxy("MyParaStyle");
			bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			bldr.AppendRun("Para 1", textProps);
			StText text = new StText(Cache, m_text.Hvo);
			int iPara = text.ParagraphsOS.Count;
			StTxtPara para1 = bldr.CreateParagraph(text.Hvo, iPara);

			// verify paragraph 1
			AssertEx.RunIsCorrect(para1.Contents.UnderlyingTsString, 0, "Para 1", null,
				m_wsArbitrary);

			// Build Second StTxtPara -- Builder should have been cleared
			textProps = StyleUtils.CharStyleTextProps("BringBrangBrung", m_wsArbitrary);
			bldr.AppendRun("Para 2", textProps);
			StTxtPara para2 = bldr.CreateParagraph(text.Hvo, iPara + 1);

			Assert.AreEqual(text.ParagraphsOS[iPara].Hvo, para1.Hvo);
			Assert.AreEqual(text.ParagraphsOS[iPara + 1].Hvo, para2.Hvo);

			// Re-verify paragraph 1
			AssertEx.RunIsCorrect(para1.Contents.UnderlyingTsString, 0, "Para 1", null,
				m_wsArbitrary);
			// verify paragraph 2
			AssertEx.RunIsCorrect(para2.Contents.UnderlyingTsString, 0, "Para 2",
				"BringBrangBrung", m_wsArbitrary);
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
			CheckDisposed();

			// create an StTxtPara
			StTxtParaBldr bldr = new StTxtParaBldr(Cache);
			IParaStylePropsProxy proxy = new DummyProxy("EmptyPara");
			bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
			bldr.AppendRun(string.Empty, textProps);
			Assert.AreEqual(0, bldr.Length);
			StTxtPara para = bldr.CreateParagraph(m_text.Hvo, 1);
			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.Props, para.StyleRules);
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
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
			CheckDisposed();

			// Build an StTxtPara
			StTxtParaBldr bldr = new StTxtParaBldr(Cache);
			IParaStylePropsProxy proxy = new DummyProxy("Para Meister");
			bldr.ParaStylePropsProxy = proxy;
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.LanguageEncodings.Item(0).Hvo);
			bldr.AppendRun("Run 1", textProps);

			StText text = new StText(Cache, m_text.Hvo);
			int iPara = text.ParagraphsOS.Count;

			StTxtPara para = bldr.CreateParagraph(text.Hvo);

			// verify paragraph's state
			Assert.IsNotNull(para);
			Assert.AreEqual(proxy.Props, para.StyleRules);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Run 1", null,
				m_wsArbitrary);
			Assert.AreEqual(para.Hvo, text.ParagraphsOS[iPara].Hvo);
			// Builder should now be cleared
			Assert.AreEqual(0, bldr.Length);
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
			CheckDisposed();

			// create an StTxtPara
			using (StTxtParaBldr bldr = new StTxtParaBldr(Cache))
			{
				IParaStylePropsProxy proxy = new DummyProxy("Whatever");
				bldr.ParaStylePropsProxy = proxy;
				ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
				bldr.AppendRun("My run ", textProps);
				Assert.AreEqual(7, bldr.Length);
				// trim the space off the end
				bldr.TrimTrailingSpaceInPara();
				Assert.AreEqual(6, bldr.Length);
				StTxtPara para = bldr.CreateParagraph(m_text.Hvo, 1);
				// verify paragraph contents
				AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "My run", null,
					m_wsArbitrary);
			} // Dispose() frees ICU resources.
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
			CheckDisposed();

			// create an StTxtPara
			using (StTxtParaBldr bldr = new StTxtParaBldr(Cache))
			{
				IParaStylePropsProxy proxy = new DummyProxy("Whatever");
				bldr.ParaStylePropsProxy = proxy;
				ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
				bldr.AppendRun("My run  ", textProps);
				Assert.AreEqual(8, bldr.Length);
				// trim one of the spaces off the end
				bldr.TrimTrailingSpaceInPara();
				Assert.AreEqual(7, bldr.Length);
				StTxtPara para = bldr.CreateParagraph(m_text.Hvo, 1);
				// verify paragraph contents
				AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "My run ", null,
					m_wsArbitrary);
			} // Dispose() frees ICU resources.
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
			CheckDisposed();

			// Build an StTxtPara
			using (StTxtParaBldr bldr = new StTxtParaBldr(Cache))
			{
				IParaStylePropsProxy proxy = new DummyProxy("Whatever");
				bldr.ParaStylePropsProxy = proxy;
				ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
				bldr.AppendRun("My run", textProps);
				Assert.AreEqual(6, bldr.Length);
				// Attempt to trim the space off the end -- nothing should happen.
				bldr.TrimTrailingSpaceInPara();
				Assert.AreEqual(6, bldr.Length);
				StTxtPara para = bldr.CreateParagraph(m_text.Hvo, 1);
				// verify paragraph contents
				AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "My run", null,
					m_wsArbitrary);
			} // Dispose() frees ICU resources.
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
			CheckDisposed();

			// create an StTxtPara
			using (StTxtParaBldr bldr = new StTxtParaBldr(Cache))
			{
				IParaStylePropsProxy proxy = new DummyProxy("Whatever");
				bldr.ParaStylePropsProxy = proxy;
				ITsTextProps textProps = StyleUtils.CharStyleTextProps(null, m_wsArbitrary);
				bldr.AppendRun("My run  ", textProps);
				Assert.AreEqual(8, bldr.Length);
				// trim the space off the end
				bldr.TrimTrailingSpaceInPara();
				Assert.AreEqual(7, bldr.Length);
				bldr.TrimTrailingSpaceInPara();
				Assert.AreEqual(6, bldr.Length);
				StTxtPara para = bldr.CreateParagraph(m_text.Hvo, 1);
				// verify paragraph contents
				AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "My run", null,
					m_wsArbitrary);
			} // Dispose() frees ICU resources.
		}
	}
	#endregion
}
