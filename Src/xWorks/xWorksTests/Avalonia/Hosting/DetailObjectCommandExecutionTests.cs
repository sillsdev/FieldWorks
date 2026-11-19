// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using XCore;
// Both namespaces above define DataTree; the help-topic test means the WinForms one.
using LegacyDataTree = SIL.FieldWorks.Common.Framework.DetailControls.DataTree;
using PhonEnvRecognizer = SIL.LCModel.Core.Phonology.PhonEnvRecognizer;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The context menus of the Avalonia detail view on the real product host: a
	/// <see cref="RecordEditView"/> loaded through <see cref="MockFwXWindow"/> in the New UI
	/// mode. Each menu is built as <c>OnDetailMenuRequested</c> builds it, through
	/// <see cref="XCoreMenuBridge"/> and the row's native authorities; invoking an item's
	/// Execute is the user clicking it. The tests assert what the menu offers, what a
	/// command changes in the model, and that the recomposed detail reflects the change.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class DetailObjectCommandExecutionTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private RecordEditView m_view;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			// The WinForms DataTree the help-topic test shows needs the layout/parts Inventory,
			// keyed by the project path: give the test project a writable temp path first.
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
			// The lexicon detail layout includes the Chorus-backed MessageSlice, which localizes
			// strings when built; register a minimal LocalizationManager so the WinForms
			// DataTree can build the full tree headlessly.
			TestLocalizationManagerBootstrap.EnsureInitialized();
			// The mock app has no inner help-topic provider; give the PropertyTable a null-returning
			// stub so Help can be asked for a topic while a menu is built.
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			// Bootstrap the layout/parts Inventory the production RecordEditView loads via
			// EnsureDataTreeInitialized (LayoutCache loads the real lexicon .fwlayout/Parts).
			// Without it, DataTree.GetTemplateForObjLayout finds a null layout inventory and ShowObject
			// throws an NRE. This is the same bootstrap the DictionaryConfigurationMigrator tests use.
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestEntry);

			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();

			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the lexicon edit RecordEditView to load");
			EnsureCurrentRecord(m_view);
			Assert.That(GetField(m_view, "m_activeUIFramework"), Is.EqualTo(UIFramework.Avalonia),
				"precondition: lexiconEdit resolves to Avalonia under the New UI mode");
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyTestData);
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

		// ===== Reference-vector rows: the per-item menu and Move Left / Move Right =====

		private sealed class ItemSelection : IDetailItemSelection
		{
			public string SelectedItemKey { get; set; }
			public int SelectedItemIndex { get; set; }
		}

		private static DetailMenuRequest ItemRequest(DetailField field, int index, TextEditorStub editor)
		{
			editor.SelectedItemKey = field.Items[index].Key;
			editor.SelectedItemIndex = index;
			return DetailMenuRequest.FromAnchor(null, field, DetailMenuKind.ItemMenu, editor);
		}

		// Two complex forms whose primary lexeme is the test entry, so its Subentries row shows
		// two items; then the host re-shows the record so its edit context knows the row.
		private void MakeTwoSubentries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stem = GetMorphTypeOrCreateOne("stem");
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				foreach (var form in new[] { "command-entry-sub-a", "command-entry-sub-b" })
				{
					var complex = AddLexeme(m_createdObjects, form, stem, "sub " + form, noun);
					var ler = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
					complex.EntryRefsOS.Add(ler);
					ler.RefType = LexEntryRefTags.krtComplexForm;
					ler.ComponentLexemesRS.Add(m_entry);
					ler.PrimaryLexemesRS.Add(m_entry);
				}
			});
			DrainMediatorAndIdleQueues();
			RefreshedDetailFieldCount();
		}

		private DetailField SubentriesField()
		{
			var field = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.SingleOrDefault(f => f.Field == "Subentries" && f.Kind == DetailFieldKind.ReferenceVector);
			Assert.That(field, Is.Not.Null, "the entry composes a Subentries vector row");
			Assert.That(field.Items, Has.Count.EqualTo(2));
			return field;
		}

		[Test]
		public void SubentriesItemMenu_OffersTheJumpAsCtrlClickDefault_AndTheMoveCommands_ForTheClickedItem()
		{
			MakeTwoSubentries();
			var request = ItemRequest(SubentriesField(), 0);

			var items = m_view.BuildReferenceItemMenu(request, out var itemUi);
			try
			{
				Assert.That(itemUi, Is.Not.Null, "the clicked item's object UI answers the jumps");
				var jump = items.FirstOrDefault(i => !i.IsSeparator
					&& i.Label.StartsWith("Show Entry in Lexicon", StringComparison.Ordinal));
				Assert.That(jump, Is.Not.Null, "the clicked entry's jump command materializes");
				Assert.That(jump.Label, Does.EndWith(SIL.FieldWorks.FdoUi.CmObjectUi.CtrlClickSuffix),
					"the first enabled jump is marked as the Ctrl+click default, with the shared suffix");
				Assert.That(jump.IsEnabled, Is.True);

				var left = FindItem(items, "Move Left");
				var right = FindItem(items, "Move Right");
				Assert.That(left, Is.Not.Null, "Move Left materializes on the item menu");
				Assert.That(right, Is.Not.Null, "Move Right materializes on the item menu");
				Assert.That(left.IsEnabled, Is.False, "the first item cannot move left");
				Assert.That(right.IsEnabled, Is.True, "the first item can move right");
			}
			finally
			{
				itemUi?.Dispose();
			}
		}

		private static DetailMenuRequest ItemRequest(DetailField field, int index)
			=> DetailMenuRequest.FromAnchor(null, field, DetailMenuKind.ItemMenu,
				new ItemSelection { SelectedItemKey = field.Items[index].Key, SelectedItemIndex = index });

		// The item menu's native authority answers the reference-choices menu, so an item menu
		// is built from the row and the item alone.

		[Test]
		public void ReferenceItemAuthority_AnswersEveryLeafOfItsMenu()
		{
			MakeTwoSubentries();
			var authority = m_view.CreateReferenceItemAuthority(ItemRequest(SubentriesField(), 0), out var itemUi);
			using (itemUi)
			{
				var window = m_propertyTable.GetValue<XWindow>("window");
				var leaves = LeavesOf(ReferenceItemMenuAuthority.MenuId);
				Assert.That(leaves.Count, Is.GreaterThanOrEqualTo(40),
					"the shipped menu carries the jumps, the filters, the two marks and the moves");
				foreach (var leaf in leaves)
				{
					Assert.That(() => authority.Build(ReferenceItemMenuAuthority.MenuId, leaf), Throws.Nothing,
						"the authority does not answer leaf '{0}'", leaf.CommandId);
				}
				Assert.That(() => XCoreMenuBridge.CreateMenuItems(window, new[] { ReferenceItemMenuAuthority.MenuId },
					authority), Throws.Nothing, "the whole menu builds through the bridge");
				var targets = leaves
					.Where(l => l.Message == SIL.FieldWorks.FdoUi.CmObjectUi.JumpToToolMessage)
					.Select(l => l.Command.TargetId).Distinct().ToList();
				Assert.That(targets, Is.EqualTo(new[] { Guid.Empty }),
					"building the menu leaves no jump command carrying a target for a later menu");
			}
		}

		[Test]
		public void ReferenceItemAuthority_RejectsALeafItDoesNotAnswer()
		{
			MakeTwoSubentries();
			var authority = m_view.CreateReferenceItemAuthority(ItemRequest(SubentriesField(), 0), out var itemUi);
			using (itemUi)
			{
				var foreignLeaf = LeafOf(RecordEditView.ObjectMenuId, ObjectMenuAuthority.HelpCommandId);

				Assert.That(() => authority.Build(ReferenceItemMenuAuthority.MenuId, foreignLeaf),
					Throws.InvalidOperationException, "an owned id must be answered in full, never partially");
			}
		}

		[Test]
		public void ItemMenu_IsOwned_ForReferenceChoices_AndForEnvironments()
		{
			MakeTwoSubentries();
			var authority = m_view.CreateReferenceItemAuthority(ItemRequest(SubentriesField(), 0), out var itemUi);
			using (itemUi)
			{
				Assert.That(authority.Owns(itemUi.ContextMenuId), Is.True, "a subentry's item menu is answered natively");
				Assert.That(authority.Owns(RecordEditView.ObjectMenuId), Is.False);
			}
			var envAuthority = m_view.CreateReferenceItemAuthority(ItemRequest(EnvironmentsFieldWithOneItem(), 0),
				out var envUi);
			using (envUi)
			{
				Assert.That(envUi.ContextMenuId, Is.EqualTo(ReferenceItemMenuAuthority.EnvironmentMenuId));
				Assert.That(envAuthority.Owns(envUi.ContextMenuId), Is.True,
					"an environment's item menu is answered natively too");
			}
		}

		[Test]
		public void ReferenceItemMenu_OfASubentry_IsBuiltWithoutTheMediator()
		{
			MakeTwoSubentries();
			var request = ItemRequest(SubentriesField(), 0);
			var window = m_propertyTable.GetValue<XWindow>("window");
			var spy = new DisplaySpyColleague();
			window.Mediator.AddColleague(spy);
			try
			{
				var items = m_view.BuildReferenceItemMenu(request, out var itemUi);
				using (itemUi)
				{
					Assert.That(spy.Asked, Is.False, "no leaf of an owned item menu reaches the mediator");
					var jump = FindItem(items, "Show Entry in Lexicon" + SIL.FieldWorks.FdoUi.CmObjectUi.CtrlClickSuffix);
					Assert.That(jump?.Execute, Is.Not.Null, "the item's object UI answers the jump directly");
					Assert.That(FindItem(items, "Move Right")?.IsEnabled, Is.True);
				}
			}
			finally
			{
				window.Mediator.RemoveColleague(spy);
			}
		}

		// The item menus the authority renders, pinned per row kind; the trees were captured
		// from the mediator path the authority replaced, once the two were proved equivalent.
		[Test]
		public void ReferenceItemMenu_RendersThePinnedTree_ForEachRowKind()
		{
			MakeTwoSubentries();
			AddAnthropologyCategoryToSense();
			var fields = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Where(f => f.Kind == DetailFieldKind.ReferenceVector && f.Items.Count > 0).ToList();

			var rendered = new Dictionary<string, string>();
			foreach (var field in fields)
			{
				var request = ItemRequest(field, 0);
				var items = m_view.BuildReferenceItemMenu(request, out var itemUi);
				if (itemUi == null)
					continue;
				itemUi.Dispose();
				rendered[field.Field] = Describe(items);
			}
			foreach (var pair in rendered)
				TestContext.WriteLine("=== " + pair.Key + Environment.NewLine + pair.Value);

			Assert.That(rendered.Keys, Is.SupersetOf(new[] { "Subentries", "AnthroCodes" }));
			Assert.That(rendered["Subentries"], Is.EqualTo(PinnedSubentriesItemMenu));
			Assert.That(rendered["AnthroCodes"], Is.EqualTo(PinnedAnthroCodesItemMenu));
		}

		private const string Enabled = " [enabled=True checked=False executes=True]";
		private const string Disabled = " [enabled=False checked=False executes=False]";

		private static readonly string PinnedSubentriesItemMenu = string.Join(Environment.NewLine, new[]
		{
			"Show Entry in Lexicon" + SIL.FieldWorks.FdoUi.CmObjectUi.CtrlClickSuffix + Enabled,
			"---",
			"Show Entry in Concordance" + Enabled,
			"---",
			"Move Left" + Disabled,
			"Move Right" + Enabled
		});

		private static readonly string PinnedAnthroCodesItemMenu = string.Join(Environment.NewLine, new[]
		{
			"Show in Anthropology Category list" + SIL.FieldWorks.FdoUi.CmObjectUi.CtrlClickSuffix + Enabled,
			"Filter for Lexical Entries with this category" + Enabled,
			"Filter for Notebook Records with this category" + Enabled
		});

		// The mark reads the clicked item, so the menu offers it with no WinForms selection.
		[Test]
		public void ShowSubentryUnderComponent_OnAComplexFormsComponentsRow_TogglesThePrimaryLexeme()
		{
			MakeTwoSubentries();
			var complexForm = m_entry.ComplexFormEntries.First();
			var complexFormRef = complexForm.EntryRefsOS.Single();
			var field = DetailComposer.Compose(complexForm, Cache).Model.Fields.Single(f =>
				f.Field == ReferenceItemMenuAuthority.ComponentLexemesField && f.ObjectHvo == complexFormRef.Hvo);
			var request = ItemRequest(field, field.Items.ToList().FindIndex(i => i.Key == m_entry.Guid.ToString()));
			const string label = "Show Subentry under this Component";

			var items = m_view.BuildReferenceItemMenu(request, out var itemUi);
			using (itemUi)
			{
				var show = FindItem(items, label);
				Assert.That(show, Is.Not.Null, "the mark is offered for a component of a complex form");
				Assert.That(show.IsChecked, Is.True, "the component is a primary lexeme");
				show.Execute();
				DrainMediatorAndIdleQueues();
				Assert.That(complexFormRef.PrimaryLexemesRS, Does.Not.Contain(m_entry));
			}
			items = m_view.BuildReferenceItemMenu(request, out itemUi);
			using (itemUi)
			{
				var show = FindItem(items, label);
				Assert.That(show.IsChecked, Is.False);
				show.Execute();
				DrainMediatorAndIdleQueues();
				Assert.That(complexFormRef.PrimaryLexemesRS, Is.EqualTo(new ICmObject[] { m_entry }));
			}
		}

		[Test]
		public void AnthropologyCategoryItem_OffersTheFilterJumps_AndItsListJumpAsTheDefault()
		{
			var sense = AddAnthropologyCategoryToSense();
			var field = DetailComposer.Compose(m_entry, Cache).Model.Fields.Single(f =>
				f.Field == AnthroItemFilterLink.FieldName && f.ObjectHvo == sense.Hvo);

			var category = sense.AnthroCodesRC.First();
			var items = m_view.BuildReferenceItemMenu(ItemRequest(field, 0), out var itemUi);
			using (itemUi)
			{
				var lexicon = FindItem(items, "Filter for Lexical Entries with this category");
				var notebook = FindItem(items, "Filter for Notebook Records with this category");
				Assert.That(lexicon?.Execute, Is.Not.Null, "the lexicon filter jump is offered and runs");
				Assert.That(notebook?.Execute, Is.Not.Null, "the notebook filter jump is offered and runs");
				var link = CaptureFollowLink(lexicon.Execute);
				Assert.That(link, Is.Not.Null, "the filter jump posts a link");
				Assert.That(link.ToolName, Is.EqualTo("lexiconEdit"));
				Assert.That(link.PropertyTableEntries.Single(p => p.name == "HvoOfAnthroItem").value,
					Is.EqualTo(category.Hvo.ToString()), "the link filters on the clicked category");
				var listJump = items.FirstOrDefault(i => !i.IsSeparator
					&& i.Label.StartsWith("Show in ", StringComparison.Ordinal));
				Assert.That(listJump?.Label, Does.EndWith(SIL.FieldWorks.FdoUi.CmObjectUi.CtrlClickSuffix),
					"the category's own list jump is the Ctrl+click default");
				Assert.That(FindItem(items, "Move Right"), Is.Null, "a category collection has no order to move in");
			}

			MakeTwoSubentries();
			items = m_view.BuildReferenceItemMenu(ItemRequest(SubentriesField(), 0), out itemUi);
			using (itemUi)
			{
				Assert.That(FindItem(items, "Filter for Lexical Entries with this category"), Is.Null,
					"the filter jumps belong to the Anthropology Categories row alone");
			}
		}

		// The first sense gains one anthropology category, so the entry composes an Anthropology
		// Categories row with one item.
		private ILexSense AddAnthropologyCategoryToSense()
		{
			var sense = m_entry.SensesOS[0];
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (Cache.LangProject.AnthroListOA == null)
					Cache.LangProject.AnthroListOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				var category = Cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>().Create();
				Cache.LangProject.AnthroListOA.PossibilitiesOS.Add(category);
				category.Name.SetAnalysisDefaultWritingSystem("Kinship");
				sense.AnthroCodesRC.Add(category);
			});
			DrainMediatorAndIdleQueues();
			return sense;
		}

		// An allomorph carrying one environment: PhoneEnv is a reference COLLECTION, which
		// resolves to a different object UI, and so to a different menu, than a sequence.
		private DetailField EnvironmentsFieldWithOneItem(string saved = "/_#")
		{
			IMoStemAllomorph allomorph = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(allomorph);
				allomorph.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("command-allomorph", Cache.DefaultVernWs));
				var env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				Cache.LanguageProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
				env.StringRepresentation =
					TsStringUtils.MakeString(saved, Cache.DefaultVernWs);
				allomorph.PhoneEnvRC.Add(env);
			});
			DrainMediatorAndIdleQueues();
			RefreshedDetailFieldCount();

			var field = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.SingleOrDefault(f => f.Field == "PhoneEnv" && f.ObjectHvo == allomorph.Hvo);
			Assert.That(field, Is.Not.Null, "the allomorph composes an environments row");
			Assert.That(field.Items, Has.Count.EqualTo(1));
			return field;
		}

		/// <summary>
		/// A reference collection resolves to the collection object UI, which is the only one
		/// that names the environments menu. The base UI names the generic menu, whose commands
		/// are all gated to other classes -- so it builds nothing and the right-click shows no
		/// menu at all.
		/// </summary>
		[Test]
		public void AnEnvironmentItem_ResolvesToTheMenuThatCarriesItsCommands()
		{
			var request = ItemRequest(EnvironmentsFieldWithOneItem(), 0);

			using (var ui = m_view.ResolveItemUi(request))
			{
				Assert.That(ui, Is.Not.Null, "the clicked environment resolves to an object UI");
				Assert.That(((SIL.FieldWorks.FdoUi.ReferenceBaseUi)ui).ContextMenuId,
					Is.EqualTo("mnuEnvReferenceChoices"),
					"the generic menu carries no environment command, so it would build empty");
			}

			var items = m_view.BuildReferenceItemMenu(request, out var colleague);
			try
			{
				Assert.That(items, Is.Not.Empty,
					"an empty menu is indistinguishable from the right-click doing nothing");
			}
			finally
			{
				colleague?.Dispose();
			}
		}

		// ===== Environment rows: the item menu and the two insert label menus =====

		private const string JumpToEnvironmentsLabel = "Show in Environments list";
		private const string DescribeErrorLabel = "Describe Error in Environment";
		private const string InsertSlashLabel = "Insert Environment slash";
		private const string InsertBarLabel = "Insert Environment bar";
		private const string InsertNaturalClassLabel = "Insert Natural Class";
		private const string InsertOptionalItemLabel = "Insert Optional Item";
		private const string InsertHashMarkLabel = "Insert Word Boundary";
		private static readonly string[] InsertLabels =
		{
			InsertSlashLabel, InsertBarLabel, InsertNaturalClassLabel, InsertOptionalItemLabel, InsertHashMarkLabel
		};

		private static void AssertInsertsEnabled(IReadOnlyList<DetailMenuItem> items, params string[] enabled)
		{
			foreach (var label in InsertLabels)
			{
				var item = FindItem(items, label);
				Assert.That(item, Is.Not.Null, "'{0}' is offered", label);
				var expected = Array.IndexOf(enabled, label) >= 0;
				Assert.That(item.IsEnabled, Is.EqualTo(expected), "'{0}' enabled", label);
				Assert.That(item.Execute != null, Is.EqualTo(expected), "'{0}' executes iff enabled", label);
			}
		}

		[Test]
		public void EnvironmentItemAuthority_AnswersEveryLeafOfItsMenu_AndTakesTheJumpAsDefault()
		{
			var field = EnvironmentsFieldWithOneItem();
			var authority = m_view.CreateReferenceItemAuthority(ItemRequest(field, 0), out var itemUi);
			using (itemUi)
			{
				var window = m_propertyTable.GetValue<XWindow>("window");
				var leaves = LeavesOf(ReferenceItemMenuAuthority.EnvironmentMenuId);
				Assert.That(leaves.Count, Is.EqualTo(7), "the jump, Describe Error and the five inserts");
				foreach (var leaf in leaves)
				{
					Assert.That(() => authority.Build(ReferenceItemMenuAuthority.EnvironmentMenuId, leaf),
						Throws.Nothing, "the authority does not answer leaf '{0}'", leaf.CommandId);
				}
				var items = XCoreMenuBridge.CreateMenuItems(window,
					new[] { ReferenceItemMenuAuthority.EnvironmentMenuId }, authority);
				Assert.That(items, Is.Not.Empty, "the whole menu builds through the bridge");
				Assert.That(authority.DefaultActivation, Is.Not.Null, "Ctrl+click runs the jump");
			}
		}

		[Test]
		public void EnvironmentItemMenu_IsBuiltWithoutTheMediator()
		{
			var field = EnvironmentsFieldWithOneItem();
			var window = m_propertyTable.GetValue<XWindow>("window");
			var spy = new DisplaySpyColleague();
			window.Mediator.AddColleague(spy);
			try
			{
				var items = m_view.BuildReferenceItemMenu(ItemRequest(field, 0), out var itemUi);
				using (itemUi)
				{
					Assert.That(spy.Asked, Is.False, "no leaf of the environment item menu reaches the mediator");
					var jump = FindItem(items, JumpToEnvironmentsLabel + SIL.FieldWorks.FdoUi.CmObjectUi.CtrlClickSuffix);
					Assert.That(jump?.IsEnabled, Is.True, "the item's object UI answers the jump directly");
					var describe = FindItem(items, DescribeErrorLabel);
					Assert.That(describe, Is.Not.Null);
					Assert.That(describe.IsEnabled, Is.False, "a well-formed environment has no error to describe");
					// The request carried no text editor, so nothing can be typed into.
					AssertInsertsEnabled(items);
				}
			}
			finally
			{
				window.Mediator.RemoveColleague(spy);
			}
		}

		[Test]
		public void EnvironmentItemMenu_EnablesTheInserts_ByTheEditorsCaret_AndTypesIntoIt()
		{
			var field = EnvironmentsFieldWithOneItem(); // its item reads "/_#"
			var afterSlash = new TextEditorStub { EditorText = "/_#", Caret = 1 };
			var items = m_view.BuildReferenceItemMenu(ItemRequest(field, 0, afterSlash), out var itemUi);
			using (itemUi)
			{
				AssertInsertsEnabled(items, InsertNaturalClassLabel, InsertOptionalItemLabel, InsertHashMarkLabel);
				FindItem(items, InsertOptionalItemLabel).Execute();
				Assert.That(afterSlash.Typed, Is.EqualTo(new[] { (1, 1, "()", 1) }),
					"the optional item is typed at the snapshotted caret, which lands between its parentheses");
			}

			var beforeSlash = new TextEditorStub { EditorText = "/_#", Caret = 0 };
			items = m_view.BuildReferenceItemMenu(ItemRequest(field, 0, beforeSlash), out itemUi);
			using (itemUi)
			{
				AssertInsertsEnabled(items);
			}

			var noSlashYet = new TextEditorStub { EditorText = "a", Caret = 1 };
			items = m_view.BuildReferenceItemMenu(ItemRequest(field, 0, noSlashYet), out itemUi);
			using (itemUi)
			{
				AssertInsertsEnabled(items, InsertSlashLabel);
				FindItem(items, InsertSlashLabel).Execute();
				Assert.That(noSlashYet.Typed, Is.EqualTo(new[] { (1, 1, "/", 0) }));
			}
		}

		[Test]
		public void EnvironmentsLabelMenu_IsFullyOwned_AndItsInsertsTypeIntoTheSlot()
		{
			var field = EnvironmentsFieldWithOneItem();
			var ids = LabelMenuIds(field);
			Assert.That(ids, Does.Contain(EnvironmentInsertMenuAuthority.EnvironmentsMenuId));

			// The caret sits in the empty slot for a new environment: no item is current.
			var slot = new TextEditorStub { EditorText = string.Empty, Caret = 0 };
			var authority = m_view.CreateMenuAuthority(LabelMenuRequest(field, slot));
			Assert.That(XCoreMenuBridge.OwnsAll(authority, ids), Is.True,
				"every id of the Environments label menu is answered natively");
			var items = BuildWithSpy(ids, authority, out var asked);
			Assert.That(asked, Is.False, "no leaf reaches the mediator");
			AssertInsertsEnabled(items, InsertSlashLabel);
			FindItem(items, InsertSlashLabel).Execute();
			Assert.That(slot.Typed, Is.EqualTo(new[] { (0, 0, "/", 0) }), "the slash starts the new environment");
			Assert.That(FindItem(items, "Always visible"), Is.Not.Null, "the per-object group rides along");

			// No current item and no editor: nothing to type into.
			items = BuildWithSpy(ids, m_view.CreateMenuAuthority(LabelMenuRequest(field, NoItem)), out _);
			AssertInsertsEnabled(items);
		}

		/// <summary>
		/// The String Representation row of an environment composes as Unsupported (its editor
		/// is a WinForms plugin slice) on a tool that is not in the Avalonia catalog yet. Its
		/// menu is owned all the same, with its inserts disabled.
		/// </summary>
		[Test]
		public void StringRepresentationLabelMenu_IsOwned_WithItsInsertsDisabled()
		{
			EnvironmentsFieldWithOneItem();
			var environment = Cache.LanguageProject.PhonologicalDataOA.EnvironmentsOS.Last();
			var field = DetailComposer.Compose(environment, Cache, layoutName: "Edit").Model.Fields
				.SingleOrDefault(f => f.Field == "StringRepresentation");
			Assert.That(field, Is.Not.Null, "the environment composes its String Representation row");
			Assert.That(field.Kind, Is.EqualTo(DetailFieldKind.Unsupported));
			Assert.That(field.MenuId, Is.EqualTo(EnvironmentInsertMenuAuthority.StringRepresentationMenuId));

			var ids = LabelMenuIds(field);
			var authority = m_view.CreateMenuAuthority(LabelMenuRequest(field, NoItem));
			Assert.That(XCoreMenuBridge.OwnsAll(authority, ids), Is.True);
			var items = BuildWithSpy(ids, authority, out var asked);
			Assert.That(asked, Is.False);
			AssertInsertsEnabled(items);
		}

		[Test]
		public void EnvironmentInsertAuthority_RejectsALeafItDoesNotAnswer()
		{
			var field = EnvironmentsFieldWithOneItem();
			var authority = new EnvironmentInsertMenuAuthority(LabelMenuRequest(field, NoItem), m_view);
			var foreignLeaf = LeafOf(RecordEditView.ObjectMenuId, ObjectMenuAuthority.HelpCommandId);

			Assert.That(() => authority.Build(EnvironmentInsertMenuAuthority.EnvironmentsMenuId, foreignLeaf),
				Throws.InvalidOperationException, "an owned id must be answered in full, never partially");
			Assert.That(() => authority.BuildList(EnvironmentInsertMenuAuthority.EnvironmentsMenuId,
				"WritingSystemOptionsForSlice"), Throws.InvalidOperationException,
				"neither owned id has a list submenu, so any list is refused");
		}

		[Test]
		public void DescribeError_IsEnabledForAMalformedEnvironment_WithTheExplanationWinFormsShows()
		{
			const string malformed = "/ _ [";
			var valid = EnvironmentsFieldWithOneItem();
			var allomorph = (IMoStemAllomorph)Cache.ServiceLocator.ObjectRepository.GetObject(valid.ObjectHvo);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var bad = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				Cache.LanguageProject.PhonologicalDataOA.EnvironmentsOS.Add(bad);
				bad.StringRepresentation = TsStringUtils.MakeString(malformed, Cache.DefaultVernWs);
				allomorph.PhoneEnvRC.Add(bad);
			});
			DrainMediatorAndIdleQueues();
			RefreshedDetailFieldCount();
			var field = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "PhoneEnv" && f.ObjectHvo == allomorph.Hvo);
			var badIndex = field.Items.ToList().FindIndex(i => i.Name.Contains("["));
			Assert.That(badIndex, Is.GreaterThanOrEqualTo(0));
			Assert.That(field.Items[badIndex].HasValidationMessage, Is.True, "precondition: the domain flags it");

			var items = m_view.BuildReferenceItemMenu(ItemRequest(field, badIndex), out var itemUi);
			using (itemUi)
			{
				var describe = FindItem(items, DescribeErrorLabel);
				Assert.That(describe?.IsEnabled, Is.True, "a malformed environment has an error to describe");
				Assert.That(describe.Execute, Is.Not.Null);
			}

			// What PhoneEnvReferenceView.ShowEnvironmentError would show for the same string,
			// built the way that view built it before the rule moved to EnvironmentErrors.
			var recognizer = new PhonEnvRecognizer(Cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
				Cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
			Assert.That(recognizer.Recognize(malformed), Is.False);
			StringServices.CreateErrorMessageFromXml(malformed, recognizer.ErrorMessage, out _, out var winFormsMessage);
			var shown = ((IEnvironmentMenuHost)m_view).DescribeEnvironmentError(malformed);
			TestContext.WriteLine("WinForms: " + winFormsMessage);
			TestContext.WriteLine("Avalonia: " + shown);
			Assert.That(shown, Is.EqualTo(winFormsMessage), "Describe Error shows the same explanation on both stacks");
			Assert.That(field.Items[badIndex].ValidationMessage, Is.EqualTo(winFormsMessage),
				"and the item's tooltip agrees");
		}

		/// <summary>
		/// Describe Error judges the text in the item's editor, unsaved edits included, as the
		/// WinForms view judges the text in its view; the inserts in the same menu do too.
		/// </summary>
		[Test]
		public void DescribeError_IsEnabled_WhenAnUnsavedEditBreaksAWellFormedEnvironment()
		{
			var field = EnvironmentsFieldWithOneItem("/_#");

			var broken = new TextEditorStub { EditorText = "/#", Caret = 2 };
			var items = m_view.BuildReferenceItemMenu(ItemRequest(field, 0, broken), out var itemUi);
			using (itemUi)
			{
				Assert.That(FindItem(items, DescribeErrorLabel)?.IsEnabled, Is.True,
					"the editor's text has lost its bar, so there is an error to describe");
			}
		}

		/// <summary>
		/// The saved environment is malformed but the editor holds the fix, so the saved verdict
		/// must not reach the menu: a stale error is worse than none.
		/// </summary>
		[Test]
		public void DescribeError_IsDisabled_WhenAnUnsavedEditFixesAMalformedEnvironment()
		{
			var field = EnvironmentsFieldWithOneItem("/#");
			Assert.That(field.Items[0].HasValidationMessage, Is.True, "precondition: the saved text is malformed");

			var fixedText = new TextEditorStub { EditorText = "/#_", Caret = 3 };
			var items = m_view.BuildReferenceItemMenu(ItemRequest(field, 0, fixedText), out var itemUi);
			using (itemUi)
			{
				Assert.That(FindItem(items, DescribeErrorLabel)?.IsEnabled, Is.False,
					"the editor's text has its bar back, so there is no error to describe");
			}
		}

		/// <summary>
		/// A tool's filter list reaches the composer through the view. The shipped Exception
		/// "Features" configuration (ProdRestrictEdit) names basicPlusFilter.xml, which withholds
		/// Status; composing through the view's own configuration, rather than a hand-built id
		/// set, is what shows the file, the property and the composer are joined.
		/// </summary>
		[Test]
		public void AToolsFilterList_ReachesTheComposer_ThroughTheView()
		{
			ICmPossibility restriction = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				restriction = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				Cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Add(restriction);
				restriction.Name.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("restriction", Cache.DefaultAnalWs));
			});
			var original = SwapToolConfiguration(null);
			try
			{
				var shipped = ProdRestrictEditParameters();
				Assert.That(shipped.Attributes["filterPath"], Is.Not.Null,
					"precondition: the shipped configuration names a filter list");

				var unfiltered = (XmlElement)shipped.Clone();
				unfiltered.RemoveAttribute("filterPath");
				SwapToolConfiguration(unfiltered);
				Assert.That(ComposedFields(restriction), Does.Contain("Status"),
					"control: without the filter list, the same view composes the Status row");

				SwapToolConfiguration(shipped);
				Assert.That(ComposedFields(restriction), Does.Not.Contain("Status"),
					"the view's filter list reached the composer and withheld the row");
			}
			finally
			{
				SwapToolConfiguration(original);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					if (restriction != null && restriction.IsValidObject)
						restriction.Delete();
				});
			}
		}

		private IReadOnlyList<string> ComposedFields(ICmObject obj)
			=> m_view.ComposeDetail(obj, showHidden: true).Model.Fields.Select(f => f.Field).ToList();

		// The record-edit parameters of the shipped ProdRestrictEdit tool: the node that carries
		// its filterPath and its layout.
		private static XmlElement ProdRestrictEditParameters()
		{
			var document = new XmlDocument();
			document.Load(Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer",
				"Configuration", "Grammar", "Edit", "toolConfiguration.xml"));
			var node = (XmlElement)document.SelectSingleNode(
				"//tool[@value='ProdRestrictEdit']//parameters[@filterPath]");
			Assert.That(node, Is.Not.Null, "precondition: the shipped ProdRestrictEdit parameters");
			return node;
		}

		// Installs a tool configuration on the view the way ReadParameters does, layout
		// included, and clears the memoized filter list. Returns the one it replaced; null
		// changes nothing.
		private XmlNode SwapToolConfiguration(XmlNode configuration)
		{
			var parameters = typeof(XCoreUserControl).GetField("m_configurationParameters",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var layout = typeof(RecordEditView).GetField("m_layoutName",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var memo = typeof(RecordEditView).GetField("m_hiddenSliceIds",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(parameters, Is.Not.Null);
			Assert.That(layout, Is.Not.Null);
			Assert.That(memo, Is.Not.Null);
			var previous = (XmlNode)parameters.GetValue(m_view);
			if (configuration != null)
			{
				parameters.SetValue(m_view, configuration);
				layout.SetValue(m_view, configuration.Attributes?["layout"]?.Value);
				memo.SetValue(m_view, null);
			}
			return previous;
		}

		[Test]
		public void SubentriesCtrlClick_ResolvesTheClickedEntry_AndRunsTheDefaultJumpPath()
		{
			MakeTwoSubentries();
			var field = SubentriesField();
			var targetHvo = Cache.ServiceLocator.ObjectRepository.GetObject(new Guid(field.Items[1].Key)).Hvo;
			var request = DetailMenuRequest.FromDefaultActivation(null, field,
				new ItemSelection { SelectedItemKey = field.Items[1].Key, SelectedItemIndex = 1 });

			// The host's half: the clicked item resolves to its own object UI, wired to this
			// view's mediator so its jump commands can run.
			using (var ui = m_view.ResolveItemUi(request))
			{
				Assert.That(ui, Is.Not.Null, "the clicked subentry resolves to an object UI");
				Assert.That(ui.Object.Hvo, Is.EqualTo(m_entry.Hvo), "the UI's root is the row's object");
				Assert.That(ui.Mediator, Is.Not.Null);
				Assert.That(ui.PropTable, Is.Not.Null);
				Assert.That(((SIL.FieldWorks.FdoUi.ReferenceBaseUi)ui).ContextMenuId, Is.EqualTo("mnuReferenceChoices"));
			}
			Assert.That(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(targetHvo), Is.True);

			// The default activation posts the first enabled jump's link for the clicked
			// subentry.
			var link = CaptureFollowLink(() => m_view.OnDetailItemMenuRequested(request));
			Assert.That(link, Is.Not.Null, "the Ctrl+click default runs a jump");
			Assert.That(link.ToolName, Is.EqualTo("lexiconEdit"), "Show Entry in Lexicon is the first enabled jump");
			Assert.That(link.TargetGuid, Is.EqualTo(new Guid(field.Items[1].Key)), "the jump targets the clicked subentry");
		}

		// Runs an action and returns the link it posts, once the post has been delivered.
		private FwLinkArgs CaptureFollowLink(Action action)
			=> FollowLinkSpy.Capture(m_propertyTable.GetValue<XWindow>("window").Mediator, action,
				DrainMediatorAndIdleQueues);

		[Test]
		public void SubentriesMoveRight_ThroughTheHost_ReordersTheRow()
		{
			MakeTwoSubentries();
			var field = SubentriesField();
			var before = field.Items.Select(i => i.Key).ToList();

			m_view.MoveReferenceItem(field, before[0], forward: true);
			DrainMediatorAndIdleQueues();

			var after = SubentriesField().Items.Select(i => i.Key).ToList();
			Assert.That(after, Is.EqualTo(new[] { before[1], before[0] }), "the first item moved right");
			Assert.That(RefreshedDetailFieldCount(), Is.GreaterThan(0),
				"the host re-shows the record after the coalesced completion");
		}

		// Help topics: every composed row that has a WinForms slice twin must resolve the SAME
		// topic id the slice generates, with the provider knowing nothing and knowing much.

		private sealed class PatternHelpTopicProvider : IHelpTopicProvider
		{
			private readonly Func<string, bool> _knows;
			public PatternHelpTopicProvider(Func<string, bool> knows) { _knows = knows; }
			public string GetHelpString(string id) => id != null && _knows(id) ? id : null;
			public string HelpFile => string.Empty;
		}

		// A lexical relation from the test entry to another, so the entry composes a relation
		// row whose WinForms twin is a Targets slice.
		private void MakeLexicalRelation()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stem = GetMorphTypeOrCreateOne("stem");
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				var other = AddLexeme(m_createdObjects, "command-entry-synonym", stem, "synonym", noun);
				var lexDb = Cache.LangProject.LexDbOA;
				if (lexDb.ReferencesOA == null)
					lexDb.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				var type = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
				lexDb.ReferencesOA.PossibilitiesOS.Add(type);
				type.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryCollection;
				type.Name.set_String(Cache.DefaultAnalWs, "Synonyms");
				var reference = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
				type.MembersOC.Add(reference);
				reference.TargetsRS.Add(m_entry);
				reference.TargetsRS.Add(other);
			});
			DrainMediatorAndIdleQueues();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void HelpTopics_ResolveLikeTheWinFormsSlices_ForEveryTwinnedRow(bool providerKnowsToolTopics)
		{
			AddSense("second gloss");
			MakeTwoSubentries();
			MakeLexicalRelation();
			// The provider decides which generated candidate wins; both sides read the same one.
			m_propertyTable.SetProperty("HelpTopicProvider",
				new PatternHelpTopicProvider(id => providerKnowsToolTopics && id.Contains("-lexiconEdit-")), false);
			m_propertyTable.SetPropertyPersistence("HelpTopicProvider", false);
			var fields = DetailComposer.Compose(m_entry, Cache).Model.Fields;
			var dataTree = ShowWinFormsDataTree();

			var compared = 0;
			var relationsCompared = 0;
			var mismatches = new List<string>();
			foreach (var sliceObj in dataTree.Slices)
			{
				if (!(sliceObj is Slice slice) || slice.Object == null || slice.Flid == 0)
					continue;
				var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
				var sliceField = mdc.FieldExists(slice.Flid) ? mdc.GetFieldName(slice.Flid) : null;
				if (sliceField == null)
					continue;
				// A relation's WinForms twin is a Targets slice on the relation; the row keeps
				// the
				// relations property as its field and names Targets in its help-topic source.
				var twin = sliceField == "Targets"
					? fields.FirstOrDefault(f => f.ObjectHvo == slice.Object.Hvo
						&& f.HelpTopicSource?.FieldName == "Targets")
					: fields.FirstOrDefault(f => f.ObjectHvo == slice.Object.Hvo
						&& string.Equals(f.Field, sliceField, StringComparison.Ordinal));
				if (twin == null)
					continue;
				compared++;
				if (sliceField == "Targets")
					relationsCompared++;
				var expected = slice.GetSliceHelpTopicID();
				var actual = m_view.ResolveHelpTopic(twin);
				if (!string.Equals(expected, actual, StringComparison.Ordinal))
					mismatches.Add($"{sliceField} on {slice.Object.ClassName}: slice '{expected}', row '{actual}'");
			}
			Assert.That(compared, Is.GreaterThan(5), "enough rows have a WinForms twin to make the comparison meaningful");
			Assert.That(relationsCompared, Is.GreaterThan(0), "the lexical relation's Targets slice found its row");
			Assert.That(mismatches, Is.Empty);
		}

		// Switches the view to the WinForms detail view, whose DataTree then shows the entry.
		private LegacyDataTree ShowWinFormsDataTree()
		{
			m_propertyTable.SetProperty("UIMode", "Legacy", true);
			DrainMediatorAndIdleQueues();
			Assert.That(GetField(m_view, "m_activeUIFramework"), Is.EqualTo(UIFramework.Legacy),
				"precondition: the view switched to the WinForms detail view");
			Assert.That(m_view.DatTree.Root, Is.EqualTo(m_entry), "precondition: the DataTree shows the entry");
			return m_view.DatTree;
		}

		// The reorder-vector menu's native authority, in every state a row can be in.

		private static readonly ItemSelection NoItem = new ItemSelection { SelectedItemKey = null, SelectedItemIndex = -1 };

		private static DetailMenuRequest LabelMenuRequest(DetailField field, IDetailItemSelection selection)
			=> DetailMenuRequest.FromAnchor(null, field, DetailMenuKind.SliceMenu, selection);

		private static string[] LabelMenuIds(DetailField field)
			=> RecordEditView.ComposeSliceMenuIds(field.MenuId, field.IsMultiStringRow).ToArray();

		// The entry's read-only Complex Forms row: it binds the reorder menu but composes as
		// text.
		private DetailField ComplexFormsField()
		{
			var field = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.SingleOrDefault(f => f.Field == "ComplexFormEntries");
			Assert.That(field, Is.Not.Null, "the entry composes a Complex Forms row");
			Assert.That(field.Kind, Is.EqualTo(DetailFieldKind.Text), "precondition: the row is read-only text");
			Assert.That(field.MenuId, Is.EqualTo(ReorderVectorMenuAuthority.MenuId));
			return field;
		}

		// One line per item -- label, enabled, checked, whether it can execute -- children
		// indented, so two trees compare as text and a mismatch reads at a glance.
		private static string Describe(IReadOnlyList<DetailMenuItem> items, string indent = "")
		{
			var lines = new List<string>();
			foreach (var item in items)
			{
				if (item.IsSeparator)
				{
					lines.Add(indent + "---");
					continue;
				}
				lines.Add(string.Format("{0}{1} [enabled={2} checked={3} executes={4}]", indent, item.Label,
					item.IsEnabled, item.IsChecked, item.Execute != null));
				if (item.Children.Count > 0)
					lines.Add(Describe(item.Children, indent + "  "));
			}
			return string.Join(Environment.NewLine, lines);
		}

		[Test]
		public void ReorderVectorLabelMenu_WithNoCurrentItem_OffersTheMovesDisabled_AndAlphabeticalOrder()
		{
			MakeTwoSubentries();
			var field = SubentriesField();
			Assert.That(field.MenuId, Is.EqualTo(ReorderVectorMenuAuthority.MenuId), "precondition: Subentries binds the reorder menu");

			var menu = Describe(BuildItems(LabelMenuIds(field), m_view.CreateMenuAuthority(LabelMenuRequest(field, NoItem))));

			Assert.That(menu, Does.Contain("Move Left" + Disabled), "no current item: Move Left offered, disabled");
			Assert.That(menu, Does.Contain("Move Right" + Disabled), "no current item: Move Right offered, disabled");
			Assert.That(menu, Does.Contain("Alphabetical Order" + Enabled), "reorder='true': Alphabetical Order offered and enabled");
		}

		// Pins the merge of the row's own menu with the shared object menu.
		[Test]
		public void ReorderVectorLabelMenu_KeepsTheSeparator_BetweenTheReorderMenu_AndTheObjectMenu()
		{
			MakeTwoSubentries();
			var field = SubentriesField();
			var ids = LabelMenuIds(field);

			var items = BuildItems(ids, m_view.CreateMenuAuthority(LabelMenuRequest(field, NoItem))).ToList();

			var alphabetical = items.FindIndex(i => i.Label == "Alphabetical Order");
			var visibility = items.FindIndex(i => i.Label == "Field Visibility");
			Assert.That(alphabetical, Is.GreaterThanOrEqualTo(0), "the row's own menu ends with Alphabetical Order");
			Assert.That(visibility, Is.EqualTo(alphabetical + 2), "exactly one item lies between the two source menus");
			Assert.That(items[alphabetical + 1].IsSeparator, Is.True,
				"mnuDataTree-Object's leading separator divides it from the row's own menu");
		}

		[Test]
		public void ReorderVectorLabelMenu_ReadOnlyComplexFormsRow_HidesTheMoves_AndKeepsAlphabeticalOrder()
		{
			MakeTwoSubentries();
			var field = ComplexFormsField();

			var menu = Describe(BuildItems(LabelMenuIds(field), m_view.CreateMenuAuthority(LabelMenuRequest(field, NoItem))));

			Assert.That(menu, Does.Not.Contain("Move Left"), "WinForms hides the moves on this row by its label");
			Assert.That(menu, Does.Not.Contain("Move Right"));
			Assert.That(menu, Does.Contain("Alphabetical Order" + Enabled), "reorder='true': the order can still be reset");
		}

		[Test]
		public void ReorderVectorLabelMenu_NativeAuthority_ForTheFirstItem_EnablesOnlyMoveRight()
		{
			MakeTwoSubentries();
			var field = SubentriesField();
			var first = new ItemSelection { SelectedItemKey = field.Items[0].Key, SelectedItemIndex = 0 };

			var items = BuildItems(LabelMenuIds(field), m_view.CreateReorderVectorAuthority(LabelMenuRequest(field, first)));

			Assert.That(FindItem(items, "Move Left").IsEnabled, Is.False, "the first item cannot move left");
			Assert.That(FindItem(items, "Move Right").IsEnabled, Is.True, "the first item can move right");
			Assert.That(FindItem(items, "Alphabetical Order").IsEnabled, Is.True);
		}

		[Test]
		public void ReorderVectorMenu_RealSequenceRow_OffersTheMoves_ButNoAlphabeticalOrder()
		{
			// A real reference sequence reorders but has no virtual ordering to discard.
			var field = new DetailField("Components", "Components", "ComponentLexemes", null,
				DetailFieldKind.ReferenceVector, EditorClassification.Known, "Components", null, HostRouting.Inherit,
				null, null, null, isEditable: true, menuId: ReorderVectorMenuAuthority.MenuId, objectHvo: m_entry.Hvo,
				items: new[] { new DetailChoiceOption("a", "A"), new DetailChoiceOption("b", "B") },
				canReorderItems: true, canResetItemOrder: false);
			var second = new ItemSelection { SelectedItemKey = "b", SelectedItemIndex = 1 };

			var items = BuildItems(new[] { ReorderVectorMenuAuthority.MenuId },
				m_view.CreateReorderVectorAuthority(LabelMenuRequest(field, second)));

			Assert.That(FindItem(items, "Move Left").IsEnabled, Is.True);
			Assert.That(FindItem(items, "Move Right").IsEnabled, Is.False, "the last item cannot move right");
			Assert.That(FindItem(items, "Alphabetical Order"), Is.Null, "no reorder='true', no Alphabetical Order");
		}

		// Every leaf under the owned menu, submenus included, must be one the authority knows.
		[Test]
		public void ReorderVectorAuthority_AnswersEveryLeafOfItsMenu()
		{
			var field = new DetailField("Row", "Row", "Subentries", null, DetailFieldKind.ReferenceVector,
				EditorClassification.Known, "Row", null, HostRouting.Inherit, null, null, null,
				menuId: ReorderVectorMenuAuthority.MenuId, objectHvo: m_entry.Hvo,
				items: new[] { new DetailChoiceOption("a", "A") }, canReorderItems: true, canResetItemOrder: true);
			var authority = m_view.CreateReorderVectorAuthority(LabelMenuRequest(field, NoItem));

			var leaves = LeavesOf(ReorderVectorMenuAuthority.MenuId);
			Assert.That(leaves, Is.Not.Empty, "the menu must define at least one leaf");
			foreach (var leaf in leaves)
			{
				Assert.That(() => authority.Build(ReorderVectorMenuAuthority.MenuId, leaf), Throws.Nothing,
					"the authority does not answer leaf '{0}'", leaf.CommandId);
			}
		}

		// Every command leaf of a menu id, submenus included, as the bridge resolves it from
		// the configuration.
		private IReadOnlyList<DetailMenuLeaf> LeavesOf(string menuId)
			=> XCoreMenuBridge.ResolveMenu(m_propertyTable.GetValue<XWindow>("window"), menuId).Leaves;

		// One command leaf of a menu id, by command id.
		private DetailMenuLeaf LeafOf(string menuId, string commandId)
			=> LeavesOf(menuId).First(l => l.CommandId == commandId);

		[Test]
		public void ReorderVectorAuthority_RejectsALeafItDoesNotAnswer()
		{
			MakeTwoSubentries();
			var authority = m_view.CreateReorderVectorAuthority(LabelMenuRequest(SubentriesField(), NoItem));
			var foreignLeaf = LeafOf(RecordEditView.ObjectMenuId, "CmdDataTree-Help");

			Assert.That(() => authority.Build(ReorderVectorMenuAuthority.MenuId, foreignLeaf),
				Throws.InvalidOperationException, "an owned id must be answered in full, never partially");
		}

		[Test]
		public void LabelMenu_IsFullyOwned_OnEveryRowKind()
		{
			MakeTwoSubentries();
			var subentries = SubentriesField();
			var authority = m_view.CreateMenuAuthority(LabelMenuRequest(subentries, NoItem));

			Assert.That(XCoreMenuBridge.OwnsAll(authority, LabelMenuIds(subentries)), Is.True,
				"the reorder menu and the shared object menu are both answered natively");
			var helpBound = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.First(f => f.MenuId == ObjectMenuAuthority.HelpMenuId);
			Assert.That(XCoreMenuBridge.OwnsAll(authority, LabelMenuIds(helpBound)), Is.True,
				"a row bound to the empty Help menu merges only owned ids");
			var multiString = OwnedMultiStringField();
			Assert.That(XCoreMenuBridge.OwnsAll(m_view.CreateMenuAuthority(LabelMenuRequest(multiString, NoItem)),
				LabelMenuIds(multiString)), Is.True,
				"a multi-string row's menu, writing-system list included, is answered natively");
			var lexemeForm = LexemeFormField();
			Assert.That(m_view.CreateWritingSystemsMenuAuthority(lexemeForm).Owns(WritingSystemsMenuAuthority.MenuId),
				Is.True, "the shared multi-string group is owned on the Lexeme Form row too");
			Assert.That(XCoreMenuBridge.OwnsAll(m_view.CreateMenuAuthority(LabelMenuRequest(lexemeForm, NoItem)),
				LabelMenuIds(lexemeForm)), Is.True,
				"the Lexeme Form row's menu, its own jump, swap and convert leaves included, is answered natively");
			Assert.That(m_view.CreateWritingSystemsMenuAuthority(subentries).Owns(WritingSystemsMenuAuthority.MenuId),
				Is.False, "a row that is not multi-string never claims the multi-string menu");
			Assert.That(XCoreMenuBridge.OwnsAll(null, new[] { ReorderVectorMenuAuthority.MenuId }), Is.False);
		}

		[Test]
		public void AlphabeticalOrder_ThroughTheHost_DiscardsTheVirtualOrdering()
		{
			MakeTwoSubentries();
			var field = SubentriesField();
			Assert.That(field.CanResetItemOrder, Is.True, "reorder='true' composes as a resettable order");
			var defaultOrder = field.Items.Select(i => i.Key).ToList();
			m_view.MoveReferenceItem(field, defaultOrder[0], forward: true);
			DrainMediatorAndIdleQueues();
			Assert.That(SubentriesField().Items.Select(i => i.Key), Is.EqualTo(new[] { defaultOrder[1], defaultOrder[0] }),
				"precondition: a virtual ordering now overrides the default order");

			m_view.ResetReferenceOrder(SubentriesField());
			DrainMediatorAndIdleQueues();

			Assert.That(SubentriesField().Items.Select(i => i.Key), Is.EqualTo(defaultOrder),
				"the stored ordering is gone; the items are back in the property's default order");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(SubentriesField().Items.Select(i => i.Key), Is.EqualTo(new[] { defaultOrder[1], defaultOrder[0] }),
				"the reset is its own undo step");
		}

		// The bridge's owned-menu path: an owned id is populated and converted without the
		// mediator, submenus included.

		// Answers every leaf of the ids it owns with the leaf's own label, hiding the command
		// ids it is told to hide.
		private sealed class EchoAuthority : IDetailMenuAuthority
		{
			private readonly HashSet<string> _owned;
			private readonly HashSet<string> _hidden;
			public EchoAuthority(IEnumerable<string> owned, params string[] hidden)
			{
				_owned = new HashSet<string>(owned, StringComparer.Ordinal);
				_hidden = new HashSet<string>(hidden, StringComparer.Ordinal);
			}
			public bool Owns(string menuId) => _owned.Contains(menuId);
			public DetailMenuItem Build(string menuId, DetailMenuLeaf leaf)
				=> _hidden.Contains(leaf.CommandId) ? null
					: new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true);

			// Echoes the list's id as one item, so a test can see where it spliced in.
			public IReadOnlyList<DetailMenuItem> BuildList(string menuId, string listId)
				=> new[] { new DetailMenuItem("list:" + listId, isEnabled: true) };
		}

		// Records whether the mediator asked anyone to display the Always-visible command or a
		// Show-in-tool jump.
		private sealed class DisplaySpyColleague : IxCoreColleague
		{
			public bool Asked { get; private set; }
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;
			public bool OnDisplayShowFieldAlwaysVisible(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
		}

		private IReadOnlyList<DetailMenuItem> BuildWithSpy(string[] ids, IDetailMenuAuthority authority,
			out bool mediatorAsked)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var spy = new DisplaySpyColleague();
			window.Mediator.AddColleague(spy);
			try
			{
				var items = XCoreMenuBridge.CreateMenuItems(window, ids, authority);
				mediatorAsked = spy.Asked;
				return items;
			}
			finally
			{
				window.Mediator.RemoveColleague(spy);
			}
		}

		[Test]
		public void OwnedMenu_WithSubmenus_IsBuiltWithoutAskingTheMediator()
		{
			var ids = new[] { RecordEditView.ObjectMenuId };

			var items = BuildWithSpy(ids, new EchoAuthority(ids), out var asked);

			Assert.That(asked, Is.False, "an owned id never reaches the mediator, submenu leaves included");
			var visibility = FindItem(items, "Field Visibility");
			var move = FindItem(items, "Move Field");
			Assert.That(visibility, Is.Not.Null, "the Field Visibility submenu is built from its configuration");
			Assert.That(visibility.Children.Select(c => c.Label),
				Is.EqualTo(new[] { "Always visible", "Normally hidden, unless non-empty", "Normally hidden" }));
			Assert.That(move?.Children.Select(c => c.Label), Is.EqualTo(new[] { "Move Up", "Move Down" }));
			Assert.That(FindItem(items, "Help..."), Is.Not.Null);
			Assert.That(items[0].IsSeparator, Is.False, "the menu's leading separator is trimmed");
		}

		[Test]
		public void UnownedMenu_ShowsItsCommandsDisabled_WithoutAskingTheMediator()
		{
			var items = BuildWithSpy(new[] { RecordEditView.ObjectMenuId }, null, out var asked);

			Assert.That(asked, Is.False, "an id no authority owns asks the mediator nothing either");
			var visibility = FindItem(items, "Field Visibility");
			Assert.That(visibility?.Children.Select(c => c.Label),
				Is.EqualTo(new[] { "Always visible", "Normally hidden, unless non-empty", "Normally hidden" }),
				"a submenu keeps its label and its commands");
			var leaves = visibility.Children.Concat(FindItem(items, "Move Field").Children)
				.Concat(new[] { FindItem(items, "Help...") }).ToList();
			Assert.That(leaves.Select(l => l.IsEnabled || l.Execute != null), Is.All.False, "no command can run");
			Assert.That(items[0].IsSeparator, Is.False, "the menu's leading separator is trimmed");

			// Nothing supplies a list submenu's items, so only its configured commands show.
			items = BuildWithSpy(new[] { RecordEditView.MultiStringSliceMenuId }, null, out asked);
			Assert.That(asked, Is.False);
			Assert.That(FindItem(items, "Writing Systems")?.Children.Select(c => c.Label),
				Is.EqualTo(new[] { "Show all right now", "Configure..." }));
		}

		// The XML walk sees what ChoiceGroup.Populate sees: top-level leaves exactly, nested
		// ones as a subset (the mediator drops unshown submenus). No item may name a missing
		// command.
		[Test]
		public void ResolveMenu_ReadsWhatChoiceGroupPopulates_ForEveryContextMenuId()
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var configuration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			var ids = configuration.SelectNodes("//contextMenus/menu[@id]").Cast<XmlNode>()
				.Select(n => n.Attributes["id"].Value).Distinct().ToList();
			Assert.That(ids.Count, Is.GreaterThan(48), "the window configuration defines the tools' context menus");

			var undefinedCommands = new List<string>();
			foreach (var id in ids)
			{
				var resolved = XCoreMenuBridge.ResolveMenu(window, id);
				var group = window.GetContextMenuChoiceGroup(new[] { id });
				group.PopulateNow();

				var groupTopLevel = group.OfType<CommandChoice>().Select(Describe).ToList();
				var resolvedTopLevel = resolved.Entries.Where(e => e.Leaf != null || IsUndefinedCommand(e))
					.Select(Describe).ToList();
				Assert.That(resolvedTopLevel, Is.EqualTo(groupTopLevel), "menu '{0}': top-level leaves", id);
				var resolvedLeaves = resolved.Leaves.Select(l => Describe(l.CommandId, l.Label))
					.Concat(AllEntries(resolved.Entries).Where(IsUndefinedCommand).Select(Describe)).ToList();
				Assert.That(resolvedLeaves, Is.SupersetOf(ChoiceLeaves(group).Select(Describe)),
					"menu '{0}': nested leaves", id);
				Assert.That(resolved.ListIds, Is.EquivalentTo(ListIds(group)), "menu '{0}': list submenus", id);
				undefinedCommands.AddRange(resolved.Unanswerable.Where(u => u.StartsWith("command", StringComparison.Ordinal))
					.Select(u => id + ": " + u));
			}
			Assert.That(undefinedCommands, Is.Empty,
				"context-menu items naming an undefined command (ChoiceGroup fails on them at display time)");
		}

		// Walks the ChoiceGroup side exactly as the configuration-walk side describes a leaf;
		// a CommandChoice whose command is undefined has no label to read.
		private static string Describe(CommandChoice choice)
		{
			try
			{
				return Describe(choice.HelpId, choice.Label);
			}
			catch (SIL.Utils.ConfigurationException)
			{
				return Describe(choice.HelpId, null);
			}
		}

		private static string Describe(DetailMenuEntry entry)
			=> entry.Leaf != null
				? Describe(entry.Leaf.CommandId, entry.Leaf.Label)
				: Describe(UndefinedCommandId(entry), null);

		private static string Describe(string commandId, string label)
			=> label == null ? commandId : commandId + " = " + label;

		private static bool IsUndefinedCommand(DetailMenuEntry entry) => UndefinedCommandId(entry) != null;

		// The reason text names the id: "command 'X' is not defined".
		private static string UndefinedCommandId(DetailMenuEntry entry)
		{
			const string prefix = "command '";
			if (entry.Unanswerable == null || !entry.Unanswerable.StartsWith(prefix, StringComparison.Ordinal))
				return null;
			var end = entry.Unanswerable.IndexOf('\'', prefix.Length);
			return entry.Unanswerable.Substring(prefix.Length, end - prefix.Length);
		}

		private static IEnumerable<DetailMenuEntry> AllEntries(IReadOnlyList<DetailMenuEntry> entries)
		{
			foreach (var entry in entries)
			{
				yield return entry;
				if (entry.Children == null)
					continue;
				foreach (var nested in AllEntries(entry.Children))
					yield return nested;
			}
		}

		// Every command leaf of a populated choice group, submenus included.
		private static IEnumerable<CommandChoice> ChoiceLeaves(ChoiceGroup group)
		{
			foreach (var member in group)
			{
				if (member is ChoiceGroup submenu)
				{
					submenu.PopulateNow();
					foreach (var leaf in ChoiceLeaves(submenu))
						yield return leaf;
				}
				else if (member is CommandChoice leaf)
				{
					yield return leaf;
				}
			}
		}

		private static IEnumerable<string> ListIds(ChoiceGroup group)
		{
			foreach (var member in group)
			{
				if (!(member is ChoiceGroup submenu))
					continue;
				if (!string.IsNullOrEmpty(submenu.ListId))
				{
					yield return submenu.ListId;
					continue;
				}
				foreach (var listId in ListIds(submenu))
					yield return listId;
			}
		}

		[Test]
		public void OwnedSubmenu_WhoseLeavesAreAllHidden_IsOmitted()
		{
			var ids = new[] { RecordEditView.ObjectMenuId };
			var authority = new EchoAuthority(ids, "CmdAlwaysVisible", "CmdIfData", "CmdNormallyHidden");

			var items = BuildWithSpy(ids, authority, out _);

			Assert.That(FindItem(items, "Field Visibility"), Is.Null, "a submenu with no visible leaf is dropped");
			Assert.That(FindItem(items, "Move Field"), Is.Not.Null);
		}

		// A sequence item header has no node of its own, so the locator answers false without
		// compiling or logging; a ghost row stands in for its empty sequence node.
		[Test]
		public void TryLocateOverrideTarget_SkipsASequenceItemHeader_AndMapsAGhostRowToItsNode()
		{
			var sense = m_entry.SensesOS[0];
			Assert.That(sense.ExamplesOS, Is.Empty, "precondition: the sense composes a ghost Example row");
			var fields = DetailComposer.Compose(m_entry, Cache).Model.Fields;
			var senseHeader = fields.First(f => f.ObjectHvo == sense.Hvo && f.Field == "Senses");
			Assert.That(senseHeader.StableId, Does.Contain("@").And.Contain("/item"),
				"precondition: the header carries the composer's item path after the hvo");
			var ghost = fields.Single(f => f.ObjectHvo == sense.Hvo && f.Field == "Examples"
				&& f.StableId.EndsWith(DetailField.GhostStableIdSuffix, StringComparison.Ordinal));
			var nodeId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(ghost.StableId);
			nodeId = nodeId.Substring(0, nodeId.Length - DetailField.GhostStableIdSuffix.Length);

			Assert.That(m_view.TryLocateOverrideTarget(senseHeader, out _, out _), Is.False,
				"a sequence item header is never an override target");
			Assert.That(m_view.TryLocateOverrideTarget(ghost, out var templateId, out var location), Is.True,
				"the ghost row locates the node it stands in for");
			Assert.That(templateId, Is.EqualTo(nodeId));
			Assert.That(location, Is.Not.Null);
		}

		[Test]
		public void OwnedMenu_WithAListSubmenu_TakesItsItemsFromTheAuthority()
		{
			// The writing-system list is the one list-populated submenu under an owned id.
			var ids = new[] { RecordEditView.MultiStringSliceMenuId };
			var items = BuildWithSpy(ids, new EchoAuthority(ids), out var askedMediator);

			var writingSystems = FindItem(items, "Writing Systems");
			Assert.That(writingSystems, Is.Not.Null, "the owned submenu survives");
			Assert.That(writingSystems.Children.Select(c => c.Label),
				Does.Contain("list:WritingSystemOptionsForSlice"),
				"the authority's list items splice in where the list submenu sat");
			Assert.That(askedMediator, Is.False,
				"an owned id asks the mediator nothing, the list submenu included");
		}

		// The multi-string menu's native authority: the Writing Systems submenu answered from
		// the row and the shared rule, the rest delegated to the per-object authority.

		[Test]
		public void WritingSystemsMenuAuthority_AnswersEveryLeafOfItsMenu()
		{
			var authority = m_view.CreateWritingSystemsMenuAuthority(LexemeFormField());
			var window = m_propertyTable.GetValue<XWindow>("window");
			var menu = XCoreMenuBridge.ResolveMenu(window, WritingSystemsMenuAuthority.MenuId);

			Assert.That(menu.Leaves.Select(l => l.CommandId), Is.EquivalentTo(new[]
			{
				WritingSystemsMenuAuthority.ShowAllCommandId, WritingSystemsMenuAuthority.ConfigureCommandId,
				ObjectMenuAuthority.AlwaysVisibleCommandId, ObjectMenuAuthority.IfDataCommandId,
				ObjectMenuAuthority.NormallyHiddenCommandId, ObjectMenuAuthority.MoveFieldUpCommandId,
				ObjectMenuAuthority.MoveFieldDownCommandId, ObjectMenuAuthority.HelpCommandId
			}), "the shipped menu defines exactly the leaves the authority knows");
			foreach (var leaf in menu.Leaves)
			{
				Assert.That(() => authority.Build(WritingSystemsMenuAuthority.MenuId, leaf), Throws.Nothing,
					"the authority does not answer leaf '{0}'", leaf.CommandId);
			}
			// The list submenu has no configured leaves; the authority supplies its items.
			Assert.That(menu.ListIds, Is.EqualTo(new[] { WritingSystemsMenuAuthority.WritingSystemListId }),
				"the writing-system list is the menu's one list submenu");
			Assert.That(authority.BuildList(WritingSystemsMenuAuthority.MenuId,
				WritingSystemsMenuAuthority.WritingSystemListId), Is.Not.Empty,
				"the writing-system list offers the row's options");
		}

		[Test]
		public void WritingSystemsMenuAuthority_RejectsWhatItDoesNotAnswer()
		{
			var authority = m_view.CreateWritingSystemsMenuAuthority(LexemeFormField());
			var foreignLeaf = LeafOf(ReorderVectorMenuAuthority.MenuId, ReorderVectorMenuAuthority.AlphabeticalOrderCommandId);

			Assert.That(() => authority.Build(WritingSystemsMenuAuthority.MenuId, foreignLeaf),
				Throws.InvalidOperationException, "a foreign leaf is refused, never left to the mediator");
			Assert.That(() => authority.BuildList(WritingSystemsMenuAuthority.MenuId, "SomeOtherList"),
				Throws.InvalidOperationException, "a foreign list is refused too");
		}

		[Test]
		public void MultiStringMenu_IsBuiltWithoutTheMediator()
		{
			UseHelpProvider(KnowsEveryTopic);
			var field = OwnedMultiStringField();

			var items = BuildWithSpy(LabelMenuIds(field),
				m_view.CreateMenuAuthority(LabelMenuRequest(field, NoItem)), out var asked);

			Assert.That(asked, Is.False, "nothing in a multi-string row's label menu reaches the mediator");
			var writingSystems = FindItem(items, "Writing Systems");
			Assert.That(writingSystems, Is.Not.Null);
			var toggles = writingSystems.Children
				.Where(c => c.Label != "Show all right now" && c.Label != "Configure...").ToList();
			Assert.That(toggles, Is.Not.Empty, "the row's writing systems are offered");
			Assert.That(toggles.Count(c => c.IsChecked), Is.GreaterThan(0), "the shown ones are checked");
			Assert.That(toggles.Where(c => c.IsEnabled).Select(c => c.Execute), Is.All.Not.Null,
				"an enabled toggle executes");
			Assert.That(FindItem(items, "Show all right now")?.Execute, Is.Not.Null);
			Assert.That(FindItem(items, "Configure...")?.Execute, Is.Not.Null);
		}

		// The per-object menu's native authority: Field Visibility, Move Field and Help answered
		// from the row and the override layer.

		private static IHelpTopicProvider KnowsEveryTopic => new PatternHelpTopicProvider(id => true);

		private void UseHelpProvider(IHelpTopicProvider provider)
		{
			m_propertyTable.SetProperty("HelpTopicProvider", provider, false);
			m_propertyTable.SetPropertyPersistence("HelpTopicProvider", false);
		}

		// The label menu exactly as OnDetailMenuRequested builds it, from the row's authorities.
		private IReadOnlyList<DetailMenuItem> BuildAsTheHostDoes(string[] ids, DetailField field)
			=> m_view.BuildMenuItems(ids, LabelMenuRequest(field, NoItem));

		[Test]
		public void ObjectMenuAuthority_AnswersEveryLeafOfItsMenus()
		{
			var authority = m_view.CreateObjectMenuAuthority(LexemeFormField());
			var leaves = LeavesOf(ObjectMenuAuthority.MenuId);

			Assert.That(leaves.Select(l => l.CommandId), Is.EquivalentTo(new[]
			{
				ObjectMenuAuthority.AlwaysVisibleCommandId, ObjectMenuAuthority.IfDataCommandId,
				ObjectMenuAuthority.NormallyHiddenCommandId, ObjectMenuAuthority.MoveFieldUpCommandId,
				ObjectMenuAuthority.MoveFieldDownCommandId, ObjectMenuAuthority.HelpCommandId
			}), "the shipped menu defines exactly the leaves the authority knows");
			foreach (var leaf in leaves)
			{
				Assert.That(() => authority.Build(ObjectMenuAuthority.MenuId, leaf), Throws.Nothing,
					"the authority does not answer leaf '{0}'", leaf.CommandId);
			}

			Assert.That(LeavesOf(ObjectMenuAuthority.HelpMenuId), Is.Empty, "mnuDataTree-Help defines no leaf of its own");
			Assert.That(authority.Owns(ObjectMenuAuthority.HelpMenuId), Is.True);
		}

		[Test]
		public void ObjectMenu_OfAnOrdinaryRow_IsBuiltWithoutTheMediator()
		{
			MakeTwoSubentries();
			UseHelpProvider(KnowsEveryTopic);
			var field = SubentriesField();
			var ids = LabelMenuIds(field);

			var items = BuildWithSpy(ids, m_view.CreateMenuAuthority(LabelMenuRequest(field, NoItem)), out var asked);

			Assert.That(asked, Is.False, "nothing in the label menu reaches the mediator");
			var visibility = FindItem(items, "Field Visibility");
			Assert.That(visibility, Is.Not.Null);
			Assert.That(visibility.Children.Count(c => c.IsChecked), Is.EqualTo(1),
				"exactly one visibility is the row's current one");
			Assert.That(visibility.Children.Select(c => c.Execute), Is.All.Not.Null,
				"a located row's visibility items all execute");
			var move = FindItem(items, "Move Field");
			Assert.That(move?.Children.Select(c => c.Label), Is.EqualTo(new[] { "Move Up", "Move Down" }));
			Assert.That(FindItem(items, "Help...")?.Execute, Is.Not.Null, "Help opens the row's own topic");
		}

		// An unlocatable row is the agreed exception to a row's field commands: they disable.
		[Test]
		public void ObjectMenu_OnEveryRow_OffersItsFieldCommands_DisabledWhereTheRowIsUnlocatable()
		{
			AddSense("second gloss");
			MakeTwoSubentries();
			UseHelpProvider(KnowsEveryTopic);
			var fields = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Where(f => f.ObjectHvo != 0 && !string.IsNullOrEmpty(f.Field)).ToList();

			var located = 0;
			var unlocatable = new List<string>();
			foreach (var field in fields)
			{
				var native = BuildAsTheHostDoes(LabelMenuIds(field), field);
				var visibility = FindItem(native, "Field Visibility").Children;
				var fieldItems = visibility.Concat(FindItem(native, "Move Field").Children).ToList();
				if (!m_view.TryLocateOverrideTarget(field, out _, out _))
				{
					unlocatable.Add(string.Format("{0} ({1}) stableId={2} class={3} layout={4}", field.Label,
						field.Field, field.StableId, field.ClassName, field.LayoutName));
					Assert.That(fieldItems.Select(i => i.IsEnabled), Is.All.False,
						"an unlocatable row disables its field commands: " + field.Label);
					continue;
				}
				located++;
				Assert.That(visibility.Select(i => i.Execute), Is.All.Not.Null,
					"a located row's visibility items all execute: " + field.Label);
				Assert.That(visibility.Count(i => i.IsChecked), Is.EqualTo(1),
					"exactly one visibility is the row's current one: " + field.Label);
			}
			TestContext.WriteLine("Rows with no override target:" + Environment.NewLine
				+ string.Join(Environment.NewLine, unlocatable));
			Assert.That(located, Is.GreaterThan(10), "enough rows are located to make the check meaningful");
		}

		[Test]
		public void Help_OnPublishSenseIn_OpensTheRowsOwnTopic()
		{
			UseHelpProvider(KnowsEveryTopic);
			var sense = m_entry.SensesOS[0];
			// A sense excluded from no publication hides this row unless hidden fields show.
			var field = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true).Model.Fields
				.Single(f => f.ObjectHvo == sense.Hvo && f.Field == "PublishIn");

			var topic = m_view.KnownHelpTopic(field);

			Assert.That(topic, Does.Contain("PublishIn"), "the row's help topic names the row's field");
			var items = BuildItems(new[] { ObjectMenuAuthority.MenuId }, m_view.CreateObjectMenuAuthority(field));
			Assert.That(FindItem(items, "Help...")?.IsEnabled, Is.True);
		}

		[Test]
		public void ObjectMenu_HidesHelp_WhenTheProviderHasNoTopic()
		{
			// The fixture's default provider knows no topic; WinForms hides Help for that too.
			var items = BuildItems(new[] { ObjectMenuAuthority.MenuId }, m_view.CreateObjectMenuAuthority(LexemeFormField()));

			Assert.That(FindItem(items, "Help..."), Is.Null, "no known topic, no Help item");
			Assert.That(FindItem(items, "Field Visibility"), Is.Not.Null);
		}

		// A WinForms slice always ends at a topic, the generic one at worst; so does a row that
		// has no object to generate from.
		[Test]
		public void Help_OnARowWithoutAnObject_OffersTheGenericTopic()
		{
			UseHelpProvider(new PatternHelpTopicProvider(id => id == FieldHelpTopics.NoHelpTopic));
			var field = new DetailField("Row", "Row", "Nothing", null, DetailFieldKind.Text,
				EditorClassification.Known, "Row", null, HostRouting.Inherit, null, null, null);
			Assert.That(field.ObjectHvo, Is.EqualTo(0), "precondition: no object, no help source");

			Assert.That(m_view.ResolveHelpTopic(field), Is.EqualTo(FieldHelpTopics.NoHelpTopic));
			var items = BuildItems(new[] { ObjectMenuAuthority.MenuId }, m_view.CreateObjectMenuAuthority(field));
			Assert.That(FindItem(items, "Help...")?.IsEnabled, Is.True, "the generic topic is still a topic");
		}

		[Test]
		public void ObjectMenu_OnARowWithoutOverrideContext_DisablesVisibilityAndMove_AndKeepsHelp()
		{
			UseHelpProvider(KnowsEveryTopic);
			// No class or layout: the row's override target cannot be located.
			var field = new DetailField("Row", "Row", "Subentries", null, DetailFieldKind.Text,
				EditorClassification.Known, "Row", null, HostRouting.Inherit, null, null, null,
				objectHvo: m_entry.Hvo) { HelpTopicId = "khtpTest-Row" };

			var items = BuildItems(new[] { ObjectMenuAuthority.MenuId }, m_view.CreateObjectMenuAuthority(field));

			var visibility = FindItem(items, "Field Visibility");
			Assert.That(visibility.Children.Select(c => c.IsEnabled), Is.All.False, "nothing to act on: disabled, never guessed");
			Assert.That(visibility.Children.Select(c => c.Execute), Is.All.Null);
			Assert.That(FindItem(items, "Move Field").Children.Select(c => c.IsEnabled), Is.All.False);
			Assert.That(FindItem(items, "Help...")?.IsEnabled, Is.True, "Help needs no override target");
		}

		[Test]
		public void FieldVisibility_ThroughTheAuthority_WritesTheOverride()
		{
			var field = LexemeFormField();
			DeleteOverrideFor(field);
			try
			{
				var items = BuildItems(new[] { ObjectMenuAuthority.MenuId }, m_view.CreateObjectMenuAuthority(field));
				var item = FindItem(items, "Normally hidden");
				Assert.That(item?.IsEnabled, Is.True);

				item.Execute();
				DrainMediatorAndIdleQueues();

				var stored = ReadOverrideFor(field);
				Assert.That(stored, Is.Not.Null, "choosing a visibility writes a project override");
				var op = stored.Operations.Single(o => o.Kind == ViewOverrideOperationKind.SetVisibility);
				Assert.That(op.StableId, Is.EqualTo(ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId)));
			}
			finally
			{
				DeleteOverrideFor(field);
			}
		}

		// A sense with no examples composes a ghost Example prompt whose stable id carries the
		// ghost marker; its field commands act on the Examples sequence node itself.
		[Test]
		public void GhostRow_LocatesItsSequenceNode_AndItsVisibilityWritesThatNode()
		{
			var sense = m_entry.SensesOS[0];
			Assert.That(sense.ExamplesOS, Is.Empty, "precondition: the sense composes a ghost Example row");
			var field = DetailComposer.Compose(m_entry, Cache).Model.Fields.Single(f =>
				f.ObjectHvo == sense.Hvo && f.Field == "Examples"
				&& f.StableId.EndsWith(DetailField.GhostStableIdSuffix, StringComparison.Ordinal));
			DeleteOverrideFor(field);
			var nodeId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId);
			nodeId = nodeId.Substring(0, nodeId.Length - DetailField.GhostStableIdSuffix.Length);

			Assert.That(m_view.TryLocateOverrideTarget(field, out var templateId, out _), Is.True,
				"the ghost row locates the node it stands in for");
			Assert.That(templateId, Is.EqualTo(nodeId));

			try
			{
				var items = BuildItems(new[] { ObjectMenuAuthority.MenuId }, m_view.CreateObjectMenuAuthority(field));
				FindItem(items, "Normally hidden").Execute();
				DrainMediatorAndIdleQueues();

				var op = ReadOverrideFor(field).Operations.Single(o => o.Kind == ViewOverrideOperationKind.SetVisibility);
				Assert.That(op.StableId, Is.EqualTo(nodeId), "the override names the sequence node, not the ghost");
			}
			finally
			{
				DeleteOverrideFor(field);
			}
		}

		[Test]
		public void ObjectMenuAuthority_RejectsALeafItDoesNotAnswer()
		{
			var authority = m_view.CreateObjectMenuAuthority(LexemeFormField());
			var foreignLeaf = LeafOf(ReorderVectorMenuAuthority.MenuId, ReorderVectorMenuAuthority.AlphabeticalOrderCommandId);

			Assert.That(() => authority.Build(ObjectMenuAuthority.MenuId, foreignLeaf),
				Throws.InvalidOperationException, "an owned id must be answered in full, never partially");
		}

		// -----------------------------------------------------------------
		// Lexeme Form row menu: Writing Systems
		// -----------------------------------------------------------------

		// The writing-system entries come from an inline list: they splice into the submenu
		// between its two commands.
		[Test]
		public void LexemeFormSliceMenu_WritingSystemsSubmenu_ListsTheProjectWritingSystems()
		{
			var field = LexemeFormField();
			var items = BuildAsTheHostDoes(LabelMenuIds(field), field);

			var writingSystems = FindItem(items, "Writing Systems");
			Assert.That(writingSystems, Is.Not.Null,
				"the multistring group contributes the Writing Systems submenu");

			var labels = writingSystems.Children.Where(c => !c.IsSeparator).Select(c => c.Label).ToList();
			Assert.That(labels, Does.Contain("Show all right now"));
			Assert.That(labels, Does.Contain("Configure..."));
			Assert.That(FindItem(items, "Show all right now").IsEnabled, Is.True);
			Assert.That(FindItem(items, "Configure...").IsEnabled, Is.True);

			var spliced = labels.Where(l => l != "Show all right now" && l != "Configure...").ToList();
			Assert.That(spliced, Is.Not.Empty,
				"the inline list must contribute the row's writing systems, not be nested away");

			var vernacular = Cache.LangProject.CurrentVernacularWritingSystems
				.Select(ws => ws.DisplayLabel).ToList();
			Assert.That(spliced.Intersect(vernacular), Is.Not.Empty,
				"the spliced entries are the project's vernacular writing systems (the Lexeme Form's ws set)");
		}

		// THE decisive copy test: unchecking one of two vernaculars must store the REDUCED
		// set (the slice still holds the pre-click set right after OnClick), and re-checking
		// must store the restored set.
		[Test]
		public void WritingSystemToggle_TwoVernaculars_StoresTheReducedThenRestoredSet()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			try
			{
				var writingSystems = BuildWritingSystemsSubmenu(field);
				var checkedEntries = writingSystems.Children
					.Where(c => !c.IsSeparator && c.IsChecked && c.Execute != null).ToList();
				TestContext.WriteLine("checked: " + string.Join(", ",
					checkedEntries.Select(e => e.Label)));
				Assert.That(checkedEntries.Count, Is.EqualTo(2),
					"precondition: both vernacular writing systems start visible");
				var toggled = checkedEntries[0].Label;

				checkedEntries[0].Execute();
				DrainMediatorAndIdleQueues();

				var reduced = StoredWritingSystems(field);
				TestContext.WriteLine("reduced: " + string.Join(",", reduced));
				Assert.That(reduced, Is.EqualTo(new[] { "es" }),
					"the stored op holds the set AFTER the uncheck, not the pre-click set");

				// Re-check the same writing system on a freshly built menu: the ADD direction.
				// The menu reads the row's stored selection, so take the row as the host's
				// refresh recomposed it, override applied.
				field = LexemeFormFieldWithOverrides();
				writingSystems = BuildWritingSystemsSubmenu(field);
				var reAdd = writingSystems.Children.Single(c => !c.IsSeparator && c.Label == toggled);
				Assert.That(reAdd.IsChecked, Is.False, "the unchecked toggle stays unchecked");
				Assert.That(reAdd.Execute, Is.Not.Null, "an unchecked toggle is clickable");
				reAdd.Execute();
				DrainMediatorAndIdleQueues();

				var restored = StoredWritingSystems(field);
				TestContext.WriteLine("restored: " + string.Join(",", restored));
				// Exact order matters: a toggle stores the set in option order, so rows never
				// reorder when a writing system is re-checked.
				Assert.That(restored, Is.EqualTo(new[] { "fr", "es" }),
					"re-checking stores the restored set in option order");
			}
			finally
			{
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// The Lexeme Form row is keyed by the MoForm's own descended (class, layout). Compose
		// must resolve an override for it, or the row's customizations are written and never
		// read.
		[Test]
		public void Compose_ResolvesTheOverrideForADescendedLayout()
		{
			var field = LexemeFormField();
			var store = GetOverrideStore();
			var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId);
			TestContext.WriteLine(
				$"class={field.ClassName} layout={field.LayoutName} template={templateId}");
			try
			{
				store.Save(new ViewDefinitionOverride(field.ClassName, field.LayoutName, "detail",
					new[]
					{
						new ViewOverrideOperation(ViewOverrideOperationKind.SetLabel, templateId,
							label: "OverrideMarker")
					}, null));

				var asked = new List<string>();
				var recomposed = DetailComposer.Compose(m_entry, Cache, showHiddenFields: false,
					overrides: (cls, layout) =>
					{
						asked.Add(cls + "/" + layout);
						return store.TryGet(cls, layout);
					});
				TestContext.WriteLine("compose asked for: " + string.Join(", ", asked));

				var row = FindLexemeFormRow(recomposed.Model);
				Assert.That(row.Label, Is.EqualTo("OverrideMarker"),
					"an override keyed by the row's own (class, layout) must reach the composed row");
			}
			finally
			{
				DeleteOverrideFor(field);
			}
		}

		// -----------------------------------------------------------------
		// "Show all right now": the transient writing-system reveal
		// -----------------------------------------------------------------

		// The intercepted item must reveal WITHOUT persisting: the row composes with the full
		// set while the stored override keeps the restriction.
		[Test]
		public void ShowAllRightNow_RevealsTheFullSet_WithoutWritingTheOverride()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			try
			{
				var fullSet = field.Values.Select(v => v.WsTag).ToList();
				Assume.That(fullSet, Is.EqualTo(new List<string> { "fr", "es" }),
					"precondition: the unrestricted Lexeme Form row shows both vernaculars");
				GetOverrideStore().Save(new ViewDefinitionOverride(field.ClassName, field.LayoutName,
					"detail", new[]
					{
						new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibleWritingSystems,
							ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId),
							writingSystems: new[] { "fr" })
					}, null));
				Assert.That(ComposedFormWsTags(RevealedFields()), Is.EqualTo(new[] { "fr" }),
					"precondition: the override restricts the composed row");

				InvokeShowAllRightNow(field);

				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)),
					"the click marks the row's part in the host's transient reveal set");
				Assert.That(ComposedFormWsTags(RevealedFields()), Is.EqualTo(fullSet),
					"the revealed row composes with the full writing-system set");
				Assert.That(StoredWritingSystems(field), Is.EqualTo(new[] { "fr" }),
					"the stored override keeps the restriction -- the reveal is never persisted");
			}
			finally
			{
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		private ILexEntry AddOtherEntry()
		{
			ILexEntry other = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stemMorphType = GetMorphTypeOrCreateOne("stem");
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				other = AddLexeme(m_createdObjects, "other-entry", stemMorphType, "other gloss", noun);
			});
			return other;
		}

		// LT-22692: leaving a record with an edit still open builds the next record's view once;
		// the left record's commit is judged against the record about to be shown.
		[Test]
		public void LeavingARecordWithAnOpenEdit_BuildsTheNextRecordOnce()
		{
			var other = AddOtherEntry();
			m_view.Clerk.JumpToRecord(m_entry.Hvo);
			DrainMediatorAndIdleQueues();

			var holder = (DetailEditContextHolder)GetField(m_view, "m_detailEditContext");
			var shown = ((DetailHostControl)GetField(m_view, "m_avaloniaEntryForm")).ShownDetail;
			var gloss = shown.Fields.First(f => f.Field == "Gloss" && f.IsEditable
				&& f.Values != null && f.Values.Count > 0);
			Assert.That(holder.Current.TrySetText(gloss, gloss.Values[0].WsTag, "edited gloss"),
				Is.True);
			Assert.That(holder.Current.IsOpen, Is.True, "precondition: the edit is still open");

			var shows = m_view.DetailShowCount;
			m_view.Clerk.JumpToRecord(other.Hvo);
			DrainMediatorAndIdleQueues();

			Assert.That(m_view.DetailShowCount, Is.EqualTo(shows + 1),
				"the next record is built once, not again for the left record's commit");
		}

		// LT-22692: while no record is recorded as shown, an outside change to the clerk's
		// current record still brings it up.
		[Test]
		public void OutsideChange_WithNoRecordShown_ShowsTheClerksCurrentRecord()
		{
			m_view.Clerk.JumpToRecord(m_entry.Hvo);
			DrainMediatorAndIdleQueues();
			typeof(RecordEditView).GetMethod("SetShownRecord",
				BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m_view, new object[] { null, null });

			var shows = m_view.DetailShowCount;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("outside", Cache.DefaultAnalWs)));
			DrainMediatorAndIdleQueues();

			Assert.That(m_view.DetailShowCount, Is.EqualTo(shows + 1));
			Assert.That(GetField(m_view, "m_shownRecord"), Is.SameAs(m_entry));
		}

		// LT-22692: the quiet wait runs only the last action deferred to it, and only once.
		[Test]
		public void DeferUntilQuiet_RunsOnlyTheLastAction_Once()
		{
			var ran = new List<string>();
			m_view.DeferUntilQuiet(() => ran.Add("first"));
			m_view.DeferUntilQuiet(() => ran.Add("second"));
			Assert.That(m_view.IsRefreshQuietPending, Is.True, "the wait is running");

			m_view.OnRefreshQuiet();
			Assert.That(ran, Is.EqualTo(new[] { "second" }));
			Assert.That(m_view.IsRefreshQuietPending, Is.False, "and stops once it has run");

			m_view.OnRefreshQuiet();
			Assert.That(ran.Count, Is.EqualTo(1), "nothing runs twice");
		}

		// The reveal survives everything within the record and expires only when the shown
		// record changes.
		[Test]
		public void TransientReveal_ExpiresOnRecordNavigation()
		{
			var field = LexemeFormField();
			InvokeShowAllRightNow(field);
			Assert.That(RevealedFields(), Does.Contain(TemplateId(field)), "precondition: revealed");

			var other = AddOtherEntry();
			m_view.Clerk.JumpToRecord(other.Hvo);
			DrainMediatorAndIdleQueues();

			Assert.That(RevealedFields(), Is.Empty,
				"showing a different record expires every transient reveal");
		}

		// A configuration write replaces the reveal: after a toggle, the row shows the newly
		// stored set, not the stale full set.
		[Test]
		public void WritingSystemToggle_ReplacesTheTransientReveal()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			try
			{
				InvokeShowAllRightNow(field);
				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)), "precondition: revealed");

				var toggle = BuildWritingSystemsSubmenu(field).Children
					.First(c => !c.IsSeparator && c.IsChecked && c.Execute != null);
				var toggledLabel = toggle.Label;
				toggle.Execute();
				DrainMediatorAndIdleQueues();

				Assert.That(RevealedFields(), Does.Not.Contain(TemplateId(field)),
					"persisting a new display set replaces the transient reveal");

				// Re-check the toggled writing system: the toggle persists the visible set into
				// the part-ref Inventory, which is shared across this fixture's tests.
				var reAdd = BuildWritingSystemsSubmenu(field).Children
					.Single(c => !c.IsSeparator && c.Label == toggledLabel);
				reAdd.Execute();
				DrainMediatorAndIdleQueues();
			}
			finally
			{
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// Drives the "Show all right now" item for a row the way OnDetailMenuRequested would:
		// build the submenu, invoke, drain.
		private void InvokeShowAllRightNow(DetailField field)
		{
			var showAll = FindItem(BuildWritingSystemsSubmenu(field).Children, "Show all right now");
			Assert.That(showAll, Is.Not.Null, "the reveal item must materialize");
			Assert.That(showAll.IsEnabled, Is.True);
			Assert.That(showAll.Execute, Is.Not.Null);
			showAll.Execute();
			DrainMediatorAndIdleQueues();
		}

		// The reveal set is keyed by the part's template id, not the runtime row id.
		private static string TemplateId(DetailField field)
			=> ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId);

		// A failed override save must leave the reveal alone: ending it without recomposing
		// would collapse the row at the next unrelated refresh, with nothing to explain it.
		[Test]
		public void TransientReveal_SurvivesAFailedOverrideSave()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			// A file where the store expects its directory: Save's CreateDirectory throws.
			var blocker = Path.Combine(Path.GetTempPath(), "fw-reveal-" + Guid.NewGuid().ToString("N"));
			File.WriteAllText(blocker, "not a directory");
			var storeField = typeof(RecordEditView).GetField("m_viewOverrideStore",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(storeField, Is.Not.Null, "the override store field must exist");
			var original = storeField.GetValue(m_view);
			string toggledLabel = null;
			try
			{
				InvokeShowAllRightNow(field);
				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)), "precondition: revealed");

				storeField.SetValue(m_view, new ViewDefinitionOverrideStore(blocker));
				var toggle = BuildWritingSystemsSubmenu(field).Children
					.First(c => !c.IsSeparator && c.IsChecked && c.Execute != null);
				toggledLabel = toggle.Label;
				toggle.Execute();
				DrainMediatorAndIdleQueues();

				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)),
					"a save that failed must leave the transient reveal in place");
			}
			finally
			{
				storeField.SetValue(m_view, original);
				File.Delete(blocker);
				// The toggle persists into the part-ref inventory this fixture shares; re-check
				// it or later tests see no visible writing system.
				if (toggledLabel != null)
				{
					BuildWritingSystemsSubmenu(field).Children
						.Single(c => !c.IsSeparator && c.Label == toggledLabel).Execute();
					DrainMediatorAndIdleQueues();
				}
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// -----------------------------------------------------------------
		// Move Field
		// -----------------------------------------------------------------

		// A row at the TOP LEVEL of its own layout has no parent StableId, but move
		// enablement reads only the sibling order, so the item enables. The write must
		// then reorder the ROOT list, keyed on an empty id.
		[Test]
		public void MoveField_OnATopLevelRow_ReordersTheRootList()
		{
			// Chosen by SHAPE, not by name: a named row that stops being movable would leave this
			// test passing on a move it never made.
			var field = FindTopLevelMovableRow(out var location);
			Assert.That(field, Is.Not.Null,
				"the entry layout must still compose at least one movable top-level row");
			// The subject was located in the SHIPPED model; clear any stored override so the
			// menu's own location, which compiles with overrides applied, agrees with it.
			DeleteOverrideFor(field);
			var up = location.CanMoveUp;
			var expected = ViewDefinitionOverrideEditor.ComputeMovedOrder(
				location.SiblingOrder, location.Index, up);
			TestContext.WriteLine($"class={field.ClassName} layout={field.LayoutName} "
				+ $"template={ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId)} "
				+ $"index={location.Index} up={up}");
			try
			{
				var items = BuildAsTheHostDoes(new[] { ObjectMenuAuthority.MenuId }, field);
				var item = FindItem(items, up ? "Move Up" : "Move Down");
				Assert.That(item, Is.Not.Null, "the Move Field submenu must offer the item");
				Assert.That(item.IsEnabled, Is.True,
					"a row with somewhere to move must enable its Move item");

				item.Execute();
				DrainMediatorAndIdleQueues();

				var stored = ReadOverrideFor(field);
				Assert.That(stored, Is.Not.Null,
					"an enabled Move must write a project override, not silently do nothing");
				var op = stored.Operations.Single(o =>
					o.Kind == ViewOverrideOperationKind.ReorderChildren);
				Assert.That(op.StableId, Is.Empty,
					"a top-level move reorders the root list, keyed on the empty id");
				Assert.That(op.ChildOrder, Is.EqualTo(expected),
					"the stored order must be the siblings with this row swapped one position");
			}
			finally
			{
				DeleteOverrideFor(field);
			}
		}

		// The first composed row its own layout places at the top level (LocateTarget
		// reports no parent) and that can move, found through the compile + locate pair
		// the menu uses.
		private DetailField FindTopLevelMovableRow(out ViewNodeLocation location)
		{
			foreach (var field in DetailComposer.Compose(m_entry, Cache).Model.Fields)
			{
				if (string.IsNullOrEmpty(field.ClassName)
					|| string.IsNullOrEmpty(field.LayoutName)
					|| !Cache.ServiceLocator.ObjectRepository.TryGetObject(
						field.ObjectHvo, out var obj))
				{
					continue;
				}

				var model = DetailComposer.CompileForObject(Cache, obj, field.LayoutName);
				var located = model == null
					? null
					: ViewDefinitionOverrideEditor.LocateTarget(model,
						ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId));
				if (located != null && located.ParentStableId == null
					&& (located.CanMoveUp || located.CanMoveDown))
				{
					location = located;
					return field;
				}
			}

			location = null;
			return null;
		}

		// The host's transient reveal set, read through the same seam the production compose
		// uses.
		private HashSet<string> RevealedFields()
			=> (HashSet<string>)GetField(m_view, "m_showAllWsFields");

		// The Lexeme Form row's composed writing-system tags under the CURRENT stored override
		// and the given reveal set -- the same Compose call ShowAvaloniaEntry makes.
		private IReadOnlyList<string> ComposedFormWsTags(HashSet<string> reveal)
			=> FindLexemeFormRow(DetailComposer.Compose(m_entry, Cache,
					overrides: (cls, layout) => GetOverrideStore().TryGet(cls, layout),
					showAllWritingSystemsFields: reveal).Model)
				.Values.Select(v => v.WsTag).ToList();

		// -----------------------------------------------------------------
		// Helpers -- production-path command drivers
		// -----------------------------------------------------------------

		// The composed Lexeme Form row, as the production host resolves it before raising a
		// menu request.
		private DetailField LexemeFormField()
			=> FindLexemeFormRow(DetailComposer.Compose(m_entry, Cache).Model);

		// The same row as the host composes it after a refresh: the project override applied.
		private DetailField LexemeFormFieldWithOverrides()
			=> FindLexemeFormRow(DetailComposer.Compose(m_entry, Cache,
				overrides: (cls, layout) => GetOverrideStore().TryGet(cls, layout)).Model);

		// A multi-string row bound to no menu of its own, or to the empty Help menu, so every id
		// its label menu merges is owned once the multi-string group is.
		private DetailField OwnedMultiStringField()
		{
			var field = DetailComposer.Compose(m_entry, Cache).Model.Fields.FirstOrDefault(f =>
				f.IsMultiStringRow && f.ObjectHvo != 0
				&& (string.IsNullOrEmpty(f.MenuId) || f.MenuId == ObjectMenuAuthority.HelpMenuId));
			Assume.That(field, Is.Not.Null, "the fixture composes a multi-string row with an owned binding");
			return field;
		}

		// The one locator for the Lexeme Form row (the MoForm's own Form field) in a composed
		// model.
		private DetailField FindLexemeFormRow(DetailModel model)
			=> model.Fields.Single(f => f.Field == "Form" && f.Kind == DetailFieldKind.Text
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);

		private ViewDefinitionOverrideStore GetOverrideStore()
		{
			var storeProperty = typeof(RecordEditView).GetProperty("ViewOverrideStore",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(storeProperty, Is.Not.Null, "the override store seam must exist");
			var store = (ViewDefinitionOverrideStore)storeProperty.GetValue(m_view);
			Assert.That(store, Is.Not.Null, "the project must have a reachable override store");
			return store;
		}

		private ViewDefinitionOverride ReadOverrideFor(DetailField field)
			=> GetOverrideStore().TryGet(field.ClassName, field.LayoutName);

		// The Writing Systems submenu as the host builds it for the Lexeme Form row.
		private DetailMenuItem BuildWritingSystemsSubmenu(DetailField field)
		{
			var items = BuildAsTheHostDoes(
				new[] { "mnuDataTree-LexemeForm", "mnuDataTree-MultiStringSlice" }, field);
			var writingSystems = FindItem(items, "Writing Systems");
			Assert.That(writingSystems, Is.Not.Null,
				"precondition: the Writing Systems submenu is present");
			return writingSystems;
		}

		// The writing systems the stored override op carries for the field's row.
		private IReadOnlyList<string> StoredWritingSystems(DetailField field)
		{
			var stored = ReadOverrideFor(field);
			Assert.That(stored, Is.Not.Null,
				"toggling a writing system must write a project override");
			return stored.Operations.Single(o =>
				o.Kind == ViewOverrideOperationKind.SetVisibleWritingSystems).WritingSystems;
		}

		// Saving an empty patch deletes the file, so overrides never leak between tests.
		private void DeleteOverrideFor(DetailField field)
			=> GetOverrideStore().Save(new ViewDefinitionOverride(
				field.ClassName, field.LayoutName, "detail", null, null));

		private IReadOnlyList<DetailMenuItem> BuildItems(string[] menuIds, IDetailMenuAuthority authority)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			Assert.That(window, Is.Not.Null);
			return XCoreMenuBridge.CreateMenuItems(window, menuIds, authority);
		}

		// Items come from XCoreMenuBridge with accelerators already stripped; match on the visible label,
		// searching submenus too (some commands nest).
		private static DetailMenuItem FindItem(IReadOnlyList<DetailMenuItem> items, string label)
		{
			foreach (var item in items)
			{
				if (item.IsSeparator)
					continue;
				if (string.Equals(item.Label, label, StringComparison.Ordinal))
					return item;
				var nested = FindItem(item.Children, label);
				if (nested != null)
					return nested;
			}
			return null;
		}

		// -----------------------------------------------------------------
		// Helpers -- refresh / detail assertions
		// -----------------------------------------------------------------

		// Calls the host's real RefreshAvaloniaDetail and reports the field count of the recomposed
		// model, proving the host can re-render after the command without throwing.
		private int RefreshedDetailFieldCount()
		{
			var refresh = typeof(RecordEditView).GetMethod("RefreshAvaloniaDetail",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(refresh, Is.Not.Null);
			refresh.Invoke(m_view, null);
			DrainMediatorAndIdleQueues();
			return DetailComposer.Compose(m_entry, Cache).Model.Fields.Count;
		}

		// ----------------------------------------------------------------------------------------
		// Bootstrap helpers (mirrors RecordEditViewActiveHostContractTests)
		// ----------------------------------------------------------------------------------------

		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "command-entry", stemMorphType, "first gloss", noun);
		}

		private void AddSense(string gloss)
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				AddSenseToEntry(m_createdObjects, m_entry, gloss, noun);
			});
			DrainMediatorAndIdleQueues();
		}

		private void DestroyTestData()
		{
			if (m_createdObjects == null)
				return;
			foreach (var obj in m_createdObjects)
			{
				if (obj.IsValidObject && obj is ILexEntry)
					obj.Delete();
			}
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected the RecordEditView configuration node for tool '{0}'.", toolValue);

			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private void EnsureCurrentRecord(RecordEditView control)
		{
			if (control.Clerk.CurrentObject == null)
			{
				control.Clerk.JumpToRecord(m_entry.Hvo);
				DrainMediatorAndIdleQueues();
			}
			// Make our entry the displayed record so commands target it.
			if (control.Clerk.CurrentObject?.Hvo != m_entry.Hvo)
			{
				control.Clerk.JumpToRecord(m_entry.Hvo);
				DrainMediatorAndIdleQueues();
			}
			Assert.That(control.Clerk.CurrentObject, Is.Not.Null);
		}

		private static object GetField(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			return field.GetValue(target);
		}

		// DrainMediatorAndIdleQueues is inherited from XWorksAppTestBase.
	}
}
