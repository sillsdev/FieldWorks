// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CustomListDlgTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.CoreImpl;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the CustomList dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CustomListDlgTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		private ISubscriber m_subscriber;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			PubSubSystemFactory.CreatePubSubSystem(out m_publisher, out m_subscriber);
			m_propertyTable = PropertyTableFactory.CreatePropertyTable(m_publisher);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
			}
			m_propertyTable = null;
			m_publisher = null;
			m_subscriber = null;

			base.TestTearDown();
		}

		#region Helper Methods

		void SetUserWs(string wsStr)
		{
			var wsMgr = Cache.ServiceLocator.GetInstance<IWritingSystemManager>();
			IWritingSystem userWs;
			if (!wsMgr.TryGetOrSet(wsStr, out userWs))
			{
				var wsFact = Cache.WritingSystemFactory;
				wsFact.get_Engine(wsStr); // this installs it if it wasn't found.
				if (!wsMgr.TryGetOrSet(wsStr, out userWs))
					Assert.Fail("Totally Unknown Writing System: " + wsStr);
			}
			wsMgr.UserWritingSystem = userWs;
		}

		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the Name of the custom list.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetGetListName()
		{
			// Setup
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				dlg.SetTestCache(Cache);
				var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
				Assert.True(wsFr > 0, "Test failed because French ws is not installed.");
				dlg.InitializeMultiString();
				// setup up multistring controls
				var nameTss = Cache.TsStrFactory.MakeString("Gens", wsFr);

				// SUT (actually tests both Set and Get)
				dlg.SetListNameForWs(nameTss, wsFr);

				Assert.AreEqual("Gens", dlg.GetListNameForWs(wsFr).Text, "Setting the custom list Name failed.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the Description of the custom list.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetGetListDescription()
		{
			// Setup
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				dlg.SetTestCache(Cache);
				var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
				Assert.True(wsFr > 0, "Test failed because French ws is not installed.");
				var wsSp = Cache.WritingSystemFactory.GetWsFromStr("es");
				Assert.True(wsSp > 0, "Test failed because Spanish ws is not installed.");
				dlg.InitializeMultiString();
				// setup up multistring controls
				var nameTssFr = Cache.TsStrFactory.MakeString("Une description en français!", wsFr);
				var nameTssSp = Cache.TsStrFactory.MakeString("Un descripción en español?", wsSp);

				// SUT (actually tests both Set and Get)
				dlg.SetDescriptionForWs(nameTssFr, wsFr);
				dlg.SetDescriptionForWs(nameTssSp, wsSp);

				Assert.AreEqual("Une description en français!", dlg.GetDescriptionForWs(wsFr).Text,
					"Setting the custom list Description in French failed.");
				Assert.AreEqual("Un descripción en español?", dlg.GetDescriptionForWs(wsSp).Text,
				"Setting the custom list Description in Spanish failed.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the routine that checks for duplicate names in Possibility lists repo.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsListNameDuplicated_French_Yes()
		{
			// Setup
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				dlg.SetTestCache(Cache);
				SetUserWs("fr"); // user ws needs to be French for this test
				var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
				Assert.True(wsFr > 0, "Test failed because French ws is not installed.");
				dlg.InitializeMultiString();
				// setup up multistring controls
				var nameTss = Cache.TsStrFactory.MakeString("Gens-test", wsFr);
				var newList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(
					"testPeople", Cache.DefaultUserWs);
				newList.Name.set_String(wsFr, nameTss);
				// set French alternative in new list to "Gens-test"
				dlg.SetListNameForWs(nameTss, wsFr);
				// set dialog list name French alternative to the same thing

				// SUT
				bool fdup = dlg.IsNameDuplicated;
				Assert.IsTrue(fdup, "Couldn't detect list with duplicate French name?!");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the routine that checks for duplicate names in Possibility lists repo.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsListNameDuplicated_French_No()
		{
			// Setup
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				dlg.SetTestCache(Cache);
				SetUserWs("fr"); // user ws needs to be French for this test
				var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
				Assert.True(wsFr > 0, "Test failed because French ws is not installed.");
				dlg.InitializeMultiString();
				// setup up multistring controls
				var nameTss = Cache.TsStrFactory.MakeString("Gens-test", wsFr);
				// set dialog list name French alternative to "Gens-test", but don't create a list
				// with that name.
				dlg.SetListNameForWs(nameTss, wsFr);

				// SUT
				bool fdup = dlg.IsNameDuplicated;

				Assert.IsFalse(fdup, "Detected a list with duplicate French name?!");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Title of the configure list dialog.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetDialogTitle_Add()
		{
			// AddList subclass of CustomListDlg
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				// Dialog Title should default to "New List"
				Assert.AreEqual("New List", dlg.Text,
					"Dialog default title for AddList dialog is wrong.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Title of the configure list dialog.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetDialogTitle_Configure()
		{
			// Configure subclass of CustomListDlg
			using (var dlg = new ConfigureListDlg(null, null, Cache.LangProject.LocationsOA))
			{
				// Dialog Title should default to "Configure List"
				Assert.AreEqual("Configure List", dlg.Text,
					"Dialog default title for ConfigureList dialog is wrong.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the default checkbox values.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetCheckBoxes_defaults()
		{
			using (var dlg = new AddListDlg(m_propertyTable, m_publisher))
			{
				// SUT; Get default checkbox values
				var hier = dlg.SupportsHierarchy;
				var sort = dlg.SortByName;
				var dupl = dlg.AllowDuplicate;

				// Verify
				Assert.IsFalse(hier, "'Support hierarchy' default value should be false.");
				Assert.IsFalse(sort, "'Sort items by name' default value should be false.");
				Assert.IsTrue(dupl, "'Allow duplicate items' default value should be true.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting non-default checkbox values.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetCheckBoxesToOtherValues()
		{
			using (var dlg = new ConfigureListDlg(m_propertyTable, m_publisher, Cache.LangProject.LocationsOA))
			{
				// SUT; Set non-default checkbox values
				dlg.SupportsHierarchy = true;
				dlg.SortByName = true;
				dlg.AllowDuplicate = false;

				// Verify
				Assert.IsTrue(dlg.SupportsHierarchy, "'Support hierarchy' value should be set to true.");
				Assert.IsTrue(dlg.SortByName, "'Sort items by name' value should be set to true.");
				Assert.IsFalse(dlg.AllowDuplicate, "'Allow duplicate items' value should be set to false.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the default writing system possibilities.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetDefaultWsComboEntries()
		{
			// SUT
			using (var dlg = new AddListDlg(m_propertyTable, m_publisher))
			{
				// Verify
				Assert.AreEqual(WritingSystemServices.kwsAnals, dlg.SelectedWs,
					"Wrong default writing system in combo box.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the writing system value.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetWsComboSelectedItem()
		{
			// SUT
			using (var dlg = new ConfigureListDlg(m_propertyTable, m_publisher, Cache.LangProject.LocationsOA))
			{
				dlg.SelectedWs = WritingSystemServices.kwsVerns;

				// Verify
				Assert.AreEqual(WritingSystemServices.kwsVerns, dlg.SelectedWs,
					"Wrong writing system in combo box.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the installed UI languages.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestGetUiWssAndInstall()
		{
			var testStrings = new List<string> { "en", "es", "fr" };
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				dlg.SetTestCache(Cache);
				// must set the test cache because this method needs one
				SetUserWs("es");

				// SUT
				var wss = dlg.GetUiWssAndInstall(testStrings);

				// Verify
				Assert.AreEqual(2, wss.Count,
					"Wrong number of wss found.");
				var fenglish = wss.Where(ws => ws.IcuLocale == "en").Any();
				var fspanish = wss.Where(ws => ws.IcuLocale == "es").Any();
				var ffrench = wss.Where(ws => ws.IcuLocale == "fr").Any();
				Assert.IsTrue(fenglish, "English not found.");
				Assert.IsTrue(fspanish, "Spanish not found.");
				Assert.IsFalse(ffrench, "French should not be found.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the installed UI languages, included one regional variety.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestGetUiWssAndInstall_dialect()
		{
			var testStrings = new List<string> { "en", "es-MX", "fr" };
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				dlg.SetTestCache(Cache);
				// must set the test cache because this method needs one
				SetUserWs("es-MX");

				// SUT
				var wss = dlg.GetUiWssAndInstall(testStrings);

				// Verify
				Assert.AreEqual(2, wss.Count,
					"Wrong number of wss found.");
				var fenglish = wss.Where(ws => ws.IcuLocale == "en").Any();
				// Interesting! We input the string "es-MX" and get out the string "es_MX"!
				var fspanish = wss.Where(ws => ws.IcuLocale == "es_MX").Any();
				var ffrench = wss.Where(ws => ws.IcuLocale == "fr").Any();
				Assert.IsTrue(fenglish, "English not found.");
				Assert.IsTrue(fspanish, "Spanish(Mexican) not found.");
				Assert.IsFalse(ffrench, "French should not be found.");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the installed UI languages, when it's English.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestGetUiWssAndInstall_OnlyEnglish()
		{
			var testStrings = new List<string> { "en", "es", "fr" };
			using (var dlg = new TestCustomListDlg(m_propertyTable, m_publisher))
			{
				dlg.SetTestCache(Cache);
				// must set the test cache because this method needs one
				SetUserWs("en");

				// SUT
				var wss = dlg.GetUiWssAndInstall(testStrings);

				// Verify
				Assert.AreEqual(1, wss.Count,
					"Wrong number of wss found.");
				var fenglish = wss.Where(ws => ws.IcuLocale == "en").Any();
				var fspanish = wss.Where(ws => ws.IcuLocale == "es").Any();
				var ffrench = wss.Where(ws => ws.IcuLocale == "fr").Any();
				Assert.IsTrue(fenglish, "English not found.");
				Assert.IsFalse(fspanish, "Spanish should not found.");
				Assert.IsFalse(ffrench, "French should not be found.");
			}
		}
	}

	/// <summary>
	/// CustomListDlg for testing
	/// </summary>
	public class TestCustomListDlg : CustomListDlg
	{
		public TestCustomListDlg(IPropertyTable propertyTable, IPublisher publisher)
			: base(propertyTable, publisher)
		{
		}

		#region Protected methods made Internal

		internal List<IWritingSystem> GetUiWssAndInstall(IEnumerable<string> uiLanguages)
		{
			return GetUiWritingSystemAndEnglish();
		}

		internal void SetTestCache(FdoCache cache)
		{
			Cache = cache;
		}

		internal void SetListNameForWs(ITsString name, int ws)
		{
			SetListName(name, ws);
		}

		internal ITsString GetListNameForWs(int ws)
		{
			return GetListName(ws);
		}

		internal void InitializeMultiString()
		{
			InitializeMultiStringControls();
		}

		internal void SetDescriptionForWs(ITsString name, int ws)
		{
			SetDescription(name, ws);
		}

		internal ITsString GetDescriptionForWs(int ws)
		{
			return GetDescription(ws);
		}

		internal bool IsNameDuplicated
		{
			get { return IsListNameDuplicated; }
		}

		#endregion

	}
}
