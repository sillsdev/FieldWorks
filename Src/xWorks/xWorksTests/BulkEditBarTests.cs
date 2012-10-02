// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion

using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;

using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
//using NMock;
//using NMock.Constraints;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks
{
	public class BulkEditBarTestsBase : InDatabaseFdoTestBase
	{
		/// <summary>
		/// m_window is needed for processing xcore messages when simulating user events.
		/// </summary>
		protected MockFwXWindow m_window = null;
		protected Mediator m_mediator = null;
		protected BulkEditBarForTests m_bulkEditBar = null;
		protected BrowseViewerForTests m_bv = null;

		#region Setup and Teardown

		private void SetupBulkEditBarAndBrowseViewer()
		{
			m_bulkEditBar = m_window.FindControl("BulkEditBar") as BulkEditBarForTests;
			m_bv = m_bulkEditBar.Parent as BrowseViewerForTests;
		}

		private void ControlAssemblyReplacements()
		{
			ControlAssemblyReplacement replacement = new ControlAssemblyReplacement();
			replacement.m_toolName = "bulkEditEntriesOrSenses";
			replacement.m_controlName = "EntryOrSenseBulkEdit";
			replacement.m_targetAssembly = "xWorks.dll";
			replacement.m_targetControlClass = "SIL.FieldWorks.XWorks.RecordBrowseView";
			replacement.m_newAssembly = "xWorksTests.dll";
			replacement.m_newControlClass = "SIL.FieldWorks.XWorks.BulkEditBarTestsBase+RecordBrowseViewForTests";
			m_window.AddReplacement(replacement);
		}

		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;
			try
			{
				base.Dispose(disposing);
			}
			finally
			{
				FdoCache.RestoreTestLangProj();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			InitializeNewWindow();
			m_window.ActivateTool("bulkEditEntriesOrSenses");
			SetupBulkEditBarAndBrowseViewer();
			new CreateModifyTimeManager(m_fdoCache);
		}

		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			// delete property table settings.
			m_window.PropertyTable.RemoveLocalAndGlobalSettings();

			if (m_window != null && !m_window.IsDisposed)
				m_window.Dispose();
			m_bulkEditBar = null;
			m_bv = null;
			m_window = null;
			m_mediator = null;
			base.Exit();
			// overkill, but no way to directly clear individual properties.
			Cache.ClearAllData();
			if (m_fdoCache.CreateModifyManager != null)
			{
				m_fdoCache.CreateModifyManager.Dispose();
				m_fdoCache.CreateModifyManager = null;
			}
		}

		protected virtual void InitializeNewWindow()
		{
			m_window = new MockFwXWindow();
			m_window.Init(Cache);
			// delete property table settings.
			m_window.PropertyTable.RemoveLocalAndGlobalSettings();
			ReinitializeWindow();
			ControlAssemblyReplacements();
			m_window.LoadUI(DirectoryFinder.GetFWCodeFile(@"Language Explorer\Configuration\Main.xml"));
		}

		private void ReinitializeWindow()
		{
			m_mediator = m_window.Mediator;
			List<IVwVirtualHandler> commonVhs = BaseVirtualHandler.InstallVirtuals(@"Language Explorer\Configuration\Main.xml",
				new string[] { "SIL.FieldWorks.FDO." }, m_fdoCache, true, false);
			InstallVirtuals(@"Language Explorer\Configuration\Lexicon\areaConfiguration.xml",
				new string[] { "SIL.FieldWorks.FDO." });
			m_installedVirtualHandlers.AddRange(commonVhs);
			m_window.InstalledVirtualHandlers = m_installedVirtualHandlers;
			m_window.ProcessPendingItems();
		}

		#endregion Setup and Teardown

		protected void MasterRefresh()
		{
			m_window.OnMasterRefresh(null);
			ReinitializeWindow();
			SetupBulkEditBarAndBrowseViewer();
		}

		protected internal class BulkEditBarForTests : BulkEditBar
		{
			/// <summary>
			/// m_window is needed for processing xcore messages when simulating user events.
			/// </summary>
			MockFwXWindow m_wnd;
			internal BulkEditBarForTests(BrowseViewer bv, XmlNode spec, Mediator mediator, FdoCache cache)
				: base(bv, spec, mediator, cache)
			{
				Form mainWindow = (Form)mediator.PropertyTable.GetValue("window") as Form;
				m_wnd = mainWindow as MockFwXWindow;
			}

			internal void SwitchTab(string tabName)
			{
				int tabIndex = (int)Enum.Parse(typeof(BulkEditBar.BulkEditBarTabs), tabName);
				m_operationsTabControl.SelectedIndex = tabIndex;
				m_wnd.ProcessPendingItems();
			}

			/// <summary>
			///
			/// </summary>
			internal int SelectedTab
			{
				get { return m_operationsTabControl.SelectedIndex; }
			}

			/// <summary>
			///
			/// </summary>
			internal FieldComboItem SelectedTargetFieldItem
			{
				get { return m_currentTargetCombo.SelectedItem as FieldComboItem; }
			}

			internal FieldComboItem SetTargetField(string label)
			{
				m_currentTargetCombo.Text = label;
				if (m_currentTargetCombo.Text != label)
					throw new ApplicationException(String.Format("Couldn't change to target field {0}, need to ShowColumn()", label));
				// trigger event explictly, since tests don't do it reliably.
				m_wnd.ProcessPendingItems();
				return SelectedTargetFieldItem;
			}

			internal List<FieldComboItem> GetTargetFields()
			{
				List<FieldComboItem> items = new List<FieldComboItem>();
				foreach (FieldComboItem item in m_currentTargetCombo.Items)
					items.Add(item);
				return items;
			}

			internal Control GetTabControlChild(string controlName)
			{
				Control[] matches = m_operationsTabControl.SelectedTab.Controls.Find(controlName, true);
				if (matches != null && matches.Length > 0)
					return matches[0];
				return null;
			}

			internal IBulkEditSpecControl CurrentBulkEditSpecControl
			{
				get { return m_beItems[m_itemIndex].BulkEditControl; }
			}

			/// <summary>
			/// See comments on PersistSettings.
			/// </summary>
			bool m_fSaveSettings = false;
			protected internal override void SaveSettings()
			{
				if (PersistSettings)
					base.SaveSettings();
			}

			/// <summary>
			/// For some reason, my (EricP's) version of ReSharper (4.1.933.3)
			/// takes a LONG time to run tests when trying to XmlSerialize BulkEditBarTabPage settings.
			/// In any case, it's probably reasonable to not care about Persisting Settings except
			/// for tests that switch between BulkEditBar tabs or do a MasterRefresh().
			///
			/// Here's where it takes too long:
			/// JetBrains.ReSharper.TaskRunnerFramework.dll!JetBrains.ReSharper.TaskRunnerFramework.AssemblyLoader.ResolveAssemblyFileByName(string name = "XMLViews.XmlSerializers, Version=1.0.3443.22565, Culture=neutral, PublicKeyToken=null", bool isFullName = true) Line 173 + 0x8 bytes	C#
			/// JetBrains.ReSharper.TaskRunnerFramework.dll!JetBrains.ReSharper.TaskRunnerFramework.AssemblyLoader.ResolveAssembly(object sender = {System.AppDomain}, System.ResolveEventArgs args = {System.ResolveEventArgs}) Line 107 + 0xd bytes	C#
			/// XMLUtils.DLL!SIL.Utils.XmlUtils.SerializeObjectToXmlString(object objToSerialize = {SIL.FieldWorks.Common.Controls.BulkEditBar.ListChoiceTabPageSettings}) Line 592 + 0x26 bytes	C#
			/// XMLViews.DLL!SIL.FieldWorks.Common.Controls.BulkEditBar.BulkEditTabPageSettings.SerializeSettings() Line 2566 + 0x8 bytes	C#
			/// </summary>
			internal bool PersistSettings
			{
				get { return m_fSaveSettings;  }
				set { m_fSaveSettings = value; }
			}

			internal void ClickPreview()
			{
				m_previewButton_Click(null, EventArgs.Empty);
				m_wnd.ProcessPendingItems();
			}

			internal void ClickApply()
			{
				m_ApplyButton_Click(null, EventArgs.Empty);
				m_wnd.ProcessPendingItems();
			}
		}

		protected internal class BrowseViewerForTests : BrowseViewer
		{
			MockFwXWindow m_wnd = null;

			internal BrowseViewerForTests(XmlNode nodeSpec, int hvoRoot, int fakeFlid, FdoCache cache, Mediator mediator, ISortItemProvider sortItemProvider)
				: base(nodeSpec, hvoRoot, fakeFlid, cache, mediator, sortItemProvider)
			{
				Form mainWindow = (Form)mediator.PropertyTable.GetValue("window") as Form;
				m_wnd = mainWindow as MockFwXWindow;
				m_xbv.MakeRoot(); // needed to process OnRecordNavigation
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="bv"></param>
			/// <param name="spec"></param>
			/// <param name="mediator"></param>
			/// <param name="cache"></param>
			/// <returns></returns>
			protected override BulkEditBar CreateBulkEditBar(BrowseViewer bv, XmlNode spec, Mediator mediator, FdoCache cache)
			{
				return new BulkEditBarForTests(bv, spec, mediator, cache);
			}

			private AnywhereMatcher CreateAnywhereMatcher(string pattern, int ws)
			{
				IVwPattern ivwpattern = VwPatternClass.Create();
				ivwpattern.Pattern = StringUtils.MakeTss(pattern, ws);
				ivwpattern.MatchCase = true;
				ivwpattern.MatchDiacritics = true;

				// Default values because we don't set these here
				ivwpattern.MatchOldWritingSystem = false;
				ivwpattern.MatchWholeWord = false;
				ivwpattern.UseRegularExpressions = false;
				ivwpattern.IcuLocale = Cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
				return new AnywhereMatcher(ivwpattern);
			}

			internal FilterSortItem SetFilter(string columnName, string filterType, string query)
			{
				// get ColumnInfo for specified column
				FilterSortItem fsiTarget = FindColumnInfo(columnName);
				int index = fsiTarget.Combo.FindStringExact(StringUtils.MakeTss(filterType, Cache.DefaultUserWs));
				if (index < 0)
					return null;

				FilterComboItem fci = fsiTarget.Combo.Items[index] as FilterComboItem;
				if (filterType.EndsWith("..."))
				{
					// these are dialogs
					if (filterType == "Filter for...")
					{
						int ws = (fci as FindComboItem).Ws;
						(fci as FindComboItem).Matcher = CreateAnywhereMatcher(query, ws);
						fci.InvokeWithInstalledMatcher();
					}
					else if (filterType == "Choose...")
					{
						// by default match on "Any"
						(fci as ListChoiceComboItem).InvokeWithColumnSpecFilter(ListMatchOptions.Any, new List<string>(new string[] { query }));
					}
				}
				else
				{
					// invoke simple filters.
					fci.Invoke();
				}
				m_wnd.ProcessPendingItems();
				return fsiTarget;
			}

			private FilterSortItem FindColumnInfo(string columnName)
			{
				FilterSortItem fsiTarget = null;
				foreach (FilterSortItem fsi in m_filterBar.ColumnInfo)
				{
					if (fsi.Spec.Attributes["label"].Value == columnName ||
						fsi.Spec.Attributes["headerlabel"] != null &&
						fsi.Spec.Attributes["headerlabel"].Value == columnName)
					{
						fsiTarget = fsi;
						break;
					}
				}
				return fsiTarget;
			}

			internal FilterSortItem SetSort(string columnName)
			{
				FilterSortItem fsiTarget = FindColumnInfo(columnName);
				List<FilterSortItem> fsiList = new List<FilterSortItem>(m_filterBar.ColumnInfo);
				int indexColSpec = fsiList.IndexOf(fsiTarget);
				int indexOfColumnHeader = indexColSpec + ColumnIndexOffset();
				m_lvHeader_ColumnClick(this, new ColumnClickEventArgs(indexOfColumnHeader));
				m_wnd.ProcessPendingItems();
				return fsiTarget;
			}

			internal void ShowColumn(string layoutName)
			{
				// get column matching the given layoutName
				List<XmlNode> possibleColumns = m_xbv.Vc.ComputePossibleColumns();
				XmlNode colSpec = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "layout", layoutName, null);
				if (this.IsColumnHidden(colSpec))
				{
					this.AppendColumn(colSpec);
					UpdateColumnList();
				}
			}

			internal void OnUncheckAll()
			{
				base.OnUncheckAll(null, EventArgs.Empty);
			}

			internal void UnselectItem(int hvo)
			{
				SetItemCheckedState(hvo, false, false);
			}

			internal void SelectItem(int hvo)
			{
				SetItemCheckedState(hvo, true, false);
			}

			internal void UncheckItems(IEnumerable<int> items)
			{
				foreach (int hvo in items)
					UnselectItem(hvo);
			}

			internal IList<int> UncheckedItems()
			{
				IList<int> uncheckedItems = new List<int>();
				foreach (int hvoItem in AllItems)
				{
					if (!IsItemChecked(hvoItem))
						uncheckedItems.Add(hvoItem);
				}

				return uncheckedItems;
			}
		}

		protected class RecordBrowseViewForTests : RecordBrowseView
		{
			protected override BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, FdoCache cache, Mediator mediator,
				ISortItemProvider sortItemProvider)
			{
				return new BrowseViewerForTests(nodeSpec, hvoRoot, fakeFlid, cache, mediator, sortItemProvider);
			}
		}

	}

	[TestFixture]
	public class BulkEditBarTests : BulkEditBarTestsBase
	{
		#region BulkEditEntries tests
		[Test]
		public void ChoiceFilters()
		{
			m_bulkEditBar.PersistSettings = true;
			//MessageBox.Show("Debug ListChoiceTargetSelection");
			m_bulkEditBar.SwitchTab("ListChoice");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			FilterSortItem fsFilter = m_bv.SetFilter("Lexeme Form", "Filter for...", "underlying form");
			// next make a chooser filter on "Entry Type" column
			fsFilter = m_bv.SetFilter("Morph Type", "Choose...", "root");
			m_bv.SetSort("Lexeme Form");
			// Make sure our
			Assert.AreEqual(1, m_bv.AllItems.Count);

			// now switch list items to senses, and see if our Main Entry filter still has results.
			// TargetField == Sense (e.g. "Grammatical Category")
			m_bulkEditBar.SetTargetField("Grammatical Category");
			Assert.AreEqual("Grammatical Category", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			// make sure current record is a Sense
			// Make sure filter is still applied on right column during the transition.
			// verify there are 4 rows
			Assert.AreEqual(4, m_bv.AllItems.Count);

			// make sure we can refresh and still have the filter set.
			MasterRefresh();
			Assert.AreEqual("Grammatical Category", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			Assert.AreEqual(4, m_bv.AllItems.Count);
		}

		[Test]
		public void ChooseLabel()
		{
			// LT-9940 Bulk Edit List Choice tab, the "Choose..." button loses its label.
			m_bulkEditBar.PersistSettings = true;
			m_bulkEditBar.SwitchTab("ListChoice");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			m_bv.ShowColumn("VariantEntryTypesBrowse");
			FilterSortItem fsFilter = m_bv.SetFilter("Variant Types", "Non-blanks", "");
			m_bv.SetSort("Lexeme Form");
			Assert.AreEqual(2, m_bv.AllItems.Count);

			// TargetField == Complex or Variant Entry References (e.g. "Variant Types")
			m_bulkEditBar.SetTargetField("Variant Types");
			Assert.AreEqual("Variant Types", m_bulkEditBar.SelectedTargetFieldItem.ToString());

			// verify there are 2 rows
			Assert.AreEqual(2, m_bv.AllItems.Count);
			Assert.AreEqual("Choose...", m_bulkEditBar.CurrentBulkEditSpecControl.Control.Text);

			// make sure we can refresh and still have the filter set.
			MasterRefresh();
			Assert.AreEqual("Variant Types", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			Assert.AreEqual(2, m_bv.AllItems.Count);
			Assert.AreEqual("Choose...", m_bulkEditBar.CurrentBulkEditSpecControl.Control.Text);
		}

		[Test]
		public void ListChoiceTargetSelection()
		{
			//MessageBox.Show("Debug ListChoiceTargetSelection");
			m_bulkEditBar.SwitchTab("ListChoice");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			FilterSortItem fsFilter = m_bv.SetFilter("Lexeme Form", "Filter for...", "underlying form"); // 'underlying form'
			m_bv.SetSort("Lexeme Form");
			Assert.AreEqual(1, m_bv.AllItems.Count);
			// Make sure we have the expected target fields
			List<FieldComboItem> targetFields = m_bulkEditBar.GetTargetFields();
			Assert.AreEqual(2, targetFields.Count);
			Assert.AreEqual("Morph Type", targetFields[0].ToString());
			Assert.AreEqual("Grammatical Category", targetFields[1].ToString());

			// TargetField == Entry (e.g. "Morph Type")
			m_bulkEditBar.SetTargetField("Morph Type");
			Assert.AreEqual("Morph Type", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			// make sure current record is an Entry
			int hvoOfCurrentEntry = m_bv.AllItems[m_bv.SelectedIndex];
			Assert.AreEqual(LexEntry.kclsidLexEntry, Cache.GetClassOfObject(hvoOfCurrentEntry));
			// verify there is still only 1 row.
			Assert.AreEqual(1, m_bv.AllItems.Count);
			// Set sorter on a sense field and make sure unchecking one entry unchecks them all
			m_bv.SetSort("Grammatical Category");
			int numOfEntryRows = m_bv.AllItems.Count;
			// we expect to have more than one Entry rows when sorted on a sense field
			Assert.Less(1, numOfEntryRows);
			Assert.AreEqual(numOfEntryRows, m_bv.CheckedItems.Count);	// all checked.
			// check current item, should check all rows.
			m_bv.SetCheckedItems(new List<int>());	// uncheck all rows.
			Assert.AreEqual(0, m_bv.CheckedItems.Count);
			m_bv.SetCheckedItems(new List<int>(new int[] { hvoOfCurrentEntry }));
			Assert.AreEqual(numOfEntryRows, m_bv.CheckedItems.Count);

			// TargetField == Sense (e.g. "Grammatical Category")
			m_bulkEditBar.SetTargetField("Grammatical Category");
			Assert.AreEqual("Grammatical Category", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			// make sure current record is a Sense
			int hvoOfCurrentSense = m_bv.AllItems[m_bv.SelectedIndex];
			Assert.AreEqual(LexSense.kclsidLexSense, Cache.GetClassOfObject(hvoOfCurrentSense));
			// Make sure filter is still applied on right column during the transition.
			// verify there are 4 rows
			Assert.AreEqual(4, m_bv.AllItems.Count);

			// make sure checking only one sense should only check one row.
			m_bv.SetCheckedItems(new List<int>());	// uncheck all rows.
			m_bv.SetCheckedItems(new List<int>(new int[] { hvoOfCurrentSense }));
			Assert.AreEqual(1, m_bv.CheckedItems.Count);

			// take off the filter and make sure switching between Senses/Entries maintains a selection
			// in the ownership tree.
			m_bv.SetFilter("Lexeme Form", "Show All", null);
			hvoOfCurrentSense = m_bv.AllItems[m_bv.SelectedIndex];
			// now switch back to Entry level
			m_bulkEditBar.SetTargetField("Morph Type");
			hvoOfCurrentEntry = m_bv.AllItems[m_bv.SelectedIndex];
			Assert.AreEqual(LexEntry.kclsidLexEntry, Cache.GetClassOfObject(hvoOfCurrentEntry));
			// make sure this entry owns the Sense we were on.
			Assert.AreEqual(hvoOfCurrentEntry, Cache.GetOwnerOfObjectOfClass(hvoOfCurrentSense, LexEntry.kclsidLexEntry));
		}

		[Test]
		public void BulkCopyTargetSelection()
		{
			m_bulkEditBar.SwitchTab("BulkCopy");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			FilterSortItem fsFilter = m_bv.SetFilter("Lexeme Form", "Filter for...", "underlying form"); // 'underlying form'
			m_bv.SetSort("Lexeme Form");
			Assert.AreEqual(1, m_bv.AllItems.Count);
			// Make sure we have the expected target fields
			List<FieldComboItem> targetFields = m_bulkEditBar.GetTargetFields();
			Assert.AreEqual(4, targetFields.Count);
			Assert.AreEqual("Lexeme Form", targetFields[0].ToString());
			Assert.AreEqual("Citation Form", targetFields[1].ToString());
			Assert.AreEqual("Glosses", targetFields[2].ToString());
			Assert.AreEqual("Definition", targetFields[3].ToString());

			// TargetField == Entry
			m_bulkEditBar.SetTargetField("Citation Form");
			// make sure current record is an Entry
			int hvoOfCurrentEntry = m_bv.AllItems[m_bv.SelectedIndex];
			Assert.AreEqual(LexEntry.kclsidLexEntry, Cache.GetClassOfObject(hvoOfCurrentEntry));
			// verify there is still only 1 row.
			Assert.AreEqual(1, m_bv.AllItems.Count);

			// TargetField == Sense
			m_bulkEditBar.SetTargetField("Glosses");
			// make sure current record is a Sense
			int hvoOfCurrentSense = m_bv.AllItems[m_bv.SelectedIndex];
			Assert.AreEqual(LexSense.kclsidLexSense, Cache.GetClassOfObject(hvoOfCurrentSense));
			// Make sure filter is still applied on right column during the transition.
			// verify there are 4 rows
			Assert.AreEqual(4, m_bv.AllItems.Count);
		}

		[Test]
		public void DeleteTargetSelection()
		{
			m_bulkEditBar.SwitchTab("Delete");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			FilterSortItem fsFilter = m_bv.SetFilter("Lexeme Form", "Filter for...", "underlying form"); // 'underlying form'
			m_bv.SetSort("Lexeme Form");
			Assert.AreEqual(1, m_bv.AllItems.Count);
			// Make sure we have the expected target fields
			List<FieldComboItem> targetFields = m_bulkEditBar.GetTargetFields();
			Assert.AreEqual(7, targetFields.Count);
			Assert.AreEqual("Lexeme Form", targetFields[0].ToString());
			Assert.AreEqual("Citation Form", targetFields[1].ToString());
			Assert.AreEqual("Glosses", targetFields[2].ToString());
			Assert.AreEqual("Definition", targetFields[3].ToString());
			Assert.AreEqual("Grammatical Category", targetFields[4].ToString());
			Assert.AreEqual("Entries (Rows)", targetFields[5].ToString());
			Assert.AreEqual("Senses (Rows)", targetFields[6].ToString());

			// TargetField == Sense
			m_bulkEditBar.SetTargetField("Senses (Rows)");
			// make sure current record is a Sense
			int hvoOfCurrentSense = m_bv.AllItems[m_bv.SelectedIndex];
			Assert.AreEqual(LexSense.kclsidLexSense, Cache.GetClassOfObject(hvoOfCurrentSense));
			// Make sure filter is still applied on right column during the transition.
			// verify there are 4 rows
			Assert.AreEqual(4, m_bv.AllItems.Count);

			// TargetField == Entry
			m_bulkEditBar.SetTargetField("Entries (Rows)");
			// make sure current record is an Entry
			int hvoOfCurrentEntry = m_bv.AllItems[m_bv.SelectedIndex];
			Assert.AreEqual(LexEntry.kclsidLexEntry, Cache.GetClassOfObject(hvoOfCurrentEntry));
			// verify there is still only 1 row.
			Assert.AreEqual(1, m_bv.AllItems.Count);

			m_bv.ShowColumn("VariantEntryTypesBrowse");
			targetFields = m_bulkEditBar.GetTargetFields();
			Assert.AreEqual(9, targetFields.Count);
			Assert.AreEqual("Variant Types", targetFields[5].ToString());
			Assert.AreEqual("Complex or Variant Entry References (Rows)", targetFields[8].ToString());
		}

		/// <summary>
		/// (LT8958) List choice: Locations
		/// </summary>
		[Test]
		public void Pronunciations_ListChoice_Locations()
		{
			m_bulkEditBar.PersistSettings = true;
			// setup data.
			ILexPronunciation firstPronunciation;
			ILexEntry firstEntryWithPronunciation;
			ILexEntry firstEntryWithoutPronunciation;
			List<ILexEntry> entriesWithoutPronunciations;
			List<ILexPronunciation> pronunciations;
			SetupPronunciationData(out firstPronunciation,
				out firstEntryWithPronunciation,
				out firstEntryWithoutPronunciation,
				out entriesWithoutPronunciations,
				out pronunciations);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			// first select an entry with a pronunciation, and see if we move to that entry's pronunciation
			// when we switch to pronunciations list.
			clerk.JumpToRecord(firstEntryWithPronunciation.Hvo);
			m_window.ProcessPendingItems();
			Assert.AreEqual(firstEntryWithPronunciation.Hvo, clerk.CurrentObject.Hvo);
			// make sure we're not on the first index, since when we switch to pronunciations,
			// we want to make sure there is logic in place for keeping the index on a child pronunciation of this entry.
			Assert.Less(0, clerk.CurrentIndex);

			m_bulkEditBar.SwitchTab("ListChoice");
			int cOriginal = m_bv.ColumnSpecs.Count;
			// add column for Pronunciation Location
			m_bv.ShowColumn("Location");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 1, m_bv.ColumnSpecs.Count);
			m_bulkEditBar.SetTargetField("Locations");
			Assert.AreEqual("Locations", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			// check number of options and first is "jungle" (or Empty?)
			FwComboBox listChoiceControl = m_bulkEditBar.GetTabControlChild("m_listChoiceControl") as FwComboBox;
			Assert.IsNotNull(listChoiceControl);
			// expect to have some options.
			Assert.Less(2, listChoiceControl.Items.Count);
			// expect the first option to be of class CmLocation
			HvoTssComboItem item = listChoiceControl.Items[0] as HvoTssComboItem;
			Assert.AreEqual(CmLocation.kclsidCmLocation, Cache.GetClassOfObject(item.Hvo));
			// check browse view class changed to LexPronunciation
			Assert.AreEqual(LexPronunciation.kclsidLexPronunciation, m_bv.ListItemsClass);
			// check that clerk list has also changed.
			Assert.AreEqual(LexPronunciation.kclsidLexPronunciation, m_bv.SortItemProvider.ListItemsClass);
			// make sure the list size includes all pronunciations, and all entries that don't have pronunciations.
			Assert.AreEqual(clerk.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);

			// make sure we're on the pronunciation of the entry we changed from
			Assert.AreEqual(firstPronunciation.Hvo, clerk.CurrentObject.Hvo);
			// change the first pronunciation's (non-existing) location to something else
			Assert.AreEqual(0, firstPronunciation.LocationRAHvo);
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstPronunciation.Hvo }));
			// set list choice to the first location (eg. 'jungle')
			listChoiceControl.SelectedItem = item;

			int cPronunciations = firstEntryWithPronunciation.PronunciationsOS.Count;
			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			// make sure we changed the list option and didn't add another separate pronunciation.
			Assert.AreEqual(item.Hvo, firstPronunciation.LocationRAHvo);
			Assert.AreEqual(cPronunciations, firstEntryWithPronunciation.PronunciationsOS.Count);
			Assert.AreEqual(clerk.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);

			// now create a new pronunciation on an entry that does not have one.
			cPronunciations = firstEntryWithoutPronunciation.PronunciationsOS.Count;
			Assert.AreEqual(0, cPronunciations);
			clerk.JumpToRecord(firstEntryWithoutPronunciation.Hvo);
			m_window.ProcessPendingItems();
			Assert.AreEqual(firstEntryWithoutPronunciation.Hvo, clerk.CurrentObject.Hvo);
			int currentIndex = clerk.CurrentIndex;
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstEntryWithoutPronunciation.Hvo }));

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();

			// check that current index has remained the same.
			Assert.AreEqual(currentIndex, clerk.CurrentIndex);
			// but current object (entry) now has a Pronunciation
			Assert.Less(0, firstEntryWithoutPronunciation.PronunciationsOS.Count);
			ILexPronunciation newPronunciation = firstEntryWithoutPronunciation.PronunciationsOS[0];
			// make sure we gave the new pronunciation the expected location.
			Assert.AreEqual(item.Hvo, newPronunciation.LocationRAHvo);

			// now change the location to something else, and make sure we didn't create another pronunciation.
			HvoTssComboItem item2 = listChoiceControl.Items[1] as HvoTssComboItem;
			listChoiceControl.SelectedItem = item2;
			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			// make sure we gave the new pronunciation the expected location.
			Assert.AreEqual(item2.Hvo, newPronunciation.LocationRAHvo);
			Assert.AreEqual(clerk.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);

			// refresh list, and make sure the clerk now has the pronunciation, not the entry.
			this.MasterRefresh();
			clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			Assert.AreEqual(newPronunciation.Hvo, clerk.CurrentObject.Hvo);
			// also make sure the total count of the list has not changed.
			// we only converted an entry (ghost) to pronunciation.
			Assert.AreEqual(clerk.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);
		}

		private void SetupPronunciationData(out ILexPronunciation firstPronunciation, out ILexEntry firstEntryWithPronunciation, out ILexEntry firstEntryWithoutPronunciation, out List<ILexEntry> entriesWithoutPronunciations, out List<ILexPronunciation> pronunciations)
		{
			firstPronunciation = null;
			firstEntryWithPronunciation = null;
			firstEntryWithoutPronunciation = null;
			entriesWithoutPronunciations = new List<ILexEntry>();
			pronunciations = new List<ILexPronunciation>();
			// find an entry with pronunciations.
			foreach (ILexEntry entry in Cache.LangProject.LexDbOA.EntriesOC)
			{
				if (entry.PronunciationsOS.Count > 0)
				{
					pronunciations.AddRange(entry.PronunciationsOS);
					if (firstPronunciation == null)
					{
						firstEntryWithPronunciation = entry;
						firstPronunciation = entry.PronunciationsOS[0];
						pronunciations.Add(entry.PronunciationsOS.Append(new LexPronunciation()));
					}
				}
				else
				{
					entriesWithoutPronunciations.Add(entry);
					if (firstEntryWithoutPronunciation == null)
						firstEntryWithoutPronunciation = entry;
				}
			}
		}

		/// <summary>
		/// (LT8958) Bulk Copy/Click Copy/Bulk Replace/Process: Pronunciation(Form), CV Pattern, Tone
		/// </summary>
		[Test]
		public void Pronunciations_StringFields_Multilingual()
		{
			ILexPronunciation firstPronunciation;
			ILexEntry firstEntryWithPronunciation;
			ILexEntry firstEntryWithoutPronunciation;
			List<ILexEntry> entriesWithoutPronunciations;
			List<ILexPronunciation> pronunciations;
			SetupPronunciationData(out firstPronunciation,
				out firstEntryWithPronunciation,
				out firstEntryWithoutPronunciation,
				out entriesWithoutPronunciations,
				out pronunciations);

			// do a bulk copy from LexemeForm to Pronunciations
			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bv.ShowColumn("Pronunciation");
			m_bv.ShowColumn("CVPattern");
			m_bv.ShowColumn("Tone");

			FwOverrideComboBox bulkCopySourceCombo = m_bulkEditBar.GetTabControlChild("m_bulkCopySourceCombo") as FwOverrideComboBox;
			NonEmptyTargetControl bcNonEmptyTargetControl = m_bulkEditBar.GetTabControlChild("m_bcNonEmptyTargetControl") as NonEmptyTargetControl;
			// set to overwrite
			bcNonEmptyTargetControl.NonEmptyMode = NonEmptyTargetOptions.Overwrite;
			bulkCopySourceCombo.Text = "Lexeme Form";
			// first bulk copy the "Pronunciations" field, which is a multilingual field
			m_bulkEditBar.SetTargetField("Pronunciations");

			// first bulk copy into an existing pronunciation
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstPronunciation.Hvo }));
			Assert.AreEqual(firstPronunciation.Form.VernacularDefaultWritingSystem, "Pronunciation");

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			string lexemeForm = firstEntryWithPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem;
			Assert.AreEqual(lexemeForm, firstPronunciation.Form.VernacularDefaultWritingSystem);

			// next bulk copy into an empty (ghost) pronunciation
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstEntryWithoutPronunciation.Hvo }));
			lexemeForm = firstEntryWithoutPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem;

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			Assert.AreEqual(1, firstEntryWithoutPronunciation.PronunciationsOS.Count);
			Assert.AreEqual(lexemeForm, firstEntryWithoutPronunciation.PronunciationsOS[0].Form.VernacularDefaultWritingSystem);
		}

		/// <summary>
		/// (LT8958) Bulk Copy/Click Copy/Bulk Replace/Process: Pronunciation(Form), CV Pattern, Tone
		/// </summary>
		[Test]
		public void Pronunciations_StringFields_SimpleString()
		{
			ILexPronunciation firstPronunciation;
			ILexEntry firstEntryWithPronunciation;
			ILexEntry firstEntryWithoutPronunciation;
			List<ILexEntry> entriesWithoutPronunciations;
			List<ILexPronunciation> pronunciations;
			SetupPronunciationData(out firstPronunciation,
				out firstEntryWithPronunciation,
				out firstEntryWithoutPronunciation,
				out entriesWithoutPronunciations,
				out pronunciations);

			// do a bulk copy from LexemeForm to Pronunciations
			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bv.ShowColumn("Pronunciation");
			m_bv.ShowColumn("CVPattern");
			m_bv.ShowColumn("Tone");

			FwOverrideComboBox bulkCopySourceCombo = m_bulkEditBar.GetTabControlChild("m_bulkCopySourceCombo") as FwOverrideComboBox;
			NonEmptyTargetControl bcNonEmptyTargetControl = m_bulkEditBar.GetTabControlChild("m_bcNonEmptyTargetControl") as NonEmptyTargetControl;
			// set to overwrite
			bcNonEmptyTargetControl.NonEmptyMode = NonEmptyTargetOptions.Overwrite;
			bulkCopySourceCombo.Text = "Lexeme Form";
			// first bulk copy the "Pronunciations" field, which is a multilingual field
			m_bulkEditBar.SetTargetField("Tones");

			// first bulk copy into an existing pronunciation
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstPronunciation.Hvo }));
			Assert.AreEqual(firstPronunciation.Tone.Text, null);

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			string lexemeForm = firstEntryWithPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem;
			Assert.AreEqual(lexemeForm, firstPronunciation.Tone.Text);

			// next bulk copy into an empty (ghost) pronunciation
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstEntryWithoutPronunciation.Hvo }));
			lexemeForm = firstEntryWithoutPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem;

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			Assert.AreEqual(1, firstEntryWithoutPronunciation.PronunciationsOS.Count);
			Assert.AreEqual(lexemeForm, firstEntryWithoutPronunciation.PronunciationsOS[0].Tone.Text);
		}

		private void SetupAllomorphsData(out IMoForm firstAllomorph, out ILexEntry firstEntryWithAllomorph,
			out ILexEntry firstEntryWithoutAllomorph,
			out List<ILexEntry> entriesWithoutAllomorphs,
			out List<IMoForm> allomorphs)
		{
			firstAllomorph = null;
			firstEntryWithAllomorph = null;
			firstEntryWithoutAllomorph = null;
			entriesWithoutAllomorphs = new List<ILexEntry>();
			allomorphs = new List<IMoForm>();
			using (new UndoRedoTaskHelper(m_fdoCache, "SetupAllomorphsData", "SetupAllomorphsData"))
			{
				// find an entry with allomorphs.
				foreach (ILexEntry entry in Cache.LangProject.LexDbOA.EntriesOC)
				{
					if (entry.AlternateFormsOS.Count > 0)
					{
						allomorphs.AddRange(entry.AlternateFormsOS);
						if (firstAllomorph == null)
						{
							firstEntryWithAllomorph = entry;
							firstAllomorph = entry.AlternateFormsOS[0];
							allomorphs.Add(entry.AlternateFormsOS.Append(new MoStemAllomorph()));
						}
					}
					else
					{
						entriesWithoutAllomorphs.Add(entry);
						if (firstEntryWithoutAllomorph == null)
							firstEntryWithoutAllomorph = entry;
					}
				}
			}
		}

		/// <summary>
		/// LT-4268 Bulk Edit Allomorphs, list choice for IsAbstractForm (boolean field)
		/// </summary>
		[Test]
		public void Allomorphs_IsAbstractForm()
		{
			m_bulkEditBar.PersistSettings = true;
			// setup data.
			IMoForm firstAllomorph;
			ILexEntry firstEntryWithAllomorph;
			ILexEntry firstEntryWithoutAllomorph;
			List<ILexEntry> entriesWithoutAllomorphs;
			List<IMoForm> allomorphs;
			SetupAllomorphsData(out firstAllomorph,
				out firstEntryWithAllomorph,
				out firstEntryWithoutAllomorph,
				out entriesWithoutAllomorphs,
				out allomorphs);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			// first select an entry with an allomorph , and see if we move to that entry's pronunciation
			// when we switch to pronunciations list.
			clerk.JumpToRecord(firstEntryWithAllomorph.Hvo);
			m_window.ProcessPendingItems();
			Assert.AreEqual(firstEntryWithAllomorph.Hvo, clerk.CurrentObject.Hvo);
			// make sure we're not on the first index, since when we switch to pronunciations,
			// we want to make sure there is logic in place for keeping the index on a child pronunciation of this entry.
			Assert.Less(0, clerk.CurrentIndex);

			m_bulkEditBar.SwitchTab("ListChoice");
			int cOriginal = m_bv.ColumnSpecs.Count;
			// add column for Pronunciation Location
			m_bv.ShowColumn("IsAbstractFormForAllomorph");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 1, m_bv.ColumnSpecs.Count);
			m_bulkEditBar.SetTargetField("Is Abstract Form (Allomorph)");
			Assert.AreEqual("Is Abstract Form (Allomorph)", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			// check number of options and first is "jungle" (or Empty?)
			ComboBox listChoiceControl = m_bulkEditBar.GetTabControlChild("m_listChoiceControl") as ComboBox;
			Assert.IsNotNull(listChoiceControl);
			// expect to have some options (yes & no).
			Assert.AreEqual(2, listChoiceControl.Items.Count);
			IntComboItem item = listChoiceControl.Items[1] as IntComboItem;
			Assert.AreEqual("yes", item.ToString()); // 'yes'
			// check browse view class changed to LexPronunciation
			Assert.AreEqual(MoForm.kclsidMoForm, m_bv.ListItemsClass);
			// check that clerk list has also changed.
			Assert.AreEqual(MoForm.kclsidMoForm, m_bv.SortItemProvider.ListItemsClass);
			// make sure the list size includes all allomorphs, and all entries that don't have allomorphs.
			Assert.AreEqual(clerk.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);

			// make sure we're on the pronunciation of the entry we changed from
			Assert.AreEqual(firstAllomorph.Hvo, clerk.CurrentObject.Hvo);
			// change the first pronunciation's IsAbstract to something else
			Assert.AreEqual(false, firstAllomorph.IsAbstract);
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstAllomorph.Hvo }));
			listChoiceControl.SelectedItem = item; // change to 'yes'

			int cAllomorphs = firstEntryWithAllomorph.AlternateFormsOS.Count;
			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			// make sure we changed the list option and didn't add another separate pronunciation.
			Assert.AreEqual(Convert.ToBoolean(item.Value), firstAllomorph.IsAbstract);
			Assert.AreEqual(cAllomorphs, firstEntryWithAllomorph.AlternateFormsOS.Count);
			Assert.AreEqual(clerk.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);

			// now create a new allomorph on an entry that does not have one.
			cAllomorphs = firstEntryWithoutAllomorph.AlternateFormsOS.Count;
			Assert.AreEqual(0, cAllomorphs);
			clerk.JumpToRecord(firstEntryWithoutAllomorph.Hvo);
			m_window.ProcessPendingItems();
			Assert.AreEqual(firstEntryWithoutAllomorph.Hvo, clerk.CurrentObject.Hvo);
			int currentIndex = clerk.CurrentIndex;
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { firstEntryWithoutAllomorph.Hvo }));

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();

			// check that current index has remained the same.
			Assert.AreEqual(currentIndex, clerk.CurrentIndex);
			// but current object (entry) now has an allomorph
			Assert.Less(0, firstEntryWithoutAllomorph.AlternateFormsOS.Count);
			IMoForm newAllomorph = firstEntryWithoutAllomorph.AlternateFormsOS[0];
			// make sure we gave the new allomorph the expected setting.
			Assert.AreEqual(Convert.ToBoolean(item.Value), newAllomorph.IsAbstract);

			// now change the location to something else, and make sure we didn't create another pronunciation.
			IntComboItem item2 = listChoiceControl.Items[0] as IntComboItem;
			listChoiceControl.SelectedItem = item2;
			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();
			// make sure we gave the new pronunciation the expected location.
			Assert.AreEqual(Convert.ToBoolean(item2.Value), newAllomorph.IsAbstract);
			Assert.AreEqual(clerk.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);

			// refresh list, and make sure the clerk now has the allomorph, not the entry.
			this.MasterRefresh();
			clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			Assert.AreEqual(newAllomorph.Hvo, clerk.CurrentObject.Hvo);
			// also make sure the total count of the list has not changed.
			// we only converted an entry (ghost) to pronunciation.
			Assert.AreEqual(clerk.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);
		}

		/// <summary>
		/// </summary>
		[Test]
		public void EntryRefs_ListChoice_VariantEntryTypes()
		{
			// setup data.
			Dictionary<int, List<int>> variantRefs = new Dictionary<int, List<int>>();
			LexEntry.LoadAllVariantEntryRefs(m_fdoCache, variantRefs);
			int hvoSecondVariant = new List<int>(variantRefs.Keys)[2];
			int hvoSecondVariantRef = variantRefs[hvoSecondVariant][0];
			ILexEntryType choiceFreeVariant = null;
			foreach (ILexEntryType entryType in m_fdoCache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS)
			{
				if (entryType.Name.AnalysisDefaultWritingSystem == "Free Variant")
				{
					choiceFreeVariant = entryType;
					break;
				}
			}
			m_bulkEditBar.SwitchTab("ListChoice");
			int cOriginal = m_bv.ColumnSpecs.Count;
			// add column for Pronunciation Location
			m_bv.ShowColumn("VariantEntryTypesBrowse");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 1, m_bv.ColumnSpecs.Count);
			m_bulkEditBar.SetTargetField("Variant Types");
			Assert.AreEqual("Variant Types", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			clerk.JumpToRecord(hvoSecondVariantRef);
			m_window.ProcessPendingItems();
			Assert.AreEqual(hvoSecondVariantRef, clerk.CurrentObject.Hvo);
			// make sure we're not on the first index, since when we switch to pronunciations,
			// we want to make sure there is logic in place for keeping the index on a child pronunciation of this entry.
			Assert.Less(0, clerk.CurrentIndex);
			ILexEntryRef secondVariantRef = clerk.CurrentObject as ILexEntryRef;
			ILexEntryType firstVariantRefType = secondVariantRef.VariantEntryTypesRS[0];
			Assert.AreEqual("Spelling Variant", firstVariantRefType.Name.AnalysisDefaultWritingSystem);

			// check number of options
			ComplexListChooserBEditControl listChoiceControl = m_bulkEditBar.CurrentBulkEditSpecControl as ComplexListChooserBEditControl;
			Assert.IsNotNull(listChoiceControl);
			// check browse view class changed to LexPronunciation
			Assert.AreEqual(LexEntryRef.kclsidLexEntryRef, m_bv.ListItemsClass);
			// check that clerk list has also changed.
			Assert.AreEqual(LexEntryRef.kclsidLexEntryRef, m_bv.SortItemProvider.ListItemsClass);
			// allow changing an existing variant entry type to something else.
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { hvoSecondVariantRef }));
			// set list choice to "Free Variant" and Replace mode.

			listChoiceControl.ChosenHvos = new int[] { choiceFreeVariant.Hvo };
			listChoiceControl.ReplaceMode = true;

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();

			// make sure we gave the LexEntryRef the expected type.
			Assert.AreEqual(choiceFreeVariant.Hvo, secondVariantRef.VariantEntryTypesRS[0].Hvo);

			// Now try to add a variant entry type to a complex entry reference,
			// verify nothing changed.
			Dictionary<int, List<int>> complexRefs = new Dictionary<int, List<int>>();
			LexEntry.LoadAllComplexFormEntryRefs(m_fdoCache, complexRefs);
			int hvoComplexEntry = new List<int>(complexRefs.Keys)[0];
			int hvoComplexRef = complexRefs[hvoComplexEntry][0];

			m_bv.ShowColumn("ComplexEntryTypesBrowse");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 2, m_bv.ColumnSpecs.Count);
			m_bulkEditBar.SetTargetField("Complex Form Types");
			Assert.AreEqual("Complex Form Types", m_bulkEditBar.SelectedTargetFieldItem.ToString());
			clerk.JumpToRecord(hvoComplexRef);
			ILexEntryRef complexEntryRef = clerk.CurrentObject as ILexEntryRef;
			Assert.AreEqual(0, complexEntryRef.VariantEntryTypesRS.Count);

			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { hvoComplexRef }));
			// set list choice to "Free Variant" and Replace mode.

			listChoiceControl.ChosenHvos = new int[] { choiceFreeVariant.Hvo };
			listChoiceControl.ReplaceMode = true;

			m_bulkEditBar.ClickPreview(); // make sure we don't crash clicking preview button.
			m_bulkEditBar.ClickApply();

			// make sure we didn't add a variant entry type to the complex entry ref.
			Assert.AreEqual(0, complexEntryRef.VariantEntryTypesRS.Count);
		}

		#endregion BulkEditEntries tests
	}

	/// <summary>
	/// Maintain consistency of checked boxes when switching to target fields owned by different classes. (LT-8986)
	/// </summary>
	[TestFixture]
	public class BulkEditCheckBoxBehaviorTests : BulkEditBarTestsBase
	{
		/// <summary>
		/// queries the lexical database to find an entry with multiple descendents
		/// </summary>
		/// <returns></returns>
		private ILexEntry CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList()
		{
			ILexPronunciation pronunciation;
			ILexEntry ZZZparentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation(out pronunciation);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			clerk.UpdateList(true);
			return ZZZparentEntry;
		}

		private ILexEntry CreateZZZparentEntryWithMultipleSensesAndPronunciation(out ILexPronunciation pronunciation)
		{
			string formLexEntry = "ZZZparentEntry";
			int clsidForm = 0;
			ILexEntry parentEntry = LexEntry.CreateEntry(Cache,
														 MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm),
														 StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs), "ZZZparentEntry.sense1", null);
			ILexSense parentEntry_Sense1 = parentEntry.SensesOS[0];
			ILexSense parentEntry_Sense2 = LexSense.CreateSense(parentEntry, null, "ZZZparentEntry.sense2");
			pronunciation = parentEntry.PronunciationsOS.Append(new LexPronunciation());
			pronunciation.Form.SetAlternative("samplePronunciation", Cache.DefaultVernWs);
			return parentEntry;
		}

		private ICollection<ILexEntry> FindEntriesWithoutSenses()
		{
			IList<ILexEntry> entries = new List<ILexEntry>();
			foreach (ILexEntry e in Cache.LangProject.LexDbOA.EntriesOC)
			{
				if (e.SensesOS.Count == 0)
					entries.Add(e);
			}
			return entries;
		}

		private IDictionary<int, int> GetParentOfClassMap(IList<int> items, int clsidParent)
		{
			IDictionary<int, int> itemToParent = new Dictionary<int, int>();
			foreach (int hvoItem in items)
			{
				int hvoEntry = Cache.GetOwnerOfObjectOfClass(hvoItem, clsidParent);
				if (hvoEntry != 0)
					itemToParent.Add(hvoItem, hvoEntry);

			}
			return itemToParent;
		}

		#region FilterBehavior

		protected abstract class FilterBehavior : IDisposable
		{
			protected BulkEditCheckBoxBehaviorTests m_testFixture;

			protected FilterBehavior(BulkEditCheckBoxBehaviorTests testFixture)
			{
				m_testFixture = testFixture;
				FirstBehavior();
			}

			static internal FilterBehavior Create(BulkEditCheckBoxBehaviorTests testFixture)
			{
				if (testFixture is BulkEditCheckBoxBehaviorTestsWithFilterChanges)
					return new PusAndShowAll(testFixture);
				else
					return new NoFilter(testFixture);
			}

			#region IDisposable Members

			public void Dispose()
			{
				FinalBehavior();
				m_testFixture = null;
			}

			protected abstract void FirstBehavior();

			protected abstract void FinalBehavior();

			#endregion
		}

		protected class PusAndShowAll : FilterBehavior
		{
			internal PusAndShowAll(BulkEditCheckBoxBehaviorTests testFixture) : base(testFixture)
			{
			}

			protected override void FirstBehavior()
			{
				m_testFixture.m_bv.SetFilter("Lexeme Form", "Filter for...", "pus");
			}

			protected override void FinalBehavior()
			{
				m_testFixture.m_bv.SetFilter("Lexeme Form", "Show All", null);
			}
		}

		protected class NoFilter : FilterBehavior
		{
			internal NoFilter(BulkEditCheckBoxBehaviorTests testFixture)
				: base(testFixture)
			{
			}

			protected override void FirstBehavior()
			{
				// no behavior
			}

			protected override void FinalBehavior()
			{
				// no behavior
			}
		}

		#endregion FilterBehavior

		#region CheckboxBehavior_LT8986

		/// <summary>
		/// 1. When the view first comes up (has not been displayed since database opened),
		/// all items should be checked;
		/// don't persist set of checked items.
		///
		/// Review:
		///     In the future it may be helpful to hang onto it if they just switched to another view and came back.
		/// </summary>
		[Test]
		public virtual void CheckboxBehavior_AllItemsShouldBeInitiallyChecked()
		{
			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bulkEditBar.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntry.kclsidLexEntry, m_bv.ListItemsClass);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			// check that clerk list has also changed.
			Assert.AreEqual(clerk.ListSize, m_bv.CheckedItems.Count);

			// Check that we don't persist initial selection state
			m_bv.OnUncheckAll();
			Assert.AreEqual(0, m_bv.CheckedItems.Count);
			MasterRefresh();

			clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			Assert.AreEqual(clerk.ListSize, m_bv.CheckedItems.Count);
		}

		/// <summary>
		/// 2. When a change of filter or similar operation causes new items to be added to the list,
		/// restore any previous selected state (or default to 'selected')
		/// </summary>
		[Test]
		public virtual void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Selected()
		{
			ILexEntry ZZZparentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bulkEditBar.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntry.kclsidLexEntry, m_bv.ListItemsClass);

			// select only "ZZZparentEntry" before we filter it out.
			m_bv.OnUncheckAll();
			m_bv.SetCheckedItems(new List<int>(new int[] { ZZZparentEntry.Hvo }));
			Assert.AreEqual(1, m_bv.CheckedItems.Count);

			// Filter on "pus" and make sure everything now unselected.
			m_bv.SetFilter("Lexeme Form", "Filter for...", "pus");
			Assert.AreEqual(0, m_bv.CheckedItems.Count);

			// Broaden the to include everything again, and make sure that
			// our entry is still selected.
			m_bv.SetFilter("Lexeme Form", "Show All", null);
			Assert.AreEqual(1, m_bv.CheckedItems.Count);
			Assert.AreEqual(ZZZparentEntry.Hvo, m_bv.CheckedItems[0]);
		}

		[Test]
		public virtual void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Unselected()
		{
			ILexEntry ZZZparentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bulkEditBar.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntry.kclsidLexEntry, m_bv.ListItemsClass);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			// unselect our test data
			m_bv.UnselectItem(ZZZparentEntry.Hvo);
			IList<int> unselectedItems = m_bv.UncheckedItems();
			Assert.AreEqual(1, unselectedItems.Count);
			Assert.AreEqual(ZZZparentEntry.Hvo, unselectedItems[0]);

			// Filter on "pus" and make sure nothing is unselected.
			m_bv.SetFilter("Lexeme Form", "Filter for...", "pus");
			IList<int> unselectedItemsAfterFilterPus = m_bv.UncheckedItems();
			Assert.AreEqual(0, unselectedItemsAfterFilterPus.Count);

			// Extend our filter and make sure we've restored the thing we had selected.
			m_bv.SetFilter("Lexeme Form", "Show All", null);
			IList<int> unselectedItemsAfterShowAll = m_bv.UncheckedItems();
			Assert.AreEqual(1, unselectedItemsAfterShowAll.Count);
			Assert.AreEqual(ZZZparentEntry.Hvo, unselectedItemsAfterShowAll[0]);
		}

		/// <summary>
		/// 3. When we change the bulk edit field from a class to a "descendent" class
		/// (for example, from Entry to Sense...potentially many rows in the new list for each row in the old list),
		/// check all the items that are descendents of the ones that were checked before
		/// (e.g., all the senses of each checked Entry will get checked).
		/// </summary>
		[Test]
		public virtual void CheckboxBehavior_DescendentItemsShouldInheritSelection_Select()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			ILexEntry entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bulkEditBar.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntry.kclsidLexEntry, m_bv.ListItemsClass);

			m_bv.OnUncheckAll();
			// select the entry.
			m_bv.SetCheckedItems(new List<int>(new int[] {entryWithMultipleDescendents.Hvo}));
			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Glosses");

			Set<int> allSensesForEntry = new Set<int>(
				FdoVectorUtils.ConvertCmObjectsToHvos<ILexSense>(entryWithMultipleDescendents.AllSenses));
			Set<int> checkedItems = new Set<int>(m_bv.CheckedItems);
			Assert.AreEqual(allSensesForEntry.Count, checkedItems.Count, "Checked items mismatched.");
			Assert.IsTrue(checkedItems.Equals(allSensesForEntry), "Checked items mismatched.");
		}

		[Test]
		public virtual void CheckboxBehavior_DescendentItemsShouldInheritSelection_UnSelect()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			ILexEntry entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bulkEditBar.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntry.kclsidLexEntry, m_bv.ListItemsClass);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			// unselect the entry.
			m_bv.UnselectItem(entryWithMultipleDescendents.Hvo);

			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Glosses");

			Set<int> allSensesForEntry = new Set<int>(
			   FdoVectorUtils.ConvertCmObjectsToHvos<ILexSense>(entryWithMultipleDescendents.AllSenses));
			Set<int> uncheckedItems = new Set<int>(m_bv.UncheckedItems());
			Assert.AreEqual(allSensesForEntry.Count, uncheckedItems.Count, "Unchecked items mismatched.");
			Assert.IsTrue(uncheckedItems.Equals(allSensesForEntry), "Unchecked items mismatched.");
		}

		/// <summary>
		/// 4. When we change the bulk edit field to a "parent" class
		/// (for example, from Sense to Entry...one row in the new list for many in the old),
		/// check any item in the new list where are least one descendent is checked in the old...
		/// for example, an Entry will be checked if any of its senses was checked.
		/// </summary>
		[Test]
		public void CheckboxBehavior_ParentClassesItemsShouldInheritSelection_Selected()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			ILexEntry entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			// some entries (like variants) don't have senses, so we need to factor those into our results.
			ICollection<ILexEntry> entriesWithoutSenses = FindEntriesWithoutSenses();

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bulkEditBar.SetTargetField("Glosses");
			Assert.AreEqual(LexSense.kclsidLexSense, m_bv.ListItemsClass);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			m_bv.OnUncheckAll();
			// select the sense.
			m_bv.SetCheckedItems(new int[] { entryWithMultipleDescendents.AllSenses[0].Hvo });

			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Lexeme Form");

			Set<int> selectedEntries = new Set<int>(new int[] {entryWithMultipleDescendents.Hvo});
			selectedEntries.AddRange(FdoVectorUtils.ConvertCmObjectsToHvos<ILexEntry>(entriesWithoutSenses));
			Set<int> checkedItems = new Set<int>(m_bv.CheckedItems);
			Assert.AreEqual(selectedEntries.Count, checkedItems.Count, "Checked items mismatched.");
			Assert.IsTrue(checkedItems.Equals(selectedEntries), "Checked items mismatched.");
		}

		/// <summary>
		/// parents whose children are all unselected should be unselected.
		/// </summary>
		[Test]
		public void CheckboxBehavior_ParentClassesItemsShouldInheritSelection_UnSelected()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			ILexEntry entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bulkEditBar.SetTargetField("Glosses");
			Assert.AreEqual(LexSense.kclsidLexSense, m_bv.ListItemsClass);
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			// unselect all the senses belonging to this entry
			m_bv.UncheckItems(FdoVectorUtils.ConvertCmObjectsToHvos<ILexSense>(entryWithMultipleDescendents.AllSenses));

			// switch to the parent list
			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Lexeme Form");

			Set<int> unselectedEntries = new Set<int>(new int[] { entryWithMultipleDescendents.Hvo });
			Set<int> uncheckedItems = new Set<int>(m_bv.UncheckedItems());
			Assert.AreEqual(unselectedEntries.Count, uncheckedItems.Count, "Unchecked items mismatched.");
			Assert.IsTrue(uncheckedItems.Equals(unselectedEntries), "Unchecked items mismatched.");
		}

		/// <summary>
		/// 5. When we change fields to a "sibling" class (for example, from Sense to Pronunciation....many:many relationship),
		/// apply rule 4 to decide which common ancestors should be checked,
		/// then rule 3 to decide which items in the new list should be.
		/// For example, check all the rows for a pronunciation field
		/// that belongs to an entry for which at least one sense was checked.
		/// </summary>
		[Test]
		public void CheckboxBehavior_SiblingClassesItemsShouldInheritSelectionThroughParent_Selected()
		{
			// first create an entry with a pronunciation and some senses.
			ILexPronunciation pronunciation;
			ILexEntry parentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation(out pronunciation);

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bv.ShowColumn("Pronunciation");
			m_bulkEditBar.SetTargetField("Pronunciations");
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			// go through each of the pronunciation items, and find the LexEntry owner.
			IDictionary<int, int> pronunciationsToEntries = GetParentOfClassMap(m_bv.AllItems,
				LexEntry.kclsidLexEntry);
			// uncheck everything before we switch to sibling list.
			m_bv.OnUncheckAll();
			m_bv.SelectItem(pronunciation.Hvo);
			// now switch to (sense) sibling list
			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Glosses");
			// validate that only the siblings are selected.
			Set<int> hvoSenseSiblings = new Set<int>(FdoVectorUtils.ConvertCmObjectsToHvos<ILexSense>(parentEntry.AllSenses));
			Assert.AreEqual(hvoSenseSiblings.Count, m_bv.CheckedItems.Count);
			Assert.IsTrue(hvoSenseSiblings.Equals(new Set<int>(m_bv.CheckedItems)));
		}

		[Test]
		public void CheckboxBehavior_SiblingClassesItemsShouldInheritSelectionThroughParent_UnSelected()
		{
			// first create an entry with a pronunciation and some senses.
			ILexPronunciation pronunciation;
			ILexEntry parentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation(out pronunciation);

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bv.ShowColumn("Pronunciation");
			m_bulkEditBar.SetTargetField("Pronunciations");
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			// go through each of the pronunciation items, and find the LexEntry owner.
			IDictionary<int, int> pronunciationsToEntries = GetParentOfClassMap(m_bv.AllItems,
				LexEntry.kclsidLexEntry);
			// Unselect one sibling
			m_bv.UnselectItem(pronunciation.Hvo);
			// now switch to (sense) sibling list
			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Glosses");
			// validate that only the siblings are unselected.
			Set<int> hvoSenseSiblings = new Set<int>(FdoVectorUtils.ConvertCmObjectsToHvos<ILexSense>(parentEntry.AllSenses));
			Set<int> uncheckedItems = new Set<int>(m_bv.UncheckedItems());
			Assert.AreEqual(hvoSenseSiblings.Count, uncheckedItems.Count);
			Assert.IsTrue(hvoSenseSiblings.Equals(uncheckedItems));
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void CheckboxBehavior_SiblingClassesItemsShouldInheritSelectionThroughParent_UnselectAll()
		{
			// first create an entry with a pronunciation and some senses.
			ILexPronunciation pronunciation;
			ILexEntry parentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation(out pronunciation);
			// some entries (like variants) don't have senses, so we need to factor those into our results.
			ICollection<ILexEntry> entriesWithoutSenses = FindEntriesWithoutSenses();

			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bv.ShowColumn("Allomorph");
			m_bulkEditBar.SetTargetField("Glosses");
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;

			// Unselect All
			m_bv.OnUncheckAll();
			// now switch to allomorphs
			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Allomorphs");
			// validate that everything (except variant allomorph?) is still not selected.
			Set<int> checkedItems = new Set<int>(m_bv.CheckedItems);
			Set<int> selectedEntries = new Set<int>();
			selectedEntries.AddRange(FdoVectorUtils.ConvertCmObjectsToHvos<ILexEntry>(entriesWithoutSenses));
			Assert.AreEqual(selectedEntries.Count, checkedItems.Count);
		}

		/// <summary>
		/// 6 (2b) When we [move up] two or more levels in the hierarchy (e.g., from Example translations to Entries),
		/// it may be that new items appear in the list (e.g., entries none of whose senses have examples that could have translations).
		/// Following rule 2, these should be checked (if anything is checked?).
		/// NOTE: typically moving up the ownership tree will reduce the number of items, but
		/// "Example translations" is exceptional in that we haven't provided the capability
		/// of ghosting objects that belong to ghostable parents (ie. Examples need to exist before
		/// we can ghost Translations owned by Examples.)
		/// </summary>
		[Test]
		public void CheckboxBehavior_SelectParentsThatWereNotInOwnershipTreeOfChildList()
		{
			m_bulkEditBar.SwitchTab("BulkCopy");
			m_bv.ShowColumn("ExampleTranslation");
			m_bulkEditBar.SetTargetField("Example Translations");
			RecordClerk clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
			// having fewer translations than parent entries is strange
			// but it's currently the only way we can allow bulk editing translations.
			// We can allow ghosting for Examples that don't have translations
			// but not for a translation of a ghosted (not-yet existing) Example.
			Assert.Less(clerk.ListSize, Cache.LangProject.LexDbOA.EntriesOC.Count);

			// Uncheck everything before we switch to parent list
			m_bv.OnUncheckAll();
			IList<int> uncheckedTranslationItems = m_bv.UncheckedItems();
			Assert.AreEqual(uncheckedTranslationItems.Count, clerk.ListSize);

			// go through each of the translation items, and find the LexEntry owner.
			IDictionary<int, int> translationsToEntries = GetParentOfClassMap(uncheckedTranslationItems,
				LexEntry.kclsidLexEntry);
			Set<int> expectedUnselectedEntries = new Set<int>(translationsToEntries.Values);

			// Now switch to Entries and expect the new parent items to be selected.
			using (FilterBehavior.Create(this))
				m_bulkEditBar.SetTargetField("Lexeme Form");

			Set<int> entriesSelected = new Set<int>(m_bv.CheckedItems);
			Set<int> entriesUnselected = new Set<int>(m_bv.UncheckedItems());
			Assert.AreEqual(expectedUnselectedEntries.Count, entriesUnselected.Count, "Unselected items mismatched.");
			Assert.IsTrue(expectedUnselectedEntries.Equals(entriesUnselected), "Unselected items mismatched.");
			Assert.Greater(entriesSelected.Count, 0);
		}

		/// <summary>
		///
		/// Review: (EricP) Ask about the scenario about user changing their mind about the target,
		/// and then selecting more then they wanted to.
		///
		/// (JohnT)
		/// 7. Behavior should not depend on any state that was not visible before the current change.
		/// For example, if the user changes from a Sense field to an Entry field
		/// and then back to a Sense one, extra senses may become checked
		/// (where some but not all the senses of an entry were originally checked).
		/// We could conceivably remember which senses were previously checked
		/// and make use of this for any entries that didn't change state,
		/// or at least if NO entries changed state since we were last showing a Sense field.
		/// However, the result of changing from an Entry field to a Sense field is
		/// then unpredictable from what the user can see...it depends on past actions he may have forgotten.
		/// </summary>
		[Test]
		public void CheckboxBehavior_PreserveChildMixedSelectedionsUnlessUserChangesSelectionState()
		{
			// TODO.
			//Assert.Fail();
		}



		#endregion MaintainCheckboxesSwitchingTargetListOwners_LT8986
	}


	/// <summary>
	/// Add a layer of complexity to certain BulkEditCheckBoxBehaviorTests by performing a filter
	/// before switching list classes (e.g. entries to senses).
	/// </summary>
	[TestFixture]
	public class BulkEditCheckBoxBehaviorTestsWithFilterChanges : BulkEditCheckBoxBehaviorTests
	{
		/// <summary>
		///
		/// </summary>
		[Ignore("no need to test again.")]
		[Test]
		public override void CheckboxBehavior_AllItemsShouldBeInitiallyChecked()
		{
			// no need to test again, when subclass has already done so.
		}

		[Ignore("no need to test again.")]
		[Test]
		public override void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Selected()
		{
			// no need to test again, when subclass has already done so.
		}

		[Ignore("no need to test again.")]
		[Test]
		public override void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Unselected()
		{
			// no need to test again, when subclass has already done so.
		}
	}
}