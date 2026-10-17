// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// End-to-end proof that the Lexicon menu commands the native authority answers change
	/// the model through the product <see cref="RecordEditView"/>: the item's own Execute runs
	/// the host, which asks for confirmation, edits in one undoable step and tells the re-show
	/// where to put the keyboard. Jumps post the link the view follows.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class LexiconMenuExecutionTests : XWorksAppTestBase
	{
		private const string LexemeFormMenuId = "mnuDataTree-LexemeForm";
		private const string VariantFormContextMenuId = "mnuDataTree-VariantFormContext";

		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private ILexSense m_sense1;
		private ILexSense m_sense2;
		private IMoStemAllomorph m_allomorph;
		private ILexEntry m_variant;
		private ILexEntry m_affix;
		private SIL.LCModel.IText m_text;
		private ILexExampleSentence m_example;
		private ICmTranslation m_translation;
		private ILexExtendedNote m_note1;
		private ILexExtendedNote m_note2;
		private ILexEtymology m_etymology1;
		private ILexEtymology m_etymology2;
		private ILexEntryRef m_complexFormRef;
		private RecordEditView m_view;

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

		[Test]
		public void DeleteTranslation_RemovesIt_InOneUndoableStep_WhenTheUserConfirms()
		{
			var confirmed = 0;
			m_view.DeleteConfirmation = ui =>
			{
				Assert.That(ui.Object.Hvo, Is.EqualTo(m_translation.Hvo), "the confirmation names the row's object");
				confirmed++;
				return true;
			};
			var translationHvo = m_translation.Hvo;

			InvokeLabelMenuItem(TranslationRow(), "Delete Translation");

			Assert.That(confirmed, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(translationHvo), Is.False, "the translation is gone");
			Assert.That(m_example.TranslationsOC, Is.Empty);
			Assert.That(Rows(m_entry).Any(f => f.ObjectHvo == translationHvo), Is.False,
				"the recomposed detail view has no row for it");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_example.TranslationsOC, Has.Count.EqualTo(1), "one Undo brings the translation back");
		}

		[Test]
		public void DeleteTranslation_LeavesTheModelAlone_WhenTheUserDeclines()
		{
			m_view.DeleteConfirmation = ui => false;

			InvokeLabelMenuItem(TranslationRow(), "Delete Translation");

			Assert.That(m_translation.IsValidObject, Is.True);
			Assert.That(m_example.TranslationsOC, Has.Count.EqualTo(1));
		}

		[Test]
		public void DeleteComplexFormInfo_RemovesTheEntryRef()
		{
			m_view.DeleteConfirmation = ui => true;
			var row = Rows(m_entry).Single(f => f.MenuId == "mnuDataTree-ComplexFormSpec");
			Assert.That(row.ObjectHvo, Is.EqualTo(m_complexFormRef.Hvo), "precondition: the row shows the entry's own ref");

			InvokeLabelMenuItem(row, "Delete Complex Form Info");

			Assert.That(m_entry.EntryRefsOS, Is.Empty);
			Assert.That(m_entry.IsValidObject, Is.True, "only the ref goes, not the entry");
		}

		// Candidates: the rows before the object's rows (its own and its descendants'), nearest
		// first, then the rows after. The header before a last translation dies with it.
		[Test]
		public void RowsToFocusAfterDeleting_AreTheNeighbours_BeforeThenAfter_NearestFirst()
		{
			var model = DetailComposer.Compose(m_entry, Cache).Model;
			var fields = model.Fields.ToList();
			var ownedByTranslation = fields.Where(f => f.ObjectHvo == m_translation.Hvo).ToList();
			var first = fields.IndexOf(ownedByTranslation.First());
			var last = fields.IndexOf(ownedByTranslation.Last());
			Assert.That(first, Is.GreaterThan(1), "precondition: rows precede the translation's");
			Assert.That(fields[first - 1].Field, Is.EqualTo("Translations"), "precondition: the section header is just before");
			Assert.That(fields[first - 2].ObjectHvo, Is.EqualTo(m_example.Hvo), "precondition: the example's row is next");

			var candidates = RecordEditView.RowsToFocusAfterDeleting(model, m_translation);

			var expected = Enumerable.Range(0, first).Reverse().Select(i => fields[i].RowAutomationId)
				.Concat(Enumerable.Range(last + 1, fields.Count - last - 1).Select(i => fields[i].RowAutomationId));
			Assert.That(candidates, Is.EqualTo(expected.ToList()));
			Assert.That(RecordEditView.RowsToFocusAfterDeleting(model, m_example)[0],
				Is.EqualTo(fields[fields.FindIndex(f => f.ObjectHvo == m_example.Hvo) - 1].RowAutomationId),
				"the translation's rows belong to the example that owns it");
			Assert.That(RecordEditView.RowsToFocusAfterDeleting(model, m_entry), Is.Empty,
				"every row belongs to the entry, so nothing is left to focus");
			Assert.That(RecordEditView.RowsToFocusAfterDeleting(null, m_translation), Is.Empty);
		}

		[Test]
		public void SwapLexemeForm_MakesThePickedAllomorphTheLexemeForm_InOneUndoableStep()
		{
			var oldLexemeForm = m_entry.LexemeFormOA;
			ILexEntry asked = null;
			m_view.AllomorphToSwap = entry =>
			{
				asked = entry;
				return m_allomorph;
			};

			InvokeLabelMenuItem(LexemeFormRow(m_entry), "Swap Lexeme Form with Allomorph...");

			Assert.That(asked, Is.SameAs(m_entry), "the user picks among the shown entry's allomorphs");
			Assert.That(m_entry.LexemeFormOA, Is.SameAs(m_allomorph));
			Assert.That(m_entry.AlternateFormsOS, Is.EqualTo(new IMoForm[] { oldLexemeForm }));
			Assert.That(LexemeFormRow(m_entry).ObjectHvo, Is.EqualTo(m_allomorph.Hvo), "the recomposed row shows the new lexeme form");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_entry.LexemeFormOA, Is.SameAs(oldLexemeForm), "one Undo swaps them back");
			Assert.That(m_entry.AlternateFormsOS, Is.EqualTo(new IMoForm[] { m_allomorph }));
		}

		[Test]
		public void SwapLexemeForm_LeavesTheModelAlone_WhenTheUserCancels()
		{
			var oldLexemeForm = m_entry.LexemeFormOA;
			m_view.AllomorphToSwap = entry => null;

			InvokeLabelMenuItem(LexemeFormRow(m_entry), "Swap Lexeme Form with Allomorph...");

			Assert.That(m_entry.LexemeFormOA, Is.SameAs(oldLexemeForm));
			Assert.That(m_entry.AlternateFormsOS, Is.EqualTo(new IMoForm[] { m_allomorph }));
		}

		[Test]
		public void ConvertLexemeForm_ReplacesTheForm_WithoutAskingWhenNothingIsLost()
		{
			ShowEntry(m_affix);
			var oldForm = m_affix.LexemeFormOA;
			m_view.ConversionLossAccepted = warning => throw new InvalidOperationException("nothing is lost: no question");

			InvokeLabelMenuItem(LexemeFormRow(m_affix), "Convert to Affix Process");

			Assert.That(m_affix.LexemeFormOA, Is.InstanceOf<IMoAffixProcess>());
			Assert.That(m_affix.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("ing"), "the form text carries over");
			Assert.That(oldForm.IsValidObject, Is.False);
			Assert.That(LexemeFormRow(m_affix).ObjectHvo, Is.EqualTo(m_affix.LexemeFormOA.Hvo), "the recomposed row shows the new form");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_affix.LexemeFormOA, Is.InstanceOf<IMoAffixAllomorph>(), "one Undo restores the old form");
		}

		[Test]
		public void ConvertLexemeForm_AsksAboutTheLoss_AndConvertsOnlyWhenTheUserAccepts()
		{
			ShowEntry(m_affix);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var environment = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(environment);
				((IMoAffixAllomorph)m_affix.LexemeFormOA).PhoneEnvRC.Add(environment);
			});
			var warnings = new List<string>();
			m_view.ConversionLossAccepted = warning =>
			{
				warnings.Add(warning);
				return false;
			};

			InvokeLabelMenuItem(LexemeFormRow(m_affix), "Convert to Affix Process");

			Assert.That(warnings, Is.EqualTo(new[] { "The environment will be lost! Do you still want to continue?" }));
			Assert.That(m_affix.LexemeFormOA, Is.InstanceOf<IMoAffixAllomorph>(), "declined: nothing changes");

			m_view.ConversionLossAccepted = warning => true;
			InvokeLabelMenuItem(LexemeFormRow(m_affix), "Convert to Affix Process");

			Assert.That(m_affix.LexemeFormOA, Is.InstanceOf<IMoAffixProcess>(), "accepted: the form converts");
		}

		[Test]
		public void InStringJumps_FollowTheLinkTheRowResolves()
		{
			var lexemeForm = LexemeFormRow(m_entry);
			var variantForm = Rows(m_entry).Single(f => f.ContextMenuId == VariantFormContextMenuId);
			Assert.That(variantForm.ObjectHvo, Is.EqualTo(m_variant.LexemeFormOA.Hvo), "precondition: the variant's form row");

			var toConcordance = CaptureFollowLink(() => InvokeInStringMenuItem(lexemeForm, "Show Lexeme Form in Concordance"));
			Assert.That(toConcordance?.ToolName, Is.EqualTo("concordance"));
			Assert.That(toConcordance.TargetGuid, Is.EqualTo(m_entry.LexemeFormOA.Guid));

			var toEntry = CaptureFollowLink(() => InvokeInStringMenuItem(lexemeForm, "Show Entry in Concordance"));
			Assert.That(toEntry?.TargetGuid, Is.EqualTo(m_entry.Guid), "the entry owning the form");

			var toVariant = CaptureFollowLink(() => InvokeInStringMenuItem(variantForm, "Show Entry in Lexicon"));
			Assert.That(toVariant?.ToolName, Is.EqualTo("lexiconEdit"));
			Assert.That(toVariant.TargetGuid, Is.EqualTo(m_variant.Guid), "the variant entry, not the shown one");
		}

		// The one jump that writes: a text without a notebook record gets one, associated with
		// it, and the jump shows that record. A second jump reuses it.
		[Test]
		public void NotebookJump_FromATextWithoutARecord_CreatesTheRecord_AndShowsIt()
		{
			var leaf = XCoreMenuBridge.ResolveMenu(m_propertyTable.GetValue<XWindow>("window"), "mnuTextInfo-Notebook")
				.Leaves.Single();
			var row = new DetailField("Text/notebook@" + m_text.Hvo, "Notebook Record", null, null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
				menuId: "mnuTextInfo-Notebook", objectHvo: m_text.Hvo);
			Assert.That(m_text.AssociatedNotebookRecord, Is.Null, "precondition: the text has no record");
			var item = m_view.JumpItem(leaf, row);
			Assert.That(item.Execute, Is.Not.Null, "the jump is offered before the record exists");

			var link = CaptureFollowLink(item.Execute);

			var record = m_text.AssociatedNotebookRecord;
			Assert.That(record, Is.Not.Null, "the jump created the record and associated it with the text");
			m_createdObjects.Add(record);
			Assert.That(link?.ToolName, Is.EqualTo("notebookEdit"));
			Assert.That(link.TargetGuid, Is.EqualTo(record.Guid), "the jump shows the new record, not the text");

			var again = CaptureFollowLink(m_view.JumpItem(leaf, row).Execute);
			Assert.That(m_text.AssociatedNotebookRecord, Is.SameAs(record), "no second record");
			Assert.That(again?.TargetGuid, Is.EqualTo(record.Guid));
		}

		[Test]
		public void InsertSubsense_FromTheSubsensesRow_AppendsItUnderTheSense_InOneUndoableStep()
		{
			var row = Rows(m_entry).Single(f => f.MenuId == "mnuDataTree-Subsenses" && f.ObjectHvo == m_sense1.Hvo);
			Assert.That(row.GhostPrompt, Is.Not.Null, "precondition: no subsense yet, so the prompt row binds the id");

			InvokeLabelMenuItem(row, "Insert Subsense");

			Assert.That(m_sense1.SensesOS, Has.Count.EqualTo(1));
			var subsense = m_sense1.SensesOS[0];
			Assert.That(subsense.MorphoSyntaxAnalysisRA, Is.SameAs(m_sense1.MorphoSyntaxAnalysisRA),
				"the new subsense takes its parent's grammatical info");
			Assert.That(Rows(m_entry).Any(f => f.ObjectHvo == subsense.Hvo), Is.True, "the recomposed view shows it");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense1.SensesOS, Is.Empty, "one Undo removes it");
		}

		[Test]
		public void InsertSense_FromTheFirstSensesHotlinks_GoesRightAfterIt()
		{
			InvokeHotlinksItem(HeaderRowOf(m_sense1), "Insert Sense");

			Assert.That(m_entry.SensesOS, Has.Count.EqualTo(3));
			Assert.That(m_entry.SensesOS[0], Is.SameAs(m_sense1));
			Assert.That(m_entry.SensesOS[2], Is.SameAs(m_sense2), "the new sense sits between the two");
			Assert.That(m_entry.SensesOS[1].MorphoSyntaxAnalysisRA, Is.SameAs(m_sense1.MorphoSyntaxAnalysisRA),
				"the new sense takes the grammatical info of the one before it");
		}

		[Test]
		public void InsertExample_FromTheSensesHotlinks_GoesLast()
		{
			InvokeHotlinksItem(HeaderRowOf(m_sense1), "Insert Example");

			Assert.That(m_sense1.ExamplesOS, Has.Count.EqualTo(2));
			Assert.That(m_sense1.ExamplesOS[0], Is.SameAs(m_example));
		}

		[Test]
		public void InsertExtendedNote_FromTheFirstNotesHotlinks_GoesRightAfterIt()
		{
			InvokeHotlinksItem(HeaderRowOf(m_note1), "Insert Extended Note");

			Assert.That(m_sense1.ExtendedNoteOS, Has.Count.EqualTo(3));
			Assert.That(m_sense1.ExtendedNoteOS[0], Is.SameAs(m_note1));
			Assert.That(m_sense1.ExtendedNoteOS[2], Is.SameAs(m_note2), "the new note sits between the two");
		}

		// The Extended Notes section shows only with data, so no prompt row inserts the first.
		[Test]
		public void InsertExtendedNote_FromTheSectionRow_GoesLast()
		{
			var section = Rows(m_entry).Single(f => f.MenuId == "mnuDataTree-ExtendedNotes" && f.ObjectHvo == m_sense1.Hvo);
			Assert.That(section.Kind, Is.EqualTo(DetailFieldKind.Header), "precondition: the section header binds the id");
			Assert.That(Rows(m_entry).Any(f => f.MenuId == "mnuDataTree-ExtendedNotes" && f.ObjectHvo == m_sense2.Hvo), Is.False,
				"precondition: the second sense has no note, so no section row");

			InvokeLabelMenuItem(section, "Insert Extended Note");

			Assert.That(m_sense1.ExtendedNoteOS, Has.Count.EqualTo(3));
			Assert.That(m_sense1.ExtendedNoteOS[1], Is.SameAs(m_note2), "the new note is last");
		}

		[Test]
		public void InsertEtymology_FromTheFirstEtymologysHotlinks_GoesRightAfterIt()
		{
			InvokeHotlinksItem(HeaderRowOf(m_etymology1), "Insert Etymology");

			Assert.That(m_entry.EtymologyOS, Has.Count.EqualTo(3));
			Assert.That(m_entry.EtymologyOS[0], Is.SameAs(m_etymology1));
			Assert.That(m_entry.EtymologyOS[2], Is.SameAs(m_etymology2), "the new etymology sits between the two");
		}

		[Test]
		public void InsertDetailObject_ReturnsTheNewObject_AndNothingWhenTheRowsObjectIsGone()
		{
			var leaf = XCoreMenuBridge.ResolveMenu(m_propertyTable.GetValue<XWindow>("window"), "mnuDataTree-Etymology-Hotlinks")
				.Leaves.Single();
			var row = HeaderRowOf(m_etymology2);
			var gone = new DetailField(row.StableId, row.Label, row.Field, null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
				hotlinksId: row.HotlinksId, objectHvo: int.MaxValue - 1);

			var created = m_view.InsertDetailObject(row, leaf.Command);

			Assert.That(created, Is.InstanceOf<ILexEtymology>());
			Assert.That(created.Owner, Is.SameAs(m_entry));
			Assert.That(m_view.InsertDetailObject(gone, leaf.Command), Is.Null);
			Assert.That(m_entry.EtymologyOS, Has.Count.EqualTo(3));
		}

		// Builds the row's label menu as the host does and runs the named item's own Execute.
		private void InvokeLabelMenuItem(DetailField row, string label)
			=> Invoke(RecordEditView.ComposeSliceMenuIds(row.MenuId, row.IsMultiStringRow).ToArray(), row,
				DetailMenuKind.SliceMenu, label);

		// Builds the row's in-string menu as the host does and runs the named item's own Execute.
		private void InvokeInStringMenuItem(DetailField row, string label)
			=> Invoke(RecordEditView.ComposeInStringMenuIds(row.ContextMenuId).ToArray(), row,
				DetailMenuKind.ContextMenu, label);

		// Builds the row's hotlinks menu as the host does and runs the named item's own Execute.
		private void InvokeHotlinksItem(DetailField row, string label)
			=> Invoke(new[] { row.HotlinksId }, row, DetailMenuKind.Hotlinks, label);

		private void Invoke(string[] ids, DetailField row, DetailMenuKind kind, string label)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var request = DetailMenuRequest.FromAnchor(null, row, kind);
			var items = XCoreMenuBridge.CreateMenuItems(window, ids, null, null, m_view.CreateMenuAuthority(request));
			var item = items.FirstOrDefault(i => i.Label == label);
			Assert.That(item, Is.Not.Null, "the menu offers '{0}'", label);
			Assert.That(item.Execute, Is.Not.Null, "'{0}' is enabled", label);
			item.Execute();
			DrainMediatorAndIdleQueues();
		}

		// Runs an action and returns the link it posts, once the post has been delivered.
		private FwLinkArgs CaptureFollowLink(Action action)
			=> FollowLinkSpy.Capture(m_propertyTable.GetValue<XWindow>("window").Mediator, action,
				DrainMediatorAndIdleQueues);

		private void ShowEntry(ILexEntry entry)
		{
			m_view.Clerk.JumpToRecord(entry.Hvo);
			DrainMediatorAndIdleQueues();
		}

		private IReadOnlyList<DetailField> Rows(ILexEntry entry) => DetailComposer.Compose(entry, Cache).Model.Fields;

		private DetailField TranslationRow()
			=> Rows(m_entry).Single(f => f.Field == "Translation" && f.ObjectHvo == m_translation.Hvo);

		private DetailField LexemeFormRow(ILexEntry entry) => Rows(entry).Single(f => f.MenuId == LexemeFormMenuId);

		// The object's item header row, the one its hotlinks hang off.
		private DetailField HeaderRowOf(ICmObject obj)
			=> Rows(m_entry).First(f => f.ObjectHvo == obj.Hvo && f.Kind == DetailFieldKind.Header && f.HotlinksId != null);

		// A complex-form stem entry with a spelling variant, an allomorph, two etymologies and
		// two senses (the first with a translated example and two notes); a suffix; a text.
		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "command-entry", stemMorphType, "first gloss", noun);
			m_sense1 = m_entry.SensesOS[0];
			m_sense2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(m_sense2);
			m_note1 = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
			m_sense1.ExtendedNoteOS.Add(m_note1);
			m_note2 = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
			m_sense1.ExtendedNoteOS.Add(m_note2);
			m_etymology1 = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(m_etymology1);
			m_etymology2 = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(m_etymology2);
			m_allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_entry.AlternateFormsOS.Add(m_allomorph);
			m_allomorph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("command-allomorph", Cache.DefaultVernWs));
			m_example = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
			m_sense1.ExamplesOS.Add(m_example);
			m_example.Example.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("una casa", Cache.DefaultVernWs));
			var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>()
				.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
			m_translation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(m_example, type);
			m_translation.Translation.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("a house", Cache.DefaultAnalWs));

			var component = AddLexeme(m_createdObjects, "part", stemMorphType, "part gloss", noun);
			m_complexFormRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			m_entry.EntryRefsOS.Add(m_complexFormRef);
			m_complexFormRef.RefType = LexEntryRefTags.krtComplexForm;
			m_complexFormRef.ComponentLexemesRS.Add(component);
			m_complexFormRef.PrimaryLexemesRS.Add(component);
			m_complexFormRef.ComplexEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
				.GetObject(LexEntryTypeTags.kguidLexTypCompound));

			m_variant = AddLexeme(m_createdObjects, "command-variant", stemMorphType, "variant gloss", noun);
			var variantRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			m_variant.EntryRefsOS.Add(variantRef);
			variantRef.RefType = LexEntryRefTags.krtVariant;
			variantRef.ComponentLexemesRS.Add(m_entry);
			variantRef.VariantEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
				.GetObject(LexEntryTypeTags.kguidLexTypSpellingVar));

			m_affix = AddLexeme(m_createdObjects, "ing", GetMorphTypeOrCreateOne("suffix"), "suffix gloss", noun);
			Assert.That(m_affix.LexemeFormOA, Is.InstanceOf<IMoAffixAllomorph>(), "precondition: a suffix entry has an affix form");

			m_text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			m_createdObjects.Add(m_text);
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
	}
}
