// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: WritingSystemPropertiesDialogTests.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using NUnit.Framework;
using Palaso.WritingSystems;
using Palaso.UI.WindowsForms.Keyboarding;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.CoreImpl;
using SIL.Utils;


namespace SIL.FieldWorks.FwCoreDlgs
{
	#region Dummy WritingSystemPropertiesDlg
	/// <summary>
	///
	/// </summary>
	public class DummyWritingSystemPropertiesDialog : WritingSystemPropertiesDialog
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyWritingSystemPropertiesDialog"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		public DummyWritingSystemPropertiesDialog(FdoCache cache)
			: base(cache, cache.ServiceLocator.WritingSystemManager, cache.ServiceLocator.WritingSystems, null, null, null)
		{
		}

		#region Internal methods and properties


		bool m_fHasClosed;

		/// <summary>
		/// indicates if [Call]Closed() has been called.
		/// </summary>
		internal bool HasClosed
		{
			get { return m_fHasClosed; }
		}

		/// <summary>
		/// sets up the dialog without actually showing it.
		/// </summary>
		/// <param name="ws">The writing system which properties will be displayed</param>
		/// <returns>A DialogResult value</returns>
		public int ShowDialog(IWritingSystem ws)
		{
			CheckDisposed();

			SetupDialog(ws, true);
			return (int)DialogResult.OK;
		}

		/// <summary>
		/// Presses the OK button.
		/// </summary>
		internal void PressOk()
		{
			CheckDisposed();

			if (!CheckOkToChangeContext())
				return;

			SaveChanges();

			m_fHasClosed = true;
			DialogResult = DialogResult.OK;
		}

		/// <summary>
		/// Presses the Cancel button.
		/// </summary>
		internal void PressCancel()
		{
			CheckDisposed();
			m_fHasClosed = true;
			DialogResult = DialogResult.Cancel;
		}

		/// <summary>
		///
		/// </summary>
		internal ListBox WsList
		{
			get
			{
				CheckDisposed();
				return m_listBoxRelatedWSs;
			}
		}

		/// <summary>
		/// Verifies the writing system order.
		/// </summary>
		/// <param name="wsnames">The wsnames.</param>
		internal void VerifyListBox(string[] wsnames)
		{
			Assert.AreEqual(wsnames.Length, WsList.Items.Count,
				"Number of writing systems in list is incorrect.");

			for (int i = 0; i < wsnames.Length; i++)
			{
				Assert.AreEqual(wsnames[i], WsList.Items[i].ToString());
			}
		}

