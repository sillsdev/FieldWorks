// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportXhtmlTests.cs
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.ExportTests
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ExportXhtml using an in-memory cache
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportXhtmlTests : ScrInMemoryFdoTestBase
	{
		private ExportXhtml m_exporter;
		string m_fileName;
		FilteredScrBooks m_filter;
		IScrBook m_book;
		FwStyleSheet m_stylesheet;

		#region setup,teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_fileName = FileUtils.GetTempFile("tmp");
			FileUtils.Delete(m_fileName);	// exporter pops up dialog if file exists!
			m_stylesheet = new FwStyleSheet();
			m_stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);

			m_book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_book, "Genesis");

			m_filter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(123);

			m_filter.ShowAllBooks();

			m_exporter = new ExportXhtml(m_fileName, Cache, m_filter, ExportWhat.AllBooks, 1, 0, 0,
				string.Empty, m_stylesheet, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// lears member variables, cleans up temp files, shuts down the cache, etc.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_exporter = null;
			m_filter = null;
			if (m_fileName != null)
				FileUtils.Delete(m_fileName);
			m_stylesheet = null;
			m_fileName = null;

			base.TestTearDown();
		}
		#endregion

		#region GetRunString Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetRunString when the given runText has no hard line breaks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRunString_NoHardLB()
		{
			Assert.AreEqual("Text with no line breaks!",
				ReflectionHelper.GetStrResult(m_exporter, "GetRunString",
				"Text with no line breaks!"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetRunString when the given runText begins with a hard line break.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRunString_StartingHardLB()
		{
			Assert.AreEqual("Text with starting line break!",
				ReflectionHelper.GetStrResult(m_exporter, "GetRunString",
				StringUtils.kChHardLB + "Text with starting line break!"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetRunString when the given runText ends with a hard line break.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRunString_EndingHardLB()
		{
			Assert.AreEqual("Text with ending line break!",
				ReflectionHelper.GetStrResult(m_exporter, "GetRunString",
				"Text with ending line break!" + StringUtils.kChHardLB));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetRunString when the given runText contains a hard line break adjacent to a
		/// space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRunString_HardLbWithSpace()
		{
			Assert.AreEqual("Text with line break adjacent to space!",
				ReflectionHelper.GetStrResult(m_exporter, "GetRunString",
				"Text with line" + StringUtils.kChHardLB + " break adjacent to space!"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetRunString when the given runText contains mutliple hard line breaks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRunString_MultipleHardLBs()
		{
			Assert.AreEqual("Text with multiple hard line breaks!",
				ReflectionHelper.GetStrResult(m_exporter, "GetRunString",
				StringUtils.kChHardLB + "Text with multiple" + StringUtils.kChHardLB +
				"hard line breaks!" + StringUtils.kChHardLB));
		}
		#endregion
	}
}
