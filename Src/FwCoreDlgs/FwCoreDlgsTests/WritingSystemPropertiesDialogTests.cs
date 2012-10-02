// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WritingSystemPropertiesDialogTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using NMock;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;


namespace SIL.FieldWorks.FwCoreDlgs
{
	#region Dummy WritingSystemPropertiesDlg
	/// ------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class DummyWritingSystemPropertiesDialog : WritingSystemPropertiesDialog
	{
		static internal Dictionary<string, string> s_icuLocaleToTmpLangDefFiles = new Dictionary<string, string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyWritingSystemPropertiesDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyWritingSystemPropertiesDialog(FdoCache cache)
			: base(cache, null, null)
		{
			this.OnAboutToMergeWritingSystems += new EventHandler(wsProps_OnAboutToMergeWritingSystems);
		}

		void wsProps_OnAboutToMergeWritingSystems(object sender, EventArgs e)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="strServer"></param>
		/// <param name="strDatabase"></param>
		protected override void CheckServerAndDatabaseInfo(string strServer, string strDatabase)
		{
			// do nothing for InMemory cache.
		}

		/// <summary>
		/// serialize the language definitions to temporary files, so we don't clobber the ones
		/// used by the application or other tests.
		/// </summary>
		/// <param name="finalLangDef"></param>
		protected override void Serialize(LanguageDefinition finalLangDef)
		{
			if (m_cache.LanguageWritingSystemFactoryAccessor.BypassInstall)
				SerializeToTempfile(finalLangDef);
			else
				base.Serialize(finalLangDef);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="finalLangDef"></param>
		/// <returns></returns>
		protected override bool IsLocaleInLanguagesDir(LanguageDefinition finalLangDef)
		{
			if (m_cache.LanguageWritingSystemFactoryAccessor.BypassInstall)
			{
				string filename;
				if (s_icuLocaleToTmpLangDefFiles.TryGetValue(finalLangDef.WritingSystem.IcuLocale,
					out filename) && File.Exists(filename))
				{
					return true;
				}
			}

			return base.IsLocaleInLanguagesDir(finalLangDef);
		}

		internal static void CopyLangDefToTempLanguageDir(string icuLocale, string srcFilename)
		{
			if (File.Exists(srcFilename))
			{
				string srcName = Path.GetFileName(srcFilename);
				string dstFullPath = Path.Combine(Path.GetTempPath(), srcName);
				if (File.Exists(dstFullPath))
					File.SetAttributes(dstFullPath, FileAttributes.Normal);
				File.Copy(srcFilename, dstFullPath, true);
				File.SetAttributes(dstFullPath, FileAttributes.Normal);
				if (File.Exists(dstFullPath))
					s_icuLocaleToTmpLangDefFiles.Add(icuLocale, dstFullPath);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="langDef"></param>
		internal static void SerializeToTempfile(LanguageDefinition langDef)
		{
			if (LanguageDefinitionFactory.WritingSystemFactory == null)
				LanguageDefinitionFactory.WritingSystemFactory = langDef.WritingSystem.WritingSystemFactory;
			string tmpFilename;
			if (!s_icuLocaleToTmpLangDefFiles.TryGetValue(langDef.WritingSystem.IcuLocale, out tmpFilename))
			{
				tmpFilename = Path.GetTempFileName();
				langDef.Serialize(tmpFilename);
				if (File.Exists(tmpFilename))
					s_icuLocaleToTmpLangDefFiles.Add(langDef.WritingSystem.IcuLocale, tmpFilename);
				return;
			}
			langDef.Serialize(tmpFilename);
		}

		/// <summary>
		///  first try to deserialize from a temporary (test) xml, if any.
		/// </summary>
		/// <param name="qwsf"></param>
		/// <param name="locale"></param>
		/// <returns></returns>
		protected override LanguageDefinition CreateLanguageDefFromXml(ILgWritingSystemFactory qwsf,
			string locale)
		{
			LanguageDefinition langDef = null;
			if (m_cache.LanguageWritingSystemFactoryAccessor.BypassInstall)
				langDef = DeserializeLanguageDefFromTempfile(qwsf, locale);
			if (langDef == null)
				return base.CreateLanguageDefFromXml(qwsf, locale);
			return langDef;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="qwsf"></param>
		/// <param name="locale"></param>
		/// <returns></returns>
		internal static LanguageDefinition DeserializeLanguageDefFromTempfile(ILgWritingSystemFactory qwsf, string locale)
		{
			LanguageDefinitionFactory langDefFactory = new LanguageDefinitionFactory();
			if (LanguageDefinitionFactory.WritingSystemFactory == null)
				LanguageDefinitionFactory.WritingSystemFactory = qwsf;
			string filename;
			if (s_icuLocaleToTmpLangDefFiles.TryGetValue(locale, out filename) && File.Exists(filename))
			{
				langDefFactory.Deserialize(filename);
				return langDefFactory.LanguageDefinition as LanguageDefinition;
			}
			return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		protected override Set<NamedWritingSystem> GetAllNamedWritingSystems()
		{
			Set<NamedWritingSystem> namedWritingSystems = base.GetAllNamedWritingSystems();
			Set<string> names = new Set<string>();
			foreach (ILgWritingSystem ws in m_cache.LanguageEncodings)
				names.Add(ws.ICULocale);

			// Now add the ones from the temporary languageDefns.
			List<string> tmpLDFs = new List<string>(s_icuLocaleToTmpLangDefFiles.Values);
			foreach (KeyValuePair<string,string> kvp in s_icuLocaleToTmpLangDefFiles)
			{
				// Not one we already found in the database.
				if (!names.Contains(kvp.Key))
				{
					LanguageDefinition langDef = DeserializeLanguageDefFromTempfile(m_cache.LanguageWritingSystemFactoryAccessor, kvp.Key);
					string displayName = langDef.DisplayName;
					namedWritingSystems.Add(new NamedWritingSystem(displayName, kvp.Key));
				}
			}
			return namedWritingSystems;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				CleanupTempLangDefFiles();
			}
			base.Dispose(disposing);
		}

		internal static void CleanupTempLangDefFiles()
		{
			// delete all the temporary language definitions.
			foreach (string filename in s_icuLocaleToTmpLangDefFiles.Values)
			{
				File.SetAttributes(filename, FileAttributes.Normal);
				File.Delete(filename);
			}
			s_icuLocaleToTmpLangDefFiles.Clear();
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Calls the show dialog. (using UserWs)
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		internal int CallShowDialog()
		{
			CheckDisposed();
			return CallShowDialog(0);
		}

		/// <summary>
		/// Call Show dialog initializing with respect to the given ws
		/// </summary>
		/// <param name="wsHvo"></param>
		/// <returns></returns>
		internal int CallShowDialog(int wsHvo)
		{
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			int nRet = (int)DialogResult.Abort;
			if (wsHvo == 0)
				wsHvo = wsf.UserWs;
			IWritingSystem ws = wsf.get_EngineOrNull(wsHvo);
			System.Diagnostics.Debug.WriteLine("Abbr: " + ws.get_Abbr(wsHvo));
			nRet = ShowDialog(ws);
			return nRet;
		}

		bool m_fHasClosed = false;
		/// <summary>
		/// don't close in tests, b/c it prematurely disposes object.
		/// </summary>
		protected override void CallClose()
		{
			m_fHasClosed = true;
		}

		/// <summary>
		/// indicates if [Call]Closed() has been called.
		/// </summary>
		internal bool HasClosed
		{
			get { return m_fHasClosed; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// sets up the dialog without actually showing it.
		/// </summary>
		/// <param name="ws">The writing system which properties will be displayed</param>
		/// <returns>A DialogResult value</returns>
		/// --------------------------------------------------------------------------------
		public override int ShowDialog(IWritingSystem ws)
		{
			CheckDisposed();

			if (!TrySetupDialog(ws))
				return (int)DialogResult.Abort;
			return (int)DialogResult.OK;
		}



		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Presses the OK button.
		/// </summary>
		/// --------------------------------------------------------------------------------
		internal void PressOk()
		{
			CheckDisposed();

			OnOk(null, EventArgs.Empty);

			foreach (LanguageDefinition langDef in this.GetPendingWsMerges())
			{
				// for tests, just mark as being merged
				langDef.IcuLocaleTarget = "";
			}
		}

		/// <summary>
		/// override, since this causes problems in tests.
		/// </summary>
		/// <returns></returns>
		protected override IFwDbMergeWrtSys CreateFwDbMergeWrtSysClass()
		{
			return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="dmws"></param>
		/// <param name="wsIdOld"></param>
		/// <param name="oldWsName"></param>
		/// <param name="wsIdNew"></param>
		/// <param name="newWsName"></param>
		protected override void DoPendingWsMerge(IFwDbMergeWrtSys dmws, int wsIdOld, string oldWsName,
			int wsIdNew, string newWsName)
		{
		}

		/// <summary>
		/// </summary>
		protected override void CloseDbAndWindows()
		{
		}

		/// <summary>
		/// </summary>
		protected override void CreateNewMainWnd()
		{
		}

		/// <summary>
		/// Presses the Cancel button.
		/// </summary>
		internal void PressCancel()
		{
			CheckDisposed();
			OnCancel(null, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ListBox WsList
		{
			get
			{
				CheckDisposed();
				return m_listBoxRelatedWSs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the writing system order.
		/// </summary>
		/// <param name="wsnames">The wsnames.</param>
		/// ------------------------------------------------------------------------------------
		internal void VerifyListBox(string[] wsnames)
		{
			Assert.AreEqual(wsnames.Length, WsList.Items.Count,
				"Number of writing systems in list is incorrect.");

			for (int i = 0; i < wsnames.Length; i++)
			{
				Assert.AreEqual(wsnames[i], WsList.Items[i].ToString());
				Assert.AreEqual(wsnames[i], FinalLanguageDefns[i].DisplayName);
			}
		}

		/// <summary>
		///
		/// </summary>
		internal void VerifyFullIcuLocale(string IcuCompareStr)
		{
			//Ensure the ICU locale is set correctly
			Assert.AreEqual(m_regionVariantControl.ConstructIcuLocaleFromAbbreviations(), IcuCompareStr);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="langAbbr"></param>
		internal void VerifyRelatedIcuLocale(string langAbbr)
		{
			foreach (LanguageDefinition langDef in m_listFinalLangDefs)
			{
				Assert.AreEqual(langAbbr, langDef.LocaleAbbr);
			}
		}

		internal void VerifyPendingMerges(string[] icuLocalesGettingMerged)
		{
			List<string> icuLocalesToMerge = new List<string>(icuLocalesGettingMerged);
			List<LanguageDefinition> langDefsToMerge = GetPendingWsMerges();
			List<string> langDefIcuLocales = new List<string>();
			Assert.AreEqual(icuLocalesToMerge.Count, langDefsToMerge.Count);
			foreach (LanguageDefinition langDef in langDefsToMerge)
			{
				Assert.IsTrue(langDef.HasPendingMerge());
				langDefIcuLocales.Add(langDef.WritingSystem.IcuLocale);
			}
			foreach (string icuLocale in icuLocalesToMerge)
			{
				Assert.Contains(icuLocale, langDefIcuLocales);
			}
		}

		internal void VerifyOverwritesPending(string[] icuLocalesOrig)
		{
			// verify the language definition has been loaded, and that
			// it still does not exist in the database, but only on the disk.
			List<string> icuLocalesToOverwrite = new List<string>(icuLocalesOrig);
			List<LanguageDefinition> langDefsToOverwrite = GetPendingWsOverwrites();
			Assert.AreEqual(icuLocalesOrig.Length, langDefsToOverwrite.Count);
			foreach (LanguageDefinition langDef in langDefsToOverwrite)
			{
				Assert.Contains(langDef.IcuLocaleTarget, icuLocalesToOverwrite);
				// verify this language definition has been reloaded from xml
				// since we initialized the dialog.
				Assert.AreEqual(langDef.IcuLocaleOriginal, langDef.WritingSystem.IcuLocale);
				// make sure we haven't loaded it into the database yet.
				Assert.IsFalse(langDef.IsWritingSystemInDb());
				Assert.IsTrue(langDef.IsLocaleInLanguagesDir());
			}
		}


		internal void VerifyLoadedForListBoxSelection(string expectedItemName)
		{
			Assert.AreEqual(expectedItemName, WsList.SelectedItem.ToString());
			int selectedIndex = WsList.SelectedIndex;
			VerifyLoadedForListBoxSelection(expectedItemName, selectedIndex);
		}

		internal void VerifyLoadedForListBoxSelection(string expectedItemName, int selectedIndex)
		{
			LanguageDefinition langDef = m_listFinalLangDefs[selectedIndex];
			Assert.AreEqual(expectedItemName, langDef.DisplayName);
			Assert.AreEqual(langDef, m_langDefCurrent);
			ValidateWsListInfo();
			ValidateGeneralInfo();
			// Validate each tab is setup to match the current language definition info.
			ValidateGeneralTab();
			ValidateFontsTab();
			ValidateKeyboardTab();
			ValidateConvertersTab();
			ValidateSortingTab();
			ValidateCharactersTab();
		}

		internal void VerifyCurrentLangDefIsLoadedWithDefaults()
		{
			// we expect the current language definition to have default fonts.
			Assert.AreEqual("Times New Roman", m_langDefCurrent.WritingSystem.DefaultSerif);
			Assert.AreEqual("Arial", m_langDefCurrent.WritingSystem.DefaultSansSerif);
			Assert.AreEqual("Charis SIL", m_langDefCurrent.WritingSystem.DefaultBodyFont);
		}

		internal void VerifyLanguageDefinitionsAreEqual(int indexA, int indexB)
		{
			Assert.Less(indexA, m_listFinalLangDefs.Count);
			Assert.Less(indexB, m_listFinalLangDefs.Count);
			Assert.IsTrue(LanguageDefinition.HaveSameValues(m_listFinalLangDefs[indexA], m_listFinalLangDefs[indexB]));
		}

		internal void VerifyAddWsContextMenuItems(string[] expectedItems)
		{
			ContextMenuStrip cms = PopulateAddWsContextMenu();
			if (expectedItems != null)
			{
				Assert.AreEqual(expectedItems.Length, cms.Items.Count);
				List<string> actualItems = new List<string>();
				foreach (ToolStripItem item in cms.Items)
					actualItems.Add(item.ToString());
				foreach (string item in expectedItems)
					Assert.Contains(item, actualItems);
			}
			else
			{
				// don't expect a context menu
				Assert.AreEqual(0, cms.Items.Count);
			}
		}

		/// <summary>
		///
		/// </summary>
		internal void ValidateWsListInfo()
		{
			// Validate current selected language def.
			Assert.AreEqual(m_langDefCurrent.DisplayName, WsList.SelectedItem.ToString());
			// TODO: Validate state of buttons
		}


		/// <summary>
		///
		/// </summary>
		internal void ValidateGeneralInfo()
		{
			// Check Language Name & EthnologueCode
			Assert.AreEqual(m_langDefCurrent.LocaleName, m_tbLanguageName.Text);
			// make sure LocaleName is properly setup as Language name, not as DisplayName.
			Assert.IsTrue(m_langDefCurrent.LocaleName.IndexOf("(") == -1);
			Assert.AreEqual(!String.IsNullOrEmpty(m_langDefCurrent.EthnoCode) ?
			m_langDefCurrent.EthnoCode : "<None>", m_LanguageCode.Text);
		}

		internal void ValidateGeneralTab()
		{
			Assert.AreEqual(m_langDefCurrent.WritingSystem.get_Abbr(m_displayWs), m_ShortWsName.Text);
			// TODO: need something to internally validate the Region Variant Control.
			Assert.AreEqual(m_langDefCurrent, m_regionVariantControl.LangDef);
			Assert.AreEqual(m_langDefCurrent.WritingSystem.RightToLeft, rbRightToLeft.Checked);
		}

		internal void ValidateFontsTab()
		{
			Assert.AreEqual(m_langDefCurrent, m_defaultFontsControl.LangDef);
		}

		internal void ValidateKeyboardTab()
		{
			Assert.AreEqual(m_langDefCurrent, m_KeyboardControl.LangDef);
		}

		internal void ValidateConvertersTab()
		{
			Assert.AreEqual(m_langDefCurrent.WritingSystem.LegacyMapping != null ?
				m_langDefCurrent.WritingSystem.LegacyMapping : "<None>",
				cbEncodingConverter.SelectedItem.ToString());
		}
		internal void ValidateSortingTab()
		{
			Assert.AreEqual(GetIcuRules(m_langDefCurrent), txtIcuRules.Text);
		}
		internal void ValidateCharactersTab()
		{
			Assert.AreEqual(m_langDefCurrent.PuaDefinitionCount, m_lstPUACharacters.Items.Count);
			//Next add the characters which are checked to the PUA list for the writing system
			if (m_langDefCurrent.PuaDefinitionCount > 0)
			{
				foreach (CharDef charDef in m_langDefCurrent.PuaDefinitions)
					Assert.IsTrue(IsDefinedPuaCharacter(charDef.code));
			}
		}

		#region General Info
		/// <summary>
		///
		/// </summary>
		internal TextBox LanguageNameTextBox
		{
			get { return m_tbLanguageName; }
		}

		string m_selectedLanguageName = null;
		string m_selectedEthnologueCode = null;
		List<string> m_expectedOrigIcuLocales = new List<string>();
		List<ShowMsgBoxStatus> m_expectedMsgBoxes = new List<ShowMsgBoxStatus>();
		List<DialogResult> m_resultsToEnforce = new List<DialogResult>();
		DialogResult m_ethnologueDlgResultToEnforce = DialogResult.None;

		internal void SelectEthnologueCodeDlg(string languageName, string ethnologueCode, string country,
			DialogResult ethnologueDlgResultToEnforce,
			DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] expectedMsgBoxes,
			string[] expectedOrigIcuLocales,
			DialogResult[] resultsToEnforce)
		{
			m_selectedLanguageName = languageName;
			m_selectedEthnologueCode = ethnologueCode;
			m_ethnologueDlgResultToEnforce = ethnologueDlgResultToEnforce;

			m_expectedMsgBoxes = new List<ShowMsgBoxStatus>(expectedMsgBoxes);
			if (expectedOrigIcuLocales != null)
				m_expectedOrigIcuLocales = new List<string>(expectedOrigIcuLocales);
			m_resultsToEnforce = new List<DialogResult>(resultsToEnforce);
			try
			{
				btnModifyEthnologueInfo_Click(this, null);
				Assert.AreEqual(0, m_expectedMsgBoxes.Count);
				Assert.AreEqual(0, m_expectedOrigIcuLocales.Count);
				Assert.AreEqual(0, m_resultsToEnforce.Count);
			}
			finally
			{
				m_expectedMsgBoxes.Clear();
				m_resultsToEnforce.Clear();
				m_expectedOrigIcuLocales.Clear();

				m_selectedLanguageName = null;
				m_selectedEthnologueCode = null;
				m_ethnologueDlgResultToEnforce = DialogResult.None;
			}
		}

		/// <summary>
		/// make tests assume we're setup for merging by default.
		/// </summary>
		protected override bool IsSetupForMergingWss
		{
			get { return true; }
		}

		/// <summary>
		/// Check the expected state of MsgBox being encountered.
		/// </summary>
		internal DialogResult DoExpectedMsgBoxResult(ShowMsgBoxStatus encountered, string icuOrigLocale)
		{
			DialogResult result = DoExpectedMsgBoxResult(encountered);
			Assert.AreEqual(m_expectedOrigIcuLocales[0], icuOrigLocale);
			m_expectedOrigIcuLocales.RemoveAt(0);
			return result;
		}

		private DialogResult DoExpectedMsgBoxResult(ShowMsgBoxStatus encountered)
		{
			// we always expect message boxes.
			Assert.Less(0, m_expectedMsgBoxes.Count);
			Assert.AreEqual(m_expectedMsgBoxes[0], encountered);
			m_expectedMsgBoxes.RemoveAt(0);
			DialogResult result = m_resultsToEnforce[0];
			m_resultsToEnforce.RemoveAt(0);
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
				dlg.LangName = m_selectedLanguageName;
				dlg.EthCode = m_selectedEthnologueCode;
			}
			return DialogResult = m_ethnologueDlgResultToEnforce;
		}

		/// <summary>
		///
		/// </summary>
		internal enum ShowMsgBoxStatus
		{
			None,
			CheckUniqueWsCantOverwriteWsInDb,
			CheckUniqueWsCantCreateDuplicateWs,
			CheckIcuNamesCantChangeUserWs,
			CheckIcuNamesTooBadWsAlreadyInDb,
			CheckIcuNamesWsAlreadyInDb,
			CheckIcuNamesLocaleAlreadyInLanguages
		}

		/// <summary>
		///
		/// </summary>
		protected override void ShowMsgBoxCantOverwriteWsInDb()
		{
			DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckUniqueWsCantOverwriteWsInDb);
		}

		/// <summary>
		///
		/// </summary>
		protected override void ShowMsgBoxCantCreateDuplicateWs()
		{
			DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckUniqueWsCantCreateDuplicateWs);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="origIcuLocale"></param>
		protected override void ShowMsgCantChangeUserWs(string origIcuLocale)
		{
			DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckIcuNamesCantChangeUserWs, origIcuLocale);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="finalLangDef"></param>
		/// <param name="origIcuLocale"></param>
		/// <param name="strExisting"></param>
		protected override void ShowMsgTooBadWsAlreadyInDb(LanguageDefinition finalLangDef, string origIcuLocale, string strExisting)
		{
			DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckIcuNamesTooBadWsAlreadyInDb, origIcuLocale);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="finalLangDef"></param>
		/// <param name="origIcuLocale"></param>
		/// <param name="strExisting"></param>
		/// <param name="wsOld"></param>
		/// <returns></returns>
		protected override DialogResult ShowMsgWsAlreadyInDb(LanguageDefinition finalLangDef, string origIcuLocale, string strExisting, IWritingSystem wsOld)
		{
			return DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb, origIcuLocale);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="finalLangDef"></param>
		/// <returns></returns>
		protected override DialogResult ShowMsgLocaleAlreadyInLanguages(LanguageDefinition finalLangDef)
		{
			return DoExpectedMsgBoxResult(ShowMsgBoxStatus.CheckIcuNamesLocaleAlreadyInLanguages, finalLangDef.IcuLocaleOriginal);
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
			TabControlCancelEventArgs args = new TabControlCancelEventArgs(null, -1, false, TabControlAction.Deselecting);
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
			ContextMenuStrip cms = PopulateAddWsContextMenu();
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
		internal bool PressBtnRemove()
		{
			// Note: For some reason btnRemove.PerformClick() does not trigger the event.
			if (btnRemove.Enabled)
			{
				btnRemove_Click(this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		#endregion General Info

		#region General Tab
		internal void SetVariantName(string newVariantName)
		{
			m_regionVariantControl.VariantName = newVariantName;
			m_regionVariantControl.RegionVariantControl_Leave(null, EventArgs.Empty);
		}

		internal void SetScriptName(string newScriptName)
		{
			m_regionVariantControl.ScriptName = newScriptName;
			m_regionVariantControl.RegionVariantControl_Leave(null, EventArgs.Empty);
		}

		#endregion General Tab

		#region Dummy PuaCharacterDlg
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class DummyPUACharacterDlg : PUACharacterDlg
		{
			private static PUACharacter s_testPuaCharacter;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the pua character for testing.
			/// </summary>
			/// <value>The test pua character.</value>
			/// --------------------------------------------------------------------------------
			public static PUACharacter TestPuaCharacter
			{
				get { return s_testPuaCharacter; }
				set { s_testPuaCharacter = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Changes the textbox associated with the given unicode property
			/// changing a single character at a time as though the user were typing it.
			/// </summary>
			/// <param name="unicodePropertyIndex">The index of the Unicode property as listed in:
			/// http://www.unicode.org/Public/UNIDATA/UCD.html</param>
			/// <param name="text"></param>
			/// --------------------------------------------------------------------------------
			protected void EnterTextInTextBox(int unicodePropertyIndex, string text)
			{
				TextBox textBox;
				switch (unicodePropertyIndex)
				{
					case 0:
						textBox = m_txtCodepoint;
						break;
					case 1:
						textBox = m_txtName;
						break;
					case 5:
						textBox = m_txtDecomposition;
						break;
					case 6:
					case 7:
					case 8:
						textBox = m_txtNumericValue;
						break;
					case 12:
						textBox = m_txtUpperEquiv;
						break;
					case 13:
						textBox = m_txtLowerEquiv;
						break;
					case 14:
						textBox = m_txtTitleEquiv;
						break;
					default:
						// Don't attempt to continue if the associated Control isn't a TextBox.
						return;
				}
				// Don't allow the "test" user to edit disabled boxes.
				if (textBox.Enabled == false)
					return;

				textBox.Text = text;

				//foreach (char character in text)
				//{
				//    // TODO: figure how to move the selection to after the new
				//    // character without _TextChanged being called
				//    textBox.Text += character;
				//}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Changes the comboBox associated with the given unicode property
			/// </summary>
			/// <param name="unicodePropertyIndex">The index of the Unicode property as listed in:
			/// http://www.unicode.org/Public/UNIDATA/UCD.html</param>
			/// <param name="selectValue">The value to select</param>
			/// --------------------------------------------------------------------------------
			protected void SelectInComboBox(int unicodePropertyIndex, UcdProperty selectValue)
			{
				ComboBox comboBox;
				switch (unicodePropertyIndex)
				{
					case 2:
						comboBox = m_cbGeneralCategory;
						break;
					case 3:
						comboBox = m_cbCanonicalCombClass;
						break;
					case 4:
						comboBox = m_cbBidiClass;
						break;
					case 5:
						comboBox = m_cbCompatabilityDecomposition;
						break;
					case 6:
					case 7:
					case 8:
						comboBox = m_cbNumericType;
						break;
					default:
						// Don't attempt to continue if the associated Control isn't a TextBox.
						return;
				}
				// Don't allow the "test" user to edit disabled boxes.
				if (comboBox.Enabled == false)
					return;

				comboBox.SelectedItem = selectValue;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Calls the ShowDialog method (so that we can prevent showing the dialog in the tests)
			/// </summary>
			/// <returns></returns>
			/// ------------------------------------------------------------------------------------
			public override DialogResult CallShowDialog()
			{
				CheckDisposed();

				OnLoad(EventArgs.Empty);
				m_puaChar = s_testPuaCharacter;
				FillFormFromPUACharacter(true);

				// First set the codepoint to open up the valid characters
				EnterTextInTextBox(0, s_testPuaCharacter.CodePoint);
				// Enter the name
				EnterTextInTextBox(1, s_testPuaCharacter.Name);

				// general category first, so that the fields are in the same order
				SelectInComboBox(2, s_testPuaCharacter.GeneralCategory);

				// Set the decomposition
				SelectInComboBox(5, s_testPuaCharacter.CompatabilityDecomposition);
				EnterTextInTextBox(5, s_testPuaCharacter.Decomposition);
				// Numeric
				SelectInComboBox(8, s_testPuaCharacter.NumericType);
				EnterTextInTextBox(8, s_testPuaCharacter.NumericValue);
				// ULT equivelants
				EnterTextInTextBox(12, s_testPuaCharacter.Upper);
				EnterTextInTextBox(13, s_testPuaCharacter.Lower);
				EnterTextInTextBox(14, s_testPuaCharacter.Title);
				// Other values
				SelectInComboBox(3, s_testPuaCharacter.CanonicalCombiningClass);
				SelectInComboBox(4, s_testPuaCharacter.BidiClass);

				//TODO: fill in all the other modification stuff here

				m_btnOK_Click(null, null);

				return DialogResult.OK;
			}

		}
		#endregion // Dummy PuaCharacterDlg

		#region PUA Tab

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Simulates pressing the "Add" button.
		/// </summary>
		/// <param name="puaChar">The pua char.</param>
		/// --------------------------------------------------------------------------------
		public void PressBtnNewPUA(PUACharacter puaChar)
		{
			CheckDisposed();

			// Set the test pua character used to hold all the information to enter.
			// NOTE: This is never used directly,
			//       it is only used to hold values to be copied one at time
			DummyPUACharacterDlg.TestPuaCharacter = puaChar;
			// "Click" new
			m_btnNewPUA_Click(null, null);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Tests the "Modify" button.
		/// </summary>
		/// <param name="puaChar">The pua char.</param>
		/// --------------------------------------------------------------------------------
		public void PressBtnModifyPUA(PUACharacter puaChar)
		{
			CheckDisposed();

			// Set the test pua character used to hold all the information to enter.
			// NOTE: This is never used directly,
			//       it is only used to hold values to be copied one at time
			DummyPUACharacterDlg.TestPuaCharacter = puaChar;
			// Select puaChar in the list
			foreach (PuaListItem pualistItem in m_lstPUACharacters.Items)
			{
				if (pualistItem.PUAChar.CodePoint == puaChar.CodePoint)
				{
					m_lstPUACharacters.SelectedItem = pualistItem;
					break;
				}
			}
			// "click" modify
			m_btnModifyPUA_Click(null, null);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the PUA character as string
		/// </summary>
		/// <param name="addedPuaChar">The PUACharacter that was added.</param>
		/// <returns>
		/// </returns>
		/// --------------------------------------------------------------------------------
		public string GetPuaString(PUACharacter addedPuaChar)
		{
			CheckDisposed();

			foreach (PuaListItem pualistItem in m_lstPUACharacters.Items)
			{
				// Find the PUACharacter in the list.
				if (pualistItem.PUAChar.CodePoint == addedPuaChar.CodePoint)
				{
					// Return the value of the actual PUACharacter that is in the list
					return pualistItem.PUAChar.ToString();
				}
			}
			return "No PUACharacter found";
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Selectes the given PUACharacter via its codepoint from the list
		/// </summary>
		/// <param name="puaChar">The pua char.</param>
		/// --------------------------------------------------------------------------------
		public void SelectPuaCharacter(PUACharacter puaChar)
		{
			CheckDisposed();

			foreach (PuaListItem puaListItem in m_lstPUACharacters.Items)
			{
				// Find the PUACharacter in the list.
				if (puaListItem.PUAChar.CodePoint == puaChar.CodePoint)
				{
					// Return the value of the actual PUACharacter that is in the list
					m_lstPUACharacters.SelectedItem = puaListItem;
					break;
				}
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Tests to see if a PUACharacter with the given codepoint was added to m_lstPUACharacters
		/// and that it has the same properties.
		/// </summary>
		/// <param name="addedPuaChar">The PUACharacter that was added.</param>
		/// <returns>
		/// 	<c>true</c> if the character is found and it matches.
		/// </returns>
		/// --------------------------------------------------------------------------------
		public bool IsPuaAdded(PUACharacter addedPuaChar)
		{
			CheckDisposed();

			foreach (PuaListItem pualistItem in m_lstPUACharacters.Items)
			{
				// Find the PUACharacter in the list.
				if (pualistItem.PUAChar.CodePoint == addedPuaChar.CodePoint)
					return pualistItem.PUAChar.Equals(addedPuaChar);
			}
			return false;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Tests to see if a PUACharacter with the given codepoint was added to m_lstPUACharacters
		/// </summary>
		/// <param name="codepoint">The codepoint of the PUACharacter</param>
		/// <returns><c>true</c> if PUA codepoint was added, otherwise <c>false</c>.</returns>
		/// --------------------------------------------------------------------------------
		public bool IsPuaCodepointAdded(string codepoint)
		{
			CheckDisposed();

			foreach (PuaListItem pualistItem in m_lstPUACharacters.Items)
			{
				if (pualistItem.PUAChar.CodePoint == codepoint)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the PUA character dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override PUACharacterDlg CreatePUACharacterDlg()
		{
			return new DummyPUACharacterDlg();
		}

		#endregion PUA Tab

	}
	#endregion // Dummy WritingSystemPropertiesDlg

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TestFwProjPropertiesDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class WritingSystemPropertiesDialogTests: BaseTest
	{
		private InMemoryFdoCache m_inMemoryCache;
		private DummyWritingSystemPropertiesDialog m_dlg;
		private int m_hvoWsKalabaIpa = 0;
		Dictionary<string, string> m_icuLocaleToLangDefFile = new Dictionary<string,string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILangProject LangProj
		{
			get {return m_inMemoryCache.Cache.LangProject;}
		}

		#region Test Setup and Tear-Down
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_inMemoryCache = InMemoryFdoCache.CreateInMemoryFdoCache();
			m_inMemoryCache.InitializeLangProject();
			m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;
			m_inMemoryCache.InitializeWritingSystemEncodings();
			int hvoWsNew = SimulateInstallLanguage("xkal__IPA", "Kalaba (IPA)",
				"Arial", "Times New Roman", "Charis SIL");
			m_hvoWsKalabaIpa = hvoWsNew;
			CreateTempLanguageDefinitionFileFromWs(InMemoryFdoCache.s_wsHvos.XKal);
			CreateTempLanguageDefinitionFileFromWs(m_hvoWsKalabaIpa);
			CreateTempLanguageDefinitionFileFromNewWs("xwsd", "WSDialog",
				"Arial", "Courier New", "Charis SIL");
			CreateTempLanguageDefinitionFileFromNewWs("xwsd__IPA", "WSDialog (IPA)",
				"Doulos SIL", "Doulos SIL", "Doulos SIL");
			DummyWritingSystemPropertiesDialog.CopyLangDefToTempLanguageDir("xtst",
				Path.Combine(DirectoryFinder.FwSourceDirectory, @"FwCoreDlgs\FwCoreDlgsTests\xtst.xml"));
			m_dlg = new DummyWritingSystemPropertiesDialog(m_inMemoryCache.Cache);
		}

		private LanguageDefinition CreateTempLanguageDefinitionFileFromNewWs(string icuLocale, string enName,
			string defaultHeadingFont, string defaultFont, string defaultBodyFont)
		{
			int hvoWsNew = SimulateInstallLanguage(icuLocale, enName,
				defaultHeadingFont, defaultFont, defaultBodyFont);
			LanguageDefinition langDef = CreateTempLanguageDefinitionFileFromWs(hvoWsNew);
			return langDef;
		}

		private LanguageDefinition CreateTempLanguageDefinitionFileFromWs(int hvoWs)
		{
			IWritingSystem ws = m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(hvoWs);
			string shortAbbr = ws.get_Abbr(InMemoryFdoCache.s_wsHvos.En);
			if (String.IsNullOrEmpty(shortAbbr))
			{
				// set default abbr, if not set.
				string enName = ws.LanguageName; // ws.get_Name(InMemoryFdoCache.s_wsHvos.En);
				ws.set_Abbr(InMemoryFdoCache.s_wsHvos.En, WritingSystemPropertiesDialog.CreateDefaultLanguageNameAbbr(enName));
			}
			// this is part of setup, so pretend this is how we actually loaded it.
			ws.Dirty = false;
			LanguageDefinitionFactory ldf = new LanguageDefinitionFactory(ws);
			LanguageDefinition langDef = ldf.LanguageDefinition as LanguageDefinition;
			DummyWritingSystemPropertiesDialog.SerializeToTempfile(langDef);
			return langDef;
		}


		private int SimulateInstallLanguage(string icuLocale, string enName, string defaultHeadingFont, string defaultFont, string defaultBodyFont)
		{
			int hvoWsNew = m_inMemoryCache.SetupWs(icuLocale);
			LgWritingSystem ws = m_inMemoryCache.CreateWritingSystem(m_inMemoryCache.Cache,
				hvoWsNew, icuLocale, new int[] { InMemoryFdoCache.s_wsHvos.En },
				new string[] { enName }, defaultHeadingFont, defaultFont, defaultBodyFont);
			m_inMemoryCache.Cache.LanguageEncodings.Add(ws);
			IWritingSystem lgws = m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(hvoWsNew);
			return hvoWsNew;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Teardown()
		{
			if (m_dlg != null)
			{
				m_dlg.Dispose();
				m_dlg = null;
			}
			else
			{
				DummyWritingSystemPropertiesDialog.CleanupTempLangDefFiles();
			}
			m_inMemoryCache.Dispose();
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			wsf.Shutdown();
		}
		#endregion

		#region Helper Methods

		private void VerifyNewlyAddedLanguageDefns(string [] newExpectedIcuLocales)
		{
			List<LanguageDefinition> newLangDefs = m_dlg.NewlyAddedLanguageDefns();
			List<string> expectedIcuLocales = new List<string>(newExpectedIcuLocales);
			Assert.AreEqual(newExpectedIcuLocales.Length, newLangDefs.Count);
			List<string> actualIcuLocales = new List<string>();
			foreach (LanguageDefinition langDef in newLangDefs)
				actualIcuLocales.Add(langDef.WritingSystem.IcuLocale);
			foreach (string expectedIcuLocale in expectedIcuLocales)
				Assert.Contains(expectedIcuLocale, actualIcuLocales);
		}

		private void VerifyWsNames(int[] hvoWss, string[] wsNames, string[] icuLocales)
		{
			ILgWritingSystemFactory wsf = m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor;
			int i = 0;
			foreach (int hvoWs in hvoWss)
			{
				IWritingSystem wsEngine = wsf.get_EngineOrNull(hvoWs);
				Assert.IsNotNull(wsEngine);
				Assert.AreEqual(wsNames[i], wsEngine.get_Name(InMemoryFdoCache.s_wsHvos.En));
				Assert.AreEqual(icuLocales[i], wsEngine.IcuLocale);
				i++;
			}
		}

		private void VerifyWsNames(string[] wsNames, string[] icuLocales)
		{
			ILgWritingSystemFactory wsf = m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor;
			int i = 0;
			foreach (string icuLocale in icuLocales)
			{
				int hvoWs = wsf.GetWsFromStr(icuLocale);
				Assert.IsTrue(hvoWs > 0);
				IWritingSystem wsEngine = wsf.get_EngineOrNull(hvoWs);
				Assert.IsNotNull(wsEngine);
				Assert.AreEqual(wsNames[i], wsEngine.get_Name(InMemoryFdoCache.s_wsHvos.En));
				Assert.AreEqual(icuLocales[i], wsEngine.IcuLocale);
				i++;
			}
		}

		#endregion


		#region Tests
		// See comment on AnalysisWsListAdd
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WsListContent()
		{
			// Setup dialog to show Kalaba (xkal) related wss.
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)" });
			m_dlg.VerifyRelatedIcuLocale("xkal");
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba");
			// Select Kalaba (IPA) and verify dialog is setup for that one.
			m_dlg.WsList.SelectedItem = "Kalaba (IPA)";
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba (IPA)");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_LanguageNameChange()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			m_dlg.LanguageNameTextBox.Text = "Kalab";
			m_dlg.VerifyListBox(new string[] { "Kalab", "Kalab (IPA)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalab");
			m_dlg.PressOk();
			Assert.AreEqual(DialogResult.OK, m_dlg.DialogResult);
			Assert.AreEqual(true, m_dlg.IsChanged);
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalab", "Kalab (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_AddNewWs_OK()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// Verify Remove doesn't (yet) do anything for Wss already in the Database.
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)" });
			m_dlg.PressBtnRemove();
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)" });
			// Switch tabs, so we can test that Add New Ws will switch to General Tab.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyAddWsContextMenuItems(new string[] { "&Writing System for Kalaba..." });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("&Writing System for Kalaba...");
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new string[] {"Kalaba", "Kalaba (IPA)", "Kalaba"});
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba", 2);
			m_dlg.VerifyCurrentLangDefIsLoadedWithDefaults();
			// verify we automatically switched back to General Tab.
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// Verify Switching context is not OK (force user to make unique Ws)
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckUniqueWsCantOverwriteWsInDb,
				DialogResult.OK);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// make sure we can't select a different ethnologue code.
			m_dlg.SelectEthnologueCodeDlg("", "", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] {
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckUniqueWsCantOverwriteWsInDb},
				null,
				new DialogResult[] { DialogResult.OK });
			// Change Region or Variant info.
			m_dlg.SetVariantName("Phonetic");
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)", "Kalaba (Phonetic)" });
			// Now update the Ethnologue code, and cancel msg box to check we restored the expected newly added language defns.
			m_dlg.SelectEthnologueCodeDlg("WSDialog", "wsd", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] {
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb},
				new string[] { "xkal" },
				new DialogResult[] { DialogResult.Cancel});
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedLanguageDefns(new string[] { "xkal__X_ETIC" });
			// Now update the Ethnologue code, check we still have expected newly added language defns.
			m_dlg.SelectEthnologueCodeDlg("Kala", "", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] { }, new string[] { }, new DialogResult[] { });
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedLanguageDefns(new string[] { "xkal__X_ETIC" });
			// Now try adding a second/duplicate ws.
			m_dlg.PressBtnAdd("&Writing System for Kala...");
			m_dlg.VerifyListBox(new string[] { "Kala", "Kala (IPA)", "Kala (Phonetic)", "Kala" });
			m_dlg.VerifyLoadedForListBoxSelection("Kala", 3);
			m_dlg.SetVariantName("Phonetic");
			m_dlg.VerifyListBox(new string[] { "Kala", "Kala (IPA)", "Kala (Phonetic)", "Kala (Phonetic)"});
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckUniqueWsCantCreateDuplicateWs,
				DialogResult.OK);
			m_dlg.PressBtnRemove();
			m_dlg.VerifyListBox(new string[] { "Kala", "Kala (IPA)", "Kala (Phonetic)"});
			m_dlg.VerifyLoadedForListBoxSelection("Kala (Phonetic)");
			// Do OK
			m_dlg.PressOk();
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedLanguageDefns(new string[] { "xkal__X_ETIC" });
			// Verify we've actually created the new ws.
			VerifyWsNames(
				new string[] { "Kala", "Kala (IPA)", "Kala (Phonetic)"},
				new string[] { "xkal", "xkal__IPA", "xkal__X_ETIC" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_AddExistingWs_OK()
		{
			CreateTempLanguageDefinitionFileFromNewWs("xtst__IPA", "TestOnly (IPA)",
				"Doulos SIL", "Doulos SIL", "Doulos SIL");
			int hvoTstIpa = m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("xtst__IPA");
			m_dlg.CallShowDialog(hvoTstIpa);
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyAddWsContextMenuItems(new string[] { "TestOnly", "&Writing System for TestOnly..." });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("TestOnly");
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new string[] { "TestOnly (IPA)", "TestOnly" });
			m_dlg.VerifyLoadedForListBoxSelection("TestOnly");
			// verify we automatically switched back to General Tab.
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			// first, make sure we can remove the newly added writing system.
			m_dlg.PressBtnRemove();
			m_dlg.VerifyListBox(new string[] { "TestOnly (IPA)" });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("TestOnly");
			// Do OK
			m_dlg.PressOk();
			// Verify we've actually added the existing ws.
			VerifyWsNames(
				new string[] { "TestOnly (IPA)", "TestOnly" },
				new string[] { "xtst__IPA", "xtst"});
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_CopyWs_OK()
		{
			m_dlg.CallShowDialog(m_hvoWsKalabaIpa);
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba (IPA)");
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)" });
			// Switch tabs, so we can test that Add New Ws will switch to General Tab.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsFonts);
			// Click on Copy Button
			m_dlg.PressBtnCopy();
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)", "Kalaba (IPA)" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba (IPA)", 2);
			m_dlg.VerifyLanguageDefinitionsAreEqual(1, 2);
			// verify we automatically switched back to General Tab.
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// Verify Switching context is not OK (force user to make unique Ws)
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckUniqueWsCantOverwriteWsInDb,
				DialogResult.OK);
			m_dlg.VerifyTab(WritingSystemPropertiesDialog.kWsGeneral);
			// Change Region or Variant info.
			m_dlg.SetVariantName("Phonetic");
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)", "Kalaba (Phonetic)" });
			// Do OK
			m_dlg.PressOk();
			// Verify dialog indicates a list to add to current (vernacular) ws list
			VerifyNewlyAddedLanguageDefns(new string[] { "xkal__X_ETIC" });
			// Verify we've actually created the new ws.
			VerifyWsNames(
				new string[] { "Kalaba", "Kalaba (IPA)", "Kalaba (Phonetic)" },
				new string[] { "xkal", "xkal__IPA", "xkal__X_ETIC" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsIcuCode_Cancel()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// change to nonconflicting ethnologue code
			m_dlg.SelectEthnologueCodeDlg("Silly", "xxx", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] { },
				new string[] { },
				new DialogResult[] { });
			m_dlg.VerifyListBox(new string[] { "Silly", "Silly (IPA)" });
			m_dlg.VerifyRelatedIcuLocale("xxxx");
			m_dlg.VerifyLoadedForListBoxSelection("Silly");
			m_dlg.PressCancel();
			Assert.AreEqual(false, m_dlg.IsChanged);
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalaba", "Kalaba (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsIcuCode_Ok()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// change to nonconflicting ethnologue code
			m_dlg.SelectEthnologueCodeDlg("Silly", "xxx", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] { },
				new string[] { },
				new DialogResult[] { });
			m_dlg.VerifyListBox(new string[] { "Silly", "Silly (IPA)" });
			m_dlg.VerifyRelatedIcuLocale("xxxx");
			m_dlg.VerifyLoadedForListBoxSelection("Silly");
			m_dlg.PressOk();
			Assert.AreEqual(true, m_dlg.IsChanged);
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Silly", "Silly (IPA)" },
				new string[] { "xxxx", "xxxx__IPA" });
		}

		/// <summary>
		/// change ethnologue code to one conflicting with an existing icu code.
		/// Merge all original data into the existing code data.
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsIcuCode_MergeAll_Cancel()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// change ethnologue code to one conflicting with an existing icu code.
			// say we want to merge the first and second one.
			m_dlg.SelectEthnologueCodeDlg("WSDialog", "wsd", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] {
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb,
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb},
				new string[] { "xkal", "xkal__IPA" },
				new DialogResult[] {DialogResult.OK, DialogResult.OK });
			m_dlg.VerifyPendingMerges(new string[] { "xwsd", "xwsd__IPA" });
			m_dlg.PressCancel();
			Assert.AreEqual(false, m_dlg.IsChanged);
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalaba", "Kalaba (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsIcuCode_MergeAll_Ok()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// change ethnologue code to one conflicting with an existing icu code.
			// say we want to merge the first and second one.
			m_dlg.SelectEthnologueCodeDlg("WSDialog", "wsd", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] {
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb,
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb},
				new string[] { "xkal", "xkal__IPA" },
				new DialogResult[] { DialogResult.OK, DialogResult.OK });
			m_dlg.VerifyPendingMerges(new string[] { "xwsd", "xwsd__IPA" });
			m_dlg.PressOk();
			m_dlg.VerifyPendingMerges(new string[] {});
			// real merging currently involves a real db connection
			// for now just verify we haven't overwritten the original ws.
			Assert.AreEqual(false, m_dlg.IsChanged);
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalaba", "Kalaba (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
			// TODO: Merge tests should actually do a merge:
			//VerifyWsNames(
			//    new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
			//    new string[] { "WSDialog", "WSDialog (IPA)" },
			//    new string[] { "xwsd", "xwsd__IPA" });
		}

		/// <summary>
		/// change ethnologue code to one conflicting with an existing icu code.
		/// Deny merging all original data into existing data, so that
		/// the dialog forces user to change the code name to something else.
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsIcuCode_DontMergeOne()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// change ethnologue code to one conflicting with an existing icu code.
			// say we want to merge the first but not the second one.
			m_dlg.SelectEthnologueCodeDlg("WSDialog", "wsd", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[] {
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb,
					DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesWsAlreadyInDb},
				new string[] { "xkal", "xkal__IPA" },
				new DialogResult[] { DialogResult.OK, DialogResult.Cancel });
			m_dlg.VerifyPendingMerges(new string[] { });
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)" });
			m_dlg.PressOk();
			// TODO: figure out how to make IsChanged more accurrate.
			// Assert.AreEqual(false, m_dlg.IsChanged);
			// make sure we don't do the overwriting until the user presses OK
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalaba", "Kalaba (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
		}

		/// <summary>
		/// change ethnologue code to one conflicting with a language definition file
		/// loaded in another project.
		/// Prompt the user to load one into the project (abandoning any changes to current wss).
		/// </summary>
		[Test]
		[Ignore("LanguageDefinition.HasPendingOverwrite() and HasPendingMerge() are looking in the DistFiles/Language directory, rather than the Temp directory.")]
		public void General_EthnologueCodeChanged_ModifyWsIcuCode_OverwriteOriginalWsWithWsInstalledInAnotherDb_Yes()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// change ethnologue code to one conflicting with an existing icu code that's not yet
			// in our database, and indicate we want to load the existing lang def, and overwrite the orig ws.
			m_dlg.SelectEthnologueCodeDlg("TestOnly", "tst", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[]
					{DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesLocaleAlreadyInLanguages},
				new string[] { "xkal" },
				new DialogResult[] { DialogResult.Yes });
			// make sure we don't do the overwriting until the user presses OK
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalaba", "Kalaba (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
			m_dlg.VerifyOverwritesPending(new string[] { "xkal" });
			m_dlg.PressOk();
			m_dlg.VerifyOverwritesPending(new string[] { });
			Assert.AreEqual(true, m_dlg.IsChanged);
			// Verify that we've added the Ws to our Db.
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "TestOnly", "TestOnly (IPA)" },
				new string[] { "xtst", "xtst__IPA" });
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void General_EthnologueCodeChanged_ModifyWsIcuCode_OverwriteOriginalWsWithWsInstalledInAnotherDb_No()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			// change ethnologue code to one conflicting with an existing icu code that's not yet
			// in our database, and say we don't want to load the existing lang def and overwrite the existing ws.
			m_dlg.SelectEthnologueCodeDlg("TestOnly", "tst", "", DialogResult.OK,
				new DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus[]
					{DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckIcuNamesLocaleAlreadyInLanguages},
				new string[] { "xkal" },
				new DialogResult[] { DialogResult.No });
			// make sure we don't do any overwriting
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalaba", "Kalaba (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
			m_dlg.VerifyOverwritesPending(new string[] { });
			m_dlg.PressOk();
			m_dlg.VerifyOverwritesPending(new string[] { });
			//Assert.AreEqual(false, m_dlg.IsChanged);
			// Verify that we've added the Ws to our Db.
			VerifyWsNames(
				new int[] { InMemoryFdoCache.s_wsHvos.XKal, m_hvoWsKalabaIpa },
				new string[] { "Kalaba", "Kalaba (IPA)" },
				new string[] { "xkal", "xkal__IPA" });
		}


		/// <summary>
		///
		/// </summary>
		[Test]
		public void GeneralTab_ScriptChanged_Duplicate()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba");

			m_dlg.VerifyAddWsContextMenuItems(new string[] { "&Writing System for Kalaba..." });
			// Click on Add Button...selecting "Add New..." option.
			m_dlg.PressBtnAdd("&Writing System for Kalaba...");
			// Verify WsList has new item and it is selected
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)", "Kalaba" });
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba", 2);

			m_dlg.SetScriptName("Arabic");
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)", "Kalaba (Arabic)"});
			//Verify that the Script, Region and Variant abbreviations are correct.
			m_dlg.VerifyFullIcuLocale("xkal_Arab");
			m_dlg.PressBtnCopy();
			m_dlg.VerifyListBox(new string[] { "Kalaba", "Kalaba (IPA)", "Kalaba (Arabic)", "Kalaba (Arabic)" });

			// expect msgbox error.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckUniqueWsCantCreateDuplicateWs,
				DialogResult.OK);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GeneralTab_VariantNameChanged_Duplicate()
		{
			m_dlg.CallShowDialog(InMemoryFdoCache.s_wsHvos.XKal);
			m_dlg.VerifyLoadedForListBoxSelection("Kalaba");
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsGeneral);
			m_dlg.SetVariantName("IPA");
			m_dlg.VerifyListBox(new string[] { "Kalaba (IPA)", "Kalaba (IPA)" });
			// expect msgbox error.
			m_dlg.SwitchTab(WritingSystemPropertiesDialog.kWsFonts,
				DummyWritingSystemPropertiesDialog.ShowMsgBoxStatus.CheckUniqueWsCantOverwriteWsInDb,
				DialogResult.OK);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		[Ignore("TODO")]
		public void GeneralTab_RegionVariantChanged()
		{
			//
		}

		#endregion
	}
}
