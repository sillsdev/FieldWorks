// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using XCore;
using static SIL.FieldWorks.XWorks.DetailMenuTestSupport;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The label menu of a sense's Reversal Entries row in the Avalonia detail view, answered
	/// natively through the product <see cref="RecordEditView"/>: its Writing Systems submenu
	/// lists the row's own options, and choosing among them changes which reversal indexes
	/// the row shows.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class ReversalEntriesRowMenuTests : XWorksAppTestBase
	{
		private const string ReversalField = "ReferringReversalIndexEntries";
		private const string WritingSystemsLabel = "Writing Systems";
		private const string ShowAllLabel = "Show all right now";
		private const string ConfigureLabel = "Configure...";

		private PropertyTable m_propertyTable;
		private RecordEditView m_view;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private CoreWritingSystemDefinition m_english;
		private CoreWritingSystemDefinition m_spanish;
		private bool m_addedSpanish;
		private DetailField m_overriddenRow;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			Cache.ProjectId.Path = Path.Combine(Path.GetTempPath(), Cache.ProjectId.Name,
				Cache.ProjectId.Name + ".junk");
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			TestLocalizationManagerBootstrap.EnsureInitialized();
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateData);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView(m_propertyTable, "lexiconEdit");
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the lexicon edit RecordEditView to load");
			m_view.Clerk.JumpToRecord(m_entry.Hvo);
			DrainMediatorAndIdleQueues();
		}

		[TearDown]
		public void TearDownWindow()
		{
			// Saving an empty patch deletes the file, so overrides never leak between tests.
			if (m_overriddenRow != null)
			{
				OverrideStore().Save(new ViewDefinitionOverride(m_overriddenRow.ClassName,
					m_overriddenRow.LayoutName, "detail", null, null));
			}
			m_overriddenRow = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in Enumerable.Reverse(m_createdObjects))
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
				if (m_addedSpanish)
					Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Remove(m_spanish);
			});
			m_createdObjects = null;
			m_entry = null;
			m_view = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		[Test]
		public void TheLabelMenu_IsAnsweredNatively_WithoutTheMediator()
		{
			var row = ReversalRow();
			var ids = RecordEditView.ComposeSliceMenuIds(row.MenuId, row.IsMultiStringRow).ToArray();
			Assert.That(ids, Is.EqualTo(new[] { WritingSystemsMenuAuthority.MenuId }),
				"precondition: the row binds the writing-systems menu itself");
			var authority = m_view.CreateMenuAuthority(DetailMenuRequest.FromAnchor(null, row, DetailMenuKind.SliceMenu));

			Assert.That(XCoreMenuBridge.OwnsAll(authority, ids), Is.True,
				"the row's whole label menu has a native authority");
			var window = m_propertyTable.GetValue<XWindow>("window");
			var spy = new MenuDisplaySpyColleague();
			window.Mediator.AddColleague(spy);
			IReadOnlyList<DetailMenuItem> items;
			try
			{
				items = XCoreMenuBridge.CreateMenuItems(window, ids, authority);
			}
			finally
			{
				window.Mediator.RemoveColleague(spy);
			}

			Assert.That(spy.Asked, Is.False, "nothing in the row's label menu reaches the mediator");
			Assert.That(items.Where(i => !i.IsSeparator).Select(i => i.Label).Take(3),
				Is.EqualTo(new[] { "Field Visibility", WritingSystemsLabel, "Move Field" }));
		}

		[Test]
		public void TheLeavesBesideWritingSystems_AreThePerObjectMenus()
		{
			var row = ReversalRow();
			var window = m_propertyTable.GetValue<XWindow>("window");

			var own = MenuItems(m_view, m_propertyTable, row).Where(i => i.Label != WritingSystemsLabel).ToList();
			var perObject = XCoreMenuBridge.CreateMenuItems(window, new[] { RecordEditView.ObjectMenuId },
				m_view.CreateObjectMenuAuthority(row));

			Assert.That(Describe(own), Is.EqualTo(Describe(perObject)),
				"Field Visibility, Move Field and Help answer as on any other row");
		}

		[Test]
		public void TheWritingSystemsMenu_OffersEveryAnalysisWritingSystem_AllShownUntilTheUserChooses()
		{
			var options = ReversalEntryWritingSystems.Options(Cache);
			Assert.That(Cache.LangProject.CurrentAnalysisWritingSystems, Does.Not.Contain(m_spanish),
				"precondition: the project has not checked Spanish");

			var menu = WritingSystemsMenu(ReversalRow());

			Assert.That(menu.Select(i => i.Label), Is.EqualTo(
				new[] { ShowAllLabel }.Concat(options.Select(ws => ws.DisplayLabel)).Concat(new[] { ConfigureLabel })),
				"the row's own options, not another row's: every analysis writing system, checked or not");
			var toggles = menu.Skip(1).Take(options.Count).ToList();
			Assert.That(toggles.Select(i => i.IsChecked), Is.All.True, "nothing is stored, so all are shown");
			Assert.That(toggles.Select(i => i.Execute), Is.All.Not.Null);
			// Both are disabled on the WinForms slice, which has no handler for them.
			Assert.That(Item(menu, ShowAllLabel).Execute, Is.Not.Null, "Show all right now works on this row");
			Assert.That(Item(menu, ConfigureLabel).Execute, Is.Not.Null, "Configure works on this row");
			Assert.That(GroupsOf(ReversalRow()), Is.EqualTo(new[] { m_english.Id, m_spanish.Id }),
				"an entry in a writing system the project has not checked is shown by default");
		}

		[Test]
		public void SwitchingAWritingSystemOff_StoresTheRest_AndTheRowDropsItsIndex()
		{
			var options = ReversalEntryWritingSystems.Options(Cache).Select(ws => ws.Id).ToList();

			Toggle(m_spanish);

			var kept = options.Where(id => id != m_spanish.Id).ToList();
			Assert.That(StoredWritingSystems(), Is.EquivalentTo(kept), "the override stores what is still shown");
			var row = ReversalRow();
			Assert.That(row.VisibleWritingSystems, Is.EquivalentTo(kept), "the recomposed row carries the selection");
			Assert.That(GroupsOf(row), Is.EqualTo(new[] { m_english.Id }), "the Spanish index is no longer shown");
			var menu = WritingSystemsMenu(row);
			Assert.That(Item(menu, m_spanish.DisplayLabel).IsChecked, Is.False);
			Assert.That(Item(menu, m_english.DisplayLabel).IsChecked, Is.True);

			Toggle(m_spanish);

			Assert.That(StoredWritingSystems(), Is.EquivalentTo(options), "switching it back on stores it again");
			Assert.That(GroupsOf(ReversalRow()), Is.EqualTo(new[] { m_english.Id, m_spanish.Id }));
		}

		[Test]
		public void TheLastShownWritingSystem_CannotBeSwitchedOff()
		{
			foreach (var ws in ReversalEntryWritingSystems.Options(Cache).Where(ws => ws != m_english))
				Toggle(ws);

			var last = Item(WritingSystemsMenu(ReversalRow()), m_english.DisplayLabel);

			Assert.That(last.IsChecked, Is.True);
			Assert.That(last.IsEnabled, Is.False, "the row can never be left showing nothing");
			Assert.That(last.Execute, Is.Null);
			Assert.That(StoredWritingSystems(), Is.EqualTo(new[] { m_english.Id }));
		}

		[Test]
		public void ShowAllRightNow_ShowsEveryIndexAgain_WithoutChangingWhatIsStored()
		{
			Toggle(m_spanish);
			var stored = StoredWritingSystems().ToList();
			Assert.That(GroupsOf(ReversalRow()), Is.EqualTo(new[] { m_english.Id }), "precondition: Spanish is off");

			Item(WritingSystemsMenu(ReversalRow()), ShowAllLabel).Execute();
			DrainMediatorAndIdleQueues();

			Assert.That(GroupsOf(ReversalRow()), Is.EqualTo(new[] { m_english.Id, m_spanish.Id }),
				"the reveal shows every index again");
			Assert.That(StoredWritingSystems(), Is.EqualTo(stored), "the reveal stores nothing");
			Assert.That(Item(WritingSystemsMenu(ReversalRow()), m_spanish.DisplayLabel).IsChecked, Is.False,
				"the menu still shows the stored selection");
		}

		// The dialog is modal, so the test reads what the host hands it instead of opening it.
		[Test]
		public void Configure_IsGivenTheRowsOwnOptions_AndItsStoredSelection()
		{
			var options = ReversalEntryWritingSystems.Options(Cache);

			var row = ReversalRow();
			var choices = m_view.WritingSystemChoicesOf(row);
			Assert.That(choices.Options, Is.EqualTo(options));
			Assert.That(choices.Shown(row.VisibleWritingSystems), Is.EqualTo(options),
				"nothing is stored, so the dialog opens with every option checked");

			Toggle(m_spanish);

			row = ReversalRow();
			choices = m_view.WritingSystemChoicesOf(row);
			Assert.That(choices.Options, Is.EqualTo(options),
				"a writing system that was switched off is still offered");
			Assert.That(choices.Shown(row.VisibleWritingSystems),
				Is.EqualTo(options.Where(ws => ws != m_spanish)));
		}

		// The sense's Reversal Entries row among the rows the host shows now.
		private DetailField ReversalRow()
			=> ShownRows(m_view).Single(f => f.Kind == DetailFieldKind.Custom && f.Field == ReversalField);

		// The children of the row's Writing Systems submenu, as the host builds its label menu.
		private IReadOnlyList<DetailMenuItem> WritingSystemsMenu(DetailField row)
			=> Item(MenuItems(m_view, m_propertyTable, row), WritingSystemsLabel).Children;

		// Runs the writing system's own toggle in the row's menu and lets the view recompose.
		private void Toggle(CoreWritingSystemDefinition ws)
		{
			var row = ReversalRow();
			m_overriddenRow = row;
			Run(WritingSystemsMenu(row), ws.DisplayLabel);
			DrainMediatorAndIdleQueues();
		}

		// The writing systems the stored override op carries for the row.
		private IReadOnlyList<string> StoredWritingSystems()
		{
			var row = ReversalRow();
			var stored = OverrideStore().TryGet(row.ClassName, row.LayoutName);
			Assert.That(stored, Is.Not.Null, "toggling a writing system must write a project override");
			return stored.Operations.Single(o =>
				o.Kind == ViewOverrideOperationKind.SetVisibleWritingSystems).WritingSystems;
		}

		private ViewDefinitionOverrideStore OverrideStore()
		{
			var storeProperty = typeof(RecordEditView).GetProperty("ViewOverrideStore",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(storeProperty, Is.Not.Null, "the override store seam must exist");
			var store = (ViewDefinitionOverrideStore)storeProperty.GetValue(m_view);
			Assert.That(store, Is.Not.Null, "the project must have a reachable override store");
			return store;
		}

		// The writing systems the row's editor shows a reversal index for, in display order.
		// Each index is a group the editor stamps with <row automation id>.<ws id>.
		private List<string> GroupsOf(DetailField row)
		{
			var editor = row.ControlFactory(new SliceFactoryContext());
			var window = new Window { Content = editor, Width = 520, Height = 360 };
			try
			{
				window.Show();
				Dispatcher.UIThread.RunJobs();
				var shown = new HashSet<string>(editor.GetVisualDescendants().OfType<Control>()
					.Select(AutomationProperties.GetAutomationId).Where(id => id != null));
				var prefix = (row.AutomationId ?? ReversalIndexEntryPlugin.DefaultAutomationId) + ".";
				return ReversalEntryWritingSystems.Options(Cache).Select(ws => ws.Id)
					.Where(id => shown.Contains(prefix + id)).ToList();
			}
			finally
			{
				window.Close();
			}
		}

		// An entry whose one sense has a reversal entry in English, the project's checked
		// analysis writing system, and one in Spanish, which the project has not checked.
		private void CreateData()
		{
			var stem = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "casa", stem, "house", noun);
			var sense = m_entry.SensesOS[0];
			m_english = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "es", false, false, out m_spanish);
			var analysis = Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems;
			m_addedSpanish = !analysis.Contains(m_spanish);
			if (m_addedSpanish)
				analysis.Add(m_spanish);
			AddReversalEntry(m_english, "house", sense);
			AddReversalEntry(m_spanish, "casa", sense);
		}

		private void AddReversalEntry(CoreWritingSystemDefinition ws, string form, ILexSense sense)
		{
			var indexes = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			var existing = Cache.LanguageProject.LexDbOA.ReversalIndexesOC
				.Any(index => index.WritingSystem == ws.Id);
			var reversalIndex = indexes.FindOrCreateIndexForWs(ws.Handle);
			var entry = reversalIndex.FindOrCreateReversalEntry(form);
			entry.SensesRS.Add(sense);
			// Reverse order at teardown: the entry goes before an index made for it.
			if (!existing)
				m_createdObjects.Add(reversalIndex);
			m_createdObjects.Add(entry);
		}
	}
}
