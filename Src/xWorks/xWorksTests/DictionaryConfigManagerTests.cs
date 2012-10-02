// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DictionaryConfigManagerTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the logic contained in the DictionaryConfigManager, by using a stub to represent
	/// the actual dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DictionaryConfigManagerTests
	{
		private DictionaryConfigViewerStub m_testView;
		private DictionaryConfigTestPresenter m_testPresenter;

		[SetUp]
		public void CreateTestData()
		{
			// Constructor for DictionaryConfigViewerStub also creates DictionaryConfigPresenterStub
			// subclass of DictionaryConfigManager that is more easily tested.
			m_testView = new DictionaryConfigViewerStub();
			m_testPresenter = m_testView.TestPresenter;
		}

		[TearDown]
		public void DeleteTestData()
		{
			m_testView = null;
			m_testPresenter = null;
		}

		#region Utility methods

		private DictConfigItem GetKeyFromValue(string dispName)
		{
			return m_testPresenter.StubConfigDict.Where(
				keyValPair => dispName == keyValPair.Value.DispName).Select(
				keyValPair => keyValPair.Value).FirstOrDefault();
		}

		/// <summary>
		/// Loads the configurations into the presenter and tests that the right
		/// number have been loaded.
		/// </summary>
		/// <param name="configs"></param>
		private void LoadConfigListAndTest(List<Tuple<string, string, bool>> configs)
		{
			var citems = configs.Count + 2; // Plus two for Root and Stem originals
			m_testPresenter.LoadConfigList(configs);
			Assert.AreEqual(citems, m_testPresenter.StubConfigDict.Count,
							"Wrong number of items loaded into config dictionary.");
		}

		/// <summary>
		/// Loads the configurations into the presenter, tests that they've been
		/// loaded and sets the original view to 'initialView' and tests that the
		/// current view is also updated.
		/// </summary>
		/// <param name="configs"></param>
		/// <param name="initialView"></param>
		/// <returns>Total number of configuration items stored.</returns>
		private int LoadConfigListAndTest(List<Tuple<string, string, bool>> configs,
										   string initialView)
		{
			LoadConfigListAndTest(configs);
			m_testPresenter.StubOrigView = initialView;
			Assert.AreEqual(initialView, m_testPresenter.StubCurView,
							"Setting original view failed to set current view.");
			return m_testPresenter.StubConfigDict.Count;
		}

		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the LoadInternalDictionary method and the UpdateCurView method.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void LoadInternalDictionary_UpdateCurView()
		{
			// Setup
			const string stest1 = "C1";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>(stest1, "Name1", true),
				new Tuple<string, string, bool>(stest2, "Name2", false),
				new Tuple<string, string, bool>("C3", "Name3", false)};

			// SUT
			LoadConfigListAndTest(configs, stest2);
			m_testPresenter.UpdateCurSelection(stest1);

			// Verify
			Assert.AreEqual(stest1, m_testPresenter.StubCurView, "UpdateCurView didn't work.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method TryMarkForDeletion in the case where the item is not protected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MarkForDeletion_Normal()
		{
			// Setup
			const string stest1 = "C1";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>(stest1, "Name1", true),
				new Tuple<string, string, bool>(stest2, "Name2", false),
				new Tuple<string, string, bool>("C3", "Name3", false)};
			LoadConfigListAndTest(configs, stest1);

			// SUT
			var result = m_testPresenter.TryMarkForDeletion(stest2);

			// Verify
			Assert.IsTrue(result, "Mark for delete has wrong return value.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.IsTrue(item.UserMarkedDelete, "Mark for delete failed.");
			Assert.AreEqual("C1", m_testPresenter.StubCurView,
				"Delete shouldn't affect current view in this case.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method TryMarkForDeletion in the case where the item is protected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MarkForDeletion_ProtectedItem()
		{
			// Setup
			const string stest1 = "C1";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>(stest1, "Name1", true),
				new Tuple<string, string, bool>(stest2, "Name2", true),
				new Tuple<string, string, bool>("C3", "Name3", false)};
			LoadConfigListAndTest(configs, stest1);

			// SUT
			var result = m_testPresenter.TryMarkForDeletion(stest2);

			// Verify
			Assert.IsFalse(result, "Mark for delete has wrong return value.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.IsFalse(item.UserMarkedDelete, "Mark for delete succeeded wrongly.");
			Assert.AreEqual(stest1, m_testPresenter.StubCurView,
				"Delete shouldn't affect current view in this case.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method TryMarkForDeletion in the case where the item is also the current
		/// view (but not the original one).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MarkForDeletion_DeletingCurrentViewButNotOriginal()
		{
			// Setup
			const string stest1 = "C1";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>(stest1, "Name1", true),
				new Tuple<string, string, bool>(stest2, "Name2", false),
				new Tuple<string, string, bool>("C3", "Name3", false)};
			LoadConfigListAndTest(configs, stest1);
			m_testPresenter.StubCurView = stest2; // update current view

			// SUT
			var result = m_testPresenter.TryMarkForDeletion(stest2);

			// Verify
			Assert.IsTrue(result, "Mark for delete has wrong return value.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.IsTrue(item.UserMarkedDelete, "Mark for delete failed.");
			Assert.AreEqual(stest1, m_testPresenter.StubCurView,
				"Delete should have changed current view back to the original.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method TryMarkForDeletion in the case where the item is also the current
		/// and original view and its not protected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MarkForDeletion_DeletingUnprotectedOriginal()
		{
			// Setup
			const string stest1 = "C1";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>(stest1, "Name1", true),
				new Tuple<string, string, bool>(stest2, "Name2", false),
				new Tuple<string, string, bool>("C3", "Name3", false)};
			LoadConfigListAndTest(configs, stest2);

			// SUT
			var result = m_testPresenter.TryMarkForDeletion(stest2);

			// Verify
			Assert.IsTrue(result, "Mark for delete has wrong return value.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.IsTrue(item.UserMarkedDelete, "Mark for delete failed.");
			Assert.AreEqual(stest2, m_testPresenter.StubOrigView,
				"Delete should not have changed original view.");
			Assert.AreEqual(stest1, m_testPresenter.StubCurView,
				"Delete should have changed current view to the first protected view.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method TryMarkForDeletion in the case where the item is also the current
		/// and original view and its not protected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MarkForDeletion_DeletingUnprotectedOriginal_TestAlphabeticalOrder()
		{
			// Setup
			const string stest1 = "C3";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(stest2, "Name2", false),
				new Tuple<string, string, bool>(stest1, "AlphaName3", true)};
			LoadConfigListAndTest(configs, stest2);

			// SUT
			var result = m_testPresenter.TryMarkForDeletion(stest2);

			// Verify
			Assert.IsTrue(result, "Mark for delete has wrong return value.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.IsTrue(item.UserMarkedDelete, "Mark for delete failed.");
			Assert.AreEqual(stest2, m_testPresenter.StubOrigView,
				"Delete should not have changed original view.");
			Assert.AreEqual(stest1, m_testPresenter.StubCurView,
				"Delete should have changed current view to the first protected view.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SetListViewItems.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CopyConfigItem()
		{
			// Setup
			const string stest1 = "Name2";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(stest2, stest1, false),
				new Tuple<string, string, bool>("C3", "AlphaName3", true)};
			var cnt = LoadConfigListAndTest(configs, stest2);

			// SUT
			m_testPresenter.CopyConfigItem(stest2);

			// Verify
			Assert.AreEqual(cnt + 1, m_testPresenter.StubConfigDict.Count,
				"Should have added a new item.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.IsFalse(item.IsNew, "Old item should not be marked as New.");
			var configItem = GetKeyFromValue("Copy of " + stest1);
			Assert.IsNotNull(configItem, "Didn't find an item with the right Name.");
			Assert.IsTrue(configItem.IsNew, "New item should be marked as New.");
			Assert.AreEqual(stest2, configItem.CopyOf,
				"New item should be marked as a 'Copy of' old item.");
			Assert.AreEqual(stest2, m_testPresenter.StubOrigView,
				"Copy should not have changed original view.");
			Assert.AreEqual(configItem.UniqueCode, m_testPresenter.StubCurView,
				"Copy should have changed current view to the new view.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method CopyConfigItem in the case where we try to copy a copy.
		/// We may need to do that eventually, but for now it makes things lots easier to not
		/// allow it.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CopyOfCopyProhibited()
		{
			// Setup
			const string stest1 = "Name2";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(stest2, stest1, false),
				new Tuple<string, string, bool>("C3", "AlphaName3", true)};
			var cnt = LoadConfigListAndTest(configs, stest2);

			// SUT1
			m_testPresenter.CopyConfigItem(stest2);

			// Verify1
			Assert.AreEqual(cnt + 1, m_testPresenter.StubConfigDict.Count,
				"Should have added a new item.");
			var configItem = GetKeyFromValue("Copy of " + stest1);
			Assert.IsNotNull(configItem, "Didn't find an item with the right Name.");

			// SUT2
			m_testPresenter.CopyConfigItem(configItem.UniqueCode);
			Assert.AreEqual(cnt + 1, m_testPresenter.StubConfigDict.Count,
				"Should not have copied the copy.");
			Assert.AreEqual(configItem.UniqueCode, m_testPresenter.StubCurView,
				"Copy should have changed current view to the new view.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method RenameConfigItem.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RenameConfigItem()
		{
			// Setup
			const string stest1 = "Name2";
			const string stest2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(stest2, stest1, false),
				new Tuple<string, string, bool>("C3", "AlphaName3", true)};
			var cnt = LoadConfigListAndTest(configs, stest2);
			const string newName = "A Glorious New Name!";

			// SUT1
			m_testPresenter.RenameConfigItem(stest2, newName);

			// Verify1
			Assert.AreEqual(cnt, m_testPresenter.StubConfigDict.Count,
				"Should have the same number of items.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.AreEqual(newName, item.DispName,
				"Should have renamed config item.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method RenameConfigItem in the case where that item is a protected view.
		/// (Renaming should not be allowed.)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Ignore("This is now checked before the edit is allowed! Nevermind!")]
		public void RenameConfigItem_Protected()
		{
			// Setup
			const string sname2 = "Name2";
			const string sid2 = "C2";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(sid2, sname2, true),
				new Tuple<string, string, bool>("C3", "AlphaName3", false)};
			var cnt = LoadConfigListAndTest(configs, sid2);
			const string newName = "A Glorious New Name!";

			// SUT1
			m_testPresenter.RenameConfigItem(sid2, newName);

			// Verify1
			Assert.AreEqual(cnt, m_testPresenter.StubConfigDict.Count,
				"Should have the same number of items.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(sid2, out item);
			Assert.AreEqual(sname2, item.DispName,
				"Should not have renamed protected config item.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method RenameConfigItem where the name is changed to the same as
		/// an existing name.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RenameConfigItem_NameInUse()
		{
			// Setup
			const string stest1 = "Name2";
			const string stest2 = "C2";
			const string stest3 = "AlphaName3";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(stest2, stest1, false),
				new Tuple<string, string, bool>("C3", stest3, true)};
			var cnt = LoadConfigListAndTest(configs, stest2);
			const string newName = stest3;

			// SUT1
			m_testPresenter.RenameConfigItem(stest2, newName);

			// Verify1
			Assert.AreEqual(cnt, m_testPresenter.StubConfigDict.Count,
				"Should have the same number of items.");
			DictConfigItem item;
			m_testPresenter.StubConfigDict.TryGetValue(stest2, out item);
			Assert.AreNotEqual(newName, item.DispName,
				"Should not have renamed config item.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method CopyConfigItem where the name that should be produced is already in
		/// use by another (copy of an existing) item.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CopyConfigItem_NameInUse()
		{
			// Setup
			const string stest1 = "AlphaName1";
			const string stest2 = "C1";
			const string stest3 = "Copy of " + stest1;
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>(stest2, stest1, true),
				new Tuple<string, string, bool>("C2", stest3, false)};
			var cnt = LoadConfigListAndTest(configs, stest2);
			const string newName = "Copy of " + stest1 + " (2)";

			// SUT1
			m_testPresenter.CopyConfigItem(stest2);

			// Verify1
			Assert.AreEqual(cnt + 1, m_testPresenter.StubConfigDict.Count,
				"Should have gained a copied item.");
			DictConfigItem item;
			var configItem = GetKeyFromValue(newName);
			Assert.IsNotNull(configItem, "Didn't find an item with the right Name.");
			Assert.IsTrue(configItem.IsNew, "New item should be marked as New.");
			Assert.AreEqual(stest2, configItem.CopyOf,
				"New item should be marked as a 'Copy of' old item.");
			Assert.AreEqual(stest2, m_testPresenter.StubOrigView,
				"Copy should not have changed original view.");
			Assert.AreEqual(configItem.UniqueCode, m_testPresenter.StubCurView,
				"Copy should have changed current view to the new view.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method CopyConfigItem where the name that should be produced is already in
		/// use by two other items. (i.e. Name and Name (2) are already used)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CopyConfigItem_NameInUseTwice()
		{
			// Setup
			const string stest1 = "AlphaName1";
			const string stest2 = "C1";
			const string stest3 = "Copy of " + stest1;
			const string stest4 = stest3 + " (2)";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>(stest2, stest1, true),
				new Tuple<string, string, bool>("C2", stest3, false),
				new Tuple<string, string, bool>("C3", stest4, false)};
			var cnt = LoadConfigListAndTest(configs, stest2);
			const string newName = "Copy of " + stest1 + " (3)";

			// SUT1
			m_testPresenter.CopyConfigItem(stest2);

			// Verify1
			Assert.AreEqual(cnt + 1, m_testPresenter.StubConfigDict.Count,
				"Should have gained a copied item.");
			DictConfigItem item;
			var configItem = GetKeyFromValue(newName);
			Assert.IsNotNull(configItem, "Didn't find an item with the right Name.");
			Assert.IsTrue(configItem.IsNew, "New item should be marked as New.");
			Assert.AreEqual(stest2, configItem.CopyOf,
				"New item should be marked as a 'Copy of' old item.");
			Assert.AreEqual(stest2, m_testPresenter.StubOrigView,
				"Copy should not have changed original view.");
			Assert.AreEqual(configItem.UniqueCode, m_testPresenter.StubCurView,
				"Copy should have changed current view to the new view.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method PersistState. Do the IDictConfigManager properties return the right
		/// things?
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestPersistResults()
		{
			// Setup
			const string sid2 = "C2";
			const string sname2 = "Name2";
			const string sid3 = "C3";
			const string sname3 = "AlphaName3";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(sid2, sname2, false),
				new Tuple<string, string, bool>(sid3, sname3, false)};
			LoadConfigListAndTest(configs, sid2);
			const string newName = "A Glorious New Name!";
			m_testPresenter.CopyConfigItem(sid2); // copy one
			m_testPresenter.RenameConfigItem(sid2, newName); // rename one
			m_testPresenter.TryMarkForDeletion(sid3); // delete one

			// SUT1
			m_testPresenter.PersistState();

			// Verify1
			Assert.AreEqual(1, m_testPresenter.NewConfigurationViews.Count(),
				"Wrong number of new items.");
			var configItem = GetKeyFromValue("Copy of " + sname2);
			Assert.IsNotNull(configItem, "Didn't find an item with the right Name.");
			Assert.AreEqual(configItem.UniqueCode, m_testPresenter.NewConfigurationViews.First().Item1,
				"Wrong unique code reported for new item.");
			Assert.AreEqual(sid2, m_testPresenter.NewConfigurationViews.First().Item2,
				"Wrong Copy Of reported for new item.");
			Assert.AreEqual(1, m_testPresenter.RenamedExistingViews.Count());
			Assert.AreEqual(sid2, m_testPresenter.RenamedExistingViews.First().Item1,
				"Wrong item reported as renamed.");
			Assert.AreEqual(newName, m_testPresenter.RenamedExistingViews.First().Item2,
				"Wrong new name reported for renamed item.");
			Assert.AreEqual(1, m_testPresenter.ConfigurationViewsToDelete.Count(),
				"Wrong number of deleted items.");
			Assert.AreEqual(sid3, m_testPresenter.ConfigurationViewsToDelete.First(),
				"Wrong item reported as deleted.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method PersistState. Does the RenamedExistingViews property return the right
		/// thing when a view is also marked as deleted?
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestPersistResults_RenamedAndDeleted()
		{
			// Setup
			const string sid2 = "C2";
			const string sname2 = "Name2";
			const string sid3 = "C3";
			const string sname3 = "AlphaName3";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(sid2, sname2, false),
				new Tuple<string, string, bool>(sid3, sname3, false)};
			LoadConfigListAndTest(configs, sid2);
			const string newName = "A Glorious New Name!";
			m_testPresenter.CopyConfigItem(sid2); // copy one
			m_testPresenter.RenameConfigItem(sid2, newName); // rename one
			m_testPresenter.TryMarkForDeletion(sid2); // delete the renamed one

			// SUT1
			m_testPresenter.PersistState();

			// Verify1
			Assert.AreEqual(1, m_testPresenter.NewConfigurationViews.Count(),
				"Wrong number of new items.");
			var configItem = GetKeyFromValue("Copy of " + sname2);
			Assert.IsNotNull(configItem, "Didn't find an item with the right Name.");
			Assert.AreEqual(configItem.UniqueCode, m_testPresenter.NewConfigurationViews.First().Item1,
				"Wrong unique code reported for new item.");
			Assert.AreEqual(sid2, m_testPresenter.NewConfigurationViews.First().Item2,
				"Wrong Copy Of reported for new item.");
			Assert.IsNull(m_testPresenter.RenamedExistingViews,
				"Deleted view should not be reported as renamed too.");
			Assert.AreEqual(1, m_testPresenter.ConfigurationViewsToDelete.Count(),
				"Wrong number of deleted items.");
			Assert.AreEqual(sid2, m_testPresenter.ConfigurationViewsToDelete.First(),
				"Wrong item reported as deleted.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method PersistState. Do the IDictConfigManager properties return null when
		/// NOT persisted?
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestUnpersistedResults()
		{
			// Setup
			const string sid2 = "C2";
			const string sname2 = "Name2";
			const string sid3 = "C3";
			const string sname3 = "AlphaName3";
			// Tuple is <uniqueCode, dispName, protected?>
			var configs = new List<Tuple<string, string, bool>> {
				new Tuple<string, string, bool>("C1", "BetaName1", true),
				new Tuple<string, string, bool>(sid2, sname2, false),
				new Tuple<string, string, bool>(sid3, sname3, false)};
			LoadConfigListAndTest(configs, sid2);
			const string newName = "A Glorious New Name!";
			m_testPresenter.CopyConfigItem(sid2); // copy one
			m_testPresenter.RenameConfigItem(sid2, newName); // rename one
			m_testPresenter.TryMarkForDeletion(sid3); // delete one

			// SUT1
			//m_testPresenter.PersistState(); Don't persist!

			// Verify1
			Assert.IsNull(m_testPresenter.RenamedExistingViews,
				"Should not have reported any views renamed.");
			Assert.IsNull(m_testPresenter.ConfigurationViewsToDelete,
				"Should not have reported any views deleted.");
			Assert.IsNull(m_testPresenter.NewConfigurationViews,
				"Should not have reported any views copied.");
		}
	}
}