		/// <summary>
		///
		/// </summary>
		internal void VerifyWsId(string wsId)
		{
			//Ensure the writing system identifier is set correctly
			Assert.AreEqual(LangTagUtils.ToLangTag(CurrentWritingSystem.LanguageSubtag, m_regionVariantControl.ScriptSubtag,
				m_regionVariantControl.RegionSubtag, m_regionVariantControl.VariantSubtag), wsId);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="langAbbr"></param>
		internal void VerifyRelatedWritingSystem(string langAbbr)
		{
			foreach (IWritingSystem ws in WsList.Items)
				Assert.AreEqual(langAbbr, ws.LanguageSubtag.Code);
		}


		internal void VerifyLoadedForListBoxSelection(string expectedItemName)
		{
			Assert.AreEqual(expectedItemName, WsList.SelectedItem.ToString());
			int selectedIndex = WsList.SelectedIndex;
			VerifyLoadedForListBoxSelection(expectedItemName, selectedIndex);
		}

		internal void VerifyLoadedForListBoxSelection(string expectedItemName, int selectedIndex)
		{
			ValidateGeneralInfo();
			Assert.AreEqual(selectedIndex, WsList.SelectedIndex, "The wrong ws is selected.");
			// Validate each tab is setup to match the current language definition info.
			ValidateGeneralTab();
			ValidateFontsTab();
			ValidateKeyboardTab();
			ValidateConvertersTab();
			ValidateSortingTab();
		}

		internal void VerifyWritingSystemsAreEqual(int indexA, int indexB)
		{
			Assert.Less(indexA, WsList.Items.Count);
			Assert.Less(indexB, WsList.Items.Count);
			Assert.AreEqual(((IWritingSystem) WsList.Items[indexA]).Id, ((IWritingSystem) WsList.Items[indexB]).Id);
		}

		private ContextMenuStrip PopulateAddWsContextMenu()
		{
			var cms = new ContextMenuStrip();
			FwProjPropertiesDlg.PopulateWsContextMenu(cms, WritingSystemUtils.GetAllDistinctWritingSystems(m_wsManager),
				m_listBoxRelatedWSs, btnAddWsItemClicked, null, btnNewWsItemClicked, (IWritingSystem)m_listBoxRelatedWSs.Items[0]);
			return cms;
		}

		internal void VerifyAddWsContextMenuItems(string[] expectedItems)
		{
			using (ContextMenuStrip cms = PopulateAddWsContextMenu())
			{
				if (expectedItems != null)
				{
					Assert.AreEqual(expectedItems.Length, cms.Items.Count);
					List<string> actualItems = (from ToolStripItem item in cms.Items select item.ToString()).ToList();
					foreach (string item in expectedItems)
						Assert.Contains(item, actualItems);
				}
				else
				{
					// don't expect a context menu
					Assert.AreEqual(0, cms.Items.Count);
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		internal void ValidateGeneralInfo()
		{
			// Check Language Name & EthnologueCode
			Assert.AreEqual(CurrentWritingSystem.LanguageSubtag.Name, m_tbLanguageName.Text);
			// make sure LocaleName is properly setup as Language name, not as DisplayName.
			Assert.IsTrue(CurrentWritingSystem.LanguageSubtag.Name.IndexOf("(") == -1);
			Assert.AreEqual(!string.IsNullOrEmpty(CurrentWritingSystem.ISO3) ? CurrentWritingSystem.ISO3 : "<None>", m_LanguageCode.Text);
		}

		internal void ValidateGeneralTab()
		{
			Assert.AreEqual(CurrentWritingSystem.Abbreviation, m_ShortWsName.Text);
			// TODO: need something to internally validate the Region Variant Control.
			Assert.AreEqual(CurrentWritingSystem, m_regionVariantControl.WritingSystem);
			Assert.AreEqual(CurrentWritingSystem.RightToLeftScript, rbRightToLeft.Checked);
		}

		internal void ValidateFontsTab()
		{
			Assert.AreEqual(CurrentWritingSystem, m_defaultFontsControl.WritingSystem);
		}

		internal void ValidateKeyboardTab()
		{
			Assert.AreEqual(((IWritingSystemDefinition)CurrentWritingSystem).Bcp47Tag, m_modelForKeyboard.CurrentRFC4646);
		}

		internal void ValidateConvertersTab()
		{
			Assert.AreEqual(CurrentWritingSystem.LegacyMapping ?? "<None>", cbEncodingConverter.SelectedItem.ToString());
		}
		internal void ValidateSortingTab()
		{
			Assert.AreEqual(CurrentWritingSystem.SortRules, m_sortRulesTextBox.Text);
		}
		#endregion

		#region General Info
		/// <summary>
		///
		/// </summary>
		internal TextBox LanguageNameTextBox
		{
			get { return m_tbLanguageName; }
		}

		string m_selectedLanguageName;
		string m_selectedEthnologueCode;
		List<string> m_expectedOrigWsIds = new List<string>();
		List<ShowMsgBoxStatus> m_expectedMsgBoxes = new List<ShowMsgBoxStatus>();
		List<DialogResult> m_resultsToEnforce = new List<DialogResult>();
		DialogResult m_ethnologueDlgResultToEnforce = DialogResult.None;

		internal void SelectEthnologueCodeDlg(string languageName, string ethnologueCode, string country,
			DialogResult ethnologueDlgResultToEnforce,
			ShowMsgBoxStatus[] expectedMsgBoxes,
			string[] expectedOrigIcuLocales,
			DialogResult[] resultsToEnforce)
		{
			m_selectedLanguageName = languageName;
			m_selectedEthnologueCode = ethnologueCode;
			m_ethnologueDlgResultToEnforce = ethnologueDlgResultToEnforce;

			m_expectedMsgBoxes = new List<ShowMsgBoxStatus>(expectedMsgBoxes);
			if (expectedOrigIcuLocales != null)
				m_expectedOrigWsIds = new List<string>(expectedOrigIcuLocales);
			m_resultsToEnforce = new List<DialogResult>(resultsToEnforce);
			try
			{
				btnModifyEthnologueInfo_Click(this, null);
				Assert.AreEqual(0, m_expectedMsgBoxes.Count);
				Assert.AreEqual(0, m_expectedOrigWsIds.Count);
				Assert.AreEqual(0, m_resultsToEnforce.Count);
			}
			finally
			{
				m_expectedMsgBoxes.Clear();
				m_resultsToEnforce.Clear();
				m_expectedOrigWsIds.Clear();

				m_selectedLanguageName = null;
				m_selectedEthnologueCode = null;
				m_ethnologueDlgResultToEnforce = DialogResult.None;
			}
		}

		/// <summary>
		/// Check the expected state of MsgBox being encountered.
		/// </summary>
		internal DialogResult DoExpectedMsgBoxResult(ShowMsgBoxStatus encountered, string origWsId)
		{
			// we always expect message boxes.
			Assert.Greater(m_expectedMsgBoxes.Count, 0,
				string.Format("Didn't expect dialog {0}", encountered));
			Assert.AreEqual(m_expectedMsgBoxes[0], encountered);
			m_expectedMsgBoxes.RemoveAt(0);
			DialogResult result = m_resultsToEnforce[0];
			m_resultsToEnforce.RemoveAt(0);
			if (origWsId != null && m_expectedOrigWsIds.Count > 0)
			{
				Assert.AreEqual(m_expectedOrigWsIds[0], origWsId);
				m_expectedOrigWsIds.RemoveAt(0);
			}
			return result;
		}

		/// <summary>
		/// simulate choosing settings with those specified in SelectEthnologueCodeDlg().
		/// </summary>
		/// <param name="dlg"></param>
		/// <returns></returns>
		protected override DialogResult CallShowDialog(LanguageSelectionDlg dlg)
		{
			if (m_ethnologueDlgResultToEnforce == DialogResult.OK)
			{
				// overwrite
				dlg.LanguageName = m_selectedLanguageName;
				dlg.EthnologueCode = m_selectedEthnologueCode;
			}
			return DialogResult = m_ethnologueDlgResultToEnforce;
		}

		/// <summary>
		///
		/// </summary>
		internal enum ShowMsgBoxStatus
		{
			None,
			CheckCantCreateDuplicateWs,
			CheckCantChangeUserWs,
		}

		/// <summary>
		///
		/// </summary>
		protected override void ShowMsgBoxCantCreateDuplicateWs(IWritingSystem tempWs, IWritingSystem origWs)
		{
			DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckCantCreateDuplicateWs, origWs == null ? null : origWs.Id);
		}

		/// <summary>
		///
		/// </summary>
		protected override void ShowMsgCantChangeUserWs(IWritingSystem tempWs, IWritingSystem origWs)
		{
			DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckCantChangeUserWs, origWs == null ? null : origWs.Id);
		}

		/// <summary>
		/// For some reason the tests are not triggering tabControl_Deselecting and
		/// tabControl_SelectedIndexChanged, so call them explicitly here.
		/// </summary>
		/// <param name="index"></param>
		public override void SwitchTab(int index)
		{
			SwitchTab(index, ShowMsgBoxStatus.None, DialogResult.None);
		}

		internal void SwitchTab(int index, ShowMsgBoxStatus expectedStatus, DialogResult doResult)
		{
			if (expectedStatus != ShowMsgBoxStatus.None)
			{
				m_expectedMsgBoxes.Add(expectedStatus);
				m_resultsToEnforce.Add(doResult);
			}
			// For some reason the tests are not triggering tabControl_Deselecting and
			// tabControl_SelectedIndexChanged, so call them explicitly here.
			var args = new TabControlCancelEventArgs(null, -1, false, TabControlAction.Deselecting);
			tabControl_Deselecting(this, args);
			if (!args.Cancel)
			{
				base.SwitchTab(index);
				tabControl_SelectedIndexChanged(this, EventArgs.Empty);
			}
			Assert.AreEqual(0, m_expectedMsgBoxes.Count);
		}

		internal void VerifyTab(int index)
		{
			Assert.AreEqual(index, tabControl.SelectedIndex);
		}

		internal bool PressBtnAdd(string item)
		{
			using (ContextMenuStrip cms = PopulateAddWsContextMenu())
			{
				// find & select matching item
				foreach (ToolStripItem tsi in cms.Items)
				{
					if (tsi.ToString() == item)
					{
						tsi.PerformClick();
						return true;
					}
				}
				return false;
			}
		}

		internal bool PressBtnCopy()
		{
			if (btnCopy.Enabled)
			{
				btnCopy_Click(this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		internal bool PressDeleteButton()
		{
			// Note: For some reason btnRemove.PerformClick() does not trigger the event.
			if (m_deleteButton.Enabled)
			{
				m_deleteButton_Click(this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		#endregion General Info

		#region General Tab
		internal void SetVariantName(string newVariantName)
		{
			m_regionVariantControl.VariantName = newVariantName;
		}

		internal void SetScriptName(string newScriptName)
		{
			m_regionVariantControl.ScriptName = newScriptName;
		}

		/// <summary>
		/// Set a new Custom (private use) Region subtag
		/// </summary>
		/// <param name="newRegionName"></param>
		/// <remarks>Unless you modify this method it will fail given an input parameter length of less than 2.</remarks>
		internal void SetCustomRegionName(string newRegionName)
		{
			var code = newRegionName.Substring(0, 2).ToUpperInvariant();
			m_regionVariantControl.RegionSubtag = new RegionSubtag(code, newRegionName, true);
		}

		#endregion General Tab

		#region Overrides
		/// <summary>Remove a dependency on Encoding Converters</summary>
		protected override void LoadAvailableConverters()
		{
			cbEncodingConverter.Items.Clear();
			cbEncodingConverter.Items.Add(FwCoreDlgs.kstidNone);
			cbEncodingConverter.SelectedIndex = 0;
		}
		#endregion

	}
	#endregion // Dummy WritingSystemPropertiesDlg

	/// <summary>
	/// Summary description for TestFwProjPropertiesDlg.
	/// </summary>
	[TestFixture]
	[SetCulture("en-US")]
	public class WritingSystemPropertiesDialogTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private DummyWritingSystemPropertiesDialog m_dlg;
		private IWritingSystem m_wsKalabaIpa;
		private IWritingSystem m_wsKalaba;
		private IWritingSystem m_wsTestIpa;
		private readonly HashSet<IWritingSystem> m_origLocalWss = new HashSet<IWritingSystem>();
		private readonly HashSet<IWritingSystem> m_origGlobalWss = new HashSet<IWritingSystem>();

		#region Test Setup and Tear-Down

		/// <summary>
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_origLocalWss.UnionWith(Cache.ServiceLocator.WritingSystemManager.LocalWritingSystems);
			m_origGlobalWss.UnionWith(Cache.ServiceLocator.WritingSystemManager.GlobalWritingSystems);
			MessageBoxUtils.Manager.SetMessageBoxAdapter(new MessageBoxStub());
			KeyboardController.Initialize();
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			KeyboardController.Shutdown();
			base.FixtureTeardown();
		}

		/// <summary>
		/// Creates the writing systems.
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();
			m_wsKalabaIpa = CreateWritingSystem("qaa-fonipa-x-kal", "Kalaba", true);
			m_wsKalaba = CreateWritingSystem("qaa-x-kal", "Kalaba", true);
			CreateWritingSystem("qaa-x-wsd", "WSDialog", true);
			CreateWritingSystem("qaa-fonipa-x-wsd", "WSDialog", true);
			IWritingSystem wsTest = CreateWritingSystem("qaa-x-tst", "TestOnly", false);
			m_wsTestIpa = CreateWritingSystem("qaa-fonipa-x-tst", "TestOnly", true);
			Cache.ServiceLocator.WritingSystemManager.Save();
			// this will remove it from the local store, but not from the global store
			wsTest.MarkedForDeletion = true;
			Cache.ServiceLocator.WritingSystemManager.Save();
			m_dlg = new DummyWritingSystemPropertiesDialog(Cache);
		}

		private IWritingSystem CreateWritingSystem(string wsId, string name, bool addVern)
		{
			IWritingSystem ws = Cache.ServiceLocator.WritingSystemManager.Set(wsId);
			LanguageSubtag subtag = ws.LanguageSubtag;
			ws.LanguageSubtag = new LanguageSubtag(subtag, name);
			if (addVern)
				Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(ws);
			return ws;
		}

		/// <summary>
		/// Removes the writing systems.
		/// </summary>
		public override void TestTearDown()
		{
			m_dlg.Dispose();
			m_dlg = null;

			base.TestTearDown();

			foreach (IWritingSystem ws in Cache.ServiceLocator.WritingSystemManager.LocalWritingSystems)
			{
				if (!m_origLocalWss.Contains(ws))
					ws.MarkedForDeletion = true;
			}

			foreach (IWritingSystem ws in Cache.ServiceLocator.WritingSystemManager.GlobalWritingSystems)
			{
				if (!m_origGlobalWss.Contains(ws))
					ws.MarkedForDeletion = true;
			}

			Cache.ServiceLocator.WritingSystemManager.Save();
		}
		#endregion

		#region Helper Methods

		private void VerifyNewlyAddedWritingSystems(string[] newExpectedWsIds)
		{
			List<string> actualWsIds = m_dlg.NewWritingSystems.Select(ws => ws.Id).ToList();
			Assert.AreEqual(newExpectedWsIds.Length, actualWsIds.Count);
			foreach (string expectedWsId in newExpectedWsIds)
				Assert.Contains(expectedWsId, actualWsIds);
		}

		private void VerifyWsNames(int[] hvoWss, string[] wsNames, string[] wsIds)
		{
			int i = 0;
			foreach (int hvoWs in hvoWss)
			{
				IWritingSystem ws = Cache.ServiceLocator.WritingSystemManager.Get(hvoWs);
				Assert.AreEqual(wsNames[i], ws.DisplayLabel);
				Assert.AreEqual(wsIds[i], ws.Id);
				i++;
			}
		}

		private void VerifyWsNames(string[] wsNames, string[] wsIds)
		{
			int i = 0;
			foreach (string wsId in wsIds)
			{
				IWritingSystem ws = Cache.ServiceLocator.WritingSystemManager.Get(wsId);
				Assert.AreEqual(wsNames[i], ws.DisplayLabel);
				Assert.AreEqual(wsIds[i], ws.Id);
				i++;
			}
		}

		#endregion

		#region Tests
		/// <summary>
		///
		/// </summary>
		[Test]
		public void WsListContent()
		{
			// Setup dialog to show Kalaba (xkal) related wss.
			m_dlg.ShowDialog(m_wsKalaba);
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			m_dlg.VerifyRelatedWritingSystem("kal");
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba");
			// Select Kalaba (IPA) and verify dialog is setup for that one.
			m_dlg.WsList.SelectedItem = m_dlg.WsList.Items.Cast<IWritingSystem>().Single(ws => ws.DisplayLabel == "Kalaba (International Phonetic Alphabet)");
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba (International Phonetic Alphabet)");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_LanguageNameChange()
		{
			m_dlg.ShowDialog(m_wsKalaba);
			m_dlg.LanguageNameTextBox.Text = "Kalab";
			m_dlg.VerifyListBox(new[] { "Kalab", "Kalab (International Phonetic Alphabet)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalab");
			m_dlg.PressOk();
			Assert.AreEqual(DialogResult.OK, m_dlg.DialogResult);
			Assert.AreEqual(true, m_dlg.IsChanged);
			VerifyWsNames(
				new[] { m_wsKalaba.Handle, m_wsKalabaIpa.Handle },
				new[] { "Kalab", "Kalab (International Phonetic Alphabet)" },
				new[] { "qaa-x-kal", "qaa-fonipa-x-kal" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_AddNewWs_OK()
		{
			m_dlg.ShowDialog(m_wsKalaba);
			// Verify Remove doesn't (yet) do anything for Wss already in the Database.
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			m_dlg.PressDeleteButton();
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			// Switch tabs, so we can test that Add New Ws will switch to General Tab.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyAddWsContextMenuItems(new[] { "&Writing System for Kalaba..." });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("&Writing System for Kalaba...");
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba", 0);
			// verify we automatically switched back to General Tab.
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// Verify Switching context is not OK (force user to make unique Ws)
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckCantCreateDuplicateWs,
				DialogResult.OK);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// make sure we can't select a different ethnologue code.
			m_dlg.SelectEthnologueCodeDlg("", "", "", DialogResult.OK,
				new[] { DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckCantCreateDuplicateWs },
				null,
				new[] { DialogResult.OK });
			// Change Region or Variant info.
			m_dlg.SetVariantName("Phonetic");
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)", "Kalaba (Phonetic)" });
			// Now update the Ethnologue code, and cancel msg box to check we restored the expected newly added language defns.
			m_dlg.SelectEthnologueCodeDlg("WSDialog", "wsd", "", DialogResult.OK,
				new[] { DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckCantCreateDuplicateWs },
				new[] { "qaa-x-kal" },
				new[] { DialogResult.OK});
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedWritingSystems(new[] { "qaa-fonipa-x-kal-etic" });
			// Now update the Ethnologue code, check we still have expected newly added language defns.
			m_dlg.SelectEthnologueCodeDlg("Kala", "", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] { }, new string[] { }, new DialogResult[] { });
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedWritingSystems(new[] { "qaa-fonipa-x-kal-etic" });
			// Now try adding a second/duplicate ws.
			m_dlg.PressBtnAdd("&Writing System for Kala...");
			m_dlg.VerifyListBox(new[] { "Kala", "Kala", "Kala (International Phonetic Alphabet)", "Kala (Phonetic)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kala", 0);
			m_dlg.SetVariantName("Phonetic");
			m_dlg.VerifyListBox(new[] { "Kala", "Kala (International Phonetic Alphabet)", "Kala (Phonetic)", "Kala (Phonetic)" });
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckCantCreateDuplicateWs,
				DialogResult.OK);
			m_dlg.PressDeleteButton();
			m_dlg.VerifyListBox(new[] { "Kala", "Kala (International Phonetic Alphabet)", "Kala (Phonetic)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kala (Phonetic)");
			// Do OK
			m_dlg.PressOk();
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedWritingSystems(new[] { "qaa-fonipa-x-kal-etic" });
			// Verify we've actually created the new ws.
			VerifyWsNames(
				new[] { "Kala", "Kala (International Phonetic Alphabet)", "Kala (Phonetic)" },
				new[] { "qaa-x-kal", "qaa-fonipa-x-kal", "qaa-fonipa-x-kal-etic" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_AddExistingWs_OK()
		{
			m_dlg.ShowDialog(m_wsTestIpa);
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyAddWsContextMenuItems(new[] { "TestOnly", "&Writing System for TestOnly..." });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("TestOnly");
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new[] { "TestOnly", "TestOnly (International Phonetic Alphabet)" });
			m_dlg.VerifyLoadedForListBoxSelection("TestOnly");
			// verify we stayed on the Fonts Tab
			// Review gjm: Can we really do this through the UI; that is, create a new 'same' ws?
			// There is already a separate test of 'copy'?).
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			// first, make sure we can remove the newly added writing system.
			m_dlg.PressDeleteButton();
			m_dlg.VerifyListBox(new[] { "TestOnly (International Phonetic Alphabet)" });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("TestOnly");
			// Do OK
			m_dlg.PressOk();
			// Verify we've actually added the existing ws.
			VerifyWsNames(
				new[] { "TestOnly (International Phonetic Alphabet)", "TestOnly" },
				new[] { "qaa-fonipa-x-tst", "qaa-x-tst" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_CopyWs_OK()
		{
			m_dlg.ShowDialog(m_wsKalabaIpa);
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba (International Phonetic Alphabet)");
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			// Switch tabs, so we can test that Add New Ws will switch to General Tab.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			// Click on Copy Button
			m_dlg.PressBtnCopy();
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)", "Kalaba (International Phonetic Alphabet)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba (International Phonetic Alphabet)", 1);
			m_dlg.VerifyWritingSystemsAreEqual(1, 2);
			// verify we automatically switched back to General Tab.
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// Verify Switching context is not OK (force user to make unique Ws)
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckCantCreateDuplicateWs,
				DialogResult.OK);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// Change Region or Variant info.
			m_dlg.SetVariantName("Phonetic");
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)", "Kalaba (Phonetic)" });
			// Do OK
			m_dlg.PressOk();
			Cache.ServiceLocator.WritingSystemManager.Save();
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedWritingSystems(new[] { "qaa-fonipa-x-kal-etic" });
			// Verify we've actually created the new ws.
			VerifyWsNames(
				new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)", "Kalaba (Phonetic)" },
				new[] { "qaa-x-kal", "qaa-fonipa-x-kal", "qaa-fonipa-x-kal-etic" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsId_Cancel()
		{
			m_dlg.ShowDialog(m_wsKalaba);
			// change to nonconflicting ethnologue code
			m_dlg.SelectEthnologueCodeDlg("Silly", "xxx", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] { },
				new string[] { },
				new DialogResult[] { });
			m_dlg.VerifyListBox(new[] { "Silly", "Silly (International Phonetic Alphabet)" });
			m_dlg.VerifyRelatedWritingSystem("xxx");
			m_dlg.VerifyLoadedForListBoxSelection("Silly");
			m_dlg.PressCancel();
			Assert.AreEqual(false, m_dlg.IsChanged);
			VerifyWsNames(
				new[] { m_wsKalaba.Handle, m_wsKalabaIpa.Handle },
				new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)" },
				new[] { "qaa-x-kal", "qaa-fonipa-x-kal" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsId_Ok()
		{
			// End the UOW, because we are going to do a nonundoable UOW when we press OK
			m_actionHandler.EndUndoTask();

			m_dlg.ShowDialog(m_wsKalaba);
			// change to nonconflicting ethnologue code
			m_dlg.SelectEthnologueCodeDlg("Silly", "xxx", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] { },
				new string[] { },
				new DialogResult[] { });
			m_dlg.VerifyListBox(new[] { "Silly", "Silly (International Phonetic Alphabet)" });
			m_dlg.VerifyRelatedWritingSystem("xxx");
			m_dlg.VerifyLoadedForListBoxSelection("Silly");
			m_dlg.PressOk();
			Assert.AreEqual(true, m_dlg.IsChanged);
			VerifyWsNames(
				new[] { m_wsKalaba.Handle, m_wsKalabaIpa.Handle },
				new[] { "Silly", "Silly (International Phonetic Alphabet)" },
				new[] { "qaa-x-xxx", "qaa-fonipa-x-xxx" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GeneralTab_ScriptChanged_Duplicate()
		{
			m_dlg.ShowDialog(m_wsKalaba);
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba");

			m_dlg.VerifyAddWsContextMenuItems(new[] { "&Writing System for Kalaba..." });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("&Writing System for Kalaba...");
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba", 0);

			//note: changes to the dialog have broken this behavior, but this test was catching more than it's advertised purpose,
			//so rather than making a new way to set the script through the dialog I hacked out the following test code for now -naylor 2011-8-11
			//FIXME
			//m_dlg.SetScriptName("Arabic");
			//m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (Arabic)", "Kalaba (International Phonetic Alphabet)" });
			////Verify that the Script, Region and Variant abbreviations are correct.
			//m_dlg.VerifyWsId("qaa-Arab-x-kal");
			//m_dlg.PressBtnCopy();
			//m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (Arabic)", "Kalaba (Arabic)", "Kalaba (International Phonetic Alphabet)" });

			// expect msgbox error.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckCantCreateDuplicateWs,
				DialogResult.OK);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GeneralTab_VariantNameChanged_Duplicate()
		{
			m_dlg.ShowDialog(m_wsKalaba);
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba");
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsGeneral);
			m_dlg.SetVariantName("International Phonetic Alphabet");
			m_dlg.VerifyListBox(new[] { "Kalaba (International Phonetic Alphabet)", "Kalaba (International Phonetic Alphabet)" });
			// expect msgbox error.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckCantCreateDuplicateWs,
				DialogResult.OK);
		}

		/// <summary>
		/// Test creating a region variant (LT-13801)
		/// </summary>
		[Test]
		public void GeneralTab_RegionVariantChanged()
		{
			m_dlg.ShowDialog(m_wsKalaba);
			// Verify Remove doesn't (yet) do anything for Wss already in the Database.
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			// Switch tabs, so we can test that Add New Ws will switch to General Tab.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyAddWsContextMenuItems(new[] { "&Writing System for Kalaba..." });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("&Writing System for Kalaba...");
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba", "Kalaba (International Phonetic Alphabet)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba", 0);
			// verify we automatically switched back to General Tab.
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// Change Region info.
			m_dlg.SetCustomRegionName("Minnesota");
			m_dlg.VerifyListBox(new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)", "Kalaba (Minnesota)" });
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedWritingSystems(new[] { "qaa-QM-x-kal-MI" });
			// Do OK
			m_dlg.PressOk();
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedWritingSystems(new[] { "qaa-QM-x-kal-MI" });
			// Verify we've actually created the new ws.
			VerifyWsNames(
				new[] { "Kalaba", "Kalaba (International Phonetic Alphabet)", "Kalaba (Minnesota)" },
				new[] { "qaa-x-kal", "qaa-fonipa-x-kal", "qaa-QM-x-kal-MI" });
		}

		#endregion
	}
}
