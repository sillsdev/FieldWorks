// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Runtime.InteropServices;
using FieldWorks.TestUtilities;
using NUnit.Framework;
using RootSite.TestUtilities;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorerTests.Impls
{
	/// <summary />
	public class FwFindReplaceDlgBaseTests : ScrInMemoryLcmTestBase
	{
		#region Data members
		/// <summary></summary>
		protected const string m_kTitleText = "Blah, blah, blah!";
		/// <summary></summary>
		internal DummyFwFindReplaceDlg m_dlg;
		/// <summary></summary>
		protected DummyBasicView m_vwRootsite;
		/// <summary></summary>
		protected IVwPattern m_vwPattern;
		/// <summary></summary>
		protected IVwStylesheet m_Stylesheet;
		/// <summary></summary>
		protected IStText m_text;
		/// <summary></summary>
		protected IScrBook m_genesis;
		#endregion

		#region setup & teardown
		/// <summary>
		/// Initializes the ScrReference for testing.
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			CoreWritingSystemDefinition ws;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-fonipa-x-etic", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ur", out ws);
		}

		/// <summary>
		/// Create the dialog
		/// </summary>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			var wsManager = Cache.ServiceLocator.WritingSystemManager;
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			m_text = AddTitleToMockedBook(m_genesis, m_kTitleText, wsManager.GetWsFromStr("en-fonipa-x-etic"));
			m_vwPattern = VwPatternClass.Create();
			m_Stylesheet = new TestFwStylesheet();
			m_vwRootsite = new DummyBasicView
			{
				StyleSheet = m_Stylesheet,
				Cache = Cache,
				MyDisplayType = DisplayType.kMappedPara // Needed for some footnote tests
			};
			m_vwRootsite.MakeRoot(m_text.Hvo, ScrBookTags.kflidTitle, 3);
			m_dlg = new DummyFwFindReplaceDlg();
			var wsContainer = Cache.ServiceLocator.WritingSystems;
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("en-fonipa-x-etic"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("fr"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("de"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("es"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("ur"));
		}

		/// <summary>
		/// Dispose of the dialog
		/// </summary>
		[TearDown]
		public override void TestTearDown()
		{
			if (m_dlg != null)
			{
				if (m_dlg.IsHandleCreated)
				{
					m_dlg.Close();
				}
				m_dlg.Dispose();
				m_dlg = null;
			}
			if (m_vwRootsite != null)
			{
				m_vwRootsite.Dispose();
				m_vwRootsite = null;
			}
			if (m_vwPattern != null)
			{
				if (Marshal.IsComObject(m_vwPattern))
				{
					Marshal.ReleaseComObject(m_vwPattern);
				}
				m_vwPattern = null;
			}
			if (m_Stylesheet != null)
			{
				if (Marshal.IsComObject(m_Stylesheet))
				{
					Marshal.ReleaseComObject(m_Stylesheet);
				}
				m_Stylesheet = null;
			}
			m_text = null;
			m_genesis = null;
			base.TestTearDown();
		}
		#endregion
	}
}