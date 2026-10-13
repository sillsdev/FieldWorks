// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;
using LegacyDataTree = SIL.FieldWorks.Common.Framework.DetailControls.DataTree;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The message-keyed authority for the Lexicon detail menus, as the product's
	/// <see cref="RecordEditView"/> wires it: which context-menu ids it owns (pinned, so a PR
	/// that adds an answerer shows the menus it switched over), the predicate behind that
	/// list, and the contracts every owned id must meet.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class LexiconMenuAuthorityTests : XWorksAppTestBase
	{
		// The ids owned today: every leaf of each sends DataTreeDelete. Ownership is by message
		// over the whole window configuration, so other areas' ids and unbound ids count too.
		private static readonly string[] PinnedOwnedIds =
		{
			// Lexicon rows: Translation, and Complex Form Type on a complex form.
			"mnuDataTree-Translation", "mnuDataTree-ComplexFormSpec",
			// Lexicon ids no part reaches (-CmMedia's pronunciation part does not exist).
			"mnuDataTree-CmMedia", "mnuDataTree-Variant", "mnuDataTree-DeleteEtymology",
			"mnuDataTree-DeletePronunciation",
			// Grammar: two posEdit rows (Inflection Class Name, Stem Allomorph Label), then
			// tools without an Avalonia detail view.
			"mnuDataTree-POS-InflectionClass", "mnuDataTree-POS-StemName",
			"mnuDataTree-ClosedFeature-Value", "mnuDataTree-MoStemName-Region", "mnuDataTree-Phoneme-Code",
			"mnuDataTree-Delete-Adhoc-Allomorph", "mnuDataTree-Delete-Adhoc-Group",
			"mnuDataTree-Delete-Adhoc-Morpheme",
			// Lists: no Avalonia detail view.
			"mnuDataTree-DeleteCustomItem", "mnuDataTree-DeletePossibility", "mnuDataTree-DeleteQuestion"
		};

		// The ids a Lexicon Edit row raises, the rows the contracts below are proved on.
		private static readonly string[] LexiconRowIds = { "mnuDataTree-Translation", "mnuDataTree-ComplexFormSpec" };

		// The messages of mnuDataTree-Object's six leaves, the test subject for the predicate.
		private static readonly string[] ObjectMenuMessages =
		{
			"ShowFieldAlwaysVisible", "ShowFieldIfData", "ShowFieldNormallyHidden",
			"MoveFieldUp", "MoveFieldDown", "DataTreeHelp"
		};

		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private ICmTranslation m_translation;
		private RecordEditView m_view;

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
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestEntry);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the lexicon edit RecordEditView to load");
			m_view.Clerk.JumpToRecord(m_entry.Hvo);
			DrainMediatorAndIdleQueues();
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in Enumerable.Reverse(m_createdObjects))
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
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

		// Contract: the owned-id list is exact, and every owned id builds in full through the
		// bridge, so an unanswered leaf fails here instead of reverting a menu to the adapter.
		[Test]
		public void OwnsExactlyThePinnedIds_AndAnswersEveryLeafOfEachOne()
		{
			var authority = m_view.CreateLexiconMenuAuthority(TranslationRow());
			var window = Window;

			var owned = ContextMenuIds().Where(authority.Owns).ToList();

			Assert.That(owned, Is.EquivalentTo(PinnedOwnedIds),
				"the authority's owned ids changed; update the pin so the PR shows which menus switched over");
			foreach (var id in owned)
			{
				foreach (var leaf in XCoreMenuBridge.ResolveMenu(window, id).Leaves)
				{
					Assert.That(() => authority.Build(id, leaf), Throws.Nothing,
						"menu '{0}': the authority does not answer leaf '{1}'", id, leaf.CommandId);
				}
				Assert.That(() => XCoreMenuBridge.CreateMenuItems(window, new[] { id }, null, null, authority),
					Throws.Nothing, "menu '{0}' builds in full through the bridge", id);
			}
		}

		[Test]
		public void Owns_WhenEveryLeafsMessageIsAnswered_AndNotOneMessageShort()
		{
			var authority = NewAuthority();
			foreach (var message in ObjectMenuMessages.Take(ObjectMenuMessages.Length - 1))
				authority.Answer(message, Echo);
			Assert.That(authority.Owns(RecordEditView.ObjectMenuId), Is.False,
				"one unanswered message leaves the id with the adapter");

			authority.Answer(ObjectMenuMessages.Last(), Echo);

			Assert.That(authority.Owns(RecordEditView.ObjectMenuId), Is.True,
				"the id is owned the moment its last message has an answerer");
			var items = XCoreMenuBridge.CreateMenuItems(Window, new[] { RecordEditView.ObjectMenuId },
				null, null, authority);
			Assert.That(items.Select(i => i.Label), Does.Contain("Help..."),
				"the owned menu builds from the answerers alone");
		}

		[Test]
		public void NeverOwns_AMenuWithoutLeaves_OrWithAListSubmenu_OrWithAPropertyToggle()
		{
			var authority = NewAuthority();
			foreach (var message in ObjectMenuMessages)
				authority.Answer(message, Echo);
			authority.Answer("DataTreeWritingSystemsShowAll", Echo);
			authority.Answer("DataTreeWritingSystemsConfigureDlg", Echo);

			Assert.That(authority.Owns(ObjectMenuAuthority.HelpMenuId), Is.False,
				"mnuDataTree-Help has no leaf, so there is nothing to answer");
			Assert.That(XCoreMenuBridge.ResolveMenu(Window, RecordEditView.MultiStringSliceMenuId).Leaves
				.All(l => authority.AnsweredMessages.Contains(l.Message)), Is.True,
				"precondition: every configured leaf of the multi-string menu is answered");
			Assert.That(authority.Owns(RecordEditView.MultiStringSliceMenuId), Is.False,
				"a list-populated submenu has no configured leaves to answer, so the id is not owned");

			var window = Window;
			var withToggle = ContextMenuIds().Select(id => XCoreMenuBridge.ResolveMenu(window, id))
				.First(m => m.Unanswerable.Any(u => u.StartsWith("property", StringComparison.Ordinal)) && m.Leaves.Count > 0);
			foreach (var message in withToggle.Leaves.Select(l => l.Message).Distinct()
				.Where(m => !authority.AnsweredMessages.Contains(m)))
			{
				authority.Answer(message, Echo);
			}
			Assert.That(authority.Owns(withToggle.MenuId), Is.False,
				"menu '{0}': a property toggle belongs to the property table, so the id is not owned", withToggle.MenuId);
		}

		[Test]
		public void Build_RejectsALeafWhoseMessageHasNoAnswerer_AndEveryList()
		{
			var authority = m_view.CreateLexiconMenuAuthority(TranslationRow());
			var help = XCoreMenuBridge.ResolveMenu(Window, RecordEditView.ObjectMenuId).Leaves
				.First(l => l.CommandId == ObjectMenuAuthority.HelpCommandId);

			Assert.That(() => authority.Build(RecordEditView.ObjectMenuId, help),
				Throws.InvalidOperationException, "an owned id must be answered in full, never partially");
			Assert.That(() => authority.BuildList(RecordEditView.MultiStringSliceMenuId,
				MultiStringMenuAuthority.WritingSystemListId), Throws.InvalidOperationException);
		}

		// Contract: on every Lexicon Edit row binding an owned id, the owned menu renders what
		// the adapter path rendered; the row's other groups are proved by their own authorities.
		[Test]
		public void OwnedMenu_RendersWhatTheAdapterPathRendered_OnEveryLexiconRowThatBindsOne()
		{
			var rows = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Where(f => LexiconRowIds.Contains(f.MenuId)).ToList();
			Assert.That(rows.Select(f => f.Label), Is.EquivalentTo(new[] { "Translations 1", "Translation", "Complex Form Type" }),
				"precondition: the entry composes the rows the owned ids are bound to");

			var mismatches = new List<string>();
			foreach (var row in rows)
			{
				EnsureAdapter(row.ObjectHvo, row.Field);
				Assert.That(AdapterTree.CurrentSlice?.Object?.Hvo, Is.EqualTo(row.ObjectHvo),
					"precondition: the adapter targets the row's object for " + row.Label);
				var ids = new[] { row.MenuId };
				var before = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null, null));
				var after = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null,
					m_view.CreateMenuAuthority(LabelMenuRequest(row))));
				Assert.That(after, Does.Contain("enabled=True checked=False executes=True"),
					row.Label + ": Delete is offered and runs");
				if (!string.Equals(before, after, StringComparison.Ordinal))
				{
					mismatches.Add(string.Format("{0} ({1}):{2}--- adapter path{2}{3}{2}--- authority{2}{4}",
						row.Label, row.Field, Environment.NewLine, before, after));
				}
			}
			Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
		}

		// Contract: an owned label menu never asks the mediator and never builds the adapter.
		[Test]
		public void OwnedLabelMenus_AreBuiltWithoutTheAdapterOrTheMediator()
		{
			Assert.That(HiddenTreeExists, Is.False, "precondition: no hidden tree yet");
			var rows = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Where(f => LexiconRowIds.Contains(f.MenuId)).ToList();
			Assert.That(rows, Has.Count.EqualTo(3), "precondition");
			foreach (var row in rows)
			{
				var ids = RecordEditView.ComposeSliceMenuIds(row.MenuId, row.IsMultiStringRow).ToArray();
				var authority = m_view.CreateMenuAuthority(LabelMenuRequest(row));
				Assert.That(XCoreMenuBridge.OwnsAll(authority, ids), Is.True, row.Label + ": every id of the label menu is owned");

				var items = BuildWithSpy(ids, authority, out var asked);

				Assert.That(asked, Is.False, row.Label + ": the mediator was asked");
				Assert.That(items.Any(i => i.Label.StartsWith("Delete", StringComparison.Ordinal)), Is.True);
			}
			Assert.That(HiddenTreeExists, Is.False, "an owned menu never builds the hidden tree");
		}

		[Test]
		public void DeleteLeaf_IsAbsent_WithoutARow_OnAGhostRow_AndOnARowWithoutAnObject()
		{
			var leaf = DeleteTranslationLeaf();
			var translation = TranslationRow();
			var ghost = new DetailField(translation.StableId + DetailField.GhostStableIdSuffix, "Translation", "Translation",
				null, DetailFieldKind.Text, EditorClassification.Known, null, null, HostRouting.Inherit, null, null, null,
				menuId: translation.MenuId, objectHvo: translation.ObjectHvo, ghostPrompt: "Add a translation");
			var noObject = new DetailField("Row", "Row", "Translation", null, DetailFieldKind.Text,
				EditorClassification.Known, null, null, HostRouting.Inherit, null, null, null, menuId: translation.MenuId);

			Assert.That(m_view.DeleteItem(leaf, null), Is.Null, "no row");
			Assert.That(m_view.DeleteItem(leaf, ghost), Is.Null, "a ghost row has no object to delete");
			Assert.That(m_view.DeleteItem(leaf, noObject), Is.Null, "no object");
			Assert.That(m_view.DeleteItem(leaf, translation)?.IsEnabled, Is.True, "the real row offers it");
		}

		[Test]
		public void DeleteLeaf_IsDisabled_WhenTheObjectIsGone_AndWhenTheRuleRefuses()
		{
			var leaf = DeleteTranslationLeaf();
			var translation = TranslationRow();
			var note = StringTable.Table.GetString(DetailObjectDeletion.CannotDeleteKey);
			var gone = new DetailField(translation.StableId, "Translation", "Translation", null, DetailFieldKind.Text,
				EditorClassification.Known, null, null, HostRouting.Inherit, null, null, null,
				menuId: translation.MenuId, objectHvo: int.MaxValue - 1);
			// A sense of an entry without entry refs sits in a required field: the only one
			// stays.
			ILexEntry plain = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				plain = AddLexeme(m_createdObjects, "plain", GetMorphTypeOrCreateOne("stem"), "plain gloss",
					GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA)));
			var onlySense = new DetailField("Sense", "Sense", "Senses", null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
				menuId: translation.MenuId, objectHvo: plain.SensesOS[0].Hvo);

			var goneItem = m_view.DeleteItem(leaf, gone);
			var refused = m_view.DeleteItem(leaf, onlySense);

			Assert.That(goneItem.IsEnabled, Is.False);
			Assert.That(goneItem.Execute, Is.Null);
			Assert.That(goneItem.Label, Is.EqualTo("Delete Translation" + note));
			Assert.That(refused.IsEnabled, Is.False, "the rule refuses the last item of a required field");
			Assert.That(refused.Label, Does.EndWith(note));
		}

		private XWindow Window => m_propertyTable.GetValue<XWindow>("window");

		private bool HiddenTreeExists => (bool)GetField(m_view, "m_dataTreeInitialized");

		private LegacyDataTree AdapterTree => (LegacyDataTree)GetField(m_view, "m_dataEntryForm");

		private LexiconMenuAuthority NewAuthority()
		{
			var window = Window;
			return new LexiconMenuAuthority(id => XCoreMenuBridge.ResolveMenu(window, id));
		}

		private static DetailMenuItem Echo(DetailMenuLeaf leaf)
			=> new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true);

		private static DetailMenuRequest LabelMenuRequest(DetailField field)
			=> DetailMenuRequest.FromAnchor(null, field, DetailMenuKind.SliceMenu);

		private DetailField TranslationRow()
			=> DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Translation" && f.ObjectHvo == m_translation.Hvo);

		private DetailMenuLeaf DeleteTranslationLeaf()
			=> XCoreMenuBridge.ResolveMenu(Window, "mnuDataTree-Translation").Leaves.Single();

		// Every context-menu id the window configuration defines, each once.
		private IEnumerable<string> ContextMenuIds()
		{
			var configuration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(configuration, Is.Not.Null);
			return configuration.SelectNodes("//contextMenus/menu[@id]").Cast<XmlNode>()
				.Select(n => n.Attributes["id"].Value).Distinct(StringComparer.Ordinal).ToList();
		}

		private IReadOnlyList<DetailMenuItem> BuildWithSpy(string[] ids, IDetailMenuAuthority authority,
			out bool mediatorAsked)
		{
			var window = Window;
			var spy = new DisplaySpyColleague();
			window.Mediator.AddColleague(spy);
			try
			{
				var items = XCoreMenuBridge.CreateMenuItems(window, ids, null, null, authority);
				mediatorAsked = spy.Asked;
				return items;
			}
			finally
			{
				window.Mediator.RemoveColleague(spy);
			}
		}

		// Answers no display query, only records that one arrived.
		private sealed class DisplaySpyColleague : IxCoreColleague
		{
			public bool Asked { get; private set; }
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;
			public bool OnDisplayDataTreeDelete(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayShowFieldAlwaysVisible(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
		}

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

		private void EnsureAdapter(int targetHvo, string fieldName)
		{
			var method = typeof(RecordEditView).GetMethod("EnsureMenuCommandAdapter",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "EnsureMenuCommandAdapter must exist (adapter targeting seam)");
			method.Invoke(m_view, new object[] { targetHvo, fieldName });
		}

		// An entry with a sense whose example has a free translation, made a complex form of
		// another entry so its own LexEntryRef shows the Complex Form Type row.
		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "command-entry", stemMorphType, "first gloss", noun);
			var example = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
			m_entry.SensesOS[0].ExamplesOS.Add(example);
			example.Example.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("una casa", Cache.DefaultVernWs));
			var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>()
				.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
			m_translation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(example, type);
			m_translation.Translation.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("a house", Cache.DefaultAnalWs));

			var component = AddLexeme(m_createdObjects, "part", stemMorphType, "part gloss", noun);
			var complexFormRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			m_entry.EntryRefsOS.Add(complexFormRef);
			complexFormRef.RefType = LexEntryRefTags.krtComplexForm;
			complexFormRef.ComponentLexemesRS.Add(component);
			complexFormRef.PrimaryLexemesRS.Add(component);
			complexFormRef.ComplexEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
				.GetObject(LexEntryTypeTags.kguidLexTypCompound));
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(windowConfiguration, Is.Not.Null);
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected the RecordEditView configuration node for tool '{0}'.", toolValue);

			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private static object GetField(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			return field.GetValue(target);
		}
	}
}
