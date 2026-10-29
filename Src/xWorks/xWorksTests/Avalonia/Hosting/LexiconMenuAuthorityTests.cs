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
using SIL.LCModel.DomainServices;
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
		// The ids owned today: each leaf sends an answered message, an Insert only in
		// scope. Ownership is by message, so other areas' and unbound ids count too.
		private static readonly string[] PinnedOwnedIds =
		{
			// Lexicon rows: Translation, and Complex Form Type on a complex form.
			"mnuDataTree-Translation", "mnuDataTree-ComplexFormSpec",
			// Lexicon rows: the Lexeme Form row's label menu and the in-string menus of the
			// Lexeme Form, Citation Form and variant form rows.
			"mnuDataTree-LexemeForm", "mnuDataTree-LexemeFormContext", "mnuDataTree-CitationFormContext",
			"mnuDataTree-VariantFormContext",
			// Lexicon rows: the Subsenses and Extended Notes prompt rows, and the hotlinks of
			// a sense, an extended note and an etymology.
			"mnuDataTree-Subsenses", "mnuDataTree-ExtendedNotes", "mnuDataTree-Sense-Hotlinks",
			"mnuDataTree-ExtendedNote-Hotlinks", "mnuDataTree-Etymology-Hotlinks",
			// Lexicon rows: the Allomorphs, Variants and Examples section rows, and the
			// Allomorphs and Variants hotlinks.
			"mnuDataTree-AlternateForms", "mnuDataTree-VariantForms", "mnuDataTree-Examples",
			"mnuDataTree-AlternateForms-Hotlinks", "mnuDataTree-VariantForms-Hotlinks",
			// Lexicon rows: the item menus of etymology, extended note, note examples, example,
			// pronunciation and Variant Info; the picture menu, no Avalonia row yet (LT-22772).
			"mnuDataTree-Etymology", "mnuDataTree-ExtendedNote", "mnuDataTree-ExtendedNote-Examples",
			"mnuDataTree-Example", "mnuDataTree-Pronunciation", "mnuDataTree-VariantSpec", "mnuDataTree-Picture",
			// Lexicon rows: the item menus of a stem or affix allomorph and of an affix process,
			// a variant's form row, and the lexical-relation rows (add or replace a reference).
			"mnuDataTree-Allomorph", "mnuDataTree-AffixProcess", "mnuDataTree-VariantForm",
			"mnuDataTree-DeleteAddLexReference", "mnuDataTree-DeleteReplaceLexReference",
			// Lexicon rows: a sense's header.
			"mnuDataTree-Sense",
			// Lexicon: an id no part reaches (its pronunciation part does not exist).
			"mnuDataTree-CmMedia",
			// Grammar: posEdit rows (two item rows, five section rows: Affix Templates, Affix
			// Slots, Inflection Classes, Subclasses, Stem Allomorph Labels), then other tools.
			"mnuDataTree-POS-InflectionClass", "mnuDataTree-POS-StemName",
			"mnuDataTree-POS-AffixTemplates", "mnuDataTree-POS-AffixSlots", "mnuDataTree-POS-InflectionClasses",
			"mnuDataTree-POS-InflectionClass-Subclasses", "mnuDataTree-POS-StemNames",
			"mnuDataTree-ClosedFeature-Value", "mnuDataTree-ClosedFeature-Values", "mnuDataTree-MoStemName-Region",
			"mnuDataTree-MoStemName-Regions", "mnuDataTree-Phoneme-Code", "mnuDataTree-Phoneme-Codes",
			"mnuDataTree-Delete-Adhoc-Allomorph", "mnuDataTree-Delete-Adhoc-Group",
			"mnuDataTree-Delete-Adhoc-Morpheme", "mnuDataTree-Adhoc-Group-Members",
			// Texts: the Info tab's jumps to the interlinear text and its notebook record; no
			// Avalonia detail view.
			"mnuDataTree-Text", "mnuTextInfo-Notebook",
			// Lists and Reversal: no Avalonia detail view.
			"mnuDataTree-DeletePossibility", "mnuDataTree-DeleteQuestion",
			"mnuDataTree-InsertQuestion", "mnuDataTree-InsertReversalSubentry", "mnuDataTree-InsertReversalSubentry-Hotlinks"
		};

		// The owned ids a Lexicon Edit row binds as its label menu, its in-string menu and its
		// hotlinks: the rows the contracts below are proved on.
		private static readonly string[] LexiconLabelMenuIds =
		{
			"mnuDataTree-Translation", "mnuDataTree-ComplexFormSpec", "mnuDataTree-LexemeForm",
			"mnuDataTree-Subsenses", "mnuDataTree-AlternateForms", "mnuDataTree-VariantForms",
			"mnuDataTree-Examples", "mnuDataTree-Etymology", "mnuDataTree-ExtendedNote",
			"mnuDataTree-ExtendedNote-Examples", "mnuDataTree-Example", "mnuDataTree-Pronunciation",
			"mnuDataTree-VariantSpec", "mnuDataTree-Allomorph", "mnuDataTree-AffixProcess", "mnuDataTree-VariantForm",
			"mnuDataTree-DeleteAddLexReference", "mnuDataTree-DeleteReplaceLexReference", "mnuDataTree-Sense"
		};

		private static readonly string[] LexiconInStringMenuIds =
			{ "mnuDataTree-LexemeFormContext", "mnuDataTree-CitationFormContext", "mnuDataTree-VariantFormContext" };

		private static readonly string[] LexiconHotlinksIds =
		{
			"mnuDataTree-Sense-Hotlinks", "mnuDataTree-ExtendedNote-Hotlinks", "mnuDataTree-Etymology-Hotlinks",
			"mnuDataTree-AlternateForms-Hotlinks", "mnuDataTree-VariantForms-Hotlinks"
		};

		private const string LexemeFormContextMenuId = "mnuDataTree-LexemeFormContext";

		// The messages of mnuDataTree-Object's six leaves, the test subject for the predicate.
		private static readonly string[] ObjectMenuMessages =
		{
			"ShowFieldAlwaysVisible", "ShowFieldIfData", "ShowFieldNormallyHidden",
			"MoveFieldUp", "MoveFieldDown", "DataTreeHelp"
		};

		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private IMoStemAllomorph m_allomorph;
		private IMoAffixProcess m_affixProcess;
		private ILexEntry m_variant;
		private ILexReference m_synonymSet;
		private ILexReference m_antonymPair;
		private ILexReference m_partOfWhole;
		private ICmTranslation m_translation;
		private ILexExtendedNote m_note;
		private ILexExtendedNote m_note2;
		private ILexEtymology m_etymology;
		private ILexEtymology m_etymology2;
		private ILexPronunciation m_pronunciation;
		private ICmPicture m_picture;
		private ILexEntryRef m_variantRef;
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
				"the authority's owned ids changed; update the pin so the PR shows which menus switched over. Owned now: "
				+ string.Join(", ", owned.OrderBy(id => id, StringComparer.Ordinal)));
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

		// Contract: on every Lexicon Edit row binding an owned id, by any menu kind, the
		// owned menu renders what the adapter path rendered.
		[Test]
		public void OwnedMenu_RendersWhatTheAdapterPathRendered_OnEveryLexiconRowThatBindsOne()
		{
			var rows = OwnedMenuRows();
			Assert.That(rows.Select(r => r.MenuId).Distinct(), Is.EquivalentTo(
				LexiconLabelMenuIds.Concat(LexiconInStringMenuIds).Concat(LexiconHotlinksIds)),
				"precondition: the entry composes a row for every owned id");

			var mismatches = new List<string>();
			foreach (var (row, menuId, kind) in rows)
			{
				EnsureAdapter(row.ObjectHvo, row.Field);
				Assert.That(AdapterTree.CurrentSlice?.Object?.Hvo, Is.EqualTo(row.ObjectHvo),
					"precondition: the adapter targets the row's object for " + row.Label);
				var ids = new[] { menuId };
				var before = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null, null));
				var after = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null,
					m_view.CreateMenuAuthority(MenuRequest(row, kind))));
				Assert.That(after, Does.Contain("enabled=True checked=False executes=True"),
					row.Label + ": " + menuId + " offers a command that runs");
				if (menuId == "mnuDataTree-ExtendedNote-Hotlinks")
				{
					// An accepted divergence: the adapter path disables the insert under a
					// note, since neither the entry nor the note has ExtendedNote; natively
					// the sense that owns the note takes it.
					Assert.That(before, Is.EqualTo("Insert Extended Note [enabled=False checked=False executes=False]"));
					Assert.That(after, Is.EqualTo("Insert Extended Note [enabled=True checked=False executes=True]"));
					continue;
				}
				if (RowIsTheSequencesOwner(row, menuId))
				{
					// A section row and an empty sequence's prompt row carry the item id with the
					// OWNER as their object; the adapter path offers the owner's Delete and Move
					// there, natively absent. The rest is the same.
					Assert.That(before, Does.Contain("Delete ").Or.Contain("Move "),
						row.Label + ": the adapter path offers the owner's Delete or Move");
					Assert.That(ItemLines(after), Is.EqualTo(ItemLines(before).Where(l => !l.StartsWith("Delete ") && !l.StartsWith("Move "))),
						row.Label + " (" + menuId + ")");
					continue;
				}
				if (!string.Equals(before, after, StringComparison.Ordinal))
				{
					mismatches.Add(string.Format("{0} ({1}):{2}--- adapter path{2}{3}{2}--- authority{2}{4}",
						row.Label, menuId, Environment.NewLine, before, after));
				}
			}
			Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
		}

		// The fixture's entry has one sense, where Demote, Promote and Move Sense to a New Entry
		// are all disabled; here a second sense and a subsense enable them.
		[Test]
		public void SenseMenu_RendersWhatTheAdapterPathRendered_WhereDemotePromoteAndSplitApply()
		{
			ILexSense second = null;
			ILexSense subsense = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var senses = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
				m_entry.SensesOS.Add(second = senses.Create());
				second.SensesOS.Add(subsense = senses.Create());
			});
			DrainMediatorAndIdleQueues();
			var ids = new[] { "mnuDataTree-Sense" };
			var rendered = new Dictionary<ILexSense, string>();

			foreach (var sense in new[] { m_entry.SensesOS[0], second, subsense })
			{
				var row = Rows().First(f => f.ObjectHvo == sense.Hvo && f.MenuId == "mnuDataTree-Sense");
				EnsureAdapter(row.ObjectHvo, row.Field);
				Assert.That(AdapterTree.CurrentSlice?.Object?.Hvo, Is.EqualTo(sense.Hvo),
					"precondition: the adapter targets the sense of " + row.Label);
				var before = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null, null));
				var after = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null,
					m_view.CreateMenuAuthority(MenuRequest(row, DetailMenuKind.SliceMenu))));
				Assert.That(after, Is.EqualTo(before), row.Label);
				rendered[sense] = after;
			}

			Assert.That(rendered[second], Does.Contain("Demote [enabled=True"));
			Assert.That(rendered[second], Does.Contain("Move Sense to a New Entry [enabled=True"));
			Assert.That(rendered[subsense], Does.Contain("Promote [enabled=True"));
		}

		// Contract: an owned menu never asks the mediator and never builds the adapter.
		[Test]
		public void OwnedMenus_AreBuiltWithoutTheAdapterOrTheMediator()
		{
			Assert.That(HiddenTreeExists, Is.False, "precondition: no hidden tree yet");
			var rows = OwnedMenuRows();
			Assert.That(rows, Has.Count.EqualTo(42),
				"precondition: twenty-seven ids; -Translation, -VariantForms and -DeleteReplaceLexReference on two rows each, the item ids and hotlinks on every item's rows");
			foreach (var (row, menuId, kind) in rows)
			{
				var ids = kind == DetailMenuKind.ContextMenu
					? RecordEditView.ComposeInStringMenuIds(menuId).ToArray()
					: kind == DetailMenuKind.Hotlinks
					? new[] { menuId }
					: RecordEditView.ComposeSliceMenuIds(menuId, row.IsMultiStringRow).ToArray();
				var authority = m_view.CreateMenuAuthority(MenuRequest(row, kind));
				Assert.That(XCoreMenuBridge.OwnsAll(authority, ids), Is.True, row.Label + ": every id of the menu is owned");

				var items = BuildWithSpy(ids, authority, out var asked);

				Assert.That(asked, Is.False, row.Label + ": the mediator was asked");
				Assert.That(items.Any(i => i.Execute != null), Is.True, row.Label + ": " + menuId + " offers a command");
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
			// An entry without entry refs requires its senses, so the only one stays. The entry
			// is shown, so its sense is no other entry's part (a variant back reference).
			ILexEntry plain = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				plain = AddLexeme(m_createdObjects, "plain", GetMorphTypeOrCreateOne("stem"), "plain gloss",
					GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA)));
			m_view.Clerk.JumpToRecord(plain.Hvo);
			DrainMediatorAndIdleQueues();
			var onlySense = new DetailField("Sense", "Sense", "Senses", null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
				menuId: "mnuDataTree-Sense", objectHvo: plain.SensesOS[0].Hvo);
			var deleteSense = Leaf("mnuDataTree-Sense", "CmdDataTree-Delete-Sense");

			var goneItem = m_view.DeleteItem(leaf, gone);
			var refused = m_view.DeleteItem(deleteSense, onlySense);

			Assert.That(goneItem.IsEnabled, Is.False);
			Assert.That(goneItem.Execute, Is.Null);
			Assert.That(goneItem.Label, Is.EqualTo("Delete Translation" + note));
			Assert.That(refused.IsEnabled, Is.False, "the rule refuses the last item of a required field");
			Assert.That(refused.Label, Does.EndWith(note));
		}

		// A jump is enabled only on a row the rule resolves a target for; with no row the
		// shown entry stands in. The adapter path showed a targetless jump enabled and
		// inert; natively it is disabled.
		[Test]
		public void JumpLeaf_IsEnabledOnlyWhenTheRowOffersATarget()
		{
			var formJump = Leaf(LexemeFormContextMenuId, "CmdLexemeFormJumpToConcordance");
			var entryJump = Leaf(LexemeFormContextMenuId, "CmdEntryJumpToConcordance");
			var lexemeForm = LexemeFormRow();
			var citationForm = Rows().Single(f => f.Field == "CitationForm" && f.ObjectHvo == m_entry.Hvo);

			Assert.That(m_view.JumpItem(formJump, lexemeForm).IsEnabled, Is.True, "the Form field of the lexeme form");
			Assert.That(m_view.JumpItem(formJump, citationForm).IsEnabled, Is.False, "the entry is no form");
			Assert.That(m_view.JumpItem(formJump, null).IsEnabled, Is.False, "no row: the shown entry is no form");
			Assert.That(m_view.JumpItem(entryJump, citationForm).IsEnabled, Is.True);
			Assert.That(m_view.JumpItem(entryJump, lexemeForm).IsEnabled, Is.True, "the entry owning the form");
			Assert.That(m_view.JumpItem(entryJump, null).IsEnabled, Is.True, "no row: the shown entry");
			Assert.That(m_view.JumpItem(entryJump, null).Label, Is.EqualTo("Show Entry in Concordance"));
		}

		[Test]
		public void SwapLeaf_IsAbsentWithoutAnAllomorph_AndConvertLeaves_FollowTheFormsClass()
		{
			var swap = Leaf("mnuDataTree-LexemeForm", "CmdDataTree-Swap-LexemeForm");
			var toProcess = Leaf("mnuDataTree-LexemeForm", "CmdDataTree-Convert-LexemeForm-AffixProcess");
			var toAffixForm = Leaf("mnuDataTree-LexemeForm", "CmdDataTree-Convert-LexemeForm-AffixAllomorph");
			var lexemeForm = LexemeFormRow();

			Assert.That(m_view.SwapLexemeFormItem(swap, lexemeForm)?.IsEnabled, Is.True, "the entry has an allomorph");
			Assert.That(m_view.SwapLexemeFormItem(swap, lexemeForm).Label, Is.EqualTo("Swap Lexeme Form with Allomorph..."));
			Assert.That(m_view.SwapLexemeFormItem(swap, null)?.IsEnabled, Is.True, "no row: the shown entry");
			Assert.That(m_view.ConvertLexemeFormItem(toProcess, lexemeForm), Is.Null, "a stem converts to nothing");
			Assert.That(m_view.ConvertLexemeFormItem(toAffixForm, lexemeForm), Is.Null);
			Assert.That(m_view.ConvertLexemeFormItem(toProcess, null), Is.Null, "no row: no form");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry.AlternateFormsOS.Clear();
				m_entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			});
			var affixForm = LexemeFormRow();

			Assert.That(m_view.SwapLexemeFormItem(swap, affixForm), Is.Null, "nothing to swap with");
			Assert.That(m_view.ConvertLexemeFormItem(toProcess, affixForm)?.IsEnabled, Is.True, "an affix form converts to a process");
			Assert.That(m_view.ConvertLexemeFormItem(toProcess, affixForm).Label, Is.EqualTo("Convert to Affix Process"));
			Assert.That(m_view.ConvertLexemeFormItem(toAffixForm, affixForm), Is.Null, "it already is one");
		}

		// Offered for the row's object (a ghost row's included); disabled where neither the
		// shown entry nor the row's object has the field; absent with no row or no object.
		[Test]
		public void InsertLeaf_IsOfferedOnTheGhostRow_DisabledWithoutATarget_AndAbsentWithoutAnObject()
		{
			var subsense = Leaf("mnuDataTree-Subsenses", "CmdDataTree-Insert-SubSense");
			var extendedNote = Leaf("mnuDataTree-ExtendedNotes", "CmdDataTree-Insert-ExtNote");
			var subsensesRow = Rows().Single(f => f.MenuId == "mnuDataTree-Subsenses");
			var senseRow = Rows().First(f => f.ObjectHvo == m_entry.SensesOS[0].Hvo && f.Kind == DetailFieldKind.Header);
			var etymologyRow = Rows().First(f => f.ObjectHvo == m_etymology.Hvo);
			var noObject = new DetailField("Row", "Row", "Senses", null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null, menuId: subsensesRow.MenuId);

			Assert.That(subsensesRow.GhostPrompt, Is.Not.Null, "precondition: a sense without subsenses shows the prompt row");
			var onGhost = m_view.InsertItem(subsense, subsensesRow);
			Assert.That(onGhost.IsEnabled, Is.True, "the ghost row inserts into the sense it stands in for");
			Assert.That(onGhost.Label, Is.EqualTo("Insert Subsense"));
			Assert.That(m_view.InsertItem(extendedNote, senseRow).IsEnabled, Is.True, "the sense has ExtendedNote");
			var noTarget = m_view.InsertItem(extendedNote, etymologyRow);
			Assert.That(noTarget.IsEnabled, Is.False, "neither the entry nor an etymology has ExtendedNote");
			Assert.That(noTarget.Execute, Is.Null);
			Assert.That(noTarget.Label, Is.EqualTo("Insert Extended Note"));
			Assert.That(m_view.InsertItem(subsense, null), Is.Null, "no row");
			Assert.That(m_view.InsertItem(subsense, noObject), Is.Null, "no object");
		}

		// Only the inserts that need the whole tree stay with the adapter, so their ids do
		// too: SubPossibilities.
		[Test]
		public void InsertLeaves_OutsideTheScope_LeaveTheirIdsWithTheAdapter()
		{
			var authority = m_view.CreateLexiconMenuAuthority(TranslationRow());

			Assert.That(m_view.InsertLeafInScope(Leaf("mnuDataTree-Subsenses", "CmdDataTree-Insert-SubSense")), Is.True);
			Assert.That(m_view.InsertLeafInScope(Leaf("mnuDataTree-AlternateForms-Hotlinks", "CmdDataTree-Insert-AlternateForm")),
				Is.True, "the abstract MoForm: the entry chooses the class");
			Assert.That(m_view.InsertLeafInScope(Leaf("mnuDataTree-AlternateForms", "CmdDataTree-Insert-AffixProcess")), Is.True);
			Assert.That(m_view.InsertLeafInScope(Leaf("mnuDataTree-VariantForms", "CmdDataTree-Insert-VariantForm")),
				Is.True, "a virtual back reference: the dialog adds the variant");
			Assert.That(m_view.InsertLeafInScope(Leaf("mnuDataTree-POS-SubPossibilities", "CmdDataTree-Insert-POS-SubPossibilities")),
				Is.False, "SubPossibilities needs the whole tree");
			Assert.That(authority.Owns("mnuDataTree-POS-SubPossibilities"), Is.False);
			Assert.That(() => authority.Build("mnuDataTree-POS-SubPossibilities",
				Leaf("mnuDataTree-POS-SubPossibilities", "CmdDataTree-Insert-POS-SubPossibilities")),
				Throws.InvalidOperationException, "a leaf outside the scope is not answered");
		}

		// The allomorph inserts follow the entry: an affix process only on an affix entry, an
		// allomorph on any entry.
		[Test]
		public void AllomorphInserts_AreOfferedByTheEntrysMorphType()
		{
			var section = Rows().Single(f => f.MenuId == "mnuDataTree-AlternateForms");
			var affixProcess = Leaf("mnuDataTree-AlternateForms", "CmdDataTree-Insert-AffixProcess");
			var allomorph = Leaf("mnuDataTree-AlternateForms", "CmdDataTree-Insert-AlternateForm");

			Assert.That(m_view.InsertItem(affixProcess, section), Is.Null, "a stem entry has no affix process");
			Assert.That(m_view.InsertItem(affixProcess, LexemeFormRow()), Is.Null, "a row inside the entry asks the entry");
			Assert.That(m_view.InsertItem(allomorph, section)?.IsEnabled, Is.True);
			Assert.That(m_view.InsertItem(allomorph, section).Label, Is.EqualTo("Insert Allomorph"));

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => m_entry.LexemeFormOA.MorphTypeRA =
				GetMorphTypeOrCreateOne("suffix"));

			Assert.That(m_view.InsertItem(affixProcess, section)?.IsEnabled, Is.True, "a suffix entry has affix processes");
		}

		// Offered on any row of the shown entry, for the row's entry or else the shown one.
		[Test]
		public void VariantInsert_IsOfferedForTheEntry_AndAbsentWithoutAnObject()
		{
			var leaf = Leaf("mnuDataTree-VariantForms", "CmdDataTree-Insert-VariantForm");
			var rows = Rows().Where(f => f.MenuId == "mnuDataTree-VariantForms").ToList();
			var section = rows.Single(f => f.HotlinksId != null);
			var banner = rows.Single(f => f.Field == "VariantFormEntryBackRefs");
			var noObject = new DetailField("Row", "Row", "VariantFormEntryBackRefs", null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null, menuId: section.MenuId);

			Assert.That(rows, Has.Count.EqualTo(2), "precondition: the section row and the banner over the variants");
			Assert.That(m_view.InsertItem(leaf, section)?.IsEnabled, Is.True);
			Assert.That(m_view.InsertItem(leaf, banner)?.IsEnabled, Is.True);
			Assert.That(m_view.InsertItem(leaf, section).Label, Is.EqualTo("Insert Variant"));
			Assert.That(m_view.InsertItem(leaf, TranslationRow())?.IsEnabled, Is.True, "a row of another class: the shown entry");
			Assert.That(m_view.InsertItem(leaf, noObject), Is.Null);
		}

		// Move follows the object's position in its sequence. The items are absent on the
		// section row and the prompt row, whose object is the sequence's owner, and with no row.
		[Test]
		public void MoveLeaves_FollowThePosition_AndAreAbsentOffAnItemsOwnRows()
		{
			var up = Leaf("mnuDataTree-Etymology", "CmdDataTree-MoveUp-Etymology");
			var down = Leaf("mnuDataTree-Etymology", "CmdDataTree-MoveDown-Etymology");
			var first = Rows().First(f => f.ObjectHvo == m_etymology.Hvo);
			var second = Rows().First(f => f.ObjectHvo == m_etymology2.Hvo);
			var pronunciations = Rows().Single(f => f.MenuId == "mnuDataTree-Pronunciation" && f.ObjectHvo == m_entry.Hvo);
			var noteExamples = Rows().Where(f => f.MenuId == "mnuDataTree-ExtendedNote-Examples").ToList();
			var moveUpPronunciation = Leaf("mnuDataTree-Pronunciation", "CmdDataTree-MoveUp-Pronunciation");
			var moveUpNoteExample = Leaf("mnuDataTree-ExtendedNote-Examples", "CmdDataTree-MoveUp-ExampleInNote");

			Assert.That(m_view.MoveItem(up, first, up: true).IsEnabled, Is.False, "the first cannot move up");
			Assert.That(m_view.MoveItem(up, first, up: true).Label, Is.EqualTo("Move Etymology Up"));
			Assert.That(m_view.MoveItem(down, first, up: false).IsEnabled, Is.True);
			Assert.That(m_view.MoveItem(up, second, up: true).IsEnabled, Is.True);
			Assert.That(m_view.MoveItem(down, second, up: false).IsEnabled, Is.False, "the last cannot move down");
			Assert.That(m_view.MoveItem(moveUpPronunciation, pronunciations, up: true), Is.Null, "the section row's object is the entry");
			Assert.That(noteExamples.Select(f => f.GhostPrompt != null), Is.EquivalentTo(new[] { false, true }),
				"precondition: the banner over a note's example, and an empty note's prompt row");
			foreach (var row in noteExamples)
				Assert.That(m_view.MoveItem(moveUpNoteExample, row, up: true), Is.Null, row.Label + ": the row's object is the note");
			Assert.That(m_view.MoveItem(up, null, up: true), Is.Null, "no row");
		}

		// Section and prompt rows carry the item id with the owner as their object, so Delete
		// does not act there. A command may name the item's class or the owner's with the field.
		[Test]
		public void DeleteLeaf_IsAbsentWhereTheRowsObjectIsTheOwner()
		{
			var deletePronunciation = Leaf("mnuDataTree-Pronunciation", "CmdDataTree-Delete-Pronunciation");
			var deleteNoteExample = Leaf("mnuDataTree-ExtendedNote-Examples", "CmdDataTree-Delete-ExampleInNote");
			var deletePicture = Leaf("mnuDataTree-Picture", "CmdDataTree-Delete-Picture");
			var deleteVariantInfo = Leaf("mnuDataTree-VariantSpec", "CmdDataTree-Delete-VariantSpec");
			var section = Rows().Single(f => f.MenuId == "mnuDataTree-Pronunciation" && f.ObjectHvo == m_entry.Hvo);
			var formRow = Rows().Single(f => f.Field == "Form" && f.ObjectHvo == m_pronunciation.Hvo);
			var banner = Rows().Single(f => f.MenuId == "mnuDataTree-ExtendedNote-Examples" && f.GhostPrompt == null);

			Assert.That(m_view.DeleteItem(deletePronunciation, section), Is.Null, "its object is the entry");
			Assert.That(m_view.DeleteItem(deletePronunciation, formRow)?.IsEnabled, Is.True);
			Assert.That(m_view.DeleteItem(deleteNoteExample, banner), Is.Null, "its object is the note");
			Assert.That(RecordEditView.CommandActsOn(m_entry, deletePronunciation.Command), Is.False);
			Assert.That(RecordEditView.CommandActsOn(m_pronunciation, deletePronunciation.Command), Is.True);
			Assert.That(RecordEditView.CommandActsOn(m_picture, deletePicture.Command), Is.True,
				"the picture commands name the owning sense and its Pictures field");
			Assert.That(RecordEditView.CommandActsOn(m_entry.SensesOS[0], deletePicture.Command), Is.True, "the sense is of the class");
			Assert.That(RecordEditView.CommandActsOn(m_entry, deleteVariantInfo.Command), Is.True,
				"a command naming no class acts on any object");
			Assert.That(RecordEditView.CommandActsOn(m_entry,
				Leaf("mnuDataTree-MoveReversalIndexEntry", "CmdDataTree-Delete-Subentry").Command), Is.True,
				"a class the model lacks (that command misspells ReversalIndexEntry) hides nothing");
		}

		// The first EntryRefs slot is the complex form's, so the variant ref behind it cannot
		// move up.
		[Test]
		public void VariantInfoMoveUp_IsDisabled_BehindTheComplexForm()
		{
			var up = Leaf("mnuDataTree-VariantSpec", "CmdDataTree-MoveUp-VariantSpec");
			var down = Leaf("mnuDataTree-VariantSpec", "CmdDataTree-MoveDown-VariantSpec");
			var variantInfo = Rows().Single(f => f.MenuId == "mnuDataTree-VariantSpec");
			Assert.That(variantInfo.ObjectHvo, Is.EqualTo(m_variantRef.Hvo), "precondition: the Variant Type row of the entry's own ref");
			Assert.That(m_entry.EntryRefsOS.IndexOf(m_variantRef), Is.EqualTo(1), "precondition: behind the complex form");

			Assert.That(m_view.MoveItem(up, variantInfo, up: true).IsEnabled, Is.False);
			Assert.That(m_view.MoveItem(up, variantInfo, up: true).Label, Is.EqualTo("Move Variant Info Up"));
			Assert.That(m_view.MoveItem(down, variantInfo, up: false).IsEnabled, Is.False, "it is also last");
		}

		// Picture Properties is offered only for a picture. No Avalonia row shows one yet
		// (LT-22772), so the menu is proved on a built row against the adapter.
		[Test]
		public void PictureMenu_IsOfferedForAPicture_AndRendersWhatTheAdapterPathRendered()
		{
			var leaf = Leaf("mnuDataTree-Picture", "CmdDataTree-Properties-Picture");
			var row = PictureRow();

			Assert.That(m_view.PicturePropertiesItem(leaf, row)?.IsEnabled, Is.True);
			Assert.That(m_view.PicturePropertiesItem(leaf, row).Label, Is.EqualTo("Picture Properties"));
			Assert.That(m_view.PicturePropertiesItem(leaf, TranslationRow()), Is.Null, "no picture");
			Assert.That(m_view.PicturePropertiesItem(leaf, null), Is.Null, "no row");

			EnsureAdapter(m_picture.Hvo, "Caption");
			Assert.That(AdapterTree.CurrentSlice?.Object?.Hvo, Is.EqualTo(m_picture.Hvo), "precondition: the adapter renders the picture");
			var ids = new[] { "mnuDataTree-Picture" };
			var before = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null, null));
			var after = Describe(XCoreMenuBridge.CreateMenuItems(Window, ids, null, null,
				m_view.CreateMenuAuthority(MenuRequest(row, DetailMenuKind.SliceMenu))));

			Assert.That(after, Is.EqualTo(before));
			Assert.That(after, Does.Contain("Picture Properties [enabled=True checked=False executes=True]"));
			Assert.That(after, Does.Contain("Move Picture Up [enabled=False"));
			Assert.That(after, Does.Contain("Move Picture Down [enabled=True"));
			Assert.That(after, Does.Contain("Delete Picture [enabled=True"));
		}

		// Tripwire: fails once sense pictures compose, so the synthetic picture rows give way to
		// the real ones.
		[Test]
		public void NoAvaloniaRowBindsThePictureMenu_UntilPicturesCompose()
		{
			Assert.That(m_entry.SensesOS[0].PicturesOS, Has.Count.EqualTo(2), "precondition: the sense has pictures");

			var pictureRows = Rows().Where(f => f.MenuId == "mnuDataTree-Picture").ToList();

			Assert.That(pictureRows, Is.Empty,
				"Sense pictures now compose (LT-22772). Prove mnuDataTree-Picture on the real rows: add it to "
				+ "LexiconLabelMenuIds so the equivalence and no-mediator contracts cover it, replace PictureRow() "
				+ "and the built picture row in LexiconMenuExecutionTests with the composed one, drop the comments "
				+ "tagged LT-22772 in both files, then delete this test.");
		}

		[Test]
		public void InsertMediaLeaf_IsOfferedOnEveryRowOfAnEntry_AndAbsentWithoutAnObject()
		{
			var leaf = Leaf("mnuDataTree-Pronunciation", "CmdInsertMediaFile");
			var formRow = Rows().Single(f => f.Field == "Form" && f.ObjectHvo == m_pronunciation.Hvo);
			var section = Rows().Single(f => f.MenuId == "mnuDataTree-Pronunciation" && f.ObjectHvo == m_entry.Hvo);
			var noObject = new DetailField("Row", "Row", "Pronunciations", null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null, menuId: section.MenuId);

			Assert.That(m_view.InsertMediaItem(leaf, formRow)?.IsEnabled, Is.True);
			Assert.That(m_view.InsertMediaItem(leaf, formRow).Label, Is.EqualTo("Insert Sound or Movie"));
			Assert.That(m_view.InsertMediaItem(leaf, section)?.IsEnabled, Is.True, "the entry's first pronunciation takes the files");
			Assert.That(m_view.InsertMediaItem(leaf, TranslationRow())?.IsEnabled, Is.True, "any row of the entry");
			Assert.That(m_view.InsertMediaItem(leaf, noObject), Is.Null, "no object");
			Assert.That(m_view.InsertMediaItem(leaf, null), Is.Null, "no row");
		}

		[Test]
		public void FindExampleSentence_IsOfferedOnARowWithAnObject()
		{
			var leaf = Leaf("mnuDataTree-Examples", "CmdFindExampleSentence");
			var examples = Rows().Single(f => f.MenuId == "mnuDataTree-Examples");
			var noObject = new DetailField("Row", "Row", "Examples", null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null, menuId: examples.MenuId);

			Assert.That(m_view.GuiControlItem(leaf, examples)?.IsEnabled, Is.True);
			Assert.That(m_view.GuiControlItem(leaf, examples).Label, Is.EqualTo("Find example sentence..."));
			Assert.That(m_view.GuiControlItem(leaf, null), Is.Null, "no row");
			Assert.That(m_view.GuiControlItem(leaf, noObject), Is.Null, "no object");
		}

		[Test]
		public void MergeLeaf_IsEnabledWithACandidate_DisabledWithTheNoteWithout_AndAbsentOnAGhostRow()
		{
			var leaf = Leaf("mnuDataTree-Allomorph", "CmdDataTree-Merge-Allomorph");
			var stem = FormRow(m_allomorph);
			var process = FormRow(m_affixProcess);
			var ghost = new DetailField(stem.StableId + DetailField.GhostStableIdSuffix, "Form", "Form", null,
				DetailFieldKind.Text, EditorClassification.Known, null, null, HostRouting.Inherit, null, null, null,
				menuId: stem.MenuId, objectHvo: stem.ObjectHvo, ghostPrompt: "Add a form");

			Assert.That(m_view.MergeItem(leaf, stem)?.IsEnabled, Is.True, "the lexeme form is of the stem's class");
			Assert.That(m_view.MergeItem(leaf, stem).Label, Is.EqualTo("Merge Allomorph into..."));
			Assert.That(m_view.MergeItem(leaf, process)?.IsEnabled, Is.False, "nothing of the process's class to merge into");
			Assert.That(m_view.MergeItem(leaf, process).Label,
				Is.EqualTo("Merge Allomorph into..." + StringTable.Table.GetString(DetailObjectMerge.CannotMergeKey)));
			Assert.That(m_view.MergeItem(leaf, ghost), Is.Null, "a ghost row has no object to merge");
			Assert.That(m_view.MergeItem(leaf, null), Is.Null, "no row");
		}

		// A variant's form row shows the variant's lexeme form, but Delete Variant and Delete
		// Reference act on the variant entry and its ref to the shown entry.
		[Test]
		public void VariantFormRow_DeleteActsOnTheVariantEntry_AndDeleteReferenceNeedsItsRef()
		{
			var row = VariantFormRow();
			var deleteVariant = Leaf("mnuDataTree-VariantForm", "CmdDataTree-Delete-Variant");
			var deleteReference = Leaf("mnuDataTree-VariantForm", "CmdDataTree-Delete-VariantReference");
			var note = StringTable.Table.GetString(DetailObjectDeletion.CannotDeleteKey);

			Assert.That(m_view.DeleteItem(deleteVariant, row)?.IsEnabled, Is.True, "an entry is unowned, so it can go");
			Assert.That(m_view.DeleteItem(deleteVariant, row).Label, Is.EqualTo("Delete Variant"));
			Assert.That(m_view.DeleteReferenceItem(deleteReference, row)?.IsEnabled, Is.True);
			Assert.That(m_view.DeleteReferenceItem(deleteReference, row).Label, Is.EqualTo("Delete Reference"));

			var allomorph = FormRow(m_allomorph);
			Assert.That(m_view.DeleteItem(deleteVariant, allomorph), Is.Null, "the shown entry's own form is no LexEntry");
			Assert.That(m_view.DeleteReferenceItem(deleteReference, allomorph)?.IsEnabled, Is.False, "no variant, no ref");
			Assert.That(m_view.DeleteReferenceItem(deleteReference, allomorph).Label, Is.EqualTo("Delete Reference" + note));
			var ghost = new DetailField(row.StableId + DetailField.GhostStableIdSuffix, "Variant Form", "Form", null,
				DetailFieldKind.Text, EditorClassification.Known, null, null, HostRouting.Inherit, null, null, null,
				menuId: row.MenuId, objectHvo: m_variant.Hvo, ghostPrompt: "Add a form");
			Assert.That(m_view.DeleteReferenceItem(deleteReference, ghost), Is.Null, "a ghost row offers no delete");
		}

		[Test]
		public void SwapAllomorphLeaf_IsEnabledOnAnAllomorph_DisabledWithoutALexemeForm_AndAbsentOffAForm()
		{
			var leaf = Leaf("mnuDataTree-Allomorph", "CmdDataTree-Swap-Allomorph");
			var stem = FormRow(m_allomorph);

			Assert.That(m_view.SwapAllomorphItem(leaf, stem)?.IsEnabled, Is.True);
			Assert.That(m_view.SwapAllomorphItem(leaf, TranslationRow()), Is.Null, "no form on the row");
			Assert.That(m_view.SwapAllomorphItem(leaf, null), Is.Null, "no row");

			// The fixture is rebuilt per test, so the lexeme form need not come back.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => m_entry.LexemeFormOA = null);
			Assert.That(m_view.SwapAllomorphItem(leaf, stem)?.IsEnabled, Is.False, "nothing to swap with");
		}

		[Test]
		public void RelationLeaves_AreEnabledOnARelationRow_AndAbsentElsewhere()
		{
			var add = Leaf("mnuDataTree-DeleteAddLexReference", "CmdDataTree-Add-ToLexReference");
			var replace = Leaf("mnuDataTree-DeleteReplaceLexReference", "CmdDataTree-Replace-LexReference");
			var edit = Leaf("mnuDataTree-DeleteAddLexReference", "CmdDataTree-EditDetails-LexReference");
			var delete = Leaf("mnuDataTree-DeleteAddLexReference", "CmdDataTree-Delete-LexReference");
			var synonyms = RelationRow(m_synonymSet);
			var antonym = RelationRow(m_antonymPair);
			Assert.That(synonyms.MenuId, Is.EqualTo("mnuDataTree-DeleteAddLexReference"), "precondition");
			Assert.That(antonym.MenuId, Is.EqualTo("mnuDataTree-DeleteReplaceLexReference"), "precondition");
			Assert.That(RelationRow(m_partOfWhole).MenuId, Is.EqualTo("mnuDataTree-DeleteReplaceLexReference"),
				"precondition: a part sees its whole");

			Assert.That(m_view.AddReferenceItem(add, synonyms)?.IsEnabled, Is.True);
			Assert.That(m_view.AddReferenceItem(replace, antonym)?.IsEnabled, Is.True);
			Assert.That(m_view.EditReferenceItem(edit, synonyms)?.IsEnabled, Is.True);
			Assert.That(m_view.DeleteItem(delete, synonyms)?.IsEnabled, Is.True);
			Assert.That(m_view.DeleteItem(delete, synonyms).Label, Is.EqualTo("Delete Relation"));
			Assert.That(m_view.AddReferenceItem(add, TranslationRow()), Is.Null, "no relation on the row");
			Assert.That(m_view.EditReferenceItem(edit, TranslationRow()), Is.Null);
			Assert.That(m_view.AddReferenceItem(add, null), Is.Null, "no row");
		}

		private XWindow Window => m_propertyTable.GetValue<XWindow>("window");

		private DetailField FormRow(IMoForm form) => Rows().Single(f => f.ObjectHvo == form.Hvo && f.Field == "Form");

		private DetailField VariantFormRow()
			=> Rows().Single(f => f.MenuId == "mnuDataTree-VariantForm" && f.ObjectHvo == m_variant.LexemeFormOA.Hvo);

		private DetailField RelationRow(ILexReference relation) => Rows().Single(f => f.ObjectHvo == relation.Hvo);

		private bool HiddenTreeExists => (bool)GetField(m_view, "m_dataTreeInitialized");

		private LegacyDataTree AdapterTree => (LegacyDataTree)GetField(m_view, "m_dataEntryForm");

		private LexiconMenuAuthority NewAuthority()
		{
			var window = Window;
			return new LexiconMenuAuthority(id => XCoreMenuBridge.ResolveMenu(window, id));
		}

		private static DetailMenuItem Echo(DetailMenuLeaf leaf)
			=> new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true);

		private static DetailMenuRequest MenuRequest(DetailField field, DetailMenuKind kind)
			=> DetailMenuRequest.FromAnchor(null, field, kind);

		private IReadOnlyList<DetailField> Rows() => DetailComposer.Compose(m_entry, Cache).Model.Fields;

		private DetailField TranslationRow()
			=> Rows().Single(f => f.Field == "Translation" && f.ObjectHvo == m_translation.Hvo);

		private DetailField LexemeFormRow() => Rows().Single(f => f.MenuId == "mnuDataTree-LexemeForm");

		// The Caption row of the sense's first picture, as the composer will build it once it
		// loads the generated parts (LT-22772).
		private DetailField PictureRow()
			=> new DetailField("Picture/caption@" + m_picture.Hvo, "Caption", "Caption", null, DetailFieldKind.Text,
				EditorClassification.Known, null, null, HostRouting.Inherit, null, null, null,
				menuId: "mnuDataTree-Picture", objectHvo: m_picture.Hvo);

		// Rows carrying an item id with the sequence's owner as object: the Pronunciations
		// section row (the entry) and both rows over a note's examples (the note).
		private bool RowIsTheSequencesOwner(DetailField row, string menuId)
			=> (menuId == "mnuDataTree-Pronunciation" && row.ObjectHvo == m_entry.Hvo)
				|| menuId == "mnuDataTree-ExtendedNote-Examples";

		// The item lines of a described menu, separators dropped.
		private static List<string> ItemLines(string described)
			=> described.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Where(l => l != "---").ToList();

		// Every (row, owned id, menu kind) the entry's rows bind: the label menus first, then
		// the in-string menus, then the hotlinks.
		private List<(DetailField Row, string MenuId, DetailMenuKind Kind)> OwnedMenuRows()
		{
			var rows = Rows();
			var bound = rows.Where(f => LexiconLabelMenuIds.Contains(f.MenuId))
				.Select(f => (f, f.MenuId, DetailMenuKind.SliceMenu));
			bound = bound.Concat(rows.Where(f => LexiconInStringMenuIds.Contains(f.ContextMenuId))
				.Select(f => (f, f.ContextMenuId, DetailMenuKind.ContextMenu)));
			bound = bound.Concat(rows.Where(f => LexiconHotlinksIds.Contains(f.HotlinksId))
				.Select(f => (f, f.HotlinksId, DetailMenuKind.Hotlinks)));
			return bound.ToList();
		}

		private DetailMenuLeaf DeleteTranslationLeaf()
			=> XCoreMenuBridge.ResolveMenu(Window, "mnuDataTree-Translation").Leaves.Single();

		private DetailMenuLeaf Leaf(string menuId, string commandId)
			=> XCoreMenuBridge.ResolveMenu(Window, menuId).Leaves.Single(l => l.CommandId == commandId);

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
			public bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplaySwapLexemeWithAllomorph(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayConvertLexemeForm(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayDataTreeInsert(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayLaunchGuiControl(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayMoveUpObjectInSequence(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayMoveDownObjectInSequence(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayInsertMediaFile(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayPictureProperties(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayDataTreeMerge(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayDataTreeDeleteReference(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayDataTreeAddReference(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayDataTreeEdit(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplaySwapAllomorphWithLexeme(object commandObject, ref UIItemDisplayProperties display)
			{
				Asked = true;
				return false;
			}
			public bool OnDisplayConvertAllomorph(object commandObject, ref UIItemDisplayProperties display)
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

		// A complex form and variant: two allomorphs, a spelling variant, two etymologies,
		// two pronunciations, an antonym and a whole; its sense has an example, pictures,
		// notes and synonyms.
		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "command-entry", stemMorphType, "first gloss", noun);
			m_note = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
			m_entry.SensesOS[0].ExtendedNoteOS.Add(m_note);
			var noteExample = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
			m_note.ExamplesOS.Add(noteExample);
			noteExample.Example.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("in a note", Cache.DefaultVernWs));
			m_note2 = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
			m_entry.SensesOS[0].ExtendedNoteOS.Add(m_note2);
			m_etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(m_etymology);
			m_etymology2 = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(m_etymology2);
			m_pronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			m_entry.PronunciationsOS.Add(m_pronunciation);
			m_pronunciation.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("pro", Cache.DefaultVernWs));
			m_entry.PronunciationsOS.Add(Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create());
			var pictureFolder = DomainObjectServices.FindOrCreateFolder(Cache, LangProjectTags.kflidPictures,
				CmFolderTags.DefaultPictureFolder);
			m_picture = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			m_entry.SensesOS[0].PicturesOS.Add(m_picture);
			m_picture.PictureFileRA = DomainObjectServices.FindOrCreateFile(pictureFolder, "command-picture.jpg");
			var secondPicture = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			m_entry.SensesOS[0].PicturesOS.Add(secondPicture);
			secondPicture.PictureFileRA = DomainObjectServices.FindOrCreateFile(pictureFolder, "second-picture.jpg");
			m_allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_entry.AlternateFormsOS.Add(m_allomorph);
			m_allomorph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("command-allomorph", Cache.DefaultVernWs));
			m_affixProcess = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
			m_entry.AlternateFormsOS.Add(m_affixProcess);
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
			var baseEntry = AddLexeme(m_createdObjects, "base", stemMorphType, "base gloss", noun);
			m_variantRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			m_entry.EntryRefsOS.Add(m_variantRef);
			m_variantRef.RefType = LexEntryRefTags.krtVariant;
			m_variantRef.ComponentLexemesRS.Add(baseEntry);
			m_variantRef.VariantEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
				.GetObject(LexEntryTypeTags.kguidLexTypSpellingVar));

			m_variant = AddLexeme(m_createdObjects, "command-variant", stemMorphType, "variant gloss", noun);
			var variantRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			m_variant.EntryRefsOS.Add(variantRef);
			variantRef.RefType = LexEntryRefTags.krtVariant;
			variantRef.ComponentLexemesRS.Add(m_entry);
			variantRef.VariantEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
				.GetObject(LexEntryTypeTags.kguidLexTypSpellingVar));

			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			m_synonymSet = AddRelation("Synonyms", LexRefTypeTags.MappingTypes.kmtSenseCollection,
				m_entry.SensesOS[0], component.SensesOS[0], baseEntry.SensesOS[0]);
			m_antonymPair = AddRelation("Antonym", LexRefTypeTags.MappingTypes.kmtEntryPair, m_entry, baseEntry);
			m_partOfWhole = AddRelation("Whole", LexRefTypeTags.MappingTypes.kmtEntryTree, component, m_entry);
		}

		// A relation of a new type of the given mapping, over the targets in that order.
		private ILexReference AddRelation(string typeName, LexRefTypeTags.MappingTypes mapping, params ICmObject[] targets)
		{
			var type = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(type);
			m_createdObjects.Add(type);
			type.MappingType = (int)mapping;
			type.Name.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(typeName, Cache.DefaultAnalWs));
			var relation = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			type.MembersOC.Add(relation);
			foreach (var target in targets)
				relation.TargetsRS.Add(target);
			return relation;
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
