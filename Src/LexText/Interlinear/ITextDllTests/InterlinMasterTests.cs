// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.XWorks;
using XCore;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class InterlinMasterTests : MemoryOnlyBackendProviderTestBase, IDisposable
	{
		private IWritingSystem m_wsDefaultVern, m_wsOtherVern, m_wsEn;
		private IStText m_sttNoExplicitWs, m_sttEmptyButWithWs;
		private IStText m_stText;

		private FwXApp m_application;
		private FwXWindow m_window;
		private Mediator m_mediator;

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_application != null)
					m_application.Dispose();
				if (m_window != null)
					m_window.Dispose();
				if (m_mediator != null)
					m_mediator.Dispose();
			}
		}

		~InterlinMasterTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion disposal

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DoSetupFixture);
		}

		/// <summary>non-undoable task because setting up an StText must be done in a Unit of Work</summary>
		private void DoSetupFixture()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;

			// set up default vernacular ws.
			m_wsDefaultVern = Cache.ServiceLocator.WritingSystemManager.Get("fr");
			m_wsOtherVern = Cache.ServiceLocator.WritingSystemManager.Get("es");
			m_wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en");
			Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(m_wsOtherVern);
			Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(m_wsOtherVern);
			Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(m_wsDefaultVern);
			Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Insert(0, m_wsDefaultVern);

			// set up an StText with an empty paragraph with default Contents (empty English TsString)
			m_sttNoExplicitWs = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			Cache.ServiceLocator.GetInstance<ITextFactory>().Create().ContentsOA = m_sttNoExplicitWs;
			m_sttNoExplicitWs.AddNewTextPara(null);
			Assert.AreEqual(m_wsEn.Handle, m_sttNoExplicitWs.MainWritingSystem, "Our code counts on English being the defualt WS for very empty texts");

			// set up an StText with an empty paragraph with an empty TsString in a non-default vernacular
			m_sttEmptyButWithWs = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			Cache.ServiceLocator.GetInstance<ITextFactory>().Create().ContentsOA = m_sttEmptyButWithWs;
			m_sttEmptyButWithWs.AddNewTextPara(null);
			((IStTxtPara)m_sttEmptyButWithWs.ParagraphsOS[0]).Contents = TsStringUtils.MakeTss(string.Empty, m_wsOtherVern.Handle);
		}

		[Test]
		public void ShowRoot_ReplacesGlobalDefaultWsWithDefaultVernInEmptyText()
		{
			using(var interlinMaster = new TestInterlinMaster(m_mediator, m_sttNoExplicitWs))
			{
				interlinMaster.TestShowRecord(); // SUT
			}
			Assert.That(m_sttNoExplicitWs.IsEmpty, "Our text should still be empty");
			Assert.AreEqual(m_wsDefaultVern.Handle, m_sttNoExplicitWs.MainWritingSystem, "The WS for the text should now be the default vernacular");
		}

		[Test]
		public void ShowRoot_MaintainsSelectedWsInEmptyText()
		{
			using(var interlinMaster = new TestInterlinMaster(m_mediator, m_sttEmptyButWithWs))
			{
				interlinMaster.TestShowRecord(); // SUT
			}
			Assert.That(m_sttEmptyButWithWs.IsEmpty, "Our text should still be empty");
			Assert.AreEqual(m_wsOtherVern.Handle, m_sttEmptyButWithWs.MainWritingSystem, "The WS for the text should still be the other vernacular");
		}

		#region Test Classes
		private class TestInterlinMaster : InterlinMaster
		{
			public TestInterlinMaster(Mediator mediator, IStText stText)
			{
				m_mediator = mediator;
				if(ExistingClerk != null)
				{
					System.Diagnostics.Debug.WriteLine("****** Disposing a {0} whose current StText's WS is {1}; replacing with {2}. ******",
						ExistingClerk.GetType().Name,
						Cache.ServiceLocator.WritingSystemManager.Get(((IStText)ExistingClerk.CurrentObject).MainWritingSystem).Id,
						Cache.ServiceLocator.WritingSystemManager.Get(stText.MainWritingSystem).Id);
					ExistingClerk.Dispose();
				}
				Clerk = new TestClerk(mediator, stText);
			}

			protected internal override IStText RootStText
			{
				get { return ((TestClerk)Clerk).m_stText; }
			}

			public void TestShowRecord()
			{
				ShowRecord();
			}
		}

		private class TestClerk : InterlinearTextsRecordClerk
		{
			internal readonly IStText m_stText;

			public TestClerk(Mediator mediator, IStText stText)
			{
				m_mediator = mediator;
				m_stText = stText;
			}

			protected override void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, String.Format(
					"****** Missing Dispose call for a {0} whose current StText's WS is {1}. ******",
					GetType().Name, Cache.ServiceLocator.WritingSystemManager.Get(m_stText.MainWritingSystem).Id));
				base.Dispose(disposing);
			}

			public override int CurrentObjectHvo { get { return m_stText.Hvo; } }
		}
		#endregion Test Classes
	}
}
