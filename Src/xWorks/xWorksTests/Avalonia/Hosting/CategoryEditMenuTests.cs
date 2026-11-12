// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;
using static SIL.FieldWorks.XWorks.DetailMenuTestSupport;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Grammar area's menus in the Avalonia detail view, answered natively through the
	/// product <see cref="RecordEditView"/>: every row menu of a category in Category Edit,
	/// Duplicate and Move on an affix slot and an affix template, Insert Subcategory, the
	/// Insert menu's commands, and the Subitems menu of Exception "Features".
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class CategoryEditMenuTests : XWorksAppTestBase
	{
		private const string SlotMenuId = "mnuDataTree-POS-AffixSlot";
		private const string TemplateMenuId = "mnuDataTree-POS-AffixTemplate";
		private const string SubcategoriesMenuId = "mnuDataTree-POS-SubPossibilities";
		private const string SubitemsMenuId = "mnuDataTree-SubPossibilities";

		private PropertyTable m_propertyTable;
		private RecordEditView m_view;
		private int m_categoryDepth;
		private int m_restrictionDepth;
		private IPartOfSpeech m_verb;
		private IPartOfSpeech m_irregular;
		private IPartOfSpeech m_noun;
		private IMoInflAffixSlot m_tense;
		private IMoInflAffixSlot m_person;
		private IMoInflAffixTemplate m_finite;
		private IMoInflAffixTemplate m_infinitive;
		private ICmPossibility m_restriction;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			// The hidden DataTree the equivalence baseline drives loads the layout Inventory,
			// which is keyed by the project path.
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
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateData);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in new ICmObject[] { m_verb, m_noun, m_restriction })
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
				Cache.LangProject.PartsOfSpeechOA.Depth = m_categoryDepth;
				Cache.LangProject.MorphologicalDataOA.ProdRestrictOA.Depth = m_restrictionDepth;
			});
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
		public void EveryRowMenu_OfACategoryWithSlotsTemplatesAndSubcategories_IsAnsweredNatively()
		{
			ShowCategory(m_verb);
			var rows = ShownRows();
			foreach (var id in new[] { SlotMenuId, TemplateMenuId, SubcategoriesMenuId })
				Assert.That(rows.Any(r => r.MenuId == id), Is.True, "precondition: a row binds " + id);

			Assert.That(UnansweredMenus(rows), Is.Empty, "no Category Edit menu needs the hidden tree");
		}

		// Contract: the item menus and the Subcategories menu render what the adapter path
		// rendered, and are built without the mediator.
		[Test]
		public void TheSlotTemplateAndSubcategoriesMenus_RenderWhatTheAdapterPathRendered_WithoutTheMediator()
		{
			ShowCategory(m_verb);
			var window = m_propertyTable.GetValue<XWindow>("window");
			foreach (var row in new[] { SlotRow(m_tense), TemplateRow(m_finite), SubcategoriesRow() })
			{
				var ids = new[] { row.MenuId };
				var authority = m_view.CreateMenuAuthority(DetailMenuRequest.FromAnchor(null, row, DetailMenuKind.SliceMenu));
				var spy = new MenuDisplaySpyColleague();
				window.Mediator.AddColleague(spy);
				string native;
				try
				{
					native = Describe(XCoreMenuBridge.CreateMenuItems(window, ids, null, null, authority));
				}
				finally
				{
					window.Mediator.RemoveColleague(spy);
				}
				Assert.That(spy.Asked, Is.False, row.MenuId + ": the mediator was asked");
				Assert.That(native, Does.Contain("enabled=True checked=False executes=True"), row.MenuId);

				EnsureAdapter(row.ObjectHvo, row.Field);
				var adapter = Describe(XCoreMenuBridge.CreateMenuItems(window, ids, null, null, null));

				Assert.That(native, Is.EqualTo(adapter), row.MenuId);
			}
		}

		[Test]
		public void DuplicateAffixSlot_CopiesTheSlotIntoItsCategory_InOneUndoableStep()
		{
			ShowCategory(m_verb);

			Invoke(SlotRow(m_tense), "Duplicate Affix Slot");

			Assert.That(m_verb.AffixSlotsOC, Has.Count.EqualTo(3));
			Assert.That(m_tense.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Tense"), "the original keeps its name");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_verb.AffixSlotsOC, Is.EquivalentTo(new[] { m_tense, m_person }), "one Undo");
		}

		[Test]
		public void DuplicateAffixTemplate_PutsTheCopyRightAfterTheTemplate_InOneUndoableStep()
		{
			ShowCategory(m_verb);

			Invoke(TemplateRow(m_finite), "Duplicate Affix Template");

			Assert.That(m_verb.AffixTemplatesOS, Has.Count.EqualTo(3));
			Assert.That(m_verb.AffixTemplatesOS[0], Is.EqualTo(m_finite));
			Assert.That(m_verb.AffixTemplatesOS[2], Is.EqualTo(m_infinitive));
			Assert.That(m_verb.AffixTemplatesOS[1].PrefixSlotsRS.ToList(), Is.EqualTo(new[] { m_tense }));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_verb.AffixTemplatesOS.ToList(), Is.EqualTo(new[] { m_finite, m_infinitive }), "one Undo");
		}

		[Test]
		public void MoveAffixSlot_AsksForACategory_AndMovesTheSlotThere_InOneUndoableStep()
		{
			ShowCategory(m_verb);
			m_view.MoveDestinationChosen = (current, title) =>
			{
				Assert.That(current, Is.EqualTo(m_verb), "the category the slot is in");
				Assert.That(title, Is.EqualTo("Move Affix Slot"));
				return m_noun;
			};

			Invoke(SlotRow(m_tense), "Move Affix Slot");

			Assert.That(m_tense.Owner, Is.EqualTo(m_noun));
			Assert.That(ShownRows().Any(r => r.ObjectHvo == m_tense.Hvo), Is.False, "the category no longer shows the slot");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_tense.Owner, Is.EqualTo(m_verb), "one Undo");
		}

		[Test]
		public void MoveAffixSlot_Cancelled_OrToTheCategoryItIsIn_ChangesNothing()
		{
			ShowCategory(m_verb);
			var steps = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			m_view.MoveDestinationChosen = (current, title) => null;
			Invoke(SlotRow(m_tense), "Move Affix Slot");
			m_view.MoveDestinationChosen = (current, title) => current;
			Invoke(SlotRow(m_tense), "Move Affix Slot");

			Assert.That(m_tense.Owner, Is.EqualTo(m_verb));
			Assert.That(Cache.ActionHandlerAccessor.UndoableSequenceCount, Is.EqualTo(steps), "no undoable step");
		}

		[Test]
		public void MoveAffixTemplate_FromASubcategoryToTheCategoryAbove_TakesItsSlotAlong()
		{
			IMoInflAffixTemplate template = null;
			IMoInflAffixSlot slot = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_irregular.AffixSlotsOC.Add(slot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create());
				slot.Name.SetAnalysisDefaultWritingSystem("Aspect");
				m_irregular.AffixTemplatesOS.Add(
					template = Cache.ServiceLocator.GetInstance<IMoInflAffixTemplateFactory>().Create());
				template.SuffixSlotsRS.Add(slot);
			});
			ShowCategory(m_irregular);
			m_view.MoveDestinationChosen = (current, title) =>
			{
				Assert.That(title, Is.EqualTo("Move Affix Template"));
				return m_verb;
			};

			Invoke(TemplateRow(template), "Move Affix Template");

			Assert.That(template.Owner, Is.EqualTo(m_verb));
			Assert.That(slot.Owner, Is.EqualTo(m_verb), "the slot it uses follows");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(template.Owner, Is.EqualTo(m_irregular), "one Undo");
			Assert.That(slot.Owner, Is.EqualTo(m_irregular));
		}

		// Not run: a subcategory is created with the modal category catalog dialog.
		[Test]
		public void InsertSubcategory_GoesUnderTheShownCategory_AndBesideASubcategoryFromItsRow()
		{
			var command = CommandOf("CmdDataTree-Insert-POS-SubPossibilities");

			ShowCategory(m_verb);
			Assert.That(Item(MenuItems(SubcategoriesRow()), "Insert Subcategory...").IsEnabled, Is.True);
			Assert.That(m_view.ResolveInsertTarget(m_verb, command).Owner, Is.EqualTo(m_verb), "a top-level category");
			Assert.That(m_view.ResolveInsertTarget(m_irregular, command).Owner, Is.EqualTo(m_verb),
				"from a subcategory's row, beside it");

			ShowCategory(m_irregular);
			Assert.That(Item(MenuItems(SubcategoriesRow()), "Insert Subcategory...").IsEnabled, Is.True);
			Assert.That(m_view.ResolveInsertTarget(m_irregular, command).Owner, Is.EqualTo(m_irregular), "a nested category");
		}

		[Test]
		public void TheCommandAuthority_InCategoryEdit_AnswersTheInsertMenu_ForTheShownCategory()
		{
			ShowCategory(m_verb);
			Assert.That(m_view.CreateCommandAuthority().AnsweredMessages, Is.EquivalentTo(new[] { RecordEditView.InsertMessage }));
			var authority = CommandAuthorities.Of(m_propertyTable);
			foreach (var id in new[]
			{
				"CmdDataTree-Insert-POS-SubPossibilities", "CmdDataTree-Insert-POS-AffixTemplate",
				"CmdDataTree-Insert-POS-AffixSlot", "CmdDataTree-Insert-POS-InflectionClass"
			})
			{
				var display = Display();
				Assert.That(authority.UpdateDisplay(CommandOf(id), display), Is.True, id);
				Assert.That(display.Visible && display.Enabled, Is.True, id);
			}

			Assert.That(authority.Execute(CommandOf("CmdDataTree-Insert-POS-AffixSlot")), Is.True);
			DrainMediatorAndIdleQueues();

			Assert.That(m_verb.AffixSlotsOC, Has.Count.EqualTo(3), "a new slot in the shown category");
		}

		[Test]
		public void ExceptionFeatures_AnswersEveryRowMenuNatively_AndInsertsASubitemInAHierarchicalList()
		{
			ShowTool("ProdRestrictEdit", m_restriction);
			Assert.That(ShownRows().Any(r => r.MenuId == SubitemsMenuId), Is.False, "a flat list has no Subitems row");
			Assert.That(UnansweredMenus(ShownRows()), Is.Empty, "a flat list");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => Cache.LangProject.MorphologicalDataOA.ProdRestrictOA.Depth = 127);
			m_view.RefreshDisplay();
			DrainMediatorAndIdleQueues();
			var subitems = ShownRows().First(r => r.MenuId == SubitemsMenuId);
			Assert.That(UnansweredMenus(ShownRows()), Is.Empty, "a hierarchical list");

			Invoke(subitems, "Insert subitem");

			Assert.That(m_restriction.SubPossibilitiesOS, Has.Count.EqualTo(1));
		}

		// The template's Description part holds one row per state of Active; hiding the row
		// holds in both.
		[Test]
		public void FieldVisibility_OnATemplatesDescription_HoldsWhenActiveIsUnticked()
		{
			ShowCategory(m_verb);
			var row = DescriptionRow(m_finite);
			Assert.That(row, Is.Not.Null, "precondition: an active template shows its Description");
			var store = OverrideStore();
			store.Save(new ViewDefinitionOverride(row.ClassName, row.LayoutName, "detail", null, null));
			try
			{
				var window = m_propertyTable.GetValue<XWindow>("window");
				var items = XCoreMenuBridge.CreateMenuItems(window, new[] { ObjectMenuAuthority.MenuId },
					null, null, m_view.CreateObjectMenuAuthority(row));
				Run(Item(items, "Field Visibility").Children, "Normally hidden");
				DrainMediatorAndIdleQueues();

				var ops = store.TryGet(row.ClassName, row.LayoutName).Operations;
				Assert.That(ops.Select(o => o.Kind), Is.All.EqualTo(ViewOverrideOperationKind.SetVisibility));
				Assert.That(ops, Has.Count.EqualTo(2), "one write per state of Active");
				Assert.That(ops.Select(o => o.Visibility), Is.All.EqualTo(ViewVisibility.Never));
				Assert.That(DescriptionRow(m_finite), Is.Null, "hidden while active");

				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => m_finite.Disabled = true);
				m_view.RefreshDisplay();
				DrainMediatorAndIdleQueues();

				Assert.That(DescriptionRow(m_finite), Is.Null, "still hidden once Active is unticked");
			}
			finally
			{
				store.Save(new ViewDefinitionOverride(row.ClassName, row.LayoutName, "detail", null, null));
			}
		}

		// Verb (two slots, two templates, the subcategory Irregular) and Noun in a
		// hierarchical category list; one exception feature in its flat list.
		private void CreateData()
		{
			var services = Cache.ServiceLocator;
			var list = Cache.LangProject.PartsOfSpeechOA;
			m_categoryDepth = list.Depth;
			list.Depth = 127;
			var categories = services.GetInstance<IPartOfSpeechFactory>();
			list.PossibilitiesOS.Add(m_verb = categories.Create());
			m_verb.Name.SetAnalysisDefaultWritingSystem("Verb");
			m_verb.SubPossibilitiesOS.Add(m_irregular = categories.Create());
			m_irregular.Name.SetAnalysisDefaultWritingSystem("Irregular");
			list.PossibilitiesOS.Add(m_noun = categories.Create());
			m_noun.Name.SetAnalysisDefaultWritingSystem("Noun");
			var slots = services.GetInstance<IMoInflAffixSlotFactory>();
			m_verb.AffixSlotsOC.Add(m_tense = slots.Create());
			m_tense.Name.SetAnalysisDefaultWritingSystem("Tense");
			m_verb.AffixSlotsOC.Add(m_person = slots.Create());
			m_person.Name.SetAnalysisDefaultWritingSystem("Person");
			var templates = services.GetInstance<IMoInflAffixTemplateFactory>();
			m_verb.AffixTemplatesOS.Add(m_finite = templates.Create());
			m_finite.Name.SetAnalysisDefaultWritingSystem("Finite");
			m_finite.PrefixSlotsRS.Add(m_tense);
			m_verb.AffixTemplatesOS.Add(m_infinitive = templates.Create());
			m_infinitive.Name.SetAnalysisDefaultWritingSystem("Infinitive");
			var restrictions = Cache.LangProject.MorphologicalDataOA.ProdRestrictOA;
			m_restrictionDepth = restrictions.Depth;
			restrictions.Depth = 1;
			restrictions.PossibilitiesOS.Add(m_restriction = services.GetInstance<ICmPossibilityFactory>().Create());
			m_restriction.Name.SetAnalysisDefaultWritingSystem("irregular plural");
		}

		private void ShowCategory(IPartOfSpeech category) => ShowTool("posEdit", category);

		// Loads the tool's detail view and shows the record in it.
		private void ShowTool(string tool, ICmObject record)
		{
			LoadRecordEditView(m_propertyTable, tool);
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the RecordEditView of " + tool + " to load");
			m_view.Clerk.JumpToRecord(record.Hvo);
			DrainMediatorAndIdleQueues();
			Assert.That(m_view.Clerk.CurrentObject, Is.EqualTo(record), "precondition: the view shows the record");
		}

		private DetailField SlotRow(IMoInflAffixSlot slot)
			=> ShownRows().First(f => f.ObjectHvo == slot.Hvo && f.MenuId == SlotMenuId);

		private DetailField TemplateRow(IMoInflAffixTemplate template)
			=> ShownRows().First(f => f.ObjectHvo == template.Hvo && f.MenuId == TemplateMenuId);

		private DetailField SubcategoriesRow() => ShownRows().First(f => f.MenuId == SubcategoriesMenuId);

		private DetailField DescriptionRow(IMoInflAffixTemplate template)
			=> ShownRows().FirstOrDefault(f => f.ObjectHvo == template.Hvo && f.Field == "Description");

		private ViewDefinitionOverrideStore OverrideStore()
			=> (ViewDefinitionOverrideStore)typeof(RecordEditView)
				.GetProperty("ViewOverrideStore", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(m_view);

		// The menus of the rows that some id leaves to the hidden tree.
		private List<string> UnansweredMenus(IEnumerable<DetailField> rows)
		{
			var unanswered = new List<string>();
			foreach (var row in rows)
			{
				foreach (var menu in MenusOf(row))
				{
					var authority = m_view.CreateMenuAuthority(DetailMenuRequest.FromAnchor(null, row, menu.Kind));
					if (!XCoreMenuBridge.OwnsAll(authority, menu.Ids))
						unanswered.Add(row.Label + " " + menu.Kind + ": " + string.Join(", ", menu.Ids));
				}
			}
			return unanswered;
		}

		private IReadOnlyList<DetailMenuItem> MenuItems(DetailField row)
			=> DetailMenuTestSupport.MenuItems(m_view, m_propertyTable, row);

		private void Invoke(DetailField row, string label)
		{
			Run(MenuItems(row), label);
			DrainMediatorAndIdleQueues();
		}

		private Command CommandOf(string id) => DetailMenuTestSupport.CommandOf(m_propertyTable, id);

		private void EnsureAdapter(int targetHvo, string fieldName)
			=> DetailMenuTestSupport.EnsureAdapter(m_view, targetHvo, fieldName);

		private List<DetailField> ShownRows() => DetailMenuTestSupport.ShownRows(m_view);
	}
}
