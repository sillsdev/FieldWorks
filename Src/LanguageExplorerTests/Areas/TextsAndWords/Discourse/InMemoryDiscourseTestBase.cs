// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using SIL.LCModel;

namespace LanguageExplorerTests.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Base class for several sets of tests for the Constituent chart, which share the need
	/// to create an in-memory LCM cache, a text, at least one paragraph, and make wordforms
	/// in that paragraph.
	/// </summary>
	public class InMemoryDiscourseTestBase : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		internal IStText m_stText;
		internal IStTxtPara m_firstPara;
		internal DiscourseTestHelper m_helper;

		#region Overrides of LcmTestBase

		/// <summary>
		/// Create minimal test data required for every test.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_helper = new DiscourseTestHelper(Cache);
			m_firstPara = m_helper.FirstPara;
			m_stText = m_firstPara.Owner as IStText;
		}

		public override void TestTearDown()
		{
			try
			{
				m_helper = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}
		#endregion

		/// <summary>
		/// Make and parse a new unique paragraph and append it to the current text.
		/// </summary>
		internal IStTxtPara MakeParagraph()
		{
			return m_helper.MakeParagraph();
		}

		/// <summary>
		/// Each test should call this before calling the SUT if the SUT has its own UOW.
		/// </summary>
		protected void EndSetupTask()
		{
			if (m_actionHandler.CurrentDepth > 0)
			{
				m_actionHandler.EndUndoTask();
			}
		}

		protected static ChartLocation MakeLocObj(IConstChartRow row, int icol)
		{
			return new ChartLocation(row, icol);
		}
	}
}
